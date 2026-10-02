using System.Net;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Workflow;
using Nexora.Modules.Finance.Infrastructure;
using Nexora.Modules.Finance.Domain;
using static Nexora.Api.Tests.WorkflowTestClient;
namespace Nexora.Api.Tests;
public sealed class WorkflowDeliveryTests
{
 private sealed class Transport:IWorkflowTransport
 {
  public int Calls;public bool Fail;public Dictionary<string,string>? Last;
  public Task<WorkflowActionResult> DeliverAsync(WorkflowConnector connector,Dictionary<string,string> request,Guid actionId,string kind,CancellationToken ct){Calls++;Last=request;if(Fail)throw new TimeoutException();return Task.FromResult(new WorkflowActionResult("Accepted",new(){{"status","delivered"}}));}
 }
 private static async Task<JsonNode> Prepare(HttpClient client,string entity,string action,Dictionary<string,string> inputs,string onError="stop")
 {
  var graph=new WorkflowGraph(2,entity,"manual","action",[new("action","action","Action",Next:"end",ActionKey:action,Inputs:inputs,OnError:onError),new("end","end","Complete")]);var draft=await Json(await Send(client,"/api/v1/workflows/definitions",HttpMethod.Post,new{name="Delivery",graph}));var published=await Json(await Send(client,$"/api/v1/workflows/definitions/{Id(draft)}/publish",HttpMethod.Post,new{version=Version(draft)}));return await Json(await Send(client,$"/api/v1/workflows/definitions/{Id(draft)}/activate",HttpMethod.Post,new{version=Version(published["definition"]!)}));
 }
 private static async Task<JsonNode> Start(HttpClient client,JsonNode definition,string recordId)=>await Json(await Send(client,$"/api/v1/workflows/definitions/{Id(definition)}/execute",HttpMethod.Post,new{recordId,requestKey=Guid.NewGuid().ToString()}),HttpStatusCode.Accepted);
 private static async Task Tick(IdentityApiFactory app,int count=1){var worker=new WorkflowWorker(app.Services.GetRequiredService<IServiceScopeFactory>(),app.Services.GetRequiredService<WorkflowExecutionContext>(),NullLogger<WorkflowWorker>.Instance);for(var i=0;i<count;i++)await worker.TickAsync(default);}
 private static Task<JsonNode> Connector(HttpClient client,string kind)=>Send(client,"/api/v1/workflows/connectors",HttpMethod.Post,new{name="Provider",kind,endpoint=kind=="smtp"?"smtp.example.test":"https://api.example.test/",port=587,username="tester",secret="test-secret-value",sender="sender@example.test",model="",isActive=true}).ContinueWith(async response=>await Json(await response)).Unwrap();
 [Fact] public async Task Email_templates_resolve_values_and_committed_delivery_is_not_sent_twice()
 {
  var transport=new Transport();using var app=new IdentityApiFactory(null,transport);using var client=await Login(app);var contact=await Contact(client);var connector=await Connector(client,"smtp");Assert.True(connector["hasSecret"]!.GetValue<bool>());Assert.Null(connector["protectedSecret"]);
  var template=await Json(await Send(client,"/api/v1/workflows/email-templates",HttpMethod.Post,new{name="Review",subject="Welcome {{record.label}}",body="Hello {{record.label}}"}));var definition=await Prepare(client,"contacts","communication.email.send",new(){{"connectorId",Id(connector)},{"templateId",Id(template)},{"recipient","recipient@example.test"}});var run=await Start(client,definition,Id(contact));await Tick(app,2);Assert.Equal(1,transport.Calls);Assert.Equal("Welcome Workflow Contact",transport.Last!["subject"]);
  await using(var scope=app.Services.CreateAsyncScope()){var db=scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();var saved=await db.Set<WorkflowConnector>().IgnoreQueryFilters().SingleAsync();Assert.DoesNotContain("test-secret-value",saved.ProtectedSecret);var row=await db.Set<WorkflowRun>().IgnoreQueryFilters().SingleAsync();var principal=await scope.ServiceProvider.GetRequiredService<IWorkflowPrincipalDirectory>().ResolveAsync(row.TenantId,row.ActorId,default);using var identity=app.Services.GetRequiredService<WorkflowExecutionContext>().Enter(principal!);var action=scope.ServiceProvider.GetServices<IWorkflowActionHandler>().Single(h=>h.Descriptor.Key=="communication.email.send");await action.ExecuteDetailedAsync(new(row.Id,"action",WorkflowProcessor.ActionId(row.Id,"action"),row.ActorId,row.RecordLabel,row.RecordId),transport.Last!,default);}
  Assert.Equal(1,transport.Calls);Assert.Single((await Json(await client.GetAsync("/api/v1/workflows/deliveries"))).AsArray());
 }
 [Fact] public async Task Ambiguous_provider_outcomes_require_explicit_reconciliation_before_retry()
 {
  var transport=new Transport{Fail=true};using var app=new IdentityApiFactory(null,transport);using var client=await Login(app);var contact=await Contact(client);var connector=await Connector(client,"http");var definition=await Prepare(client,"contacts","integration.http.post",new(){{"connectorId",Id(connector)},{"path","submit"},{"body","{\"name\":\"{{record.label}}\"}"}});var run=await Start(client,definition,Id(contact));await Tick(app);var failed=(await Json(await client.GetAsync("/api/v1/workflows/runs/"+Id(run))))["run"]!;Assert.Equal("failed",failed["status"]!.GetValue<string>());await Json(await Send(client,"/api/v1/workflows/runs/"+Id(run)+"/retry",HttpMethod.Post,new{version=Version(failed)}));await Tick(app);Assert.Equal(1,transport.Calls);
  var delivery=(await Json(await client.GetAsync("/api/v1/workflows/deliveries")))[0]!;Assert.Equal("unknown",delivery["status"]!.GetValue<string>());Assert.Equal(HttpStatusCode.BadRequest,(await Send(client,"/api/v1/workflows/deliveries/"+Id(delivery)+"/reconcile",HttpMethod.Post,new{version=Version(delivery),confirmedNotDelivered=false})).StatusCode);Assert.Equal(HttpStatusCode.NoContent,(await Send(client,"/api/v1/workflows/deliveries/"+Id(delivery)+"/reconcile",HttpMethod.Post,new{version=Version(delivery),confirmedNotDelivered=true})).StatusCode);
  transport.Fail=false;failed=(await Json(await client.GetAsync("/api/v1/workflows/runs/"+Id(run))))["run"]!;await Json(await Send(client,"/api/v1/workflows/runs/"+Id(run)+"/retry",HttpMethod.Post,new{version=Version(failed)}));await Tick(app,2);Assert.Equal(2,transport.Calls);Assert.Equal("completed",(await Json(await client.GetAsync("/api/v1/workflows/runs/"+Id(run))))["run"]!["status"]!.GetValue<string>());
 }
 [Fact] public async Task Finance_action_posts_once_with_domain_amounts_and_atomic_receipt()
 {
  using var app=new IdentityApiFactory();using var client=await Login(app);var contact=await Contact(client);var product=await Json(await Send(client,"/api/v1/finance/products",HttpMethod.Post,new{name="Service",sku="service",unitPrice=10,currency="GBP",vatBasisPoints=2000}));var order=await Json(await Send(client,"/api/v1/finance/orders",HttpMethod.Post,new{customerId=Id(contact),lines=new[]{new{productId=Id(product),quantity=2}}}));var definition=await Prepare(client,"orders","finance.order.post",new(){{"orderId","{{record.id}}"}});var run=await Start(client,definition,Id(order));await Tick(app,2);var invoices=await Json(await client.GetAsync("/api/v1/finance/invoices"));Assert.Single(invoices.AsArray());Assert.Equal(24,invoices[0]!["total"]!.GetValue<decimal>());
  await using var scope=app.Services.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();var row=await db.Set<WorkflowRun>().IgnoreQueryFilters().SingleAsync();var principal=await scope.ServiceProvider.GetRequiredService<IWorkflowPrincipalDirectory>().ResolveAsync(row.TenantId,row.ActorId,default);using var identity=app.Services.GetRequiredService<WorkflowExecutionContext>().Enter(principal!);var handler=scope.ServiceProvider.GetServices<IWorkflowActionHandler>().Single(h=>h.Descriptor.Key=="finance.order.post");await handler.ExecuteDetailedAsync(new(row.Id,"action",WorkflowProcessor.ActionId(row.Id,"action"),row.ActorId,row.RecordLabel,row.RecordId),new(){{"orderId",Id(order)}},default);Assert.Equal(1,await scope.ServiceProvider.GetRequiredService<FinanceDbContext>().Set<PostedInvoice>().CountAsync());
 }
 [Fact] public async Task Archiving_the_current_record_does_not_prevent_the_workflow_from_completing()
 {
  using var app=new IdentityApiFactory();using var client=await Login(app);var contact=await Contact(client);var definition=await Prepare(client,"contacts","crm.record.archive",new(){{"recordId","{{record.id}}"}});var run=await Start(client,definition,Id(contact));await Tick(app,2);Assert.Equal("completed",(await Json(await client.GetAsync("/api/v1/workflows/runs/"+Id(run))))["run"]!["status"]!.GetValue<string>());
 }
}
