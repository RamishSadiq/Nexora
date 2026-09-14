# Stage 3: CRM foundation

## Delivered

The Relationships workspace now manages Contacts and Accounts backed by SQL Server. Both use a typed CRM record with a fixed kind, lifecycle status, category, owner, team, archive state, timestamps and optimistic concurrency token. Child rows hold addresses, communication methods and consent evidence, directed record relationships, notes, tags, file attachments and typed custom-field values.

The UI supports create, edit, detail, archive and restore; name search, status/state filters, stable sorting and pagination; personal saved views including column choices; and related information with an activity timeline. List filters and the selected record are represented in the URL. Forms retain input when validation fails and explain stale-record conflicts. Related sections are bounded to 100 items each; timeline pages contain 50 events. Lists return 20 rows by default and at most 100.

Custom-field administrators can create up to 50 tenant definitions. Fields apply to either Contacts or Accounts and have an immutable text, decimal, boolean or date type. Values use separate typed columns, not an untyped JSON/EAV payload. Empty values clear the field; numbers support 14 integer digits and 4 decimal places.

## Module and security boundaries

`Nexora.Modules.Crm` references only BuildingBlocks. `IRequestIdentity` and `IIdentityDirectory` are published contracts implemented by Identity; CRM never queries Identity domain models. Owners and teams must resolve within the authenticated tenant.

Every CRM row has TenantId. Global filters cover all tables, with an additional UserId filter for personal views. Synchronous and asynchronous SaveChanges guards validate current and persisted tenant ownership, protecting detached writes; audit rows cannot be changed or deleted. Composite foreign keys include TenantId for parent records, related targets and custom-field definitions, so SQL Server also rejects cross-tenant child references. Application writes must use SaveChanges rather than raw SQL or unguarded bulk updates.

All CRM endpoints require `crm.read`. Shared mutations also require `crm.manage`, field definitions require `crm.configure`, and file upload/download requires `crm.files` (upload also requires manage). All writes, including personal views, validate the session-bound CSRF token. Tenant and actor IDs come from the signed session, never request bodies. API responses disable caching. Missing or foreign records return 404.

Record changes and related-data writes append tenant-scoped activity with actor, UTC time and request correlation ID in the same SaveChanges transaction. Parent version changes serialize child mutations against archive/edit operations. PATCH and archive/restore require the last observed version and return 409 on conflicts.

Files are limited to 5 MB and PDF/text/CSV/PNG/JPEG names, stored in SQL Server for this foundation. They download as `application/octet-stream` attachments with `nosniff`; contents are never rendered inline. Object storage, malware scanning, retention and aggregate storage quotas remain production-hardening work.

## API

- `GET/POST /api/v1/crm/records`, with `kind=contact|account` for lists
- `GET/PATCH /api/v1/crm/records/{id}`
- `POST /api/v1/crm/records/{id}/archive|restore`
- `GET /api/v1/crm/records/{id}/timeline?page=1`
- `POST /api/v1/crm/records/{id}/addresses|communications|relationships|notes|tags|files`
- `DELETE /api/v1/crm/records/{id}/addresses|communications|relationships|tags/{childId}`
- `GET /api/v1/crm/records/{id}/files/{fileId}`
- `GET/POST /api/v1/crm/fields`; `PUT /api/v1/crm/records/{id}/fields/{fieldId}`
- `GET/POST /api/v1/crm/views`; `DELETE /api/v1/crm/views/{id}`
- `GET /api/v1/crm/directory`

The list accepts `search`, `status`, `sort=name|-name|updated`, `archived`, `page`, and `pageSize`. PATCH accepts the full editable core fields plus version; it is not JSON Patch. A relationship is directed: add the reverse link explicitly when needed.

## Local setup and migration

Use the existing development seed password flow in README. Restarting the API with that password adds the CRM permissions to the local administrator and applies the CRM migration. Sign out and in again to refresh the permission claims on existing sessions. Microsoft Entra remains optional and unconnected as documented in Stage 2.

CRM owns the `crm` schema and a separate `crm.__EFMigrationsHistory` table. For controlled deployment, set `NEXORA_CONNECTION_STRING` and run:

```powershell
dotnet tool restore
dotnet ef database update --project backend/src/Modules/Nexora.Modules.Crm --startup-project backend/src/Nexora.Api --context CrmDbContext
```

The initial migration only creates CRM tables and constraints. It does not modify legacy Sales API tables or existing Identity data. Production startup does not auto-migrate.

## Verification

```powershell
dotnet test backend/Nexora.slnx --configuration Release
$env:NEXORA_TEST_SQLSERVER = 'Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True'
dotnet test backend/Nexora.slnx --configuration Release
npm run lint:web
npm run build:web
```

API tests run against SQLite with real foreign keys. An explicitly configured SQL Server test creates a unique temporary catalog, applies the migration twice, inserts 10,000 records, checks isolation and foreign keys, and removes only its generated catalog. It is reported skipped when NEXORA_TEST_SQLSERVER is unset.

Observed LocalDB performance: 20 filtered page queries over 10,000 rows, p95 11.2 ms and maximum 269.9 ms. This is a local persistence check, not a production end-to-end load guarantee.

Browser verification on a separate NexoraStage3Review database covered sign-in, Contact creation, editing via keyboard, note saving, saved-view persistence, archive/restore with keyboard confirmation, and narrow/desktop layouts. Confirmation dialogs use native HTML dialog elements with accessible labels, focus containment and Escape cancellation. Controls have accessible labels, list/table semantics and visible focus styles; newly opened details receive focus. Full assistive-technology testing and representative production-load acceptance remain release gates.

## Subsequent work

The core lifecycle is ready for Membership to reference stable CRM IDs through published contracts. Bulk operations, merge/duplicate review, exports, required/custom-field visibility policies, object storage/scanning and full production usability/load acceptance remain subsequent work. Entra and recovery flows remain Stage 2 follow-ups. No production data was migrated or deployed.
