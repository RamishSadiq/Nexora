using System.Globalization;
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
 private static void AddWorkflowMoneyCommands(IServiceCollection services)
 {
  services.AddScoped<IWorkflowActionHandler>(sp=>new WorkflowInspectionAction(new("finance.receipt.inspect","Check receipt reconciliation and available funds","finance.read",["receiptId"],[new("receiptId","Receipt","reference",true)],["reconciled","available","currency","reference"]),"receiptId",async(id,ct)=>{var db=sp.GetRequiredService<FinanceDbContext>();var row=await ReceiptAsync(db,id,ct);return new("Receipt reviewed",new(){{"reconciled",string.IsNullOrWhiteSpace(row.StatementReference)?"false":"true"},{"available",(await Available(db,row,ct)).ToString(CultureInfo.InvariantCulture)},{"currency",row.Currency},{"reference",row.Reference}});}));
  foreach(var operation in new[]{"credit","refund","reconcile"})services.AddScoped<IWorkflowActionHandler>(sp=>{
   var db=sp.GetRequiredService<FinanceDbContext>();var who=sp.GetRequiredService<IRequestIdentity>();var target=operation=="credit"?"invoiceId":"receiptId";
   return new WorkflowCommandAction<FinanceDbContext>(db,who,new("finance."+operation+".record",operation=="refund"?"Record confirmed external refund (does not send funds)":operation=="credit"?"Record invoice credit":"Reconcile recorded receipt","finance.post",[target,"amount","currency","reference","reason"],[new(target,"Finance record","reference",true),new("amount","Amount","number",true),new("currency","Currency","choice",true,["GBP","EUR","USD"]),new("reference","External or statement reference","text",true),new("reason","Reason","text",true)],["recordId"]),(input,ct)=>{Check(Guid.TryParse(input.GetValueOrDefault(target),out _),"Choose a finance record.");Check(decimal.TryParse(input.GetValueOrDefault("amount"),NumberStyles.Number,CultureInfo.InvariantCulture,out var amount),"Enter a valid amount.");Money(amount);Choice(input.GetValueOrDefault("currency",""),"GBP","EUR","USD");Text(input.GetValueOrDefault("reference",""),100);Text(input.GetValueOrDefault("reason",""),500);return Task.CompletedTask;},async(context,input,ct)=>{
    var id=Guid.Parse(input[target]);var amount=decimal.Parse(input["amount"],CultureInfo.InvariantCulture);var http=new DefaultHttpContext{TraceIdentifier="workflow:"+context.RunId};Guid result;
    if(operation=="credit"){var invoice=await Invoice(db,id,ct);Check(invoice.Currency==input["currency"],"Currency must match the invoice.");result=(await RecordCredit(invoice,new(amount,input["reference"],input["reason"]),db,who,http,ct,context.ActionId)).Id;}
    else{var receipt=await ReceiptAsync(db,id,ct);Check(receipt.Currency==input["currency"],"Currency must match the receipt.");if(operation=="refund")result=(await RecordRefund(receipt,new(amount,input["reference"],input["reason"]),db,who,http,ct,context.ActionId)).Id;else{RecordReconciliation(receipt,new(receipt.Version,input["reference"],amount,input["currency"]),db,who,http);result=receipt.Id;}}
    return new("Finance "+operation+" recorded",new(){{"recordId",result.ToString()}});
   });
  });
 }
 private static async Task<Credit> RecordCredit(PostedInvoice invoice,MoneyRequest request,FinanceDbContext db,IRequestIdentity who,HttpContext http,CancellationToken ct,Guid? rowId=null)
 {
  Money(request.Amount);var credited=(await db.Set<Credit>().Where(x=>x.InvoiceId==invoice.Id).ToListAsync(ct)).Sum(x=>x.Amount);Check(request.Amount<=invoice.Total-credited,"Credit exceeds the uncredited invoice total.");var row=new Credit{Id=rowId??Guid.NewGuid(),TenantId=who.TenantId!.Value,InvoiceId=invoice.Id,Amount=request.Amount,Reference=Text(request.Reference,100),Reason=Text(request.Reason,500)};db.Add(row);Touch(invoice);History(db,who,http,invoice.Id,"invoice.credited",row.Reason);return row;
 }
 private static async Task<Refund> RecordRefund(Receipt receipt,MoneyRequest request,FinanceDbContext db,IRequestIdentity who,HttpContext http,CancellationToken ct,Guid? rowId=null)
 {
  Money(request.Amount);Check(request.Amount<=await Available(db,receipt,ct),"Refund exceeds unallocated funds. Reverse allocations first if necessary.");var row=new Refund{Id=rowId??Guid.NewGuid(),TenantId=who.TenantId!.Value,ReceiptId=receipt.Id,Amount=request.Amount,Reference=Text(request.Reference,100),Reason=Text(request.Reason,500)};db.Add(row);Touch(receipt);History(db,who,http,receipt.Id,"refund.recorded",row.Reason);return row;
 }
 private static void RecordReconciliation(Receipt receipt,ReconcileRequest request,FinanceDbContext db,IRequestIdentity who,HttpContext http)
 {
  Check(receipt.StatementReference==null,"Receipt is already reconciled.");Check(receipt.Amount==request.Amount&&receipt.Currency==request.Currency,"Statement amount and currency must match the original receipt.");receipt.StatementReference=Text(request.StatementReference,100);History(db,who,http,receipt.Id,"receipt.reconciled",receipt.StatementReference);
 }
}
