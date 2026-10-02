using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using static Nexora.BuildingBlocks.Endpoints.ModuleEndpoints;
namespace Nexora.Modules.Workflow;
public static partial class WorkflowModule
{
 private static async Task<bool> Assigned(WorkflowApproval row,IRequestIdentity who,IIdentityDirectory directory,CancellationToken ct)=>row.AssignedUserId==who.UserId||(row.AssignedUserId==null&&row.AssignedTeamId!=null&&(await directory.GetTeamMembersAsync(row.AssignedTeamId.Value,ct)).Contains(who.UserId!.Value));
 private static bool DecisionAllowed(WorkflowApproval row,HttpContext http)=>row.RequiredPermission==""||Permissions(http).Contains(row.RequiredPermission)&&Permissions(http).Contains(row.RequiredPermission.Split('.')[0]+".read");
 private static void MapOperations(RouteGroupBuilder api)
 {
  api.MapGet("/approvals",async(WorkflowDbContext db,IRequestIdentity who,IIdentityDirectory directory,WorkflowExpressions expressions,HttpContext http,CancellationToken ct)=>{
   var rows=await db.Set<WorkflowApproval>().Where(x=>x.Status=="pending").OrderBy(x=>x.DeadlineUtc).Take(200).ToArrayAsync(ct);var result=new List<object>();
   foreach(var row in rows){if(!await Assigned(row,who,directory,ct)||!DecisionAllowed(row,http))continue;var run=await db.Set<WorkflowRun>().SingleAsync(x=>x.Id==row.RunId,ct);if(run.Status is "cancelled" or "completed" or "failed")continue;var version=await db.Set<WorkflowVersion>().SingleAsync(x=>x.Id==run.WorkflowVersionId,ct);var graph=WorkflowValidation.Parse(version.GraphJson);if(!Permissions(http).Contains(expressions.Metadata(graph.EntityKey)?.Permission??"unavailable"))continue;result.Add(new{row.Id,row.RunId,row.Title,row.DeadlineUtc,row.Version,row.RequiredPermission,run.RecordLabel});}return result;
  }).RequireAuthorization("workflows.approve");
  api.MapPost("/approvals/{id:guid}/decision",async(Guid id,WorkflowApprovalDecision request,WorkflowDbContext db,IRequestIdentity who,IIdentityDirectory directory,WorkflowExpressions expressions,HttpContext http,CancellationToken ct)=>{
   var row=await db.Set<WorkflowApproval>().SingleOrDefaultAsync(x=>x.Id==id,ct)??throw new KeyNotFoundException();
   if(!await Assigned(row,who,directory,ct)||!DecisionAllowed(row,http))return Results.Forbid();
   var run=await db.Set<WorkflowRun>().SingleAsync(x=>x.Id==row.RunId,ct);var version=await db.Set<WorkflowVersion>().SingleAsync(x=>x.Id==run.WorkflowVersionId,ct);expressions.Source(WorkflowValidation.Parse(version.GraphJson).EntityKey,Permissions(http));
   if(row.Version!=request.Version)return Conflict();Check(row.Status=="pending"&&row.DeadlineUtc>DateTime.UtcNow,"This approval has expired or already been decided.");Check(run.Status is "queued" or "waiting","This run no longer accepts a decision.");Choice(request.Decision,"approved","rejected");
   row.Status=request.Decision;row.Reason=Text(request.Reason,500);row.DecidedBy=who.UserId;row.DecidedAtUtc=DateTime.UtcNow;
   var token=await db.Set<WorkflowToken>().SingleAsync(x=>x.Id==row.TokenId,ct);token.DueAtUtc=null;run.DueAtUtc=null;run.Status="queued";run.LeaseUntilUtc=null;
   db.Add(new WorkflowStep{TenantId=row.TenantId,RunId=run.Id,NodeId=row.NodeId,Kind="decision",Outcome=request.Decision+" by "+row.DecidedBy,OutputsJson=System.Text.Json.JsonSerializer.Serialize(new{decision=row.Status,reason=row.Reason,decidedBy=row.DecidedBy}),Sequence=++run.Steps});await db.SaveChangesAsync(ct);return Results.Ok(new{row.Id,row.Status});
  }).RequireAuthorization("workflows.approve");
  api.MapGet("/exceptions",async(WorkflowDbContext db,WorkflowExpressions expressions,HttpContext http,CancellationToken ct)=>{
   var now=DateTime.UtcNow;var stale=now.AddMinutes(-10);var rows=await db.Set<WorkflowRun>().Where(x=>x.Status=="failed"||(x.Status=="queued"&&x.UpdatedAtUtc<stale)||(x.Status=="waiting"&&x.DueAtUtc<stale)).OrderBy(x=>x.UpdatedAtUtc).Take(200).ToArrayAsync(ct);var result=new List<object>();
   foreach(var run in rows){var version=await db.Set<WorkflowVersion>().SingleAsync(x=>x.Id==run.WorkflowVersionId,ct);if(!Permissions(http).Contains(expressions.Metadata(WorkflowValidation.Parse(version.GraphJson).EntityKey)?.Permission??"unavailable"))continue;result.Add(new{run.Id,run.RecordLabel,run.Status,run.Error,run.CurrentNodeId,run.DueAtUtc,run.UpdatedAtUtc,run.Version,category=run.Status=="failed"?"failed":"overdue processing"});}return result;
  }).RequireAuthorization("workflows.history");
 }
 public sealed record WorkflowApprovalDecision(Guid Version,string Decision,string Reason);
}
