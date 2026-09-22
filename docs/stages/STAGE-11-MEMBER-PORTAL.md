# Member portal — local access and own-profile foundation

The product owner selected local development first; the production identity provider remains undecided. This phase supplies a separate portal experience at `/portal`, a dedicated HttpOnly cookie, an explicit revocable user/tenant/Contact binding and a restricted own-profile API. It does not implement verified external identity, invitations, self-registration, email recovery or membership self-service.

## Access boundaries

- `/api/v1/portal/login` checks existing Identity credentials with lockout enabled and requires an explicit active PortalAccess grant. It issues only portal claims, never staff roles or permissions. The staff cookie is neither accepted nor overwritten by portal authentication.
- Every profile request checks the live binding, active user and tenant, security stamp and active non-archived Contact. Revocation takes effect on the next request. A new grant does not reactivate an old cookie.
- Only `name` and `version` are returned by the profile API. PUT accepts exactly those fields, derives the Contact from the binding, updates the shared CRM record with optimistic concurrency and writes a CRM activity in the same save. Email, addresses, roles, internal notes, attributes and membership decisions are not exposed.
- Portal mutations require antiforgery tokens from `/api/v1/portal/csrf`, whose identity is explicitly resolved from the portal scheme even when staff cookies coexist. Login shares the existing IP rate limiter. Portal reads set no-store.
- Staff provisioning and revocation require both `identity.manage` and `crm.manage`, validate the same tenant and record audit events. Active user and Contact bindings are unique. Revoked bindings are retained.
- The local adapter and local member provisioning are available only when both Development and `Identity:Provider:LocalDevelopmentEnabled` are enabled. Production access remains closed until a verified provider adapter and recovery lifecycle are implemented.

## Local operation

The published website preview remains on port 3016. The portal preview uses 3018 with API 5098 and the same LocalDB `NexoraDev` catalog. A dedicated role-free account `member.demo@nexora.local` and its own Contact have been created. Its generated local password is in the ignored file `artifacts/portal-local/DEMO-ACCESS.md`.

```powershell
dotnet build backend/src/Nexora.Api --configuration Portal
dotnet ef database update --project backend/src/Modules/Nexora.Modules.Identity --context NexoraIdentityDbContext --configuration Portal --no-build
./scripts/Start-WebsiteLocal.ps1 -Portal
```

In VS Code select **Portal (API + Web)** and press F5. It uses the existing isolated WebsiteDebug API build and source-based Next.js debugger on 5097/3017, opening `/portal`. Stop the Website debug compound before using the Portal compound because they share debug ports. Preview processes on 3016/3018 are separate copies; edit/debug the actual source through VS Code for hot reload.

Staff API operations:

- POST `/api/v1/admin/portal-access/local-member`: `{email,password,contactId}` creates a local role-free user and grant transactionally; no credentials are returned.
- POST `/api/v1/admin/portal-access`: `{userId,contactId}` grants an existing active user access to an active Contact in the same tenant.
- DELETE `/api/v1/admin/portal-access/{id}` revokes a grant.

There is no public registration endpoint or staff access-management UI in this slice. Login email is not used to infer a Contact relationship. Never make the local credential adapter a production registration system.

## Verification — 19 September 2026

- 9 portal API tests passed: cookie separation, restricted projection, CSRF, field allowlist, stale-version conflict, shared CRM persistence, logout, revocation/regrant, tenant ownership, uniqueness, inactive user/tenant, security stamp change, disabled local adapter, local provisioning rollback and role-free user creation.
- Full API suite: 89 passed, 4 opt-in SQL Server tests skipped. Platform migration replay ran separately on LocalDB and passed, including pending model checks. Architecture suite: 2 passed.
- Isolated browser journey passed: provision member, sign in, edit own profile, reload, verify staff CRM sees the change, reject staff API access and revoke the active session. Desktop and 390px mobile screenshots were visually reviewed.
- TypeScript and targeted portal ESLint passed. Production frontend build was not rerun; the previously documented Google Fonts connectivity limitation remains.

Run the browser check with `node scripts/Test-Website.mjs --portal`. It uses a disposable database and generated credentials; it leaves existing preview/debug processes untouched.

## Next

Personal draft/save/review/submit is now implemented; see [portal applications](STAGE-12-PORTAL-APPLICATIONS.md) for current routes, safeguards and verification. Earlier notes below describe the preceding overview milestone.

### Personal membership overview — 19 September 2026

The portal now reads `/api/v1/portal/memberships` through `IMemberMembershipDirectory`, using the same live access checks as the profile. It shows personal membership product, status and dates, plus individual application product, status and submission date. Queries retain tenant filters, derive the Contact from the verified binding and exclude organisation records. Internal decision reasons, applicant notes, pricing and staff review history are not projected. The UI distinguishes loading, empty and retryable failure states.

Ten portal API tests and the updated desktop/mobile browser journey passed. The added test seeds another Contact, another tenant and an organisation record and verifies none appear in the own-record response. The browser creates and approves a personal application through staff APIs and verifies both its status and the resulting membership appear. TypeScript and targeted lint passed. A clearly named demo membership was also added to the local demonstration member. Application creation by members remains the next increment; the current overview does not submit applications.

Add own-membership summaries and application draft/submit/status through shared Membership commands with restricted DTOs. Extend the profile only after defining the member-editable fields and contact-verification requirements. Production provider, invitations and recovery remain explicit prerequisites to a public launch.
