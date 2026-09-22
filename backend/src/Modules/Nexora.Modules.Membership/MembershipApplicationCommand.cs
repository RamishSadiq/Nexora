using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Membership.Domain;
using Nexora.Modules.Membership.Infrastructure;
using static Nexora.BuildingBlocks.Endpoints.ModuleEndpoints;
namespace Nexora.Modules.Membership;
internal static class MembershipApplicationCommand
{
 public static async Task<MembershipApplication> CreateAsync(ApplicationRequest r, MembershipDbContext db, ICrmDirectory crm, IRequestIdentity who, string correlationId, CancellationToken ct)
 {
            var contact = await crm.FindActiveAsync(r.ContactId,ct) ?? throw new KeyNotFoundException(); Check(contact.Kind == "contact", "Membership requires a Contact.");
            CrmIdentity? account = null;
            if (r.AccountId.HasValue) { account = await crm.FindActiveAsync(r.AccountId.Value,ct) ?? throw new KeyNotFoundException(); Check(account.Kind == "account", "Membership organisation must be an Account."); }
            var product = await db.Set<MembershipProduct>().SingleOrDefaultAsync(x => x.Id == r.ProductId && x.IsActive,ct) ?? throw new KeyNotFoundException();
            Choice(r.ApplicationType, "corporate", "individual", "affiliate");
            Check(r.ApplicationType != "corporate" || account != null, "Corporate applications require an organisation.");
            Check(r.ApplicationType != "individual" || account == null, "Individual applications must not include an organisation.");
            if (account != null) Check(await crm.HasCurrentAccountContactAsync(account.Id, contact.Id, ct), "Link the main Contact to this organisation in CRM before submitting.");
            Check(r.TermsAccepted, "Terms must be accepted before submitting an application.");
            Check(r.ApplicantRole == null || r.ApplicantRole.Length <= 80, "Applicant role must not exceed 80 characters.");
            Check(r.ApplicantNotes == null || r.ApplicantNotes.Length <= 4000, "Applicant notes must not exceed 4000 characters.");
            Check(r.StartsAtUtc.Date >= DateTime.UtcNow.Date && r.StartsAtUtc.Date <= DateTime.UtcNow.Date.AddYears(2), "Choose a start date within the next two years.");
            var policy = await db.Set<MembershipPolicy>().SingleOrDefaultAsync(x => x.ProductId == product.Id, ct);
            var quote = MembershipOperations.Quote(product, policy, DateTime.SpecifyKind(r.StartsAtUtc.Date, DateTimeKind.Utc));
            var application = new MembershipApplication { TenantId = who.TenantId!.Value, AccountId = account?.Id, AccountName = account?.Name, ContactId = contact.Id, ContactName = contact.Name, ProductId = product.Id, ApplicationType = r.ApplicationType, ApplicantRole = TextOptional(r.ApplicantRole, 80), TermsAccepted = true, TermsAcceptedAtUtc = DateTime.UtcNow, ApplicantNotes = TextOptional(r.ApplicantNotes, 4000), RequestedStartUtc = DateTime.SpecifyKind(r.StartsAtUtc.Date, DateTimeKind.Utc), RequestedEndUtc = quote.EndsAtUtc, QuotedRate = quote.Total, Currency = product.Currency, TermMonths = product.TermMonths };
            db.Add(application); db.Add(new MembershipHistory { TenantId = who.TenantId!.Value, ActorUserId = who.UserId!.Value, SubjectId = application.Id, Action = "application.submitted", Reason = "Submitted for review", CorrelationId = correlationId }); return application;
 }
 private static string? TextOptional(string? value,int max) => string.IsNullOrWhiteSpace(value) ? null : Text(value,max);
}
