using System.Security.Claims;
using Nexora.BuildingBlocks.Security;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexora.Modules.Identity.Domain;
using Nexora.Modules.Identity.Infrastructure.Persistence;
using Nexora.Modules.Identity.Security;

namespace Nexora.Modules.Identity;

public static class IdentityModule
{
    public static IServiceCollection AddNexoraIdentity(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment)
    {
        var connectionString = configuration.GetConnectionString("Nexora")
            ?? throw new InvalidOperationException("Connection string 'Nexora' is required.");

        services.Configure<IdentityProviderOptions>(configuration.GetSection("Identity:Provider"));
        services.AddScoped<Nexora.BuildingBlocks.Security.ICrmContextProvider, Nexora.Modules.Identity.Security.CrmPortalContext>();
        services.AddSingleton(new LocalIdentitySettings(isDevelopment &&
            configuration.GetValue<bool>("Identity:Provider:LocalDevelopmentEnabled")));
        services.AddScoped<ILocalIdentityAdapter, LocalIdentityAdapter>();
        services.AddHttpContextAccessor();
        services.AddSingleton<WorkflowExecutionContext>();
        services.AddScoped<IWorkflowPrincipalDirectory, WorkflowPrincipalDirectory>();
        services.AddScoped<IRequestIdentity, RequestIdentity>();
        services.AddScoped<IIdentityDirectory, IdentityDirectoryService>();
        services.AddScoped<IPortalRecipientDirectory, PortalRecipientDirectory>();
        services.AddScoped<IWorkflowPortalDirectory, WorkflowPortalDirectory>();
        services.AddScoped<IWorkflowActionHandler>(sp=>new WorkflowInspectionAction(new("identity.portal.inspect-contact","Review explicit portal binding and organisation delegation","identity.read",["contactId"],[new("contactId","Contact","reference",true)],["hasLogin","issues","invalidDelegations"],AdditionalPermissions:["crm.read"]),"contactId",async(id,ct)=>{var result=await sp.GetRequiredService<IWorkflowPortalDirectory>().ReviewAsync(id,ct);return new("Portal delegation reviewed without changing grants",new(){{"hasLogin",result.HasLogin?"true":"false"},{"issues",result.Issues},{"invalidDelegations",result.InvalidDelegations.ToString()}});}));
        services.AddScoped<IPublicTenantDirectory, PublicTenantDirectory>();
        services.AddScoped<IScheduledPublisherDirectory, ScheduledPublisherDirectory>();
        services.AddScoped<ITenantContext, HttpTenantContext>();
        services.AddDbContext<NexoraIdentityDbContext>(options => options.UseSqlServer(connectionString));

        services
            .AddIdentityCore<NexoraUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<NexoraRole>()
            .AddSignInManager()
            .AddEntityFrameworkStores<NexoraIdentityDbContext>()
            .AddDefaultTokenProviders()
            .AddClaimsPrincipalFactory<NexoraClaimsPrincipalFactory>();

        services
            .AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();

        services.ConfigureApplicationCookie(options =>
        {
            options.Events.OnValidatePrincipal = async context =>
            {
                // Resolve current tenant-scoped roles on each request: an old cookie must not
                // retain access after its user or security group has been changed.
                var previous = context.HttpContext.User;
                context.HttpContext.User = context.Principal!;
                try
                {
                    var signIn = context.HttpContext.RequestServices.GetRequiredService<SignInManager<NexoraUser>>();
                    var user = await signIn.ValidateSecurityStampAsync(context.Principal);
                    if (user == null || !user.IsActive)
                    {
                        context.RejectPrincipal();
                        return;
                    }
                    var factory = context.HttpContext.RequestServices.GetRequiredService<IUserClaimsPrincipalFactory<NexoraUser>>();
                    var refreshed = await factory.CreateAsync(user);
                    context.ShouldRenew = !context.Principal!.Claims.Select(claim => (claim.Type, claim.Value)).OrderBy(claim => claim.Type).ThenBy(claim => claim.Value)
                        .SequenceEqual(refreshed.Claims.Select(claim => (claim.Type, claim.Value)).OrderBy(claim => claim.Type).ThenBy(claim => claim.Value));
                    context.ReplacePrincipal(refreshed);
                }
                finally { context.HttpContext.User = previous; }
            };
            options.Cookie.Name = isDevelopment ? "Nexora.Session" : "__Host-Nexora.Session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = isDevelopment
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        services.AddScoped<IAuthorizationHandler, ActiveTenantAuthorizationHandler>();
        var activeTenantRequirement = new ActiveTenantRequirement();
        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(activeTenantRequirement)
                .Build())
            .AddPolicy(NexoraPermissions.IdentityRead, policy =>
                policy.RequireAuthenticatedUser()
                    .AddRequirements(activeTenantRequirement)
                    .RequireClaim(NexoraClaimTypes.Permission, NexoraPermissions.IdentityRead))
            .AddPolicy(NexoraPermissions.IdentityManage, policy =>
                policy.RequireAuthenticatedUser()
                    .AddRequirements(activeTenantRequirement)
                    .RequireClaim(NexoraClaimTypes.Permission, NexoraPermissions.IdentityManage))
            .AddPolicy(NexoraPermissions.AuditRead, policy =>
                policy.RequireAuthenticatedUser()
                    .AddRequirements(activeTenantRequirement)
                    .RequireClaim(NexoraClaimTypes.Permission, NexoraPermissions.AuditRead));

        foreach (var permission in NexoraPermissions.All.Skip(3))
        {
            services.AddAuthorizationBuilder().AddPolicy(permission, policy => policy.RequireAuthenticatedUser()
                .AddRequirements(activeTenantRequirement).RequireClaim(NexoraClaimTypes.Permission, permission));
        }

        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = isDevelopment ? "Nexora.Antiforgery" : "__Host-Nexora.Antiforgery";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = isDevelopment
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("identity-login", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 8,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true,
                    }));
        });

        services.AddPortal(isDevelopment);
        return services;
    }

    public static IEndpointRouteBuilder MapNexoraIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/api/v1/auth").WithTags("Identity");

        auth.MapGet("/providers", (LocalIdentitySettings settings) => Results.Ok(new
        {
            localDevelopmentEnabled = settings.Enabled,
            entraEnabled = false,
        }));

        auth.MapGet("/csrf", (IAntiforgery antiforgery, HttpContext context) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            return Results.Ok(new CsrfResponse(tokens.RequestToken!));
        });

        auth.MapPost("/login", LoginAsync)
            .RequireRateLimiting("identity-login");

        auth.MapGet("/session", GetSessionAsync)
            .RequireAuthorization();

        auth.MapPost("/logout", LogoutAsync)
            .RequireAuthorization();

        endpoints.MapGet("/api/v1/admin/access-overview", GetAccessOverviewAsync)
            .WithTags("Administration")
            .RequireAuthorization(NexoraPermissions.IdentityRead);

        endpoints.MapPortal();
        endpoints.MapAdministration();
        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        ILocalIdentityAdapter adapter,
        HttpContext httpContext,
        IAntiforgery antiforgery,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["credentials"] = ["Email and password are required."],
            });
        }

        try
        {
            await antiforgery.ValidateRequestAsync(httpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "The security token is invalid or expired.");
        }

        return await adapter.SignInAsync(request, httpContext, cancellationToken);
    }

    private static async Task<IResult> GetSessionAsync(
        ClaimsPrincipal principal,
        UserManager<NexoraUser> userManager,
        NexoraIdentityDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null || !user.IsActive)
        {
            return Results.Unauthorized();
        }

        var tenant = await dbContext.Tenants
            .Where(candidate => candidate.Id == user.TenantId && candidate.Status == TenantStatus.Active)
            .Select(candidate => new { candidate.Id, candidate.Name, candidate.Slug })
            .SingleOrDefaultAsync(cancellationToken);

        if (tenant is null)
        {
            return Results.Unauthorized();
        }

        var roles = await userManager.GetRolesAsync(user);
        var permissions = principal.FindAll(NexoraClaimTypes.Permission)
            .Select(claim => claim.Value)
            .Distinct()
            .Order()
            .ToArray();

        return Results.Ok(new SessionResponse(
            user.Id,
            user.DisplayName,
            user.Email!,
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            roles.Order().ToArray(),
            permissions));
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext httpContext,
        IAntiforgery antiforgery,
        SignInManager<NexoraUser> signInManager)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(httpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "The security token is invalid or expired.");
        }

        await signInManager.SignOutAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> GetAccessOverviewAsync(
        ClaimsPrincipal principal,
        UserManager<NexoraUser> userManager,
        RoleManager<NexoraRole> roleManager,
        NexoraIdentityDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var tenantId = Guid.Parse(principal.FindFirstValue(NexoraClaimTypes.TenantId)!);

        var users = await userManager.Users
            .Where(user => user.TenantId == tenantId)
            .OrderBy(user => user.DisplayName)
            .Select(user => new AccessUser(user.Id, user.DisplayName, user.Email!, user.IsActive))
            .ToListAsync(cancellationToken);

        var roles = await roleManager.Roles
            .Where(role => role.TenantId == tenantId)
            .OrderBy(role => role.Name)
            .Select(role => new AccessRole(role.Id, role.Name!, role.Description, role.IsSystem))
            .ToListAsync(cancellationToken);

        var teams = await dbContext.Teams
            .Where(team => team.TenantId == tenantId)
            .OrderBy(team => team.Name)
            .Select(team => new AccessTeam(team.Id, team.Name, team.Members.Count))
            .ToListAsync(cancellationToken);

        return Results.Ok(new AccessOverviewResponse(users, roles, teams));
    }


}

public sealed record LoginRequest(string Email, string Password, bool RememberMe);
public sealed record CsrfResponse(string Token);
public sealed record SessionResponse(
    Guid UserId,
    string DisplayName,
    string Email,
    Guid TenantId,
    string TenantName,
    string TenantSlug,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);
public sealed record AccessUser(Guid Id, string DisplayName, string Email, bool IsActive);
public sealed record AccessRole(Guid Id, string Name, string? Description, bool IsSystem);
public sealed record AccessTeam(Guid Id, string Name, int MemberCount);
public sealed record AccessOverviewResponse(
    IReadOnlyList<AccessUser> Users,
    IReadOnlyList<AccessRole> Roles,
    IReadOnlyList<AccessTeam> Teams);
