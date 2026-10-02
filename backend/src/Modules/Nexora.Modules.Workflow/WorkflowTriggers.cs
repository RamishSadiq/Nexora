using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Persistence;
using Nexora.BuildingBlocks.Security;
namespace Nexora.Modules.Workflow;
public sealed class WorkflowTriggers(IServiceScopeFactory scopes,IEnumerable<IWorkflowEntitySource> sources,WorkflowExecutionContext execution)
{
 public static DateTime Next(WorkflowScheduleConfig config,DateTime after)
 {
  if(config.Mode=="once")return config.AtUtc!.Value;
  if(config.Mode is "interval" or "relative")return after.AddMinutes(config.IntervalMinutes);
  var zone=TimeZoneInfo.FindSystemTimeZoneById(config.Timezone);var local=TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(after,DateTimeKind.Utc),zone);var time=TimeOnly.Parse(config.Time);var date=local.Date.Add(time.ToTimeSpan());
  for(var i=0;i<9;i++,date=date.AddDays(1)){if(date<=local||config.Mode=="weekly"&&(int)date.DayOfWeek!=config.Weekday||zone.IsInvalidTime(date))continue;return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(date,DateTimeKind.Unspecified),zone);}throw new ValidationException("Cannot resolve schedule time.");
 }
 public static async Task<WorkflowRun> EnqueueAsync(WorkflowDbContext db,WorkflowVersion version,Guid actor,Guid recordId,string label,string key,string triggerJson="{}",Guid? parent=null,Guid? root=null,Guid[]? ancestry=null,string context="{}",CancellationToken ct=default)
 {
  var existing=await db.Set<WorkflowRun>().SingleOrDefaultAsync(x=>x.WorkflowVersionId==version.Id&&x.RequestKey==key,ct);if(existing!=null)return existing;
  var graph=WorkflowValidation.Parse(version.GraphJson);var run=new WorkflowRun{TenantId=version.TenantId,ActorId=actor,WorkflowVersionId=version.Id,RecordId=recordId,RecordLabel=label,RequestKey=key,CurrentNodeId=graph.EntryNodeId,TriggerJson=triggerJson,ParentRunId=parent,RootRunId=root,AncestryJson=JsonSerializer.Serialize((ancestry??[]).Append(version.DefinitionId)),ContextJson=context};db.Add(run);return run;
 }
 public async Task TickAsync(CancellationToken ct)
 {
  foreach(var source in sources)
  foreach(var ev in await source.PendingEventsAsync(ct))
  {
   await using var scope=scopes.CreateAsyncScope();var directory=scope.ServiceProvider.GetRequiredService<IWorkflowPrincipalDirectory>();var db=scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();
   var definitions=await db.Set<WorkflowDefinition>().IgnoreQueryFilters().AsNoTracking().Where(x=>x.TenantId==ev.TenantId&&!x.IsArchived&&x.ActiveVersionId!=null).ToArrayAsync(ct);
   foreach(var definition in definitions)
   {
    var version=await db.Set<WorkflowVersion>().IgnoreQueryFilters().AsNoTracking().SingleAsync(x=>x.TenantId==ev.TenantId&&x.Id==definition.ActiveVersionId,ct);var graph=WorkflowValidation.Parse(version.GraphJson);var activatedAt=await db.Set<WorkflowAudit>().IgnoreQueryFilters().Where(x=>x.TenantId==ev.TenantId&&x.DefinitionId==definition.Id&&x.Action=="version.activated").OrderByDescending(x=>x.CreatedAtUtc).Select(x=>(DateTime?)x.CreatedAtUtc).FirstOrDefaultAsync(ct);if(activatedAt==null||ev.CreatedAtUtc<activatedAt)continue;if(graph.EntityKey!=ev.EntityKey||graph.Trigger is "manual" or "external" or "scheduled"||version.CreatedAtUtc>ev.CreatedAtUtc)continue;
    var ancestry=JsonSerializer.Deserialize<Guid[]>(ev.AncestryJson)??[];if(ancestry.Contains(definition.Id)||ancestry.Length>=8)continue;
    var current=WorkflowExpressions.Object(ev.CurrentJson);var previous=WorkflowExpressions.Object(ev.PreviousJson);bool Changed(string field)=>WorkflowExpressions.Text(current[field])!=WorkflowExpressions.Text(previous[field]);
    var matched=graph.Trigger==ev.Kind||ev.Kind=="record.updated"&&(graph.Trigger=="record.status-changed"&&Changed("status")||graph.Trigger=="record.field-changed"&&Changed(graph.TriggerField!)||graph.Trigger=="record.assigned"&&(Changed("ownerUserId")||Changed("assigneeUserId")));
    if(!matched)continue;var principal=await directory.ResolveAsync(ev.TenantId,version.PublishedBy,ct);if(principal==null||!principal.Permissions.Contains("workflows.execute")||!principal.Permissions.Contains(source.Metadata.Permission))continue;using var identity=execution.Enter(principal);
    var expression=scope.ServiceProvider.GetRequiredService<WorkflowExpressions>();var context=new JsonObject{["record"]=current.DeepClone(),["previous"]=previous.DeepClone(),["trigger"]=new JsonObject{["kind"]=ev.Kind,["id"]=ev.Id.ToString()},["variables"]=new JsonObject()};
    if(graph.Filter!=null&&!await expression.EvaluateAsync(graph.Filter,context,graph.EntityKey,principal.Permissions,ct))continue;
    await EnqueueAsync(db,version,principal.UserId,ev.RecordId,WorkflowExpressions.Text(current["label"]),"event:"+ev.Id,new JsonObject{["record"]=current.DeepClone(),["previous"]=previous.DeepClone(),["kind"]=ev.Kind,["id"]=ev.Id.ToString()}.ToJsonString(),ev.CausationRunId,ev.RootRunId,ancestry,ct:ct);try{await db.SaveChangesAsync(ct);}catch(DbUpdateException){db.ChangeTracker.Clear();if(!await db.Set<WorkflowRun>().AnyAsync(x=>x.WorkflowVersionId==version.Id&&x.RequestKey=="event:"+ev.Id,ct))throw;}
   }
   await source.AcknowledgeEventAsync(ev.TenantId,ev.Id,ct);
  }
  await using var discovery=scopes.CreateAsyncScope();var discoveryDb=discovery.ServiceProvider.GetRequiredService<WorkflowDbContext>();var now=DateTime.UtcNow;
  var schedules=await discoveryDb.Set<WorkflowSchedule>().IgnoreQueryFilters().AsNoTracking().Where(x=>x.IsEnabled&&x.NextAtUtc<=now&&(x.LeaseUntilUtc==null||x.LeaseUntilUtc<now)).Take(20).ToArrayAsync(ct);
  foreach(var candidate in schedules)
  {
   var claimed=await discoveryDb.Set<WorkflowSchedule>().IgnoreQueryFilters().Where(x=>x.Id==candidate.Id&&x.TenantId==candidate.TenantId&&x.IsEnabled&&x.NextAtUtc<=now&&(x.LeaseUntilUtc==null||x.LeaseUntilUtc<now)).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.LeaseUntilUtc,now.AddMinutes(3)),ct);if(claimed!=1)continue;
   await using var scope=scopes.CreateAsyncScope();var principal=await scope.ServiceProvider.GetRequiredService<IWorkflowPrincipalDirectory>().ResolveAsync(candidate.TenantId,candidate.ActorId,ct);using var identity=execution.Enter(principal??new(candidate.TenantId,candidate.ActorId,[]));var db=scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();var row=await db.Set<WorkflowSchedule>().SingleAsync(x=>x.Id==candidate.Id,ct);
   try
   {
    if(principal==null||!principal.Permissions.Contains("workflows.execute"))throw new ValidationException("Schedule owner's permission was revoked.");var definition=await db.Set<WorkflowDefinition>().SingleAsync(x=>x.Id==row.DefinitionId,ct);if(definition.IsArchived||definition.ActiveVersionId!=row.WorkflowVersionId){row.IsEnabled=false;await db.SaveChangesAsync(ct);continue;}
    var version=await db.Set<WorkflowVersion>().SingleAsync(x=>x.Id==row.WorkflowVersionId,ct);var graph=WorkflowValidation.Parse(version.GraphJson);var config=graph.Schedule!;var expression=scope.ServiceProvider.GetRequiredService<WorkflowExpressions>();var source=expression.Source(graph.EntityKey,principal.Permissions);var records=await source.QueryAsync(graph.Filter,1000,ct);if(records.Total>1000)throw new ValidationException("Schedule filter matches more than 1,000 records. Narrow it.");
    foreach(var record in records.Items)
    {
     var occurrence=row.NextAtUtc.ToString("O");if(config.Mode=="relative"){if(!DateTime.TryParse(record.Values.GetValueOrDefault(config.AnchorField!),null,System.Globalization.DateTimeStyles.AdjustToUniversal|System.Globalization.DateTimeStyles.AssumeUniversal,out var anchor))continue;var due=anchor.AddDays(config.OffsetDays);if(due>now||due<row.ActivatedAtUtc)continue;occurrence=due.ToString("O");}
     var key="schedule:"+WorkflowProcessor.ActionId(record.Id,occurrence);await EnqueueAsync(db,version,principal.UserId,record.Id,record.Label,key,new JsonObject{["kind"]="scheduled",["atUtc"]=occurrence}.ToJsonString(),ct:ct);
    }
    row.NextAtUtc=Next(config,now);row.IsEnabled=config.Mode!="once";row.LastError="";row.LeaseUntilUtc=null;await db.SaveChangesAsync(ct);
   }
   catch(Exception ex) when(ex is not OperationCanceledException){db.ChangeTracker.Clear();row=await db.Set<WorkflowSchedule>().SingleAsync(x=>x.Id==candidate.Id,ct);row.LastError=ex is ValidationException?ex.Message:"Schedule processing failed; retry remains pending.";row.LeaseUntilUtc=now.AddMinutes(1);await db.SaveChangesAsync(ct);}
  }
 }
}
