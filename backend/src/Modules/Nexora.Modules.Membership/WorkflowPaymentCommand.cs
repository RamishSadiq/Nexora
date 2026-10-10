using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Membership.Domain;
using Nexora.Modules.Membership.Infrastructure;
using static Nexora.BuildingBlocks.Endpoints.ModuleEndpoints;
namespace Nexora.Modules.Membership;
public static partial class MembershipModule
{
 private static void AddWorkflowPaymentCommand(IServiceCollection services)
 {
  services.AddScoped<IWorkflowActionHandler>(sp=>{var db=sp.GetRequiredService<MembershipDbContext>();var who=sp.GetRequiredService<IRequestIdentity>();return new WorkflowCommandAction<MembershipDbContext>(db,who,new("membership.member.renew-settled","Renew against a linked reconciled invoice","membership.manage",["membershipId","invoiceId","termEndUtc","reason"],[new("membershipId","Membership","reference",true),new("invoiceId","Linked renewal invoice","reference",true),new("termEndUtc","Reviewed current term end","date",true),new("reason","Reason","text",true)],["membershipId","endsAtUtc"],AdditionalPermissions:["finance.read"]),(input,ct)=>{
   Check(Guid.TryParse(input.GetValueOrDefault("membershipId"),out _)&&Guid.TryParse(input.GetValueOrDefault("invoiceId"),out _),"Choose a membership and its linked invoice.");Check(DateTime.TryParse(input.GetValueOrDefault("termEndUtc"),CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out _),"Choose the reviewed term end.");Text(input.GetValueOrDefault("reason",""),500);return Task.CompletedTask;
  },async(context,input,ct)=>{
   var member=await db.Set<MemberSubscription>().SingleOrDefaultAsync(x=>x.Id==Guid.Parse(input["membershipId"]),ct)??throw new KeyNotFoundException();var term=DateTime.Parse(input["termEndUtc"],CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind).ToUniversalTime();Check(member.EndsAtUtc==term,"The membership term changed after review; do not reuse the payment.");Check(member.Status is "active" or "lapsed","Membership is not renewable.");
   var invoice=await sp.GetRequiredService<IWorkflowSettlementDirectory>().InvoiceAsync(Guid.Parse(input["invoiceId"]),ct)??throw new KeyNotFoundException();var product=await db.Set<MembershipProduct>().SingleAsync(x=>x.Id==member.ProductId,ct);var policy=await db.Set<MembershipPolicy>().SingleOrDefaultAsync(x=>x.ProductId==member.ProductId,ct);var quote=MembershipOperations.Quote(product,policy,member.EndsAtUtc>DateTime.UtcNow.Date?member.EndsAtUtc:DateTime.UtcNow.Date,freeMembership:member.FreeMembership==true);
   Check(invoice.CustomerId==MembershipFinancialArrangementFields.InvoiceCustomerId(member)&&invoice.Currency==quote.Currency&&invoice.Total==quote.Total&&invoice.Total>0&&invoice.VerifiedAllocated>=invoice.Total,"Linked invoice must match the member customer, currency and renewal quote and be fully allocated from reconciled receipts.");var reference="invoice:"+invoice.InvoiceId;Check(!await db.Set<MembershipReview>().AnyAsync(x=>x.PaymentReference==reference,ct),"This invoice has already been used for a membership term.");
   var review=await db.Set<MembershipReview>().SingleOrDefaultAsync(x=>x.MembershipId==member.Id&&x.TermEndUtc==term,ct);if(review==null){review=new(){TenantId=who.TenantId!.Value,MembershipId=member.Id,TermEndUtc=term};db.Add(review);}review.PaymentVerified=true;review.PaymentReference=reference;member.UpdatedAtUtc=DateTime.UtcNow;
   await ApplyMemberChange(member,"renew",input["reason"],db,who,new DefaultHttpContext{TraceIdentifier="workflow:"+context.RunId},ct);return new("Renewed using verified linked settlement",new(){{"membershipId",member.Id.ToString()},{"endsAtUtc",member.EndsAtUtc.ToString("O")}});
  });});
 }
}
