using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Domain;
using Nexora.Modules.Crm.Infrastructure;

namespace Nexora.Modules.Crm;

public static partial class CrmModule
{
    public sealed record ProfileDefinitionRequest(string ParentNode, string ChildNode, Guid? Version = null, bool IsArchived = false);
    public sealed record ProfileSlotRequest(int Slot, Guid DefinitionId);
    public sealed record ProfileAssignmentRequest(Guid Version, ProfileSlotRequest[]? Assignments);

    private static void MapProfileTaxonomy(RouteGroupBuilder api)
    {
        api.MapGet("/profile-definitions", async (CrmDbContext db, CancellationToken ct) => Results.Ok(await db.Set<ProfileDefinition>()
            .OrderBy(x => x.ParentNode).ThenBy(x => x.ChildNode)
            .Select(x => new { x.Id, x.ParentNode, x.ChildNode, x.IsArchived, x.Version }).ToListAsync(ct)));

        api.MapPost("/profile-definitions", async (ProfileDefinitionRequest request, CrmDbContext db, IRequestIdentity who, CancellationToken ct) =>
        {
            var row = new ProfileDefinition { TenantId = who.TenantId!.Value };
            ApplyDefinition(row, request);
            await ValidateDefinitionAsync(db, row, ct);
            db.Add(row); await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/crm/profile-definitions/{row.Id}", row);
        }).RequireAuthorization("crm.manage");

        api.MapPut("/profile-definitions/{definitionId:guid}", async (Guid definitionId, ProfileDefinitionRequest request, CrmDbContext db, CancellationToken ct) =>
        {
            var row = await db.Set<ProfileDefinition>().SingleOrDefaultAsync(x => x.Id == definitionId, ct) ?? throw new KeyNotFoundException();
            if (request.Version != row.Version) throw new DbUpdateConcurrencyException();
            ApplyDefinition(row, request);
            await ValidateDefinitionAsync(db, row, ct);
            row.Version = Guid.NewGuid(); await db.SaveChangesAsync(ct);
            return Results.Ok(row);
        }).RequireAuthorization("crm.manage");

        api.MapPut("/records/{id:guid}/profiles", async (Guid id, ProfileAssignmentRequest request, CrmDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
        {
            var record = await RecordAsync(db, id, ct, true);
            if (request.Version != record.Version) throw new DbUpdateConcurrencyException();
            var assignments = request.Assignments ?? throw new ValidationException("Provide profile assignments; use an empty list to clear them.");
            if (assignments.Length > 5 || assignments.Any(x => x == null || x.Slot is < 1 or > 5 || x.DefinitionId == Guid.Empty) ||
                assignments.Select(x => x.Slot).Distinct().Count() != assignments.Length || assignments.Select(x => x.DefinitionId).Distinct().Count() != assignments.Length)
                throw new ValidationException("Choose distinct profiles in slots 1 to 5.");
            var existing = await db.Set<RecordProfileAssignment>().Where(x => x.RecordId == id).ToListAsync(ct);
            var ids = assignments.Select(x => x.DefinitionId).ToArray();
            var definitions = await db.Set<ProfileDefinition>().Where(x => ids.Contains(x.Id)).ToListAsync(ct);
            if (definitions.Count != ids.Length) throw new ValidationException("A selected profile is unavailable.");
            if (assignments.Any(x => definitions.Single(d => d.Id == x.DefinitionId).IsArchived && !existing.Any(e => e.Slot == x.Slot && e.DefinitionId == x.DefinitionId)))
                throw new ValidationException("Archived profiles may be retained or removed, but cannot be newly assigned.");
            foreach (var slot in Enumerable.Range(1, 5))
            {
                var chosen = assignments.SingleOrDefault(x => x.Slot == slot);
                var stored = existing.SingleOrDefault(x => x.Slot == slot);
                if (chosen == null) { if (stored != null) db.Remove(stored); }
                else if (stored != null) stored.DefinitionId = chosen.DefinitionId;
                else db.Add(new RecordProfileAssignment { TenantId = record.TenantId, RecordId = id, Slot = slot, DefinitionId = chosen.DefinitionId });
            }
            record.Version = Guid.NewGuid(); record.UpdatedAtUtc = DateTime.UtcNow;
            Audit(db, who, http, id, "profiles.updated"); await db.SaveChangesAsync(ct);
            return Results.Ok(new { record.Version });
        }).RequireAuthorization("crm.manage");
    }

    private static void ApplyDefinition(ProfileDefinition row, ProfileDefinitionRequest request)
    {
        if (request.ParentNode?.Any(char.IsControl) == true || request.ChildNode?.Any(char.IsControl) == true)
            throw new ValidationException("Profile names cannot contain control characters.");
        row.ParentNode = Required(request.ParentNode, 160); row.ChildNode = Required(request.ChildNode, 160);
        row.NormalizedParent = row.ParentNode.ToUpperInvariant(); row.NormalizedChild = row.ChildNode.ToUpperInvariant(); row.IsArchived = request.IsArchived;
    }

    private static async Task ValidateDefinitionAsync(CrmDbContext db, ProfileDefinition row, CancellationToken ct)
    {
        if (await db.Set<ProfileDefinition>().AnyAsync(x => x.Id != row.Id && x.NormalizedParent == row.NormalizedParent && x.NormalizedChild == row.NormalizedChild, ct))
            throw new ValidationException("This parent and child profile already exists.");
    }
}
