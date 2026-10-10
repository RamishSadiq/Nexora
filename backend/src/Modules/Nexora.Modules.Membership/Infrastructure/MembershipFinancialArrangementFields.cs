using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Membership.Domain;

namespace Nexora.Modules.Membership.Infrastructure;

public static class MembershipFinancialArrangementFields
{
    public static MembershipFinancialArrangementDetails Read(MemberSubscription member) => new(member.RenewalMonth, member.RecordedRenewalDate, member.NextRenewalDate, member.PromotionCode,
        member.PaymentMethod, member.PaymentFrequency, member.PaidById, member.PaidByName, member.PaidByKind, member.InvoiceToAccount, member.InvoicingAccountId, member.InvoicingAccountName, member.PaymentProvider);
    public static Guid InvoiceCustomerId(MemberSubscription member) => member.InvoiceToAccount == true && member.InvoicingAccountId.HasValue ? member.InvoicingAccountId.Value : member.PaidById ?? member.AccountId ?? member.ContactId;
}
