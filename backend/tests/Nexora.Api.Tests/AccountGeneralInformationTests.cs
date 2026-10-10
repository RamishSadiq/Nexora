using System.Net;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Contracts;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;

public sealed partial class PortalEndpointTests
{
    [Fact]
    public async Task Account_general_information_round_trips_validates_preserves_and_clears()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app);
        var account = await Json(await Send(staff, "/api/v1/crm/records", HttpMethod.Post, new { kind = "account", name = "Display company", status = "active" }), HttpStatusCode.Created);
        var path = $"/api/v1/crm/records/{Id(account)}"; var initial = await Json(await staff.GetAsync(path)); Assert.Null(initial["accountGeneralInformation"]);
        var details = new AccountDetails("Company full name", "Company trading name", new DateOnly(2001, 2, 3));
        var general = new AccountGeneralInformation(" SRC-ACCOUNT-42 ", "Dormant", "Association", "Engineering", " Recorded venue ", "Campaign", "1–5 million", 0, "0–10", "South", "London", "OLD-42");
        await Json(await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(account), accountDetails = details, accountGeneralInformation = general }));
        var saved = await Json(await staff.GetAsync(path)); Assert.Equal("SRC-ACCOUNT-42", saved["accountGeneralInformation"]!["sourceAccountId"]!.GetValue<string>()); Assert.Equal("Recorded venue", saved["accountGeneralInformation"]!["venue"]!.GetValue<string>());
        Assert.Equal(0, saved["accountGeneralInformation"]!["numberOfEmployees"]!.GetValue<int>()); Assert.Equal("Dormant", saved["accountGeneralInformation"]!["tradingStatus"]!.GetValue<string>());
        Assert.Equal("active", saved["record"]!["status"]!.GetValue<string>()); Assert.Equal("Display company", saved["record"]!["name"]!.GetValue<string>()); Assert.Equal("Company full name", saved["accountDetails"]!["fullName"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.Conflict, (await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(account), accountDetails = details, accountGeneralInformation = general })).StatusCode);
        foreach (var invalid in new[] { general with { NumberOfEmployees = -1 }, general with { IndustrySector = new string('a', 161) }, general with { TradingStatus = "Bad\u0001" } })
            Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(saved["record"]!), accountDetails = details, accountGeneralInformation = invalid })).StatusCode);
        foreach (var count in new object[] { 1.5m, 2147483648L }) Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(saved["record"]!), accountDetails = details, accountGeneralInformation = new { numberOfEmployees = count } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(saved["record"]!), accountDetails = details, generalInformation = new ContactGeneralInformation() })).StatusCode);
        var contact = await Contact(staff); Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, $"/api/v1/crm/records/{Id(contact)}/profile", HttpMethod.Put, new { version = Version(contact), contactDetails = new ContactDetails("Dr"), accountGeneralInformation = general })).StatusCode);
        using var outside = await Login(app, "outside@nexora.test"); Assert.Equal(HttpStatusCode.Forbidden, (await Send(outside, path + "/profile", HttpMethod.Put, new { version = Version(saved["record"]!), accountDetails = details, accountGeneralInformation = general })).StatusCode);
        // Omission from an older client must preserve the newly added Account fields.
        await Json(await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(saved["record"]!), accountDetails = details with { TradingAs = "Renamed trading name" } }));
        saved = await Json(await staff.GetAsync(path)); Assert.Equal("OLD-42", saved["accountGeneralInformation"]!["oldReference"]!.GetValue<string>()); Assert.Equal(0, saved["accountGeneralInformation"]!["numberOfEmployees"]!.GetValue<int>());
        await Json(await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(saved["record"]!), accountDetails = details, accountGeneralInformation = new AccountGeneralInformation(Region: " ") }));
        saved = await Json(await staff.GetAsync(path)); Assert.All(saved["accountGeneralInformation"]!.AsObject(), field => Assert.Null(field.Value)); Assert.Equal("Company full name", saved["accountDetails"]!["fullName"]!.GetValue<string>());
        Assert.Contains("profile.updated", (await Json(await staff.GetAsync(path + "/timeline"))).ToJsonString());
        await Json(await Send(staff, path + "/archive", HttpMethod.Post, new { version = Version(saved["record"]!) })); saved = await Json(await staff.GetAsync(path));
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(saved["record"]!), accountDetails = details, accountGeneralInformation = general })).StatusCode);
    }

    [Fact]
    public async Task Account_general_information_stays_outside_delegated_portal_organisation_profile()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var portal = app.CreateClient();
        var (access, contact) = await Grant(app, staff); await PortalLogin(portal);
        var account = await Json(await Send(staff, "/api/v1/crm/records", HttpMethod.Post, new { kind = "account", name = "Company", status = "active" }), HttpStatusCode.Created);
        var path = $"/api/v1/crm/records/{Id(account)}";
        await Json(await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(account), accountDetails = new AccountDetails("Public organisation name"), accountGeneralInformation = new AccountGeneralInformation(SourceAccountId: "STAFF-SOURCE-ID", TurnoverRange: "STAFF-TURNOVER", NumberOfEmployees: 42) }));
        await Json(await Send(staff, $"/api/v1/crm/accounts/{Id(account)}/contacts", HttpMethod.Post, new { contactId = Id(contact), startDate = DateTime.UtcNow.Date, workClassification = "unknown", roles = Array.Empty<string>() }));
        await Json(await Send(staff, $"/api/v1/admin/portal-access/{access}/organisations", HttpMethod.Post, new { accountId = Id(account), canSubmitApplications = false }));
        var profile = await Json(await portal.GetAsync($"/api/v1/portal/organisations/{Id(account)}/profile"));
        Assert.Equal("Public organisation name", profile["accountDetails"]!["fullName"]!.GetValue<string>());
        foreach (var field in new[] { "accountGeneralInformation", "sourceAccountId", "tradingStatus", "industrySector", "turnoverRange", "numberOfEmployees", "oldReference", "STAFF-SOURCE-ID", "STAFF-TURNOVER" }) Assert.DoesNotContain(field, profile.ToJsonString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await portal.GetAsync(path)).StatusCode);
    }
}
