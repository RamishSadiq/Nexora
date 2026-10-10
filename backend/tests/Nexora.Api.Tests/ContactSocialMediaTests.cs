using System.Net;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Contracts;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;

public sealed partial class PortalEndpointTests
{
    [Fact]
    public async Task Contact_social_details_round_trip_validate_clear_and_remain_staff_only()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var member = app.CreateClient();
        var (_, contact) = await Grant(app, staff); await PortalLogin(member);
        var path = $"/api/v1/crm/records/{Id(contact)}"; var names = new ContactDetails(FirstName: "Alex");
        var initial = await Json(await staff.GetAsync(path));
        var social = new ContactSocialMediaDetails(" facebook-user ", "x-user", "linkedin-user", "youtube-user", "Community A, Community B", "google-user", 3000000000L, 0, 12, 13, 14, new DateOnly(2001, 2, 3), " Manual review ");
        await Json(await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(initial["record"]!), contactDetails = names, socialMediaDetails = social, communicationPermissions = new ContactCommunicationPermissions(StopAllCommunication: true) }));
        var saved = await Json(await staff.GetAsync(path)); var fields = saved["socialMediaDetails"]!;
        Assert.Equal("facebook-user", fields["facebookId"]!.GetValue<string>()); Assert.Equal("x-user", fields["xId"]!.GetValue<string>());
        Assert.Equal("linkedin-user", fields["linkedInId"]!.GetValue<string>()); Assert.Equal("youtube-user", fields["youTubeId"]!.GetValue<string>());
        Assert.Equal("Community A, Community B", fields["onlineCommunities"]!.GetValue<string>()); Assert.Equal("google-user", fields["googleId"]!.GetValue<string>());
        Assert.Equal(3000000000L, fields["numberOfFollowers"]!.GetValue<long>()); Assert.Equal(0, fields["numberOfFollowing"]!.GetValue<long>());
        Assert.Equal(12, fields["numberOfTweetsSent"]!.GetValue<long>()); Assert.Equal(13, fields["numberOfMentions"]!.GetValue<long>()); Assert.Equal(14, fields["numberOfTweetsWithTags"]!.GetValue<long>());
        Assert.Equal("2001-02-03", fields["metricsObservedOn"]!.GetValue<string>()); Assert.Equal("Manual review", fields["metricsSource"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.Conflict, (await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(initial["record"]!), contactDetails = names, socialMediaDetails = social })).StatusCode);
        foreach (var invalid in new[] { social with { NumberOfFollowers = -1 }, social with { NumberOfFollowing = 9007199254740992L }, social with { FacebookId = new string('a', 321) }, social with { XId = "Bad\u0001" }, social with { MetricsSource = new string('a', 161) }, social with { MetricsObservedOn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)) } })
            Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(saved["record"]!), contactDetails = names, socialMediaDetails = invalid })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(saved["record"]!), contactDetails = names, socialMediaDetails = new { numberOfFollowers = 1.5 } })).StatusCode);
        var portal = await Json(await member.GetAsync("/api/v1/portal/profile")); Assert.Null(portal["socialMediaDetails"]); Assert.Null(portal["contactDetails"]!["facebookId"]);
        Assert.Equal(HttpStatusCode.BadRequest, (await PortalSend(member, "/profile", HttpMethod.Put, new { name = "Attempt", version = Version(portal), socialMediaDetails = social })).StatusCode);
        await Json(await PortalSend(member, "/profile", HttpMethod.Put, new { name = "Member", version = Version(portal), contactDetails = names }));
        saved = await Json(await staff.GetAsync(path)); Assert.Equal("facebook-user", saved["socialMediaDetails"]!["facebookId"]!.GetValue<string>());
        await Json(await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(saved["record"]!), contactDetails = names }));
        saved = await Json(await staff.GetAsync(path)); Assert.Equal(3000000000L, saved["socialMediaDetails"]!["numberOfFollowers"]!.GetValue<long>());
        await Json(await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(saved["record"]!), contactDetails = names, socialMediaDetails = new ContactSocialMediaDetails(FacebookId: " ") }));
        saved = await Json(await staff.GetAsync(path)); Assert.All(saved["socialMediaDetails"]!.AsObject(), field => Assert.Null(field.Value));
        Assert.True(saved["communicationPermissions"]!["stopAllCommunication"]!.GetValue<bool>());
        Assert.Contains("profile.updated", (await Json(await staff.GetAsync(path + "/timeline"))).ToJsonString());
        var account = await Json(await Send(staff, "/api/v1/crm/records", HttpMethod.Post, new { kind = "account", name = "Social boundary", status = "active" }), HttpStatusCode.Created);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, $"/api/v1/crm/records/{Id(account)}/profile", HttpMethod.Put, new { version = Version(account), accountDetails = new AccountDetails("Account"), socialMediaDetails = social })).StatusCode);
        await Json(await Send(staff, path + "/archive", HttpMethod.Post, new { version = Version(saved["record"]!) })); saved = await Json(await staff.GetAsync(path));
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, path + "/profile", HttpMethod.Put, new { version = Version(saved["record"]!), contactDetails = names, socialMediaDetails = social })).StatusCode);
    }
}
