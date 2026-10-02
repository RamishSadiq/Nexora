using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Persistence;
namespace Nexora.BuildingBlocks.Security;
// Module-owned commands stage their domain changes and history; receipt and effects commit together.
public sealed class WorkflowCommandAction<TDb>(TDb db,IRequestIdentity identity,WorkflowActionDescriptor descriptor,Func<Dictionary<string,string>,CancellationToken,Task> validate,Func<WorkflowActionContext,Dictionary<string,string>,CancellationToken,Task<WorkflowActionResult>> stage):IWorkflowActionHandler where TDb:DbContext
{
 public WorkflowActionDescriptor Descriptor=>descriptor;
 public Task ValidateAsync(Dictionary<string,string> inputs,CancellationToken ct)=>validate(inputs,ct);
 public async Task<WorkflowActionResult> ExecuteDetailedAsync(WorkflowActionContext context,Dictionary<string,string> inputs,CancellationToken ct)
 {
  var receipt=await db.Set<WorkflowActionReceipt>().SingleOrDefaultAsync(x=>x.Id==context.ActionId,ct);if(receipt!=null)return JsonSerializer.Deserialize<WorkflowActionResult>(receipt.ResultJson)!;await validate(inputs,ct);var result=await stage(context,inputs,ct);db.Add(new WorkflowActionReceipt{Id=context.ActionId,TenantId=identity.TenantId!.Value,RunId=context.RunId,ActionKey=descriptor.Key,ResultJson=JsonSerializer.Serialize(result)});await db.SaveChangesAsync(ct);return result;
 }
 public async Task<string> ExecuteAsync(WorkflowActionContext context,Dictionary<string,string> inputs,CancellationToken ct)=>(await ExecuteDetailedAsync(context,inputs,ct)).Message;
}
