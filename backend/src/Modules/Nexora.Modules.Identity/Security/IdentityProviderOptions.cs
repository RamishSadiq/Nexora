namespace Nexora.Modules.Identity.Security;

// Configuration for the forthcoming OIDC adapter. Local development needs no Entra credentials.
public sealed class IdentityProviderOptions
{
    public bool LocalDevelopmentEnabled { get; set; }
    public EntraOptions Entra { get; set; } = new();
}

public sealed class EntraOptions
{
    public string? TenantId { get; set; }
    public string? ClientId { get; set; }
    public string CallbackPath { get; set; } = "/signin-oidc";
}
