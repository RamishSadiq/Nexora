using System.Buffers.Binary;
using System.ComponentModel.DataAnnotations;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Nexora.BuildingBlocks.Documents;

public sealed record DocumentUpload(string Name, byte[] Content);
public interface IDocumentScanner
{
    // Only an explicit clean result permits download. Unavailable includes malformed replies.
    Task<string> ScanAsync(byte[] content, CancellationToken ct);
}

public static class DocumentSafety
{
    public const int MaxBytes = 5 * 1024 * 1024;
    public static async Task<DocumentUpload> ReadAsync(HttpRequest request, CancellationToken ct)
    {
        if (!request.HasFormContentType || request.ContentLength is null or <= 0 or > MaxBytes + 65536)
            throw new ValidationException("Upload one file up to 5 MB.");
        var form = await request.ReadFormAsync(ct);
        if (form.Files.Count != 1 || form.Files[0].Name != "file" || form.Count != 1 || !form.ContainsKey("version"))
            throw new ValidationException("Only a file and the current draft version are accepted.");
        var file = form.Files[0];
        var name = Path.GetFileName(file.FileName.Replace('\\', '/')).Trim();
        if (name.Length is 0 or > 160 || name.Any(char.IsControl) || file.Length is <= 0 or > MaxBytes)
            throw new ValidationException("Use a filename up to 160 characters and a file up to 5 MB.");
        await using var input = file.OpenReadStream();
        using var output = new MemoryStream();
        var buffer = new byte[16384];
        int count;
        while ((count = await input.ReadAsync(buffer, ct)) > 0)
        {
            if (output.Length + count > MaxBytes) throw new ValidationException("Choose a file up to 5 MB.");
            output.Write(buffer, 0, count);
        }
        var bytes = output.ToArray();
        var valid = Path.GetExtension(name).ToLowerInvariant() switch
        {
            ".pdf" => bytes.AsSpan().StartsWith("%PDF-"u8),
            ".png" => bytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            ".jpg" or ".jpeg" => bytes.AsSpan().StartsWith(new byte[] { 255, 216, 255 }),
            ".txt" => IsText(bytes),
            _ => false
        };
        if (!valid) throw new ValidationException("Choose a PDF, PNG, JPEG or UTF-8 text file with matching content.");
        return new(name, bytes);
    }
    private static bool IsText(byte[] bytes)
    {
        try { return bytes.Length > 0 && !new UTF8Encoding(false, true).GetString(bytes).Any(c => char.IsControl(c) && c is not '\r' and not '\n' and not '\t'); }
        catch (DecoderFallbackException) { return false; }
    }
    public static IResult Download(HttpContext http, string name, byte[] content)
    {
        http.Response.Headers.CacheControl = "no-store";
        http.Response.Headers["X-Content-Type-Options"] = "nosniff";
        http.Response.Headers.ContentSecurityPolicy = "sandbox";
        return Results.File(content, "application/octet-stream", name, enableRangeProcessing: false);
    }
}

// Protocol: https://docs.clamav.net/manual/Usage/ClamdProtocol.html
// Host/port come only from trusted server configuration, never from an upload.
public sealed class ClamDocumentScanner(IConfiguration configuration) : IDocumentScanner
{
    public async Task<string> ScanAsync(byte[] content, CancellationToken ct)
    {
        var host = configuration["Documents:ClamAv:Host"];
        if (string.IsNullOrWhiteSpace(host)) return "quarantined";
        var port = configuration.GetValue("Documents:ClamAv:Port", 3310);
        if (port is < 1 or > 65535) return "quarantined";
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(host, port, timeout.Token);
            await using var stream = client.GetStream();
            await stream.WriteAsync("zINSTREAM\0"u8.ToArray(), timeout.Token);
            var size = new byte[4];
            for (var offset = 0; offset < content.Length; offset += 16384)
            {
                var length = Math.Min(16384, content.Length - offset);
                BinaryPrimitives.WriteInt32BigEndian(size, length);
                await stream.WriteAsync(size, timeout.Token);
                await stream.WriteAsync(content.AsMemory(offset, length), timeout.Token);
            }
            await stream.WriteAsync(new byte[4], timeout.Token);
            var reply = new List<byte>();
            var one = new byte[1];
            while (reply.Count < 1024 && await stream.ReadAsync(one, timeout.Token) == 1)
            {
                if (one[0] == 0)
                {
                    var result = Encoding.UTF8.GetString(reply.ToArray());
                    return result == "stream: OK" ? "clean" : result.StartsWith("stream: ") && result.EndsWith(" FOUND") ? "rejected" : "quarantined";
                }
                reply.Add(one[0]);
            }
            return "quarantined";
        }
        catch (Exception e) when (e is SocketException or IOException || e is OperationCanceledException && !ct.IsCancellationRequested)
        { return "quarantined"; }
    }
}
