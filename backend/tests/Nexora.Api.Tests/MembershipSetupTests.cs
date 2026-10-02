using System.Net;
using System.Net.Http.Json;
using static Nexora.Api.Tests.WorkflowTestClient;
namespace Nexora.Api.Tests;
public sealed class MembershipSetupTests
{
 [Fact]
 public async Task Lookup_lists_persist_separately_validate_input_and_guard_versions()
 {
  using var app = new IdentityApiFactory(); using var client = await Login(app);
  string[] kinds = ["member-type", "member-category", "member-subscription-band", "member-rate-type", "member-rate", "member-status", "member-application-status"];
  foreach (var kind in kinds)
  {
   var path = "/api/v1/membership/setup/" + kind;
   var row = await Json(await Send(client, path, HttpMethod.Post, new {name = "Standard", description = "Initial description"}));
   var changed = await Json(await Send(client, path + "/" + Id(row), HttpMethod.Patch, new {name = "Updated", description = "Updated description", version = Version(row)}));
   Assert.Equal("Updated description", changed["description"]!.GetValue<string>());
   Assert.Equal(HttpStatusCode.Conflict, (await Send(client, path + "/" + Id(row), HttpMethod.Patch, new {name = "Stale", description = "", version = Version(row)})).StatusCode);
   var list = (await Json(await client.GetAsync(path))).AsArray(); Assert.Single(list); Assert.Equal("Updated", list[0]!["name"]!.GetValue<string>());
   Assert.Equal(HttpStatusCode.Conflict, (await Send(client, path, HttpMethod.Post, new {name = "Updated", description = "Duplicate"})).StatusCode);
  }
  Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/membership/setup/unknown")).StatusCode);
  Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, "/api/v1/membership/setup/member-type", HttpMethod.Post, new {name = " ", description = ""})).StatusCode);
  Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, "/api/v1/membership/setup/member-type", HttpMethod.Post, new {name = "Too long", description = new string('a',2001)})).StatusCode);
  Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/membership/setup/member-type", new {name = "No CSRF", description = ""})).StatusCode);
  using var outside = await Login(app, "outside@nexora.test"); Assert.Equal(HttpStatusCode.Forbidden, (await outside.GetAsync("/api/v1/membership/setup/member-type")).StatusCode);
  Assert.Empty((await Json(await client.GetAsync("/api/v1/membership/products"))).AsArray());
 }
}
