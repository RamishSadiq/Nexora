using Nexora.BuildingBlocks.Domain;
namespace Nexora.Modules.Events.Domain;
public sealed class Offering : TenantEntity
{
    public string Kind { get; set; } = "event";
    public string Name { get; set; } = "";
    public string Venue { get; set; } = "";
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public int Capacity { get; set; }
    public int PassingScore { get; set; } = 50;
    public string Status { get; set; } = "published";
}
public sealed class EventSession : TenantEntity
{
    public Guid OfferingId { get; set; }
    public string Name { get; set; } = "";
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
}
public sealed class Enrollment : TenantEntity
{
    public Guid OfferingId { get; set; }
    public Guid ContactId { get; set; }
    public string ContactName { get; set; } = "";
    public string Status { get; set; } = "registered";
}
public sealed class ExamResult : TenantEntity, IAppendOnly
{
    public Guid EnrollmentId { get; set; }
    public int Score { get; set; }
    public string Outcome { get; set; } = "";
    public Guid ActorUserId { get; set; }
}
public sealed class EventHistory : TenantEntity, IAppendOnly
{
    public Guid OfferingId { get; set; }
    public Guid? EnrollmentId { get; set; }
    public string Action { get; set; } = "";
    public string Reason { get; set; } = "";
    public Guid ActorUserId { get; set; }
    public string CorrelationId { get; set; } = "";
}
