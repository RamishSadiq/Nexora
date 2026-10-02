using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Finance.Domain;
using Nexora.Modules.Finance.Infrastructure;
using static Nexora.BuildingBlocks.Endpoints.ModuleEndpoints;
namespace Nexora.Modules.Finance;
public static partial class FinanceModule
{
 private static void AddWorkflowCommands(IServiceCollection services)
 {
  AddWorkflowInspections(services);
  services.AddScoped<IWorkflowSettlementDirectory,WorkflowSettlementDirectory>();
  AddWorkflowMoneyCommands(services);
  services.AddScoped<IWorkflowActionHandler>(sp=>{var db=sp.GetRequiredService<FinanceDbContext>();var who=sp.GetRequiredService<IRequestIdentity>();return new WorkflowCommandAction<FinanceDbContext>(db,who,new("finance.order.post","Post draft order to invoice","finance.post",["orderId"],[new("orderId","Draft order","reference",true)],["invoiceId","number","total"]),(input,ct)=>{Check(Guid.TryParse(input.GetValueOrDefault("orderId"),out _),"Choose a draft order.");return Task.CompletedTask;},async(context,input,ct)=>{var order=await db.Set<SalesOrder>().SingleOrDefaultAsync(x=>x.Id==Guid.Parse(input["orderId"]),ct)??throw new KeyNotFoundException();var invoice=PostOrder(order,db,who,new DefaultHttpContext{TraceIdentifier="workflow:"+context.RunId},context.ActionId);return new("Invoice posted",new(){{"invoiceId",invoice.Id.ToString()},{"number",invoice.Number},{"total",invoice.Total.ToString(System.Globalization.CultureInfo.InvariantCulture)}});});});
 }
}
