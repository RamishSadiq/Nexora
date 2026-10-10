using System.Net;
using System.Text.Json.Nodes;
using Nexora.Modules.Crm.Contracts;

namespace Nexora.Api.Tests;

public sealed partial class CrmEndpointTests
{
    [Fact]
    public async Task Address_details_and_live_billing_source_preserve_records_and_enforce_versions()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var outside = await Login(app, "outside@nexora.test");
        var contact = await Create(staff, "Address details"); var path = $"/api/v1/crm/records/{contact["id"]!.GetValue<string>()}";
        async Task<JsonNode> Current() => await Json(await staff.GetAsync(path));
        var detail = await Current();
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, path + "/billing-address", HttpMethod.Put, new { version = detail["record"]!["version"]!.GetValue<string>(), useMailingAddress = true })).StatusCode);
        await Json(await Send(staff, path + "/addresses", HttpMethod.Post, new { label = "Legacy", line1 = "Old road", city = "Old town", postalCode = "OLD1", country = "UK" }));
        detail = await Current(); Assert.Equal("other", detail["addresses"]![0]!["purpose"]!.GetValue<string>());
        var request = new AddressRequest("Mailing", "1 Road", "Building", "York", "North", "YO1 1AA", "UK", Purpose: "mailing", Line3: "Floor 3", Line4: "Room 4", County: "Yorkshire", AddressType: "Work", AccountName: "Example Ltd", JobTitle: "Director", Version: Guid.Parse(detail["record"]!["version"]!.GetValue<string>()));
        var mailing = await Json(await Send(staff, path + "/addresses", HttpMethod.Post, request));
        Assert.Equal(HttpStatusCode.Conflict, (await Send(staff, path + "/addresses", HttpMethod.Post, request with { Purpose = "other" })).StatusCode);
        detail = await Current(); var version = Guid.Parse(detail["record"]!["version"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, path + "/addresses", HttpMethod.Post, request with { Version = version })).StatusCode);
        var billing = await Json(await Send(staff, path + "/addresses", HttpMethod.Post, request with { Purpose = "billing", Label = "Billing", Line1 = "Finance road", Version = version }));
        detail = await Current(); version = Guid.Parse(detail["record"]!["version"]!.GetValue<string>());
        Assert.Equal("Finance road", detail["effectiveBillingAddress"]!["line1"]!.GetValue<string>());
        await Json(await Send(staff, path + "/billing-address", HttpMethod.Put, new { version, useMailingAddress = true }));
        Assert.Equal(HttpStatusCode.Conflict, (await Send(staff, path + "/billing-address", HttpMethod.Put, new { version, useMailingAddress = false })).StatusCode);
        detail = await Current(); version = Guid.Parse(detail["record"]!["version"]!.GetValue<string>());
        Assert.Equal("1 Road", detail["effectiveBillingAddress"]!["line1"]!.GetValue<string>());
        Assert.Equal("Floor 3", detail["effectiveBillingAddress"]!["line3"]!.GetValue<string>());
        Assert.Equal("Room 4", detail["effectiveBillingAddress"]!["line4"]!.GetValue<string>());
        Assert.Equal("Yorkshire", detail["effectiveBillingAddress"]!["county"]!.GetValue<string>());
        Assert.Equal("North", detail["effectiveBillingAddress"]!["region"]!.GetValue<string>());
        Assert.Equal("Example Ltd", detail["effectiveBillingAddress"]!["accountName"]!.GetValue<string>());
        Assert.Equal("Director", detail["effectiveBillingAddress"]!["jobTitle"]!.GetValue<string>());
        Assert.Equal("Work", detail["effectiveBillingAddress"]!["addressType"]!.GetValue<string>());
        var mailingPath = path + "/addresses/" + mailing["id"]!.GetValue<string>();
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, mailingPath, HttpMethod.Delete)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, mailingPath, HttpMethod.Put, request with { Purpose = "other", Version = version })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, path + "/addresses/" + billing["id"]!.GetValue<string>(), HttpMethod.Put, request with { Purpose = "billing", Version = version })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, mailingPath, HttpMethod.Put, request with { Version = null })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, mailingPath, HttpMethod.Put, request with { Line3 = new string('a', 201), Version = version })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(outside, mailingPath, HttpMethod.Put, request with { Version = version })).StatusCode);
        await Json(await Send(staff, mailingPath, HttpMethod.Put, request with { Line1 = "Updated road", County = "", Version = version }));
        detail = await Current(); version = Guid.Parse(detail["record"]!["version"]!.GetValue<string>());
        Assert.Equal("Updated road", detail["effectiveBillingAddress"]!["line1"]!.GetValue<string>()); Assert.Null(detail["effectiveBillingAddress"]!["county"]);
        var other = await Create(staff, "Other address parent");
        Assert.Equal(HttpStatusCode.NotFound, (await Send(staff, $"/api/v1/crm/records/{other["id"]!.GetValue<string>()}/addresses/{mailing["id"]!.GetValue<string>()}", HttpMethod.Put, request with { Version = Guid.Parse(other["version"]!.GetValue<string>()) })).StatusCode);
        await Json(await Send(staff, path + "/billing-address", HttpMethod.Put, new { version, useMailingAddress = false }));
        detail = await Current(); Assert.Equal("Finance road", detail["effectiveBillingAddress"]!["line1"]!.GetValue<string>());
        Assert.Equal(3, detail["addresses"]!.AsArray().Count);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(staff, mailingPath, HttpMethod.Delete)).StatusCode);
        detail = await Current(); Assert.Equal("Finance road", detail["effectiveBillingAddress"]!["line1"]!.GetValue<string>());
        Assert.Contains("address.billing-source.updated", (await Json(await staff.GetAsync(path + "/timeline"))).ToJsonString());
        await Json(await Send(staff, path + "/archive", HttpMethod.Post, new { version = detail["record"]!["version"]!.GetValue<string>() }));
        detail = await Current();
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, path + "/billing-address", HttpMethod.Put, new { version = detail["record"]!["version"]!.GetValue<string>(), useMailingAddress = true })).StatusCode);
    }
}
