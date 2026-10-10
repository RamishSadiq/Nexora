namespace Nexora.Modules.Crm.Domain;

public sealed class ProfileDefinition : TenantRow
{
    public string ParentNode { get; set; } = "";
    public string ChildNode { get; set; } = "";
    public string NormalizedParent { get; set; } = "";
    public string NormalizedChild { get; set; } = "";
    public bool IsArchived { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
}

public sealed class RecordProfileAssignment : RecordChild
{
    public int Slot { get; set; }
    public Guid DefinitionId { get; set; }
}
