using System.Net;
using System.Net.Http.Json;
using Nexora.Modules.Identity;
using static Nexora.Api.Tests.WorkflowTestClient;
namespace Nexora.Api.Tests;
public sealed class RecordPresentationTests
{
 [Fact]
 public async Task Contact_audit_includes_field_values_dates_and_rejects_other_tenants()
 {
  using var app=new IdentityApiFactory();using var client=await Login(app);var record=await Contact(client);var path="/api/v1/crm/records/"+Id(record);
  await Json(await Send(client,path,HttpMethod.Patch,new{kind="contact",name="Updated label",status="lapsed",version=Version(record)}));
  var audit=await Json(await client.GetAsync(path+"/data-audit"));Assert.True(audit["total"]!.GetValue<int>()>=2);Assert.Contains("Workflow Contact",audit.ToJsonString());Assert.Contains("Updated label",audit.ToJsonString());Assert.Contains("lapsed",audit.ToJsonString());Assert.NotNull(audit["latestAtUtc"]);
  var empty=await Json(await client.GetAsync(path+"/data-audit?fromUtc=2100-01-01T00:00:00Z"));Assert.Equal(0,empty["total"]!.GetValue<int>());
  using var outside=await Login(app,"outside@nexora.test");Assert.Equal(HttpStatusCode.NotFound,(await outside.GetAsync(path+"/data-audit")).StatusCode);
  Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync(path+"/photo")).StatusCode);
  var csrf=await client.GetFromJsonAsync<CsrfResponse>("/api/v1/auth/csrf");using var body=new MultipartFormDataContent();body.Add(new StringContent(Version(record)),"version");body.Add(new ByteArrayContent("not an image"u8.ToArray()),"file","bad.png");using var request=new HttpRequestMessage(HttpMethod.Post,path+"/photo"){Content=body};request.Headers.Add("X-CSRF-TOKEN",csrf!.Token);Assert.Equal(HttpStatusCode.BadRequest,(await client.SendAsync(request)).StatusCode);
 }

 private sealed class Scanner(string result):Nexora.BuildingBlocks.Documents.IDocumentScanner { public Task<string> ScanAsync(byte[] content,CancellationToken ct)=>Task.FromResult(result); }
 [Theory]
 [InlineData("clean",HttpStatusCode.OK)]
 [InlineData("unavailable",HttpStatusCode.BadRequest)]
 public async Task Contact_photo_requires_clean_scan_and_uses_record_version(string scan,HttpStatusCode expected)
 {
  using var app=new IdentityApiFactory(new Scanner(scan));using var client=await Login(app);var record=await Contact(client);var path="/api/v1/crm/records/"+Id(record);
  var csrf=await client.GetFromJsonAsync<CsrfResponse>("/api/v1/auth/csrf");var bytes=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII=");
  using var body=new MultipartFormDataContent();body.Add(new StringContent(Version(record)),"version");body.Add(new ByteArrayContent(bytes),"file","avatar.png");using var request=new HttpRequestMessage(HttpMethod.Post,path+"/photo"){Content=body};request.Headers.Add("X-CSRF-TOKEN",csrf!.Token);
  Assert.Equal(expected,(await client.SendAsync(request)).StatusCode);
  if(scan=="clean") { var photo=await client.GetAsync(path+"/photo");Assert.Equal(HttpStatusCode.OK,photo.StatusCode);Assert.Equal("image/png",photo.Content.Headers.ContentType!.MediaType);Assert.Equal(bytes,await photo.Content.ReadAsByteArrayAsync());var detail=await Json(await client.GetAsync(path));Assert.NotNull(detail["record"]!["photoFileId"]);Assert.NotEqual(Version(record),Version(detail["record"]!));using var outside=await Login(app,"outside@nexora.test");Assert.Equal(HttpStatusCode.Forbidden,(await outside.GetAsync(path+"/photo")).StatusCode); }
 }
}
