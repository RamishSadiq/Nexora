using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;

public sealed class AssistantBoundaryTests
{
    [Fact]
    public async Task Legacy_signed_tokens_cannot_read_crm_even_with_a_valid_tenant_session()
    {
        using var app = new IdentityApiFactory();
        using var admin = await Login(app);
        var contact = await Contact(admin);
        var session = await Json(await admin.GetAsync("/api/v1/auth/session"));
        const string secret = "assistant-boundary-test-secret";
        app.Services.GetRequiredService<IConfiguration>()["NEXORCHESTR_SHARED_SECRET"] = secret;
        var payload = WebEncoders.Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new
        {
            tenantId = session["tenantId"]!.GetValue<string>(),
            userId = session["userId"]!.GetValue<string>(),
            exp = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds()
        }));
        var signature = WebEncoders.Base64UrlEncode(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload)));
        var path = $"/api/v1/assistant/crm-context?accessToken={payload}.{signature}&id={Id(contact)}";

        using var anonymous = app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(path)).StatusCode);
        var response = await admin.GetAsync(path);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(Id(contact), body);
        Assert.DoesNotContain("Workflow Contact", body);

        using var outside = await Login(app, "outside@nexora.test");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await outside.GetAsync(path)).StatusCode);
    }
}
