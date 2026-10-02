using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Finance.Domain;
using Nexora.Modules.Finance.Infrastructure;
namespace Nexora.Modules.Finance;
internal sealed class WorkflowSettlementDirectory(FinanceDbContext db):IWorkflowSettlementDirectory
{
 public async Task<WorkflowSettlement?> InvoiceAsync(Guid invoiceId,CancellationToken ct)
 {
  var invoice=await db.Set<PostedInvoice>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==invoiceId,ct);if(invoice==null)return null;
  var allocated=await (from a in db.Set<Allocation>() join r in db.Set<Receipt>() on a.ReceiptId equals r.Id where a.InvoiceId==invoiceId&&r.CustomerId==invoice.CustomerId&&r.Currency==invoice.Currency&&r.StatementReference!=null&&r.StatementReference!="" select (decimal?)a.Amount).SumAsync(ct)??0;
  return new(invoice.Id,invoice.CustomerId,invoice.Currency,invoice.Total,allocated);
 }
}
