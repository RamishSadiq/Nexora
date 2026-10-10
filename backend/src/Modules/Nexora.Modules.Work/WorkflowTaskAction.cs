using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Work.Domain;
using Nexora.Modules.Work.Infrastructure;
using static Nexora.BuildingBlocks.Endpoints.ModuleEndpoints;
namespace Nexora.Modules.Work;
public sealed class WorkTaskCommands(WorkDbContext db,IIdentityDirectory directory,ICrmDirectory crm,IRequestIdentity who)
{
 public async Task ValidateAsync(WorkRequest r,CancellationToken ct)
 {
  Text(r.Title);Check(r.Description==null||r.Description.Length<=500,"Description is too long.");Choice(r.Status,"open","in-progress","done","cancelled");Choice(r.Priority,"low","normal","high");Check(await directory.IsValidOwnerAsync(r.AssigneeUserId,r.TeamId,ct),"Choose an active assignee and team in your tenant.");if(r.ContactId!=null)Check(await crm.FindActiveAsync(r.ContactId.Value,ct)!=null,"Related Contact is unavailable.");
 }
 public async Task<WorkItem> CreateAsync(WorkRequest r,Guid id,string correlation,CancellationToken ct)
 {
  var existing=await db.Set<WorkItem>().SingleOrDefaultAsync(x=>x.Id==id,ct);if(existing!=null)return existing;
  await ValidateAsync(r,ct);
  var row=new WorkItem{Id=id,TenantId=who.TenantId!.Value,Title=Text(r.Title),Description=r.Description?.Trim()??"",Status=r.Status,Priority=r.Priority,DueAtUtc=r.DueAtUtc,AssigneeUserId=r.AssigneeUserId,TeamId=r.TeamId,ContactId=r.ContactId};db.Add(row);
  if(row.AssigneeUserId!=null)db.Add(new Notification{TenantId=row.TenantId,UserId=row.AssigneeUserId.Value,WorkItemId=id,Message="Task assigned: "+row.Title});
  db.Add(new WorkHistory{TenantId=row.TenantId,SubjectId=id,ActorUserId=who.UserId!.Value,Action="task.created",Reason=row.Title,CorrelationId=correlation});await db.SaveChangesAsync(ct);return row;
 }
}
public sealed class WorkflowTaskAction(WorkTaskCommands commands):IWorkflowActionHandler
{
 public WorkflowActionDescriptor Descriptor=>new("work.task.create","Create task","work.manage",["title","description","priority","assigneeUserId","teamId","dueInDays","contactId"],[new("title","Title","text",true),new("description","Description"),new("priority","Priority","choice",false,["low","normal","high"]),new("assigneeUserId","Assignee","reference"),new("teamId","Team","reference"),new("dueInDays","Due in days","number"),new("contactId","Related Contact","reference")],["taskId"]);
 private static WorkRequest Request(Dictionary<string,string> inputs,string recordLabel)
 {
  string Get(string key,string fallback="")=>inputs.GetValueOrDefault(key,fallback);
  Guid? Id(string key){var value=Get(key);if(string.IsNullOrWhiteSpace(value))return null;Check(Guid.TryParse(value,out _),"Choose a valid user/team.");return Guid.Parse(value);}
  var title=Get("title").Replace("{{record.label}}",recordLabel);var desc=Get("description").Replace("{{record.label}}",recordLabel);
  var due=Get("dueInDays");DateTime? dueAt=null;if(due!=""){Check(int.TryParse(due,out var days)&&days is >=0 and <=3650,"Due date must be 0–3650 days from execution.");dueAt=DateTime.UtcNow.AddDays(int.Parse(due));}
  return new(title,desc,"open",Get("priority","normal"),dueAt,Id("assigneeUserId"),Id("teamId"),Id("contactId"),null);
 }
 public Task ValidateAsync(Dictionary<string,string> inputs,CancellationToken ct)
 {
  Check(inputs.Count<=7&&inputs.Keys.All(Descriptor.Inputs.Contains),"Unsupported action input.");Check(inputs.Values.All(x=>x!=null&&x.Length<=500),"Action inputs are too long.");
  return commands.ValidateAsync(Request(inputs,"Record"),ct);
 }
 public async Task<WorkflowActionResult> ExecuteDetailedAsync(WorkflowActionContext context,Dictionary<string,string> inputs,CancellationToken ct){var task=await commands.CreateAsync(Request(inputs,context.RecordLabel),context.ActionId,"workflow:"+context.RunId,ct);return new("Task created: "+task.Id,new(){{"taskId",task.Id.ToString()}});}
 public async Task<string> ExecuteAsync(WorkflowActionContext context,Dictionary<string,string> inputs,CancellationToken ct)
 {
  var task=await commands.CreateAsync(Request(inputs,context.RecordLabel),context.ActionId,"workflow:"+context.RunId,ct);return "Task created: "+task.Id;
 }
}
