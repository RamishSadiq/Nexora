using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nexora.Modules.Identity.Domain;
using Nexora.Modules.Identity.Infrastructure.Persistence;

namespace Nexora.Modules.Identity.Security;

public interface ILocalIdentityAdapter
{
    Task<IResult> SignInAsync(LoginRequest request, HttpContext context, CancellationToken cancellationToken);
}

internal sealed record LocalIdentitySettings(bool Enabled);

internal sealed class LocalIdentityAdapter(
    LocalIdentitySettings settings,
    UserManager<NexoraUser> userManager,
    SignInManager<NexoraUser> signInManager,
    NexoraIdentityDbContext dbContext) : ILocalIdentityAdapter
{
    public async Task<IResult> SignInAsync(LoginRequest request, HttpContext httpContext, CancellationToken cancellationToken)
    {
        if (!settings.Enabled) return Results.Problem(statusCode: 503, title: "Local sign-in is disabled. Microsoft Entra sign-in is not yet connected.");
        using var systemAccess = dbContext.BeginSystemAccess();
        var normalizedEmail = userManager.NormalizeEmail(request.Email.Trim());
        var user = await userManager.Users.SingleOrDefaultAsync(
            candidate => candidate.NormalizedEmail == normalizedEmail,
            cancellationToken);

        var tenantIsActive = user is not null && await dbContext.Tenants.AnyAsync(
            tenant => tenant.Id == user.TenantId && tenant.Status == TenantStatus.Active,
            cancellationToken);

        if (user is null || !user.IsActive || !tenantIsActive)
        {
            await RecordLoginAsync(dbContext, null, request.Email, "denied", httpContext, cancellationToken);
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Email or password is incorrect.");
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            request.Password,
            request.RememberMe,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            await RecordLoginAsync(dbContext, user, user.Email, result.IsLockedOut ? "locked" : "denied", httpContext, cancellationToken);
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: result.IsLockedOut
                    ? "This account is temporarily locked. Try again later."
                    : "Email or password is incorrect.");
        }

        await RecordLoginAsync(dbContext, user, user.Email, "succeeded", httpContext, cancellationToken);
        return Results.Ok(new { authenticated = true });
    }

    private static async Task RecordLoginAsync(
        NexoraIdentityDbContext dbContext,
        NexoraUser? user,
        string? subject,
        string outcome,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var normalizedSubject = subject?.Trim().ToLowerInvariant();
        dbContext.AuditEvents.Add(new AuditEvent
        {
            TenantId = user?.TenantId,
            ActorUserId = user?.Id,
            EventType = "identity.login",
            Outcome = outcome,
            Subject = normalizedSubject is null
                ? null
                : normalizedSubject[..Math.Min(normalizedSubject.Length, 320)],
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
