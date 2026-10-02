using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Persistence;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Contracts;
using Nexora.Modules.Crm.Domain;
using Nexora.Modules.Crm.Infrastructure;
namespace Nexora.Modules.Crm;
public static partial class CrmModule
{
 public sealed class WorkflowRecordAction(CrmDbContext db,IIdentityDirectory directory,IRequestIdentity who,string operation):IWorkflowActionHandler
 {
  public WorkflowActionDescriptor Descriptor=>new("crm.record."+operation,operation=="archive"?"Archive Contact / Account":operation=="create"?"Create Contact / Account":"Update / assign Contact / Account","crm.manage",operation=="archive"?["recordId"]:operation=="create"?["kind","name","status","category","ownerUserId","ownerTeamId"]:["recordId","name","status","category","ownerUserId","ownerTeamId"],operation=="archive"?[new("recordId","Record ID","reference",true)]:operation=="create"?[new("kind","Record type","choice",true,["contact","account"]),new("name","Name","text",true),new("status","Status","choice",false,["active","prospect","inactive"]),new("category","Category"),new("ownerUserId","Owner user","reference"),new("ownerTeamId","Owner team","reference")]:[new("recordId","Record ID","reference",true),new("name","Name"),new("status","Status","choice",false,["active","prospect","inactive"]),new("category","Category"),new("ownerUserId","Owner user","reference"),new("ownerTeamId","Owner team","reference")],["recordId","name","status"]);
  private static Guid? GuidValue(Dictionary<string,string> input,string key){var text=input.GetValueOrDefault(key);if(string.IsNullOrWhiteSpace(text))return null;return Guid.TryParse(text,out var id)?id:throw new ValidationException("Invalid reference: "+key);}
  public Task ValidateAsync(Dictionary<string,string> input,CancellationToken ct){if(input.Keys.Any(x=>!Descriptor.Inputs.Contains(x))||input.Values.Any(x=>x.Length>500))throw new ValidationException("Unsupported action input.");if(operation!="create"&&GuidValue(input,"recordId")==null)throw new ValidationException("Choose a record.");if(operation=="create"){Choice(input.GetValueOrDefault("kind"),"contact","account");Required(input.GetValueOrDefault("name"),160);}if(input.ContainsKey("status"))Choice(input["status"],"active","prospect","inactive");return Task.CompletedTask;}
  public async Task<WorkflowActionResult> ExecuteDetailedAsync(WorkflowActionContext context,Dictionary<string,string> inputs,CancellationToken ct)
  {
   var receipt=await db.Set<WorkflowActionReceipt>().SingleOrDefaultAsync(x=>x.Id==context.ActionId,ct);if(receipt!=null)return JsonSerializer.Deserialize<WorkflowActionResult>(receipt.ResultJson)!;
   await ValidateAsync(inputs,ct);var row=operation=="create"?new CrmRecord{Id=context.ActionId,TenantId=who.TenantId!.Value,Kind=inputs["kind"]}:await RecordAsync(db,GuidValue(inputs,"recordId")!.Value,ct,true);
   if(operation=="archive"){row.IsArchived=true;row.Version=Guid.NewGuid();row.UpdatedAtUtc=DateTime.UtcNow;}
   else{var request=new RecordRequest(row.Kind,inputs.GetValueOrDefault("name",row.Name),inputs.GetValueOrDefault("status",operation=="create"?"prospect":row.Status),inputs.TryGetValue("category",out var category)?category:row.Category,inputs.ContainsKey("ownerUserId")?GuidValue(inputs,"ownerUserId"):row.OwnerUserId,inputs.ContainsKey("ownerTeamId")?GuidValue(inputs,"ownerTeamId"):row.OwnerTeamId,null);await CrmModule.ValidateAsync(request,directory,ct);Apply(row,request);}
   if(operation=="create")db.Add(row);db.Add(new Activity{TenantId=row.TenantId,RecordId=row.Id,ActorUserId=who.UserId!.Value,Action="record."+(operation=="create"?"created":operation=="archive"?"archived":"updated"),CorrelationId="workflow:"+context.RunId});
   var result=new WorkflowActionResult("Record "+operation+": "+row.Id,new(){{"recordId",row.Id.ToString()},{"name",row.Name},{"status",row.Status}});db.Add(new WorkflowActionReceipt{Id=context.ActionId,TenantId=row.TenantId,RunId=context.RunId,ActionKey=Descriptor.Key,ResultJson=JsonSerializer.Serialize(result)});await db.SaveChangesAsync(ct);return result;
  }
  public async Task<string> ExecuteAsync(WorkflowActionContext context,Dictionary<string,string> inputs,CancellationToken ct)=>(await ExecuteDetailedAsync(context,inputs,ct)).Message;
 }
}
