using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Identity;
using Nexora.Modules.Workflow;
using static Nexora.Api.Tests.WorkflowTestClient;
namespace Nexora.Api.Tests;
public sealed class WorkflowOperationsTests
{
 private static WorkflowNode End()=>new("end","end","Complete");
 private static async Task Tick(IdentityApiFactory app,int count=1){var worker=new WorkflowWorker(app.Services.GetRequiredService<IServiceScopeFactory>(),app.Services.GetRequiredService<WorkflowExecutionContext>(),NullLogger<WorkflowWorker>.Instance);for(var i=0;i<count;i++)await worker.TickAsync(default);}
 private static async Task<JsonNode> Prepare(HttpClient client,WorkflowGraph graph){var draft=await Json(await Send(client,"/api/v1/workflows/definitions",HttpMethod.Post,new{name="Operations test",graph}));var publish=await Json(await Send(client,$"/api/v1/workflows/definitions/{Id(draft)}/publish",HttpMethod.Post,new{version=Version(draft)}));return await Json(await Send(client,$"/api/v1/workflows/definitions/{Id(draft)}/activate",HttpMethod.Post,new{version=Version(publish["definition"]!)}));}
 private static async Task<JsonNode> Start(HttpClient client,JsonNode definition,string target)=>await Json(await Send(client,$"/api/v1/workflows/definitions/{Id(definition)}/execute",HttpMethod.Post,new{recordId=target,requestKey=Guid.NewGuid().ToString()}),HttpStatusCode.Accepted);
 private static async Task Due(IdentityApiFactory app){await using var scope=app.Services.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();await db.Set<WorkflowRun>().IgnoreQueryFilters().ExecuteUpdateAsync(s=>s.SetProperty(x=>x.DueAtUtc,(DateTime?)null));await db.Set<WorkflowToken>().IgnoreQueryFilters().ExecuteUpdateAsync(s=>s.SetProperty(x=>x.DueAtUtc,(DateTime?)null));}
 [Fact] public async Task Approval_waits_durably_only_assignee_can_decide_and_replay_is_rejected()
 {
  using var app=new IdentityApiFactory();using var admin=await Login(app);var contact=await Contact(admin);
  var group=await Json(await Send(admin,"/api/v1/admin/security-groups",HttpMethod.Post,new{name="Reviewers",description="",permissions=new[]{"workflows.read","workflows.approve","crm.read","crm.manage"}}));
  var user=await Json(await Send(admin,"/api/v1/admin/users",HttpMethod.Post,new{displayName="Reviewer",email="reviewer@nexora.test",password=IdentityApiFactory.AdminPassword,isActive=true,groupIds=new[]{Id(group)}}));using var reviewer=await Login(app,"reviewer@nexora.test");
  var definition=await Prepare(admin,new(2,"contacts","manual","approve",[new("approve","approval","Explicit review",IfTrue:"task",IfFalse:"end",TimeoutNext:"end",ApproverUserId:Id(user),RequiredPermission:"crm.manage"),new("task","action","Approved follow-up",Next:"end",ActionKey:"work.task.create",Inputs:new(){{"title","Approved {{record.label}}"}}),End()]));var run=await Start(admin,definition,Id(contact));await Tick(app);await Due(app);await Tick(app);
  var approval=(await Json(await reviewer.GetAsync("/api/v1/workflows/approvals")))[0]!;Assert.Empty((await Json(await admin.GetAsync("/api/v1/workflows/approvals"))).AsArray());Assert.Empty((await Json(await admin.GetAsync("/api/v1/work/items"))).AsArray());
  var request=new{version=Version(approval),decision="approved",reason="Information verified"};Assert.Equal(HttpStatusCode.Forbidden,(await Send(admin,$"/api/v1/workflows/approvals/{Id(approval)}/decision",HttpMethod.Post,request)).StatusCode);
  var restricted=await Json(await Send(admin,"/api/v1/admin/security-groups/"+Id(group),HttpMethod.Patch,new{name="Reviewers",description="",permissions=new[]{"workflows.read","workflows.approve","crm.read"},concurrencyStamp=group["concurrencyStamp"]!.GetValue<string>()}));
  Assert.Equal(HttpStatusCode.Forbidden,(await Send(reviewer,$"/api/v1/workflows/approvals/{Id(approval)}/decision",HttpMethod.Post,request)).StatusCode);
  await Json(await Send(admin,"/api/v1/admin/security-groups/"+Id(group),HttpMethod.Patch,new{name="Reviewers",description="",permissions=new[]{"workflows.read","workflows.approve","crm.read","crm.manage"},concurrencyStamp=restricted["concurrencyStamp"]!.GetValue<string>()}));
  await Json(await Send(reviewer,$"/api/v1/workflows/approvals/{Id(approval)}/decision",HttpMethod.Post,request));Assert.Equal(HttpStatusCode.Conflict,(await Send(reviewer,$"/api/v1/workflows/approvals/{Id(approval)}/decision",HttpMethod.Post,request)).StatusCode);await Tick(app,3);
  Assert.Equal("completed",(await Json(await admin.GetAsync("/api/v1/workflows/runs/"+Id(run))))["run"]!["status"]!.GetValue<string>());Assert.Single((await Json(await admin.GetAsync("/api/v1/work/items"))).AsArray());await using var scope=app.Services.CreateAsyncScope();Assert.Equal(1,await scope.ServiceProvider.GetRequiredService<WorkflowDbContext>().Set<WorkflowApproval>().IgnoreQueryFilters().CountAsync());
 }
 [Fact] public async Task Approval_timeout_routes_to_escalation_without_authorising_the_success_branch()
 {
  using var app=new IdentityApiFactory();using var client=await Login(app);var user=(await client.GetFromJsonAsync<SessionResponse>("/api/v1/auth/session"))!.UserId;var contact=await Contact(client);
  var definition=await Prepare(client,new(2,"contacts","manual","approval",[new("approval","approval","Review",IfTrue:"end",IfFalse:"end",TimeoutNext:"escalate",ApproverUserId:user.ToString()),new("escalate","action","Escalation task",Next:"end",ActionKey:"work.task.create",Inputs:new(){{"title","Approval overdue"}}),End()]));var run=await Start(client,definition,Id(contact));await Tick(app);
  await using(var scope=app.Services.CreateAsyncScope()){await scope.ServiceProvider.GetRequiredService<WorkflowDbContext>().Set<WorkflowApproval>().IgnoreQueryFilters().ExecuteUpdateAsync(s=>s.SetProperty(x=>x.DeadlineUtc,DateTime.UtcNow.AddMinutes(-1)));}await Due(app);await Tick(app,3);Assert.Single((await Json(await client.GetAsync("/api/v1/work/items"))).AsArray());Assert.Empty((await Json(await client.GetAsync("/api/v1/workflows/approvals"))).AsArray());Assert.Equal("completed",(await Json(await client.GetAsync("/api/v1/workflows/runs/"+Id(run))))["run"]!["status"]!.GetValue<string>());
 }
 [Fact] public async Task Business_wait_rereads_current_record_and_polls_without_duplicating_trace()
 {
  using var app=new IdentityApiFactory();using var client=await Login(app);var contact=await Contact(client);var definition=await Prepare(client,new(2,"contacts","manual","await",[new("await","await-condition","Wait for verification",Next:"end",TimeoutNext:"end",Predicate:new(Field:"category",Operator:"equals",Value:"verified")),End()]));var run=await Start(client,definition,Id(contact));await Tick(app);await Due(app);await Tick(app);var waiting=await Json(await client.GetAsync("/api/v1/workflows/runs/"+Id(run)));Assert.Single(waiting["steps"]!.AsArray());Assert.Equal("waiting",waiting["run"]!["status"]!.GetValue<string>());
  await Json(await Send(client,"/api/v1/crm/records/"+Id(contact),HttpMethod.Patch,new{kind="contact",name="Workflow Contact",status="active",category="verified",version=Version(contact)}));await Due(app);await Tick(app,2);Assert.Equal("completed",(await Json(await client.GetAsync("/api/v1/workflows/runs/"+Id(run))))["run"]!["status"]!.GetValue<string>());
 }
 [Theory][MemberData(nameof(TemplateKeys))] public async Task Operations_templates_validate_against_actual_registered_fields_and_actions(string key)
 {
  using var app=new IdentityApiFactory();using var client=await Login(app);var draft=await Json(await Send(client,"/api/v1/workflows/templates/"+key+"/draft",HttpMethod.Post,new{reminderDays=3,beforeExpiryDays=30,graceDays=7}));foreach(var keyNode in draft["graph"]!["requiredVariables"]?.AsArray()??new JsonArray()){var required=keyNode!.GetValue<string>();draft["graph"]!["variables"]![required]=required=="amount"?"10":required.EndsWith("Id")?Guid.NewGuid().ToString():"Reviewed reference";}var validation=await Json(await Send(client,"/api/v1/workflows/validate",HttpMethod.Post,draft["graph"]));Assert.Empty(validation["errors"]!.AsArray());
 }
 public static IEnumerable<object[]> TemplateKeys()=>WorkflowOperationsTemplates.Keys.Select(k=>new object[]{k});
 [Fact] public async Task Cancelling_a_run_closes_its_pending_approvals()
 {
  using var app=new IdentityApiFactory();using var client=await Login(app);var user=(await client.GetFromJsonAsync<SessionResponse>("/api/v1/auth/session"))!.UserId;var contact=await Contact(client);
  var definition=await Prepare(client,new(2,"contacts","manual","approval",[new("approval","approval","Review",IfTrue:"end",IfFalse:"end",TimeoutNext:"end",ApproverUserId:user.ToString()),End()]));var run=await Start(client,definition,Id(contact));await Tick(app);var approval=(await Json(await client.GetAsync("/api/v1/workflows/approvals")))[0]!;var current=(await Json(await client.GetAsync("/api/v1/workflows/runs/"+Id(run))))["run"]!;
  await Json(await Send(client,"/api/v1/workflows/runs/"+Id(run)+"/cancel",HttpMethod.Post,new{version=Version(current)}));Assert.Empty((await Json(await client.GetAsync("/api/v1/workflows/approvals"))).AsArray());Assert.Equal(HttpStatusCode.Conflict,(await Send(client,"/api/v1/workflows/approvals/"+Id(approval)+"/decision",HttpMethod.Post,new{version=Version(approval),decision="approved",reason="Too late"})).StatusCode);
 }
 [Fact] public async Task Renewal_inspection_preserves_required_payment_and_the_domain_renewal_guard()
 {
  using var app=new IdentityApiFactory();using var client=await Login(app);var contact=await Contact(client);
  var product=await Json(await Send(client,"/api/v1/membership/products",HttpMethod.Post,new{name="Membership",category="Professional",termMonths=12,rate=100,currency="GBP",isActive=true}));
  await Json(await Send(client,$"/api/v1/membership/products/{Id(product)}/policy",HttpMethod.Put,new{renewalMode="rolling",yearEndMonth=12,yearEndDay=31,processingFee=0,credit=0,requirePayment=true}));
  var application=await Json(await Send(client,"/api/v1/membership/applications",HttpMethod.Post,new{contactId=Id(contact),productId=Id(product),startsAtUtc=DateTime.UtcNow.Date,applicationType="individual",termsAccepted=true}));
  var approved=await Json(await Send(client,$"/api/v1/membership/applications/{Id(application)}/decision",HttpMethod.Post,new{version=Version(application),decision="approve",reason="Verified"}));var memberId=approved["membershipId"]!.GetValue<string>();
  var definition=await Prepare(client,new(2,"members","manual","inspect",[new("inspect","action","Check renewal",Next:"end",ActionKey:"membership.member.inspect-renewal",Inputs:new(){{"membershipId","{{record.id}}"}}),End()]));var run=await Start(client,definition,memberId);await Tick(app,2);var detail=await Json(await client.GetAsync("/api/v1/workflows/runs/"+Id(run)));var outputs=JsonNode.Parse(detail["steps"]![0]!["outputsJson"]!.GetValue<string>())!;Assert.Equal("false",outputs["eligible"]!.GetValue<string>());Assert.Contains("payment",outputs["requirements"]!.GetValue<string>());
  var member=(await Json(await client.GetAsync("/api/v1/membership/members/"+memberId)))["member"]!;Assert.Equal(HttpStatusCode.BadRequest,(await Send(client,$"/api/v1/membership/members/{memberId}/renew",HttpMethod.Post,new{version=Version(member),reason="Must remain blocked"})).StatusCode);
 }
 [Fact] public async Task Invoice_inspection_uses_allocations_instead_of_a_synthetic_paid_status()
 {
  using var app=new IdentityApiFactory();using var client=await Login(app);var contact=await Contact(client);var product=await Json(await Send(client,"/api/v1/finance/products",HttpMethod.Post,new{name="Service",sku="service",unitPrice=10,currency="GBP",vatBasisPoints=0}));var order=await Json(await Send(client,"/api/v1/finance/orders",HttpMethod.Post,new{customerId=Id(contact),lines=new[]{new{productId=Id(product),quantity=2}}}));var invoice=await Json(await Send(client,"/api/v1/finance/orders/"+Id(order)+"/post",HttpMethod.Post,new{version=Version(order)}));
  var graph=new WorkflowGraph(2,"invoices","manual","await",[new("await","await-condition","Wait for settlement",Next:"end",TimeoutNext:"end",ActionKey:"finance.invoice.inspect",Inputs:new(){{"invoiceId","{{record.id}}"}},Predicate:new(Field:"steps.await.settled",Operator:"equals",Value:"true")),End()]);var definition=await Prepare(client,graph);var run=await Start(client,definition,Id(invoice));await Tick(app);Assert.Equal("waiting",(await Json(await client.GetAsync("/api/v1/workflows/runs/"+Id(run))))["run"]!["status"]!.GetValue<string>());
  var previewGraph=new WorkflowGraph(2,"invoices","manual","inspect",[new("inspect","action","Read actual balance",Next:"end",ActionKey:"finance.invoice.inspect",Inputs:new(){{"invoiceId","{{record.id}}"}}),End()]);
  var preview=await Json(await Send(client,"/api/v1/workflows/test",HttpMethod.Post,new{graph=previewGraph,recordId=Id(invoice)}));Assert.Equal(0,preview["mutations"]!.GetValue<int>());Assert.Equal("false",preview["trace"]![0]!["outcome"]!["outputs"]!["settled"]!.GetValue<string>());
  var receipt=await Json(await Send(client,"/api/v1/finance/receipts",HttpMethod.Post,new{customerId=Id(contact),reference="verified settlement",amount=20,currency="GBP"}));await Json(await Send(client,"/api/v1/finance/allocations",HttpMethod.Post,new{receiptId=Id(receipt),invoiceId=Id(invoice),amount=20}));await Due(app);await Tick(app,2);Assert.Equal("completed",(await Json(await client.GetAsync("/api/v1/workflows/runs/"+Id(run))))["run"]!["status"]!.GetValue<string>());
 }
}
