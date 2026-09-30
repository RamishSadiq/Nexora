using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.Modules.Crm.Domain;
using Nexora.Modules.Crm.Infrastructure;
using Nexora.Modules.Identity;

namespace Nexora.Api.Tests;

public sealed partial class CrmEndpointTests
{
    [Fact]
    public async Task Structured_profiles_enforce_tenant_kind_uniqueness_and_immutable_parent()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var outside = await Login(app, "outside@nexora.test");
        var contact = await Create(staff, "Profile parent"); var other = await Create(staff, "Other parent"); var account = await Create(staff, "Company", "account");
        var id = Guid.Parse(contact["id"]!.GetValue<string>());
        var path = $"/api/v1/crm/records/{id}";
        Assert.Equal(HttpStatusCode.NotFound, (await outside.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(outside, path + "/profile", HttpMethod.Put, new { version = contact["version"]!.GetValue<string>(), contactDetails = new { firstName = "Foreign" } })).StatusCode);
        var session = (await staff.GetFromJsonAsync<SessionResponse>("/api/v1/auth/session"))!;
        using var scope = app.Services.CreateScope();
        var options = new DbContextOptionsBuilder<CrmDbContext>().UseSqlite(scope.ServiceProvider.GetRequiredService<CrmDbContext>().Database.GetDbConnection()).Options;
        await using (var db = new CrmDbContext(options, new Identity(session.TenantId, session.UserId)))
        {
            db.Add(new ContactProfile { TenantId = session.TenantId, RecordId = Guid.Parse(account["id"]!.GetValue<string>()) });
            await Assert.ThrowsAsync<CrmBoundaryException>(() => db.SaveChangesAsync());
        }
        await using (var db = new CrmDbContext(options, new Identity(Guid.NewGuid(), Guid.NewGuid())))
        {
            Assert.Empty(await db.Set<ContactProfile>().ToListAsync());
            db.Add(new ContactProfile { TenantId = session.TenantId, RecordId = id });
            await Assert.ThrowsAsync<CrmBoundaryException>(() => db.SaveChangesAsync());
        }
        await using (var db = new CrmDbContext(options, new Identity(session.TenantId, session.UserId)))
        {
            db.Add(new ContactProfile { TenantId = session.TenantId, RecordId = id }); await db.SaveChangesAsync();
            var row = await db.Set<ContactProfile>().SingleAsync(x => x.RecordId == id);
            row.RecordId = Guid.Parse(other["id"]!.GetValue<string>());
            await Assert.ThrowsAsync<CrmBoundaryException>(() => db.SaveChangesAsync());
        }
        await using (var db = new CrmDbContext(options, new Identity(session.TenantId, session.UserId)))
        {
            db.Add(new ContactProfile { TenantId = session.TenantId, RecordId = id });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
    }
}
