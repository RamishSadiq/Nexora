using Nexora.BuildingBlocks.Persistence;
using Nexora.BuildingBlocks.Reporting;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Endpoints;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Work.Domain;
using Nexora.Modules.Work.Infrastructure;
using static Nexora.BuildingBlocks.Endpoints.ModuleEndpoints;
namespace Nexora.Modules.Work;
public static class WorkModule
{
 public static IServiceCollection AddNexoraWork(this IServiceCollection services,IConfiguration config){services.AddDbContext<WorkDbContext>(o=>o.UseSqlServer(config.GetConnectionString("Nexora"),sql=>sql.MigrationsHistoryTable("__EFMigrationsHistory","work")));services.AddScoped<IReportDataset>(sp=>new ReportDataset("tasks","Tasks","work.read",sp.GetRequiredService<WorkDbContext>().Set<WorkItem>().AsNoTracking().Select(x=>new ReportRow{Id=x.Id,Label=x.Title,Status=x.Status,CreatedAtUtc=x.CreatedAtUtc})));
        return services;}
 public static IEndpointRouteBuilder MapNexoraWork(this IEndpointRouteBuilder routes)
 {
  var api=routes.Module("work").WithAttributes<WorkDbContext>("work.manage", typeof(WorkItem));api.MapGet("/directory",(IIdentityDirectory directory,CancellationToken ct)=>directory.GetAsync(ct));api.MapGet("/contacts",(string? search,ICrmDirectory crm,CancellationToken ct)=>crm.SearchContactsAsync(search,ct));
  api.MapGet("/items",async(WorkDbContext db,string? status,CancellationToken ct)=>{var q=db.Set<WorkItem>().AsNoTracking();if(!string.IsNullOrEmpty(status)){Choice(status,"open","in-progress","done","cancelled");q=q.Where(x=>x.Status==status);}return Results.Ok(await q.OrderBy(x=>x.DueAtUtc).ThenBy(x=>x.Id).Take(200).ToListAsync(ct));});
  api.MapPost("/items",async(WorkRequest r,WorkDbContext db,IIdentityDirectory directory,ICrmDirectory crm,IRequestIdentity who,HttpContext http,CancellationToken ct)=>{await Validate(r,directory,crm,ct);var row=new WorkItem{TenantId=who.TenantId!.Value};Apply(row,r);db.Add(row);Notify(db,who,row);History(db,who,http,row.Id,"task.created",row.Title);await db.SaveChangesAsync(ct);return Results.Ok(row);}).RequireAuthorization("work.manage");
  api.MapPatch("/items/{id:guid}",async(Guid id,WorkRequest r,WorkDbContext db,IIdentityDirectory directory,ICrmDirectory crm,IRequestIdentity who,HttpContext http,CancellationToken ct)=>{var row=await db.Set<WorkItem>().SingleOrDefaultAsync(x=>x.Id==id,ct)??throw new KeyNotFoundException();if(row.Version!=r.Version)return Conflict();await Validate(r,directory,crm,ct);var previous=row.AssigneeUserId;Apply(row,r);if(previous!=row.AssigneeUserId)Notify(db,who,row);History(db,who,http,id,"task.updated",row.Status);await db.SaveChangesAsync(ct);return Results.Ok(row);}).RequireAuthorization("work.manage");
  api.MapGet("/notifications",async(WorkDbContext db,CancellationToken ct)=>await db.Set<Notification>().OrderByDescending(x=>x.CreatedAtUtc).Take(100).ToListAsync(ct));
  api.MapPost("/notifications/{id:guid}/read",async(Guid id,WorkDbContext db,CancellationToken ct)=>{var row=await db.Set<Notification>().SingleOrDefaultAsync(x=>x.Id==id,ct)??throw new KeyNotFoundException();row.IsRead=true;await db.SaveChangesAsync(ct);return Results.NoContent();});
  api.MapGet("/imports",async(WorkDbContext db,CancellationToken ct)=>await db.Set<ImportBatch>().OrderByDescending(x=>x.CreatedAtUtc).Take(100).ToListAsync(ct));
  api.MapPost("/imports/preview",async(ImportRequest r,WorkDbContext db,IRequestIdentity who,HttpContext http,CancellationToken ct)=>{Check(r.Rows is{Length:>0 and <=500},"Provide 1–500 task rows.");var batch=new ImportBatch{TenantId=who.TenantId!.Value,Name=Text(r.Name),CreatedByUserId=who.UserId!.Value,RowCount=r.Rows.Length};var rows=new List<ImportRow>();foreach(var item in r.Rows){Choice(item.Priority,"low","normal","high");rows.Add(new ImportRow{TenantId=who.TenantId.Value,BatchId=batch.Id,Title=Text(item.Title),Priority=item.Priority,DueAtUtc=item.DueAtUtc});}db.Add(batch);db.AddRange(rows);History(db,who,http,batch.Id,"import.previewed",batch.RowCount+" validated task rows");await db.SaveChangesAsync(ct);return Results.Ok(new{batch,rows});}).RequireAuthorization("work.import");
  api.MapGet("/imports/{id:guid}",async(Guid id,WorkDbContext db,CancellationToken ct)=>{var batch=await db.Set<ImportBatch>().SingleOrDefaultAsync(x=>x.Id==id,ct)??throw new KeyNotFoundException();return Results.Ok(new{batch,rows=await db.Set<ImportRow>().Where(x=>x.BatchId==id).ToListAsync(ct)});}).RequireAuthorization("work.import");
  api.MapPost("/imports/{id:guid}/commit",async(Guid id,ImportVersion r,WorkDbContext db,IRequestIdentity who,HttpContext http,CancellationToken ct)=>{var batch=await db.Set<ImportBatch>().SingleOrDefaultAsync(x=>x.Id==id,ct)??throw new KeyNotFoundException();if(batch.Version!=r.Version)return Conflict();Check(batch.Status=="preview","Import already committed.");var rows=await db.Set<ImportRow>().Where(x=>x.BatchId==id).ToListAsync(ct);foreach(var row in rows){var item=new WorkItem{TenantId=who.TenantId!.Value,Title=row.Title,Priority=row.Priority,DueAtUtc=row.DueAtUtc};db.Add(item);History(db,who,http,item.Id,"task.imported",batch.Name);}batch.Status="completed";History(db,who,http,id,"import.completed",rows.Count+" tasks");await db.SaveChangesAsync(ct);return Results.Ok(batch);}).RequireAuthorization("work.import","work.manage");
  api.MapGet("/export",async(WorkDbContext db,IRequestIdentity who,HttpContext http,CancellationToken ct)=>{var rows=await db.Set<WorkItem>().OrderBy(x=>x.CreatedAtUtc).Take(10000).ToListAsync(ct);var csv=new StringBuilder("Title,Status,Priority,DueUtc\r\n");foreach(var row in rows)csv.AppendLine(string.Join(",",Csv(row.Title),Csv(row.Status),Csv(row.Priority),Csv(row.DueAtUtc?.ToString("O")??"")));History(db,who,http,Guid.Empty,"tasks.exported",rows.Count+" rows");await db.SaveChangesAsync(ct);return Results.File(Encoding.UTF8.GetBytes(csv.ToString()),"text/csv","nexora-tasks.csv");}).RequireAuthorization("work.export");
  return routes;
 }
 private static async Task Validate(WorkRequest r,IIdentityDirectory directory,ICrmDirectory crm,CancellationToken ct){Text(r.Title);Check(r.Description==null||r.Description.Length<=500,"Description is too long.");Choice(r.Status,"open","in-progress","done","cancelled");Choice(r.Priority,"low","normal","high");Check(await directory.IsValidOwnerAsync(r.AssigneeUserId,r.TeamId,ct),"Choose an active assignee and team in your tenant.");if(r.ContactId!=null)Check(await crm.FindActiveAsync(r.ContactId.Value,ct)!=null,"Related Contact is unavailable.");}
 private static void Apply(WorkItem row,WorkRequest r){row.Title=Text(r.Title);row.Description=r.Description?.Trim()??"";row.Status=r.Status;row.Priority=r.Priority;row.AssigneeUserId=r.AssigneeUserId;row.TeamId=r.TeamId;row.ContactId=r.ContactId;row.DueAtUtc=r.DueAtUtc;}
 private static void Notify(WorkDbContext db,IRequestIdentity who,WorkItem row){if(row.AssigneeUserId!=null)db.Add(new Notification{TenantId=who.TenantId!.Value,UserId=row.AssigneeUserId.Value,WorkItemId=row.Id,Message="Task assigned: "+row.Title});}
 private static string Csv(string text){if(text.TrimStart().StartsWith('=')||text.TrimStart().StartsWith('+')||text.TrimStart().StartsWith('-')||text.TrimStart().StartsWith('@'))text="'"+text;return "\""+text.Replace("\"","\"\"")+"\"";}
 private static void History(WorkDbContext db,IRequestIdentity who,HttpContext http,Guid id,string action,string reason)=>db.Add(new WorkHistory{TenantId=who.TenantId!.Value,SubjectId=id,ActorUserId=who.UserId!.Value,Action=action,Reason=reason,CorrelationId=http.TraceIdentifier});
}
public sealed record WorkRequest(string Title,string? Description,string Status,string Priority,DateTime? DueAtUtc,Guid? AssigneeUserId,Guid? TeamId,Guid? ContactId,Guid? Version) : IAttributeRequest { public Dictionary<Guid, AttributeInput>? Attributes { get; init; } }
public sealed record ImportTask(string Title,string Priority,DateTime? DueAtUtc);
public sealed record ImportRequest(string Name,ImportTask[] Rows);
public sealed record ImportVersion(Guid Version);
