using System.Security.Claims;
namespace Nexora.BuildingBlocks.Security;

public sealed record WorkflowPrincipal(Guid TenantId, Guid UserId, string[] Permissions, Guid? RunId=null, Guid? RootRunId=null, Guid[]? Ancestry=null);
public interface IWorkflowPrincipalDirectory { Task<WorkflowPrincipal?> ResolveAsync(Guid tenantId, Guid userId, CancellationToken ct); }
// Explicit background identity, isolated per asynchronous execution. HTTP identities take precedence.
public sealed class WorkflowExecutionContext
{
    private readonly AsyncLocal<WorkflowPrincipal?> current = new();
    public WorkflowPrincipal? Current => current.Value;
    public IDisposable Enter(WorkflowPrincipal principal) { var previous=current.Value; current.Value=principal; return new Scope(()=>current.Value=previous); }
    private sealed class Scope(Action restore):IDisposable { public void Dispose()=>restore(); }
}
public sealed record WorkflowActionDescriptor(string Key, string Title, string Permission, string[] Inputs, WorkflowInput[]? InputSchema=null, string[]? Outputs=null,bool ReadOnly=false,string[]? AdditionalPermissions=null) { public string[] RequiredPermissions=>new[]{Permission,Permission.Split('.')[0]+".read"}.Concat(AdditionalPermissions??[]).Distinct().ToArray(); }
public sealed record WorkflowInput(string Key,string Title,string Type="text",bool Required=false,string[]? Choices=null);
public sealed record WorkflowActionResult(string Message, Dictionary<string,string?> Outputs);
public sealed record WorkflowActionContext(Guid RunId, string NodeId, Guid ActionId, Guid ActorId, string RecordLabel, Guid RecordId);
public interface IWorkflowActionHandler
{
    WorkflowActionDescriptor Descriptor { get; }
    Task ValidateAsync(Dictionary<string,string> inputs,CancellationToken ct);
    Task<string> ExecuteAsync(WorkflowActionContext context,Dictionary<string,string> inputs,CancellationToken ct);
    async Task<WorkflowActionResult> ExecuteDetailedAsync(WorkflowActionContext context,Dictionary<string,string> inputs,CancellationToken ct){await ValidateAsync(inputs,ct);return new(await ExecuteAsync(context,inputs,ct),new());}
}
