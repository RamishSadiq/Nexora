using System.Net;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.Modules.Identity.Domain;
using Nexora.Modules.Identity.Infrastructure.Persistence;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;

public sealed class CrmMembershipProjectionTests
{
    private static async Task<JsonNode> Product(HttpClient client) => await Json(await Send(client, "/api/v1/membership/products", HttpMethod.Post, new { name = "Annual", category = "Standard", termMonths = 12, rate = 125.50m, currency = "GBP", isActive = true }));
    private static async Task<JsonNode> Approve(HttpClient client, JsonNode contact, JsonNode product, JsonNode? account = null)
    {
        var application = await Json(await Send(client, "/api/v1/membership/applications", HttpMethod.Post, new { contactId = Id(contact), accountId = account == null ? null : Id(account), productId = Id(product), applicationType = account == null ? "individual" : "corporate", termsAccepted = true, startsAtUtc = DateTime.UtcNow.Date }));
        return await Json(await Send(client, $"/api/v1/membership/applications/{Id(application)}/decision", HttpMethod.Post, new { version = Version(application), decision = "approve", reason = "Eligible" }));
    }

    [Fact]
    public async Task Contact_membership_projection_tracks_owned_lifecycle_and_current_term_review_without_writes()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app); var contact = await Contact(client); var product = await Product(client);
        var path = $"/api/v1/crm/records/{Id(contact)}/memberships";
        Assert.Empty((await Json(await client.GetAsync(path)))["items"]!.AsArray());
        var application = await Approve(client, contact, product); var id = application["membershipId"]!.GetValue<string>();
        var member = (await Json(await client.GetAsync($"/api/v1/membership/members/{id}")))["member"]!;
        await Json(await Send(client, $"/api/v1/membership/members/{id}/review", HttpMethod.Put, new { memberVersion = Version(member), paymentVerified = false, paymentReference = "", complianceStatus = "pending", evidence = "", declarationAccepted = false, excludedFromRenewal = true, notes = "Current review" }));
        var projected = (await Json(await client.GetAsync(path)))["items"]![0]!;
        Assert.Equal("personal", projected["scope"]!.GetValue<string>()); Assert.Null(projected["accountId"]);
        Assert.Equal(id, Id(projected)); Assert.Equal("Annual", projected["productName"]!.GetValue<string>()); Assert.Equal("Standard", projected["productCategory"]!.GetValue<string>());
        Assert.Equal(125.50m, projected["rate"]!.GetValue<decimal>()); Assert.Equal("GBP", projected["currency"]!.GetValue<string>());
        Assert.Equal(Id(application), projected["applicationId"]!.GetValue<string>()); Assert.Equal(application["createdAtUtc"]!.GetValue<DateTime>(), projected["applicationDateUtc"]!.GetValue<DateTime>());
        Assert.Equal("individual", projected["applicationType"]!.GetValue<string>()); Assert.Equal(member["startsAtUtc"]!.GetValue<DateTime>(), projected["startsAtUtc"]!.GetValue<DateTime>());
        Assert.True(projected["excludedFromRenewal"]!.GetValue<bool>()); Assert.Equal("Current review", projected["reviewNotes"]!.GetValue<string>());
        Assert.Equal("membership.activated", projected["latestLifecycleAction"]!.GetValue<string>()); Assert.Equal("Eligible", projected["latestLifecycleReason"]!.GetValue<string>());
        member = (await Json(await client.GetAsync($"/api/v1/membership/members/{id}")))["member"]!;
        var cancelled = await Json(await Send(client, $"/api/v1/membership/members/{id}/cancel", HttpMethod.Post, new { version = Version(member), reason = "Member request" }));
        projected = (await Json(await client.GetAsync(path)))["items"]![0]!; Assert.Equal("cancelled", projected["status"]!.GetValue<string>()); Assert.Equal("membership.cancel", projected["latestLifecycleAction"]!.GetValue<string>());
        var review = (await Json(await client.GetAsync($"/api/v1/membership/members/{id}/operations")))["review"]!;
        await Json(await Send(client, $"/api/v1/membership/members/{id}/review", HttpMethod.Put, new { version = Version(review), memberVersion = Version(cancelled), paymentVerified = false, paymentReference = "", complianceStatus = "pending", evidence = "", declarationAccepted = false, excludedFromRenewal = false, notes = "Allow reinstatement" }));
        member = (await Json(await client.GetAsync($"/api/v1/membership/members/{id}")))["member"]!;
        var reinstated = await Json(await Send(client, $"/api/v1/membership/members/{id}/reinstate", HttpMethod.Post, new { version = Version(member), reason = "Return" }));
        projected = (await Json(await client.GetAsync(path)))["items"]![0]!; Assert.Equal("active", projected["status"]!.GetValue<string>()); Assert.Equal("membership.reinstate", projected["latestLifecycleAction"]!.GetValue<string>());
        Assert.Equal(reinstated["endsAtUtc"]!.GetValue<DateTime>(), projected["endsAtUtc"]!.GetValue<DateTime>()); Assert.False(projected["excludedFromRenewal"]!.GetValue<bool>());
        var renewed = await Json(await Send(client, $"/api/v1/membership/members/{id}/renew", HttpMethod.Post, new { version = Version(reinstated), reason = "New term" }));
        projected = (await Json(await client.GetAsync(path)))["items"]![0]!; Assert.Equal(renewed["endsAtUtc"]!.GetValue<DateTime>(), projected["endsAtUtc"]!.GetValue<DateTime>());
        Assert.Equal("membership.renew", projected["latestLifecycleAction"]!.GetValue<string>()); Assert.Null(projected["excludedFromRenewal"]); Assert.Null(projected["reviewNotes"]);
        var before = await Json(await client.GetAsync($"/api/v1/membership/members/{id}")); var crmBefore = await Json(await client.GetAsync($"/api/v1/crm/records/{Id(contact)}"));
        await Json(await client.GetAsync(path)); await Json(await client.GetAsync(path));
        var after = await Json(await client.GetAsync($"/api/v1/membership/members/{id}")); var crmAfter = await Json(await client.GetAsync($"/api/v1/crm/records/{Id(contact)}"));
        Assert.Equal(Version(before["member"]!), Version(after["member"]!)); Assert.Equal(before["history"]!.AsArray().Count, after["history"]!.AsArray().Count); Assert.Equal(Version(crmBefore["record"]!), Version(crmAfter["record"]!));
        await Json(await Send(client, $"/api/v1/crm/records/{Id(contact)}/archive", HttpMethod.Post, new { version = Version(crmAfter["record"]!) })); Assert.Single((await Json(await client.GetAsync(path)))["items"]!.AsArray());
    }

    [Fact]
    public async Task Membership_projection_separates_organisation_subject_from_applicant_and_validates_pages()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app); var contact = await Contact(client); var colleague = await Contact(client); var product = await Product(client);
        var account = await Json(await Send(client, "/api/v1/crm/records", HttpMethod.Post, new { kind = "account", name = "Organisation", status = "active" }), HttpStatusCode.Created);
        foreach (var person in new[] { contact, colleague }) await Json(await Send(client, $"/api/v1/crm/accounts/{Id(account)}/contacts", HttpMethod.Post, new { contactId = Id(person), startDate = DateTime.UtcNow.Date, workClassification = "unknown", roles = Array.Empty<string>() }));
        await Approve(client, contact, product); await Approve(client, contact, product, account);
        var path = $"/api/v1/crm/records/{Id(contact)}/memberships";
        var first = await Json(await client.GetAsync(path + "?page=1&pageSize=1")); var second = await Json(await client.GetAsync(path + "?page=2&pageSize=1"));
        Assert.Equal(2, first["total"]!.GetValue<int>()); Assert.Single(first["items"]!.AsArray()); Assert.NotEqual(Id(first["items"]![0]!), Id(second["items"]![0]!));
        var rows = (await Json(await client.GetAsync(path)))["items"]!.AsArray(); Assert.Contains(rows, x => x!["scope"]!.GetValue<string>() == "personal"); Assert.Contains(rows, x => x!["scope"]!.GetValue<string>() == "organisation" && x["accountId"]!.GetValue<string>() == Id(account));
        Assert.Empty((await Json(await client.GetAsync($"/api/v1/crm/records/{Id(colleague)}/memberships")))["items"]!.AsArray());
        var organisation = (await Json(await client.GetAsync($"/api/v1/crm/records/{Id(account)}/memberships")))["items"]!.AsArray(); Assert.Single(organisation); Assert.Equal("organisation", organisation[0]!["scope"]!.GetValue<string>());
        foreach (var query in new[] { "?page=0", "?page=10001", "?pageSize=0", "?pageSize=51" }) Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(path + query)).StatusCode);
        Assert.Empty((await Json(await client.GetAsync(path + "?page=3&pageSize=1")))["items"]!.AsArray());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/crm/records/{Guid.NewGuid()}/memberships")).StatusCode);
    }

    [Fact]
    public async Task Membership_projection_requires_both_crm_and_membership_read_permissions()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var outside = await Login(app, "outside@nexora.test"); var contact = await Contact(staff);
        var path = $"/api/v1/crm/records/{Id(contact)}/memberships";
        Assert.Equal(HttpStatusCode.Forbidden, (await outside.GetAsync(path)).StatusCode);
        using var anonymous = app.CreateClient(); Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(path)).StatusCode);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NexoraIdentityDbContext>();
            using var system = db.BeginSystemAccess();
            var role = await db.Roles.IgnoreQueryFilters().SingleAsync(x => x.Name == "Outside Administrator");
            db.RolePermissions.Add(new RolePermission { RoleId = role.Id, Permission = "membership.read" }); await db.SaveChangesAsync();
        }
        using var permittedOutside = await Login(app, "outside@nexora.test"); Assert.Equal(HttpStatusCode.NotFound, (await permittedOutside.GetAsync(path)).StatusCode);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NexoraIdentityDbContext>();
            using var system = db.BeginSystemAccess();
            var role = await db.Roles.IgnoreQueryFilters().SingleAsync(x => x.Name == "Outside Administrator");
            db.Remove(await db.RolePermissions.IgnoreQueryFilters().SingleAsync(x => x.RoleId == role.Id && x.Permission == "crm.read")); await db.SaveChangesAsync();
        }
        using var membershipOnly = await Login(app, "outside@nexora.test"); Assert.Equal(HttpStatusCode.Forbidden, (await membershipOnly.GetAsync(path)).StatusCode);
    }
}
