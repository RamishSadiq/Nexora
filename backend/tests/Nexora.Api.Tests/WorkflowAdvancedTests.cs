using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Nexora.BuildingBlocks.Persistence;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Workflow;
using Nexora.Modules.Work.Domain;
using Nexora.Modules.Work.Infrastructure;
using static Nexora.Api.Tests.WorkflowTestClient;
namespace Nexora.Api.Tests;
public sealed class WorkflowAdvancedTests
{
 private static WorkflowNode End()=>new("end","end","Complete");
 private static WorkflowNode TaskNode(string next="end",string title="Review {{record.label}}")=>new("task","action","Review",Next:next,ActionKey:"work.task.create",Inputs:new(){{"title",title}});
 private static async Task<JsonNode> Prepare(HttpClient client,WorkflowGraph graph){var draft=await Json(await Send(client,"/api/v1/workflows/definitions",HttpMethod.Post,new{name="Advanced",graph}));var publish=await Json(await Send(client,$"/api/v1/workflows/definitions/{Id(draft)}/publish",HttpMethod.Post,new{version=Version(draft)}));return await Json(await Send(client,$"/api/v1/workflows/definitions/{Id(draft)}/activate",HttpMethod.Post,new{version=Version(publish["definition"]!)}));}
 private static async Task Tick(IdentityApiFactory app,int count=1){var worker=new WorkflowWorker(app.Services.GetRequiredService<IServiceScopeFactory>(),app.Services.GetRequiredService<WorkflowExecutionContext>(),NullLogger<WorkflowWorker>.Instance);for(var i=0;i<count;i++)await worker.TickAsync(default);}
 [Fact] public async Task Query_loop_parallel_branches_and_nested_conditions_execute_and_preview_has_no_effects()
 {
  using var app=new IdentityApiFactory();using var client=await Login(app);var contact=await Contact(client);
  var graph=new WorkflowGraph(2,"contacts","manual","query",[new("query","query","Find Contacts",Next:"loop",Predicate:new(Field:"id",Operator:"equals",Value:Id(contact)),QueryEntity:"contacts",OutputVariable:"people",Limit:10),new("loop","loop","For each Contact",Next:"fork",ItemsVariable:"variables.people",Body:"task"),TaskNode("return","Review {{item.label}}"),new("return","return","Next item"),new("fork","parallel","Branches",Next:"join",Branches:["a","b"]),new("a","variable","First",Next:"join",Value:"alpha",OutputVariable:"result"),new("b","variable","Second",Next:"join",Value:"beta",OutputVariable:"result"),new("join","join","Wait for branches",Next:"check"),new("check","condition","Nested",IfTrue:"end",IfFalse:"end",Predicate:new(All:[new(Field:"status",Operator:"equals",Value:"active"),new(Any:[new(Field:"label",Operator:"contains",Value:"Workflow"),new(Field:"status",Operator:"equals",Value:"prospect")])])),End()]);
  var preview=await Json(await Send(client,"/api/v1/workflows/test",HttpMethod.Post,new{graph,recordId=Id(contact)}));Assert.Equal(0,preview["mutations"]!.GetValue<int>());Assert.Empty((await Json(await client.GetAsync("/api/v1/work/items"))).AsArray());
  var definition=await Prepare(client,graph);var run=await Json(await Send(client,$"/api/v1/workflows/definitions/{Id(definition)}/execute",HttpMethod.Post,new{recordId=Id(contact),requestKey="loop"}),HttpStatusCode.Accepted);await Tick(app,20);var detail=await Json(await client.GetAsync("/api/v1/workflows/runs/"+Id(run)));Assert.Equal("completed",detail["run"]!["status"]!.GetValue<string>());Assert.Single((await Json(await client.GetAsync("/api/v1/work/items"))).AsArray());Assert.Contains(detail["steps"]!.AsArray(),s=>s!["kind"]!.GetValue<string>()=="parallel");
 }
 [Fact] public async Task Automatic_events_are_transactional_and_cannot_trigger_themselves_recursively()
 {
  using var app=new IdentityApiFactory();using var client=await Login(app);var contact=await Contact(client);var graph=new WorkflowGraph(2,"contacts","record.updated","update",[new("update","action","Update category",Next:"end",ActionKey:"crm.record.update",Inputs:new(){{"recordId","{{record.id}}"},{"category","Auto tagged"}}),End()]);await Prepare(client,graph);
  await Json(await Send(client,"/api/v1/crm/records/"+Id(contact),HttpMethod.Patch,new{kind="contact",name="Workflow changed",status="active",category="",ownerUserId=(Guid?)null,ownerTeamId=(Guid?)null,version=Version(contact)}));await Tick(app,6);var runs=await Json(await client.GetAsync("/api/v1/workflows/runs"));Assert.Single(runs.AsArray());Assert.Equal("completed",runs[0]!["status"]!.GetValue<string>());var detail=await Json(await client.GetAsync("/api/v1/crm/records/"+Id(contact)));Assert.Equal("Auto tagged",detail["record"]!["category"]!.GetValue<string>());
 }
 [Fact] public async Task Schedule_occurrences_and_bulk_submission_are_deduplicated()
 {
  using var app=new IdentityApiFactory();using var client=await Login(app);var contact=await Contact(client);var graph=new WorkflowGraph(2,"contacts","scheduled","task",[TaskNode(),End()],new(IntervalMinutes:60),new(Field:"id",Operator:"equals",Value:Id(contact)));var definition=await Prepare(client,graph);
  await using(var scope=app.Services.CreateAsyncScope())await scope.ServiceProvider.GetRequiredService<WorkflowDbContext>().Set<WorkflowSchedule>().IgnoreQueryFilters().ExecuteUpdateAsync(s=>s.SetProperty(x=>x.NextAtUtc,DateTime.UtcNow.AddMinutes(-1)));await Tick(app,4);Assert.Single((await Json(await client.GetAsync("/api/v1/workflows/runs"))).AsArray());
  var one=await Json(await Send(client,$"/api/v1/workflows/definitions/{Id(definition)}/bulk-execute",HttpMethod.Post,new{recordIds=new[]{Id(contact)},requestKey="bulk"}),HttpStatusCode.Accepted);var two=await Json(await Send(client,$"/api/v1/workflows/definitions/{Id(definition)}/bulk-execute",HttpMethod.Post,new{recordIds=new[]{Id(contact)},requestKey="bulk"}),HttpStatusCode.Accepted);Assert.Equal(Id(one[0]!),Id(two[0]!));
 }
 [Fact] public async Task Signed_webhooks_reject_invalid_signatures_and_replay_the_same_request_once()
 {
  using var app=new IdentityApiFactory();using var client=await Login(app);var contact=await Contact(client);var definition=await Prepare(client,new(2,"contacts","external","end",[End()]));var hook=await Json(await Send(client,$"/api/v1/workflows/definitions/{Id(definition)}/webhook",HttpMethod.Post,new{}));using var anonymous=app.CreateClient();var raw=JsonSerializer.Serialize(new{recordId=Id(contact),requestKey="signed"});var stamp=DateTimeOffset.UtcNow.ToUnixTimeSeconds();var signature=Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(hook["secret"]!.GetValue<string>()),Encoding.UTF8.GetBytes(stamp+"."+raw)));
  async Task<HttpResponseMessage> SendHook(string sign){var message=new HttpRequestMessage(HttpMethod.Post,hook["url"]!.GetValue<string>()){Content=new StringContent(raw,Encoding.UTF8,"application/json")};message.Headers.Add("X-Nexora-Timestamp",stamp.ToString());message.Headers.Add("X-Nexora-Signature",sign);return await anonymous.SendAsync(message);}
  Assert.Equal(HttpStatusCode.Unauthorized,(await SendHook("00")).StatusCode);var run=await Json(await SendHook(signature),HttpStatusCode.Accepted);var replay=await Json(await SendHook(signature),HttpStatusCode.Accepted);Assert.Equal(Id(run),Id(replay));await Tick(app);Assert.Single((await Json(await client.GetAsync("/api/v1/workflows/runs"))).AsArray());
 }
 [Fact] public async Task Published_subworkflow_runs_are_pinned_and_complete_before_parent_continues()
 {
  using var app=new IdentityApiFactory();using var client=await Login(app);var contact=await Contact(client);var child=await Prepare(client,new(2,"contacts","manual","task",[TaskNode(),End()]));var parent=await Prepare(client,new(2,"contacts","manual","child",[new("child","subworkflow","Run child",Next:"end",WorkflowVersionId:child["activeVersionId"]!.GetValue<string>()),End()]));var run=await Json(await Send(client,$"/api/v1/workflows/definitions/{Id(parent)}/execute",HttpMethod.Post,new{recordId=Id(contact),requestKey="parent"}),HttpStatusCode.Accepted);await Tick(app,8);var detail=await Json(await client.GetAsync("/api/v1/workflows/runs/"+Id(run)));Assert.Equal("completed",detail["run"]!["status"]!.GetValue<string>());Assert.Single((await Json(await client.GetAsync("/api/v1/work/items"))).AsArray());
 }
 [Theory][InlineData("https://127.0.0.1/api")][InlineData("http://example.com")][InlineData("https://169.254.169.254/latest")][InlineData("https://user:password@example.com")]
 public void Connectors_reject_unsafe_endpoints(string endpoint)=>Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(()=>WorkflowConnections.Endpoint(endpoint));
}
