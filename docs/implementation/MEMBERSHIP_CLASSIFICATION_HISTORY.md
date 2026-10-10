# Membership category and subscription-band history

4 October 2026. Implements CONTACT EL2 Member_Category, EM2 Category_Date, EN2 Subscription_Band and EO2 Subscription_Band_Date. Account memberships use the same Membership-owned model.

## Definitions and assignments

The existing `member-category` and `member-subscription-band` setup lists supply the selectable definitions. These are separate from CRM Contact Category, Profile taxonomy and the membership product's Category. Their existing Setup screens provide definition management.

`MembershipClassificationChange` stores an immutable assignment or clearing for one membership and one classification kind. Each row contains the definition ID (nullable for clearing), its name at assignment time, effective calendar date, sequence, actor, recording timestamp and reason. Renaming a lookup does not rewrite current or historical assignment snapshots. Assign the renamed definition again only after an actual change to another definition/clearing; a no-op assignment is rejected.

Current values come from the latest effective date on or before the current UTC date, then the greatest sequence. A missing history is unknown; a clearing retains its effective date while the current definition/name become unset. Category and band have independent timelines. The first change may supply a historical date; subsequent changes cannot precede the latest effective date for that kind. Same-day corrections append a greater sequence and supersede the earlier value on that day, without rewriting its receipt. Future dates and the minimum/default date are rejected. This slice does not support future scheduling or inserting changes into an earlier interval.

Reasons are required, trimmed, limited to 500 characters and reject control characters. The selected definition must exist within the tenant and belong to the correct lookup list. Selecting the already-current definition, or clearing an already-unassigned classification, is rejected. Recorded changes do not automatically alter rates, product, review rules, status, term dates, membership subject or entitlements. No category or band is backfilled from product/CRM categories.

## API and interface

`GET /api/v1/membership/members/{id}/classifications` requires Membership read permission. It returns the membership version, both current classifications and a history page of 25 rows. `page` uses the existing validated Membership pagination parser. History is ordered by effective date descending, recording timestamp descending, sequence descending and ID. The current summary is independent of the requested history page.

`POST /members/{id}/classifications/category` and `/subscription-band` additionally require Membership manage permission. Payload is `{ version, definitionId, effectiveOn, reason }`; a null definition records clearing. Every change touches the parent membership version and adds a `membership.classification.updated` audit receipt. Lifecycle, administration and classification edits share the same concurrency boundary; stale saves conflict. History updates and deletes are rejected by the existing append-only domain guard. SQL also enforces tenant-bound parent/definition foreign keys, positive sequences, supported kinds and unique per-membership/kind sequences.

The Membership detail screen provides current values, assignment forms, management links and paged history. Read-only staff see the current values/history without a mutation form. Contact and Account Membership details project the current category, band and effective dates. Clearing displays Unassigned, while absent history displays Not recorded. Portal's own-membership contract remains unchanged and excludes these staff classifications. Classification audits do not replace the latest lifecycle action in the Contact view.

## Migration and next scope

`20261004092053_MembershipClassificationHistory` adds ClassificationChanges, its data audit table, foreign keys and indexes. Existing membership and administrative values remain unchanged; existing memberships have no classification until explicitly assigned. This task has not applied the migration to the shared development database. Source changes remain uncommitted.

## Verification

Six API checks passed: two new classification checks, the extended Portal boundary check and three existing Contact membership projection checks. One isolated SQL Server migration/query test passed; no selected tests were skipped. Coverage includes assignments, same-day correction, clearing, invalid/backdated/future changes, wrong lookup kind, stale saves, lookup-name snapshots, preservation of lifecycle/rate fields, read/manage permissions, current Contact projections and Portal exclusion. SQL verification covers upgrade preservation, repeat migration, current-value selection across 29 history rows, history pagination, append-only guards, unique sequences, foreign-tenant definition rejection and restricted deletion of referenced definitions.

TypeScript, focused ESLint, the production build and focused whitespace checks passed. The build retained seven existing generated CSS warnings. Authenticated browser appearance remains unverified following the preceding unavailable local-sign-in check.

Next: original join date and effective membership lifecycle dates/history. Fee exemptions, lapse policy, payer/invoicing, settlement projections and Direct Debit retain their separate scope boundaries.

## Member-type continuation

4 October 2026: member type now uses the existing `member-type` setup list, independent effective history and `POST /members/{id}/classifications/member-type`. GET returns category, subscription band and member type. The staff editor and CRM projection display all three; no classification changes rates. Migration `20261004125422_MembershipMemberTypeHistory` expands the supported-kind constraint. See [fee/lapse continuation](MEMBERSHIP_FEE_AND_LAPSE_OPTIONS.md) and [current progress](MIGRATION_TEMPLATE_PROGRESS.md).
