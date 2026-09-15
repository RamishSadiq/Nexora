namespace Nexora.Modules.Crm.Domain;

public abstract class TenantRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
}

public sealed class CrmRecord : TenantRow
{
    public string Kind { get; set; } = "contact";
    public string Name { get; set; } = "";
    public string Status { get; set; } = "active";
    public string? Category { get; set; }
    public Guid? OwnerUserId { get; set; }
    public Guid? OwnerTeamId { get; set; }
    public bool IsArchived { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public abstract class RecordChild : TenantRow
{
    public Guid RecordId { get; set; }
}
public sealed class Address : RecordChild
{
    public string Label { get; set; } = "";
    public string Line1 { get; set; } = "";
    public string? Line2 { get; set; }
    public string City { get; set; } = "";
    public string? Region { get; set; }
    public string PostalCode { get; set; } = "";
    public string Country { get; set; } = "";
}
public sealed class CommunicationMethod : RecordChild
{
    public string Kind { get; set; } = "email";
    public string Value { get; set; } = "";
    public string Consent { get; set; } = "unknown";
    public string? ConsentEvidence { get; set; }
}
public sealed class RecordRelationship : RecordChild
{
    public Guid TargetRecordId { get; set; }
    public string Label { get; set; } = "";
}
public sealed class Note : RecordChild
{
    public string Body { get; set; } = "";
    public Guid ActorUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
public sealed class RecordTag : RecordChild
{
    public string Name { get; set; } = "";
}
public sealed class RecordFile : RecordChild
{
    public string Name { get; set; } = "";
    public byte[] Content { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
public sealed class Activity : RecordChild
{
    public string Action { get; set; } = "";
    public Guid ActorUserId { get; set; }
    public string CorrelationId { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
public sealed class SavedView : TenantRow
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "contact";
    public string Search { get; set; } = "";
    public string Status { get; set; } = "";
    public string Sort { get; set; } = "name";
    public bool Archived { get; set; }
    public string Columns { get; set; } = "name,status,category";
}
public sealed class CustomFieldDefinition : TenantRow
{
    public int DisplayOrder { get; set; }
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "contact";
    public string DataType { get; set; } = "text";
}
public sealed class CustomFieldValue : RecordChild
{
    public Guid DefinitionId { get; set; }
    public string? TextValue { get; set; }
    public decimal? NumberValue { get; set; }
    public bool? BooleanValue { get; set; }
    public DateTime? DateValue { get; set; }
}
