using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Identity;
using Nexora.Modules.Membership.Domain;
using Nexora.Modules.Membership.Infrastructure;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;

public sealed class MembershipPaginationTests
{
    [Theory]
    [InlineData("members", "active")]
    [InlineData("applications", "submitted")]
    public async Task Pages_cover_all_records_with_stable_order_filtered_totals_and_tenant_isolation(string resource, string status)
    {
        using var app = new IdentityApiFactory();
        using var client = await Login(app);
        using var outside = await Login(app, "outside@nexora.test");
        var primary = (await client.GetFromJsonAsync<SessionResponse>("/api/v1/auth/session"))!;
        var secondary = (await outside.GetFromJsonAsync<SessionResponse>("/api/v1/auth/session"))!;
        await Seed(app, primary, 225);
        await Seed(app, secondary, 7);
        var url = "/api/v1/membership/" + resource;
        var defaults = await Json(await client.GetAsync(url));
        Assert.Equal(1, defaults["page"]!.GetValue<int>());
        Assert.Equal(25, defaults["pageSize"]!.GetValue<int>());
        Assert.Equal(225, defaults["totalCount"]!.GetValue<int>());
        Assert.Equal(25, defaults["items"]!.AsArray().Count);
        var ids = new HashSet<string>();
        for (var page = 1; page <= 5; page++)
        {
            var path = $"{url}?page={page}&pageSize=50";
            var data = await Json(await client.GetAsync(path));
            var items = data["items"]!.AsArray();
            Assert.Equal(page == 5 ? 25 : 50, items.Count);
            var repeated = await Json(await client.GetAsync(path));
            Assert.Equal(items.Select(x => Id(x!)), repeated["items"]!.AsArray().Select(x => Id(x!)));
            foreach (var item in items) Assert.True(ids.Add(Id(item!)), "Duplicate across pages");
        }
        Assert.Equal(225, ids.Count);
        var filtered = await Json(await client.GetAsync($"{url}?search=Target&status={status}&pageSize=100"));
        Assert.Equal(105, filtered["totalCount"]!.GetValue<int>());
        Assert.All(filtered["items"]!.AsArray(), item => {
            Assert.Contains("Target", item!["contactName"]!.GetValue<string>());
            Assert.Equal(status, item["status"]!.GetValue<string>());
            Assert.Equal(primary.TenantId, item["tenantId"]!.GetValue<Guid>());
        });
        var last = await Json(await client.GetAsync($"{url}?search=Target&status={status}&pageSize=100&page=2"));
        Assert.Equal(5, last["items"]!.AsArray().Count);
        var empty = await Json(await client.GetAsync(url + "?search=Nobody"));
        Assert.Equal(0, empty["totalCount"]!.GetValue<int>());
        Assert.Empty(empty["items"]!.AsArray());
        var beyond = await Json(await client.GetAsync(url + "?page=100&pageSize=100"));
        Assert.Equal(225, beyond["totalCount"]!.GetValue<int>());
        Assert.Empty(beyond["items"]!.AsArray());
        using var anonymous = app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await outside.GetAsync(url)).StatusCode);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("page=-1")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    [InlineData("page=2147483647&pageSize=100")]
    [InlineData("page=invalid")]
    [InlineData("status=Draft")]
    public async Task Invalid_queries_are_rejected_for_both_lists(string query)
    {
        using var app = new IdentityApiFactory();
        using var client = await Login(app);
        foreach (var resource in new[] { "members", "applications" })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/v1/membership/{resource}?{query}")).StatusCode);
    }

    private static async Task Seed(IdentityApiFactory app, SessionResponse session, int count)
    {
        using var scope = app.Services.CreateScope();
        var options = new DbContextOptionsBuilder<MembershipDbContext>().UseSqlite(scope.ServiceProvider.GetRequiredService<MembershipDbContext>().Database.GetDbConnection()).Options;
        await using var db = new MembershipDbContext(options, new Identity(session.TenantId, session.UserId));
        var product = new MembershipProduct { TenantId = session.TenantId, Name = "Annual", Category = "Standard" };
        db.Add(product);
        for (var i = 0; i < count; i++)
        {
            var application = new MembershipApplication {
                TenantId = session.TenantId, ContactId = Guid.NewGuid(), ProductId = product.Id,
                ContactName = i < 210 ? "Target Same Name" : "Other Name", Status = i % 2 == 0 ? "submitted" : "approved",
                RequestedStartUtc = DateTime.UtcNow.Date, CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };
            db.Add(application);
            db.Add(new MemberSubscription {
                TenantId = session.TenantId, ContactId = application.ContactId, ContactName = application.ContactName,
                ProductId = product.Id, ApplicationId = application.Id, Status = i % 2 == 0 ? "active" : "cancelled",
                StartsAtUtc = DateTime.UtcNow.Date, EndsAtUtc = DateTime.UtcNow.Date.AddYears(1)
            });
        }
        await db.SaveChangesAsync();
    }

    private sealed record Identity(Guid? TenantId, Guid? UserId) : IRequestIdentity;
}
