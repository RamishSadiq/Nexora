using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using Nexora.BuildingBlocks.Documents;
using Nexora.Modules.Identity;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;

public sealed class LocalScannerFactAttribute : FactAttribute
{
    public LocalScannerFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NEXORA_TEST_CLAMAV_HOST")))
            Skip = "Requires a live scanner; set NEXORA_TEST_CLAMAV_HOST to enable this integration check.";
    }
}

public sealed class LocalScannerTheoryAttribute : TheoryAttribute
{
    public LocalScannerTheoryAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NEXORA_TEST_CLAMAV_HOST")))
            Skip = "Requires a live scanner; set NEXORA_TEST_CLAMAV_HOST to enable this integration check.";
    }
}

public sealed class LocalDocumentScannerTests
{
    private static readonly byte[] Image = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII=");
    private static ClamDocumentScanner Scanner() => new(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Documents:ClamAv:Host"] = Environment.GetEnvironmentVariable("NEXORA_TEST_CLAMAV_HOST"),
        ["Documents:ClamAv:Port"] = "3310"
    }).Build());

    [LocalScannerFact]
    public async Task Live_scanner_accepts_png_and_rejects_the_benign_standard_antivirus_test_sample()
    {
        var scanner = Scanner();
        Assert.Equal("clean", await scanner.ScanAsync(Image, CancellationToken.None));
        // EICAR is a harmless industry-standard antivirus test string, assembled in memory.
        var sample = string.Concat("X5O!P%@AP[4\\PZX54(P^)7CC)7}", "$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*");
        Assert.Equal("rejected", await scanner.ScanAsync(Encoding.ASCII.GetBytes(sample), CancellationToken.None));
    }

    [LocalScannerTheory]
    [InlineData("contact")]
    [InlineData("account")]
    public async Task Photo_upload_with_live_scanning_persists_and_renders_in_an_isolated_test_database(string kind)
    {
        using var app = new IdentityApiFactory(Scanner());
        using var client = await Login(app);
        var record = await Json(await Send(client, "/api/v1/crm/records", HttpMethod.Post, new { kind, name = "Live scanner photo check", status = "active" }), HttpStatusCode.Created);
        var path = "/api/v1/crm/records/" + Id(record);
        var csrf = await client.GetFromJsonAsync<CsrfResponse>("/api/v1/auth/csrf");
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(Version(record)), "version");
        form.Add(new ByteArrayContent(Image), "file", "avatar.png");
        using var request = new HttpRequestMessage(HttpMethod.Post, path + "/photo") { Content = form };
        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(request)).StatusCode);
        var saved = await Json(await client.GetAsync(path));
        Assert.NotNull(saved["record"]!["photoFileId"]);
        Assert.NotEqual(Version(record), Version(saved["record"]!));
        using var photo = await client.GetAsync(path + "/photo");
        Assert.Equal(HttpStatusCode.OK, photo.StatusCode);
        Assert.Equal("image/png", photo.Content.Headers.ContentType!.MediaType);
        Assert.Equal(Image, await photo.Content.ReadAsByteArrayAsync());
    }
}
