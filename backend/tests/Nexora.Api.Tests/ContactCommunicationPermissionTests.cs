using System.Net;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Contracts;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;

public sealed partial class PortalEndpointTests
{
    [Fact]
    public async Task Contact_permission_fields_round_trip_validate_and_preserve_unknown_and_staff_boundaries()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var member = app.CreateClient();
        var (_, contact) = await Grant(app, staff); await PortalLogin(member);
        var path = $"/api/v1/crm/records/{Id(contact)}";
        var initial = await Json(await staff.GetAsync(path)); var names = new ContactDetails(FirstName: "Alex");
        var permissions = new ContactCommunicationPermissions(true, false, new DateOnly(2001, 2, 3), " Research partner ", true, new DateOnly(2002, 3, 4), true, new DateOnly(2003, 4, 5));
        await Json(await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(initial["record"]!), contactDetails = names, communicationPermissions = permissions }));
        var saved = await Json(await staff.GetAsync(path)); var fields = saved["communicationPermissions"]!;
        Assert.True(fields["thirdPartyEmailPermission"]!.GetValue<bool>()); Assert.False(fields["disclosureDataToThirdParty"]!.GetValue<bool>());
        Assert.Equal("2001-02-03", fields["thirdPartyDisclosureConsentDate"]!.GetValue<string>()); Assert.Equal("Research partner", fields["typeOfThirdParty"]!.GetValue<string>());
        Assert.True(fields["stopAllCommunication"]!.GetValue<bool>()); Assert.Equal("2002-03-04", fields["dateStopAllCommunication"]!.GetValue<string>());
        Assert.True(fields["bouncedInvalidEmailAddress"]!.GetValue<bool>()); Assert.Equal("2003-04-05", fields["dateEmailBounced"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.Conflict, (await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(initial["record"]!), contactDetails = names, communicationPermissions = permissions })).StatusCode);
        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));
        foreach (var invalid in new[] { permissions with { ThirdPartyDisclosureConsentDate = future }, permissions with { DateStopAllCommunication = future }, permissions with { DateEmailBounced = DateOnly.MinValue }, permissions with { TypeOfThirdParty = new string('a', 161) }, permissions with { TypeOfThirdParty = "Bad\u0001" } })
            Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(saved["record"]!), contactDetails = names, communicationPermissions = invalid })).StatusCode);
        var portal = await Json(await member.GetAsync("/api/v1/portal/profile"));
        Assert.Null(portal["communicationPermissions"]); Assert.Null(portal["contactDetails"]!["stopAllCommunication"]);
        Assert.Equal(HttpStatusCode.BadRequest, (await PortalSend(member, "/profile", HttpMethod.Put, new { name = "Attempt", version = Version(portal), communicationPermissions = permissions })).StatusCode);
        await Json(await PortalSend(member, "/profile", HttpMethod.Put, new { name = "Member", version = Version(portal), contactDetails = names with { FirstName = "Updated" } }));
        saved = await Json(await staff.GetAsync(path)); Assert.True(saved["communicationPermissions"]!["stopAllCommunication"]!.GetValue<bool>());
        await Json(await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(saved["record"]!), contactDetails = names }));
        saved = await Json(await staff.GetAsync(path)); Assert.Equal("Research partner", saved["communicationPermissions"]!["typeOfThirdParty"]!.GetValue<string>());
        await Json(await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(saved["record"]!), contactDetails = names, communicationPermissions = new ContactCommunicationPermissions(StopAllCommunication: false, TypeOfThirdParty: " ") }));
        saved = await Json(await staff.GetAsync(path)); fields = saved["communicationPermissions"]!;
        Assert.False(fields["stopAllCommunication"]!.GetValue<bool>()); Assert.Null(fields["thirdPartyEmailPermission"]); Assert.Null(fields["bouncedInvalidEmailAddress"]); Assert.Null(fields["typeOfThirdParty"]); Assert.Null(fields["dateStopAllCommunication"]);
        Assert.Contains("profile.updated", (await Json(await staff.GetAsync(path + "/timeline"))).ToJsonString());
        var account = await Json(await Send(staff, "/api/v1/crm/records", HttpMethod.Post, new { kind = "account", name = "Account permission boundary", status = "active" }), HttpStatusCode.Created);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, $"/api/v1/crm/records/{Id(account)}/profile", HttpMethod.Put, new { version = Version(account), accountDetails = new AccountDetails("Account"), communicationPermissions = permissions })).StatusCode);
        await Json(await Send(staff, path + "/archive", HttpMethod.Post, new { version = Version(saved["record"]!) }));
        saved = await Json(await staff.GetAsync(path));
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(saved["record"]!), contactDetails = names, communicationPermissions = permissions })).StatusCode);
    }
}
