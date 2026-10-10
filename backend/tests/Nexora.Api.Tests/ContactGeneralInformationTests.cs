using System.Net;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Contracts;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;

public sealed partial class PortalEndpointTests
{
    [Fact]
    public async Task Contact_general_information_round_trips_validates_and_remains_staff_only()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var member = app.CreateClient();
        var (_, contact) = await Grant(app, staff); await PortalLogin(member);
        var path = $"/api/v1/crm/records/{Id(contact)}";
        var original = await Json(await staff.GetAsync(path));
        var names = new ContactDetails(FirstName: "Alex");
        var general = new ContactGeneralInformation(" legacy-123 ", "Individual", "British", "OLD-9", "South", "London", "First line\nSecond line");
        await Json(await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(original["record"]!), contactDetails = names, generalInformation = general }));
        var saved = await Json(await staff.GetAsync(path));
        Assert.Equal("legacy-123", saved["generalInformation"]!["sourcePersonId"]!.GetValue<string>());
        Assert.Equal("Individual", saved["generalInformation"]!["contactType"]!.GetValue<string>());
        Assert.Equal("British", saved["generalInformation"]!["nationality"]!.GetValue<string>());
        Assert.Equal("OLD-9", saved["generalInformation"]!["oldReference"]!.GetValue<string>());
        Assert.Equal("South", saved["generalInformation"]!["region"]!.GetValue<string>());
        Assert.Equal("London", saved["generalInformation"]!["branch"]!.GetValue<string>());
        Assert.Equal(general.Remarks, saved["generalInformation"]!["remarks"]!.GetValue<string>());
        Assert.Equal(original["record"]!["name"]!.GetValue<string>(), saved["record"]!["name"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.Conflict, (await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(original["record"]!), contactDetails = names, generalInformation = general })).StatusCode);
        foreach (var invalid in new[] { general with { Branch = new string('a', 161) }, general with { Remarks = new string('a', 4001) }, general with { Nationality = "Bad\u0001" } })
            Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(saved["record"]!), contactDetails = names, generalInformation = invalid })).StatusCode);
        var portal = await Json(await member.GetAsync("/api/v1/portal/profile"));
        Assert.Null(portal["generalInformation"]);
        Assert.Null(portal["contactDetails"]!["oldReference"]);
        Assert.Equal(HttpStatusCode.BadRequest, (await PortalSend(member, "/profile", HttpMethod.Put, new { name = "Attempt", version = Version(portal), generalInformation = general })).StatusCode);
        await Json(await PortalSend(member, "/profile", HttpMethod.Put, new { name = "Updated display", version = Version(portal), contactDetails = names with { FirstName = "Updated" } }));
        saved = await Json(await staff.GetAsync(path));
        Assert.Equal("OLD-9", saved["generalInformation"]!["oldReference"]!.GetValue<string>());
        // A previous staff client can update names without erasing the new fields.
        await Json(await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(saved["record"]!), contactDetails = names }));
        saved = await Json(await staff.GetAsync(path));
        Assert.Equal("London", saved["generalInformation"]!["branch"]!.GetValue<string>());
        await Json(await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(saved["record"]!), contactDetails = names, generalInformation = new ContactGeneralInformation(Branch: " ") }));
        saved = await Json(await staff.GetAsync(path));
        Assert.Null(saved["generalInformation"]!["branch"]);
        Assert.Null(saved["generalInformation"]!["remarks"]);
        Assert.Contains("profile.updated", (await Json(await staff.GetAsync(path + "/timeline"))).ToJsonString());
        await Json(await Send(staff, path + "/archive", HttpMethod.Post, new { version = Version(saved["record"]!) }));
        saved = await Json(await staff.GetAsync(path));
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(saved["record"]!), contactDetails = names, generalInformation = general })).StatusCode);
    }
}
