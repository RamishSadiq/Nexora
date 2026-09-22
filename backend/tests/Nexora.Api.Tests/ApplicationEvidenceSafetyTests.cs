using System.Net.Http.Headers;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Nexora.BuildingBlocks.Documents;

namespace Nexora.Api.Tests;

public sealed class ApplicationEvidenceSafetyTests
{
    [Fact]
    public async Task Read_accepts_matching_pdf_signature_and_normalises_filename()
    {
        var request = Request("%PDF-1.7\nbody"u8.ToArray(), "..\\proof.pdf", "pdf-version");
        var upload = await DocumentSafety.ReadAsync(request, CancellationToken.None);

        Assert.Equal("proof.pdf", upload.Name);
        Assert.Equal("%PDF-1.7\nbody", System.Text.Encoding.UTF8.GetString(upload.Content));
    }

    [Theory]
    [InlineData("proof.pdf", "plain text")]
    [InlineData("proof.png", "not a png")]
    [InlineData("proof.exe", "MZ")]
    public async Task Read_rejects_unsupported_or_mismatched_content(string name, string content)
    {
        var request = Request(System.Text.Encoding.UTF8.GetBytes(content), name, "version");
        await Assert.ThrowsAsync<ValidationException>(() => DocumentSafety.ReadAsync(request, CancellationToken.None));
    }

    [Fact]
    public void Download_requires_clean_scan_and_sets_no_store_headers()
    {
        var context = new DefaultHttpContext();
        var result = DocumentSafety.Download(context, "evidence.txt", "safe"u8.ToArray());

        Assert.NotNull(result);
        Assert.Equal("no-store", context.Response.Headers.CacheControl.ToString());
        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"].ToString());
        Assert.Equal("sandbox", context.Response.Headers.ContentSecurityPolicy.ToString());
    }

    private static HttpRequest Request(byte[] bytes, string name, string version)
    {
        var context = new DefaultHttpContext();
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(file, "file", name);
        form.Add(new StringContent(version), "version");
        context.Request.ContentType = form.Headers.ContentType!.ToString();
        context.Request.ContentLength = form.Headers.ContentLength;
        context.Request.Body = form.ReadAsStream();
        return context.Request;
    }
}
