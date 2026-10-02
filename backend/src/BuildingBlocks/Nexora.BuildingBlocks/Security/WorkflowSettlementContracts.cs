namespace Nexora.BuildingBlocks.Security;

public sealed record WorkflowSettlement(Guid InvoiceId,Guid CustomerId,string Currency,decimal Total,decimal VerifiedAllocated);
public interface IWorkflowSettlementDirectory
{
 Task<WorkflowSettlement?> InvoiceAsync(Guid invoiceId,CancellationToken ct);
}
