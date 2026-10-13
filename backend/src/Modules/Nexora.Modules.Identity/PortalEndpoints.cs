using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Identity.Domain;
using Nexora.Modules.Identity.Infrastructure.Persistence;
using Nexora.Modules.Identity.Security;

namespace Nexora.Modules.Identity;

internal static partial class PortalEndpoints
{
    internal const string Scheme = "Nexora.Portal";
    internal static void AddPortal(this IServiceCollection services, bool development)
    {
        services.AddAuthentication().AddCookie(Scheme, o =>
        {
            o.Cookie.Name = development ? "Nexora.Portal" : "__Host-Nexora.Portal";
            o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Lax;
            o.Cookie.SecurePolicy = development ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            o.ExpireTimeSpan = TimeSpan.FromHours(2); o.SlidingExpiration = false;
            o.Events.OnRedirectToLogin = c => { c.Response.StatusCode = 401; return Task.CompletedTask; };
            o.Events.OnRedirectToAccessDenied = c => { c.Response.StatusCode = 403; return Task.CompletedTask; };
        });
        services.AddAuthorizationBuilder().AddPolicy(Scheme, p => p.AddAuthenticationSchemes(Scheme).RequireAuthenticatedUser());
    }

    internal static void MapPortal(this IEndpointRouteBuilder endpoints)
    {
        var portal = endpoints.MapGroup("/api/v1/portal").WithTags("Member portal");
        portal.AddEndpointFilter(async (context, next) =>
        {
            var http = context.HttpContext;
            http.Response.Headers.CacheControl = "no-store";
            var portalSession = await http.AuthenticateAsync(Scheme);
            http.User = portalSession.Succeeded ? portalSession.Principal! : new ClaimsPrincipal(new ClaimsIdentity());
            // This first adapter is deliberately unavailable outside local development.
            if (!http.RequestServices.GetRequiredService<LocalIdentitySettings>().Enabled)
                return Results.Problem(statusCode: 503, title: "Member sign-in is not configured.");
            if (!HttpMethods.IsGet(http.Request.Method))
            {
                try { await http.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(http); }
                catch (AntiforgeryValidationException) { return Results.Problem(statusCode: 400, title: "Invalid security token."); }
            }
            try { return await next(context); }
            catch (DbUpdateConcurrencyException) { return Results.Problem(statusCode: 409, title: "This record changed. Reload before trying again."); }
            catch (System.ComponentModel.DataAnnotations.ValidationException e) { return Results.Problem(statusCode: 400, title: e.Message); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (DbUpdateException) { return Results.Problem(statusCode: 409, title: "This record changed or a draft already exists. Reload and try again."); }
        });
        portal.MapGet("/csrf", async (HttpContext http, IAntiforgery csrf) =>
        {
            var session = await http.AuthenticateAsync(Scheme);
            http.User = session.Succeeded ? session.Principal! : new ClaimsPrincipal(new ClaimsIdentity());
            return Results.Ok(new CsrfResponse(csrf.GetAndStoreTokens(http).RequestToken!));
        });
        portal.MapPost("/login", Login).RequireRateLimiting("identity-login");
        MapRegistration(portal);
        var own = portal.MapGroup("").RequireAuthorization(Scheme);
        own.AddEndpointFilter(async (context, next) =>
        {
            var http = context.HttpContext;
            var db = http.RequestServices.GetRequiredService<NexoraIdentityDbContext>();
            if (!Guid.TryParse(http.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ||
                !Guid.TryParse(http.User.FindFirstValue(NexoraClaimTypes.TenantId), out var tenantId) ||
                !Guid.TryParse(http.User.FindFirstValue("portal_access"), out var accessId)) return Results.Unauthorized();
            var access = await db.PortalAccess.SingleOrDefaultAsync(x => x.Id == accessId && x.UserId == userId && x.TenantId == tenantId && !x.IsRevoked);
            var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId && x.TenantId == tenantId && x.IsActive);
            if (access == null || user == null || user.SecurityStamp != http.User.FindFirstValue("portal_stamp") ||
                !await db.Tenants.AnyAsync(x => x.Id == tenantId && x.Status == TenantStatus.Active)) return Results.Unauthorized();
            var directory = http.RequestServices.GetRequiredService<IMemberProfileDirectory>();
            if (await directory.ReadAsync(access.ContactId, http.RequestAborted) == null) return Results.Unauthorized();
            http.Items["portal_contact"] = access.ContactId;
            http.Items["portal_access"] = access.Id;
            return await next(context);
        });
        MapOrganisations(own);
        MapOrganisationRequests(own);
        own.MapGet("/configuration", async (IPortalPresentationDirectory directory, CancellationToken ct) => Results.Ok(await directory.ReadAsync(ct)));
        MapEvidence(own);
        own.MapGet("/profile", async (HttpContext http, IMemberProfileDirectory directory, CancellationToken ct) =>
            Results.Ok(await directory.ReadAsync((Guid)http.Items["portal_contact"]!, ct)));
        own.MapGet("/memberships", async (HttpContext http, IMemberMembershipDirectory directory, CancellationToken ct) =>
            Results.Ok(await directory.ReadAsync((Guid)http.Items["portal_contact"]!, ct)));
        own.MapGet("/notifications", async (HttpContext http, IPortalNotificationService notifications, CancellationToken ct) =>
            Results.Ok(await notifications.ListAsync(Guid.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier)!), ct)));
        own.MapPost("/notifications/{id:guid}/read", async (Guid id, HttpContext http, IPortalNotificationService notifications, CancellationToken ct) =>
        {
            await notifications.MarkReadAsync(Guid.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier)!), id, ct); return Results.NoContent();
        });
        own.MapGet("/application-products", async (IMemberApplicationService service, CancellationToken ct) => Results.Ok(await service.ProductsAsync(ct)));
        own.MapGet("/drafts", async (HttpContext http, IMemberApplicationService service, CancellationToken ct) => Results.Ok(await service.DraftsAsync((Guid)http.Items["portal_contact"]!, ct)));
        own.MapGet("/applications/{id:guid}", async (Guid id, HttpContext http, IMemberApplicationService service, CancellationToken ct) =>
            Results.Ok(await service.ReceiptAsync((Guid)http.Items["portal_contact"]!, id, ct)));
        own.MapDelete("/drafts/{id:guid}", async (Guid id, [Microsoft.AspNetCore.Mvc.FromBody] JsonElement body, HttpContext http, IMemberApplicationService service, CancellationToken ct) =>
        {
            if (body.ValueKind != JsonValueKind.Object || body.EnumerateObject().Count() != 1 ||
                !body.TryGetProperty("version", out var value) || value.ValueKind != JsonValueKind.String || !value.TryGetGuid(out var version))
                return Results.Problem(statusCode: 400, title: "Only the current draft version is accepted.");
            await service.DiscardAsync((Guid)http.Items["portal_contact"]!, id, version, ct);
            return Results.NoContent();
        });
        own.MapPut("/drafts/{id:guid}", async (Guid id, JsonElement body, HttpContext http, IMemberApplicationService service, CancellationToken ct) =>
        {
            if (body.ValueKind != JsonValueKind.Object || body.EnumerateObject().Count() != 4 ||
                !body.TryGetProperty("productId", out var p) || p.ValueKind != JsonValueKind.String || !p.TryGetGuid(out var productId) ||
                !body.TryGetProperty("startsAtUtc", out var s) || s.ValueKind != JsonValueKind.String || !s.TryGetDateTime(out var start) ||
                !body.TryGetProperty("notes", out var n) || n.ValueKind != JsonValueKind.String ||
                !body.TryGetProperty("version", out var v) || (v.ValueKind != JsonValueKind.Null && (v.ValueKind != JsonValueKind.String || !v.TryGetGuid(out _))))
                return Results.Problem(statusCode: 400, title: "Only productId, startsAtUtc, notes and version are accepted.");
            return Results.Ok(await service.SaveAsync((Guid)http.Items["portal_contact"]!, new SaveMemberDraft(id, v.ValueKind == JsonValueKind.Null ? null : v.GetGuid(), productId, start, n.GetString()!), ct));
        });
        own.MapPost("/drafts/{id:guid}/submit", async (Guid id, JsonElement body, HttpContext http, IMemberApplicationService service, CancellationToken ct) =>
        {
            if (body.ValueKind != JsonValueKind.Object || body.EnumerateObject().Count() != 2 ||
                !body.TryGetProperty("version", out var v) || v.ValueKind != JsonValueKind.String || !v.TryGetGuid(out var version) ||
                !body.TryGetProperty("termsAccepted", out var t) || t.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return Results.BadRequest();
            var applicationId = await service.SubmitAsync((Guid)http.Items["portal_contact"]!, id, version, t.GetBoolean(), ct);
            return Results.Ok(new { applicationId });
        });
        own.MapPut("/profile", async (JsonElement body, HttpContext http, IMemberProfileDirectory directory, CancellationToken ct) =>
        {
            if (body.ValueKind != JsonValueKind.Object || body.EnumerateObject().Count() != (2 + (body.TryGetProperty("postalAddress", out _) ? 1 : 0) + (body.TryGetProperty("contactDetails", out _) ? 1 : 0)) ||
                body.EnumerateObject().Any(p => p.Name is not ("name" or "version" or "postalAddress" or "contactDetails")) ||
                !body.TryGetProperty("name", out var nameValue) || nameValue.ValueKind != JsonValueKind.String ||
                !body.TryGetProperty("version", out var versionValue) || versionValue.ValueKind != JsonValueKind.String || !versionValue.TryGetGuid(out var version))
                return Results.Problem(statusCode: 400, title: "Only name, postalAddress, contactDetails and version are accepted.");
            var name = nameValue.GetString()?.Trim();
            if (string.IsNullOrEmpty(name) || name.Length > 160) return Results.Problem(statusCode: 400, title: "Enter a name between 1 and 160 characters.");
            var updateAddress = body.TryGetProperty("postalAddress", out var address);
            MemberPostalAddress? postal = null;
            if (updateAddress && address.ValueKind != JsonValueKind.Null)
            {
                string[] keys = ["line1", "line2", "city", "region", "postalCode", "country"];
                if (address.ValueKind != JsonValueKind.Object || address.EnumerateObject().Count() != keys.Length ||
                    keys.Any(k => !address.TryGetProperty(k, out var v) || v.ValueKind != JsonValueKind.String || v.GetString()!.Length > 160 || v.GetString()!.Any(char.IsControl)))
                    return Results.Problem(statusCode: 400, title: "Enter address fields up to 160 characters each.");
                string Value(string key) => address.GetProperty(key).GetString()!.Trim();
                if (new[] { "line1", "city", "country" }.Any(k => Value(k).Length == 0))
                    return Results.Problem(statusCode: 400, title: "Enter an address line, city and country.");
                postal = new(Value("line1"), Value("line2"), Value("city"), Value("region"), Value("postalCode"), Value("country"));
            }
            var updateDetails = body.TryGetProperty("contactDetails", out var details);
            ContactDetails? contactDetails = null;
            if (updateDetails && details.ValueKind != JsonValueKind.Null)
            {
                string[] keys = ["title", "firstName", "middleName", "lastName", "previousLastName", "postNominal"];
                if (details.ValueKind != JsonValueKind.Object || details.EnumerateObject().Count() != keys.Length ||
                    keys.Any(k => !details.TryGetProperty(k, out var v) || v.ValueKind is not (JsonValueKind.String or JsonValueKind.Null)))
                    return Results.Problem(statusCode: 400, title: "Provide only the six personal name fields.");
                string? Value(string key) => details.GetProperty(key).GetString();
                contactDetails = new(Value("title"), Value("firstName"), Value("middleName"), Value("lastName"), Value("previousLastName"), Value("postNominal"));
            }
            var result = await directory.UpdateNameAsync((Guid)http.Items["portal_contact"]!, name, version, ct, postal, updateAddress, contactDetails, updateDetails);
            return result == null ? Results.Unauthorized() : Results.Ok(result);
        });
        // Logout still works after revocation; it only removes this browser's portal cookie.
        portal.MapPost("/logout", async (HttpContext http) => { await http.SignOutAsync(Scheme); return Results.NoContent(); }).RequireAuthorization(Scheme);

        var grants = endpoints.MapGroup("/api/v1/admin/portal-access").RequireAuthorization(NexoraPermissions.IdentityManage, "crm.manage");
        grants.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            if (!HttpMethods.IsGet(context.HttpContext.Request.Method))
            {
                try { await context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context.HttpContext); }
                catch (AntiforgeryValidationException) { return Results.BadRequest(); }
            }
            try { return await next(context); }
            catch (DbUpdateException) { return Results.Conflict(); }
            catch (System.ComponentModel.DataAnnotations.ValidationException e) { return Results.Problem(statusCode: 400, title: e.Message); }
        });
        MapOrganisationAdministration(grants);
        MapOrganisationRequestReview(grants);
        grants.MapGet("", async (string? page, NexoraIdentityDbContext db, IMemberProfileDirectory contacts,
            LocalIdentitySettings settings, CancellationToken ct) =>
        {
            var number = 1;
            if (page != null && (!int.TryParse(page, out number) || number < 1 || number > 100000)) return Results.BadRequest();
            var query = from grant in db.PortalAccess.AsNoTracking()
                        join user in db.Users.AsNoTracking() on grant.UserId equals user.Id
                        orderby grant.CreatedAtUtc descending, grant.Id
                        select new { grant.Id, grant.ContactId, grant.IsRevoked, grant.CreatedAtUtc, user.DisplayName, user.Email, user.IsActive };
            var totalCount = await query.CountAsync(ct);
            var rows = await query.Skip((number - 1) * 20).Take(20).ToArrayAsync(ct);
            var items = new List<object>();
            foreach (var row in rows)
            {
                var contact = await contacts.ReadAsync(row.ContactId, ct);
                items.Add(new { row.Id, row.ContactId, row.IsRevoked, row.CreatedAtUtc, row.DisplayName, row.Email,
                    userIsActive = row.IsActive, contactName = contact?.Name, contactIsActive = contact != null });
            }
            return Results.Ok(new { items, page = number, pageSize = 20, totalCount, localProvisioningEnabled = settings.Enabled });
        });
        grants.MapPost("", async (GrantRequest request, NexoraIdentityDbContext db, IRequestIdentity who, IMemberProfileDirectory contacts, IPortalNotificationService notifications, CancellationToken ct) =>
        {
            if (!await db.Users.AnyAsync(x => x.Id == request.UserId && x.TenantId == who.TenantId && x.IsActive, ct) || await contacts.ReadAsync(request.ContactId, ct) == null) return Results.NotFound();
            var grant = new PortalAccess { TenantId = who.TenantId!.Value, UserId = request.UserId, ContactId = request.ContactId };
            db.PortalAccess.Add(grant);
            db.AuditEvents.Add(new AuditEvent { TenantId = who.TenantId, ActorUserId = who.UserId, EventType = "portal.access.granted", Outcome = "succeeded", Subject = grant.Id.ToString() });
            await db.SaveChangesAsync(ct);
            await notifications.EnqueueAsync(who.TenantId!.Value, grant.UserId, null, "portal.access.granted", "Portal access granted", "Your member portal access has been granted.", ct);
            return Results.Ok(new { grant.Id });
        });
        grants.MapPost("/local-member", async (LocalMemberRequest request, LocalIdentitySettings settings, NexoraIdentityDbContext db,
            UserManager<NexoraUser> users, IRequestIdentity who, IMemberProfileDirectory contacts, IPortalNotificationService notifications, CancellationToken ct) =>
        {
            if (!settings.Enabled) return Results.Problem(statusCode: 503, title: "Local member provisioning is disabled.");
            var contact = await contacts.ReadAsync(request.ContactId, ct);
            if (contact == null) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Length > 254 || string.IsNullOrEmpty(request.Password)) return Results.BadRequest();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var user = new NexoraUser { TenantId = who.TenantId!.Value, DisplayName = contact.Name, Email = request.Email.Trim(), UserName = request.Email.Trim() };
            var created = await users.CreateAsync(user, request.Password);
            if (!created.Succeeded) return Results.Problem(statusCode: 400, title: "Unable to create member. Use an unused valid email and a password of at least 12 characters including uppercase, lowercase, number and symbol.");
            var grant = new PortalAccess { TenantId = user.TenantId, UserId = user.Id, ContactId = request.ContactId };
            db.PortalAccess.Add(grant);
            db.AuditEvents.Add(new AuditEvent { TenantId = who.TenantId, ActorUserId = who.UserId, EventType = "portal.local-member.created", Outcome = "succeeded", Subject = grant.Id.ToString() });
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
            await notifications.EnqueueAsync(who.TenantId!.Value, user.Id, null, "portal.access.granted", "Portal access granted", "Your member portal access has been granted.", ct);
            return Results.Ok(new { grant.Id, userId = user.Id });
        });
        grants.MapDelete("/{id:guid}", async (Guid id, NexoraIdentityDbContext db, IRequestIdentity who, IPortalNotificationService notifications, CancellationToken ct) =>
        {
            var grant = await db.PortalAccess.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (grant == null) return Results.NotFound();
            grant.IsRevoked = true;
            db.AuditEvents.Add(new AuditEvent { TenantId = who.TenantId, ActorUserId = who.UserId, EventType = "portal.access.revoked", Outcome = "succeeded", Subject = grant.Id.ToString() });
            await db.SaveChangesAsync(ct);
            await notifications.EnqueueAsync(who.TenantId!.Value, grant.UserId, null, "portal.access.revoked", "Portal access revoked", "Your member portal access has been revoked.", ct);
            return Results.NoContent();
        });
    }

    private static async Task<IResult> Login(LoginRequest request, HttpContext http, NexoraIdentityDbContext db,
        UserManager<NexoraUser> users, SignInManager<NexoraUser> signIn, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password)) return Results.Unauthorized();
        using var system = db.BeginSystemAccess();
        var email = users.NormalizeEmail(request.Email.Trim());
        var user = await db.Users.SingleOrDefaultAsync(x => x.NormalizedEmail == email, ct);
        if (user == null || !user.IsActive || !await db.Tenants.AnyAsync(x => x.Id == user.TenantId && x.Status == TenantStatus.Active, ct)) return Results.Unauthorized();
        var grant = await db.PortalAccess.SingleOrDefaultAsync(x => x.UserId == user.Id && x.TenantId == user.TenantId && !x.IsRevoked, ct);
        if (grant == null || !(await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true)).Succeeded) return Results.Unauthorized();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(NexoraClaimTypes.TenantId, user.TenantId.ToString()),
            new Claim("portal_access", grant.Id.ToString()), new Claim("portal_stamp", user.SecurityStamp ?? "") }, Scheme));
        await http.SignInAsync(Scheme, principal);
        db.AuditEvents.Add(new AuditEvent { TenantId = user.TenantId, ActorUserId = user.Id, EventType = "portal.login", Outcome = "succeeded", Subject = grant.Id.ToString() });
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { authenticated = true });
    }
    public sealed record GrantRequest(Guid UserId, Guid ContactId);
    public sealed record LocalMemberRequest(string Email, string Password, Guid ContactId);
}
