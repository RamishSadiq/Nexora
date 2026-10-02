using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Persistence;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Work.Domain;
using Nexora.Modules.Work.Infrastructure;
namespace Nexora.Modules.Work;
public sealed class WorkflowTaskUpdate(WorkDbContext db,WorkTaskCommands commands,IRequestIdentity who):IWorkflowActionHandler
{
 public WorkflowActionDescriptor Descriptor=>new("work.task.update","Update / assign task","work.manage",["taskId","title","description","status","priority","assigneeUserId","teamId","dueAtUtc"],[new("taskId","Task ID","reference",true),new("title","Title"),new("description","Description"),new("status","Status","choice",false,["open","in-progress","done","cancelled"]),new("priority","Priority","choice",false,["low","normal","high"]),new("assigneeUserId","Assignee","reference"),new("teamId","Team","reference"),new("dueAtUtc","Due date","date")],["taskId","status"]);
 public Task ValidateAsync(Dictionary<string,string> inputs,CancellationToken ct){if(!Guid.TryParse(inputs.GetValueOrDefault("taskId"),out _)||inputs.Keys.Any(x=>!Descriptor.Inputs.Contains(x)))throw new ValidationException("Choose a valid task and supported fields.");return Task.CompletedTask;}
 public async Task<WorkflowActionResult> ExecuteDetailedAsync(WorkflowActionContext context,Dictionary<string,string> input,CancellationToken ct)
 {
  var receipt=await db.Set<WorkflowActionReceipt>().SingleOrDefaultAsync(x=>x.Id==context.ActionId,ct);if(receipt!=null)return JsonSerializer.Deserialize<WorkflowActionResult>(receipt.ResultJson)!;await ValidateAsync(input,ct);var row=await db.Set<WorkItem>().SingleOrDefaultAsync(x=>x.Id==Guid.Parse(input["taskId"]),ct)??throw new ValidationException("Task is unavailable.");Guid? Id(string key,Guid? fallback)=>input.ContainsKey(key)?string.IsNullOrWhiteSpace(input[key])?null:Guid.Parse(input[key]):fallback;var request=new WorkRequest(input.GetValueOrDefault("title",row.Title),input.GetValueOrDefault("description",row.Description),input.GetValueOrDefault("status",row.Status),input.GetValueOrDefault("priority",row.Priority),input.ContainsKey("dueAtUtc")?string.IsNullOrWhiteSpace(input["dueAtUtc"])?null:DateTime.Parse(input["dueAtUtc"]).ToUniversalTime():row.DueAtUtc,Id("assigneeUserId",row.AssigneeUserId),Id("teamId",row.TeamId),row.ContactId,null);await commands.ValidateAsync(request,ct);
  if(row.AssigneeUserId!=request.AssigneeUserId&&request.AssigneeUserId!=null)db.Add(new Notification{TenantId=row.TenantId,UserId=request.AssigneeUserId.Value,WorkItemId=row.Id,Message="Task assigned: "+request.Title});row.Title=request.Title.Trim();row.Description=request.Description?.Trim()??"";row.Status=request.Status;row.Priority=request.Priority;row.DueAtUtc=request.DueAtUtc;row.AssigneeUserId=request.AssigneeUserId;row.TeamId=request.TeamId;
  db.Add(new WorkHistory{TenantId=row.TenantId,SubjectId=row.Id,ActorUserId=who.UserId!.Value,Action="task.updated",Reason=row.Status,CorrelationId="workflow:"+context.RunId});var result=new WorkflowActionResult("Task updated: "+row.Id,new(){{"taskId",row.Id.ToString()},{"status",row.Status}});db.Add(new WorkflowActionReceipt{Id=context.ActionId,TenantId=row.TenantId,RunId=context.RunId,ActionKey=Descriptor.Key,ResultJson=JsonSerializer.Serialize(result)});await db.SaveChangesAsync(ct);return result;
 }
 public async Task<string> ExecuteAsync(WorkflowActionContext context,Dictionary<string,string> inputs,CancellationToken ct)=>(await ExecuteDetailedAsync(context,inputs,ct)).Message;
}
public sealed class WorkflowNotification(WorkDbContext db,IIdentityDirectory directory,IRequestIdentity who):IWorkflowActionHandler
{
 public WorkflowActionDescriptor Descriptor=>new("work.notification.send","Notify user / team","work.manage",["userId","teamId","message"],[new("userId","User","reference"),new("teamId","Team","reference"),new("message","Message","text",true)],["count"]);
 public Task ValidateAsync(Dictionary<string,string> inputs,CancellationToken ct){if(string.IsNullOrWhiteSpace(inputs.GetValueOrDefault("message"))||inputs["message"].Length>500||inputs.Keys.Any(x=>!Descriptor.Inputs.Contains(x)))throw new ValidationException("Enter a message of at most 500 characters.");return Task.CompletedTask;}
 public async Task<WorkflowActionResult> ExecuteDetailedAsync(WorkflowActionContext context,Dictionary<string,string> inputs,CancellationToken ct)
 {
  var receipt=await db.Set<WorkflowActionReceipt>().SingleOrDefaultAsync(x=>x.Id==context.ActionId,ct);if(receipt!=null)return JsonSerializer.Deserialize<WorkflowActionResult>(receipt.ResultJson)!;await ValidateAsync(inputs,ct);var recipients=new List<Guid>();if(Guid.TryParse(inputs.GetValueOrDefault("userId"),out var user)){if(!await directory.IsValidOwnerAsync(user,null,ct))throw new ValidationException("User is unavailable.");recipients.Add(user);}if(Guid.TryParse(inputs.GetValueOrDefault("teamId"),out var team)){if(!await directory.IsValidOwnerAsync(null,team,ct))throw new ValidationException("Team is unavailable.");recipients.AddRange(await directory.GetTeamMembersAsync(team,ct));}
  if(recipients.Count==0)throw new ValidationException("Choose a user or a team with active members.");foreach(var recipient in recipients.Distinct())db.Add(new Notification{TenantId=who.TenantId!.Value,UserId=recipient,Message=inputs["message"]});var result=new WorkflowActionResult("Notification sent",new(){{"count",recipients.Distinct().Count().ToString()}});db.Add(new WorkflowActionReceipt{Id=context.ActionId,TenantId=who.TenantId!.Value,RunId=context.RunId,ActionKey=Descriptor.Key,ResultJson=JsonSerializer.Serialize(result)});await db.SaveChangesAsync(ct);return result;
 }
 public async Task<string> ExecuteAsync(WorkflowActionContext context,Dictionary<string,string> inputs,CancellationToken ct)=>(await ExecuteDetailedAsync(context,inputs,ct)).Message;
}
