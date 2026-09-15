using System.Net;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.Modules.Identity.Domain;
using Nexora.Modules.Identity.Infrastructure.Persistence;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;

public sealed class EntityAttributeTests
{
    [Theory]
    [InlineData("membership", "MembershipProduct", "products")]
    [InlineData("events", "Offering", "offerings")]
    [InlineData("finance", "CatalogueProduct", "products")]
    [InlineData("engagement", "Community", "communities")]
    [InlineData("work", "WorkItem", "items")]
    public async Task Typed_attributes_CRUD_order_isolation_and_atomic_create(string module, string entity, string collection)
    {
        using var app = new IdentityApiFactory();
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NexoraIdentityDbContext>();
            using var system = db.BeginSystemAccess();
            var role = await db.Roles.SingleAsync(x => x.Name == "Outside Administrator");
            db.RolePermissions.AddRange(new RolePermission { RoleId = role.Id, Permission = module + ".read" }, new RolePermission { RoleId = role.Id, Permission = module + ".manage" });
            await db.SaveChangesAsync();
        }
        using var client = await Login(app);
        using var outside = await Login(app, "outside@nexora.test");
        var root = $"/api/v1/{module}/attributes/{entity}";
        var ids = new List<string>();
        foreach (var type in new[] { "text", "number", "boolean", "date" })
            ids.Add(Id(await Json(await Send(client, root + "/definitions", HttpMethod.Post, new { name = type + " attribute", dataType = type }))));
        var input = new Dictionary<string, object> { [ids[0]] = new { textValue = "Saved text" }, [ids[1]] = new { numberValue = 12.3456m }, [ids[2]] = new { booleanValue = false }, [ids[3]] = new { dateValue = "2026-09-15T00:00:00Z" } };
        var body = Body(module); body["attributes"] = input;
        var record = await Json(await Send(client, $"/api/v1/{module}/{collection}", HttpMethod.Post, body));
        var id = Id(record);
        var saved = (await Json(await client.GetAsync(root + "/records/" + id))).AsArray();
        Assert.Equal(4, saved.Count);
        Assert.Equal(12.3456m, saved.Single(v => v!["definitionId"]!.GetValue<string>() == ids[1])!["numberValue"]!.GetValue<decimal>());
        Assert.False(saved.Single(v => v!["definitionId"]!.GetValue<string>() == ids[2])!["booleanValue"]!.GetValue<bool>());
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, root + "/order", HttpMethod.Put, new { ids = ids.AsEnumerable().Reverse().ToArray() })).StatusCode);
        Assert.Equal(ids[3], Id((await Json(await client.GetAsync(root + "/definitions")))[0]!));
        await Json(await Send(client, root + "/definitions/" + ids[0], HttpMethod.Patch, new { name = "Renamed", dataType = "text" }));
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, root + "/definitions/" + ids[0], HttpMethod.Patch, new { name = "Renamed", dataType = "number" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, root + "/order", HttpMethod.Put, new { ids = new[] { ids[0], ids[0] } })).StatusCode);
        Assert.Empty((await Json(await outside.GetAsync(root + "/definitions"))).AsArray());
        Assert.Empty((await Json(await outside.GetAsync(root + "/records"))).AsArray());
        Assert.Equal(HttpStatusCode.NotFound, (await outside.GetAsync(root + "/records/" + id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(outside, root + "/definitions/" + ids[0], HttpMethod.Delete)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(outside, root + "/definitions/" + ids[0], HttpMethod.Patch, new { name = "Attack", dataType = "text" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(outside, root + "/records/" + id, HttpMethod.Put, new { attributes = input })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(outside, root + "/order", HttpMethod.Put, new { ids })).StatusCode);
        var invalid = Body(module); invalid["attributes"] = new Dictionary<string, object> { [Guid.NewGuid().ToString()] = new { textValue = "bad" } };
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, $"/api/v1/{module}/{collection}", HttpMethod.Post, invalid)).StatusCode);
        Assert.Single((await Json(await client.GetAsync(root + "/records"))).AsArray());
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, root + "/records/" + id, HttpMethod.Put, new { attributes = new Dictionary<string, object> { [ids[1]] = new { numberValue = 99.1234m } } })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, root + "/definitions/" + ids[1], HttpMethod.Delete)).StatusCode);
        Assert.Equal(3, (await Json(await client.GetAsync(root + "/records/" + id))).AsArray().Count);
    }

    private static Dictionary<string, object> Body(string module) => module switch
    {
        "membership" => new() { ["name"] = "Test product", ["category"] = "Standard", ["termMonths"] = 12, ["rate"] = 10m, ["currency"] = "GBP", ["isActive"] = true },
        "events" => new() { ["kind"] = "event", ["name"] = "Test event", ["venue"] = "Online", ["startsAtUtc"] = "2026-12-01T10:00:00Z", ["endsAtUtc"] = "2026-12-01T12:00:00Z", ["capacity"] = 10, ["passingScore"] = 50 },
        "finance" => new() { ["sku"] = Guid.NewGuid().ToString(), ["name"] = "Test product", ["unitPrice"] = 10m, ["currency"] = "GBP", ["vatBasisPoints"] = 0 },
        "engagement" => new() { ["kind"] = "group", ["name"] = "Test group", ["purpose"] = "Testing attributes" },
        _ => new() { ["title"] = "Test task", ["description"] = "Testing attributes", ["status"] = "open", ["priority"] = "normal" },
    };
}
