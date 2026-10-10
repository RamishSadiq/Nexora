using System.Net;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;

public sealed partial class PortalEndpointTests
{
    [Fact]
    public async Task Portal_correspondence_updates_preserve_staff_address_details_and_linked_billing()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var member = app.CreateClient();
        var (_, contact) = await Grant(app, staff); await PortalLogin(member);
        var path = $"/api/v1/crm/records/{Id(contact)}";
        await Json(await Send(staff, path + "/addresses", HttpMethod.Post, new { label = "Member correspondence", purpose = "mailing", line1 = "Original road", line3 = "Staff line 3", county = "County", city = "Town", postalCode = "POST1", country = "UK" }));
        var detail = await Json(await staff.GetAsync(path));
        await Json(await Send(staff, path + "/billing-address", HttpMethod.Put, new { version = Version(detail["record"]!), useMailingAddress = true }));
        var profile = await Json(await member.GetAsync("/api/v1/portal/profile"));
        Assert.Null(profile["postalAddress"]!["county"]); Assert.Null(profile["postalAddress"]!["purpose"]);
        await Json(await PortalSend(member, "/profile", HttpMethod.Put, new { name = "Member", version = Version(profile), postalAddress = new { line1 = "Updated road", line2 = "", city = "Town", region = "", postalCode = "POST1", country = "UK" } }));
        detail = await Json(await staff.GetAsync(path));
        Assert.Equal("Updated road", detail["effectiveBillingAddress"]!["line1"]!.GetValue<string>());
        Assert.Equal("Staff line 3", detail["effectiveBillingAddress"]!["line3"]!.GetValue<string>());
        Assert.Equal("County", detail["effectiveBillingAddress"]!["county"]!.GetValue<string>());
        profile = await Json(await member.GetAsync("/api/v1/portal/profile"));
        Assert.Equal(HttpStatusCode.BadRequest, (await PortalSend(member, "/profile", HttpMethod.Put, new { name = "Member", version = Version(profile), postalAddress = (object?)null })).StatusCode);
        detail = await Json(await staff.GetAsync(path)); Assert.Equal("Updated road", detail["effectiveBillingAddress"]!["line1"]!.GetValue<string>());
    }
}
