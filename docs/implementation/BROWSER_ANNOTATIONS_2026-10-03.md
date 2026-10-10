# Browser annotation implementation — 3 October 2026

Scope: the 18 Login/Dashboard/Contacts annotations and the subsequent 12 Membership/Events annotations.

## Delivered

- Floating space-themed login, four galaxies and a solar system, proper Nexora branding, a subtle help animation and public login guide. Motion pauses during form interaction and respects reduced-motion preferences.
- Dashboard uses permission-filtered stored reporting datasets, the signed-in user's name, 1/5/10/15/30/60/90/365-day creation-date ranges, report-name fronts and animated count backs. Empty data stays empty; sample totals, activity and chart were removed.
- Lighter workspace colours. Contacts, Members, Applications and Events/Courses/Exams headings appear in the workspace header. Listing toolbars sit immediately below it.
- Bottom listing counts/pagination and 10/20/50/100 page sizes. Member alignment, selection checkboxes and permission-aware row actions. Membership cancellation and application rejection retain existing domain actions rather than inventing deletion.
- Separate routed member/application/offering detail pages and Add application/offering creation pages. Existing decision, evidence, renewal, booking, attendance and results handlers are retained.
- Compact contact label heading and back control, Personal Details wording, Active green/Lapsed blue/Archived red banner, circular image/initials, protected PNG/JPEG image upload and read-only creation time. Lapsed is accepted consistently by CRM forms, listing filters, imports and workflow record actions.
- Per-entity DataAudit tables across all nine persistence contexts. Tracked SaveChanges writes capture old/new scalar values in the same transaction. Audit mutation is rejected, tenant queries fail closed, protected payloads and binary content are redacted. Contact history includes profile/child/relationship changes and date-range pagination.

## Verification

- Targeted ESLint and TypeScript checks passed.
- 22 targeted backend tests passed: data audit, contact presentation/photo scan/version checks, tenant persistence, stored reporting and shared contact names.
- SQL Server PlatformMigrationTests passed: all nine contexts migrate, replay, and have no pending model changes in an isolated test catalog.
- All nine migrations applied to local NexoraDev, preserving existing records.
- Browser checks passed: public help navigation and login; dashboard live results, flip and day filter; member selection/page size/detail/cancel intent; application Add/detail routes; event Add/detail routes; contact page size URL, creation date, audit controls and mobile overflow.
- Screenshots are retained under artifacts/annotations-*.png; test scripts under artifacts/verify-annotation*.cjs.

## Boundaries

- Dashboard reports count records created within the chosen period, not historical balances or reconstructed historical status.
- Audit history begins when enabled; prior values cannot be reconstructed from the older action-only timeline. Capture covers EF tracked writes, not direct database SQL or ExecuteUpdate lease operations. Sensitive/JSON payloads are deliberately redacted.
- Photo scanning uses the existing document-scanner service. Clean and unavailable scanner paths were tested with isolated test scanners; a live scanner must be configured to accept images.
- No production deployment, commit or push was performed for this annotation batch. Unrelated in-progress workflow changes remain untouched.
