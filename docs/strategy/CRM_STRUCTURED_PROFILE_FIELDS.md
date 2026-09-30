# Contact and Account field delivery — slice 1

30 September 2026. The user authorised implementation after reviewing the workbook findings. This is the first bounded field slice in master-plan section 48. Missing future modules remain deferred.

## Canonical field dictionary

All fields below are optional on existing records. Text is trimmed; blank means unset; maximum accepted length is 160 characters, with control characters rejected. The additive database columns retain the CRM default capacity of 500 characters. No lookup vocabulary is inferred from the workbook samples.

| Source sheet / cell | Exact source header | CRM-owned property | Portal scope |
| --- | --- | --- | --- |
| CONTACT C2 | Title | ContactProfile.Title | Own Contact: read and edit |
| CONTACT D2 | First_Name | ContactProfile.FirstName | Own Contact: read and edit |
| CONTACT E2 | Middle_Name | ContactProfile.MiddleName | Own Contact: read and edit |
| CONTACT F2 | Last_Name | ContactProfile.LastName | Own Contact: read and edit |
| CONTACT G2 | Previous_Last_Name | ContactProfile.PreviousLastName | Own Contact: read and edit |
| CONTACT H2 | Post_Nominal | ContactProfile.PostNominal | Own Contact: read and edit |
| ACCOUNT G2 | Account_Full_Name | AccountProfile.FullName | Current delegated Account: read only |
| ACCOUNT H2 | Trading_As | AccountProfile.TradingAs | Current delegated Account: read only |
| ACCOUNT L2 | Date_Business_Commenced | AccountProfile.DateBusinessCommenced | Current delegated Account: read only |
| ACCOUNT AF2 | Website | Existing CommunicationMethod with Kind=website | Current delegated Account: read only |

Business commencement is a nullable calendar date, stored as SQL `date`, with no timezone conversion. Default/invalid dates and future dates are rejected. Website validation retains the existing HTTP/HTTPS rule and 320-character limit. Multiple existing website records remain a list; this slice does not choose a primary website.

## Relationships and behaviour

`crm.Records` retains existing IDs, display names and concurrency versions. Each Contact or Account has at most one matching profile row, enforced by a unique `(TenantId, RecordId)` index. A composite foreign key binds the profile to the same tenant's record. CRM persistence guards reject a mismatched record kind or reparenting. No legacy display name is split or recomputed; members and staff can record name components without changing the established display name.

Staff with `crm.manage` edit the new details panel through the record list's Edit action. View/deep-link mode remains read-only; create the base record first, then open its Edit action to capture structured details. `crm.read` permits viewing. Archived records cannot be edited. Saves refresh the parent version and timestamp and append CRM activity in the same transaction. Stale saves return 409 and require reload. Field errors return 400; inaccessible records return 404; missing staff permission returns 403.

Portal uses shared CRM contracts and persistence. Personal updates have a strict nested allowlist. Omitted `contactDetails` preserves existing data for older clients; explicit null or all-empty values clears the name components. Display name and the existing correspondence address remain separately managed. Member identity determines the Contact; the request cannot supply a Contact ID.

Organisation details require an active member grant, explicit unrevoked organisation grant, current Account–Contact association and active Account. The projection exposes only display name, account details and websites. Application-submission permission is not company-edit permission, so no Portal organisation update endpoint is added. No profile fields are published to CMS/public directories by this slice.

## Changed route inventory

| Method / route | Change | Scope |
| --- | --- | --- |
| GET /api/v1/crm/records/{id} | Adds contactDetails/accountDetails | Existing staff CRM read permission |
| PUT /api/v1/crm/records/{id}/profile | New typed detail update with parent version | Staff CRM manage permission and CSRF |
| GET /api/v1/portal/profile | Adds contactDetails | Authenticated member's own Contact |
| PUT /api/v1/portal/profile | Accepts allowlisted contactDetails | Own Contact, CSRF and parent version |
| GET /api/v1/portal/organisations/{accountId}/profile | New safe company projection | Current explicit organisation grant |

## Migration and operation

`AddStructuredRecordProfiles` creates two optional tables and their tenant-aware keys. It does not backfill, import workbook rows, create users, grant access or modify membership rules. Apply through the existing API migration procedure before using the new build. Existing databases and deployed instances are not changed merely by generating the migration.

An application rollback may leave the optional tables in place. The migration's Down operation drops the profile tables and would lose captured profile values; export/backup them before any database rollback. Existing display names and other module data are outside this migration.

## Remaining sequence

Department is implemented in slice 2 below. Main Account is implemented in slice 3 below. Next: address purpose/source/history and communication preference fields. The remaining workbook fields need their own dictionary and ownership decisions before implementation. No bulk importer, lookup catalogue, company-edit grant or public-directory publication is delivered here.

## Verification

- Backend Debug and Release builds pass. The final Release build reported zero warnings; an earlier clean build emitted ASP.NET analyzer AD0001 warnings.
- Portal API coverage: 23 relevant tests pass across focused runs. An existing response-shape assertion was updated for the new allowlisted field; its cookie separation, CSRF and concurrency checks then passed.
- CRM persistence boundary test passes for tenant isolation, wrong record kind, duplicate profile and reparenting. The personal-profile test also verifies archived-record rejection.
- Both architecture tests pass.
- TypeScript and focused frontend lint pass.
- EF reports no pending CRM model changes. The complete migration chain applies to a fresh disposable SQL Express database used by the browser tests.
- Three existing Edge Portal journeys pass (organisation application/revocation, personal approval and personal rejection). The new structured-field Edge journey also passes: staff save/reload, unchanged legacy display name, member self-edit visible in CRM, delegated company fields/websites, read-only View mode and mobile-width overflow. Its final rerun used the existing CRM Edit action; the earlier attempt correctly encountered read-only View mode. Four browser journeys passed across focused runs.

The shared running local instance has not been restarted or migrated. No production deployment, workbook import or populated-database rollback is claimed.


## Slice 2 — relationship Department (30 September 2026)

Source `CONTACT!L2` (`Department`) maps to `crm.AccountContacts.Department`, alongside JobTitle. This is optional trimmed text up to 160 characters; blank/null clears it and control characters are rejected. A person may have different departments at different organisations. Importing this source value will require resolving the source Account relationship; this change does not infer Main Account or import data.

The additive `AddAccountContactDepartment` migration adds a nullable `nvarchar(160)` column. Existing relationship IDs, versions, roles, dates and history remain intact. Staff capture Department when linking a Contact from the Account page, edit it from either relationship view, and see it on current/historic rows. Existing version, tenant, archive and ended-relationship guards apply. Persistence also checks the Department length and control characters. Existing association audit events cover edits. Legacy PATCH requests that omit Department retain it; explicit null/empty clears it. Invalid JSON types are rejected.

Route changes:

- POST `/api/v1/crm/accounts/{accountId}/contacts`: optional Department capture, existing CRM manage and CSRF requirements.
- PATCH `/api/v1/crm/account-contacts/{id}`: optional Department update, preserving omission semantics and existing concurrency.
- GET `/api/v1/crm/records/{id}/account-contacts`: includes Department in the existing relationship row.
- GET `/api/v1/portal/organisations/{accountId}/association`: new projection containing only Department and JobTitle for the signed-in member's current relationship. Existing explicit organisation grant and current-link filters apply. Portal displays it read-only under Your organisation relationship; no colleague list, staff flags, history or edit permission is exposed.

The change uses the shared CRM directory contract; CMS/public publication is unchanged. Database rollback removes the new column and requires a backup/export if Department values have been captured. The shared running instance is not migrated by generating this migration.

Verification passed: four focused API tests plus the SQL Server migration/relationship test (five distinct tests; the SQL test was initially skipped without its connection setting and then explicitly run against SQL Express); TypeScript and focused lint; no pending CRM model changes; one Edge journey covering CRM Department edit and its read-only Portal projection, alongside the structured-profile round trip. The Release build succeeded with the existing ASP.NET analyzer AD0001 warnings. Disposable test databases were cleaned up; the shared running instance remains unmigrated.


## Slice 3 — Main Account selection (30 September 2026)

Source `CONTACT!K2` (`Main_Account (ID)`) maps through a resolved Account–Contact relationship to `AccountContact.IsMainAccount`. This is distinct from the existing `IsMainContact` flag, which describes the person's role at an Account. No existing relationship is automatically chosen during migration and no imported source ID is treated as an internal GUID.

A Contact has zero or one selected current Main Account. CRM staff choose or clear it from either relationship view. The chosen relationship must belong to that Contact, be current, and have unarchived Contact/Account records. A filtered unique `(TenantId, ContactId)` index applies when `IsMainAccount = 1 AND EndDate IS NULL`. Switching clears the previous selection and sets the next one inside one database transaction; Contact version checks and participating parent/relationship tokens reject stale or conflicting writes. Other relationships and their Main Contact flags remain unchanged. Activity is recorded against affected Accounts and Contact.

Ending the selected link leaves no current Main Account and retains the flag on the ended row, labelled Main account at end. Rejoining creates an unselected link. Archiving an Account does not silently choose a replacement: its relationship is visibly archived, cannot be newly selected, and the stored selection may be cleared or replaced. Portal access continues to require its existing grant/current-link checks; Main Account never grants access, creates membership or publishes data to CMS.

Route: PUT `/api/v1/crm/contacts/{contactId}/main-account`, body `{ version, associationId }`, where version is the Contact's current version and null associationId clears selection. Existing CRM manage and CSRF requirements apply; unknown/foreign/ended relationship IDs return 404, stale versions 409, archived selection 400. The relationship list now includes contactVersion for this command. Portal's existing own-relationship projection adds `isMainAccount`, displayed read-only.

`AddMainAccountSelection` adds a default-false bit and filtered unique index. No backfill/import runs. Down removes only the selection flag/index, losing selections if used; back up before rollback. The shared running database remains unmigrated.

Verification: all 30 focused API/SQL tests passed with no skips, both architecture tests passed, and all three Edge journeys passed (Account history, Contact history and CRM-to-Portal profile/Department/Main Account sharing). TypeScript, focused lint and the no-pending-model-changes check passed. SQL Server replay verifies that two different current Accounts cannot both be Main Account for one Contact, while an ended selection can coexist with a new one. This commit scope includes the preceding association-history, profile and Department deliveries and their requirement documents. The separate scheduled progress-report edit is excluded.
