using System.Net;
using System.Text.Json.Nodes;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;

public sealed partial class PortalEndpointTests
{
    [Fact]
    public async Task Main_account_switches_atomically_preserves_links_and_ends_without_reassignment()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var member = app.CreateClient();
        var (access, contact) = await Grant(app, staff); await PortalLogin(member);
        async Task<JsonNode> Company(string name) => await Json(await Send(staff, "/api/v1/crm/records", HttpMethod.Post, new { kind = "account", name, status = "active" }), HttpStatusCode.Created);
        var a = await Company("Main A"); var b = await Company("Main B");
        async Task<JsonNode> Link(JsonNode account, JsonNode person) => await Json(await Send(staff, $"/api/v1/crm/accounts/{Id(account)}/contacts", HttpMethod.Post, new { contactId = Id(person), startDate = DateTime.UtcNow.Date, workClassification = "unknown", isMainContact = true, roles = Array.Empty<string>() }));
        var first = await Link(a, contact); var second = await Link(b, contact);
        var other = await Contact(staff); var unrelated = await Link(a, other);
        var recordPath = $"/api/v1/crm/records/{Id(contact)}";
        var path = $"/api/v1/crm/contacts/{Id(contact)}/main-account";
        async Task<string> CurrentVersion() => Version((await Json(await staff.GetAsync(recordPath)))["record"]!);
        async Task<JsonArray> Rows() => (await Json(await staff.GetAsync(recordPath + "/account-contacts?includeEnded=true")))["items"]!.AsArray();
        var version = await CurrentVersion();
        Assert.Equal(HttpStatusCode.BadRequest, (await staff.PutAsync(path, System.Net.Http.Json.JsonContent.Create(new { version, associationId = Id(first) }))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(staff, path, HttpMethod.Put, new { version, associationId = Id(unrelated) })).StatusCode);
        await Json(await Send(staff, path, HttpMethod.Put, new { version, associationId = Id(first) }));
        Assert.Equal(HttpStatusCode.Conflict, (await Send(staff, path, HttpMethod.Put, new { version, associationId = Id(second) })).StatusCode);
        await Json(await Send(staff, path, HttpMethod.Put, new { version = await CurrentVersion(), associationId = Id(second) }));
        var rows = await Rows();
        Assert.Equal(2, rows.Count);
        Assert.Equal(Id(second), Assert.Single(rows, x => x!["association"]!["isMainAccount"]!.GetValue<bool>())!["association"]!["id"]!.GetValue<string>());
        Assert.All(rows, x => Assert.True(x!["association"]!["isMainContact"]!.GetValue<bool>()));
        await Json(await Send(staff, $"/api/v1/admin/portal-access/{access}/organisations", HttpMethod.Post, new { accountId = Id(b), canSubmitApplications = false }));
        Assert.True((await Json(await member.GetAsync($"/api/v1/portal/organisations/{Id(b)}/association")))["isMainAccount"]!.GetValue<bool>());
        await Json(await Send(staff, path, HttpMethod.Put, new { version = await CurrentVersion(), associationId = (string?)null }));
        Assert.DoesNotContain(await Rows(), x => x!["association"]!["isMainAccount"]!.GetValue<bool>());
        await Json(await Send(staff, path, HttpMethod.Put, new { version = await CurrentVersion(), associationId = Id(second) }));
        second = (await Rows()).Single(x => x!["association"]!["id"]!.GetValue<string>() == Id(second))!["association"]!;
        await Json(await Send(staff, $"/api/v1/crm/account-contacts/{Id(second)}/end", HttpMethod.Post, new { version = Version(second), endDate = DateTime.UtcNow.Date, reason = "Moved" }));
        Assert.DoesNotContain(await Rows(), x => x!["association"]!["endDate"] == null && x["association"]!["isMainAccount"]!.GetValue<bool>());
        Assert.Equal(HttpStatusCode.NotFound, (await Send(staff, path, HttpMethod.Put, new { version = await CurrentVersion(), associationId = Id(second) })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await member.GetAsync($"/api/v1/portal/organisations/{Id(b)}/association")).StatusCode);
        var rejoined = await Link(b, contact);
        Assert.False(rejoined["isMainAccount"]!.GetValue<bool>());
        using var outside = await Login(app, "outside@nexora.test");
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(outside, path, HttpMethod.Put, new { version = await CurrentVersion(), associationId = Id(first) })).StatusCode);
        Assert.Contains("main-account-selected", (await Json(await staff.GetAsync(recordPath + "/timeline"))).ToJsonString());
    }
}
