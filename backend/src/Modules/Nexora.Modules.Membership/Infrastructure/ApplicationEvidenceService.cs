using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Documents;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Membership.Domain;

namespace Nexora.Modules.Membership.Infrastructure;

internal sealed class ApplicationEvidenceService(MembershipDbContext db, IRequestIdentity who, IDocumentScanner scanner, IPortalRecipientDirectory recipients) : IApplicationEvidenceService
{
    private static EvidenceItem Read(ApplicationEvidence e) => new(e.Id, e.Name, e.Size, e.ScanStatus, e.CreatedAtUtc);
    private async Task<PortalApplicationDraft> Draft(Guid contactId, Guid id, Guid? accountId, CancellationToken ct) =>
        await db.Set<PortalApplicationDraft>().SingleOrDefaultAsync(d => d.Id == id && d.ContactId == contactId && d.AccountId == accountId, ct) ?? throw new KeyNotFoundException();
    private static void Editable(PortalApplicationDraft d, Guid version)
    {
        if (d.Version != version) throw new DbUpdateConcurrencyException();
        if (d.ApplicationId != null) throw new ValidationException("Submitted evidence cannot be changed.");
        // Serialize evidence changes with saves, discard, submission and other uploads.
        d.UpdatedAtUtc = DateTime.UtcNow;
    }
    private async Task<IReadOnlyList<EvidenceItem>> List(Guid draftId, CancellationToken ct) => await db.Set<ApplicationEvidence>().AsNoTracking()
        .Where(e => e.DraftId == draftId).OrderBy(e => e.CreatedAtUtc)
        .Select(e => new EvidenceItem(e.Id, e.Name, e.Size, e.ScanStatus, e.CreatedAtUtc)).ToListAsync(ct);
    private async Task<ApplicationEvidence> File(Guid draftId, Guid fileId, CancellationToken ct) =>
        await db.Set<ApplicationEvidence>().SingleOrDefaultAsync(e => e.DraftId == draftId && e.Id == fileId, ct) ?? throw new KeyNotFoundException();
    private void Audit(Guid subject, string action) => db.Add(new MembershipHistory
    {
        TenantId = who.TenantId!.Value, ActorUserId = who.UserId!.Value, SubjectId = subject,
        Action = action, Reason = "Application evidence lifecycle", CorrelationId = subject.ToString()
    });
    public async Task<IReadOnlyList<EvidenceItem>> DraftListAsync(Guid contactId, Guid draftId, Guid? accountId, CancellationToken ct)
    { await Draft(contactId, draftId, accountId, ct); return await List(draftId, ct); }
    public async Task<EvidenceChange> UploadAsync(Guid contactId, Guid draftId, Guid? accountId, Guid version, DocumentUpload upload, CancellationToken ct)
    {
        var draft = await Draft(contactId, draftId, accountId, ct); Editable(draft, version);
        if (await db.Set<ApplicationEvidence>().CountAsync(e => e.DraftId == draftId, ct) >= 10)
            throw new ValidationException("An application can hold up to 10 evidence files.");
        var item = new ApplicationEvidence { TenantId = who.TenantId!.Value, DraftId = draftId, Name = upload.Name,
            Content = upload.Content, Size = upload.Content.Length, Sha256 = Convert.ToHexString(SHA256.HashData(upload.Content)) };
        item.ScanStatus = await scanner.ScanAsync(item.Content, ct); item.ScannedAtUtc = DateTime.UtcNow;
        db.Add(item); Audit(draftId, "application.evidence.uploaded");
        var recipient = await recipients.FindPortalUserIdAsync(who.TenantId!.Value, draft.ContactId, ct);
        if (recipient is Guid userId) db.Add(new PortalNotification { TenantId = who.TenantId.Value, RecipientUserId = userId, ApplicationId = draft.ApplicationId,
            Kind = "application.evidence." + item.ScanStatus, Subject = item.ScanStatus == "clean" ? "Evidence scan passed" : "Evidence scan pending", Body = item.ScanStatus == "clean" ? $"The evidence file {item.Name} is ready to view." : $"The evidence file {item.Name} is awaiting a security scan.", DeliveryStatus = "queued" });
        await db.SaveChangesAsync(ct); return new(Read(item), draft.Version);
    }
    public async Task<EvidenceChange> RemoveAsync(Guid contactId, Guid draftId, Guid? accountId, Guid fileId, Guid version, CancellationToken ct)
    {
        var draft = await Draft(contactId, draftId, accountId, ct); Editable(draft, version);
        db.Remove(await File(draftId, fileId, ct)); Audit(draftId, "application.evidence.removed");
        await db.SaveChangesAsync(ct); return new(null, draft.Version);
    }
    public async Task<EvidenceChange> ScanDraftAsync(Guid contactId, Guid draftId, Guid? accountId, Guid fileId, Guid version, CancellationToken ct)
    {
        var draft = await Draft(contactId, draftId, accountId, ct); Editable(draft, version);
        var item = await File(draftId, fileId, ct); await Scan(item, ct);
        await db.SaveChangesAsync(ct); return new(Read(item), draft.Version);
    }
    private async Task Scan(ApplicationEvidence item, CancellationToken ct)
    {
        // A rejection cannot be overridden through a retry endpoint.
        if (item.ScanStatus != "quarantined") throw new ValidationException("Only files awaiting a scan can be checked again.");
        if (item.ScannedAtUtc > DateTime.UtcNow.AddSeconds(-30)) throw new ValidationException("Wait 30 seconds before checking the file again.");
        item.ScanStatus = await scanner.ScanAsync(item.Content, ct); item.ScannedAtUtc = DateTime.UtcNow;
        Audit(item.DraftId, "application.evidence.scanned");
        var draft = await db.Set<PortalApplicationDraft>().AsNoTracking().SingleAsync(d => d.Id == item.DraftId, ct);
        var recipient = await recipients.FindPortalUserIdAsync(who.TenantId!.Value, draft.ContactId, ct);
        if (recipient is Guid userId) db.Add(new PortalNotification { TenantId = who.TenantId.Value, RecipientUserId = userId, ApplicationId = draft.ApplicationId,
            Kind = "application.evidence." + item.ScanStatus, Subject = item.ScanStatus == "clean" ? "Evidence scan passed" : item.ScanStatus == "rejected" ? "Evidence file rejected" : "Evidence scan pending",
            Body = item.ScanStatus == "clean" ? $"The evidence file {item.Name} is ready to view." : item.ScanStatus == "rejected" ? $"The evidence file {item.Name} was rejected by the security scan." : $"The evidence file {item.Name} is awaiting a security scan.", DeliveryStatus = "queued" });
    }
    private static DocumentUpload Download(ApplicationEvidence item)
    {
        if (item.ScanStatus != "clean") throw new ValidationException("This file is not available for download until its security scan passes.");
        if (item.Sha256 != Convert.ToHexString(SHA256.HashData(item.Content))) throw new ValidationException("File integrity check failed.");
        return new(item.Name, item.Content);
    }
    public async Task<DocumentUpload> DownloadDraftAsync(Guid contactId, Guid draftId, Guid? accountId, Guid fileId, CancellationToken ct)
    { await Draft(contactId, draftId, accountId, ct); return Download(await File(draftId, fileId, ct)); }
    private async Task<Guid?> ApplicationDraft(Guid id, Guid? contactId, Guid? accountId, CancellationToken ct)
    {
        // Null contact is used only by staff endpoints protected by membership.read + crm.files.
        if (!await db.Set<MembershipApplication>().AnyAsync(a => a.Id == id &&
            (contactId == null || (accountId != null ? a.AccountId == accountId : a.AccountId == null && a.ContactId == contactId && a.ApplicationType == "individual")), ct))
            throw new KeyNotFoundException();
        return await db.Set<PortalApplicationDraft>().Where(d => d.ApplicationId == id).Select(d => (Guid?)d.Id).SingleOrDefaultAsync(ct);
    }
    public async Task<IReadOnlyList<EvidenceItem>> ApplicationListAsync(Guid applicationId, Guid? contactId, Guid? accountId, CancellationToken ct)
    { var id = await ApplicationDraft(applicationId, contactId, accountId, ct); return id.HasValue ? await List(id.Value, ct) : []; }
    public async Task<DocumentUpload> DownloadApplicationAsync(Guid applicationId, Guid? contactId, Guid? accountId, Guid fileId, CancellationToken ct)
    { var id = await ApplicationDraft(applicationId, contactId, accountId, ct) ?? throw new KeyNotFoundException(); return Download(await File(id, fileId, ct)); }
    public async Task<EvidenceItem> ScanApplicationAsync(Guid applicationId, Guid fileId, CancellationToken ct)
    {
        var id = await ApplicationDraft(applicationId, null, null, ct) ?? throw new KeyNotFoundException();
        var item = await File(id, fileId, ct); await Scan(item, ct); await db.SaveChangesAsync(ct); return Read(item);
    }
}
