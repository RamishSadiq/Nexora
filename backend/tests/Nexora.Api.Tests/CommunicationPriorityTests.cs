using System.Net;

namespace Nexora.Api.Tests;

public sealed partial class CrmEndpointTests
{
    [Fact]
    public async Task Communication_designations_edit_with_versions_preserve_consent_and_reject_duplicate_slots()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var outside = await Login(app, "outside@nexora.test");
        var contact = await Create(staff, "Communication fields"); var path = $"/api/v1/crm/records/{contact["id"]!.GetValue<string>()}";
        var legacy = await Json(await Send(staff, path + "/communications", HttpMethod.Post, new { kind = "email", value = "old@example.test", consent = "unknown" }));
        var detail = await Json(await staff.GetAsync(path));
        Assert.Equal("other", detail["communications"]![0]!["priority"]!.GetValue<string>());
        var version = detail["record"]!["version"]!.GetValue<string>();
        var primary = await Json(await Send(staff, path + "/communications", HttpMethod.Post, new { kind = "email", value = "primary@example.test", priority = "primary", consent = "granted", consentEvidence = "Requested updates", version }));
        var export = await staff.GetAsync("/api/v1/crm/export?kind=contact");
        Assert.Equal(HttpStatusCode.OK, export.StatusCode);
        var csv = await export.Content.ReadAsStringAsync(); Assert.Contains("primary@example.test", csv); Assert.DoesNotContain("old@example.test", csv);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(staff, path + "/communications", HttpMethod.Post, new { kind = "fax", value = "020 1234 1234", consent = "unknown", version })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, path + "/communications", HttpMethod.Post, new { kind = "email", value = "duplicate@example.test", priority = "primary", consent = "unknown" })).StatusCode);
        await Json(await Send(staff, path + "/communications", HttpMethod.Post, new { kind = "email", value = "secondary@example.test", priority = "secondary", consent = "unknown" }));
        foreach (var kind in new[] { "phone", "mobile", "fax" })
            await Json(await Send(staff, path + "/communications", HttpMethod.Post, new { kind, value = "+44 20 1234 5678", priority = "primary", consent = "unknown" }));
        detail = await Json(await staff.GetAsync(path)); version = detail["record"]!["version"]!.GetValue<string>();
        var primaryPath = path + "/communications/" + primary["id"]!.GetValue<string>();
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, primaryPath, HttpMethod.Put, new { version, kind = "email", value = "invalid", priority = "primary", consent = "unknown" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, primaryPath, HttpMethod.Put, new { version, kind = "email", value = "primary@example.test", priority = "secondary", consent = "unknown" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, primaryPath, HttpMethod.Put, new { version, kind = "email", value = "primary@example.test", priority = "other", consent = "denied" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, primaryPath, HttpMethod.Put, new { version, kind = "email", value = "primary@example.test", priority = "invalid", consent = "unknown" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(outside, primaryPath, HttpMethod.Put, new { version, kind = "email", value = "outside@example.test", priority = "other", consent = "unknown" })).StatusCode);
        await Json(await Send(staff, primaryPath, HttpMethod.Put, new { version, kind = "email", value = "updated@example.test", priority = "other", consent = "denied", consentEvidence = "Opted out" }));
        Assert.Equal(HttpStatusCode.Conflict, (await Send(staff, primaryPath, HttpMethod.Put, new { version, kind = "email", value = "stale@example.test", priority = "primary", consent = "unknown" })).StatusCode);
        detail = await Json(await staff.GetAsync(path)); version = detail["record"]!["version"]!.GetValue<string>();
        var legacyPath = path + "/communications/" + legacy["id"]!.GetValue<string>();
        await Json(await Send(staff, legacyPath, HttpMethod.Put, new { version, kind = "email", value = "old@example.test", priority = "primary", consent = "unknown" }));
        detail = await Json(await staff.GetAsync(path));
        var updated = detail["communications"]!.AsArray().Single(x => x!["id"]!.GetValue<string>() == primary["id"]!.GetValue<string>())!;
        Assert.Equal("updated@example.test", updated["value"]!.GetValue<string>());
        Assert.Equal("denied", updated["consent"]!.GetValue<string>()); Assert.Equal("Opted out", updated["consentEvidence"]!.GetValue<string>());
        Assert.Equal(6, detail["communications"]!.AsArray().Count);
        Assert.Contains("communication.updated", (await Json(await staff.GetAsync(path + "/timeline"))).ToJsonString());
        var otherContact = await Create(staff, "Other communication parent");
        Assert.Equal(HttpStatusCode.NotFound, (await Send(staff, $"/api/v1/crm/records/{otherContact["id"]!.GetValue<string>()}/communications/{primary["id"]!.GetValue<string>()}", HttpMethod.Put, new { version = otherContact["version"]!.GetValue<string>(), kind = "email", value = "wrong@example.test", priority = "other", consent = "unknown" })).StatusCode);
        await Json(await Send(staff, path + "/archive", HttpMethod.Post, new { version = detail["record"]!["version"]!.GetValue<string>() }));
        detail = await Json(await staff.GetAsync(path));
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, primaryPath, HttpMethod.Put, new { version = detail["record"]!["version"]!.GetValue<string>(), kind = "email", value = "archived@example.test", priority = "other", consent = "unknown" })).StatusCode);
    }
}
