using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Domain;
using Nexora.Modules.Crm.Infrastructure;
using Nexora.Modules.Identity.Domain;
using Nexora.Modules.Identity.Infrastructure.Persistence;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;
public sealed partial class PortalEndpointTests
{
    private static object Registration(string email = "new.member@nexora.test") => new { firstName = "New", lastName = "Member", email, password = "New-Member-2026!" };
    private static async Task EnableOnboarding(HttpClient staff)
    {
        await Json(await Send(staff, "/api/v1/content/site", HttpMethod.Post, new { name = "Onboarding association", tagline = "Join us" }));
        var current = await Json(await staff.GetAsync("/api/v1/content/portal"));
        var saved = await Json(await Send(staff, "/api/v1/content/portal", HttpMethod.Put, new { version = current["version"], configuration = PortalPresentation.Default with { Onboarding = new(true, true) } }));
        await Json(await Send(staff, "/api/v1/content/portal/publish", HttpMethod.Post, new { version = Version(saved), configuration = PortalPresentation.Default }));
    }

    [Fact]
    public async Task Registration_requires_published_settings_trusted_host_csrf_and_allowlisted_fields()
    {
        using var app = new IdentityApiFactory(null, sharedCrmIdentity: true); using var staff = await Login(app); using var member = app.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await PortalSend(member, "/register", HttpMethod.Post, Registration())).StatusCode);
        await EnableOnboarding(staff);
        Assert.Equal(HttpStatusCode.BadRequest, (await member.PostAsJsonAsync("/api/v1/portal/register", Registration())).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PortalSend(member, "/register", HttpMethod.Post, new { firstName = "New", lastName = "Member", email = "member@nexora.test", password = "weak" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PortalSend(member, "/register", HttpMethod.Post, new { firstName = "New", lastName = "Member", email = "member@nexora.test", password = "New-Member-2026!", tenantId = Guid.NewGuid() })).StatusCode);
        member.DefaultRequestHeaders.Host = "unconfigured.example";
        Assert.False((await Json(await member.GetAsync("/api/v1/portal/onboarding")))["allowSelfRegistration"]!.GetValue<bool>());
        Assert.Equal(HttpStatusCode.NotFound, (await PortalSend(member, "/register", HttpMethod.Post, Registration())).StatusCode);
        using var scope = app.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<NexoraIdentityDbContext>(); using var system = db.BeginSystemAccess();
        Assert.False(await db.Users.AnyAsync(x => x.Email == "new.member@nexora.test"));
    }

    [Fact]
    public async Task Registration_persists_new_contact_and_portal_only_login_without_automatic_membership()
    {
        using var app = new IdentityApiFactory(null, sharedCrmIdentity: true); using var staff = await Login(app); using var member = app.CreateClient(); using var duplicate = app.CreateClient();
        await EnableOnboarding(staff);
        var existing = await Json(await Send(staff, "/api/v1/crm/records", HttpMethod.Post, new { kind = "contact", name = "New Member", status = "active" }), HttpStatusCode.Created);
        await Json(await PortalSend(member, "/register", HttpMethod.Post, Registration()));
        Assert.Equal("New Member", (await Json(await member.GetAsync("/api/v1/portal/profile")))["name"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.Unauthorized, (await member.GetAsync("/api/v1/crm/records")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await member.GetAsync("/api/v1/admin/portal-access/organisation-requests")).StatusCode);
        Assert.Empty((await Json(await member.GetAsync("/api/v1/portal/memberships")))["memberships"]!.AsArray());
        Assert.Equal(HttpStatusCode.BadRequest, (await PortalSend(duplicate, "/register", HttpMethod.Post, Registration())).StatusCode);
        await PortalSend(member, "/logout", HttpMethod.Post);
        await Json(await PortalSend(member, "/login", HttpMethod.Post, new { email = "new.member@nexora.test", password = "New-Member-2026!", rememberMe = false }));
        Assert.Equal(HttpStatusCode.OK, (await member.GetAsync("/api/v1/portal/profile")).StatusCode);
        using var scope = app.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<NexoraIdentityDbContext>(); using var system = db.BeginSystemAccess();
        var user = await db.Users.SingleAsync(x => x.Email == "new.member@nexora.test"); var access = await db.PortalAccess.SingleAsync(x => x.UserId == user.Id);
        Assert.NotEqual(Guid.Parse(Id(existing)), access.ContactId); Assert.Empty(await db.UserRoles.Where(x => x.UserId == user.Id).ToListAsync());
        var crm = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var communication = await crm.Set<CommunicationMethod>().IgnoreQueryFilters().SingleAsync(x => x.RecordId == access.ContactId);
        Assert.Equal("unknown", communication.Consent);
        Assert.Equal("New", (await crm.Set<ContactProfile>().IgnoreQueryFilters().SingleAsync(x => x.RecordId == access.ContactId)).FirstName);
    }

    [Fact]
    public async Task Company_requests_enforce_ownership_review_versions_tenant_matching_and_live_revocation()
    {
        using var app = new IdentityApiFactory(null, sharedCrmIdentity: true); using var staff = await Login(app); using var outside = await Login(app, "outside@nexora.test");
        using var member = app.CreateClient(); using var other = app.CreateClient();
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NexoraIdentityDbContext>(); using var system = db.BeginSystemAccess();
            var role = await db.Roles.SingleAsync(x => x.Name == "Outside Administrator");
            db.RolePermissions.AddRange(new RolePermission { RoleId = role.Id, Permission = "crm.manage" }, new RolePermission { RoleId = role.Id, Permission = "identity.manage" });
            await db.SaveChangesAsync();
        }
        await EnableOnboarding(staff); await Json(await PortalSend(member, "/register", HttpMethod.Post, Registration())); await Json(await PortalSend(other, "/register", HttpMethod.Post, Registration("other.member@nexora.test")));
        var account = await Json(await Send(staff, "/api/v1/crm/records", HttpMethod.Post, new { kind = "account", name = "Example Company", status = "active" }), HttpStatusCode.Created);
        var foreign = await Json(await Send(outside, "/api/v1/crm/records", HttpMethod.Post, new { kind = "account", name = "Outside Company", status = "active" }), HttpStatusCode.Created);
        var request = await Json(await PortalSend(member, "/organisation-requests", HttpMethod.Post, new { organisationName = "Example Company", jobTitle = "Manager", message = "I represent this company.\nPlease verify my role." }));
        Assert.Equal(HttpStatusCode.Conflict, (await PortalSend(member, "/organisation-requests", HttpMethod.Post, new { organisationName = "example company", jobTitle = "", message = "Duplicate" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await member.GetAsync($"/api/v1/portal/organisations/{Id(account)}/profile")).StatusCode);
        Assert.Empty((await Json(await other.GetAsync("/api/v1/portal/organisation-requests")))["items"]!.AsArray());
        Assert.Equal(HttpStatusCode.NotFound, (await PortalSend(other, $"/organisation-requests/{Id(request)}/cancel", HttpMethod.Post, new { version = Version(request) })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(outside, $"/api/v1/admin/portal-access/organisation-requests/{Id(request)}/decision", HttpMethod.Post,
            new { version = Version(request), decision = "approved", accountId = Id(foreign), canSubmitApplications = true, decisionMessage = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, $"/api/v1/admin/portal-access/organisation-requests/{Id(request)}/decision", HttpMethod.Post,
            new { version = Version(request), decision = "approved", accountId = Id(foreign), canSubmitApplications = true, decisionMessage = "" })).StatusCode);
        var payload = new { version = Version(request), decision = "approved", accountId = Id(account), canSubmitApplications = false, decisionMessage = "Relationship verified.\nWelcome to your company portal." };
        await Json(await Send(staff, $"/api/v1/admin/portal-access/organisation-requests/{Id(request)}/decision", HttpMethod.Post, payload));
        Assert.Equal(HttpStatusCode.Conflict, (await Send(staff, $"/api/v1/admin/portal-access/organisation-requests/{Id(request)}/decision", HttpMethod.Post, payload)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await PortalSend(member, $"/organisation-requests/{Id(request)}/cancel", HttpMethod.Post, new { version = Version(request) })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await member.GetAsync($"/api/v1/portal/organisations/{Id(account)}/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.GetAsync($"/api/v1/portal/organisations/{Id(account)}/drafts")).StatusCode);
        var history = await Json(await member.GetAsync("/api/v1/portal/organisation-requests")); Assert.Contains("Relationship verified.", history.ToJsonString()); Assert.DoesNotContain("decidedBy", history.ToJsonString());
        var list = await Json(await staff.GetAsync("/api/v1/admin/portal-access")); var accessId = list["items"]!.AsArray().Single(x => x!["email"]!.GetValue<string>() == "new.member@nexora.test")!["id"]!.GetValue<string>();
        var grants = await Json(await staff.GetAsync($"/api/v1/admin/portal-access/{accessId}/organisations"));
        await Send(staff, $"/api/v1/admin/portal-access/{accessId}/organisations/{Id(grants[0]!)}", HttpMethod.Delete);
        Assert.Equal(HttpStatusCode.NotFound, (await member.GetAsync($"/api/v1/portal/organisations/{Id(account)}/profile")).StatusCode);
        var cancelled = await Json(await PortalSend(member, "/organisation-requests", HttpMethod.Post, new { organisationName = "Other Company", jobTitle = "", message = "Please review." }));
        Assert.Equal(HttpStatusCode.NoContent, (await PortalSend(member, $"/organisation-requests/{Id(cancelled)}/cancel", HttpMethod.Post, new { version = Version(cancelled) })).StatusCode);
        var rejected = await Json(await PortalSend(member, "/organisation-requests", HttpMethod.Post, new { organisationName = "Other Company", jobTitle = "", message = "Please review again." }));
        await Json(await Send(staff, $"/api/v1/admin/portal-access/organisation-requests/{Id(rejected)}/decision", HttpMethod.Post,
            new { version = Version(rejected), decision = "rejected", accountId = (Guid?)null, canSubmitApplications = false, decisionMessage = "Contact the company administrator." }));
        Assert.Contains("rejected", (await Json(await member.GetAsync("/api/v1/portal/organisation-requests"))).ToJsonString());
    }

    private sealed record RegistrationIdentity(Guid? TenantId, Guid? UserId) : IRequestIdentity;
    private sealed class FailingRegistrationCrm(DbContextOptions<CrmDbContext> options) : IPortalOnboardingCrm
    {
        public async Task CreateContactAsync(Guid tenantId, Guid userId, Guid contactId, string firstName, string lastName, string email, DbConnection connection, DbTransaction transaction, CancellationToken ct)
        {
            await using var db = new CrmDbContext(options, new RegistrationIdentity(tenantId, userId));
            db.Database.SetDbConnection(connection); await db.Database.UseTransactionAsync(transaction, ct);
            db.Add(new CrmRecord { Id = contactId, TenantId = tenantId, Name = "Rollback contact", Kind = "contact", Status = "active" }); await db.SaveChangesAsync(ct);
            throw new ValidationException("Simulated CRM failure after contact persistence.");
        }
        public async Task AssociateAsync(Guid tenantId, Guid reviewerId, Guid contactId, Guid accountId, string jobTitle, DbConnection connection, DbTransaction transaction, CancellationToken ct)
        {
            await using var db = new CrmDbContext(options, new RegistrationIdentity(tenantId, reviewerId));
            db.Database.SetDbConnection(connection); await db.Database.UseTransactionAsync(transaction, ct);
            db.Add(new AccountContact { TenantId = tenantId, AccountId = accountId, ContactId = contactId, StartDate = DateTime.UtcNow.Date }); await db.SaveChangesAsync(ct);
            throw new ValidationException("Simulated failure after relationship persistence.");
        }
    }
    [Fact]
    public async Task Registration_rolls_back_identity_and_crm_if_contact_creation_fails()
    {
        using var app = new IdentityApiFactory(null, sharedCrmIdentity: true); using var staff = await Login(app); await EnableOnboarding(staff);
        using var failing = app.WithWebHostBuilder(b => b.ConfigureTestServices(s => { s.RemoveAll<IPortalOnboardingCrm>(); s.AddScoped<IPortalOnboardingCrm, FailingRegistrationCrm>(); })); using var member = failing.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await PortalSend(member, "/register", HttpMethod.Post, Registration())).StatusCode);
        using var scope = app.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<NexoraIdentityDbContext>(); using var system = db.BeginSystemAccess();
        Assert.False(await db.Users.AnyAsync(x => x.Email == "new.member@nexora.test")); Assert.Empty(await db.PortalAccess.ToListAsync());
        Assert.False(await scope.ServiceProvider.GetRequiredService<CrmDbContext>().Records.IgnoreQueryFilters().AnyAsync(x => x.Name == "Rollback contact"));
    }

    [Fact]
    public async Task Company_approval_rolls_back_relationship_and_leaves_request_pending_on_failure()
    {
        using var app = new IdentityApiFactory(null, sharedCrmIdentity: true); using var staff = await Login(app); using var member = app.CreateClient();
        await EnableOnboarding(staff); await Json(await PortalSend(member, "/register", HttpMethod.Post, Registration()));
        var account = await Json(await Send(staff, "/api/v1/crm/records", HttpMethod.Post, new { kind = "account", name = "Rollback Company", status = "active" }), HttpStatusCode.Created);
        var request = await Json(await PortalSend(member, "/organisation-requests", HttpMethod.Post, new { organisationName = "Rollback Company", jobTitle = "", message = "Please verify." }));
        using var failing = app.WithWebHostBuilder(b => b.ConfigureTestServices(s => { s.RemoveAll<IPortalOnboardingCrm>(); s.AddScoped<IPortalOnboardingCrm, FailingRegistrationCrm>(); })); using var reviewer = failing.CreateClient();
        await Json(await Send(reviewer, "/api/v1/auth/login", HttpMethod.Post, new { email = IdentityApiFactory.AdminEmail, password = IdentityApiFactory.AdminPassword, rememberMe = false }));
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(reviewer, $"/api/v1/admin/portal-access/organisation-requests/{Id(request)}/decision", HttpMethod.Post,
            new { version = Version(request), decision = "approved", accountId = Id(account), canSubmitApplications = true, decisionMessage = "" })).StatusCode);
        using var scope = app.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<NexoraIdentityDbContext>(); using var system = db.BeginSystemAccess();
        Assert.Equal("pending", (await db.Set<PortalOrganisationRequest>().SingleAsync(x => x.Id == Guid.Parse(Id(request)))).Status);
        Assert.Empty(await db.Set<PortalOrganisationGrant>().ToListAsync());
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<CrmDbContext>().Set<AccountContact>().IgnoreQueryFilters().ToListAsync());
    }
}
