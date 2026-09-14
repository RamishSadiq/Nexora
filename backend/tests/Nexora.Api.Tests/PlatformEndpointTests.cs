using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Nexora.BuildingBlocks.Platform;

namespace Nexora.Api.Tests;

public sealed class PlatformEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PlatformEndpointTests(WebApplicationFactory<Program> application)
    {
        _client = application.CreateClient();
    }

    [Fact]
    public async Task Platform_endpoint_describes_the_running_product()
    {
        var response = await _client.GetAsync("/api/v1/platform");
        var platform = await response.Content.ReadFromJsonAsync<PlatformDescriptor>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(platform);
        Assert.Equal("Nexora", platform.Product);
        Assert.Contains("platform-shell", platform.Capabilities);
    }
}
