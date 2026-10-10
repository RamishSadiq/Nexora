# Membership workbook sheets: gaps and implementation

4 October 2026. Five related source sheets contain 106 field occurrences: Membership (54), DD Instalments (5), Membership Application (23), Membership Rate (13), Membership History (11). Exact header spelling and source cells remain in [the source inventory](../strategy/DATA_MIGRATION_FIELD_INVENTORY.md). This is application support and gap analysis; no source rows have been imported.

## Membership — 54 fields

| Source cells | Meaning | Application support and remaining scope |
| --- | --- | --- |
| B–D | Member, Contact and Account IDs | Legacy member reference, Nexora subscription ID and personal/organisation subject links. Source ID resolution remains an import step; Contact and Account are not universally both required. |
| E–F | Application and join dates | Linked application creation date and explicit original join date. Imported application date override remains missing. |
| G–K | Member type, category/band and their dates | Independent effective histories and setup definitions; no rate inferred from these values. |
| L–M | Status and status date | Owning-module lifecycle commands and effective dates; legacy status translation remains. |
| N | Primary Membership | Nullable recorded primary label implemented in this slice. Unknown is distinct from No. This is not a second entitlement or a selection rule between concurrent active memberships. |
| O–S | Archive, resignation, reinstatement, rejoin | Existing reversible archive and explicit lifecycle/rejoin evidence. Historical reconstruction/import remains separate. |
| T–Y | Joining exemption, free membership, pack tracking, renewal/lapse exclusions | Implemented options, quote rules, pack confirmation and renewal review. |
| Z | Remarks | Membership-owned administrative Remarks. |
| AA–AF | Renewal fields, promotion, payment method/frequency | Recorded arrangements implemented; no scheduling, discount execution or collection implied. |
| AG | Apply Rate to Paid By | Missing payer-based rate rule. Requires a defined rate-selection policy. |
| AH–AJ | Paid By and invoice recipient | Validated tenant-local payer and invoicing Account, separate from entitlement ownership. |
| AK–AO | Paid-through, latest payment, outstanding | Missing explicit term-linked obligations and Finance-owned settlement projection. Customer-wide totals cannot establish these fields. |
| AP–BC | 14 Direct Debit fields, including mandate-file inclusion | Provider integration remains missing. GoCardless and Stripe preference is saved, but mandates, bank information, collections, instalments and export inclusion have no implemented provider lifecycle. AQ–AS are *last* banking details, distinct from CONTACT's *current* bank fields. |

After the primary label slice, the main Membership sheet has application representation for 34/54 field occurrences: 25 general and nine finance. The other 20 comprise six finance and 14 Direct Debit fields. This count does not establish historical import or live-provider readiness.

## DD Instalments — five fields

Member_ID links to the owning subscription; Instalment No, Amount, Paid and Due Date need an owned instalment schedule and settlement evidence. No schedule model exists yet. Paid must reflect confirmed/reconciled settlement, not a manually inferred provider preference or a successful authorisation. Currency is absent from this sheet and must come from an explicit membership/migration currency mapping. GoCardless/Stripe mandate, collection, failure and reversal evidence must precede a claim that instalments are paid.

## Membership Application — 23 fields

| Source cells | Current support / gap |
| --- | --- |
| B–E Member_ID, Member_Name, Contact_ID, Account_ID | Approved membership link and Contact/Account IDs/names exist. Member_ID is absent until approval. Legacy references need resolution. |
| F–H Member_Type, Member_Category, Subscription_Band | Application-owned requested IDs and recorded names now exist. Approval creates the initial membership classification receipts without inferring values from current member classifications. |
| I–L Payment_Method/Frequency and invoicing Account | Nullable application-specific snapshots and approval handoff are implemented; invoicing Accounts are tenant-validated and rechecked at approval. |
| M Amount | Submitted fee quote is snapshotted, with joining-fee/free options; external source amounts require currency and reconciliation validation. |
| N Application_Date | Creation timestamp exists. Explicit historical application date import is missing. |
| O Application_Type(New, Upgrade, Reinstate, Rejoin) | Distinct LifecyclePurpose now records New/Rejoin. Existing ApplicationType still means corporate/individual/affiliate. Upgrade and Reinstate application transitions remain missing. |
| P–Q Application_Status, Status_Date | Submitted/approved/rejected statuses now have an operational effective status date, recorded at submission/decision. Legacy dates remain null; historical status translation/import is still missing. |
| R Exempt_Joining_Fee | Explicit nullable staff application flag and quote snapshot exist. |
| S–T Remarks, Description | Separate staff Remarks and Description are implemented and excluded from Portal receipts. Applicant notes and decision reasons retain their own meanings. |
| U–X Bank, Account_Title, Sort_Code, Account_Number | Missing protected/provider-backed capture and migration path. Do not store raw bank details in generic applicant notes or attributes. |

## Membership Rate — 13 fields

The product base rate and fee policy do not represent the source rate matrix. The generic member-rate/member-rate-type lookups are labels, not a typed rate definition. Description, rate type, fiscal period, member type/category/band selection, VAT applicability/inclusion/rate, nominal code, cost centre, join amount and renewal amount need a versioned rate model and quote snapshots. Source currency, fiscal-period boundaries, rate precedence and tax interpretation must be defined before imports or automatic payer-based rating. Current fee quotes remain unchanged by this audit.

## Membership History — 11 fields

This slice adds `GET /api/v1/membership/members/{id}/timeline`, a staff-only, read-only projection of immutable classification and lifecycle evidence. It requires Membership read permission, respects tenant filters and is available for archived members. It does not copy or rewrite source history. The Membership detail screen includes the timeline plus the existing audit history.

Member_ID is the subscription ID. Type identifies member-type/category/subscription-band/status; Value contains that dimension's recorded name or status. This maps C–F according to H (Type), rather than inventing all four dimension values on every row. Resign_Reason comes only from an explicit resignation receipt. Effective_Date uses the recorded effective date; an undated legacy receipt remains unknown. Description shows the recorded reason, without claiming it preserves a separately imported source Description.

Ceased_Date is derived as the next recorded effective observation for the same dimension and is exclusive. Days is the calendar-day difference for a closed, dated interval. Open or undated intervals have null Ceased_Date/Days. Same-day corrections retain immutable receipts and can have zero days; classification sequence determines their order. Lifecycle receipts include renewal observations because a renewal can restore a lapsed membership to active. An active renewal observation does not change the stored Status_Date. This projection uses these explicit interval semantics; the workbook's source Ceased_Date/Days conventions still need validation for historical import.

Pagination returns 25 rows with optional type filtering, pages 1–1000, ordered by effective date descending, type, classification sequence descending, recording time descending and ID. SQL queries load bounded prefixes of each source stream including all newer evidence before deriving intervals, so a page boundary cannot incorrectly leave an older interval open. Invalid types/pages are rejected; non-lifecycle option/review/admin receipts are excluded. Staff reasons, actor IDs and timeline rows are not added to Portal DTOs.

## Delivered and next work

`20261004140347_MembershipPrimaryLabel` adds nullable PrimaryMembership without backfilling Yes/No. The existing versioned Membership options endpoint updates it with a required reason and an audit receipt; CRM projects it and the staff editor offers Unknown/Yes/No. The one-active-membership-per-Contact/Account rule remains enforced. The flag does not change rates, dates, status, owner, payer, certificate or invoice.

The user-directed Membership Application slice now represents 18/23 fields, with partial lifecycle-purpose support and four deferred bank fields. See [application mapping and limits](MEMBERSHIP_APPLICATION_TEMPLATE.md). Upgrade/Reinstate application transitions and historical imports remain separate from routine approval. Subsequent slices cover the rate matrix and term-linked financial/instalment model. No migrations in this task were applied to the shared development database; changes remain uncommitted. Authenticated browser appearance remains unverified.

Verification: 19 selected API/SQL tests passed without skips, including the new primary-label and timeline checks, administration/options regressions, Portal exclusion and an isolated SQL Server migration/projection check. The two new API checks also passed in their final rerun. Coverage includes nullable flag clearing, required reasons, stale saves, unchanged entitlement fields, duplicate active membership rejection, same-day history corrections, cross-page interval closure, undated legacy receipts, renewal observations, validation and foreign-tenant denial. TypeScript in the production build, focused ESLint and the production frontend build passed; seven pre-existing generated CSS warnings remain. Generated build changes to Next configuration files were restored while retaining other workspace edits.

Final pagination-order refinement: API timeline and isolated SQL Server tests both passed after using GUID text ordering consistently in SQL and the merged projection. This preserves deterministic page boundaries even when recording timestamps tie. New-source whitespace checks passed.

Membership Application verification: 55 selected backend API/SQL tests passed with no skips, covering requested-detail submission/edit/clear, stale-version rejection, lookup-kind validation, preserved quote and name snapshots, approval handoff, unavailable invoicing Accounts, New/Rejoin purposes, existing membership lifecycle rules, and Portal privacy. The isolated SQL Server check validates nullable legacy defaults, repeat upgrade, retained values, 4,000-character text, tenant-bound lookup foreign keys and restricted lookup deletion. TypeScript, focused ESLint and the production frontend build passed; seven pre-existing generated CSS warnings remain. Build-generated configuration changes were restored while retaining other workspace edits. This evidence does not establish historical import, live-provider readiness or authenticated browser appearance.
Final focused rerun: the isolated SQL upgrade test and API permission/foreign-tenant test both passed after the final test refinements, with no skips or compiler warnings.
