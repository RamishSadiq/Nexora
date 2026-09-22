using Nexora.BuildingBlocks.Domain;
namespace Nexora.Modules.Membership.Domain;

public sealed class ApplicationEvidence : TenantEntity
{
    public Guid DraftId { get; set; }
    public string Name { get; set; } = "";
    public byte[] Content { get; set; } = [];
    public int Size { get; set; }
    public string Sha256 { get; set; } = "";
    public string ScanStatus { get; set; } = "quarantined";
    public DateTime? ScannedAtUtc { get; set; }
}
