using System.Net;
using Nexora.Modules.Identity;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;

public sealed partial class PortalEndpointTests
{
    [Fact]
    public async Task Restricted_crm_reader_has_independent_staff_and_portal_sessions()
    {
        using var app = new IdentityApiFactory();
        using var admin = await Login(app);
        var group = await Json(await Send(admin, "/api/v1/admin/security-groups", HttpMethod.Post,
            new { name = "CRM readers", description = "Disposable test", permissions = new[] { "crm.read" } }));
        const string email = "dual-reader@nexora.test";
        var user = await Json(await Send(admin, "/api/v1/admin/users", HttpMethod.Post,
            new { displayName = "Dual reader", email, isActive = true, groupIds = new[] { Id(group) }, password = IdentityApiFactory.AdminPassword }));
        // Authenticated API setup supplies tenant/actor to the retained workflow interceptor.
        var contact = await Contact(admin);
        async Task<string> GrantReader() => Id(await Json(await Send(admin, "/api/v1/admin/portal-access", HttpMethod.Post,
            new { userId = Id(user), contactId = Id(contact) })));
        var grant = await GrantReader();
        using var reader = await Login(app, email);
        Assert.Equal(HttpStatusCode.OK, (await reader.GetAsync("/api/v1/crm/records")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await reader.GetAsync("/api/v1/portal/profile")).StatusCode);
        async Task SignInPortal() => Assert.Equal(HttpStatusCode.OK, (await PortalSend(reader, "/login", HttpMethod.Post,
            new LoginRequest(email, IdentityApiFactory.AdminPassword, false))).StatusCode);
        await SignInPortal();
        Assert.Equal(HttpStatusCode.OK, (await reader.GetAsync("/api/v1/portal/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await reader.GetAsync("/api/v1/crm/records")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(reader, "/api/v1/crm/records", HttpMethod.Post,
            new { kind = "contact", name = "Denied", status = "active" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await PortalSend(reader, "/logout", HttpMethod.Post)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await reader.GetAsync("/api/v1/portal/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await reader.GetAsync("/api/v1/crm/records")).StatusCode);
        await SignInPortal();
        Assert.Equal(HttpStatusCode.NoContent, (await Send(reader, "/api/v1/auth/logout", HttpMethod.Post)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await reader.GetAsync("/api/v1/crm/records")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await reader.GetAsync("/api/v1/portal/profile")).StatusCode);
        await Json(await Send(reader, "/api/v1/auth/login", HttpMethod.Post, new LoginRequest(email, IdentityApiFactory.AdminPassword, false)));
        Assert.Equal(HttpStatusCode.NoContent, (await Send(admin, "/api/v1/admin/portal-access/" + grant, HttpMethod.Delete)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await reader.GetAsync("/api/v1/portal/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await reader.GetAsync("/api/v1/crm/records")).StatusCode);
        await GrantReader();
        Assert.Equal(HttpStatusCode.Unauthorized, (await reader.GetAsync("/api/v1/portal/profile")).StatusCode);
        await SignInPortal();
        Assert.Equal(HttpStatusCode.OK, (await reader.GetAsync("/api/v1/portal/profile")).StatusCode);
    }
}
