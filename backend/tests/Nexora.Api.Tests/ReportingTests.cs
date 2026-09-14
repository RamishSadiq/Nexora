using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.Modules.Identity.Domain;
using Nexora.Modules.Identity.Infrastructure.Persistence;
using static Nexora.Api.Tests.WorkflowTestClient;
namespace Nexora.Api.Tests;
public sealed class ReportingTests
{
 [Fact]
 public async Task Reports_apply_dataset_permissions_tenant_scope_filters_and_limits()
 {
  using var app=new IdentityApiFactory();using var client=await Login(app);await Contact(client);
  var report=await Json(await Send(client,"/api/v1/insights/query/contacts",HttpMethod.Post,new{search="Workflow",take=10}));Assert.Equal(1,report["total"]!.GetValue<int>());
  Assert.Equal(HttpStatusCode.BadRequest,(await Send(client,"/api/v1/insights/query/contacts",HttpMethod.Post,new{take=100000})).StatusCode);
  Assert.Equal(HttpStatusCode.NotFound,(await Send(client,"/api/v1/insights/query/arbitrary-table",HttpMethod.Post,new{take=10})).StatusCode);
  await using(var scope=app.Services.CreateAsyncScope()){var db=scope.ServiceProvider.GetRequiredService<NexoraIdentityDbContext>();using var access=db.BeginSystemAccess();var role=await db.Roles.SingleAsync(x=>x.Name=="Outside Administrator");db.RolePermissions.Add(new RolePermission{RoleId=role.Id,Permission="insights.read"});await db.SaveChangesAsync();}
  using var outside=await Login(app,"outside@nexora.test");var datasets=(await Json(await outside.GetAsync("/api/v1/insights/datasets"))).AsArray();Assert.Single(datasets);Assert.Equal("contacts",datasets[0]!["key"]!.GetValue<string>());
  Assert.Equal(0,(await Json(await Send(outside,"/api/v1/insights/query/contacts",HttpMethod.Post,new{take=10})))["total"]!.GetValue<int>());
  Assert.Equal(HttpStatusCode.NotFound,(await Send(outside,"/api/v1/insights/query/tasks",HttpMethod.Post,new{take=10})).StatusCode);
  Assert.Equal(HttpStatusCode.Forbidden,(await Send(outside,"/api/v1/insights/export/contacts",HttpMethod.Post,new{take=10})).StatusCode);
  var exported=await Send(client,"/api/v1/insights/export/contacts",HttpMethod.Post,new{take=10});Assert.Equal(HttpStatusCode.OK,exported.StatusCode);Assert.Contains("Workflow Contact",await exported.Content.ReadAsStringAsync());
  var ready=await client.GetAsync("/health/ready");Assert.Equal(HttpStatusCode.OK,ready.StatusCode);Assert.True(ready.Headers.Contains("X-Request-ID"));
 }
}
