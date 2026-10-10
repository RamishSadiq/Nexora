using System.Net;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;

public sealed partial class PortalEndpointTests
{
    [Fact]
    public async Task Membership_administrative_details_remain_outside_portal_contracts()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var portal = app.CreateClient();
        var (_, contact) = await Grant(app, staff); await PortalLogin(portal);
        var product = await Json(await Send(staff, "/api/v1/membership/products", HttpMethod.Post, new { name = "Portal admin boundary", category = "Standard", termMonths = 12, rate = 100, currency = "GBP", isActive = true }));
        var application = await Json(await Send(staff, "/api/v1/membership/applications", HttpMethod.Post, new { contactId = Id(contact), productId = Id(product), applicationType = "individual", termsAccepted = true, startsAtUtc = DateTime.UtcNow.Date }));
        var approved = await Json(await Send(staff, $"/api/v1/membership/applications/{Id(application)}/decision", HttpMethod.Post, new { version = Version(application), decision = "approve", reason = "Approved" }));
        var path = $"/api/v1/membership/members/{approved["membershipId"]!.GetValue<string>()}/administration";
        var initial = await Json(await staff.GetAsync(path));
        await Json(await Send(staff, path, HttpMethod.Put, new { version = Version(initial), details = new { legacyMemberReference = "STAFF-ONLY-42", membershipPackSent = true, membershipPackSentDate = "2001-02-03", remarks = "Staff administrative remarks" } }));
        var category = await Json(await Send(staff, "/api/v1/membership/setup/member-category", HttpMethod.Post, new { name = "STAFF-CATEGORY-ONLY", description = "" }));
        var administration = await Json(await staff.GetAsync(path));
        await Json(await Send(staff, path.Replace("/administration", "/classifications/category"), HttpMethod.Post, new { version = Version(administration), definitionId = Id(category), effectiveOn = "2001-02-03", reason = "Staff classification" }));
        var join = await Json(await staff.GetAsync(path.Replace("/administration", "/join-date")));
        await Json(await Send(staff, path.Replace("/administration", "/join-date"), HttpMethod.Put, new { version = Version(join), originalJoinDate = "1998-03-04", reason = "Original joining evidence" }));
        var own = await Json(await portal.GetAsync("/api/v1/portal/memberships")); Assert.Single(own["memberships"]!.AsArray());
        foreach (var field in new[] { "legacyMemberReference", "membershipPackSent", "membershipPackSentDate", "remarks", "STAFF-ONLY-42", "classifications", "STAFF-CATEGORY-ONLY", "originalJoinDate", "1998-03-04", "statusEffectiveOn", "resignedOn", "resignationReason", "reinstatedOn", "lifecycleDates", "previousMembershipId", "rejoinedOn", "isArchived", "archivedAtUtc", "archiveReason", "archivedByUserId", "memberType", "exemptJoiningFee", "freeMembership", "excludedFromLapse", "primaryMembership", "options", "financialArrangement", "renewalMonth", "recordedRenewalDate", "nextRenewalDate", "promotionCode", "paymentMethod", "paymentFrequency", "paidById", "invoicingAccountId" }) Assert.DoesNotContain(field, own.ToJsonString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await portal.GetAsync(path)).StatusCode);
    }
}
