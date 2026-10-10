using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Contracts;
using Nexora.Modules.Crm.Domain;
using Nexora.Modules.Crm.Infrastructure;

namespace Nexora.Modules.Crm;

public static partial class CrmModule
{
    private static void MapAddresses(RouteGroupBuilder api)
    {
        api.MapPost("/records/{id:guid}/addresses", async (Guid id, AddressRequest request, CrmDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
        {
            var parent = await RecordAsync(db, id, ct, true);
            if (request.Version.HasValue && request.Version.Value != parent.Version) throw new DbUpdateConcurrencyException();
            var row = new Address(); ApplyAddress(row, request);
            await ValidateAddressPurposeAsync(db, parent, row, ct);
            return await AddChildAsync(id, row, "address.added", db, who, http, ct);
        }).RequireAuthorization("crm.manage");

        api.MapPut("/records/{id:guid}/addresses/{addressId:guid}", async (Guid id, Guid addressId, AddressRequest request, CrmDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
        {
            var parent = await RecordAsync(db, id, ct, true);
            if (!request.Version.HasValue) throw new ValidationException("Supply the current record version.");
            if (request.Version.Value != parent.Version) throw new DbUpdateConcurrencyException();
            var row = await db.Set<Address>().SingleOrDefaultAsync(x => x.RecordId == id && x.Id == addressId, ct) ?? throw new KeyNotFoundException();
            if (parent.BillingUsesMailingAddress && row.Purpose == "mailing" && request.Purpose != "mailing")
                throw new ValidationException("Turn off billing same as mailing before changing the mailing address purpose.");
            ApplyAddress(row, request); await ValidateAddressPurposeAsync(db, parent, row, ct);
            parent.Version = Guid.NewGuid(); parent.UpdatedAtUtc = DateTime.UtcNow;
            Audit(db, who, http, id, "address.updated"); await db.SaveChangesAsync(ct);
            return Results.Ok(new { row.Id, parent.Version });
        }).RequireAuthorization("crm.manage");

        api.MapPut("/records/{id:guid}/billing-address", async (Guid id, BillingAddressRequest request, CrmDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
        {
            var parent = await RecordAsync(db, id, ct, true);
            if (parent.Version != request.Version) throw new DbUpdateConcurrencyException();
            if (request.UseMailingAddress && !await db.Set<Address>().AnyAsync(x => x.RecordId == id && x.Purpose == "mailing", ct))
                throw new ValidationException("Add a mailing address before using it for billing.");
            parent.BillingUsesMailingAddress = request.UseMailingAddress;
            parent.Version = Guid.NewGuid(); parent.UpdatedAtUtc = DateTime.UtcNow;
            Audit(db, who, http, id, "address.billing-source.updated"); await db.SaveChangesAsync(ct);
            return Results.Ok(new { parent.Version });
        }).RequireAuthorization("crm.manage");
    }

    private static void ApplyAddress(Address row, AddressRequest request)
    {
        Choice(request.Purpose, "mailing", "billing", "other");
        row.Label = Required(request.Label, 80); row.Purpose = request.Purpose;
        row.AddressType = Optional(request.AddressType, 80); row.AccountName = Optional(request.AccountName, 160); row.JobTitle = Optional(request.JobTitle, 160);
        row.Line1 = Required(request.Line1, 200); row.Line2 = Optional(request.Line2, 200); row.Line3 = Optional(request.Line3, 200); row.Line4 = Optional(request.Line4, 200);
        row.City = Required(request.City, 100); row.Region = Optional(request.Region, 100); row.County = Optional(request.County, 100);
        row.PostalCode = Required(request.PostalCode, 30); row.Country = Required(request.Country, 80);
    }

    private static async Task ValidateAddressPurposeAsync(CrmDbContext db, CrmRecord parent, Address row, CancellationToken ct)
    {
        if (row.Purpose == "billing" && parent.BillingUsesMailingAddress)
            throw new ValidationException("Turn off billing same as mailing before adding or editing a separate billing address.");
        if (row.Purpose != "other" && await db.Set<Address>().AnyAsync(x => x.RecordId == parent.Id && x.Id != row.Id && x.Purpose == row.Purpose, ct))
            throw new ValidationException($"A {row.Purpose} address already exists. Edit it or change its purpose to Other first.");
    }
}
