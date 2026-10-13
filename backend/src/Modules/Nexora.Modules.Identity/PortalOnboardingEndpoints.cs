using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Identity.Domain;
using Nexora.Modules.Identity.Infrastructure.Persistence;
using Nexora.Modules.Identity.Security;

namespace Nexora.Modules.Identity;

internal static partial class PortalEndpoints
{
    private static bool TextValue(JsonElement body, string name, int max, bool required, out string value, bool multiline = false)
    {
        value = "";
        if (!body.TryGetProperty(name, out var field) || field.ValueKind != JsonValueKind.String) return false;
        value = field.GetString()!.Trim();
        return value.Length <= max && (!required || value.Length > 0) && !value.Any(c => char.IsControl(c) && !(multiline && c is '\n' or '\r' or '\t'));
    }
    private static void MapRegistration(RouteGroupBuilder portal)
    {
        portal.MapGet("/onboarding", async (IPortalRegistrationDirectory directory, CancellationToken ct) =>
        {
            var site = await directory.ResolveAsync(ct);
            return Results.Ok(new { allowSelfRegistration = site?.AllowSelfRegistration == true });
        });
        portal.MapPost("/register", async (JsonElement body, HttpContext http, IPortalRegistrationDirectory directory,
            IPortalOnboardingCrm crm, NexoraIdentityDbContext db, UserManager<NexoraUser> users, CancellationToken ct) =>
        {
            if (http.User.Identity?.IsAuthenticated == true) return Results.Conflict();
            var site = await directory.ResolveAsync(ct);
            if (site?.AllowSelfRegistration != true) return Results.NotFound();
            if (!Fields(body, "firstName", "lastName", "email", "password") ||
                !TextValue(body, "firstName", 70, true, out var first) || !TextValue(body, "lastName", 70, true, out var last) ||
                !TextValue(body, "email", 254, true, out var email) || !new EmailAddressAttribute().IsValid(email) ||
                body.GetProperty("password").ValueKind != JsonValueKind.String) return Results.BadRequest();
            var password = body.GetProperty("password").GetString()!;
            if (password.Length is < 12 or > 128) return Results.Problem(statusCode: 400, title: "Use a password of 12–128 characters with uppercase, lowercase, a number and a symbol.");
            // Tenant comes only from the trusted host resolver. UserManager needs global
            // email uniqueness; no existing CRM contact is claimed by name or email.
            using var system = db.BeginSystemAccess();
            if (!await db.Tenants.AnyAsync(t => t.Id == site.TenantId && t.Status == TenantStatus.Active, ct)) return Results.NotFound();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var user = new NexoraUser { Id = Guid.NewGuid(), TenantId = site.TenantId, DisplayName = $"{first} {last}", Email = email, UserName = email };
            var created = await users.CreateAsync(user, password);
            if (!created.Succeeded) return Results.Problem(statusCode: 400, title: "Unable to create account. Use an unused valid email and a password with uppercase, lowercase, a number and a symbol.");
            var contactId = Guid.NewGuid();
            await crm.CreateContactAsync(site.TenantId, user.Id, contactId, first, last, email,
                db.Database.GetDbConnection(), transaction.GetDbTransaction(), ct);
            var access = new PortalAccess { TenantId = site.TenantId, UserId = user.Id, ContactId = contactId };
            db.Add(access);
            db.AuditEvents.Add(new AuditEvent { TenantId = site.TenantId, ActorUserId = user.Id, EventType = "portal.member.registered", Outcome = "succeeded", Subject = access.Id.ToString() });
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            var principal = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(NexoraClaimTypes.TenantId, user.TenantId.ToString()),
                new Claim("portal_access", access.Id.ToString()), new Claim("portal_stamp", user.SecurityStamp ?? "")], Scheme));
            await http.SignInAsync(Scheme, principal);
            return Results.Ok(new { authenticated = true });
        }).RequireRateLimiting("portal-registration");
    }

    private static object RequestSummary(PortalOrganisationRequest row) => new
    {
        row.Id, row.OrganisationName, row.JobTitle, row.Message, row.Status, row.Version, row.CreatedAtUtc,
        row.DecidedAtUtc, row.DecisionMessage, row.CanSubmitApplications
    };
    private static int? RequestPage(string? page) => page == null ? 1 : int.TryParse(page, out var number) && number is >= 1 and <= 100000 ? number : null;

    private static void MapOrganisationRequests(RouteGroupBuilder own)
    {
        own.MapGet("/organisation-requests", async (string? page, HttpContext http, NexoraIdentityDbContext db, CancellationToken ct) =>
        {
            var number = RequestPage(page); if (number == null) return Results.BadRequest();
            var accessId = (Guid)http.Items["portal_access"]!;
            var query = db.Set<PortalOrganisationRequest>().AsNoTracking().Where(x => x.PortalAccessId == accessId);
            var totalCount = await query.CountAsync(ct);
            var rows = await query.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id).Skip((number.Value - 1) * 20).Take(20).ToArrayAsync(ct);
            return Results.Ok(new { items = rows.Select(RequestSummary), page = number, pageSize = 20, totalCount });
        });
        own.MapPost("/organisation-requests", async (JsonElement body, HttpContext http, NexoraIdentityDbContext db, IPortalPresentationDirectory presentation, CancellationToken ct) =>
        {
            if ((await presentation.ReadAsync(ct)).Onboarding?.AllowOrganisationRequests != true) return Results.NotFound();
            if (!Fields(body, "organisationName", "jobTitle", "message") || !TextValue(body, "organisationName", 160, true, out var name) ||
                !TextValue(body, "jobTitle", 160, false, out var title) || !TextValue(body, "message", 2000, true, out var message, multiline: true)) return Results.BadRequest();
            var accessId = (Guid)http.Items["portal_access"]!;
            if (await db.Set<PortalOrganisationRequest>().CountAsync(x => x.PortalAccessId == accessId && x.Status == "pending", ct) >= 10)
                return Results.Problem(statusCode: 400, title: "You already have 10 requests awaiting review.");
            var normalized = name.ToUpperInvariant();
            if (await db.Set<PortalOrganisationRequest>().AnyAsync(x => x.PortalAccessId == accessId && x.Status == "pending" && x.NormalizedName == normalized, ct))
                return Results.Problem(statusCode: 409, title: "You already have a pending request for this company.");
            var row = new PortalOrganisationRequest { TenantId = Guid.Parse(http.User.FindFirstValue(NexoraClaimTypes.TenantId)!),
                PortalAccessId = accessId, OrganisationName = name, NormalizedName = normalized, JobTitle = title, Message = message };
            db.Add(row);
            db.AuditEvents.Add(new AuditEvent { TenantId = row.TenantId, ActorUserId = Guid.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier)!),
                EventType = "portal.organisation.requested", Outcome = "succeeded", Subject = row.Id.ToString() });
            await db.SaveChangesAsync(ct);
            return Results.Ok(RequestSummary(row));
        });
        own.MapPost("/organisation-requests/{id:guid}/cancel", async (Guid id, JsonElement body, HttpContext http, NexoraIdentityDbContext db, CancellationToken ct) =>
        {
            if (!Fields(body, "version") || !GuidValue(body, "version", out var version)) return Results.BadRequest();
            var accessId = (Guid)http.Items["portal_access"]!;
            var row = await db.Set<PortalOrganisationRequest>().SingleOrDefaultAsync(x => x.Id == id && x.PortalAccessId == accessId, ct);
            if (row == null) return Results.NotFound();
            if (row.Version != version || row.Status != "pending") return Results.Conflict();
            row.Status = "cancelled"; row.Version = Guid.NewGuid(); row.DecidedAtUtc = DateTime.UtcNow;
            db.AuditEvents.Add(new AuditEvent { TenantId = row.TenantId, ActorUserId = Guid.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier)!),
                EventType = "portal.organisation.request.cancelled", Outcome = "succeeded", Subject = row.Id.ToString() });
            await db.SaveChangesAsync(ct); return Results.NoContent();
        });
    }

    private static void MapOrganisationRequestReview(RouteGroupBuilder grants)
    {
        grants.MapGet("/organisation-requests", async (string? page, NexoraIdentityDbContext db, IMemberProfileDirectory contacts, CancellationToken ct) =>
        {
            var number = RequestPage(page); if (number == null) return Results.BadRequest();
            var query = db.Set<PortalOrganisationRequest>().AsNoTracking();
            var totalCount = await query.CountAsync(ct);
            var rows = await query.OrderBy(x => x.Status != "pending").ThenByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id)
                .Skip((number.Value - 1) * 20).Take(20).ToArrayAsync(ct);
            var items = new List<object>();
            foreach (var row in rows)
            {
                var access = await db.PortalAccess.AsNoTracking().SingleAsync(x => x.Id == row.PortalAccessId, ct);
                var user = await db.Users.AsNoTracking().SingleAsync(x => x.Id == access.UserId, ct);
                items.Add(new { request = RequestSummary(row), contactId = access.ContactId, contactName = (await contacts.ReadAsync(access.ContactId, ct))?.Name,
                    email = user.Email, accessIsActive = !access.IsRevoked && user.IsActive });
            }
            return Results.Ok(new { items, page = number, pageSize = 20, totalCount });
        });
        grants.MapPost("/organisation-requests/{id:guid}/decision", async (Guid id, JsonElement body, NexoraIdentityDbContext db,
            IRequestIdentity who, IPortalOnboardingCrm crm, CancellationToken ct) =>
        {
            if (!Fields(body, "version", "decision", "accountId", "canSubmitApplications", "decisionMessage") || !GuidValue(body, "version", out var version) ||
                !TextValue(body, "decision", 20, true, out var decision) || decision is not ("approved" or "rejected") ||
                !TextValue(body, "decisionMessage", 1000, false, out var message, multiline: true) || body.GetProperty("canSubmitApplications").ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return Results.BadRequest();
            Guid accountId = default;
            if (decision == "approved" ? !GuidValue(body, "accountId", out accountId) : body.GetProperty("accountId").ValueKind != JsonValueKind.Null || body.GetProperty("canSubmitApplications").GetBoolean()) return Results.BadRequest();
            var row = await db.Set<PortalOrganisationRequest>().SingleOrDefaultAsync(x => x.Id == id, ct);
            if (row == null) return Results.NotFound();
            if (row.Version != version || row.Status != "pending") return Results.Conflict();
            var access = await db.PortalAccess.SingleOrDefaultAsync(x => x.Id == row.PortalAccessId, ct);
            if (access == null || (decision == "approved" && (access.IsRevoked || !await db.Users.AnyAsync(x => x.Id == access.UserId && x.IsActive, ct)))) return Results.NotFound();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            if (decision == "approved")
            {
                if (await db.Set<PortalOrganisationGrant>().AnyAsync(x => x.PortalAccessId == access.Id && x.AccountId == accountId && !x.IsRevoked, ct))
                    return Results.Problem(statusCode: 409, title: "This member already has access to that organisation. Review their existing grant.");
                await crm.AssociateAsync(who.TenantId!.Value, who.UserId!.Value, access.ContactId, accountId, row.JobTitle,
                    db.Database.GetDbConnection(), transaction.GetDbTransaction(), ct);
                row.AccountId = accountId; row.CanSubmitApplications = body.GetProperty("canSubmitApplications").GetBoolean();
                db.Add(new PortalOrganisationGrant { TenantId = row.TenantId, PortalAccessId = access.Id, AccountId = accountId, CanSubmitApplications = row.CanSubmitApplications });
            }
            row.Status = decision; row.Version = Guid.NewGuid(); row.DecisionMessage = message; row.DecidedAtUtc = DateTime.UtcNow; row.DecidedBy = who.UserId;
            db.AuditEvents.Add(new AuditEvent { TenantId = row.TenantId, ActorUserId = who.UserId, EventType = $"portal.organisation.request.{decision}", Outcome = "succeeded", Subject = row.Id.ToString() });
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
            return Results.Ok(RequestSummary(row));
        });
    }
}
