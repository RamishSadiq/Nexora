# Stage 4: Membership

Core delivery: products/categories, term lengths and currency/rates; applications with immutable quote snapshots; reasoned approval/rejection; membership activation, renewal, cancellation, lapse and reinstatement; actor/correlation/date status history. The Membership workspace supports catalogue maintenance, applications and decisions, member search and lifecycle actions.

Membership references CRM through ICrmDirectory, not CRM entities. Tenant filters, guarded writes and composite keys enforce isolation. A filtered unique index prevents simultaneous active subscriptions for the same Contact/product; application versions and one-subscription-per-application keys prevent duplicate approval. Renewal uses the current product rate and extends the existing end date; quotes retain the original rate. These are membership records, not payment charges.

The module owns the membership schema and migration history. Its initial migration was applied to the isolated SQL Server review database. API integration coverage proves quote preservation, approval, renewal, cancellation, reinstatement, stale update rejection, overlapping membership rejection, missing CSRF and tenant scoping. Web lint/build pass. Member and application lists now support server-side pagination, contact-name search and domain status filters. Policy-specific proration, automated lapse jobs and full assisted-user acceptance remain follow-ups.

Permissions: membership.read, membership.manage and membership.decide. Restart development seeding and sign in again for new administrator claims. Apply controlled migrations with the MembershipDbContext design-time factory and NEXORA_CONNECTION_STRING. No real payment or member communications are sent.

## Member and application list API

GET /api/v1/membership/members and GET /api/v1/membership/applications
now return { items, totalCount, page, pageSize } instead of a bare array.
All in-repository list consumers use this envelope. External consumers must
update before adopting this version. No schema migration is required.

- page defaults to 1; pageSize defaults to 25 and accepts 1–100.
- Malformed, nonpositive and overflowing pagination inputs return HTTP 400.
- search matches ContactName; nonblank input is trimmed and limited to 160 characters.
- Member statuses: active, cancelled, lapsed.
- Application statuses: submitted, approved, rejected.
- Counts and items use the same tenant-scoped filters.
- Members sort by contact name then ID. Applications sort newest first then ID.
- A valid out-of-range page returns no items and retains the filtered total.
- Offset pagination is stable for an unchanged dataset; concurrent writes may
  change counts or page boundaries. It is not a snapshot export.

The UI maintains independent controls for both lists, resets page on filter or
page-size changes, rejects stale responses, and navigates to the last valid page
when a decision or lifecycle change removes the last result on the current page.
Loading, empty, error/retry and accessible result-range states are included.

Regression coverage:
- MembershipPaginationTests covers 225 records per list, stable repeated pages,
  filtering, totals, tenant isolation, access denial and invalid queries.
- membership-pagination.spec.ts exercises application decisions, cancellation,
  page recovery, empty results, late responses and retry through the browser.
- Run node scripts/Test-Automations.mjs membership-pagination.spec.ts for focused
  browser checks using an isolated SQL Server LocalDB database and Edge. Omit the
  filename to include the existing automation checks.

## Integration verification — 2026-09-15

- Backend Release suite with NEXORA_TEST_SQLSERVER set to LocalDB: 69 API tests
  and 2 architecture tests passed, none skipped.
- Frontend production build and TypeScript passed.
- Frontend lint: no errors; one existing unused onClose warning in CRM.
- Focused membership browser suite: both tests passed; isolated database removed.
- Full browser run also exposed a CRM layout drag-and-drop assertion failure at
  apps/web/tests/automations.spec.ts:56 (expected Custom fields first, received
  Addresses). CRM source and that test were unchanged from the handoff snapshot.
  The existing Work automation browser test passed. The full browser suite is
  therefore not recorded as passing.

The submitted external patch was malformed and referenced routes, helpers and
domain types absent from the supplied source snapshot. Its intended functionality
was implemented against the actual repository; the patch was not directly applied.
