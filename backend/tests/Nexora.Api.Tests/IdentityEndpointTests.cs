using System.Net;
using System.Net.Http.Json;
using Nexora.Modules.Identity;

namespace Nexora.Api.Tests;

public sealed class IdentityEndpointTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _application;

    public IdentityEndpointTests(IdentityApiFactory application)
    {
        _application = application;
    }

    [Fact]
    public async Task Session_requires_authentication()
    {
        using var client = _application.CreateClient();
        var response = await client.GetAsync("/api/v1/auth/session");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_creates_a_tenant_scoped_session_and_logout_removes_it()
    {
        using var client = _application.CreateClient();
        var csrf = await client.GetFromJsonAsync<CsrfResponse>("/api/v1/auth/csrf");
        Assert.NotNull(csrf);

        using var login = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new LoginRequest(
                IdentityApiFactory.AdminEmail,
                IdentityApiFactory.AdminPassword,
                true)),
        };
        login.Headers.Add("X-CSRF-TOKEN", csrf.Token);
        var loginResponse = await client.SendAsync(login);
        Assert.True(
            loginResponse.IsSuccessStatusCode,
            $"Login returned {(int)loginResponse.StatusCode}: {await loginResponse.Content.ReadAsStringAsync()}");

        var session = await client.GetFromJsonAsync<SessionResponse>("/api/v1/auth/session");
        Assert.NotNull(session);
        Assert.Equal("Northstar Group", session.TenantName);
        Assert.Contains("identity.read", session.Permissions);

        var overview = await client.GetFromJsonAsync<AccessOverviewResponse>("/api/v1/admin/access-overview");
        Assert.NotNull(overview);
        Assert.Single(overview.Users);
        Assert.DoesNotContain(overview.Users, user => user.Email == "outside@nexora.test");
        Assert.Single(overview.Teams);
        Assert.Equal("Customer Success", overview.Teams[0].Name);

        var logoutCsrf = await client.GetFromJsonAsync<CsrfResponse>("/api/v1/auth/csrf");
        using var logout = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        logout.Headers.Add("X-CSRF-TOKEN", logoutCsrf!.Token);
        Assert.Equal(HttpStatusCode.NoContent, (await client.SendAsync(logout)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/session")).StatusCode);
    }

    [Fact]
    public async Task Login_rejects_invalid_credentials_without_disclosing_the_account()
    {
        using var client = _application.CreateClient();
        var csrf = await client.GetFromJsonAsync<CsrfResponse>("/api/v1/auth/csrf");

        using var login = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new LoginRequest(
                IdentityApiFactory.AdminEmail,
                "Definitely-Wrong-1!",
                false)),
        };
        login.Headers.Add("X-CSRF-TOKEN", csrf!.Token);

        var response = await client.SendAsync(login);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Email or password is incorrect", body);
        Assert.DoesNotContain(IdentityApiFactory.AdminEmail, body);
    }

    [Fact]
    public async Task Login_rejects_a_missing_antiforgery_token()
    {
        using var client = _application.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(
                IdentityApiFactory.AdminEmail,
                IdentityApiFactory.AdminPassword,
                false));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
