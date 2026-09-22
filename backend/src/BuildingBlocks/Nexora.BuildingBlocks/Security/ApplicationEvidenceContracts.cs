using Nexora.BuildingBlocks.Documents;
namespace Nexora.BuildingBlocks.Security;

public sealed record EvidenceItem(Guid Id, string Name, int Size, string ScanStatus, DateTime CreatedAtUtc);
public sealed record EvidenceChange(EvidenceItem? Document, Guid DraftVersion);
public interface IApplicationEvidenceService
{
    Task<IReadOnlyList<EvidenceItem>> DraftListAsync(Guid contactId, Guid draftId, Guid? accountId, CancellationToken ct);
    Task<EvidenceChange> UploadAsync(Guid contactId, Guid draftId, Guid? accountId, Guid version, DocumentUpload upload, CancellationToken ct);
    Task<EvidenceChange> RemoveAsync(Guid contactId, Guid draftId, Guid? accountId, Guid fileId, Guid version, CancellationToken ct);
    Task<EvidenceChange> ScanDraftAsync(Guid contactId, Guid draftId, Guid? accountId, Guid fileId, Guid version, CancellationToken ct);
    Task<DocumentUpload> DownloadDraftAsync(Guid contactId, Guid draftId, Guid? accountId, Guid fileId, CancellationToken ct);
    Task<IReadOnlyList<EvidenceItem>> ApplicationListAsync(Guid applicationId, Guid? contactId, Guid? accountId, CancellationToken ct);
    Task<DocumentUpload> DownloadApplicationAsync(Guid applicationId, Guid? contactId, Guid? accountId, Guid fileId, CancellationToken ct);
    Task<EvidenceItem> ScanApplicationAsync(Guid applicationId, Guid fileId, CancellationToken ct);
}
