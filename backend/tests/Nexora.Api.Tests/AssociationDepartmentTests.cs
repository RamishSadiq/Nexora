using System.Net;
using System.Text.Json.Nodes;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;

public sealed partial class PortalEndpointTests
{
    [Fact]
    public async Task Department_is_per_relationship_preserved_for_old_clients_and_scoped_in_portal()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var member = app.CreateClient();
        var (access, contact) = await Grant(app, staff); await PortalLogin(member);
        async Task<JsonNode> Company(string name) => await Json(await Send(staff, "/api/v1/crm/records", HttpMethod.Post, new { kind = "account", name, status = "active" }), HttpStatusCode.Created);
        var company = await Company("Department company"); var second = await Company("Second company");
        async Task<JsonNode> Link(JsonNode account, string department) => await Json(await Send(staff, $"/api/v1/crm/accounts/{Id(account)}/contacts", HttpMethod.Post, new { contactId = Id(contact), startDate = DateTime.UtcNow.Date, department, jobTitle = "Manager", workClassification = "unknown", roles = Array.Empty<string>() }));
        var link = await Link(company, "  Finance  "); var other = await Link(second, "Training");
        Assert.Equal("Finance", link["department"]!.GetValue<string>());
        var path = $"/api/v1/crm/account-contacts/{Id(link)}";
        var legacy = new { version = Version(link), jobTitle = "Director", workClassification = "unknown", roles = Array.Empty<string>() };
        link = await Json(await Send(staff, path, HttpMethod.Patch, legacy));
        Assert.Equal("Finance", link["department"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.Conflict, (await Send(staff, path, HttpMethod.Patch, legacy)).StatusCode);
        foreach (var bad in new[] { new string('a', 161), "Invalid\nDepartment" })
            Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, path, HttpMethod.Patch, new { version = Version(link), department = bad, workClassification = "unknown", roles = Array.Empty<string>() })).StatusCode);
        var portalPath = $"/api/v1/portal/organisations/{Id(company)}/association";
        Assert.Equal(HttpStatusCode.NotFound, (await member.GetAsync(portalPath)).StatusCode);
        await Json(await Send(staff, $"/api/v1/admin/portal-access/{access}/organisations", HttpMethod.Post, new { accountId = Id(company), canSubmitApplications = false }));
        var visible = await Json(await member.GetAsync(portalPath));
        Assert.Equal(new[] { "department", "isMainAccount", "jobTitle" }, visible.AsObject().Select(x => x.Key).Order().ToArray());
        Assert.Equal("Finance", visible["department"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.NotFound, (await member.GetAsync($"/api/v1/portal/organisations/{Id(second)}/association")).StatusCode);
        link = await Json(await Send(staff, path, HttpMethod.Patch, new { version = Version(link), department = (string?)null, workClassification = "unknown", roles = Array.Empty<string>() }));
        Assert.Null(link["department"]);
        link = await Json(await Send(staff, path, HttpMethod.Patch, new { version = Version(link), department = "Operations", workClassification = "unknown", roles = Array.Empty<string>() }));
        var ended = await Json(await Send(staff, path + "/end", HttpMethod.Post, new { version = Version(link), endDate = DateTime.UtcNow.Date, reason = "Moved" }));
        Assert.Equal(HttpStatusCode.NotFound, (await member.GetAsync(portalPath)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, path, HttpMethod.Patch, new { version = Version(ended), department = "Changed history", workClassification = "unknown", roles = Array.Empty<string>() })).StatusCode);
        var history = await Json(await staff.GetAsync($"/api/v1/crm/records/{Id(contact)}/account-contacts?includeEnded=true"));
        Assert.Contains(history["items"]!.AsArray(), x => x!["association"]!["department"]?.GetValue<string>() == "Operations");
        Assert.Contains(history["items"]!.AsArray(), x => x!["association"]!["id"]!.GetValue<string>() == Id(other) && x["association"]!["department"]!.GetValue<string>() == "Training");
    }
}
