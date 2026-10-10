using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.Modules.Identity;
using Nexora.Modules.Membership.Domain;
using Nexora.Modules.Membership.Infrastructure;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;

public sealed partial class MembershipAdministrationTests
{
    [Fact]
    public async Task Member_type_has_independent_effective_history_and_does_not_change_product_or_rate()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app); var (contact, member) = await CreateMember(client);
        var type = await Json(await Send(client, "/api/v1/membership/setup/member-type", HttpMethod.Post, new { name = "Professional", description = "" }));
        var category = await Json(await Send(client, "/api/v1/membership/setup/member-category", HttpMethod.Post, new { name = "Different lookup", description = "" }));
        var path = $"/api/v1/membership/members/{Id(member)}";
        var initial = await Json(await client.GetAsync(path + "/classifications")); Assert.Null(initial["current"]!["memberType"]!["name"]);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path + "/classifications/member-type", HttpMethod.Post, new { version = Version(member), definitionId = Id(category), effectiveOn = "2001-02-03", reason = "Wrong lookup" })).StatusCode);
        var saved = await Json(await Send(client, path + "/classifications/member-type", HttpMethod.Post, new { version = Version(member), definitionId = Id(type), effectiveOn = "2001-02-03", reason = "Historical type evidence" }));
        var current = await Json(await client.GetAsync(path + "/classifications")); Assert.Equal("Professional", current["current"]!["memberType"]!["name"]!.GetValue<string>()); Assert.Null(current["current"]!["category"]!["name"]);
        var projection = (await Json(await client.GetAsync($"/api/v1/crm/records/{Id(contact)}/memberships")))["items"]![0]!; Assert.Equal("Professional", projection["classifications"]!["memberType"]!["name"]!.GetValue<string>()); Assert.Equal(125, projection["rate"]!.GetValue<decimal>());
        await Json(await Send(client, path + "/classifications/member-type", HttpMethod.Post, new { version = Version(saved), definitionId = (Guid?)null, effectiveOn = "2001-02-03", reason = "Same-day correction" }));
        current = await Json(await client.GetAsync(path + "/classifications")); Assert.Null(current["current"]!["memberType"]!["name"]); Assert.Equal(2, current["total"]!.GetValue<int>()); Assert.Contains("Professional", current["items"]!.ToJsonString());
    }

    [Fact]
    public async Task Joining_exemption_and_free_membership_use_distinct_quotes_and_preserve_existing_snapshots()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app); var (_, member) = await CreateMember(client);
        var productId = member["productId"]!.GetValue<string>(); var policyPath = $"/api/v1/membership/products/{productId}";
        var policy = await Json(await Send(client, policyPath + "/joining-fee", HttpMethod.Put, new { version = (Guid?)null, joiningFee = 25, reason = "Explicit joining fee" }));
        foreach (var invalid in new[] { -1m, 1.234m, 100000000m }) Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, policyPath + "/joining-fee", HttpMethod.Put, new { version = Version(policy), joiningFee = invalid, reason = "Invalid amount" })).StatusCode);
        var person = await Contact(client); var application = await Json(await Send(client, "/api/v1/membership/applications", HttpMethod.Post, new { contactId = Id(person), productId, applicationType = "individual", startsAtUtc = DateTime.UtcNow.Date, termsAccepted = true })); Assert.Equal(150, application["quotedRate"]!.GetValue<decimal>());
        var exemptPerson = await Contact(client); var exempt = await Json(await Send(client, "/api/v1/membership/applications", HttpMethod.Post, new { contactId = Id(exemptPerson), productId, applicationType = "individual", startsAtUtc = DateTime.UtcNow.Date, termsAccepted = true, exemptJoiningFee = true })); Assert.Equal(125, exempt["quotedRate"]!.GetValue<decimal>());
        var freePerson = await Contact(client); var free = await Json(await Send(client, "/api/v1/membership/applications", HttpMethod.Post, new { contactId = Id(freePerson), productId, applicationType = "individual", startsAtUtc = DateTime.UtcNow.Date, termsAccepted = true, freeMembership = true })); Assert.Equal(0, free["quotedRate"]!.GetValue<decimal>());
        var changed = await Json(await Send(client, policyPath + "/joining-fee", HttpMethod.Put, new { version = Version(policy), joiningFee = 35, reason = "Future quotes only" }));
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, policyPath + "/joining-fee", HttpMethod.Put, new { version = Version(policy), joiningFee = 45, reason = "Stale" })).StatusCode);
        var approved = await Json(await Send(client, $"/api/v1/membership/applications/{Id(application)}/decision", HttpMethod.Post, new { version = Version(application), decision = "approve", reason = "Snapshot approval" })); var next = (await Json(await client.GetAsync($"/api/v1/membership/members/{approved["membershipId"]!.GetValue<string>()}")))["member"]!; Assert.Equal(150, next["rate"]!.GetValue<decimal>());
        var freeApproved = await Json(await Send(client, $"/api/v1/membership/applications/{Id(free)}/decision", HttpMethod.Post, new { version = Version(free), decision = "approve", reason = "Free approval" })); var freeMember = (await Json(await client.GetAsync($"/api/v1/membership/members/{freeApproved["membershipId"]!.GetValue<string>()}")))["member"]!; Assert.True(freeMember["freeMembership"]!.GetValue<bool>());
        await Json(await Send(client, policyPath + "/policy", HttpMethod.Put, new { version = Version(changed), renewalMode = "rolling", yearEndMonth = 12, yearEndDay = 31, prorate = false, processingFee = 0, credit = 0, requirePayment = true, requireCompliance = false, requireDeclaration = false }));
        Assert.Equal(35, (await Json(await client.GetAsync(policyPath + "/policy")))["joiningFee"]!.GetValue<decimal>());
        var renewed = await Json(await Send(client, $"/api/v1/membership/members/{Id(freeMember)}/renew", HttpMethod.Post, new { version = Version(freeMember), reason = "Free renewal without artificial payment verification" })); Assert.Equal(0, renewed["rate"]!.GetValue<decimal>());
    }

    [Fact]
    public async Task Lapse_exclusion_is_nullable_versioned_and_enforced_without_changing_existing_rates()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app); var (contact, member) = await CreateMember(client); var path = $"/api/v1/membership/members/{Id(member)}";
        var initial = await Json(await client.GetAsync(path + "/options")); Assert.All(initial["options"]!.AsObject(), property => Assert.Null(property.Value));
        var saved = await Json(await Send(client, path + "/options", HttpMethod.Put, new { version = Version(initial), options = new { excludedFromLapse = true, freeMembership = false, exemptJoiningFee = true }, reason = "Protect from automatic lapse" }));
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, path + "/options", HttpMethod.Put, new { version = Version(initial), options = new { }, reason = "Stale" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path + "/options", HttpMethod.Put, new { version = Version(saved), options = new { }, reason = " " })).StatusCode);
        var session = (await client.GetFromJsonAsync<SessionResponse>("/api/v1/auth/session"))!;
        using (var scope = app.Services.CreateScope())
        {
            var scoped = scope.ServiceProvider.GetRequiredService<MembershipDbContext>(); var options = new DbContextOptionsBuilder<MembershipDbContext>().UseSqlite(scoped.Database.GetDbConnection()).Options;
            await using var db = new MembershipDbContext(options, new LifecycleTestIdentity(session.TenantId, session.UserId)); var row = await db.Set<MemberSubscription>().SingleAsync(x => x.Id == Guid.Parse(Id(member))); row.StartsAtUtc = DateTime.UtcNow.Date.AddYears(-2); row.EndsAtUtc = DateTime.UtcNow.Date.AddYears(-1); await db.SaveChangesAsync();
        }
        var current = (await Json(await client.GetAsync(path)))["member"]!; Assert.Equal(125, current["rate"]!.GetValue<decimal>());
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path + "/lapse", HttpMethod.Post, new { version = Version(current), reason = "Excluded lapse" })).StatusCode);
        var projection = (await Json(await client.GetAsync($"/api/v1/crm/records/{Id(contact)}/memberships")))["items"]![0]!; Assert.True(projection["options"]!["excludedFromLapse"]!.GetValue<bool>());
        saved = await Json(await Send(client, path + "/options", HttpMethod.Put, new { version = Version(current), options = new { }, reason = "Clear exclusion with recorded evidence" }));
        var lapsed = await Json(await Send(client, path + "/lapse", HttpMethod.Post, new { version = Version(saved), reason = "Expired membership" })); Assert.Equal("lapsed", lapsed["status"]!.GetValue<string>());
    }
}
