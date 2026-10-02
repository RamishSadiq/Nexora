using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Finance.Domain;
using Nexora.Modules.Finance.Infrastructure;
namespace Nexora.Modules.Finance;
public static partial class FinanceModule
{
 private static void AddWorkflowInspections(IServiceCollection services)=>services.AddScoped<IWorkflowActionHandler>(sp=>new WorkflowInspectionAction(new("finance.invoice.inspect","Check actual invoice balance","finance.read",["invoiceId"],[new("invoiceId","Invoice","reference",true)],["outstanding","settled","currency","invoiceId"]),"invoiceId",async(id,ct)=>{
  var db=sp.GetRequiredService<FinanceDbContext>();var invoice=await Invoice(db,id,ct);var credits=(await db.Set<Credit>().Where(x=>x.InvoiceId==id).ToListAsync(ct)).Sum(x=>x.Amount);var allocations=(await db.Set<Allocation>().Where(x=>x.InvoiceId==id).ToListAsync(ct)).Sum(x=>x.Amount);var outstanding=invoice.Total-credits-allocations;
  return new("Invoice balance checked",new(){{"outstanding",outstanding.ToString(CultureInfo.InvariantCulture)},{"settled",outstanding<=0?"true":"false"},{"currency",invoice.Currency},{"invoiceId",id.ToString()}});
 }));
}
