using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Security;
using Nexora.BuildingBlocks.Persistence;
using Nexora.Modules.Membership.Domain;
using Nexora.Modules.Membership.Infrastructure;
using Nexora.Modules.Identity;
using static Nexora.Api.Tests.WorkflowTestClient;
namespace Nexora.Api.Tests;
public sealed class MembershipEndpointTests
{
    [Fact]
    public async Task Application_quote_decision_renewal_cancellation_and_history()
    {
        using var app=new IdentityApiFactory();using var client=await Login(app);var contact=await Contact(client);
        var product=await Json(await Send(client,"/api/v1/membership/products",HttpMethod.Post,new{name="Annual",category="Standard",termMonths=12,rate=100,currency="GBP",isActive=true}));
        var application=await Json(await Send(client,"/api/v1/membership/applications",HttpMethod.Post,new{contactId=Id(contact),productId=Id(product),startsAtUtc=DateTime.UtcNow.Date}));
        await Json(await Send(client,"/api/v1/membership/products/"+Id(product),HttpMethod.Patch,new{name="Annual",category="Standard",termMonths=12,rate=150,currency="GBP",isActive=true,version=Version(product)}));
        var approved=await Json(await Send(client,"/api/v1/membership/applications/"+Id(application)+"/decision",HttpMethod.Post,new{version=Version(application),decision="approve",reason="Eligible"}));
        var id=approved["membershipId"]!.GetValue<string>();
        var detail=await Json(await client.GetAsync("/api/v1/membership/members/"+id));var member=detail["member"]!;
        Assert.Equal(100,member["rate"]!.GetValue<decimal>());
        var oldEnd=member["endsAtUtc"]!.GetValue<DateTime>();
        var renewed=await Json(await Send(client,"/api/v1/membership/members/"+id+"/renew",HttpMethod.Post,new{version=Version(member),reason="Annual renewal"}));
        Assert.Equal(oldEnd.AddMonths(12),renewed["endsAtUtc"]!.GetValue<DateTime>());Assert.Equal(150,renewed["rate"]!.GetValue<decimal>());
        Assert.Equal(HttpStatusCode.Conflict,(await Send(client,"/api/v1/membership/members/"+id+"/renew",HttpMethod.Post,new{version=Version(member),reason="Duplicate retry"})).StatusCode);
        var cancelled=await Json(await Send(client,"/api/v1/membership/members/"+id+"/cancel",HttpMethod.Post,new{version=Version(renewed),reason="Member request"}));
        Assert.Equal("cancelled",cancelled["status"]!.GetValue<string>());
        var reinstated=await Json(await Send(client,"/api/v1/membership/members/"+id+"/reinstate",HttpMethod.Post,new{version=Version(cancelled),reason="Rejoined"}));
        Assert.Equal("active",reinstated["status"]!.GetValue<string>());
        var history=(await Json(await client.GetAsync("/api/v1/membership/members/"+id)))["history"]!.AsArray();Assert.Equal(4,history.Count);
    }
    [Fact]
    public async Task Foreign_tenant_records_csrf_invalid_transitions_and_duplicate_approvals_are_rejected()
    {
        using var app=new IdentityApiFactory();using var client=await Login(app);using var outside=await Login(app,"outside@nexora.test");var contact=await Contact(client);
        var product=await Json(await Send(client,"/api/v1/membership/products",HttpMethod.Post,new{name="Annual",category="Standard",termMonths=12,rate=100,currency="GBP",isActive=true}));
        Assert.Equal(HttpStatusCode.Forbidden,(await outside.GetAsync("/api/v1/membership/products")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/v1/membership/products",new{name="No token"})).StatusCode);
        var request=new{contactId=Id(contact),productId=Id(product),startsAtUtc=DateTime.UtcNow.Date};
        var first=await Json(await Send(client,"/api/v1/membership/applications",HttpMethod.Post,request));var second=await Json(await Send(client,"/api/v1/membership/applications",HttpMethod.Post,request));
        await Json(await Send(client,"/api/v1/membership/applications/"+Id(first)+"/decision",HttpMethod.Post,new{version=Version(first),decision="approve",reason="Approved"}));
        Assert.Equal(HttpStatusCode.BadRequest,(await Send(client,"/api/v1/membership/applications/"+Id(second)+"/decision",HttpMethod.Post,new{version=Version(second),decision="approve",reason="Overlapping"})).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await Send(client,"/api/v1/membership/applications",HttpMethod.Post,new{contactId=Guid.NewGuid(),productId=Id(product),startsAtUtc=DateTime.UtcNow.Date})).StatusCode);
        var session=(await outside.GetFromJsonAsync<SessionResponse>("/api/v1/auth/session"))!;
        using var scope=app.Services.CreateScope();var options=new DbContextOptionsBuilder<MembershipDbContext>().UseSqlite(scope.ServiceProvider.GetRequiredService<MembershipDbContext>().Database.GetDbConnection()).Options;
        await using var db=new MembershipDbContext(options,new Identity(session.TenantId,session.UserId));
        Assert.Empty(await db.Set<MembershipProduct>().ToListAsync());
        db.Update(new MembershipProduct{Id=Guid.Parse(Id(product)),TenantId=session.TenantId,Name="Forged"});
        await Assert.ThrowsAsync<TenantBoundaryException>(()=>db.SaveChangesAsync());
    }
    private sealed record Identity(Guid? TenantId,Guid? UserId):IRequestIdentity;
}
