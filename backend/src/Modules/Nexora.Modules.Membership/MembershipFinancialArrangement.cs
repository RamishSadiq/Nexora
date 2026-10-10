using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Membership.Domain;
using Nexora.Modules.Membership.Infrastructure;

namespace Nexora.Modules.Membership;

public static partial class MembershipModule
{
    public sealed record MembershipFinancialArrangementRequest(Guid Version, MembershipFinancialArrangementDetails? Details, string Reason);
    private static void MapMembershipFinancialArrangement(RouteGroupBuilder api)
    {
        api.MapGet("/members/{id:guid}/financial-arrangement", async (Guid id, MembershipDbContext db, CancellationToken ct) =>
        {
            var member = await db.Set<MemberSubscription>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
            return Results.Ok(new { member.Version, details = MembershipFinancialArrangementFields.Read(member) });
        });
        api.MapPut("/members/{id:guid}/financial-arrangement", async (Guid id, MembershipFinancialArrangementRequest request, MembershipDbContext db, ICrmDirectory crm, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
        {
            var member = await db.Set<MemberSubscription>().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
            if (member.Version != request.Version) throw new DbUpdateConcurrencyException();
            var details = request.Details ?? throw new ValidationException("Provide membership financial arrangement details.");
            var reason = AdministrativeText(request.Reason, 500, false) ?? throw new ValidationException("Provide a reason for the financial arrangement change.");
            if (details.RenewalMonth is < 1 or > 12) throw new ValidationException("Renewal month must be between 1 and 12.");
            if (details.RenewalDate is DateOnly renewed && (renewed == DateOnly.MinValue || renewed > DateOnly.FromDateTime(DateTime.UtcNow))) throw new ValidationException("Recorded renewal date must be on or before today.");
            if (details.NextRenewalDate == DateOnly.MinValue || (details.RenewalDate != null && details.NextRenewalDate != null && details.NextRenewalDate < details.RenewalDate)) throw new ValidationException("Next renewal date must be valid and cannot precede the recorded renewal date.");
            if (details.InvoiceToAccount == true ? details.InvoicingAccountId == null : details.InvoicingAccountId != null) throw new ValidationException("Invoice to Account = Yes requires an invoicing Account; otherwise leave that Account unset.");
            var promotion = AdministrativeText(details.PromotionCode, 160, false); var method = AdministrativeText(details.PaymentMethod, 160, false); var frequency = AdministrativeText(details.PaymentFrequency, 160, false);
            var provider = AdministrativeText(details.PaymentProvider, 20, false)?.ToLowerInvariant();
            if (provider is not (null or "gocardless" or "stripe")) throw new ValidationException("Payment provider must be GoCardless or Stripe, or left unrecorded.");
            CrmIdentity? payer = null; CrmIdentity? invoiceAccount = null;
            if (details.PaidById is Guid payerId)
            {
                payer = payerId == member.PaidById ? new CrmIdentity(payerId, member.PaidByName ?? "", member.PaidByKind ?? "") : await crm.FindActiveAsync(payerId, ct) ?? throw new KeyNotFoundException();
                if (payer.Kind is not ("contact" or "account")) throw new ValidationException("Payer must be a Contact or Account.");
            }
            if (details.InvoicingAccountId is Guid invoiceId)
            {
                invoiceAccount = invoiceId == member.InvoicingAccountId ? new CrmIdentity(invoiceId, member.InvoicingAccountName ?? "", "account") : await crm.FindActiveAsync(invoiceId, ct) ?? throw new KeyNotFoundException();
                if (invoiceAccount.Kind != "account") throw new ValidationException("Invoice recipient must be an Account.");
            }
            member.RenewalMonth = details.RenewalMonth; member.RecordedRenewalDate = details.RenewalDate; member.NextRenewalDate = details.NextRenewalDate;
            member.PromotionCode = promotion; member.PaymentMethod = method; member.PaymentFrequency = frequency; member.PaymentProvider = provider;
            member.PaidById = payer?.Id; member.PaidByName = payer?.Name; member.PaidByKind = payer?.Kind;
            member.InvoiceToAccount = details.InvoiceToAccount; member.InvoicingAccountId = invoiceAccount?.Id; member.InvoicingAccountName = invoiceAccount?.Name;
            member.UpdatedAtUtc = DateTime.UtcNow;
            History(db, who, http, id, "membership.financial-arrangement.updated", reason);
            await db.SaveChangesAsync(ct); return Results.Ok(new { member.Version, details = MembershipFinancialArrangementFields.Read(member) });
        }).RequireAuthorization("membership.manage");
    }
}
