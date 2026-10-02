using System.ComponentModel.DataAnnotations;
namespace Nexora.BuildingBlocks.Security;
// Read-only domain inspection. Publication and execution retain the owning module's read permission.
public sealed class WorkflowInspectionAction(WorkflowActionDescriptor descriptor,string idKey,Func<Guid,CancellationToken,Task<WorkflowActionResult>> inspect):IWorkflowActionHandler
{
 public WorkflowActionDescriptor Descriptor=>descriptor with{ReadOnly=true};
 public Task ValidateAsync(Dictionary<string,string> inputs,CancellationToken ct){if(inputs.Keys.Any(k=>!descriptor.Inputs.Contains(k))||!Guid.TryParse(inputs.GetValueOrDefault(idKey),out var id)||id==Guid.Empty)throw new ValidationException("Choose a valid inspection target.");return Task.CompletedTask;}
 public async Task<WorkflowActionResult> ExecuteDetailedAsync(WorkflowActionContext context,Dictionary<string,string> inputs,CancellationToken ct){await ValidateAsync(inputs,ct);return await inspect(Guid.Parse(inputs[idKey]),ct);}
 public async Task<string> ExecuteAsync(WorkflowActionContext context,Dictionary<string,string> inputs,CancellationToken ct)=>(await ExecuteDetailedAsync(context,inputs,ct)).Message;
}
