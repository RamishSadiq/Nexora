namespace Nexora.BuildingBlocks.Security;
public sealed record WorkflowPortalReview(bool HasLogin,int InvalidDelegations,string Issues);
public interface IWorkflowPortalDirectory {Task<WorkflowPortalReview> ReviewAsync(Guid contactId,CancellationToken ct);}
