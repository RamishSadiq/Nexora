using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Domain;
using Nexora.Modules.Crm.Infrastructure;
using Nexora.Modules.Identity;

namespace Nexora.Api.Tests;

public sealed class CrmEndpointTests
{
    private sealed record Identity(Guid? TenantId, Guid? UserId) : IRequestIdentity;
    private static async Task<HttpClient> Login(IdentityApiFactory app, string email = IdentityApiFactory.AdminEmail)
    {
        var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await Send(client, "/api/v1/auth/login", HttpMethod.Post,
            new LoginRequest(email, IdentityApiFactory.AdminPassword, false))).StatusCode);
        return client;
    }
    private static async Task<HttpResponseMessage> Send(HttpClient client, string path, HttpMethod method, object? body = null)
    {
        var csrf = await client.GetFromJsonAsync<CsrfResponse>("/api/v1/auth/csrf");
        using var request = new HttpRequestMessage(method, path) { Content = body == null ? null : JsonContent.Create(body) };
        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        return await client.SendAsync(request);
    }
    private static async Task<JsonNode> Json(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected, $"Expected {expected}, got {response.StatusCode}: {text}");
        return JsonNode.Parse(text)!;
    }
    private static Task<JsonNode> Create(HttpClient client, string name, string kind = "contact") => CreateCore(client, name, kind);
    private static async Task<JsonNode> CreateCore(HttpClient client, string name, string kind) => await Json(await Send(client,
        "/api/v1/crm/records", HttpMethod.Post, new { name, kind, status = "active" }), HttpStatusCode.Created);

    [Fact]
    public async Task Contact_and_account_lifecycle_related_data_and_concurrency()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app);
        var account = await Create(client, "Northstar Account", "account");
        var contact = await Create(client, "Taylor Contact"); var id = contact["id"]!.GetValue<string>();
        var path = "/api/v1/crm/records/" + id;
        var updated = await Json(await Send(client, path, HttpMethod.Patch, new { kind = "contact", name = "Taylor Updated", status = "prospect", version = contact["version"]!.GetValue<string>() }));
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, path, HttpMethod.Patch, new { kind = "contact", name = "Stale", status = "active", version = contact["version"]!.GetValue<string>() })).StatusCode);
        await Json(await Send(client, path + "/relationships", HttpMethod.Post, new { targetRecordId = account["id"]!.GetValue<string>(), label = "works at" }));
        await Json(await Send(client, path + "/notes", HttpMethod.Post, new { body = "First conversation" }));
        await Json(await Send(client, path + "/tags", HttpMethod.Post, new { name = "Priority" }));
        await Json(await Send(client, path + "/addresses", HttpMethod.Post, new { label = "Work", line1 = "1 High Street", city = "London", postalCode = "SW1A 1AA", country = "UK" }));
        await Json(await Send(client, path + "/communications", HttpMethod.Post, new { kind = "email", value = "taylor@example.test", consent = "granted", consentEvidence = "Requested updates during onboarding" }));
        var detail = await Json(await client.GetAsync(path));
        foreach (var collection in new[] { "addresses", "communications", "relationships", "notes", "tags" }) Assert.Single(detail[collection]!.AsArray());
        var archived = await Json(await Send(client, path + "/archive", HttpMethod.Post, new { version = detail["record"]!["version"]!.GetValue<string>() }));
        Assert.True(archived["isArchived"]!.GetValue<bool>());
        Assert.Empty((await Json(await client.GetAsync("/api/v1/crm/records?kind=contact")))["items"]!.AsArray());
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path + "/notes", HttpMethod.Post, new { body = "blocked" })).StatusCode);
        await Json(await Send(client, path + "/restore", HttpMethod.Post, new { version = archived["version"]!.GetValue<string>() }));
        var timeline = await Json(await client.GetAsync(path + "/timeline"));
        Assert.Contains(timeline.AsArray(), a => a!["action"]!.GetValue<string>() == "record.restored");
        Assert.All(timeline.AsArray(), a => Assert.False(string.IsNullOrEmpty(a!["correlationId"]!.GetValue<string>())));
    }

    [Fact]
    public async Task Cross_tenant_reads_writes_ownership_and_relationships_are_blocked()
    {
        using var app = new IdentityApiFactory(); using var primary = await Login(app); using var outside = await Login(app, "outside@nexora.test");
        var row = await Create(primary, "Private Contact"); var id = row["id"]!.GetValue<string>(); var path = "/api/v1/crm/records/" + id;
        Assert.Empty((await Json(await outside.GetAsync("/api/v1/crm/records")))["items"]!.AsArray());
        Assert.Equal(HttpStatusCode.NotFound, (await outside.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outside.GetAsync(path + "/timeline")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(outside, path + "/archive", HttpMethod.Post, new { version = row["version"]!.GetValue<string>() })).StatusCode);
        var outsideSession = await outside.GetFromJsonAsync<SessionResponse>("/api/v1/auth/session");
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(primary, "/api/v1/crm/records", HttpMethod.Post, new { kind = "contact", name = "Bad owner", status = "active", ownerUserId = outsideSession!.UserId })).StatusCode);
        using var scope = app.Services.CreateScope();
        var options = new DbContextOptionsBuilder<CrmDbContext>().UseSqlite(scope.ServiceProvider.GetRequiredService<CrmDbContext>().Database.GetDbConnection()).Options;
        await using var foreignDb = new CrmDbContext(options, new Identity(outsideSession!.TenantId, outsideSession.UserId));
        var foreign = new CrmRecord { TenantId = outsideSession.TenantId, Name = "Outside account", Kind = "account" };
        foreignDb.Add(foreign); await foreignDb.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await Send(primary, path + "/relationships", HttpMethod.Post, new { targetRecordId = foreign.Id, label = "foreign" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await primary.GetAsync("/api/v1/crm/records/" + foreign.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(primary, "/api/v1/crm/records/" + foreign.Id, HttpMethod.Patch, new { kind = "account", name = "Attack", status = "active", version = foreign.Version })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(primary, "/api/v1/crm/records/" + foreign.Id + "/archive", HttpMethod.Post, new { version = foreign.Version })).StatusCode);
    }

    [Fact]
    public async Task Persistence_guards_and_composite_keys_reject_forged_ownership_and_links()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app);
        var row = await Create(client, "Owned"); var session = (await client.GetFromJsonAsync<SessionResponse>("/api/v1/auth/session"))!;
        using var scope = app.Services.CreateScope();
        var options = new DbContextOptionsBuilder<CrmDbContext>().UseSqlite(scope.ServiceProvider.GetRequiredService<CrmDbContext>().Database.GetDbConnection()).Options;
        var otherTenant = Guid.NewGuid(); var otherUser = Guid.NewGuid();
        await using (var db = new CrmDbContext(options, new Identity(otherTenant, otherUser)))
        {
            Assert.Empty(await db.Records.ToListAsync());
            db.Records.Update(new CrmRecord { Id = Guid.Parse(row["id"]!.GetValue<string>()), TenantId = otherTenant, Name = "Spoof" });
            await Assert.ThrowsAsync<CrmBoundaryException>(() => db.SaveChangesAsync());
        }
        await using (var db = new CrmDbContext(options, new Identity(otherTenant, otherUser)))
        {
            var own = new CrmRecord { TenantId = otherTenant, Name = "Own" };
            db.Add(own); await db.SaveChangesAsync();
            db.Add(new RecordRelationship { TenantId = otherTenant, RecordId = own.Id, TargetRecordId = Guid.Parse(row["id"]!.GetValue<string>()), Label = "Injected" });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
        await using (var db = new CrmDbContext(options, new Identity(null, null)))
        {
            Assert.Empty(await db.Records.ToListAsync());
            db.Add(new CrmRecord { TenantId = session.TenantId, Name = "Anonymous" });
            Assert.Throws<CrmBoundaryException>(() => db.SaveChanges());
        }
    }

    [Fact]
    public async Task Custom_fields_saved_views_files_and_validation()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app);
        var row = await Create(client, "Custom"); var id = row["id"]!.GetValue<string>(); var path = "/api/v1/crm/records/" + id;
        var field = await Json(await Send(client, "/api/v1/crm/fields", HttpMethod.Post, new { name = "Score", kind = "contact", dataType = "number" }));
        var fieldPath = path + "/fields/" + field["id"]!.GetValue<string>();
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, fieldPath, HttpMethod.Put, new { textValue = "wrong type" })).StatusCode);
        await Json(await Send(client, fieldPath, HttpMethod.Put, new { numberValue = 12.25 }));
        var view = await Json(await Send(client, "/api/v1/crm/views", HttpMethod.Post, new { name = "My contacts", kind = "contact", search = "Custom", status = "", sort = "name", archived = false, columns = "name,status" }));
        Assert.Single((await Json(await client.GetAsync("/api/v1/crm/views"))).AsArray());
        using var outside = await Login(app, "outside@nexora.test");
        Assert.Empty((await Json(await outside.GetAsync("/api/v1/crm/views"))).AsArray());
        Assert.Equal(HttpStatusCode.NotFound, (await Send(outside, "/api/v1/crm/views/" + view["id"]!.GetValue<string>(), HttpMethod.Delete)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(path + "/notes", new { body = "No CSRF" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path + "/communications", HttpMethod.Post, new { kind = "email", value = "bad", consent = "granted" })).StatusCode);
        var token = await client.GetFromJsonAsync<CsrfResponse>("/api/v1/auth/csrf");
        using var form = new MultipartFormDataContent(); form.Add(new ByteArrayContent("test attachment"u8.ToArray()), "file", "note.txt");
        using var upload = new HttpRequestMessage(HttpMethod.Post, path + "/files") { Content = form }; upload.Headers.Add("X-CSRF-TOKEN", token!.Token);
        var file = await Json(await client.SendAsync(upload));
        var download = await client.GetAsync(path + "/files/" + file["id"]!.GetValue<string>());
        Assert.Equal("test attachment", await download.Content.ReadAsStringAsync());
        Assert.Equal("attachment", download.Content.Headers.ContentDisposition!.DispositionType);
        Assert.Equal(HttpStatusCode.Forbidden, (await outside.GetAsync(path + "/files/" + file["id"]!.GetValue<string>())).StatusCode);
    }

    [Fact]
    public async Task Saved_views_are_private_within_a_tenant_and_audit_is_append_only()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app);
        var session = (await client.GetFromJsonAsync<SessionResponse>("/api/v1/auth/session"))!;
        var view = await Json(await Send(client, "/api/v1/crm/views", HttpMethod.Post, new { name = "Private", kind = "contact", search = "", status = "", sort = "name", archived = false, columns = "name" }));
        await Create(client, "Audit test");
        using var scope = app.Services.CreateScope();
        var options = new DbContextOptionsBuilder<CrmDbContext>().UseSqlite(scope.ServiceProvider.GetRequiredService<CrmDbContext>().Database.GetDbConnection()).Options;
        var anotherUser = Guid.NewGuid();
        await using (var db = new CrmDbContext(options, new Identity(session.TenantId, anotherUser)))
        {
            Assert.Empty(await db.Set<SavedView>().ToListAsync());
            db.Remove(new SavedView { Id = Guid.Parse(view["id"]!.GetValue<string>()), TenantId = session.TenantId, UserId = anotherUser });
            await Assert.ThrowsAsync<CrmBoundaryException>(() => db.SaveChangesAsync());
        }
        await using (var db = new CrmDbContext(options, new Identity(session.TenantId, session.UserId)))
        {
            db.Remove(await db.Set<Activity>().FirstAsync());
            await Assert.ThrowsAsync<CrmBoundaryException>(() => db.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task Lists_search_filter_sort_and_paginate_stably()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app);
        await Create(client, "Zed"); await Create(client, "Alpha"); await Create(client, "Other", "account");
        var page = await Json(await client.GetAsync("/api/v1/crm/records?kind=contact&pageSize=1&sort=name"));
        Assert.Equal(2, page["total"]!.GetValue<int>());
        Assert.Equal("Alpha", page["items"]![0]!["name"]!.GetValue<string>());
        var next = await Json(await client.GetAsync("/api/v1/crm/records?kind=contact&pageSize=1&page=2&sort=name"));
        Assert.Equal("Zed", next["items"]![0]!["name"]!.GetValue<string>());
        Assert.Single((await Json(await client.GetAsync("/api/v1/crm/records?search=Al&status=active")))["items"]!.AsArray());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/crm/records?sort=untrusted")).StatusCode);
    }
}
