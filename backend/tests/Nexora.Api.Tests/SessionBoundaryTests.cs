using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Nexora.Modules.Identity;
using Nexora.Modules.Identity.Infrastructure.Persistence;

namespace Nexora.Api.Tests;

public sealed class SessionBoundaryTests
{
    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client)
    {
        var csrf = await client.GetFromJsonAsync<CsrfResponse>("/api/v1/auth/csrf");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new LoginRequest(IdentityApiFactory.AdminEmail, IdentityApiFactory.AdminPassword, false))
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task Cookie_is_httponly_secure_over_https_and_logout_requires_csrf()
    {
        using var app = new IdentityApiFactory();
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var response = await LoginAsync(client);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), x => x.StartsWith("Nexora.Session="));
        Assert.Contains("httponly", cookie.ToLowerInvariant());
        Assert.Contains("secure", cookie.ToLowerInvariant());
        Assert.Contains("samesite=lax", cookie.ToLowerInvariant());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/v1/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/auth/session")).StatusCode);
    }

    [Fact]
    public async Task Forged_cookie_and_tenant_header_do_not_authenticate()
    {
        using var app = new IdentityApiFactory();
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", "Nexora.Session=preview; nexora-preview-session=true");
        client.DefaultRequestHeaders.Add("X-Tenant-Id", Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/session")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/admin/access-overview")).StatusCode);
    }

    [Fact]
    public async Task Existing_session_is_denied_when_user_is_deactivated()
    {
        using var app = new IdentityApiFactory();
        using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client)).StatusCode);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NexoraIdentityDbContext>();
            using var system = db.BeginSystemAccess();
            (await db.Users.SingleAsync(x => x.Email == IdentityApiFactory.AdminEmail)).IsActive = false;
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/auth/session")).StatusCode);
    }

    [Fact]
    public async Task Production_never_enables_local_identity_even_if_configuration_requests_it()
    {
        using var app = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Identity:Provider:LocalDevelopmentEnabled"] = "true"
            }));
        });
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await LoginAsync(client)).StatusCode);
        using var fresh = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var response = await fresh.GetAsync("/api/v1/auth/csrf");
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.StartsWith("__Host-Nexora.Antiforgery=", cookie);
        Assert.Contains("secure", cookie.ToLowerInvariant());
    }
}
