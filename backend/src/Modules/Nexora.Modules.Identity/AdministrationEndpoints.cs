using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.Modules.Identity.Domain;
using Nexora.Modules.Identity.Infrastructure.Persistence;
using Nexora.Modules.Identity.Security;
using static Nexora.BuildingBlocks.Endpoints.ModuleEndpoints;

namespace Nexora.Modules.Identity;

internal static class AdministrationEndpoints
{
    internal static void MapAdministration(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api/v1/admin").WithTags("Administration").RequireAuthorization(NexoraPermissions.IdentityRead);
        api.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            try
            {
                if (!HttpMethods.IsGet(context.HttpContext.Request.Method))
                    await context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context.HttpContext);
                return await next(context);
            }
            catch (ValidationException e) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [e.Message] }); }
            catch (AntiforgeryValidationException) { return Results.Problem(statusCode: 400, title: "The security token is invalid or expired."); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (TenantIsolationException) { return Results.Forbid(); }
            catch (DbUpdateConcurrencyException) { return Conflict(); }
            catch (DbUpdateException) { return Results.Problem(statusCode: 409, title: "The change conflicts with existing data."); }
        });
        api.MapGet("/permissions", () => NexoraPermissions.All);
        api.MapGet("/users", ListUsers);
        api.MapGet("/security-groups", ListGroups);
        api.MapPost("/users", CreateUser).RequireAuthorization(NexoraPermissions.IdentityManage);
        api.MapPatch("/users/{id:guid}", UpdateUser).RequireAuthorization(NexoraPermissions.IdentityManage);
        api.MapDelete("/users/{id:guid}", DeleteUser).RequireAuthorization(NexoraPermissions.IdentityManage);
        api.MapPost("/security-groups", CreateGroup).RequireAuthorization(NexoraPermissions.IdentityManage);
        api.MapPatch("/security-groups/{id:guid}", UpdateGroup).RequireAuthorization(NexoraPermissions.IdentityManage);
        api.MapDelete("/security-groups/{id:guid}", DeleteGroup).RequireAuthorization(NexoraPermissions.IdentityManage);
    }

    private static async Task<List<AdminUser>> ListUsers(NexoraIdentityDbContext db, CancellationToken ct)
    {
        var users = await db.Users.AsNoTracking().OrderBy(x => x.DisplayName).ToListAsync(ct);
        var memberships = await (from membership in db.UserRoles join role in db.Roles on membership.RoleId equals role.Id select new { membership.UserId, role.Id, role.Name, role.IsSystem }).ToListAsync(ct);
        return users.Select(user => new AdminUser(user.Id, user.DisplayName, user.Email!, user.IsActive, user.ConcurrencyStamp!, user.PasswordHash != null,
            memberships.Any(x => x.UserId == user.Id && x.IsSystem), memberships.Where(x => x.UserId == user.Id).Select(x => x.Id).ToArray())).ToList();
    }

    private static async Task<List<AdminGroup>> ListGroups(NexoraIdentityDbContext db, CancellationToken ct)
    {
        var roles = await db.Roles.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct);
        var permissions = await db.RolePermissions.AsNoTracking().ToListAsync(ct);
        var memberships = await db.UserRoles.AsNoTracking().ToListAsync(ct);
        return roles.Select(role => new AdminGroup(role.Id, role.Name!, role.Description ?? "", role.IsSystem, role.ConcurrencyStamp!,
            role.IsSystem ? NexoraPermissions.All : permissions.Where(x => x.RoleId == role.Id).Select(x => x.Permission).Order().ToArray(), memberships.Count(x => x.RoleId == role.Id))).ToList();
    }

    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded) throw new ValidationException(string.Join(" ", result.Errors.Select(x => x.Description)));
    }

    private static async Task<List<NexoraRole>> Groups(Guid[]? ids, NexoraIdentityDbContext db, CancellationToken ct)
    {
        Check(ids != null && ids.Length > 0, "Choose at least one security group.");
        var unique = ids!.Distinct().ToArray();
        var groups = await db.Roles.Where(x => unique.Contains(x.Id)).ToListAsync(ct);
        Check(groups.Count == unique.Length, "A security group is unavailable.");
        return groups;
    }

    private static void ValidateUser(UserRequest r)
    {
        Text(r.DisplayName); Text(r.Email, 256);
        Check(new EmailAddressAttribute().IsValid(r.Email.Trim()), "Enter a valid email login name.");
        Check(r.Password == null || r.Password.Length <= 128, "Password must not exceed 128 characters.");
    }

    private static async Task<IResult> CreateUser(UserRequest r, NexoraIdentityDbContext db, UserManager<NexoraUser> users, HttpContext http, CancellationToken ct)
    {
        ValidateUser(r); Check(!string.IsNullOrWhiteSpace(r.Password), "Set an initial password.");
        var groups = await Groups(r.GroupIds, db, ct);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var user = new NexoraUser { TenantId = Tenant(http), DisplayName = Text(r.DisplayName), Email = r.Email.Trim(), UserName = r.Email.Trim(), IsActive = r.IsActive, EmailConfirmed = true };
        Ensure(await users.CreateAsync(user, r.Password!));
        Ensure(await users.AddToRolesAsync(user, groups.Select(x => x.Name!)));
        Audit(db, http, "identity.user.created", user.Id); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return Results.Ok((await ListUsers(db, ct)).Single(x => x.Id == user.Id));
    }

    private static async Task<IResult> UpdateUser(Guid id, UserRequest r, NexoraIdentityDbContext db, UserManager<NexoraUser> users, HttpContext http, CancellationToken ct)
    {
        ValidateUser(r);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
        if (user.ConcurrencyStamp != r.ConcurrencyStamp) return Conflict();
        var groups = await Groups(r.GroupIds, db, ct);
        var existingRoles = await users.GetRolesAsync(user);
        var protectedGroups = await db.Roles.Where(x => x.IsSystem && existingRoles.Contains(x.Name!)).Select(x => x.Id).ToListAsync(ct);
        Check(protectedGroups.Count == 0 || (r.IsActive && protectedGroups.All(x => groups.Any(g => g.Id == x))), "Admin accounts must remain active and keep their Admin security group.");
        Check(user.Id != Actor(http) || r.IsActive, "You cannot disable your own account.");
        Check(user.Id != Actor(http) || groups.Any(g => g.IsSystem) || await db.RolePermissions.AnyAsync(x => groups.Select(g => g.Id).Contains(x.RoleId) && x.Permission == NexoraPermissions.IdentityManage, ct), "You cannot remove your own administration access.");
        user.DisplayName = Text(r.DisplayName); user.Email = r.Email.Trim(); user.UserName = r.Email.Trim(); user.IsActive = r.IsActive;
        Ensure(await users.UpdateAsync(user));
        var desired = groups.Select(x => x.Name!).ToArray();
        var removed = existingRoles.Except(desired).ToArray(); var added = desired.Except(existingRoles).ToArray();
        if (removed.Length > 0) Ensure(await users.RemoveFromRolesAsync(user, removed));
        if (added.Length > 0) Ensure(await users.AddToRolesAsync(user, added));
        if (!string.IsNullOrEmpty(r.Password)) Ensure(await users.ResetPasswordAsync(user, await users.GeneratePasswordResetTokenAsync(user), r.Password));
        Audit(db, http, string.IsNullOrEmpty(r.Password) ? "identity.user.updated" : "identity.user.password-reset", user.Id);
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return Results.Ok((await ListUsers(db, ct)).Single(x => x.Id == id));
    }

    private static async Task<IResult> DeleteUser(Guid id, [FromBody] VersionRequest r, NexoraIdentityDbContext db, UserManager<NexoraUser> users, HttpContext http, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
        if (user.ConcurrencyStamp != r.ConcurrencyStamp) return Conflict();
        Check(id != Actor(http), "You cannot delete your own account.");
        Check(!await (from membership in db.UserRoles join role in db.Roles on membership.RoleId equals role.Id where membership.UserId == id && role.IsSystem select role).AnyAsync(ct), "Admin accounts cannot be deleted.");
        // Portal identities have their own managed access and retained grant history.
        Check(!await db.PortalAccess.AnyAsync(x => x.UserId == id, ct), "This user has portal access history. Disable the account instead of deleting it.");
        db.TeamMembers.RemoveRange(await db.TeamMembers.Where(x => x.UserId == id).ToListAsync(ct));
        await db.SaveChangesAsync(ct);
        Ensure(await users.DeleteAsync(user)); Audit(db, http, "identity.user.deleted", id);
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return Results.NoContent();
    }

    private static void ValidateGroup(GroupRequest r)
    {
        Text(r.Name, 80); Check(r.Description != null && r.Description.Length <= 300, "Description must not exceed 300 characters.");
        Check(r.Permissions != null && r.Permissions.All(NexoraPermissions.All.Contains), "Choose only supported permissions.");
        foreach (var permission in r.Permissions!)
        {
            var module = permission.Split('.')[0]; var read = module + ".read";
            Check(!NexoraPermissions.All.Contains(read) || r.Permissions.Contains(read), "Include View access for every selected module.");
        }
    }

    private static async Task<IResult> CreateGroup(GroupRequest r, NexoraIdentityDbContext db, RoleManager<NexoraRole> roles, HttpContext http, CancellationToken ct)
    {
        ValidateGroup(r);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var role = new NexoraRole { TenantId = Tenant(http), Name = Text(r.Name, 80), Description = r.Description.Trim() };
        Ensure(await roles.CreateAsync(role));
        db.RolePermissions.AddRange(r.Permissions.Distinct().Select(permission => new RolePermission { RoleId = role.Id, Permission = permission }));
        Audit(db, http, "identity.group.created", role.Id); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return Results.Ok((await ListGroups(db, ct)).Single(x => x.Id == role.Id));
    }

    private static async Task<IResult> UpdateGroup(Guid id, GroupRequest r, NexoraIdentityDbContext db, RoleManager<NexoraRole> roles, HttpContext http, CancellationToken ct)
    {
        ValidateGroup(r);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var role = await db.Roles.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
        if (role.ConcurrencyStamp != r.ConcurrencyStamp) return Conflict();
        Check(!role.IsSystem, "The Admin security group has permanent full access and cannot be changed.");
        var ownRole = await db.UserRoles.AnyAsync(x => x.RoleId == id && x.UserId == Actor(http), ct);
        if (ownRole && !r.Permissions.Contains(NexoraPermissions.IdentityManage))
        {
            var otherRoleIds = db.UserRoles.Where(x => x.UserId == Actor(http) && x.RoleId != id).Select(x => x.RoleId);
            Check(await db.Roles.AnyAsync(x => otherRoleIds.Contains(x.Id) && x.IsSystem, ct) || await db.RolePermissions.AnyAsync(x => otherRoleIds.Contains(x.RoleId) && x.Permission == NexoraPermissions.IdentityManage, ct), "You cannot remove your own administration access.");
        }
        // Identity user-role membership is keyed by role ID, so renaming preserves membership.
        role.Name = Text(r.Name, 80); role.Description = r.Description.Trim(); Ensure(await roles.UpdateAsync(role));
        db.RolePermissions.RemoveRange(await db.RolePermissions.Where(x => x.RoleId == id).ToListAsync(ct));
        await db.SaveChangesAsync(ct);
        db.RolePermissions.AddRange(r.Permissions.Distinct().Select(permission => new RolePermission { RoleId = id, Permission = permission }));
        Audit(db, http, "identity.group.updated", id); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return Results.Ok((await ListGroups(db, ct)).Single(x => x.Id == id));
    }

    private static async Task<IResult> DeleteGroup(Guid id, [FromBody] VersionRequest r, NexoraIdentityDbContext db, RoleManager<NexoraRole> roles, HttpContext http, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var role = await db.Roles.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
        if (role.ConcurrencyStamp != r.ConcurrencyStamp) return Conflict();
        Check(!role.IsSystem, "The Admin security group cannot be deleted.");
        Check(!await db.UserRoles.AnyAsync(x => x.RoleId == id, ct), "Reassign this group's users before deleting it.");
        Ensure(await roles.DeleteAsync(role)); Audit(db, http, "identity.group.deleted", id);
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return Results.NoContent();
    }

    private static Guid Tenant(HttpContext http) => Guid.Parse(http.User.FindFirstValue(NexoraClaimTypes.TenantId)!);
    private static Guid Actor(HttpContext http) => Guid.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static void Audit(NexoraIdentityDbContext db, HttpContext http, string action, Guid subject) => db.AuditEvents.Add(new AuditEvent { TenantId = Tenant(http), ActorUserId = Actor(http), EventType = action, Outcome = "succeeded", Subject = subject.ToString(), IpAddress = http.Connection.RemoteIpAddress?.ToString() });
}

public sealed record AdminUser(Guid Id, string DisplayName, string Email, bool IsActive, string ConcurrencyStamp, bool HasPassword, bool IsProtected, Guid[] GroupIds);
public sealed record AdminGroup(Guid Id, string Name, string Description, bool IsSystem, string ConcurrencyStamp, string[] Permissions, int MemberCount);
public sealed record UserRequest(string DisplayName, string Email, bool IsActive, Guid[] GroupIds, string? Password = null, string? ConcurrencyStamp = null);
public sealed record GroupRequest(string Name, string Description, string[] Permissions, string? ConcurrencyStamp = null);
public sealed record VersionRequest(string ConcurrencyStamp);
