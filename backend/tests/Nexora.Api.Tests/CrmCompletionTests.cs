using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Documents;
using Nexora.Modules.Crm.Domain;
using Nexora.Modules.Crm.Infrastructure;
using Nexora.Modules.Identity;
using Nexora.Modules.Identity.Infrastructure.Persistence;
namespace Nexora.Api.Tests;
public sealed partial class CrmEndpointTests
{
    private sealed class CrmTestScanner : IDocumentScanner { public string Status = "quarantined"; public Task<string> ScanAsync(byte[] content, CancellationToken ct) => Task.FromResult(Status); }
    [Fact]
    public async Task Import_preview_is_read_only_commit_is_atomic_and_reconciles_duplicates_versions_and_tenants()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app); using var outside = await Login(app, "outside@nexora.test");
        const string header = "id,version,kind,name,status,category,email\n";
        var csv = header + ",,contact,Imported Person,active,Member,person@example.test";
        var preview = await Json(await Send(client, "/api/v1/crm/import", HttpMethod.Post, new { csv }));
        Assert.True(preview["valid"]!.GetValue<bool>()); Assert.Empty((await Json(await client.GetAsync("/api/v1/crm/records")))["items"]!.AsArray());
        Assert.True((await Json(await Send(client, "/api/v1/crm/import", HttpMethod.Post, new { csv, commit = true })))["committed"]!.GetValue<bool>());
        Assert.False((await Json(await Send(client, "/api/v1/crm/import", HttpMethod.Post, new { csv, commit = true })))["valid"]!.GetValue<bool>());
        var blocked = header + ",,contact,Should Roll Back,active,,other@example.test\n,,contact,Duplicate Email,active,,person@example.test";
        Assert.False((await Json(await Send(client, "/api/v1/crm/import", HttpMethod.Post, new { csv = blocked, commit = true })))["committed"]!.GetValue<bool>());
        Assert.Single((await Json(await client.GetAsync("/api/v1/crm/records")))["items"]!.AsArray());
        var exported = await client.GetStringAsync("/api/v1/crm/export?kind=contact");
        var update = exported.Replace("Imported Person", "Updated Person");
        Assert.True((await Json(await Send(client, "/api/v1/crm/import", HttpMethod.Post, new { csv = update, commit = true })))["committed"]!.GetValue<bool>());
        Assert.False((await Json(await Send(client, "/api/v1/crm/import", HttpMethod.Post, new { csv = update, commit = true })))["valid"]!.GetValue<bool>());
        Assert.Equal(HttpStatusCode.Forbidden, (await outside.GetAsync("/api/v1/crm/export?kind=contact")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(outside, "/api/v1/crm/import", HttpMethod.Post, new { csv, commit = true })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, "/api/v1/crm/import", HttpMethod.Post, new { csv = "invalid" })).StatusCode);
        await Create(client, "=Formula"); Assert.Contains("'=Formula", await client.GetStringAsync("/api/v1/crm/export?kind=contact"));
        Assert.Single((await Json(await client.GetAsync("/api/v1/crm/records?search=person%40example.test")))["items"]!.AsArray());
    }
    [Fact]
    public async Task Connected_context_enforces_source_permissions_and_tenant_scope()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app); using var outside = await Login(app, "outside@nexora.test");
        var record = await Create(client, "Connected Person"); var id = record["id"]!.GetValue<string>();
        var product = await Json(await Send(client, "/api/v1/membership/products", HttpMethod.Post, new { name = "Professional", category = "Personal", termMonths = 12, rate = 10, currency = "GBP", isActive = true }));
        await Json(await Send(client, "/api/v1/membership/applications", HttpMethod.Post, new { contactId = id, productId = product["id"]!.GetValue<string>(), startsAtUtc = DateTime.UtcNow.Date, applicationType = "individual", termsAccepted = true }));
        Assert.Single((await Json(await client.GetAsync("/api/v1/crm/records?search=Professional")))["items"]!.AsArray());
        var sections = (await Json(await client.GetAsync($"/api/v1/crm/records/{id}/context"))).AsArray();
        Assert.Single(sections.Single(x => x!["title"]!.GetValue<string>() == "Applications")!["items"]!.AsArray());
        Assert.Equal(HttpStatusCode.NotFound, (await outside.GetAsync($"/api/v1/crm/records/{id}/context")).StatusCode);
        using (var scope = app.Services.CreateScope())
        {
            var identity = scope.ServiceProvider.GetRequiredService<NexoraIdentityDbContext>(); using var system = identity.BeginSystemAccess();
            var user=await identity.Users.SingleAsync(u=>u.Email==IdentityApiFactory.AdminEmail);
            var originalRoles=await identity.UserRoles.Where(r=>r.UserId==user.Id).ToListAsync();
            var role=new Nexora.Modules.Identity.Domain.NexoraRole{Id=Guid.NewGuid(),TenantId=user.TenantId,Name="Limited CRM test",NormalizedName="LIMITED CRM TEST",IsSystem=false};identity.Roles.Add(role);
            var permissions=await identity.RolePermissions.Where(p=>originalRoles.Select(r=>r.RoleId).Contains(p.RoleId)&&p.Permission!="membership.read").Select(p=>p.Permission).Distinct().ToArrayAsync();
            foreach(var permission in permissions)identity.RolePermissions.Add(new Nexora.Modules.Identity.Domain.RolePermission{RoleId=role.Id,Permission=permission});
            identity.UserRoles.RemoveRange(originalRoles);identity.UserRoles.Add(new Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>{UserId=user.Id,RoleId=role.Id});await identity.SaveChangesAsync();
        }
        using var restrictedClient = await Login(app);
        Assert.Empty((await Json(await restrictedClient.GetAsync("/api/v1/crm/records?search=Professional")))["items"]!.AsArray());
        var restricted = (await Json(await restrictedClient.GetAsync($"/api/v1/crm/records/{id}/context"))).AsArray();
        Assert.DoesNotContain(restricted, x => x!["title"]!.GetValue<string>() is "Applications" or "Memberships" or "Submitted application documents");
    }
    [Fact]
    public async Task Crm_files_quarantine_rescan_validate_integrity_and_delete_with_versions()
    {
        var scanner = new CrmTestScanner(); using var app = new IdentityApiFactory(scanner); using var client = await Login(app);
        var record = await Create(client, "Documents"); var path = "/api/v1/crm/records/" + record["id"]!.GetValue<string>();
        var token = await client.GetFromJsonAsync<CsrfResponse>("/api/v1/auth/csrf");
        using var form = new MultipartFormDataContent(); form.Add(new ByteArrayContent("Safe text"u8.ToArray()), "file", "file.txt"); form.Add(new StringContent(record["version"]!.GetValue<string>()), "version");
        using var request = new HttpRequestMessage(HttpMethod.Post, path + "/files") { Content = form }; request.Headers.Add("X-CSRF-TOKEN", token!.Token);
        var file = await Json(await client.SendAsync(request)); var fileId = file["id"]!.GetValue<string>(); var filePath = path + "/files/" + fileId;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(filePath)).StatusCode);
        var metadata = (await Json(await client.GetAsync(path)))["files"]![0]!; scanner.Status = "clean";
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, filePath + "/scan", HttpMethod.Post, new { version = metadata["version"]!.GetValue<string>() })).StatusCode);
        var download = await client.GetAsync(filePath); Assert.Equal("Safe text", await download.Content.ReadAsStringAsync()); Assert.Equal("attachment", download.Content.Headers.ContentDisposition!.DispositionType);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, filePath, HttpMethod.Delete, new { version = metadata["version"]!.GetValue<string>() })).StatusCode);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Files SET Content = {new byte[] { 1, 2 }} WHERE Id = {Guid.Parse(fileId)}");
        }
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(filePath)).StatusCode);
        metadata = (await Json(await client.GetAsync(path)))["files"]![0]!;
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, filePath, HttpMethod.Delete, new { version = metadata["version"]!.GetValue<string>() })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(filePath)).StatusCode);
    }
}
