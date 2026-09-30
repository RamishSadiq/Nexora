using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Domain;
using Nexora.Modules.Crm.Infrastructure;
using Nexora.Modules.Identity;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;

public sealed class AccountContactTests
{
    private static Task<System.Text.Json.Nodes.JsonNode> Create(HttpClient client, string kind, string name) =>
        CreateCore(client, kind, name);
    private static async Task<System.Text.Json.Nodes.JsonNode> CreateCore(HttpClient client, string kind, string name) =>
        await Json(await Send(client, "/api/v1/crm/records", HttpMethod.Post, new { kind, name, status = "active" }), HttpStatusCode.Created);
    private static object Link(string contactId, string[]? roles = null) => new {
        contactId, startDate = DateTime.UtcNow.Date.AddDays(-30), jobTitle = "Director", workClassification = "technical",
        isMainContact = true, isAuthorisedContact = true, roles = roles ?? new[] { "finance", "membership" }
    };

    [Fact]
    public async Task Both_views_share_roles_updates_end_history_and_rejoining_without_recreating_contact()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app);
        var account = await Create(client, "account", "Company A");
        var contact = await Create(client, "contact", "Taylor");
        var path = $"/api/v1/crm/accounts/{Id(account)}/contacts";
        var link = await Json(await Send(client, path, HttpMethod.Post, Link(Id(contact))));
        foreach (var record in new[] { account, contact })
        {
            var list = await Json(await client.GetAsync($"/api/v1/crm/records/{Id(record)}/account-contacts"));
            Assert.Equal(1, list["totalCount"]!.GetValue<int>());
            var item = Assert.Single(list["items"]!.AsArray())!;
            Assert.Equal(Id(link), Id(item["association"]!));
            Assert.Equal(new[] { "finance", "membership" }, item["roles"]!.AsArray().Select(x => x!.GetValue<string>()));
            Assert.Equal("Company A", item["accountName"]!.GetValue<string>());
            Assert.Equal("Taylor", item["contactName"]!.GetValue<string>());
        }
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, path, HttpMethod.Post, Link(Id(contact)))).StatusCode);
        var update = new { version = Version(link), jobTitle = "Trainer", workClassification = "non-technical",
            isMainContact = false, isAuthorisedContact = false, roles = new[] { "training" } };
        var updated = await Json(await Send(client, $"/api/v1/crm/account-contacts/{Id(link)}", HttpMethod.Patch, update));
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, $"/api/v1/crm/account-contacts/{Id(link)}", HttpMethod.Patch, update)).StatusCode);
        var endRequest = new { version = Version(updated), endDate = DateTime.UtcNow.Date, reason = "Moved company" };
        var ended = await Json(await Send(client, $"/api/v1/crm/account-contacts/{Id(link)}/end", HttpMethod.Post, endRequest));
        Assert.Equal("Moved company", ended["endReason"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, $"/api/v1/crm/account-contacts/{Id(link)}/end", HttpMethod.Post, endRequest)).StatusCode);
        var current = await Json(await client.GetAsync($"/api/v1/crm/records/{Id(contact)}/account-contacts"));
        Assert.Empty(current["items"]!.AsArray());
        var history = await Json(await client.GetAsync($"/api/v1/crm/records/{Id(contact)}/account-contacts?includeEnded=true"));
        Assert.Single(history["items"]!.AsArray());
        Assert.Equal("training", history["items"]![0]!["roles"]![0]!.GetValue<string>());
        var second = await Create(client, "account", "Company B");
        var moved = await Json(await Send(client, $"/api/v1/crm/accounts/{Id(second)}/contacts", HttpMethod.Post, Link(Id(contact))));
        Assert.Equal(Id(contact), moved["contactId"]!.GetValue<string>());
        var rejoined = await Json(await Send(client, path, HttpMethod.Post, Link(Id(contact))));
        Assert.NotEqual(Id(link), Id(rejoined));
        var detail = await Json(await client.GetAsync($"/api/v1/crm/records/{Id(contact)}"));
        Assert.False(detail["record"]!["isArchived"]!.GetValue<bool>());
        var timeline = await Json(await client.GetAsync($"/api/v1/crm/records/{Id(contact)}/timeline"));
        Assert.Contains(timeline.AsArray(), x => x!["action"]!.GetValue<string>() == "account-contact.ended:" + Id(link));
    }

    [Fact]
    public async Task Invalid_roles_kinds_dates_archives_and_missing_csrf_are_rejected()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app);
        var account = await Create(client, "account", "Company"); var contact = await Contact(client);
        var path = $"/api/v1/crm/accounts/{Id(account)}/contacts";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(path, Link(Id(contact)))).StatusCode);
        foreach (var roles in new[] { new[] { "admin" }, new[] { "finance", "finance" } })
            Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path, HttpMethod.Post, Link(Id(contact), roles))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path, HttpMethod.Post, Link(Id(account)))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, $"/api/v1/crm/accounts/{Id(contact)}/contacts", HttpMethod.Post, Link(Id(account)))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path, HttpMethod.Post, new {
            contactId = Id(contact), startDate = DateTime.UtcNow.Date.AddDays(1), workClassification = "unknown", roles = Array.Empty<string>()
        })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/v1/crm/records/{Id(account)}/account-contacts?page=invalid")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/v1/crm/records/{Id(account)}/account-contacts?includeEnded=invalid")).StatusCode);
        await Json(await Send(client, $"/api/v1/crm/records/{Id(contact)}/archive", HttpMethod.Post, new { version = Version(contact) }));
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path, HttpMethod.Post, Link(Id(contact)))).StatusCode);
    }

    [Fact]
    public async Task Tenant_boundaries_and_permission_checks_protect_links_and_persistence()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app); using var outside = await Login(app, "outside@nexora.test");
        var account = await Create(client, "account", "Private company"); var contact = await Contact(client);
        var path = $"/api/v1/crm/accounts/{Id(account)}/contacts";
        var link = await Json(await Send(client, path, HttpMethod.Post, Link(Id(contact))));
        Assert.Equal(HttpStatusCode.NotFound, (await outside.GetAsync($"/api/v1/crm/records/{Id(account)}/account-contacts")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(outside, path, HttpMethod.Post, Link(Id(contact)))).StatusCode);
        using var anonymous = app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/api/v1/crm/records/{Id(account)}/account-contacts")).StatusCode);
        var secondary = (await outside.GetFromJsonAsync<SessionResponse>("/api/v1/auth/session"))!;
        var primary = (await client.GetFromJsonAsync<SessionResponse>("/api/v1/auth/session"))!;
        using var scope = app.Services.CreateScope();
        var options = new DbContextOptionsBuilder<CrmDbContext>().UseSqlite(scope.ServiceProvider.GetRequiredService<CrmDbContext>().Database.GetDbConnection()).Options;
        var foreignId = Guid.NewGuid();
        await using (var db = new CrmDbContext(options, new Identity(secondary.TenantId, secondary.UserId)))
        {
            Assert.Empty(await db.Set<AccountContact>().ToListAsync());
            Assert.Empty(await db.Set<AccountContactRole>().ToListAsync());
            db.Add(new CrmRecord { Id = foreignId, TenantId = secondary.TenantId, Kind = "contact", Name = "Outside" });
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.NotFound, (await Send(client, path, HttpMethod.Post, Link(foreignId.ToString()))).StatusCode);
        await using (var db = new CrmDbContext(options, new Identity(primary.TenantId, primary.UserId)))
        {
            db.Add(new AccountContact { TenantId = primary.TenantId, AccountId = Guid.Parse(Id(account)), ContactId = foreignId, StartDate = DateTime.UtcNow.Date });
            await Assert.ThrowsAsync<CrmBoundaryException>(() => db.SaveChangesAsync());
        }
        await using (var db = new CrmDbContext(options, new Identity(primary.TenantId, primary.UserId)))
        {
            var stored = await db.Set<AccountContact>().SingleAsync();
            db.Remove(stored);
            await Assert.ThrowsAsync<CrmBoundaryException>(() => db.SaveChangesAsync());
        }
    }

    [SqlServerFact]
    public async Task SqlServer_migration_replays_and_unique_current_pair_preserves_history()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("NEXORA_TEST_SQLSERVER")) {
            InitialCatalog = "NexoraAccountContactsTest_" + Guid.NewGuid().ToString("N")
        };
        var options = new DbContextOptionsBuilder<CrmDbContext>().UseSqlServer(connection.ConnectionString,
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "crm")).Options;
        var identity = new Identity(Guid.NewGuid(), Guid.NewGuid());
        await using var db = new CrmDbContext(options, identity);
        try
        {
            await db.Database.MigrateAsync(); await db.Database.MigrateAsync();
            Assert.False(db.Database.HasPendingModelChanges());
            var account = new CrmRecord { TenantId = identity.TenantId!.Value, Kind = "account", Name = "Company" };
            var contact = new CrmRecord { TenantId = account.TenantId, Kind = "contact", Name = "Person" };
            db.AddRange(account, contact); await db.SaveChangesAsync();
            AccountContact NewLink() => new() { TenantId = account.TenantId, AccountId = account.Id, ContactId = contact.Id, StartDate = DateTime.UtcNow.Date };
            var first = NewLink(); first.IsMainAccount = true; db.Add(first); await db.SaveChangesAsync();
            var otherAccount = new CrmRecord { TenantId = account.TenantId, Kind = "account", Name = "Other company" };
            db.Add(otherAccount); await db.SaveChangesAsync();
            db.Add(new AccountContact { TenantId = account.TenantId, AccountId = otherAccount.Id, ContactId = contact.Id, StartDate = DateTime.UtcNow.Date, IsMainAccount = true });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.ChangeTracker.Clear();
            db.Add(NewLink());
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.ChangeTracker.Clear();
            var stored = await db.Set<AccountContact>().SingleAsync();
            stored.EndDate = DateTime.UtcNow.Date; stored.EndReason = "Ended";
            await db.SaveChangesAsync();
            var next = NewLink(); next.IsMainAccount = true; db.Add(next); await db.SaveChangesAsync();
            Assert.Equal(2, await db.Set<AccountContact>().CountAsync());
            Assert.Single(await db.Set<AccountContact>().Where(x => x.EndDate == null).ToListAsync());
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }

    private sealed record Identity(Guid? TenantId, Guid? UserId) : IRequestIdentity;
}
