# Stage 2, first slice: Identity and tenancy

## Outcome

Nexora now has a production-shaped identity boundary and a tenant-aware administration surface. The browser authenticates through the same-origin Next.js boundary and receives an HTTP-only session cookie; credentials and session tokens are never stored in browser JavaScript storage.

## Delivered

- ASP.NET Core Identity users and roles backed by Entity Framework Core and SQL Server
- Tenants, teams, team membership, role permissions, and immutable-style audit events
- Initial identity database migration and a local-development seed path
- CSRF-protected login and logout endpoints
- Login rate limiting plus account lockout controls
- Secure cookie settings with stricter production defaults
- Authenticated session endpoint with tenant, role, and permission claims
- Permission-protected, tenant-scoped administration overview
- Next.js proxy check for protected routes, backed by authoritative API checks
- Live login, session display, logout, and access administration UI

## Security boundary

The Next.js proxy performs only an optimistic cookie-presence check for fast navigation. It is not the authorization boundary. The ASP.NET Core endpoints validate the authenticated principal, active user, active tenant, and required permission before returning protected data.

The login and logout flows require an antiforgery token. Failed and successful sign-in attempts create audit events without recording passwords. Error responses deliberately avoid revealing whether an email address exists.

## Tenant isolation

Every user, role, and team is assigned to one tenant. `NexoraIdentityDbContext` applies global query filters to tenants, users, roles, teams, audits, permissions, memberships, and Identity join tables. Tenant identity comes only from the authenticated principal; missing tenant context returns no tenant data and rejects writes.

Both synchronous and asynchronous SaveChanges validate current and persisted ownership, including detached updates/deletes and foreign-key references. Changing TenantId cannot move a record between tenants. An internal, disposable system scope is limited to credential verification and development seeding; it clears tracked entities on exit. Application code must use scoped queries and SaveChanges: raw SQL, IgnoreQueryFilters, and bulk ExecuteUpdate/ExecuteDelete bypass these application safeguards and must not be used for tenant writes. This slice implements application-level isolation, not SQL Server row-level security.

SQLite integration coverage exercises real relational queries, two tenants, unfiltered reads, Find, anonymous access, foreign inserts/updates/deletes, forged ownership, team membership, user-role and claim injection, and role permissions. It also proves same-tenant insert/update/delete succeeds. Existing SQL Server migrations remain applicable; the query filters and save guards require no schema change.

## Local development

Apply migrations and seed the first administrator by setting a password only in the current shell:

```powershell
$env:Identity__SeedAdminPassword = '<choose-a-strong-local-password>'
dotnet run --project backend/src/Nexora.Api
```

Local sign-in requires both the Development environment and `Identity:Provider:LocalDevelopmentEnabled=true` (set in appsettings.Development.json). It always remains disabled in Production, even if that setting is true. `ILocalIdentityAdapter` verifies seeded ASP.NET Core Identity credentials, including lockout and active-tenant checks, before issuing the protected session cookie. There is no preview-cookie bypass.

`GET /api/v1/auth/providers` advertises local availability. The login UI disables unavailable sign-in and labels Microsoft SSO as not connected. `Identity:Provider:Entra:TenantId`, `ClientId`, and `CallbackPath` are reserved typed configuration for the next OIDC adapter; setting them does not activate Entra yet. No Entra credentials are required or committed. Production sign-in is intentionally unavailable until that adapter is implemented.

The seed is idempotent. It creates the `Nexora Demo` tenant, `admin@nexora.local`, the platform administrator role, all Stage 2 permissions, and a starter Customer Success team. No password is committed to source control.

## Verification gate

- `dotnet build backend/Nexora.slnx --configuration Release`
- `dotnet test backend/Nexora.slnx --configuration Release`
- `npm run lint:web`
- `npm run build:web`
- Browser sign-in, administration, protected-route, and sign-out checks

## Remaining identity work

Entra authorization-code/PKCE integration and tenant provisioning remain outside this first slice. Users currently belong to one tenant and use globally unique login emails; existing Identity role-name uniqueness also remains unchanged. Role changes use the existing Identity security-stamp refresh lifecycle.

## Stage 3 entry conditions

The CRM foundation must use the tenant identifier from the authenticated API principal, enforce permissions at the data boundary, and link ownership to the users and teams delivered here.
