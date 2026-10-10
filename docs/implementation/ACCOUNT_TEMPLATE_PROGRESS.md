# ACCOUNT migration sheet progress

4 October 2026. ACCOUNT contains 160 field occurrences. This review uses the [exact source inventory](../strategy/DATA_MIGRATION_FIELD_INVENTORY.md) and current Nexora models. Application representation is separate from source-ID resolution, historical import, provider connection and successful migration. No workbook data was imported.

## General Information — A:X

The new `AccountGeneralInformation` contract adds 12 nullable Account-owned fields: SourceAccountId, TradingStatus, AccountType, IndustrySector, Venue, MarketingSource, TurnoverRange, NumberOfEmployees, EmployeesRange, Region, Branch and OldReference. Together with existing display/full/trading names, business commencement date, staff owner and main-Contact association/details, 20/24 general field occurrences now have application representation. The four missing fields are Parent_Account, Parent_Relationship, Child_Relationship and Effective_Date.

| Source | Owning representation |
| --- | --- |
| A–D Parent and reciprocal relationship/effective date | Still missing a dated Account hierarchy. Generic labelled relationships are not equivalent; parent/child kinds, cycle prevention and effective/ceased history need implementation. |
| E Account_ID | New SourceAccountId retains the previous-system reference; Nexora's GUID stays separate. No automatic source-ID deduplication or relationship resolution. |
| F–H Account names | Existing CRM display name, Account FullName and TradingAs remain separate. |
| I Trading_Status | New explicit text, separate from active/inactive CRM status. No automatic business-status translation. |
| J Account_Type; K Industry_Sector | New recorded values, distinct from generic CRM Category. |
| L Date_Business_Commenced | Existing nullable Account commencement date. |
| M Account_Owner | Existing staff user/team ownership. Source owner names require identity resolution during import; this is not entitlement ownership or the main Contact. |
| N Venue | New recorded source value. The source header alone does not establish a boolean, event-venue ID or booking capability; no venue entity is created. |
| O–Q Main Contact, Department and Job Title | Existing AccountContact association with main-Contact designation, Department and JobTitle. Do not duplicate these in AccountProfile. Source Contact IDs require resolution. |
| R Marketing_Source | New explicit source value. No campaign attribution is inferred. |
| S Turnover_Range | New recorded range; no turnover amount or currency inferred. |
| T Number_Of_Employees | New nullable nonnegative integer. Zero remains distinct from unknown. Fractional/out-of-range values are rejected. |
| U Employees_Range | New recorded range. It neither calculates nor overrides NumberOfEmployees. |
| V–X Region, Branch, Old_Reference | New recorded metadata; Region stays distinct from address County/Region and source Account ID. |

`GET /api/v1/crm/records/{id}` now returns accountGeneralInformation for Accounts only. `PUT /records/{id}/profile` accepts it alongside AccountDetails, requires CRM manage permission/current record version, rejects cross-kind profile blocks and refuses archived records. The parent version and `profile.updated` receipt cover both sets of fields. Text is trimmed, limited to 160 characters and rejects control characters; whitespace becomes null. Employee count must be a nullable nonnegative 32-bit integer.

Omitting accountGeneralInformation preserves it for older clients updating names. Providing an empty object explicitly clears the new fields without clearing AccountDetails. The staff Account profile form displays/saves the new fields; read-only staff see disabled inputs. Portal's delegated organisation profile still exposes only the existing organisation name/details/websites, excluding source references, turnover, employee count and other new staff metadata. The existing AccountDetails contract was not widened.

Migration `20261004142510_AccountGeneralInformation` adds 12 nullable AccountProfiles columns, with 160-character limits for text. Existing names, commencement date and CRM versions/status remain unchanged. It has not been applied to the shared development database. Changes are uncommitted.

## Other ACCOUNT sections

| Section / source cells | Fields | Current representation | Remaining gap |
| --- | ---: | ---: | --- |
| General Information A:X | 24 | 20 | Four dated parent/child relationship fields. |
| Emails Y:AE | 7 | 6 | Primary and five other emails use existing communication rows; Finance_Email_Address needs an explicit finance purpose. Generic secondary priority is not finance purpose. |
| Website, Mobile, Telephone and Fax AF:AM | 8 | 8 | Existing channel rows and priorities represent the values. Source ordering still needs import mapping. |
| Addresses AN:BH | 21 | 19 | Address purposes, job titles, four lines, county and live shared mailing/billing are available. Mailing/Billing Address Contact Name need explicit addressee support; AccountName is a different field. |
| Social Media BI:BS | 11 | 0 | Current identifiers/counters are Contact-owned and reject Account records. Add Account-owned social profiles/provenance. |
| Communication Permissions BT:CA | 8 | 0 | Current eight record-level permissions are Contact-owned. Account-owned consent/suppression and marketing enforcement are missing; per-method consent remains separate. |
| Directory CB:CY | 24 | 0 | Deferred publication/address-selection and public-field rules. Do not publish generic Account profiles automatically. |
| Profile CZ:DD | 5 | 5 | Existing five-slot hierarchical profile assignments support Accounts. |
| Membership General DE:EA | 23 | 23 | Membership-owned projection, including recorded primary label. Historical import/status/date reconciliation remains separate. |
| Membership Finance EB:EP | 15 | 9 | Recorded arrangement/payer/invoicing supported; payer-based rating and five term-linked settlement fields remain. |
| Membership Direct Debit EQ:FD | 14 | 0 | GoCardless/Stripe preference supported outside the source count. Masked provider-backed bank/mandate, collections, instalments and mandate-file inclusion remain missing. |

Application representation is therefore 90/160 source-field occurrences (about 56%). This count includes recorded metadata and shared owning-module projections; it does not imply complete historical imports, public directory publication or live payment integrations. The remaining 70 are the four parent fields, finance email, two addressee fields, 11 social fields, eight permissions, 24 directory fields, six finance fields and 14 Direct Debit fields.

Source quality remains important: the inventory review found repeated ACCOUNT source IDs with conflicting names/values. Reconcile those conflicts before imports; do not use last-row-wins or merge different Accounts automatically. Embedded membership/payment copies also need source-precedence checks against CONTACT and the standalone Membership sheet.

Next: dated Account hierarchy, then finance-email purpose and address addressee names, followed by Account social details and communication permissions. Directory and provider-backed settlement/Direct Debit retain their separate dependencies. Membership Application work remains saved in [membership sheet progress](MEMBERSHIP_SHEETS_PROGRESS.md).

## Verification

Five API checks and one isolated SQL Server upgrade test passed, with no selected checks skipped. Coverage includes all new general-field saves, trimmed source values, zero versus unknown employee count, negative/fractional/overflow validation, stale versions, display/status preservation, omission by older clients, explicit clearing, cross-kind rejection, unauthorised saves, archived-record protection, delegated Portal exclusion and existing profile-boundary regressions. SQL verifies upgrade/replay, existing names and record versions, nullable defaults, stored Account metadata and foreign-tenant filtering.

The invalid-number checks exposed an existing request-parsing error: malformed JSON binding threw BadHttpRequestException, which the generic API exception handler converted to HTTP 500. The handler now retains that exception's HTTP status while leaving unexpected exceptions as 500; fractional/oversized employee counts return 400 without writes. The API regression selection passed after this fix.

TypeScript, focused ESLint, production frontend build and focused whitespace checks passed. Seven existing generated CSS warnings remain. Generated Next configuration changes from the isolated build were restored. Authenticated browser appearance remains unverified; these checks do not establish deployment readiness or a migrated shared database.
