using System.Net;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.Modules.Identity.Domain;
using Nexora.Modules.Identity.Infrastructure.Persistence;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;

public sealed class MembershipClassificationTests
{
    private static async Task<(JsonNode Contact, JsonNode Member)> CreateMember(HttpClient client)
    {
        var contact = await Contact(client);
        var product = await Json(await Send(client, "/api/v1/membership/products", HttpMethod.Post, new { name = "Classifications", category = "Product category", termMonths = 12, rate = 125, currency = "GBP", isActive = true }));
        var application = await Json(await Send(client, "/api/v1/membership/applications", HttpMethod.Post, new { contactId = Id(contact), productId = Id(product), applicationType = "individual", termsAccepted = true, startsAtUtc = DateTime.UtcNow.Date }));
        var approved = await Json(await Send(client, $"/api/v1/membership/applications/{Id(application)}/decision", HttpMethod.Post, new { version = Version(application), decision = "approve", reason = "Eligible" }));
        return (contact, (await Json(await client.GetAsync($"/api/v1/membership/members/{approved["membershipId"]!.GetValue<string>()}")))["member"]!);
    }
    private static async Task<JsonNode> Lookup(HttpClient client, string kind, string name) => await Json(await Send(client, $"/api/v1/membership/setup/{kind}", HttpMethod.Post, new { name, description = "" }));

    [Fact]
    public async Task Membership_classifications_preserve_effective_history_support_same_day_correction_and_clear()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app); var (contact, member) = await CreateMember(client);
        var category = await Lookup(client, "member-category", "Gold"); var second = await Lookup(client, "member-category", "Silver"); var band = await Lookup(client, "member-subscription-band", "Band A");
        var path = $"/api/v1/membership/members/{Id(member)}"; var initial = await Json(await client.GetAsync(path + "/classifications"));
        Assert.Equal(0, initial["total"]!.GetValue<int>()); Assert.Null(initial["current"]!["category"]!["effectiveOn"]);
        var changed = await Json(await Send(client, path + "/classifications/category", HttpMethod.Post, new { version = Version(initial), definitionId = Id(category), effectiveOn = "2001-02-03", reason = "Historical category" }));
        var saved = await Json(await client.GetAsync(path + "/classifications")); Assert.Equal("Gold", saved["current"]!["category"]!["name"]!.GetValue<string>()); Assert.Equal("2001-02-03", saved["current"]!["category"]!["effectiveOn"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, path + "/classifications/category", HttpMethod.Post, new { version = Version(initial), definitionId = Id(second), effectiveOn = "2001-02-03", reason = "Stale" })).StatusCode);
        foreach (var invalid in new object[] { new { definitionId = Id(band), effectiveOn = "2001-02-04", reason = "Wrong list" }, new { definitionId = Guid.NewGuid(), effectiveOn = "2001-02-04", reason = "Missing" }, new { definitionId = Id(second), effectiveOn = "2001-02-02", reason = "Earlier" }, new { definitionId = Id(second), effectiveOn = "0001-01-01", reason = "Invalid" }, new { definitionId = Id(second), effectiveOn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)), reason = "Future" }, new { definitionId = Id(second), effectiveOn = "2001-02-04", reason = "" }, new { definitionId = Id(second), effectiveOn = "2001-02-04", reason = "Bad\u0001" }, new { definitionId = Id(second), effectiveOn = "2001-02-04", reason = new string('a', 501) } })
        {
            var payload = JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(invalid))!.AsObject(); payload["version"] = Version(changed);
            Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path + "/classifications/category", HttpMethod.Post, payload)).StatusCode);
        }
        await Json(await Send(client, $"/api/v1/membership/setup/member-category/{Id(category)}", HttpMethod.Patch, new { name = "Renamed", description = "", version = Version(category) }));
        saved = await Json(await client.GetAsync(path + "/classifications")); Assert.Equal("Gold", saved["current"]!["category"]!["name"]!.GetValue<string>());
        changed = await Json(await Send(client, path + "/classifications/category", HttpMethod.Post, new { version = Version(changed), definitionId = Id(second), effectiveOn = "2001-02-03", reason = "Same-day correction" }));
        changed = await Json(await Send(client, path + "/classifications/subscription-band", HttpMethod.Post, new { version = Version(changed), definitionId = Id(band), effectiveOn = "2001-02-03", reason = "Band assignment" }));
        var projected = (await Json(await client.GetAsync($"/api/v1/crm/records/{Id(contact)}/memberships")))["items"]![0]!;
        Assert.Equal("Silver", projected["classifications"]!["category"]!["name"]!.GetValue<string>()); Assert.Equal("Band A", projected["classifications"]!["subscriptionBand"]!["name"]!.GetValue<string>()); Assert.Equal("Product category", projected["productCategory"]!.GetValue<string>()); Assert.Equal(125, projected["rate"]!.GetValue<decimal>());
        changed = await Json(await Send(client, path + "/classifications/category", HttpMethod.Post, new { version = Version(changed), definitionId = (string?)null, effectiveOn = "2001-02-03", reason = "Clear category" }));
        saved = await Json(await client.GetAsync(path + "/classifications")); Assert.Null(saved["current"]!["category"]!["name"]); Assert.Equal("2001-02-03", saved["current"]!["category"]!["effectiveOn"]!.GetValue<string>()); Assert.Equal(4, saved["total"]!.GetValue<int>());
        Assert.Contains(saved["items"]!.AsArray(), x => x!["nameSnapshot"]?.GetValue<string>() == "Gold");
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path + "/classifications/category", HttpMethod.Post, new { version = Version(changed), effectiveOn = "2001-02-03", reason = "No change" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(path + "/classifications?page=bad")).StatusCode); Assert.Empty((await Json(await client.GetAsync(path + "/classifications?page=2")))["items"]!.AsArray());
        var current = (await Json(await client.GetAsync(path)))["member"]!; foreach (var field in new[] { "status", "startsAtUtc", "endsAtUtc", "rate", "productId", "applicationId" }) Assert.Equal(member[field]!.ToJsonString(), current[field]!.ToJsonString());
        var cancelled = await Json(await Send(client, path + "/cancel", HttpMethod.Post, new { version = Version(current), reason = "Cancelled" }));
        changed = await Json(await Send(client, path + "/classifications/subscription-band", HttpMethod.Post, new { version = Version(cancelled), effectiveOn = "2001-02-04", reason = "Clear band" }));
        Assert.Equal("cancelled", (await Json(await client.GetAsync(path)))["member"]!["status"]!.GetValue<string>());
        projected = (await Json(await client.GetAsync($"/api/v1/crm/records/{Id(contact)}/memberships")))["items"]![0]!; Assert.Equal("membership.cancel", projected["latestLifecycleAction"]!.GetValue<string>()); Assert.Null(projected["classifications"]!["subscriptionBand"]!["name"]);
    }

    [Fact]
    public async Task Classification_history_respects_tenant_and_manage_permissions()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app); using var outside = await Login(app, "outside@nexora.test"); var (_, member) = await CreateMember(client); var category = await Lookup(client, "member-category", "Gold");
        var path = $"/api/v1/membership/members/{Id(member)}/classifications";
        Assert.Equal(HttpStatusCode.Forbidden, (await outside.GetAsync(path)).StatusCode);
        using var anonymous = app.CreateClient(); Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(path)).StatusCode);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NexoraIdentityDbContext>(); using var system = db.BeginSystemAccess(); var role = await db.Roles.SingleAsync(x => x.Name == "Outside Administrator");
            db.RolePermissions.Add(new RolePermission { RoleId = role.Id, Permission = "membership.read" }); await db.SaveChangesAsync();
        }
        using var reader = await Login(app, "outside@nexora.test"); Assert.Equal(HttpStatusCode.NotFound, (await reader.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(reader, path + "/category", HttpMethod.Post, new { version = Version(member), definitionId = Id(category), effectiveOn = "2001-02-03", reason = "Denied" })).StatusCode);
    }
}
