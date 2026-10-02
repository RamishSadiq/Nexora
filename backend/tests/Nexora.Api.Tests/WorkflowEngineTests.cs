using System.Net;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Nexora.BuildingBlocks.Persistence;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Workflow;
using Nexora.Modules.Work;
using Nexora.Modules.Work.Domain;
using Nexora.Modules.Work.Infrastructure;
using static Nexora.Api.Tests.WorkflowTestClient;
namespace Nexora.Api.Tests;
public sealed class WorkflowEngineTests
{
 private static WorkflowGraph Graph(params WorkflowNode[] nodes)=>new(1,"contacts","manual",nodes[0].Id,nodes);
 private static WorkflowNode Task(string title="Review {{record.label}}")=>new("task","action","Create review task",Next:"end",ActionKey:"work.task.create",Inputs:new(){{"title",title},{"priority","normal"}});
 private static WorkflowNode End()=>new("end","end","Complete");
 private static async Task<JsonNode> Prepare(HttpClient client,WorkflowGraph graph)
 {
  var row=await Json(await Send(client,"/api/v1/workflows/definitions",HttpMethod.Post,new{name="Test workflow",description="",graph}));
  var published=await Json(await Send(client,$"/api/v1/workflows/definitions/{Id(row)}/publish",HttpMethod.Post,new{version=Version(row)}));
  return await Json(await Send(client,$"/api/v1/workflows/definitions/{Id(row)}/activate",HttpMethod.Post,new{version=Version(published["definition"]!)}));
 }
 private static async Task<JsonNode> Start(HttpClient client,JsonNode definition,JsonNode contact,string? requestKey=null)=>await Json(await Send(client,$"/api/v1/workflows/definitions/{Id(definition)}/execute",HttpMethod.Post,new{recordId=Id(contact),requestKey=requestKey??Guid.NewGuid().ToString()}),HttpStatusCode.Accepted);
 private static async Task Tick(IdentityApiFactory app)
 {
  var worker=new WorkflowWorker(app.Services.GetRequiredService<IServiceScopeFactory>(),app.Services.GetRequiredService<WorkflowExecutionContext>(),NullLogger<WorkflowWorker>.Instance);await worker.TickAsync(default);
 }
 [Fact]
 public async Task Manual_run_creates_real_task_and_history_and_deduplicates_submission()
 {
  using var app=new IdentityApiFactory();using var client=await Login(app);var contact=await Contact(client);var definition=await Prepare(client,Graph(Task(),End()));var key=Guid.NewGuid().ToString();var run=await Start(client,definition,contact,key);
  var replay=await Json(await Send(client,$"/api/v1/workflows/definitions/{Id(definition)}/execute",HttpMethod.Post,new{recordId=Id(contact),requestKey=key}));Assert.Equal(Id(run),Id(replay));
  await Tick(app);await Tick(app);var detail=await Json(await client.GetAsync("/api/v1/workflows/runs/"+Id(run)));Assert.Equal("completed",detail["run"]!["status"]!.GetValue<string>());Assert.Equal(2,detail["steps"]!.AsArray().Count);
  var tasks=await Json(await client.GetAsync("/api/v1/work/items"));Assert.Single(tasks.AsArray());Assert.Contains("Workflow Contact",tasks[0]!["title"]!.GetValue<string>());
  using var outside=await Login(app,"outside@nexora.test");Assert.Equal(HttpStatusCode.Forbidden,(await outside.GetAsync("/api/v1/workflows/definitions")).StatusCode);
 }
 [Fact]
 public async Task Wait_is_durable_version_is_pinned_and_cancel_stops_resumption()
 {
  using var app=new IdentityApiFactory();using var client=await Login(app);var contact=await Contact(client);var graph=Graph(new("wait","wait","Pause",Next:"task",DelayMinutes:1),Task("Old version"),End());var definition=await Prepare(client,graph);var run=await Start(client,definition,contact);await Tick(app);
  var waiting=await Json(await client.GetAsync("/api/v1/workflows/runs/"+Id(run)));Assert.Equal("waiting",waiting["run"]!["status"]!.GetValue<string>());await Tick(app);Assert.Empty((await Json(await client.GetAsync("/api/v1/work/items"))).AsArray());
  var changed=await Json(await Send(client,$"/api/v1/workflows/definitions/{Id(definition)}",HttpMethod.Put,new{name="Changed",description="",graph=Graph(Task("New version"),End()),version=Version(definition)}));var published=await Json(await Send(client,$"/api/v1/workflows/definitions/{Id(definition)}/publish",HttpMethod.Post,new{version=Version(changed)}));await Json(await Send(client,$"/api/v1/workflows/definitions/{Id(definition)}/activate",HttpMethod.Post,new{version=Version(published["definition"]!)}));
  await using(var scope=app.Services.CreateAsyncScope()){var db=scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();var id=Guid.Parse(Id(run));await db.Set<WorkflowRun>().IgnoreQueryFilters().Where(x=>x.Id==id).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.DueAtUtc,DateTime.UtcNow.AddMinutes(-1)));await db.Set<WorkflowToken>().IgnoreQueryFilters().Where(x=>x.RunId==id).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.DueAtUtc,DateTime.UtcNow.AddMinutes(-1)));}
  await Tick(app);await Tick(app);var task=(await Json(await client.GetAsync("/api/v1/work/items")))[0]!;Assert.Equal("Old version",task["title"]!.GetValue<string>());
  var another=await Start(client,published["definition"]!,contact);await Json(await Send(client,$"/api/v1/workflows/runs/{Id(another)}/cancel",HttpMethod.Post,new{version=Version(another)}));await Tick(app);Assert.Single((await Json(await client.GetAsync("/api/v1/work/items"))).AsArray());
 }
 [Fact]
 public async Task Recovery_after_committed_action_does_not_duplicate_task_or_domain_history()
 {
  using var app=new IdentityApiFactory();using var client=await Login(app);var contact=await Contact(client);var definition=await Prepare(client,Graph(Task("Recover once"),End()));var run=await Start(client,definition,contact);
  await using(var scope=app.Services.CreateAsyncScope())
  {
   var db=scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();var row=await db.Set<WorkflowRun>().IgnoreQueryFilters().SingleAsync();var principal=await scope.ServiceProvider.GetRequiredService<IWorkflowPrincipalDirectory>().ResolveAsync(row.TenantId,row.ActorId,default);using var identity=app.Services.GetRequiredService<WorkflowExecutionContext>().Enter(principal!);
   await scope.ServiceProvider.GetRequiredService<WorkTaskCommands>().CreateAsync(new("Recover once","","open","normal",null,null,null,null,null),WorkflowProcessor.ActionId(row.Id,"task"),"workflow:"+row.Id,default);
   // Simulates process loss after the module commits and before the workflow checkpoints.
  }
  await Tick(app);await Tick(app);Assert.Single((await Json(await client.GetAsync("/api/v1/work/items"))).AsArray());
  await using(var scope=app.Services.CreateAsyncScope()){Assert.Equal(1,await scope.ServiceProvider.GetRequiredService<WorkDbContext>().Set<WorkHistory>().IgnoreQueryFilters().CountAsync(x=>x.Action=="task.created"));}
 }
 [Fact]
 public async Task Revoked_action_permission_fails_before_mutation_and_retry_uses_original_run()
 {
  using var app=new IdentityApiFactory();using var client=await Login(app);var contact=await Contact(client);var definition=await Prepare(client,Graph(Task(),End()));var run=await Start(client,definition,contact);
  await using(var scope=app.Services.CreateAsyncScope())
  {
   var db=scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();var row=await db.Set<WorkflowRun>().IgnoreQueryFilters().SingleAsync();var principal=await scope.ServiceProvider.GetRequiredService<IWorkflowPrincipalDirectory>().ResolveAsync(row.TenantId,row.ActorId,default);using var identity=app.Services.GetRequiredService<WorkflowExecutionContext>().Enter(principal!);
   await scope.ServiceProvider.GetRequiredService<WorkflowProcessor>().StepAsync(row.Id,principal!.Permissions.Where(x=>x!="work.manage").ToArray(),default);
  }
  var failed=await Json(await client.GetAsync("/api/v1/workflows/runs/"+Id(run)));Assert.Equal("failed",failed["run"]!["status"]!.GetValue<string>());Assert.Empty((await Json(await client.GetAsync("/api/v1/work/items"))).AsArray());
  await Json(await Send(client,$"/api/v1/workflows/runs/{Id(run)}/retry",HttpMethod.Post,new{version=Version(failed["run"]!)}));await Tick(app);await Tick(app);Assert.Single((await Json(await client.GetAsync("/api/v1/work/items"))).AsArray());
 }
 [Fact]
 public async Task Branch_conditions_use_current_record_and_invalid_graphs_are_rejected()
 {
  using var app=new IdentityApiFactory();using var client=await Login(app);var contact=await Contact(client);var definition=await Prepare(client,Graph(new("check","condition","Check status",IfTrue:"task",IfFalse:"end",Field:"status",Operator:"equals",Value:"inactive"),Task(),End()));var run=await Start(client,definition,contact);await Tick(app);await Tick(app);Assert.Empty((await Json(await client.GetAsync("/api/v1/work/items"))).AsArray());
  var cyclic=Graph(new WorkflowNode("cycle","wait","Cycle",Next:"cycle",DelayMinutes:1));var cycleDraft=await Json(await Send(client,"/api/v1/workflows/definitions",HttpMethod.Post,new{name="Cycle",graph=cyclic}));Assert.Equal(HttpStatusCode.BadRequest,(await Send(client,$"/api/v1/workflows/definitions/{Id(cycleDraft)}/publish",HttpMethod.Post,new{version=Version(cycleDraft)})).StatusCode);
  Assert.Equal(HttpStatusCode.NotFound,(await Send(client,$"/api/v1/workflows/definitions/{Id(definition)}/execute",HttpMethod.Post,new{recordId=Guid.NewGuid(),requestKey="unavailable"})).StatusCode);
 }
 private sealed record Identity(Guid? TenantId,Guid? UserId):IRequestIdentity;
 [Fact]
 public async Task Tenant_write_guards_and_published_version_immutability_are_enforced()
 {
  using var app=new IdentityApiFactory();using var client=await Login(app);var definition=await Prepare(client,Graph(End()));
  await using var scope=app.Services.CreateAsyncScope();var original=scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();var options=new DbContextOptionsBuilder<WorkflowDbContext>().UseSqlite(original.Database.GetDbConnection()).Options;
  var foreignTenant=Guid.NewGuid();await using var foreign=new WorkflowDbContext(options,new Identity(foreignTenant,Guid.NewGuid()));Assert.Empty(await foreign.Set<WorkflowDefinition>().ToListAsync());foreign.Update(new WorkflowDefinition{Id=Guid.Parse(Id(definition)),TenantId=foreignTenant,Name="Stolen"});await Assert.ThrowsAsync<TenantBoundaryException>(()=>foreign.SaveChangesAsync());
  var published=await original.Set<WorkflowVersion>().IgnoreQueryFilters().SingleAsync();await using var own=new WorkflowDbContext(options,new Identity(published.TenantId,published.PublishedBy));var row=await own.Set<WorkflowVersion>().SingleAsync();row.GraphJson="{}";await Assert.ThrowsAsync<TenantBoundaryException>(()=>own.SaveChangesAsync());
 }
}
