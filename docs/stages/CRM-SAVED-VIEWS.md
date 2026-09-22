# CRM personal saved views

The Contacts and Accounts listings use the existing `/api/v1/crm/views` API and `crm.SavedViews` table. Views remain private to the authenticated user in the active tenant. Existing view IDs, kinds and comma-separated column definitions are preserved.

The Columns panel supports Name (required), Status, Category, Updated and typed custom fields. Custom columns use `field:<GUID>`. A view supports up to 54 distinct columns (four built-ins plus the existing 50-field tenant limit), bounded to 4,000 characters. Deleted or otherwise unavailable columns are removed when loading the listing; saving the view persists that correction.

Users can save a new view, apply it, save changes or rename it, and delete it with confirmation. Saving changes or renaming uses the current listing state. Views include search, status, sort, archived state, owner user, owner team, category and columns. Filters run in SQL before counting and paging. Category is an exact match, using the database's collation.

## URL behavior

A `viewId` supplies the default listing state. Explicit URL parameters override it, including empty filter values. Applying a view or changing a filter starts on page 1; loading a link preserves its page. Columns and current filters are written to the URL so reload and browser back/forward restore unsaved changes as well. Existing `record` links remain supported. A missing, inaccessible or wrong-kind view falls back to URL/default state and displays a message. Sharing a private view URL does not grant another user access to that view.

## API and migration

- `GET /api/v1/crm/views?kind=contact|account`: list the caller's views; kind is optional.
- `GET /api/v1/crm/views/{id}`: read one private view.
- `POST /api/v1/crm/views`: create, preserving the existing 200 response contract.
- `PUT /api/v1/crm/views/{id}`: update the full existing ViewRequest, including name; kind cannot change.
- `DELETE /api/v1/crm/views/{id}`: delete, returning 204.

The existing `crm.read` policy, CSRF checks and EF tenant/user query and write guards apply to all view routes. Mutations do not accept ownership from the client. `OwnerUserId` and `OwnerTeamId` on a saved view are record filters; the existing `UserId` remains the view's owner.

The `CrmSavedViewFilters` EF migration adds three nullable filter columns and widens Columns from 500 to 4,000 characters. Deploy this migration before running the updated API. No uniqueness constraint or new table is introduced. Rolling back drops the new filter values and narrows Columns; do not roll back while long column lists exist without an explicit data migration.

From the repository root, using the intended environment's `NEXORA_CONNECTION_STRING`:

```powershell
dotnet tool restore
dotnet ef database update --project backend/src/Modules/Nexora.Modules.Crm --context CrmDbContext
dotnet test backend/tests/Nexora.Api.Tests --filter FullyQualifiedName~CrmEndpointTests
node scripts/Test-Automations.mjs crm-saved-views.spec.ts
```

The browser runner creates an isolated SQL Server LocalDB database, seeds a generated development credential, starts the API and web app, runs Playwright, then removes its own processes and database. It requires LocalDB and the configured Edge browser. API tests use the existing SQLite authentication fixture. Test outcomes must be reported from actual execution, not inferred from this document.
