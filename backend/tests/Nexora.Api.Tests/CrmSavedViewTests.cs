using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Nexora.Modules.Identity;
using Nexora.Modules.Identity.Domain;
using Nexora.Modules.Identity.Infrastructure.Persistence;

namespace Nexora.Api.Tests;

public sealed partial class CrmEndpointTests
{
    [Fact]
    public async Task Saved_views_round_trip_custom_columns_filters_and_preserve_kind()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app);
        var field = await Json(await Send(client, "/api/v1/crm/fields", HttpMethod.Post, new { name = "Rating", kind = "contact", dataType = "number" }));
        var columns = "name,status,category,updatedAtUtc,field:" + field["id"]!.GetValue<string>();
        var owner = (await client.GetFromJsonAsync<SessionResponse>("/api/v1/auth/session"))!.UserId;
        object Payload(string name, string kind = "contact", string? selected = null) => new { name, kind, search = "Ada", status = "active", sort = "-name", archived = true, columns = selected ?? columns, ownerUserId = owner, category = "Member" };
        var saved = await Json(await Send(client, "/api/v1/crm/views", HttpMethod.Post, Payload("Members")));
        var path = "/api/v1/crm/views/" + saved["id"]!.GetValue<string>();
        var loaded = await Json(await client.GetAsync(path));
        Assert.Equal(columns, loaded["columns"]!.GetValue<string>());
        Assert.Equal(owner, loaded["ownerUserId"]!.GetValue<Guid>());
        Assert.Equal("Member", loaded["category"]!.GetValue<string>());
        Assert.True(loaded["archived"]!.GetValue<bool>());
        Assert.Single((await Json(await client.GetAsync("/api/v1/crm/views?kind=contact"))).AsArray());
        Assert.Empty((await Json(await client.GetAsync("/api/v1/crm/views?kind=account"))).AsArray());
        Assert.Equal("Renamed", (await Json(await Send(client, path, HttpMethod.Put, Payload("Renamed"))))["name"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path, HttpMethod.Put, Payload("Wrong kind", "account", "name"))).StatusCode);
        foreach (var invalid in new[] { "status", "name,name", "name,field:123", "name,field:" + Guid.NewGuid() })
            Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, "/api/v1/crm/views", HttpMethod.Post, Payload("Bad", selected: invalid))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, "/api/v1/crm/fields/" + field["id"]!.GetValue<string>(), HttpMethod.Delete)).StatusCode);
        // Existing views remain readable; clients remove unavailable columns before updating.
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
        await Json(await Send(client, path, HttpMethod.Put, Payload("Recovered", selected: "name")));
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, path, HttpMethod.Delete)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task Saved_view_routes_deny_other_users_and_tenants()
    {
        using var app = new IdentityApiFactory(); using var owner = await Login(app);
        var session = (await owner.GetFromJsonAsync<SessionResponse>("/api/v1/auth/session"))!;
        using (var scope = app.Services.CreateScope())
        {
            using var bootstrap = scope.ServiceProvider.GetRequiredService<NexoraIdentityDbContext>().BeginSystemAccess();
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<NexoraUser>>();
            var original = (await manager.FindByEmailAsync(IdentityApiFactory.AdminEmail))!;
            var peer = new NexoraUser { TenantId = session.TenantId, UserName = "peer@nexora.test", Email = "peer@nexora.test", DisplayName = "Peer", EmailConfirmed = true };
            Assert.True((await manager.CreateAsync(peer, IdentityApiFactory.AdminPassword)).Succeeded);
            Assert.True((await manager.AddToRolesAsync(peer, await manager.GetRolesAsync(original))).Succeeded);
        }
        var payload = new { name = "Private", kind = "contact", search = "", status = "", sort = "name", archived = false, columns = "name" };
        var view = await Json(await Send(owner, "/api/v1/crm/views", HttpMethod.Post, payload));
        var path = "/api/v1/crm/views/" + view["id"]!.GetValue<string>();
        foreach (var email in new[] { "peer@nexora.test", "outside@nexora.test" })
        {
            using var other = await Login(app, email);
            Assert.Empty((await Json(await other.GetAsync("/api/v1/crm/views"))).AsArray());
            Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await Send(other, path, HttpMethod.Put, payload)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await Send(other, path, HttpMethod.Delete)).StatusCode);
        }
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task Listing_combines_owner_team_category_and_status_before_paging()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app);
        var owner = (await client.GetFromJsonAsync<SessionResponse>("/api/v1/auth/session"))!.UserId;
        var directory = await Json(await client.GetAsync("/api/v1/crm/directory"));
        var team = directory["teams"]![0]!["id"]!.GetValue<string>();
        for (var i = 0; i < 4; i++)
            await Json(await Send(client, "/api/v1/crm/records", HttpMethod.Post, new { kind = "contact", name = $"Member {i}", status = "active", category = i == 3 ? "Other" : "Member", ownerUserId = owner, ownerTeamId = team }), HttpStatusCode.Created);
        await Create(client, "Unowned");
        Assert.Equal(5, (await Json(await client.GetAsync("/api/v1/crm/records?ownerUserId=&ownerTeamId=")))["total"]!.GetValue<int>());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/crm/records?ownerUserId=invalid")).StatusCode);
        var url = $"/api/v1/crm/records?kind=contact&status=active&category=Member&ownerUserId={owner}&ownerTeamId={team}&pageSize=2";
        var first = await Json(await client.GetAsync(url));
        Assert.Equal(3, first["total"]!.GetValue<int>()); Assert.Equal(2, first["items"]!.AsArray().Count);
        var second = await Json(await client.GetAsync(url + "&page=2"));
        Assert.Single(second["items"]!.AsArray());
        Assert.Equal("Member 2", second["items"]![0]!["name"]!.GetValue<string>());
        Assert.Equal(0, (await Json(await client.GetAsync(url.Replace(owner.ToString(), Guid.NewGuid().ToString()))))["total"]!.GetValue<int>());
    }
}
