using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Persistence;
using Nexora.Modules.Identity.Domain;
using Nexora.Modules.Identity.Infrastructure.Persistence;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;

public sealed class AutomationTests
{
    [Theory]
    [InlineData("crm", "contact", "records")]
    [InlineData("crm", "account", "records")]
    [InlineData("membership", "MembershipProduct", "products")]
    [InlineData("events", "Offering", "offerings")]
    [InlineData("finance", "CatalogueProduct", "products")]
    [InlineData("engagement", "Community", "communities")]
    [InlineData("work", "WorkItem", "items")]
    public async Task Rules_execute_on_create_and_attribute_updates_with_chaining_and_clear(string module, string kind, string collection)
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app);
        var root = $"/api/v1/{module}";
        var fields = module == "crm" ? root + "/fields" : root + $"/attributes/{kind}/definitions";
        async Task<string> Field(string name, string dataType) => Id(await Json(await Send(client, fields, HttpMethod.Post, new { name, dataType, kind })));
        var source = await Field("Amount", "number"); var result = await Field("Total", "number");
        var chained = await Field("Adjusted", "number"); var status = await Field("Label", "text");
        var configPath = root + $"/automations/{kind}";
        var sourceName = module == "work" ? "record:Title" : "record:Name";
        // Deliberately save downstream before upstream: execution must sort dependencies.
        var rules = new[] { new AutomationRule(result, "subtract", chained, "1"), new AutomationRule(source, "add", result, "5.25"), new AutomationRule(sourceName, "copy", status) };
        await Json(await Send(client, configPath, HttpMethod.Put, new { version = (string?)null, rules }));
        var body = Body(module, kind); body["attributes"] = new Dictionary<string, object> { [source] = new { numberValue = 10m } };
        var record = await Json(await Send(client, root + "/" + collection, HttpMethod.Post, body), module == "crm" ? HttpStatusCode.Created : HttpStatusCode.OK);
        var id = Id(record);
        async Task<JsonArray> Values() => module == "crm" ? (await Json(await client.GetAsync(root + "/records/" + id)))["fields"]!.AsArray() : (await Json(await client.GetAsync(root + $"/attributes/{kind}/records/{id}"))).AsArray();
        var values = await Values();
        Assert.Equal(15.25m, Value(values, result)["numberValue"]!.GetValue<decimal>());
        Assert.Equal(14.25m, Value(values, chained)["numberValue"]!.GetValue<decimal>());
        Assert.Equal("Automation record", Value(values, status)["textValue"]!.GetValue<string>());
        async Task Update(decimal? number) {
            var response = module == "crm"
                ? await Send(client, root + $"/records/{id}/fields/{source}", HttpMethod.Put, new { numberValue = number })
                : await Send(client, root + $"/attributes/{kind}/records/{id}", HttpMethod.Put, new { attributes = new Dictionary<string, object> { [source] = new { numberValue = number } } });
            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        }
        await Update(20m);
        Assert.Equal(24.25m, Value(await Values(), chained)["numberValue"]!.GetValue<decimal>());
        await Update(null);
        Assert.Null(Value(await Values(), result)["numberValue"]);
        Assert.Null(Value(await Values(), chained)["numberValue"]);
        // Referenced definitions cannot silently invalidate live rules.
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, fields + "/" + source, HttpMethod.Delete)).StatusCode);
        var current = await Json(await client.GetAsync(configPath));
        await Json(await Send(client, configPath, HttpMethod.Put, new { version = Version(current), rules = Array.Empty<AutomationRule>() }));
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, fields + "/" + source, HttpMethod.Delete)).StatusCode);
    }

    [Fact]
    public async Task Configuration_persists_across_clients_enforces_permissions_tenants_versions_and_csrf()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app);
        const string path = "/api/v1/crm/automations/contact";
        var initial = await Json(await client.GetAsync(path));
        var sections = initial["sections"]!.AsArray().Select(x => x!.GetValue<string>()).Reverse().ToArray();
        var saved = await Json(await Send(client, path, HttpMethod.Put, new { version = (string?)null, sections }));
        using var second = await Login(app);
        Assert.Equal(sections, (await Json(await second.GetAsync(path)))["sections"]!.AsArray().Select(x => x!.GetValue<string>()));
        Assert.Equal(HttpStatusCode.Conflict, (await Send(second, path, HttpMethod.Put, new { version = (string?)null, sections })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync(path, new { version = Version(saved), sections })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path, HttpMethod.Put, new { version = Version(saved), sections = new[] { "Made up section" } })).StatusCode);
        using var outside = await Login(app, "outside@nexora.test");
        var foreign = await Json(await outside.GetAsync(path)); Assert.Null(foreign["version"]); Assert.False(foreign["canManage"]!.GetValue<bool>());
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(outside, path, HttpMethod.Put, new { sections })).StatusCode);
        using var anonymous = app.CreateClient(); Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(path)).StatusCode);
        // Give the other tenant management permission; it still cannot reference our fields or overwrite our settings.
        using (var scope = app.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<NexoraIdentityDbContext>(); using var system = db.BeginSystemAccess();
            var role = await db.Roles.SingleAsync(x => x.Name == "Outside Administrator"); db.RolePermissions.Add(new RolePermission { RoleId = role.Id, Permission = "crm.manage" }); await db.SaveChangesAsync();
        }
        using var foreignManager = await Login(app, "outside@nexora.test");
        var field = Id(await Json(await Send(client, "/api/v1/crm/fields", HttpMethod.Post, new { name = "Local label", dataType = "text", kind = "contact" })));
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(foreignManager, path, HttpMethod.Put, new { rules = new[] { new AutomationRule("record:Name", "copy", field) } })).StatusCode);
        await Json(await Send(foreignManager, path, HttpMethod.Put, new { sections = sections.Reverse().ToArray() }));
        Assert.Equal(Version(saved), Version(await Json(await client.GetAsync(path))));
    }

    [Fact]
    public async Task Invalid_rules_and_overflow_reject_without_partial_record_writes()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app);
        const string fields = "/api/v1/work/attributes/WorkItem/definitions";
        var a = Id(await Json(await Send(client, fields, HttpMethod.Post, new { name = "A", dataType = "number" })));
        var b = Id(await Json(await Send(client, fields, HttpMethod.Post, new { name = "B", dataType = "number" })));
        const string path = "/api/v1/work/automations/WorkItem";
        foreach (var rules in new[] {
            new[] { new AutomationRule(a, "copy", b), new AutomationRule(b, "copy", a) },
            new[] { new AutomationRule(a, "copy", b), new AutomationRule(a, "add", b, "1") },
            new[] { new AutomationRule("record:Title", "age", b) },
            new[] { new AutomationRule(a, "copy", "record:TenantId") },
            new[] { new AutomationRule(a, "add", b, "invalid") }
        }) Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path, HttpMethod.Put, new { rules })).StatusCode);
        await Json(await Send(client, path, HttpMethod.Put, new { rules = new[] { new AutomationRule(a, "add", b, "99999999999999") } }));
        var body = Body("work", "WorkItem"); body["attributes"] = new Dictionary<string, object> { [a] = new { numberValue = 10m } };
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, "/api/v1/work/items", HttpMethod.Post, body)).StatusCode);
        Assert.Empty((await Json(await client.GetAsync("/api/v1/work/items"))).AsArray());
    }

    [Fact]
    public void Typed_functions_handle_birthdays_leap_years_comparisons_and_unmatched_values()
    {
        var age = new AutomationRule("dob", "age", "age");
        Assert.Equal(25m, Automations.Evaluate(age, new DateTime(2000, 9, 16), "date", new DateTime(2026, 9, 15)));
        Assert.Equal(26m, Automations.Evaluate(age, new DateTime(2000, 9, 15), "date", new DateTime(2026, 9, 15)));
        Assert.Equal(25m, Automations.Evaluate(age, new DateTime(2000, 2, 29), "date", new DateTime(2025, 2, 28)));
        Assert.Null(Automations.Evaluate(age, new DateTime(2030, 1, 1), "date", new DateTime(2026, 9, 15)));
        Assert.Equal(true, Automations.Evaluate(new("", "equals", "", "12.50"), 12.5m, "number", DateTime.Today));
        Assert.Equal(false, Automations.Evaluate(new("", "not-equal", "", "false"), false, "boolean", DateTime.Today));
        Assert.Equal(true, Automations.Evaluate(new("", "contains", "", "MEMBER"), "Paid member", "text", DateTime.Today));
        Assert.Equal("Current", Automations.Evaluate(new("", "set-when-equal", "", "active", "Current"), "active", "text", DateTime.Today));
        Assert.Null(Automations.Evaluate(new("", "set-when-equal", "", "active", "Current"), "cancelled", "text", DateTime.Today));
    }

    private static JsonNode Value(JsonArray rows, string id) => rows.Single(x => x!["definitionId"]!.GetValue<string>() == id)!;
    private static Dictionary<string, object> Body(string module, string kind) => module switch {
        "crm" => new() { ["name"] = "Automation record", ["kind"] = kind, ["status"] = "active" },
        "membership" => new() { ["name"] = "Automation record", ["category"] = "Standard", ["termMonths"] = 12, ["rate"] = 10m, ["currency"] = "GBP", ["isActive"] = true },
        "events" => new() { ["kind"] = "event", ["name"] = "Automation record", ["venue"] = "Online", ["startsAtUtc"] = "2026-12-01T10:00:00Z", ["endsAtUtc"] = "2026-12-01T12:00:00Z", ["capacity"] = 10 },
        "finance" => new() { ["sku"] = "AUTO", ["name"] = "Automation record", ["unitPrice"] = 10m, ["currency"] = "GBP", ["vatBasisPoints"] = 0 },
        "engagement" => new() { ["kind"] = "group", ["name"] = "Automation record", ["purpose"] = "Testing" },
        _ => new() { ["title"] = "Automation record", ["status"] = "open", ["priority"] = "normal" }
    };
}
