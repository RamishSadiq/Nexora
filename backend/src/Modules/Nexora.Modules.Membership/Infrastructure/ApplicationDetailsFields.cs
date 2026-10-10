using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Membership.Domain;

namespace Nexora.Modules.Membership.Infrastructure;

internal static class ApplicationDetailsFields
{
    internal static object Read(MembershipApplication application) => new
    {
        application.Version, application.Status, application.LifecyclePurpose, application.StatusEffectiveOn,
        details = new ApplicationRequestedDetails(application.RequestedMemberTypeId, application.RequestedCategoryId, application.RequestedSubscriptionBandId,
            application.PaymentMethod, application.PaymentFrequency, application.InvoiceToAccount, application.InvoicingAccountId, application.StaffRemarks, application.Description),
        names = new { memberType = application.RequestedMemberTypeName, category = application.RequestedCategoryName, subscriptionBand = application.RequestedSubscriptionBandName, invoicingAccount = application.InvoicingAccountName }
    };

    private static string? Text(string? value, int max, bool multiline = false)
    {
        if (value?.Length > max || value?.Any(c => char.IsControl(c) && !(multiline && c is '\r' or '\n' or '\t')) == true)
            throw new ValidationException($"Use at most {max} characters without unsupported control characters.");
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    internal static async Task ApplyAsync(MembershipApplication application, ApplicationRequestedDetails details, MembershipDbContext db, ICrmDirectory crm, CancellationToken ct)
    {
        async Task<string?> Name(Guid? id, string kind, Guid? retainedId, string? retainedName)
        {
            if (!id.HasValue) return null;
            var definition = await db.Set<MembershipSetupValue>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.Kind == kind, ct)
                ?? throw new ValidationException("The selected application classification is unavailable or belongs to a different lookup list.");
            return id == retainedId && retainedName != null ? retainedName : definition.Name;
        }
        var memberType = await Name(details.MemberTypeId, "member-type", application.RequestedMemberTypeId, application.RequestedMemberTypeName);
        var category = await Name(details.CategoryId, "member-category", application.RequestedCategoryId, application.RequestedCategoryName);
        var band = await Name(details.SubscriptionBandId, "member-subscription-band", application.RequestedSubscriptionBandId, application.RequestedSubscriptionBandName);
        if (details.InvoiceToAccount == true ? details.InvoicingAccountId == null : details.InvoicingAccountId != null)
            throw new ValidationException("Invoice to Account = Yes requires an invoicing Account; otherwise leave it unset.");
        CrmIdentity? invoice = null;
        if (details.InvoicingAccountId is Guid invoiceId)
        {
            invoice = await crm.FindActiveAsync(invoiceId, ct) ?? throw new KeyNotFoundException();
            if (invoice.Kind != "account") throw new ValidationException("Invoice recipient must be an Account.");
        }
        var method = Text(details.PaymentMethod, 160); var frequency = Text(details.PaymentFrequency, 160);
        var remarks = Text(details.Remarks, 4000, true); var description = Text(details.Description, 4000, true);
        application.RequestedMemberTypeId = details.MemberTypeId; application.RequestedMemberTypeName = memberType;
        application.RequestedCategoryId = details.CategoryId; application.RequestedCategoryName = category;
        application.RequestedSubscriptionBandId = details.SubscriptionBandId; application.RequestedSubscriptionBandName = band;
        application.PaymentMethod = method; application.PaymentFrequency = frequency; application.InvoiceToAccount = details.InvoiceToAccount;
        var invoiceName = invoice?.Id == application.InvoicingAccountId && invoice?.Id != null && application.InvoicingAccountName != null ? application.InvoicingAccountName : invoice?.Name;
        application.InvoicingAccountId = invoice?.Id; application.InvoicingAccountName = invoiceName;
        application.StaffRemarks = remarks; application.Description = description;
    }

    internal static async Task ApplyApprovalAsync(MembershipApplication application, MemberSubscription member, MembershipDbContext db, ICrmDirectory crm, IRequestIdentity who, CancellationToken ct)
    {
        var expectedPurpose = application.PreviousMembershipId.HasValue ? "rejoin" : "new";
        if (application.LifecyclePurpose != null && application.LifecyclePurpose != expectedPurpose)
            throw new ValidationException("This application purpose is not supported by the approval transition.");
        if (application.InvoiceToAccount == true)
        {
            var invoice = application.InvoicingAccountId.HasValue ? await crm.FindActiveAsync(application.InvoicingAccountId.Value, ct) : null;
            if (invoice?.Kind != "account") throw new ValidationException("The requested invoicing Account is no longer available.");
        }
        foreach (var choice in new[] { ("member-type", application.RequestedMemberTypeId, application.RequestedMemberTypeName), ("category", application.RequestedCategoryId, application.RequestedCategoryName), ("subscription-band", application.RequestedSubscriptionBandId, application.RequestedSubscriptionBandName) })
            if (choice.Item2.HasValue)
            {
                if (choice.Item3 == null) throw new ValidationException("The requested classification snapshot is incomplete.");
                db.Add(new MembershipClassificationChange { TenantId = member.TenantId, MembershipId = member.Id, Kind = choice.Item1, DefinitionId = choice.Item2, NameSnapshot = choice.Item3,
                    EffectiveOn = DateOnly.FromDateTime(DateTime.UtcNow), Sequence = 1, ActorUserId = who.UserId!.Value, Reason = "Approved application classification" });
            }
        member.PaymentMethod = application.PaymentMethod; member.PaymentFrequency = application.PaymentFrequency;
        member.InvoiceToAccount = application.InvoiceToAccount; member.InvoicingAccountId = application.InvoicingAccountId; member.InvoicingAccountName = application.InvoicingAccountName;
    }
}
