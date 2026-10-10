using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Membership.Domain;
namespace Nexora.Modules.Membership.Infrastructure;

internal sealed class MemberApplicationService(MembershipDbContext db, ICrmDirectory crm, IRequestIdentity who) : IMemberApplicationService
{
    public async Task<IReadOnlyList<PortalProduct>> ProductsAsync(CancellationToken ct, bool organisation = false) => await db.Set<MembershipProduct>()
        .Where(p => p.IsActive && (organisation ? p.PortalOrganisationApplicationsEnabled : p.PortalApplicationsEnabled) && p.PortalTerms != "").OrderBy(p => p.Name)
        .Select(p => new PortalProduct(p.Id, p.Name)).ToListAsync(ct);
    private static MemberDraft Read(PortalApplicationDraft d) => new(d.Id, d.Version, d.ProductId, d.StartsAtUtc, d.Notes, d.Total, d.Currency, d.Terms, d.ApplicationId);
    public async Task<IReadOnlyList<MemberDraft>> DraftsAsync(Guid contactId, CancellationToken ct, Guid? accountId = null) =>
        (await db.Set<PortalApplicationDraft>().Where(d => d.ContactId == contactId && d.AccountId == accountId && d.ApplicationId == null).OrderByDescending(d => d.UpdatedAtUtc).ToListAsync(ct)).Select(Read).ToArray();
    public async Task<MemberDraft> SaveAsync(Guid contactId, SaveMemberDraft r, CancellationToken ct, Guid? accountId = null)
    {
        if (r.Id == Guid.Empty || r.StartsAtUtc.Date < DateTime.UtcNow.Date || r.StartsAtUtc.Date > DateTime.UtcNow.Date.AddYears(2) || r.Notes == null || r.Notes.Length > 4000)
            throw new ValidationException("Choose a start date within the next two years and use at most 4000 characters for notes.");
        var product = await db.Set<MembershipProduct>().SingleOrDefaultAsync(p => p.Id == r.ProductId && p.IsActive && (accountId != null ? p.PortalOrganisationApplicationsEnabled : p.PortalApplicationsEnabled) && p.PortalTerms != "", ct) ?? throw new KeyNotFoundException();
        var draft = await db.Set<PortalApplicationDraft>().SingleOrDefaultAsync(d => d.Id == r.Id && d.ContactId == contactId && d.AccountId == accountId, ct);
        if (draft == null)
        {
            if (r.Version != null) throw new KeyNotFoundException();
            if (await db.Set<PortalApplicationDraft>().CountAsync(d => d.ContactId == contactId && d.AccountId == accountId && d.ApplicationId == null, ct) >= 20) throw new ValidationException("You already have 20 saved drafts.");
            draft = new PortalApplicationDraft { Id = r.Id, TenantId = who.TenantId!.Value, ContactId = contactId, AccountId = accountId };
            db.Add(draft);
        }
        else if (draft.Version != r.Version || draft.ApplicationId != null) throw new DbUpdateConcurrencyException();
        var policy = await db.Set<MembershipPolicy>().SingleOrDefaultAsync(p => p.ProductId == product.Id, ct);
        var start = DateTime.SpecifyKind(r.StartsAtUtc.Date, DateTimeKind.Utc);
        var quote = MembershipOperations.Quote(product, policy, start, joining: true);
        draft.ProductId = product.Id; draft.StartsAtUtc = start; draft.Notes = r.Notes.Trim();
        draft.ProductVersion = product.Version; draft.PolicyVersion = policy?.Version;
        draft.Total = quote.Total; draft.Currency = quote.Currency; draft.Terms = product.PortalTerms;
        await db.SaveChangesAsync(ct); return Read(draft);
    }
    public async Task<Guid> SubmitAsync(Guid contactId, Guid id, Guid version, bool termsAccepted, CancellationToken ct, Guid? accountId = null)
    {
        var draft = await db.Set<PortalApplicationDraft>().SingleOrDefaultAsync(d => d.Id == id && d.ContactId == contactId && d.AccountId == accountId, ct) ?? throw new KeyNotFoundException();
        if (draft.ApplicationId is Guid existing) return existing;
        if (draft.Version != version) throw new DbUpdateConcurrencyException();
        if (!termsAccepted) throw new ValidationException("Accept the displayed terms before submitting.");
        var product = await db.Set<MembershipProduct>().SingleOrDefaultAsync(p => p.Id == draft.ProductId && p.IsActive && (accountId != null ? p.PortalOrganisationApplicationsEnabled : p.PortalApplicationsEnabled), ct) ?? throw new KeyNotFoundException();
        var policy = await db.Set<MembershipPolicy>().SingleOrDefaultAsync(p => p.ProductId == product.Id, ct);
        if (product.Version != draft.ProductVersion || policy?.Version != draft.PolicyVersion) throw new ValidationException("Pricing or terms changed. Save the draft again, then review the updated quote and terms.");
        if (await db.Set<MembershipApplication>().AnyAsync(a => (accountId != null ? a.AccountId == accountId : a.ContactId == contactId && a.AccountId == null) && a.ProductId == product.Id && a.Status == "submitted", ct))
            throw new ValidationException("An application for this product is already awaiting review.");
        if (await db.Set<MemberSubscription>().AnyAsync(m => (accountId != null ? m.AccountId == accountId : m.ContactId == contactId && m.AccountId == null) && m.ProductId == product.Id && m.Status == "active" && m.EndsAtUtc > draft.StartsAtUtc, ct))
            throw new ValidationException("You already have an active membership for this product. Contact your association about renewal.");
        var application = await MembershipApplicationCommand.CreateAsync(new ApplicationRequest(contactId, accountId, product.Id, draft.StartsAtUtc, accountId == null ? "individual" : "corporate", null, true, draft.Notes), db, crm, who, id.ToString(), ct);
        draft.ApplicationId = application.Id;
        db.Add(new PortalNotification { TenantId = who.TenantId!.Value, RecipientUserId = who.UserId!.Value, ApplicationId = application.Id,
            Kind = "application.submitted", Subject = "Application submitted", Body = $"Your {product.Name} application was submitted for review.", DeliveryStatus = "queued" });
        // One SaveChanges transaction includes the application, history and draft pointer.
        // A concurrent retry loses the draft version update and its application insert rolls back.
        await db.SaveChangesAsync(ct); return application.Id;
    }

    public async Task DiscardAsync(Guid contactId, Guid id, Guid version, CancellationToken ct, Guid? accountId = null)
    {
        var draft = await db.Set<PortalApplicationDraft>().SingleOrDefaultAsync(d => d.Id == id && d.ContactId == contactId && d.AccountId == accountId, ct)
            ?? throw new KeyNotFoundException();
        if (draft.Version != version) throw new DbUpdateConcurrencyException();
        if (draft.ApplicationId != null) throw new ValidationException("Submitted applications cannot be discarded. Contact your association for help.");
        db.Remove(draft);
        db.Add(new MembershipHistory { TenantId = who.TenantId!.Value, ActorUserId = who.UserId!.Value,
            SubjectId = draft.Id, Action = "application.draft.discarded", Reason = "Member discarded an unsubmitted draft", CorrelationId = draft.Id.ToString() });
        // The version token also protects against a simultaneous save or submission.
        await db.SaveChangesAsync(ct);
    }

    public async Task<MemberApplicationReceipt> ReceiptAsync(Guid contactId, Guid id, CancellationToken ct, Guid? accountId = null)
    {
        var application = await db.Set<MembershipApplication>().AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == id && (accountId != null ? a.AccountId == accountId : a.ContactId == contactId && a.AccountId == null) && (accountId != null || a.ApplicationType == "individual"), ct)
            ?? throw new KeyNotFoundException();
        var product = await db.Set<MembershipProduct>().Where(p => p.Id == application.ProductId).Select(p => p.Name).SingleAsync(ct);
        var snapshot = await db.Set<PortalApplicationDraft>().AsNoTracking()
            .SingleOrDefaultAsync(d => d.ApplicationId == application.Id && d.ContactId == contactId && d.AccountId == accountId, ct);
        var membership = await db.Set<MemberSubscription>().AsNoTracking()
            .Where(m => m.ApplicationId == application.Id && (accountId != null ? m.AccountId == accountId : m.ContactId == contactId && m.AccountId == null))
            .Select(m => new OwnMembership(m.Id, product, m.Status, m.StartsAtUtc, m.EndsAtUtc)).SingleOrDefaultAsync(ct);
        // Never project staff decision reasons, history reasons or staff-entered notes.
        // Terms/notes come only from this member's retained portal submission snapshot.
        return new(application.Id, product, application.Status, application.CreatedAtUtc,
            application.RequestedStartUtc, application.RequestedEndUtc, application.QuotedRate, application.Currency,
            application.TermsAcceptedAtUtc, snapshot?.Terms, snapshot?.Notes, membership);
    }
}


