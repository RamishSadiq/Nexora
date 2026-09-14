using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Security;
using Nexora.BuildingBlocks.Persistence;
using Nexora.Modules.Work.Domain;
using Nexora.Modules.Work.Infrastructure;
using Nexora.Modules.Engagement.Domain;
using Nexora.Modules.Engagement.Infrastructure;
using System.Net;
using static Nexora.Api.Tests.WorkflowTestClient;
namespace Nexora.Api.Tests;
public sealed class EngagementWorkTests
{
 [Fact]
 public async Task Campaign_excludes_missing_and_denied_consent_and_contributions_cannot_duplicate()
 {
  using var app=new IdentityApiFactory();using var client=await Login(app);var contact=await Contact(client);var blocked=await Contact(client);
  foreach(var c in new[]{contact,blocked})await Json(await Send(client,"/api/v1/crm/records/"+Id(c)+"/communications",HttpMethod.Post,new{kind="email",value="member@example.test",consent="granted",consentEvidence="Explicit opt in"}));
  await Json(await Send(client,"/api/v1/crm/records/"+Id(blocked)+"/communications",HttpMethod.Post,new{kind="email",value="blocked@example.test",consent="denied",consentEvidence="Unsubscribed"}));
  var group=await Json(await Send(client,"/api/v1/engagement/communities",HttpMethod.Post,new{kind="committee",name="Council",purpose="Governance"}));
  foreach(var c in new[]{contact,blocked})await Json(await Send(client,"/api/v1/engagement/communities/"+Id(group)+"/members",HttpMethod.Post,new{contactId=Id(c),role="Member"}));
  var campaign=await Json(await Send(client,"/api/v1/engagement/campaigns",HttpMethod.Post,new{communityId=Id(group),name="Update",subject="News"}));
  var result=await Json(await Send(client,"/api/v1/engagement/campaigns/"+Id(campaign)+"/prepare",HttpMethod.Post,new{version=Version(campaign)}));Assert.Equal(1,result["included"]!.GetValue<int>());Assert.Equal(1,result["excluded"]!.GetValue<int>());
  Assert.Equal(HttpStatusCode.Conflict,(await Send(client,"/api/v1/engagement/campaigns/"+Id(campaign)+"/prepare",HttpMethod.Post,new{version=Version(campaign)})).StatusCode);
  var fund=await Json(await Send(client,"/api/v1/engagement/funds",HttpMethod.Post,new{name="Community",currency="GBP",target=1000}));
  var contribution=new{contactId=Id(contact),kind="donation",amount=20,reference="BANK-1"};await Json(await Send(client,"/api/v1/engagement/funds/"+Id(fund)+"/contributions",HttpMethod.Post,contribution));Assert.Equal(HttpStatusCode.Conflict,(await Send(client,"/api/v1/engagement/funds/"+Id(fund)+"/contributions",HttpMethod.Post,contribution)).StatusCode);
  var totals=await Json(await client.GetAsync("/api/v1/engagement/funds/"+Id(fund)));Assert.Equal(20,totals["donations"]!.GetValue<decimal>());Assert.Equal(0,totals["pledges"]!.GetValue<decimal>());
 }
 [Fact]
 public async Task Task_import_validates_before_commit_and_blocks_replay_and_unprivileged_access()
 {
  using var app=new IdentityApiFactory();using var client=await Login(app);
  Assert.Equal(HttpStatusCode.BadRequest,(await Send(client,"/api/v1/work/imports/preview",HttpMethod.Post,new{name="Bad",rows=new[]{new{title="Valid",priority="normal"},new{title="Invalid",priority="urgent"}}})).StatusCode);
  Assert.Empty((await Json(await client.GetAsync("/api/v1/work/items"))).AsArray());Assert.Empty((await Json(await client.GetAsync("/api/v1/work/imports"))).AsArray());
  var preview=await Json(await Send(client,"/api/v1/work/imports/preview",HttpMethod.Post,new{name="Tasks",rows=new[]{new{title="=formula",priority="normal"}}}));var batch=preview["batch"]!;
  Assert.Empty((await Json(await client.GetAsync("/api/v1/work/items"))).AsArray());
  await Json(await Send(client,"/api/v1/work/imports/"+Id(batch)+"/commit",HttpMethod.Post,new{version=Version(batch)}));Assert.Equal(HttpStatusCode.Conflict,(await Send(client,"/api/v1/work/imports/"+Id(batch)+"/commit",HttpMethod.Post,new{version=Version(batch)})).StatusCode);
  Assert.Single((await Json(await client.GetAsync("/api/v1/work/items"))).AsArray());Assert.Contains("'=formula",await client.GetStringAsync("/api/v1/work/export"));
  using var outside=await Login(app,"outside@nexora.test");Assert.Equal(HttpStatusCode.Forbidden,(await outside.GetAsync("/api/v1/work/items")).StatusCode);
  Assert.Equal(HttpStatusCode.BadRequest,(await Send(client,"/api/v1/work/items",HttpMethod.Post,new{title="Task",status="open",priority="normal",assigneeUserId=Guid.NewGuid()})).StatusCode);
 }
 private sealed record Identity(Guid? TenantId,Guid? UserId):IRequestIdentity;
 [Fact]
 public async Task Persistence_rejects_foreign_reads_and_forged_writes_for_engagement_and_work()
 {
  using var app=new IdentityApiFactory();using var client=await Login(app);
  var task=await Json(await Send(client,"/api/v1/work/items",HttpMethod.Post,new{title="Owned",status="open",priority="normal"}));
  var group=await Json(await Send(client,"/api/v1/engagement/communities",HttpMethod.Post,new{name="Owned",kind="group",purpose="Private"}));
  await using var scope=app.Services.CreateAsyncScope();var identity=new Identity(Guid.NewGuid(),Guid.NewGuid());
  var workOptions=new DbContextOptionsBuilder<WorkDbContext>().UseSqlite(scope.ServiceProvider.GetRequiredService<WorkDbContext>().Database.GetDbConnection()).Options;
  await using var work=new WorkDbContext(workOptions,identity);Assert.Empty(await work.Set<WorkItem>().ToListAsync());
  var forgedTask=new WorkItem{Id=Guid.Parse(Id(task)),TenantId=identity.TenantId!.Value,Title="Stolen",Version=Guid.Parse(Version(task))};work.Update(forgedTask);await Assert.ThrowsAsync<TenantBoundaryException>(()=>work.SaveChangesAsync());
  var engagementOptions=new DbContextOptionsBuilder<EngagementDbContext>().UseSqlite(scope.ServiceProvider.GetRequiredService<EngagementDbContext>().Database.GetDbConnection()).Options;
  await using var engagement=new EngagementDbContext(engagementOptions,identity);Assert.Empty(await engagement.Set<Community>().ToListAsync());
  engagement.Update(new Community{Id=Guid.Parse(Id(group)),TenantId=identity.TenantId.Value,Name="Stolen",Version=Guid.Parse(Version(group))});await Assert.ThrowsAsync<TenantBoundaryException>(()=>engagement.SaveChangesAsync());
 }
 [Fact]
 public async Task Assignment_notifications_are_private_even_between_users_in_one_tenant()
 {
  using var app=new IdentityApiFactory();using var client=await Login(app);var directory=await Json(await client.GetAsync("/api/v1/work/directory"));var user=directory["users"]!.AsArray()[0]!;
  var task=await Json(await Send(client,"/api/v1/work/items",HttpMethod.Post,new{title="Private task",status="open",priority="normal",assigneeUserId=Id(user)}));
  var notes=(await Json(await client.GetAsync("/api/v1/work/notifications"))).AsArray();Assert.Single(notes);var note=notes[0]!;
  await using var scope=app.Services.CreateAsyncScope();var options=new DbContextOptionsBuilder<WorkDbContext>().UseSqlite(scope.ServiceProvider.GetRequiredService<WorkDbContext>().Database.GetDbConnection()).Options;
  var other=new Identity(Guid.Parse(task["tenantId"]!.GetValue<string>()),Guid.NewGuid());await using var db=new WorkDbContext(options,other);
  Assert.Empty(await db.Set<Notification>().ToListAsync());
  db.Update(new Notification{Id=Guid.Parse(Id(note)),TenantId=other.TenantId!.Value,UserId=other.UserId!.Value,WorkItemId=Guid.Parse(Id(task)),IsRead=true,Version=Guid.Parse(Version(note))});
  await Assert.ThrowsAsync<TenantBoundaryException>(()=>db.SaveChangesAsync());
  Assert.Equal(HttpStatusCode.NoContent,(await Send(client,"/api/v1/work/notifications/"+Id(note)+"/read",HttpMethod.Post)).StatusCode);
 }
}
