using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Identity;
using Nexora.Modules.Membership.Domain;
using Nexora.Modules.Membership.Infrastructure;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;

public sealed class ListingSortingTests
{
    [Fact]
    public async Task Crm_sorts_columns_before_pagination_and_retains_saved_view_sorts()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app);
        async Task<string> Create(string name, string status, string category) => Id(await Json(await Send(client,
            "/api/v1/crm/records", HttpMethod.Post, new { kind = "contact", name, status, category }), HttpStatusCode.Created));
        var alpha = await Create("Alpha", "prospect", "Zulu");
        var zulu = await Create("Zulu", "active", "Alpha");
        foreach (var (sort, first) in new[] { ("status", zulu), ("-status", alpha), ("category", zulu), ("-category", alpha), ("updatedAtUtc", alpha), ("updated", zulu) })
        {
            var page = await Json(await client.GetAsync($"/api/v1/crm/records?sort={sort}&pageSize=1"));
            Assert.Equal(first, Id(page["items"]![0]!));
            Assert.Equal(2, page["total"]!.GetValue<int>());
            var second = await Json(await client.GetAsync($"/api/v1/crm/records?sort={sort}&pageSize=1&page=2"));
            Assert.NotEqual(first, Id(second["items"]![0]!));
        }
        var view = await Json(await Send(client, "/api/v1/crm/views", HttpMethod.Post, new { name = "Category order", kind = "contact", search = "", status = "", sort = "-category", archived = false, columns = "name,category" }));
        Assert.Equal("-category", view["sort"]!.GetValue<string>());
        using var outside = await Login(app, "outside@nexora.test");
        Assert.Empty((await Json(await outside.GetAsync("/api/v1/crm/records?sort=category")))["items"]!.AsArray());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/crm/records?sort=--name")).StatusCode);
    }

    [Theory]
    [InlineData("text")]
    [InlineData("number")]
    [InlineData("date")]
    [InlineData("boolean")]
    public async Task Crm_sorts_typed_custom_fields_and_rejects_other_tenants_fields(string type)
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app);
        var first = await Json(await Send(client, "/api/v1/crm/records", HttpMethod.Post, new { kind = "contact", name = "A higher value", status = "active" }), HttpStatusCode.Created);
        var second = await Json(await Send(client, "/api/v1/crm/records", HttpMethod.Post, new { kind = "contact", name = "Z lower value", status = "active" }), HttpStatusCode.Created);
        var field = await Json(await Send(client, "/api/v1/crm/fields", HttpMethod.Post, new { name = "Sort value", kind = "contact", dataType = type }));
        object High() => type switch { "number" => new { numberValue = 100 }, "boolean" => new { booleanValue = true }, "date" => new { dateValue = "2026-10-13T00:00:00Z" }, _ => new { textValue = "Zulu" } };
        object Low() => type switch { "number" => new { numberValue = 2 }, "boolean" => new { booleanValue = false }, "date" => new { dateValue = "2026-01-01T00:00:00Z" }, _ => new { textValue = "Alpha" } };
        await Json(await Send(client, $"/api/v1/crm/records/{Id(first)}/fields/{Id(field)}", HttpMethod.Put, High()));
        await Json(await Send(client, $"/api/v1/crm/records/{Id(second)}/fields/{Id(field)}", HttpMethod.Put, Low()));
        foreach (var (prefix, expected) in new[] { ("", Id(second)), ("-", Id(first)) })
        {
            var result = await Json(await client.GetAsync($"/api/v1/crm/records?sort={prefix}field:{Id(field)}&pageSize=1"));
            Assert.Equal(expected, Id(result["items"]![0]!));
        }
        await Json(await Send(client, "/api/v1/crm/views", HttpMethod.Post, new { name = "Field order", kind = "contact", search = "", status = "", sort = $"field:{Id(field)}", archived = false, columns = $"name,field:{Id(field)}" }));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/v1/crm/records?kind=account&sort=field:{Id(field)}")).StatusCode);
        using var outside = await Login(app, "outside@nexora.test");
        Assert.Equal(HttpStatusCode.BadRequest, (await outside.GetAsync($"/api/v1/crm/records?sort=field:{Id(field)}")).StatusCode);
    }

    [Fact]
    public async Task Membership_sorts_names_products_and_rates_across_pages()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app);
        var session = (await client.GetFromJsonAsync<SessionResponse>("/api/v1/auth/session"))!;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var options = new DbContextOptionsBuilder<MembershipDbContext>().UseSqlite(scope.ServiceProvider.GetRequiredService<MembershipDbContext>().Database.GetDbConnection()).Options;
            await using var db = new MembershipDbContext(options, new Identity(session.TenantId, session.UserId));
            var alphaProduct = new MembershipProduct { TenantId = session.TenantId, Name = "Alpha product", Category = "Test" };
            var zuluProduct = new MembershipProduct { TenantId = session.TenantId, Name = "Zulu product", Category = "Test" };
            db.AddRange(alphaProduct, zuluProduct);
            foreach (var (name, account, product, rate, end) in new[] { ("Alpha person", "Zulu company", zuluProduct.Id, 100m, 2030), ("Zulu person", "Alpha company", alphaProduct.Id, 2m, 2027) })
            {
                var application = new MembershipApplication { TenantId = session.TenantId, ContactId = Guid.NewGuid(), ContactName = name, AccountName = account, ProductId = product, QuotedRate = rate, Status = "submitted" };
                db.Add(application);
                db.Add(new MemberSubscription { TenantId = session.TenantId, ContactId = application.ContactId, ContactName = name, AccountName = account, ProductId = product, ApplicationId = application.Id, Rate = rate, EndsAtUtc = new DateTime(end, 1, 1), Status = "active" });
            }
            await db.SaveChangesAsync();
        }
        foreach (var resource in new[] { "members", "applications" })
        {
            foreach (var sort in new[] { "product", "rate", "-product", "-rate" })
            {
                var result = await Json(await client.GetAsync($"/api/v1/membership/{resource}?sort={sort}&pageSize=1"));
                Assert.Equal(2, result["totalCount"]!.GetValue<int>());
                Assert.Equal(sort.StartsWith('-') ? "Alpha person" : "Zulu person", result["items"]![0]!["contactName"]!.GetValue<string>());
            }
            foreach (var sort in new[] { "name", "-name", "status", "-status" })
            {
                var first = await Json(await client.GetAsync($"/api/v1/membership/{resource}?sort={sort}&pageSize=1"));
                var second = await Json(await client.GetAsync($"/api/v1/membership/{resource}?sort={sort}&pageSize=1&page=2"));
                Assert.NotEqual(Id(first["items"]![0]!), Id(second["items"]![0]!));
            }
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/v1/membership/{resource}?sort=untrusted")).StatusCode);
        }
        var member = await Json(await client.GetAsync("/api/v1/membership/members?sort=name&pageSize=1"));
        Assert.Equal("Alpha company", member["items"]![0]!["accountName"]!.GetValue<string>());
        var applicationPage = await Json(await client.GetAsync("/api/v1/membership/applications?sort=name&pageSize=1"));
        Assert.Equal("Alpha person", applicationPage["items"]![0]!["contactName"]!.GetValue<string>());
        var term = await Json(await client.GetAsync("/api/v1/membership/members?sort=termEnd&pageSize=1"));
        Assert.Equal("Zulu person", term["items"]![0]!["contactName"]!.GetValue<string>());
    }

    [Fact]
    public async Task Report_sorts_before_paging_and_export_preserves_the_order()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app);
        foreach (var name in new[] { "Zulu", "Alpha", "Beta" })
            await Json(await Send(client, "/api/v1/crm/records", HttpMethod.Post, new { kind = "contact", name, status = "active" }), HttpStatusCode.Created);
        var first = await Json(await Send(client, "/api/v1/insights/query/contacts", HttpMethod.Post, new { sort = "label", take = 1 }));
        Assert.Equal("Alpha", first["items"]![0]!["label"]!.GetValue<string>());
        var second = await Json(await Send(client, "/api/v1/insights/query/contacts", HttpMethod.Post, new { sort = "label", skip = 1, take = 1 }));
        Assert.Equal("Beta", second["items"]![0]!["label"]!.GetValue<string>());
        var descending = await Json(await Send(client, "/api/v1/insights/query/contacts", HttpMethod.Post, new { sort = "-label", take = 1 }));
        Assert.Equal("Zulu", descending["items"]![0]!["label"]!.GetValue<string>());
        var exported = await Send(client, "/api/v1/insights/export/contacts", HttpMethod.Post, new { sort = "label", take = 10 });
        var csv = await exported.Content.ReadAsStringAsync();
        Assert.True(csv.IndexOf("Alpha", StringComparison.Ordinal) < csv.IndexOf("Zulu", StringComparison.Ordinal));
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, "/api/v1/insights/query/contacts", HttpMethod.Post, new { sort = "invalid", take = 10 })).StatusCode);
    }

    private sealed record Identity(Guid? TenantId, Guid? UserId) : IRequestIdentity;
}
