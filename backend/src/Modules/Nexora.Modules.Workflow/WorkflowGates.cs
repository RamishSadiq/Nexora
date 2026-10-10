using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
namespace Nexora.Modules.Workflow;

public sealed record WorkflowGateResult(bool Pending,bool NewRequest,string Outcome,string? Destination);
public sealed class WorkflowGates(WorkflowDbContext db,WorkflowExpressions expressions,IIdentityDirectory directory,IEnumerable<IWorkflowActionHandler> handlers)
{
 public static readonly string[] DecisionPermissions=["membership.decide","membership.manage","finance.post","content.publish","crm.manage","events.manage","engagement.manage","work.manage"];
 public async Task<WorkflowGateResult> ProcessAsync(WorkflowRun run,WorkflowToken token,WorkflowNode node,WorkflowGraph graph,JsonObject context,string[] permissions,CancellationToken ct)
 {
  var gateId=WorkflowProcessor.ActionId(run.Id,token.Scope+":"+node.Id);
  if(node.Kind=="approval")
  {
   var approval=await db.Set<WorkflowApproval>().SingleOrDefaultAsync(x=>x.Id==gateId,ct);var created=approval==null;
   if(approval==null)
   {
    async Task<Guid?> Resolve(string? value){if(string.IsNullOrWhiteSpace(value))return null;var text=WorkflowExpressions.Text(await expressions.ResolveAsync(value,context,graph.EntityKey,permissions,ct));return Guid.TryParse(text,out var id)&&id!=Guid.Empty?id:throw new ValidationException("Choose a valid approval assignee.");}
    var user=await Resolve(node.ApproverUserId);var team=await Resolve(node.ApproverTeamId);
    if(user==null&&team==null||!await directory.IsValidOwnerAsync(user,team,ct))throw new ValidationException("Approval requires an active tenant user or team.");
    if(team!=null&&(await directory.GetTeamMembersAsync(team.Value,ct)).Length==0)throw new ValidationException("Approval team has no active members.");
    approval=new(){Id=gateId,TenantId=run.TenantId,RunId=run.Id,TokenId=token.Id,NodeId=node.Id,Title=node.Label,AssignedUserId=user,AssignedTeamId=team,RequiredPermission=node.RequiredPermission??"",DeadlineUtc=DateTime.UtcNow.AddMinutes(node.TimeoutMinutes)};db.Add(approval);
   }
   if(approval.Status=="pending"&&approval.DeadlineUtc<=DateTime.UtcNow){approval.Status="timed-out";approval.DecidedAtUtc=DateTime.UtcNow;approval.Reason="Approval deadline expired";}
   if(approval.Status=="pending"){token.Status="waiting";token.DueAtUtc=Earlier(approval.DeadlineUtc,DateTime.UtcNow.AddMinutes(node.PollMinutes));return new(true,created,"Awaiting explicit approval",null);}
   context["steps"]![node.Id]=new JsonObject{["approvalId"]=approval.Id.ToString(),["decision"]=approval.Status,["reason"]=approval.Reason,["decidedBy"]=approval.DecidedBy?.ToString()};
   return new(false,false,"Approval "+approval.Status,approval.Status=="approved"?node.IfTrue:approval.Status=="rejected"?node.IfFalse:node.TimeoutNext);
  }
  context["gates"]??=new JsonObject();var key=gateId.ToString();var started=context["gates"]![key]==null;
  if(started)context["gates"]![key]=DateTime.UtcNow.AddMinutes(node.TimeoutMinutes).ToString("O");
  var deadline=DateTime.Parse(WorkflowExpressions.Text(context["gates"]![key]),null,System.Globalization.DateTimeStyles.RoundtripKind);
  var evaluation=context;
  if(node.ActionKey!=null)
  {
   var inspection=handlers.SingleOrDefault(h=>h.Descriptor.Key==node.ActionKey) as WorkflowInspectionAction??throw new ValidationException("Business waits can only repeat read-only inspections.");
   if(!inspection.Descriptor.RequiredPermissions.All(permissions.Contains))throw new ValidationException("Inspection permission was revoked.");
   var input=new Dictionary<string,string>();foreach(var pair in node.Inputs??new())input[pair.Key]=WorkflowExpressions.Text(await expressions.ResolveAsync(pair.Value,context,graph.EntityKey,permissions,ct));
   var result=await inspection.ExecuteDetailedAsync(new(run.Id,node.Id,gateId,run.ActorId,run.RecordLabel,run.RecordId),input,ct);var output=new JsonObject();foreach(var pair in result.Outputs)output[pair.Key]=WorkflowExpressions.Literal(pair.Value);context["steps"]![node.Id]=output;
  }
  if(node.QueryEntity!=null){var target=WorkflowExpressions.Text(await expressions.ResolveAsync(node.Value,context,graph.EntityKey,permissions,ct));if(!Guid.TryParse(target,out var targetId))throw new ValidationException("Choose a valid wait target.");evaluation=(JsonObject)context.DeepClone();evaluation["record"]=await expressions.ReadRecordAsync(node.QueryEntity,targetId,permissions,ct);}
  var matched=await expressions.EvaluateAsync(node.Predicate!,evaluation,node.QueryEntity??graph.EntityKey,permissions,ct);
  if(matched||deadline<=DateTime.UtcNow){context["steps"]![node.Id]??=new JsonObject();context["steps"]![node.Id]!["status"]=matched?"matched":"timed-out";return new(false,false,matched?"Business condition satisfied":"Business wait timed out",matched?node.Next:node.TimeoutNext);}
  token.Status="waiting";token.DueAtUtc=Earlier(deadline,DateTime.UtcNow.AddMinutes(node.PollMinutes));return new(true,started,"Waiting for current business state",null);
 }
 private static DateTime Earlier(DateTime a,DateTime b)=>a<b?a:b;
}
