using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Membership.Domain;
using Nexora.Modules.Membership.Infrastructure;

namespace Nexora.Modules.Membership;

public static partial class MembershipModule
{
    public sealed record MembershipOptionsRequest(Guid Version, MembershipOptionsDetails? Options, string Reason);
    private static void MapMembershipOptions(RouteGroupBuilder api)
    {
        api.MapGet("/members/{id:guid}/options", async (Guid id, MembershipDbContext db, CancellationToken ct) =>
        {
            var member = await db.Set<MemberSubscription>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
            return Results.Ok(new { member.Version, options = ReadOptions(member) });
        });
        api.MapPut("/members/{id:guid}/options", async (Guid id, MembershipOptionsRequest request, MembershipDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
        {
            var member = await db.Set<MemberSubscription>().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
            if (member.Version != request.Version) throw new DbUpdateConcurrencyException();
            var options = request.Options ?? throw new ValidationException("Provide membership options.");
            var reason = AdministrativeText(request.Reason, 500, false) ?? throw new ValidationException("Provide a reason for changing membership options.");
            member.ExemptJoiningFee = options.ExemptJoiningFee; member.FreeMembership = options.FreeMembership; member.ExcludedFromLapse = options.ExcludedFromLapse; member.PrimaryMembership = options.PrimaryMembership;
            member.UpdatedAtUtc = DateTime.UtcNow;
            History(db, who, http, id, "membership.options.updated", reason);
            await db.SaveChangesAsync(ct); return Results.Ok(new { member.Version, options = ReadOptions(member) });
        }).RequireAuthorization("membership.manage");
        api.MapPut("/products/{id:guid}/joining-fee", async (Guid id, JoiningFeeRequest request, MembershipDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
        {
            _ = await db.Set<MembershipProduct>().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
            if (request.JoiningFee < 0 || request.JoiningFee >= 100000000 || decimal.Round(request.JoiningFee, 2) != request.JoiningFee) throw new ValidationException("Joining fee must be a nonnegative amount with two decimal places.");
            var reason = AdministrativeText(request.Reason, 500, false) ?? throw new ValidationException("Provide a joining-fee change reason.");
            var policy = await db.Set<MembershipPolicy>().SingleOrDefaultAsync(x => x.ProductId == id, ct);
            if (policy?.Version != request.Version) throw new DbUpdateConcurrencyException();
            if (policy == null) { policy = new MembershipPolicy { TenantId = who.TenantId!.Value, ProductId = id }; db.Add(policy); }
            policy.JoiningFee = request.JoiningFee; policy.UpdatedAtUtc = DateTime.UtcNow;
            History(db, who, http, id, "policy.joining-fee.updated", reason);
            await db.SaveChangesAsync(ct); return Results.Ok(policy);
        }).RequireAuthorization("membership.manage");
    }
    public sealed record JoiningFeeRequest(Guid? Version, decimal JoiningFee, string Reason);
    private static MembershipOptionsDetails ReadOptions(MemberSubscription member) => new(member.ExemptJoiningFee, member.FreeMembership, member.ExcludedFromLapse, member.PrimaryMembership);
}
