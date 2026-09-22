using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Identity.Domain;
using Nexora.Modules.Identity.Infrastructure.Persistence;

namespace Nexora.Modules.Identity;

internal static partial class PortalEndpoints
{
    private static void MapOrganisations(RouteGroupBuilder own)
    {
        own.MapGet("/organisations", async (HttpContext http, NexoraIdentityDbContext db, ICrmDirectory crm, CancellationToken ct) =>
        {
            var accessId = (Guid)http.Items["portal_access"]!;
            var contactId = (Guid)http.Items["portal_contact"]!;
            var grants = await db.Set<PortalOrganisationGrant>().AsNoTracking().Where(g => g.PortalAccessId == accessId && !g.IsRevoked).ToListAsync(ct);
            var result = new List<object>();
            foreach (var g in grants)
            {
                var account = await crm.FindActiveAsync(g.AccountId, ct);
                if (account?.Kind == "account" && await crm.HasCurrentAccountContactAsync(g.AccountId, contactId, ct))
                    result.Add(new { id = g.AccountId, account.Name, g.CanSubmitApplications });
            }
            return Results.Ok(result);
        });
        var company = own.MapGroup("/organisations/{accountId:guid}");
        company.AddEndpointFilter(async (context, next) =>
        {
            var http = context.HttpContext;
            if (!Guid.TryParse(http.Request.RouteValues["accountId"]?.ToString(), out var accountId)) return Results.NotFound();
            var db = http.RequestServices.GetRequiredService<NexoraIdentityDbContext>();
            var crm = http.RequestServices.GetRequiredService<ICrmDirectory>();
            var accessId = (Guid)http.Items["portal_access"]!;
            var contactId = (Guid)http.Items["portal_contact"]!;
            var g = await db.Set<PortalOrganisationGrant>().AsNoTracking().SingleOrDefaultAsync(g => g.PortalAccessId == accessId && g.AccountId == accountId && !g.IsRevoked, http.RequestAborted);
            if (g == null || (await crm.FindActiveAsync(accountId, http.RequestAborted))?.Kind != "account" ||
                !await crm.HasCurrentAccountContactAsync(accountId, contactId, http.RequestAborted)) return Results.NotFound();
            if ((!HttpMethods.IsGet(http.Request.Method) || http.Request.Path.Value!.Contains("/drafts")) && !g.CanSubmitApplications) return Results.StatusCode(403);
            return await next(context);
        });
        company.MapGet("/application-products", async (IMemberApplicationService service, CancellationToken ct) => Results.Ok(await service.ProductsAsync(ct, true)));
        MapEvidence(company);
        company.MapGet("/memberships", async (Guid accountId, IMemberMembershipDirectory directory, CancellationToken ct) => Results.Ok(await directory.ReadOrganisationAsync(accountId, ct)));
        company.MapGet("/drafts", async (Guid accountId, HttpContext http, IMemberApplicationService service, CancellationToken ct) => Results.Ok(await service.DraftsAsync((Guid)http.Items["portal_contact"]!, ct, accountId)));
        company.MapGet("/applications/{id:guid}", async (Guid accountId, Guid id, HttpContext http, IMemberApplicationService service, CancellationToken ct) => Results.Ok(await service.ReceiptAsync((Guid)http.Items["portal_contact"]!, id, ct, accountId)));
        company.MapPut("/drafts/{id:guid}", async (Guid accountId, Guid id, JsonElement body, HttpContext http, IMemberApplicationService service, CancellationToken ct) =>
        {
            if (!Fields(body, "version", "productId", "startsAtUtc", "notes") ||
                !GuidValue(body, "productId", out var productId) ||
                body.GetProperty("startsAtUtc").ValueKind != JsonValueKind.String || !body.GetProperty("startsAtUtc").TryGetDateTime(out var start) ||
                body.GetProperty("notes").ValueKind != JsonValueKind.String ||
                (body.GetProperty("version").ValueKind != JsonValueKind.Null && !GuidValue(body, "version", out _))) return Results.BadRequest();
            var version = body.GetProperty("version").ValueKind == JsonValueKind.Null ? (Guid?)null : body.GetProperty("version").GetGuid();
            return Results.Ok(await service.SaveAsync((Guid)http.Items["portal_contact"]!, new(id, version, productId, start, body.GetProperty("notes").GetString()!), ct, accountId));
        });
        company.MapPost("/drafts/{id:guid}/submit", async (Guid accountId, Guid id, JsonElement body, HttpContext http, IMemberApplicationService service, CancellationToken ct) =>
        {
            if (!Fields(body, "version", "termsAccepted") || !GuidValue(body, "version", out var version) || body.GetProperty("termsAccepted").ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return Results.BadRequest();
            return Results.Ok(new { applicationId = await service.SubmitAsync((Guid)http.Items["portal_contact"]!, id, version, body.GetProperty("termsAccepted").GetBoolean(), ct, accountId) });
        });
        company.MapDelete("/drafts/{id:guid}", async (Guid accountId, Guid id, [Microsoft.AspNetCore.Mvc.FromBody] JsonElement body, HttpContext http, IMemberApplicationService service, CancellationToken ct) =>
        {
            if (!Fields(body, "version") || !GuidValue(body, "version", out var version)) return Results.BadRequest();
            await service.DiscardAsync((Guid)http.Items["portal_contact"]!, id, version, ct, accountId);
            return Results.NoContent();
        });
    }

    private static bool Fields(JsonElement body, params string[] names) => body.ValueKind == JsonValueKind.Object &&
        body.EnumerateObject().Count() == names.Length && names.All(n => body.TryGetProperty(n, out _));
    private static bool GuidValue(JsonElement body, string name, out Guid value)
    {
        value = default;
        return body.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String && p.TryGetGuid(out value);
    }

    private static void MapOrganisationAdministration(RouteGroupBuilder grants)
    {
        grants.MapGet("/{accessId:guid}/organisations", async (Guid accessId, NexoraIdentityDbContext db, ICrmDirectory crm, CancellationToken ct) =>
        {
            if (!await db.PortalAccess.AnyAsync(a => a.Id == accessId, ct)) return Results.NotFound();
            var rows = await db.Set<PortalOrganisationGrant>().AsNoTracking().Where(g => g.PortalAccessId == accessId).OrderByDescending(g => g.CreatedAtUtc).ToListAsync(ct);
            var result = new List<object>();
            foreach (var row in rows) result.Add(new { row.Id, row.AccountId, name = (await crm.FindActiveAsync(row.AccountId, ct))?.Name ?? "Inactive organisation", row.CanSubmitApplications, row.IsRevoked });
            return Results.Ok(result);
        });
        grants.MapPost("/{accessId:guid}/organisations", async (Guid accessId, JsonElement body, NexoraIdentityDbContext db, ICrmDirectory crm, IRequestIdentity who, CancellationToken ct) =>
        {
            if (!Fields(body, "accountId", "canSubmitApplications") || !GuidValue(body, "accountId", out var accountId) || body.GetProperty("canSubmitApplications").ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return Results.BadRequest();
            var access = await db.PortalAccess.SingleOrDefaultAsync(a => a.Id == accessId && !a.IsRevoked, ct);
            if (access == null || !await db.Users.AnyAsync(u => u.Id == access.UserId && u.IsActive, ct) ||
                (await crm.FindActiveAsync(accountId, ct))?.Kind != "account" || !await crm.HasCurrentAccountContactAsync(accountId, access.ContactId, ct)) return Results.NotFound();
            var grant = new PortalOrganisationGrant { TenantId = who.TenantId!.Value, PortalAccessId = accessId, AccountId = accountId, CanSubmitApplications = body.GetProperty("canSubmitApplications").GetBoolean() };
            db.Add(grant);
            db.AuditEvents.Add(new AuditEvent { TenantId = who.TenantId, ActorUserId = who.UserId, EventType = "portal.organisation.granted", Outcome = "succeeded", Subject = grant.Id.ToString() });
            await db.SaveChangesAsync(ct); return Results.Ok(new { grant.Id });
        });
        grants.MapDelete("/{accessId:guid}/organisations/{id:guid}", async (Guid accessId, Guid id, NexoraIdentityDbContext db, IRequestIdentity who, CancellationToken ct) =>
        {
            var grant = await db.Set<PortalOrganisationGrant>().SingleOrDefaultAsync(g => g.Id == id && g.PortalAccessId == accessId, ct);
            if (grant == null) return Results.NotFound();
            grant.IsRevoked = true;
            db.AuditEvents.Add(new AuditEvent { TenantId = who.TenantId, ActorUserId = who.UserId, EventType = "portal.organisation.revoked", Outcome = "succeeded", Subject = grant.Id.ToString() });
            await db.SaveChangesAsync(ct); return Results.NoContent();
        });
    }
}
