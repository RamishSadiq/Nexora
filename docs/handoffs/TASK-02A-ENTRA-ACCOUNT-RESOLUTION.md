# Task 2A — Resolve a Microsoft identity to a provisioned Nexora account

## Role and delivery contract

You are the coding contributor. The primary AI owns product decisions, integration
and acceptance. Implement only this task in the supplied Nexora source snapshot.
This is the first small part of workstream 2 (production staff authentication).
It does not complete Microsoft sign-in; OIDC and browser integration follow later.

The supplied ZIP is the current working tree, including uncommitted changes.
Extract it and treat its files as the authoritative baseline. Do not reconstruct
the application from memory, the product description or the old task prompt.
Do not use the old pagination snapshot. Pagination has already been integrated.

Before editing, record the SHA-256 hashes of every existing file you will modify.
If Git is available, create a local baseline commit from the extracted snapshot.
Never produce a patch against a guessed base version.

## First response: demonstrate that you read the source

Before coding, return a short source map containing:

1. The actual signature of AddNexoraIdentity and current authentication scheme.
2. The actual NexoraUser tenant/active properties and Tenant active-status value.
3. How IdentityUserLogin<Guid> is mapped and filtered in the current context.
4. The exact visibility and disposal behaviour of BeginSystemAccess.
5. Existing test setup and how this task can use that setup without credentials.
6. Your planned changed-file list, separating existing and proposed new files.

Include file paths and brief exact excerpts supporting these observations.
Stop after this source map. The user will send it to the primary AI for a quick
compatibility review, then send you CONTINUE. This checkpoint is a deliberate
workflow requirement to avoid another implementation against invented source.
If you cannot extract/read the ZIP, say so; do not generate code.

## Read these files

- AGENTS.md and applicable nested instructions.
- README.md.
- docs/architecture/ADR-0003-authentication.md.
- docs/stages/STAGE-02-IDENTITY-TENANCY.md.
- backend/src/Modules/Nexora.Modules.Identity/IdentityModule.cs.
- backend/src/Modules/Nexora.Modules.Identity/Domain/NexoraUser.cs.
- backend/src/Modules/Nexora.Modules.Identity/Domain/Tenant.cs.
- backend/src/Modules/Nexora.Modules.Identity/Infrastructure/Persistence/NexoraIdentityDbContext.cs.
- backend/src/Modules/Nexora.Modules.Identity/Infrastructure/Persistence/TenantWriteBoundary.cs.
- backend/src/Modules/Nexora.Modules.Identity/Security/LocalIdentityAdapter.cs.
- backend/src/Modules/Nexora.Modules.Identity/Security/ActiveTenantAuthorization.cs.
- backend/src/Modules/Nexora.Modules.Identity/Security/NexoraClaimsPrincipalFactory.cs.
- backend/src/Modules/Nexora.Modules.Identity/AssemblyInfo.cs.
- backend/tests/Nexora.Api.Tests/IdentityApiFactory.cs.
- backend/tests/Nexora.Api.Tests/IdentityEndpointTests.cs.
- backend/tests/Nexora.Api.Tests/TenantPersistenceTests.cs.
- backend/tests/Nexora.Api.Tests/WorkflowTestClient.cs.

## Product decision

A future Entra OIDC adapter will validate a Microsoft identity and then resolve
its directory tenant ID (tid) and object ID (oid) to an existing Nexora user.
Nexora organisation IDs and Microsoft directory IDs are DIFFERENT identifiers.

Matching email addresses must never automatically link accounts. Microsoft claims
must never supply Nexora permissions or a Nexora organisation ID.
Staff accounts and their external-login links must already be provisioned by a
trusted process. Provisioning that process is outside this task.

## Implement after CONTINUE

Add a small internal, scoped account-resolution service in the Identity module.
Suggested file: Security/EntraAccountResolver.cs. This is a proposed NEW file,
not an assertion that it already exists.

Suggested contract (adapt internal names only if source conventions require it):

    Task<EntraAccountResolution?> ResolveAsync(
        Guid directoryTenantId, Guid objectId, CancellationToken cancellationToken);

Return a small immutable result containing the local Nexora UserId and TenantId,
or null for an unlinked/inactive identity. Do not return tracked entities or tokens.

Use the existing ASP.NET Core Identity external-login table:
- LoginProvider: "MicrosoftEntra".
- ProviderKey: lowercase GUIDs formatted as "{tid:D}:{oid:D}".
- UserId: the already provisioned Nexora user.

Use one documented helper for this key format so later provisioning code and the
OIDC adapter can share the convention. Empty GUIDs must not resolve an account.
No new mapping table, schema or migration is needed for this task.

Resolution must:
- Match the complete provider name and composite external key exactly.
- Resolve only an existing linked Nexora user.
- Require user.IsActive and the user's local tenant to be active.
- Fail closed for an absent link, absent user or inactive/absent tenant.
- Never create a user, link, tenant, role, permission, cookie or session.
- Never fall back to email, display name, or oid alone.
- Avoid revealing whether an email address or other user exists.
- Propagate cancellation and unexpected database failures rather than reporting
  them as a successful resolution.

An authentication lookup occurs before a Nexora tenant is known. Use the existing
internal BeginSystemAccess scope only around this lookup, following its disposal
and tracking rules. Do not remove query filters, add a general bypass service or
alter tenant write validation. Register the resolver as scoped in AddNexoraIdentity.

This resolver is NOT token validation. It may only be called by a future adapter
after issuer, audience, signature, expiry, state and nonce checks succeed. Document
that trust boundary. Do not expose this service as an HTTP endpoint.

## Expected changed files

- NEW: Identity/Security/EntraAccountResolver.cs (under the module path above).
- EXISTING: IdentityModule.cs, only the necessary service registration.
- NEW: backend/tests/Nexora.Api.Tests/EntraAccountResolverTests.cs.
- NEW: docs/stages/STAGE-02A-ENTRA-ACCOUNT-RESOLUTION.md.

If another change is essential, explain it in the source-map checkpoint first.
Do not modify membership, CRM, the frontend, other authentication behaviours,
test factories, package versions, database migrations or sibling projects.

## Tests

Use the current test factory, relational database and existing Identity APIs.
Seed links through existing supported Identity operations in isolated test setup.
Do not invent CreateAuthenticatedClient, TestTenantId or fixture methods.

Cover:
1. A linked active Microsoft identity resolves the exact local user and tenant.
2. Unknown links and empty GUID inputs do not resolve.
3. The same oid under a different Microsoft tid does not resolve the first link.
4. A link for a different provider does not resolve as MicrosoftEntra.
5. An inactive Nexora user does not resolve.
6. An inactive Nexora tenant does not resolve.
7. Resolving two separately linked identities does not mix their local tenants.
8. Lookup does not create or change users, links, roles or permissions.
9. After lookup, the context's original tenant filtering/system-access state is
   restored; normal operations cannot read a different tenant's data.

Test the real resolver and persistence. Do not substitute a fake resolver.

## Verification and return format

Run, if the provided environment supports them:

    dotnet test backend/tests/Nexora.Api.Tests/Nexora.Api.Tests.csproj --configuration Release --filter FullyQualifiedName~EntraAccountResolverTests
    dotnet test backend/tests/Nexora.Api.Tests/Nexora.Api.Tests.csproj --configuration Release --filter "FullyQualifiedName~IdentityEndpointTests|FullyQualifiedName~TenantPersistenceTests|FullyQualifiedName~SessionBoundaryTests"

Report actual commands and outcomes. If a required runtime or package restore is
unavailable, report NOT RUN with the actual reason. Never fabricate test output.
No frontend build or full browser suite is needed for this backend-only change.

Return a ZIP of COMPLETE added/modified files with repository-relative paths.
No ellipses, prose inside source files, pseudocode, manual diff hunks or whole-repo
replacement. Include:
- changed-files list;
- SHA-256 hashes of existing files BEFORE your changes;
- source-map/checkpoint answer;
- concise implementation summary and test report;
- any deviations or unresolved issues.

An optional patch must be generated by Git against your extracted baseline.
Do not hand-author diff headers or hunk counts. Do not deploy, push, change real
data, include secrets, or claim this slice makes production authentication ready.
