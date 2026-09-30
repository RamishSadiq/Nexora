using System.Net;
using System.Text.Json.Nodes;
using Nexora.BuildingBlocks.Security;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;

public sealed partial class PortalEndpointTests
{
    [Fact]
    public async Task Structured_names_are_shared_versioned_allowlisted_and_preserve_legacy_display_names()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var member = app.CreateClient();
        var (_, contact) = await Grant(app, staff); await PortalLogin(member);
        var path = $"/api/v1/crm/records/{Id(contact)}";
        var original = await Json(await member.GetAsync("/api/v1/portal/profile"));
        Assert.Null(original["contactDetails"]);
        var names = new ContactDetails("Dr", "Alex", "Morgan", "Smith", "Jones", "PhD");
        await Json(await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(original), contactDetails = names }));
        var profile = await Json(await member.GetAsync("/api/v1/portal/profile"));
        Assert.Equal(original["name"]!.GetValue<string>(), profile["name"]!.GetValue<string>());
        Assert.Equal("Alex", profile["contactDetails"]!["firstName"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.Conflict, (await PortalSend(member, "/profile", HttpMethod.Put, new { name = "Stale", version = Version(original), contactDetails = names })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PortalSend(member, "/profile", HttpMethod.Put, new { name = "Bad", version = Version(profile), contactDetails = new { firstName = "Injected", accountId = Guid.NewGuid() } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PortalSend(member, "/profile", HttpMethod.Put, new { name = "Bad", version = Version(profile), contactDetails = names with { FirstName = new string('a', 161) } })).StatusCode);
        profile = await Json(await PortalSend(member, "/profile", HttpMethod.Put, new { name = "Alex Smith", version = Version(profile), contactDetails = names with { MiddleName = "New middle" } }));
        var shared = await Json(await staff.GetAsync(path));
        Assert.Equal("New middle", shared["contactDetails"]!["middleName"]!.GetValue<string>());
        Assert.Equal(Version(profile), Version(shared["record"]!));
        Assert.Contains("portal.profile.updated", (await Json(await staff.GetAsync(path + "/timeline"))).ToJsonString());
        // Older clients may save only the display name without erasing structured details.
        profile = await Json(await PortalSend(member, "/profile", HttpMethod.Put, new { name = "Display only", version = Version(profile) }));
        Assert.Equal("New middle", profile["contactDetails"]!["middleName"]!.GetValue<string>());
        profile = await Json(await PortalSend(member, "/profile", HttpMethod.Put, new { name = "Display only", version = Version(profile), contactDetails = (ContactDetails?)null }));
        Assert.Null(profile["contactDetails"]!["firstName"]);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(profile), accountDetails = new AccountDetails("Wrong kind") })).StatusCode);
        await Send(staff, path + "/archive", HttpMethod.Post, new { version = Version(profile) });
        var archived = await Json(await staff.GetAsync(path));
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(archived["record"]!), contactDetails = names })).StatusCode);
    }

    [Fact]
    public async Task Organisation_profile_requires_current_grant_and_exposes_only_company_fields()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var member = app.CreateClient();
        var (access, contact) = await Grant(app, staff); await PortalLogin(member);
        var account = await Json(await Send(staff, "/api/v1/crm/records", HttpMethod.Post, new { kind = "account", name = "Company", status = "active" }), HttpStatusCode.Created);
        var path = $"/api/v1/crm/records/{Id(account)}";
        var details = new AccountDetails("Company Limited", "Company Trading", new DateOnly(2001, 2, 3));
        await Json(await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(account), accountDetails = details }));
        await Json(await Send(staff, path + "/communications", HttpMethod.Post, new { kind = "website", value = "https://example.test", consent = "unknown" }));
        await Json(await Send(staff, path + "/notes", HttpMethod.Post, new { body = "Internal company note" }));
        var portalPath = $"/api/v1/portal/organisations/{Id(account)}/profile";
        Assert.Equal(HttpStatusCode.NotFound, (await member.GetAsync(portalPath)).StatusCode);
        await Json(await Send(staff, $"/api/v1/crm/accounts/{Id(account)}/contacts", HttpMethod.Post, new { contactId = Id(contact), startDate = DateTime.UtcNow.Date, workClassification = "unknown", roles = Array.Empty<string>() }));
        var adminPath = $"/api/v1/admin/portal-access/{access}/organisations";
        var grant = await Json(await Send(staff, adminPath, HttpMethod.Post, new { accountId = Id(account), canSubmitApplications = false }));
        var profile = await Json(await member.GetAsync(portalPath));
        Assert.Equal("Company Limited", profile["accountDetails"]!["fullName"]!.GetValue<string>());
        Assert.Equal("2001-02-03", profile["accountDetails"]!["dateBusinessCommenced"]!.GetValue<string>());
        Assert.Equal("https://example.test", profile["websites"]![0]!.GetValue<string>());
        Assert.DoesNotContain("Internal company note", profile.ToJsonString());
        Assert.Equal(3, profile.AsObject().Count);
        await Send(staff, $"{adminPath}/{Id(grant)}", HttpMethod.Delete);
        Assert.Equal(HttpStatusCode.NotFound, (await member.GetAsync(portalPath)).StatusCode);
        var current = await Json(await staff.GetAsync(path));
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(current["record"]!), accountDetails = details with { DateBusinessCommenced = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)) } })).StatusCode);
    }
}
