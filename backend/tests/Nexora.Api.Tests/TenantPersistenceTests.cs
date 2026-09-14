using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.Modules.Identity.Domain;
using Nexora.Modules.Identity.Infrastructure.Persistence;
using Nexora.Modules.Identity.Security;

namespace Nexora.Api.Tests;

public sealed class TenantPersistenceTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory application;
    public TenantPersistenceTests(IdentityApiFactory application) => this.application = application;
    private sealed record TenantContext(Guid? TenantId) : ITenantContext;

    private NexoraIdentityDbContext Context(Guid? tenantId)
    {
        using var scope = application.Services.CreateScope();
        return new NexoraIdentityDbContext(new DbContextOptionsBuilder<NexoraIdentityDbContext>().UseSqlite(scope.ServiceProvider.GetRequiredService<NexoraIdentityDbContext>().Database.GetDbConnection()).Options, new TenantContext(tenantId));
    }

    private async Task<(Guid Primary, Guid Outside, Guid OutsideUser, Guid OutsideTeam, Guid OutsideRole)> IdsAsync()
    {
        await using var db = Context(null);
        using var system = db.BeginSystemAccess();
        return ((await db.Tenants.SingleAsync(x => x.Slug == "northstar")).Id,
            (await db.Tenants.SingleAsync(x => x.Slug == "outside")).Id,
            (await db.Users.SingleAsync(x => x.Email == "outside@nexora.test")).Id,
            (await db.Teams.SingleAsync(x => x.Name == "Outside Team")).Id,
            (await db.Roles.SingleAsync(x => x.Name == "Outside Administrator")).Id);
    }

    [Fact]
    public async Task Queries_are_scoped_without_endpoint_predicates_and_without_tenant_fail_closed()
    {
        var ids = await IdsAsync();
        foreach (var tenant in new Guid?[] { ids.Primary, ids.Outside, null })
        {
            await using var db = Context(tenant);
            Assert.All(await db.Users.ToListAsync(), x => Assert.Equal(tenant, x.TenantId));
            Assert.All(await db.Roles.ToListAsync(), x => Assert.Equal(tenant, x.TenantId));
            Assert.All(await db.Teams.ToListAsync(), x => Assert.Equal(tenant, x.TenantId));
            Assert.All(await db.Tenants.ToListAsync(), x => Assert.Equal(tenant, x.Id));
            Assert.All(await db.RolePermissions.Include(x => x.Role).ToListAsync(), x => Assert.Equal(tenant, x.Role.TenantId));
            Assert.All(await db.TeamMembers.Include(x => x.Team).Include(x => x.User).ToListAsync(), x =>
            {
                Assert.Equal(tenant, x.Team.TenantId);
                Assert.Equal(tenant, x.User.TenantId);
            });
            if (tenant is null)
            {
                Assert.Empty(await db.Users.ToListAsync());
                Assert.Empty(await db.Set<IdentityUserRole<Guid>>().ToListAsync());
            }
        }
        await using var primary = Context(ids.Primary);
        Assert.Null(await primary.Users.FindAsync(ids.OutsideUser));
        Assert.Null(await primary.Teams.FindAsync(ids.OutsideTeam));
        Assert.Single(await primary.Users.ToListAsync());
    }

    [Theory]
    [InlineData("insert")]
    [InlineData("update")]
    [InlineData("delete")]
    [InlineData("spoof-owner")]
    [InlineData("move-owner")]
    [InlineData("membership")]
    [InlineData("user-role")]
    [InlineData("user-claim")]
    [InlineData("permission")]
    [InlineData("missing-tenant")]
    [InlineData("user-update")]
    [InlineData("role-update")]
    [InlineData("audit")]
    public async Task Cross_tenant_writes_are_rejected_and_database_is_unchanged(string attack)
    {
        var ids = await IdsAsync();
        await using (var db = Context(attack == "missing-tenant" ? null : ids.Primary))
        {
            switch (attack)
            {
                case "user-update":
                    db.Users.Update(new NexoraUser { Id = ids.OutsideUser, TenantId = ids.Primary, DisplayName = "Injected" });
                    break;
                case "role-update":
                    db.Roles.Update(new NexoraRole { Id = ids.OutsideRole, TenantId = ids.Primary, Name = "Injected" });
                    break;
                case "audit":
                    db.AuditEvents.Add(new AuditEvent { TenantId = ids.Outside, EventType = "injected", Outcome = "injected" });
                    break;
                case "insert":
                case "missing-tenant":
                    db.Teams.Add(new Team { TenantId = ids.Outside, Name = "Injected" });
                    break;
                case "update":
                case "spoof-owner":
                    db.Teams.Update(new Team { Id = ids.OutsideTeam, TenantId = attack == "spoof-owner" ? ids.Primary : ids.Outside, Name = "Injected" });
                    break;
                case "delete":
                    db.Teams.Remove(new Team { Id = ids.OutsideTeam, TenantId = ids.Primary, Name = "Injected" });
                    break;
                case "move-owner":
                    (await db.Teams.SingleAsync()).TenantId = ids.Outside;
                    break;
                case "membership":
                    db.TeamMembers.Add(new TeamMember { TeamId = (await db.Teams.SingleAsync()).Id, UserId = ids.OutsideUser });
                    break;
                case "user-role":
                    db.Set<IdentityUserRole<Guid>>().Add(new() { UserId = ids.OutsideUser, RoleId = (await db.Roles.SingleAsync()).Id });
                    break;
                case "user-claim":
                    db.Set<IdentityUserClaim<Guid>>().Add(new() { UserId = ids.OutsideUser, ClaimType = "permission", ClaimValue = "identity.manage" });
                    break;
                case "permission":
                    db.RolePermissions.Add(new RolePermission { RoleId = ids.OutsideRole, Permission = "identity.manage" });
                    break;
            }
            await Assert.ThrowsAsync<TenantIsolationException>(() => db.SaveChangesAsync());
            Assert.Throws<TenantIsolationException>(() => db.SaveChanges());
        }
        await using var verify = Context(ids.Outside);
        Assert.Equal("Outside Team", (await verify.Teams.SingleAsync()).Name);
        Assert.Single(await verify.Users.ToListAsync());
    }

    [Fact]
    public async Task Same_tenant_insert_update_and_delete_succeed()
    {
        var ids = await IdsAsync();
        await using var db = Context(ids.Primary);
        var team = new Team { TenantId = ids.Primary, Name = "Allowed" };
        db.Teams.Add(team);
        await db.SaveChangesAsync();
        team.Name = "Updated";
        db.SaveChanges();
        db.ChangeTracker.Clear();
        Assert.Equal("Updated", (await db.Teams.SingleAsync(x => x.Id == team.Id)).Name);
        db.Teams.Remove(await db.Teams.SingleAsync(x => x.Id == team.Id));
        await db.SaveChangesAsync();
        Assert.False(await db.Teams.AnyAsync(x => x.Id == team.Id));
    }
}
