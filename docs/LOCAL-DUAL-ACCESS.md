# Local CRM and Portal access for one identity

Use an isolated local environment. Automated coverage creates a disposable
identity, never a shared NexoraDev user. The local password adapter is not evidence
of production identity-provider support. Keep passwords out of reports and logs.

The operator needs `identity.read`, `identity.manage`, `crm.read` and `crm.manage`.
`/api/v1/admin` requires `identity.read`; user/group mutations additionally require
`identity.manage`. Portal grants require `identity.manage` and `crm.manage`.
The restricted identity needs only a non-system group with `crm.read`.
Permissions combine across groups: inspect every membership. Add `crm.manage`
only when explicitly testing edits; do not assign administrator/provisioning access.

1. In Settings → Security groups (`/settings/security-groups`), create a task-named
   CRM view group. In Settings → Users (`/settings/users`), assign it to the intended
   existing active local identity. Do not create shared users. Automated fixtures
   create their own identity through the Users API in disposable databases.
2. Select an active, unarchived Contact in the same tenant. At `/settings`, under
   **Member portal access**, use **Find a Contact**, **Member Contact**, **Link an
   existing account**, **Existing login**, then **Grant portal access**. This calls
   `POST /api/v1/admin/portal-access` with `{ userId, contactId }`.
3. Inspect Users, Security groups and all Access records pages before repeating.
   Use `GET /api/v1/admin/users`, `/api/v1/admin/security-groups` and
   `/api/v1/admin/portal-access?page=N`. Reuse the intended active binding;
   investigate conflicts without revoking unrelated access.

Group creation uses `{ name, description, permissions }`; user creation uses
`{ displayName, email, password, isActive, groupIds }`. User updates use
`PATCH /api/v1/admin/users/{id}` with the current `concurrencyStamp`, full intended
membership IDs and user fields; leave password empty to retain it. Group updates
also require their current stamp. Refresh before retrying conflicts.
Staff writes use `/api/v1/auth/csrf` and the `X-CSRF-TOKEN` header.

Sign in at `/login` and `/portal` with the same identity. Development cookies are
`Nexora.Session` and `Nexora.Portal`. With both present, CRM and Portal profile
reads succeed. Portal alone must give CRM 401; staff alone must give Portal 401.
A valid-CSRF CRM write by the reader must give 403. Staff logout uses
`/api/v1/auth/logout`; Portal logout uses `/api/v1/portal/logout` and its own token
from `/api/v1/portal/csrf`. Each logout preserves the other session.

Use **Revoke member access**, **Confirm revocation** (API:
`DELETE /api/v1/admin/portal-access/{grantId}`). The existing Portal cookie must
fail its next profile request while CRM continues working. Regrant to the existing
user: the old cookie must still fail. Log out of Portal and sign in freshly.
Staff requests refresh active status, security stamp and permissions. Portal
requests validate the exact grant, user, stamp, tenant and Contact; a new grant
never reactivates a revoked grant's cookie.

For cleanup, revoke only task-created grants and restore only intended assignments
using fresh stamps. Users with Portal history cannot be deleted: disable task-owned
identities. Remove unused task-owned groups after removing assignments (deletion
requires a current stamp). Archive only task-owned CRM records with current
versions. Preserve existing records and grant history. Automated fixtures and the
website runner dispose their whole independent database.

Operator verification:

- `node scripts/Test-Website.mjs --crm` selects CRM, saved views, association
  history, structured profiles and dual-access browser coverage.
- `dotnet test backend/tests/Nexora.Api.Tests --filter FullyQualifiedName~Restricted_crm_reader`
  exercises the IdentityApiFactory regression. The SQLite fixture retains workflow
  capture; CRM setup must use authenticated APIs providing tenant/actor context.
- Association history/profile suites retain archive restrictions, ended dates,
  pagination, retry, Department and main-account coverage. Narrow-screen visual
  checks and actual suites require separately prepared runtime dependencies.

Source/test presence and agent completion are not passing QA evidence. These
checks require no production access, external messages or shared provisioning.
