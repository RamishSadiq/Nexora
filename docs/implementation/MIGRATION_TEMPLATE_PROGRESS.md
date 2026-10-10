# Migration template implementation progress

4 October 2026. The source workbook contains 22 entity sheets and 717 field-header occurrences. All sheets have been inventoried in [the field inventory](../strategy/DATA_MIGRATION_FIELD_INVENTORY.md). Inventory completion is separate from application support, import tooling and successful data migration.

Current implementation work is CONTACT first. CONTACT contains 185 field occurrences across 12 sections. Application representation now covers 127/185 occurrences (about 69%). This is a field-coverage measure, not an import-readiness or migration-completion percentage. Provider preference is an additional application field and is not included in the workbook count.

| CONTACT section | Application representation | Remaining limitations |
| --- | ---: | --- |
| General Information | 17/17 | Historical ID/status translation and validated import remain separate work. |
| Emails | 7/7 | Provider delivery/engagement evidence is separate from stored values. |
| Mobile, Telephone and Fax | 9/9 | Import validation and duplicate resolution remain. |
| Addresses | 39/39 | Explicit purpose/priority and address details implemented; import resolution remains. |
| Social Media | 11/11 | Counters retain explicit provenance; no live social synchronisation implied. |
| Communication Permissions | 8/8 | Nullable permissions and existing marketing suppression; import provenance remains. |
| EDI | 0/15 | Restricted data and access rules remain deferred. |
| Profile | 5/5 | Hierarchical taxonomy and five slots implemented. |
| Directory | 0/24 | Publication, consent and directory ownership rules remain deferred. |
| Membership General | 22/22 | Operational support implemented; historical import, status translation and backdated lifecycle reconstruction remain. |
| Membership Finance | 9/15 | Recorded arrangement only; payer rating and five settlement fields remain. Scheduling, promotion execution and provider collection are separate. |
| Membership Direct Debit | 0/13 | GoCardless and Stripe selected; provider preference implemented. Mandate, banking, collection and instalment integration remain. |

The other 21 workbook sheets are inventoried, but have not all received a complete field-by-field implementation and verification pass. Reused Contact/Account relationship or Membership support does not establish completion of an entire other sheet.

This continuation adds member-type history, joining-fee exemption, free membership, lapse exclusion, recorded financial arrangements and GoCardless/Stripe preference. See [fee/lapse options](MEMBERSHIP_FEE_AND_LAPSE_OPTIONS.md), [financial arrangements](MEMBERSHIP_FINANCIAL_ARRANGEMENT.md) and [exact CONTACT membership mapping](CONTACT_MEMBERSHIP_PROJECTION.md).

Source changes and migrations remain uncommitted. No source workbook data has been imported and this work has not applied migrations to the shared development database. Authenticated browser appearance remains unverified because local sign-in was unavailable.

Final continuation checks: 22 selected tests passed, none skipped (21 API checks and one isolated SQL Server upgrade/projection check). The selected suite covers Membership administration/general options, classification history, Portal boundaries, financial-arrangement validation and settled renewals with the default customer, an explicit payer and an explicit invoicing Account. SQL verifies nullable defaults, stored provider preference and financial arrangement, repeat upgrade, unchanged subscription values and tenant isolation. Earlier in this continuation the separate classification SQL regression also passed. TypeScript, focused ESLint and the production frontend build passed. The build retained seven existing generated CSS warnings. Shared development data and live provider accounts were untouched.

Next implementation order: explicit term-linked financial obligations and settlement projection; tenant-bound GoCardless/Stripe connections, hosted authorisation and verified events; remaining Direct Debit fields. EDI and Directory retain their documented deferred scope until business/access/publication rules are defined. Then perform the same section-by-section gap pass for ACCOUNT and the remaining entity sheets.

User-directed continuation: the five membership-related sheets are now the active priority. [Membership sheet progress](MEMBERSHIP_SHEETS_PROGRESS.md) records their 106 fields, reuse and remaining gaps. The main Membership sheet now has 34/54 fields represented after adding the recorded Primary Membership label; a paged classification/status timeline supports the Membership History view. CONTACT's 127/185 count is unchanged because its sheet has no Primary Membership column. Membership Application is the next sheet to extend.

Latest user-directed priority: move to ACCOUNT. [Account sheet progress](ACCOUNT_TEMPLATE_PROGRESS.md) records the 160-field audit and new 12-field Account general-information slice. Account general information now represents 20/24 fields, with dated parent relationships still missing. Across Account sections, 90/160 fields have application representation; Contact-only social/permission features and finance-purpose/addressee differences are explicitly excluded from that count. The unfinished membership-sheet work remains documented for a later continuation.

Latest user-directed priority: Membership Application. [Application template mapping](MEMBERSHIP_APPLICATION_TEMPLATE.md) records 18/23 represented fields, partial New/Rejoin lifecycle purpose, and four deferred protected bank fields. Requested classifications, payment/invoicing snapshots, status effective date and separate staff Remarks/Description are implemented with versioned review and approval handoff. Upgrade/Reinstate application transitions and historical source-row import remain outstanding.

Membership Application validation: 55 selected backend tests passed without skips, including isolated SQL migration checks. TypeScript, focused ESLint and the production build passed with the seven existing generated CSS warnings. Shared development migrations remain unapplied; source rows remain unimported and changes uncommitted.
