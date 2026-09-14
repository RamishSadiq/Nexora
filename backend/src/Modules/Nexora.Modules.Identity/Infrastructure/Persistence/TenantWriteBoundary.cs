using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Nexora.Modules.Identity.Domain;

namespace Nexora.Modules.Identity.Infrastructure.Persistence;

public sealed partial class NexoraIdentityDbContext
{
    private Guid? CurrentTenantId => tenantContext?.TenantId;
    internal bool SystemAccess { get; private set; }

    // Restricted to identity bootstrap and credential verification, never request input.
    internal IDisposable BeginSystemAccess()
    {
        var previous = SystemAccess;
        SystemAccess = true;
        return new Scope(() => { ChangeTracker.Clear(); SystemAccess = previous; });
    }

    private sealed class Scope(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidateWritesAsync(default).GetAwaiter().GetResult();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        await ValidateWritesAsync(cancellationToken);
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private async Task ValidateWritesAsync(CancellationToken cancellationToken)
    {
        ChangeTracker.DetectChanges();
        if (SystemAccess) return;
        var changes = ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray();
        if (changes.Length == 0) return;
        if (CurrentTenantId is not Guid tenantId) throw new TenantIsolationException();
        foreach (var entry in changes)
        {
            ValidateOwner(entry.CurrentValues, entry, tenantId);
            if (entry.State != EntityState.Added)
            {
                // Detached Update/Remove must validate persisted ownership, not caller originals.
                var stored = await entry.GetDatabaseValuesAsync(cancellationToken);
                if (stored is null) throw new TenantIsolationException();
                ValidateOwner(stored, entry, tenantId);
                await ValidateReferencesAsync(stored, tenantId, cancellationToken);
            }
            await ValidateReferencesAsync(entry.CurrentValues, tenantId, cancellationToken);
        }
    }

    private static void ValidateOwner(PropertyValues values, EntityEntry entry, Guid tenantId)
    {
        var property = entry.Metadata.FindProperty(entry.Entity is Tenant ? "Id" : "TenantId");
        if (property is not null && !Equals(values[property.Name], tenantId)) throw new TenantIsolationException();
    }

    private async Task ValidateReferencesAsync(PropertyValues values, Guid tenantId, CancellationToken cancellationToken)
    {
        foreach (var property in values.Properties)
        {
            if (values[property.Name] is not Guid id) continue;
            bool allowed = property.Name switch
            {
                "UserId" or "ActorUserId" => await Users.AnyAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken),
                "RoleId" => await Roles.AnyAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken),
                "TeamId" => await Teams.AnyAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken),
                _ => true
            };
            if (!allowed) throw new TenantIsolationException();
        }
    }
}

public sealed class TenantIsolationException() : InvalidOperationException("The operation is outside the active tenant boundary.");
