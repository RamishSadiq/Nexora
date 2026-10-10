using System.Net;
using System.Text.Json.Nodes;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Contracts;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;

public sealed partial class PortalEndpointTests
{
    [Fact]
    public async Task Managed_profiles_preserve_slots_support_swaps_and_keep_tags_and_portal_separate()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var member = app.CreateClient();
        var (_, contact) = await Grant(app, staff); await PortalLogin(member);
        var path = $"/api/v1/crm/records/{Id(contact)}";
        var a = await Json(await Send(staff, "/api/v1/crm/profile-definitions", HttpMethod.Post, new { parentNode = " Expertise ", childNode = " Engineering " }), HttpStatusCode.Created);
        var b = await Json(await Send(staff, "/api/v1/crm/profile-definitions", HttpMethod.Post, new { parentNode = "Interest", childNode = "Engineering" }), HttpStatusCode.Created);
        Assert.Equal("Expertise", a["parentNode"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, "/api/v1/crm/profile-definitions", HttpMethod.Post, new { parentNode = "expertise", childNode = "ENGINEERING" })).StatusCode);
        foreach (var invalid in new[] { new { parentNode = "", childNode = "Missing" }, new { parentNode = "Bad\u0001", childNode = "Control" }, new { parentNode = new string('x', 161), childNode = "Long" } })
            Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, "/api/v1/crm/profile-definitions", HttpMethod.Post, invalid)).StatusCode);
        await Json(await Send(staff, path + "/tags", HttpMethod.Post, new { name = "legacy" }));
        var initial = await Json(await staff.GetAsync(path));
        var slots = new[] { new { slot = 1, definitionId = Id(a) }, new { slot = 5, definitionId = Id(b) } };
        await Json(await Send(staff, path + "/profiles", HttpMethod.Put, new { version = Version(initial["record"]!), assignments = slots }));
        var saved = await Json(await staff.GetAsync(path));
        Assert.Equal(new[] { 1, 5 }, saved["profiles"]!.AsArray().Select(x => x!["slot"]!.GetValue<int>())); Assert.Single(saved["tags"]!.AsArray());
        Assert.Equal(HttpStatusCode.Conflict, (await Send(staff, path + "/profiles", HttpMethod.Put, new { version = Version(initial["record"]!), assignments = slots })).StatusCode);
        foreach (var invalid in new[] { new[] { new { slot = 0, definitionId = Id(a) } }, new[] { new { slot = 6, definitionId = Id(a) } }, new[] { new { slot = 1, definitionId = Id(a) }, new { slot = 1, definitionId = Id(b) } }, new[] { new { slot = 1, definitionId = Id(a) }, new { slot = 2, definitionId = Id(a) } }, new[] { new { slot = 1, definitionId = Guid.NewGuid().ToString() } } })
            Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, path + "/profiles", HttpMethod.Put, new { version = Version(saved["record"]!), assignments = invalid })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, path + "/profiles", HttpMethod.Put, new { version = Version(saved["record"]!) })).StatusCode);
        await Json(await Send(staff, path + "/profiles", HttpMethod.Put, new { version = Version(saved["record"]!), assignments = new[] { new { slot = 1, definitionId = Id(b) }, new { slot = 5, definitionId = Id(a) } } }));
        saved = await Json(await staff.GetAsync(path)); Assert.Equal(Id(b), saved["profiles"]![0]!["definitionId"]!.GetValue<string>());
        var archived = await Json(await Send(staff, $"/api/v1/crm/profile-definitions/{Id(a)}", HttpMethod.Put, new { parentNode = "Expertise", childNode = "Technical specialism", version = Version(a), isArchived = true }));
        Assert.Equal(HttpStatusCode.Conflict, (await Send(staff, $"/api/v1/crm/profile-definitions/{Id(a)}", HttpMethod.Put, new { parentNode = "Old", childNode = "Old", version = Version(a) })).StatusCode);
        saved = await Json(await staff.GetAsync(path)); Assert.True(saved["profiles"]![1]!["isArchived"]!.GetValue<bool>()); Assert.Equal("Technical specialism", saved["profiles"]![1]!["childNode"]!.GetValue<string>());
        await Json(await Send(staff, path + "/profiles", HttpMethod.Put, new { version = Version(saved["record"]!), assignments = new[] { new { slot = 5, definitionId = Id(a) } } }));
        saved = await Json(await staff.GetAsync(path));
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, path + "/profiles", HttpMethod.Put, new { version = Version(saved["record"]!), assignments = new[] { new { slot = 1, definitionId = Id(a) } } })).StatusCode);
        var found = await Json(await staff.GetAsync("/api/v1/crm/records?kind=contact&search=Technical%20specialism")); Assert.Contains(found["items"]!.AsArray(), x => Id(x!) == Id(contact));
        var portal = await Json(await member.GetAsync("/api/v1/portal/profile")); Assert.Null(portal["profiles"]);
        Assert.Equal(HttpStatusCode.BadRequest, (await PortalSend(member, "/profile", HttpMethod.Put, new { name = "Attempt", version = Version(portal), profiles = slots })).StatusCode);
        await Json(await PortalSend(member, "/profile", HttpMethod.Put, new { name = "Member", version = Version(portal), contactDetails = new ContactDetails(FirstName: "Member") }));
        saved = await Json(await staff.GetAsync(path)); Assert.Single(saved["profiles"]!.AsArray());
        await Json(await Send(staff, path + "/profiles", HttpMethod.Put, new { version = Version(saved["record"]!), assignments = Array.Empty<object>() }));
        saved = await Json(await staff.GetAsync(path)); Assert.Empty(saved["profiles"]!.AsArray()); Assert.Single(saved["tags"]!.AsArray());
        Assert.Contains("profiles.updated", (await Json(await staff.GetAsync(path + "/timeline"))).ToJsonString());
        var account = await Json(await Send(staff, "/api/v1/crm/records", HttpMethod.Post, new { kind = "account", name = "Profile account", status = "active" }), HttpStatusCode.Created);
        await Json(await Send(staff, $"/api/v1/crm/records/{Id(account)}/profiles", HttpMethod.Put, new { version = Version(account), assignments = new[] { new { slot = 3, definitionId = Id(b) } } }));
        Assert.Single((await Json(await staff.GetAsync($"/api/v1/crm/records/{Id(account)}")))["profiles"]!.AsArray());
        await Json(await Send(staff, path + "/archive", HttpMethod.Post, new { version = Version(saved["record"]!) })); saved = await Json(await staff.GetAsync(path));
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, path + "/profiles", HttpMethod.Put, new { version = Version(saved["record"]!), assignments = slots })).StatusCode);
        Assert.True(archived["isArchived"]!.GetValue<bool>());
    }

    [Fact]
    public async Task Profile_definitions_and_assignments_enforce_tenant_and_write_permissions()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var outside = await Login(app, "outside@nexora.test");
        var definition = await Json(await Send(staff, "/api/v1/crm/profile-definitions", HttpMethod.Post, new { parentNode = "Expertise", childNode = "Engineering" }), HttpStatusCode.Created);
        var contact = await Contact(staff); var path = $"/api/v1/crm/records/{Id(contact)}";
        Assert.Empty((await Json(await outside.GetAsync("/api/v1/crm/profile-definitions"))).AsArray());
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(outside, "/api/v1/crm/profile-definitions", HttpMethod.Post, new { parentNode = "Denied", childNode = "Denied" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(outside, $"/api/v1/crm/profile-definitions/{Id(definition)}", HttpMethod.Put, new { parentNode = "Denied", childNode = "Denied", version = Version(definition) })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(outside, path + "/profiles", HttpMethod.Put, new { version = Version(contact), assignments = new[] { new { slot = 1, definitionId = Id(definition) } } })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await app.CreateClient().GetAsync("/api/v1/crm/profile-definitions")).StatusCode);
    }
}
