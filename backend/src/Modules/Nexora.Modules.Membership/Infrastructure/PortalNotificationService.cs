using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Membership.Domain;

namespace Nexora.Modules.Membership.Infrastructure;

internal sealed class PortalNotificationService(MembershipDbContext db) : IPortalNotificationService
{
    public async Task<IReadOnlyList<PortalNotificationItem>> ListAsync(Guid userId, CancellationToken ct) => await db.Set<PortalNotification>().AsNoTracking()
        .Where(x => x.RecipientUserId == userId).OrderByDescending(x => x.CreatedAtUtc).Take(100)
        .Select(x => new PortalNotificationItem(x.Id, x.Kind, x.Subject, x.Body, x.DeliveryStatus, x.ReadAtUtc != null, x.CreatedAtUtc, x.ApplicationId)).ToListAsync(ct);
    public async Task MarkReadAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var row = await db.Set<PortalNotification>().SingleOrDefaultAsync(x => x.Id == id && x.RecipientUserId == userId, ct) ?? throw new KeyNotFoundException();
        row.ReadAtUtc ??= DateTime.UtcNow; await db.SaveChangesAsync(ct);
    }
    public async Task EnqueueAsync(Guid tenantId, Guid userId, Guid? applicationId, string kind, string subject, string body, CancellationToken ct)
    {
        db.Add(new PortalNotification { TenantId = tenantId, RecipientUserId = userId, ApplicationId = applicationId, Kind = kind, Subject = subject, Body = body, DeliveryStatus = "queued", DeliveryAttempts = 0 });
        await db.SaveChangesAsync(ct);
    }
}
