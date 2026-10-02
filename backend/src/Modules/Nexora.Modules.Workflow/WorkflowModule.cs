using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Endpoints;
using Nexora.BuildingBlocks.Reporting;
using Nexora.BuildingBlocks.Security;
using static Nexora.BuildingBlocks.Endpoints.ModuleEndpoints;
namespace Nexora.Modules.Workflow;
public static partial class WorkflowModule
{
 public static IServiceCollection AddNexoraWorkflow(this IServiceCollection services,IConfiguration config)
 {
  services.AddDbContext<WorkflowDbContext>(o=>o.UseSqlServer(config.GetConnectionString("Nexora"),sql=>sql.MigrationsHistoryTable("__EFMigrationsHistory","workflow")));
  services.AddScoped<Nexora.BuildingBlocks.Persistence.WorkflowCaptureInterceptor>();services.AddScoped<WorkflowConnections>();services.AddScoped<IWorkflowTransport,WorkflowTransport>();services.AddScoped<WorkflowPreview>();foreach(var kind in new[]{"http","email"})services.AddScoped<IWorkflowActionHandler>(sp=>new WorkflowDeliveryAction(sp.GetRequiredService<WorkflowDbContext>(),sp.GetRequiredService<WorkflowConnections>(),sp.GetRequiredService<IRequestIdentity>(),kind,sp.GetRequiredService<IWorkflowTransport>()));services.AddRateLimiter(o=>o.AddPolicy("workflow-hooks",http=>System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(http.Connection.RemoteIpAddress?.ToString()??"unknown",_=>new System.Threading.RateLimiting.FixedWindowRateLimiterOptions{PermitLimit=30,Window=TimeSpan.FromMinutes(1),QueueLimit=0})));services.AddScoped<WorkflowExpressions>();services.AddScoped<WorkflowValidation>();services.AddScoped<WorkflowGates>();services.AddScoped<WorkflowProcessor>();services.AddScoped<WorkflowTriggers>();services.AddHostedService<WorkflowWorker>();return services;
 }
 internal static string[] Permissions(HttpContext http)=>http.User.FindAll("permission").Select(x=>x.Value).ToArray();
 internal static IReportDataset Dataset(string key,IEnumerable<IReportDataset> datasets,string[] permissions)=>datasets.SingleOrDefault(x=>x.Key==key&&permissions.Contains(x.Permission))??throw new KeyNotFoundException();
 private static async Task<WorkflowDefinition> Definition(Guid id,WorkflowDbContext db,IEnumerable<IReportDataset> datasets,HttpContext http,CancellationToken ct)
 {
  var row=await db.Set<WorkflowDefinition>().SingleOrDefaultAsync(x=>x.Id==id&&!x.IsArchived,ct)??throw new KeyNotFoundException();http.RequestServices.GetRequiredService<WorkflowExpressions>().Source(WorkflowValidation.Parse(row.DraftJson).EntityKey,Permissions(http));return row;
 }
 private static void Audit(WorkflowDbContext db,IRequestIdentity who,Guid id,string action,string detail="")=>db.Add(new WorkflowAudit{TenantId=who.TenantId!.Value,ActorId=who.UserId!.Value,DefinitionId=id,Action=action,Detail=detail});
 public static IEndpointRouteBuilder MapNexoraWorkflow(this IEndpointRouteBuilder routes)
 {
  var api=routes.Module("workflows");
  api.MapGet("/metadata",(IEnumerable<Nexora.BuildingBlocks.Persistence.IWorkflowEntitySource> sources,IEnumerable<IWorkflowActionHandler> handlers,HttpContext http)=>new{entities=sources.Where(x=>Permissions(http).Contains(x.Metadata.Permission)).Select(x=>x.Metadata),actions=handlers.Where(x=>x.Descriptor.RequiredPermissions.All(Permissions(http).Contains)).Select(x=>x.Descriptor),triggers=WorkflowValidation.Triggers,nodeKinds=WorkflowValidation.NodeKinds,operators=WorkflowExpressions.Operators,permissions=Permissions(http)});
  api.MapGet("/records/{key}",async(string key,string? search,WorkflowExpressions expressions,HttpContext http,CancellationToken ct)=>{Check(search==null||search.Length<=160,"Search is too long.");return await expressions.Source(key,Permissions(http)).QueryAsync(string.IsNullOrWhiteSpace(search)?null:new(Field:"label",Operator:"contains",Value:search),100,ct);});
  api.MapGet("/definitions",async(WorkflowDbContext db,IEnumerable<IReportDataset> datasets,HttpContext http,CancellationToken ct)=>
  {
   var keys=http.RequestServices.GetServices<Nexora.BuildingBlocks.Persistence.IWorkflowEntitySource>().Where(x=>Permissions(http).Contains(x.Metadata.Permission)).Select(x=>x.Metadata.Key).ToHashSet();
   return (await db.Set<WorkflowDefinition>().AsNoTracking().Where(x=>!x.IsArchived).OrderByDescending(x=>x.UpdatedAtUtc).Take(200).ToListAsync(ct)).Where(x=>keys.Contains(WorkflowValidation.Parse(x.DraftJson).EntityKey));
  });
  api.MapPost("/definitions",async(WorkflowSave r,WorkflowDbContext db,WorkflowValidation validation,IRequestIdentity who,HttpContext http,CancellationToken ct)=>
  {
   Check(r.Graph!=null,"Provide a workflow definition.");var errors=WorkflowValidation.Structure(r.Graph!);Check(errors.Length==0,string.Join(" ",errors));http.RequestServices.GetRequiredService<WorkflowExpressions>().Source(r.Graph!.EntityKey,Permissions(http));
   var row=new WorkflowDefinition{TenantId=who.TenantId!.Value,Name=Text(r.Name),Description=string.IsNullOrWhiteSpace(r.Description)?"":Text(r.Description,500),DraftJson=WorkflowValidation.Serialize(r.Graph!)};db.Add(row);Audit(db,who,row.Id,"draft.created");await db.SaveChangesAsync(ct);return Results.Ok(row);
  }).RequireAuthorization("workflows.create");
  api.MapGet("/definitions/{id:guid}",async(Guid id,WorkflowDbContext db,IEnumerable<IReportDataset> datasets,HttpContext http,CancellationToken ct)=>
  {
   var row=await Definition(id,db,datasets,http,ct);return Results.Ok(new{definition=row,versions=await db.Set<WorkflowVersion>().Where(x=>x.DefinitionId==id).OrderByDescending(x=>x.Number).ToListAsync(ct),audit=await db.Set<WorkflowAudit>().Where(x=>x.DefinitionId==id).OrderByDescending(x=>x.CreatedAtUtc).Take(100).ToListAsync(ct)});
  });
  api.MapPut("/definitions/{id:guid}",async(Guid id,WorkflowSave r,WorkflowDbContext db,WorkflowValidation validation,IEnumerable<IReportDataset> datasets,IRequestIdentity who,HttpContext http,CancellationToken ct)=>
  {
   var row=await Definition(id,db,datasets,http,ct);if(row.Version!=r.Version)return Conflict();Check(r.Graph!=null,"Provide a workflow definition.");
   var errors=WorkflowValidation.Structure(r.Graph!);Check(errors.Length==0,string.Join(" ",errors));http.RequestServices.GetRequiredService<WorkflowExpressions>().Source(r.Graph!.EntityKey,Permissions(http));
   row.Name=Text(r.Name);row.Description=string.IsNullOrWhiteSpace(r.Description)?"":Text(r.Description,500);row.DraftJson=WorkflowValidation.Serialize(r.Graph!);Audit(db,who,id,"draft.saved");await db.SaveChangesAsync(ct);return Results.Ok(row);
  }).RequireAuthorization("workflows.edit");
  api.MapPost("/validate",async(WorkflowGraph graph,WorkflowValidation validation,HttpContext http,CancellationToken ct)=>new{errors=await validation.ValidateAsync(graph,Permissions(http),ct)});
  api.MapPost("/definitions/{id:guid}/publish",async(Guid id,WorkflowConcurrency r,WorkflowDbContext db,WorkflowValidation validation,IEnumerable<IReportDataset> datasets,IRequestIdentity who,HttpContext http,CancellationToken ct)=>
  {
   var row=await Definition(id,db,datasets,http,ct);if(row.Version!=r.Version)return Conflict();var errors=await validation.ValidateAsync(WorkflowValidation.Parse(row.DraftJson),Permissions(http),ct);Check(errors.Length==0,string.Join(" ",errors));
   var version=new WorkflowVersion{TenantId=row.TenantId,DefinitionId=id,Number=++row.PublishedNumber,GraphJson=row.DraftJson,PublishedBy=who.UserId!.Value};db.Add(version);Audit(db,who,id,"version.published",version.Number.ToString());await db.SaveChangesAsync(ct);return Results.Ok(new{definition=row,published=version});
  }).RequireAuthorization("workflows.publish");
  api.MapPost("/definitions/{id:guid}/activate",async(Guid id,WorkflowConcurrency r,WorkflowDbContext db,IEnumerable<IReportDataset> datasets,IRequestIdentity who,HttpContext http,CancellationToken ct)=>
  {
   var row=await Definition(id,db,datasets,http,ct);if(row.Version!=r.Version)return Conflict();var version=await db.Set<WorkflowVersion>().Where(x=>x.DefinitionId==id).OrderByDescending(x=>x.Number).FirstOrDefaultAsync(ct);Check(version!=null,"Publish a version first.");var validation=http.RequestServices.GetRequiredService<WorkflowValidation>();var activationErrors=await validation.ValidateAsync(WorkflowValidation.Parse(version!.GraphJson),Permissions(http),ct);Check(activationErrors.Length==0,string.Join(" ",activationErrors));row.ActiveVersionId=version.Id;await ConfigureSchedule(row,version,db,ct);Audit(db,who,id,"version.activated",version.Number.ToString());await db.SaveChangesAsync(ct);return Results.Ok(row);
  }).RequireAuthorization("workflows.activate");
  api.MapPost("/definitions/{id:guid}/deactivate",async(Guid id,WorkflowConcurrency r,WorkflowDbContext db,IEnumerable<IReportDataset> datasets,IRequestIdentity who,HttpContext http,CancellationToken ct)=>
  {
   var row=await Definition(id,db,datasets,http,ct);if(row.Version!=r.Version)return Conflict();row.ActiveVersionId=null;foreach(var schedule in await db.Set<WorkflowSchedule>().Where(x=>x.DefinitionId==id).ToListAsync(ct))schedule.IsEnabled=false;Audit(db,who,id,"workflow.deactivated");await db.SaveChangesAsync(ct);return Results.Ok(row);
  }).RequireAuthorization("workflows.activate");
  api.MapDelete("/definitions/{id:guid}",async(Guid id,[FromBody] WorkflowConcurrency r,WorkflowDbContext db,IEnumerable<IReportDataset> datasets,IRequestIdentity who,HttpContext http,CancellationToken ct)=>
  {
   var row=await Definition(id,db,datasets,http,ct);if(row.Version!=r.Version)return Conflict();row.IsArchived=true;row.ActiveVersionId=null;foreach(var schedule in await db.Set<WorkflowSchedule>().Where(x=>x.DefinitionId==id).ToListAsync(ct))schedule.IsEnabled=false;Audit(db,who,id,"workflow.archived");await db.SaveChangesAsync(ct);return Results.NoContent();
  }).RequireAuthorization("workflows.delete");
  api.MapPost("/definitions/{id:guid}/execute",async(Guid id,WorkflowStart r,WorkflowDbContext db,IEnumerable<IReportDataset> datasets,WorkflowValidation validation,IRequestIdentity who,HttpContext http,CancellationToken ct)=>
  {
   var definition=await Definition(id,db,datasets,http,ct);Check(definition.ActiveVersionId!=null,"Activate a published version first.");
   var version=await db.Set<WorkflowVersion>().SingleAsync(x=>x.Id==definition.ActiveVersionId,ct);var graph=WorkflowValidation.Parse(version.GraphJson);var errors=await validation.ValidateAsync(graph,Permissions(http),ct);Check(errors.Length==0,string.Join(" ",errors));
   var source=http.RequestServices.GetRequiredService<WorkflowExpressions>().Source(graph.EntityKey,Permissions(http));var record=await source.FindAsync(r.RecordId,ct)??throw new KeyNotFoundException();var key=Text(r.RequestKey,80);
   var existing=await db.Set<WorkflowRun>().SingleOrDefaultAsync(x=>x.WorkflowVersionId==version.Id&&x.RequestKey==key,ct);if(existing!=null){Check(existing.RecordId==r.RecordId&&existing.ActorId==who.UserId,"This request key was used for a different execution.");return Results.Ok(existing);}
   var run=new WorkflowRun{TenantId=definition.TenantId,ActorId=who.UserId!.Value,WorkflowVersionId=version.Id,RecordId=r.RecordId,RecordLabel=record.Label,RequestKey=key,CurrentNodeId=graph.EntryNodeId,AncestryJson=System.Text.Json.JsonSerializer.Serialize(new[]{version.DefinitionId}),ContextJson=System.Text.Json.JsonSerializer.Serialize(new{variables=r.Variables??new Dictionary<string,string>()})};db.Add(run);await db.SaveChangesAsync(ct);return Results.Accepted("/api/v1/workflows/runs/"+run.Id,run);
  }).RequireAuthorization("workflows.execute");
  api.MapGet("/runs",async(WorkflowDbContext db,IEnumerable<IReportDataset> datasets,HttpContext http,CancellationToken ct)=>
  {
   var versions=await db.Set<WorkflowVersion>().AsNoTracking().ToListAsync(ct);var keys=http.RequestServices.GetServices<Nexora.BuildingBlocks.Persistence.IWorkflowEntitySource>().Where(x=>Permissions(http).Contains(x.Metadata.Permission)).Select(x=>x.Metadata.Key).ToHashSet();var allowed=versions.Where(x=>keys.Contains(WorkflowValidation.Parse(x.GraphJson).EntityKey)).Select(x=>x.Id).ToArray();
   return await db.Set<WorkflowRun>().AsNoTracking().Where(x=>allowed.Contains(x.WorkflowVersionId)).OrderByDescending(x=>x.CreatedAtUtc).Take(100).ToListAsync(ct);
  }).RequireAuthorization("workflows.history");
  api.MapGet("/runs/{id:guid}",async(Guid id,WorkflowDbContext db,IEnumerable<IReportDataset> datasets,HttpContext http,CancellationToken ct)=>
  {
   var run=await FindRun(id,db,datasets,http,ct);var version=await db.Set<WorkflowVersion>().SingleAsync(x=>x.Id==run.WorkflowVersionId,ct);return Results.Ok(new{run,version,steps=await db.Set<WorkflowStep>().Where(x=>x.RunId==id).OrderBy(x=>x.Sequence).ToListAsync(ct)});
  }).RequireAuthorization("workflows.history");
  api.MapPost("/runs/{id:guid}/cancel",async(Guid id,WorkflowConcurrency r,WorkflowDbContext db,IEnumerable<IReportDataset> datasets,HttpContext http,CancellationToken ct)=>
  {
   var run=await FindRun(id,db,datasets,http,ct);if(run.Version!=r.Version)return Conflict();Check(run.Status is "queued" or "running" or "waiting","This run cannot be cancelled.");run.Status="cancelled";run.FinishedAtUtc=DateTime.UtcNow;foreach(var approval in await db.Set<WorkflowApproval>().Where(x=>x.RunId==id&&x.Status=="pending").ToListAsync(ct)){approval.Status="cancelled";approval.DecidedAtUtc=DateTime.UtcNow;approval.Reason="Run cancelled";}db.Add(new WorkflowStep{TenantId=run.TenantId,RunId=id,NodeId=run.CurrentNodeId,Kind="control",Sequence=++run.Steps,Outcome="Cancelled by authorized user"});await db.SaveChangesAsync(ct);return Results.Ok(run);
  }).RequireAuthorization("workflows.cancel");
  api.MapPost("/runs/{id:guid}/retry",async(Guid id,WorkflowConcurrency r,WorkflowDbContext db,IEnumerable<IReportDataset> datasets,WorkflowValidation validation,IRequestIdentity who,HttpContext http,CancellationToken ct)=>
  {
   var run=await FindRun(id,db,datasets,http,ct);if(run.Version!=r.Version)return Conflict();Check(run.Status=="failed","Only failed runs can be retried.");Check(run.Steps<10000,"Execution limit reached.");
   var version=await db.Set<WorkflowVersion>().SingleAsync(x=>x.Id==run.WorkflowVersionId,ct);var errors=await validation.ValidateAsync(WorkflowValidation.Parse(version.GraphJson),Permissions(http),ct);Check(errors.Length==0,string.Join(" ",errors));
   // Retry retains the original execution identity and logical action IDs.
   foreach(var token in await db.Set<WorkflowToken>().Where(x=>x.RunId==id&&x.Status=="failed").ToListAsync(ct)){token.Status="queued";token.Attempts=0;token.Error="";token.DueAtUtc=null;}run.Status="queued";run.Error="";run.Attempts=0;run.DueAtUtc=null;run.LeaseUntilUtc=null;run.FinishedAtUtc=null;db.Add(new WorkflowStep{TenantId=run.TenantId,RunId=id,NodeId=run.CurrentNodeId,Kind="control",Sequence=++run.Steps,Outcome="Retry requested by "+who.UserId});await db.SaveChangesAsync(ct);return Results.Ok(run);
  }).RequireAuthorization("workflows.retry");
  MapAdvanced(api,routes);return routes;
 }
 private static async Task<WorkflowRun> FindRun(Guid id,WorkflowDbContext db,IEnumerable<IReportDataset> datasets,HttpContext http,CancellationToken ct)
 {
  var run=await db.Set<WorkflowRun>().SingleOrDefaultAsync(x=>x.Id==id,ct)??throw new KeyNotFoundException();var version=await db.Set<WorkflowVersion>().SingleAsync(x=>x.Id==run.WorkflowVersionId,ct);http.RequestServices.GetRequiredService<WorkflowExpressions>().Source(WorkflowValidation.Parse(version.GraphJson).EntityKey,Permissions(http));return run;
 }
}
