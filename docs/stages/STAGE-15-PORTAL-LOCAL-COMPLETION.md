# Portal local completion — 23 September 2026

> Current VS Code setup (24 September): use **CRM**, **Portal** or **CMS**. [Local testing instructions](../LOCAL-TESTING.md) supersede the older launch names and LocalDB guidance below.


This delivery continues the agreed local-development portal. Production registration, invitations, identity verification, recovery and external message delivery remain deferred. Paid checkout and later event/service journeys are outside this core scope.

## Delivered

- Member notification inbox with refresh, unread indicators, persistent mark-as-read, error recovery and member navigation. Shows the latest 100 recipient-scoped messages; makes no claim of email delivery. Submission, decisions, access changes and evidence results reuse the existing durable notification service.
- Correspondence address editing alongside display name. The six address fields use the existing CRM Address storage with the reserved `Member correspondence` label. Other address labels remain untouched. This is member-supplied correspondence data, not verified identity data; login email, staff ownership, consent and company roles are not editable here.
- Strict profile payload allowlist and bounded address fields, current Contact binding and shared record concurrency. Omitting the address retains it; explicit null removes it. Stale updates cannot overwrite a newer CRM change. Existing optional null address lines are normalized for the form.
- Correct rejection wording for evidence notifications. A rejected upload is described as rejected rather than awaiting a scan.
- Clean evidence download and staff access checks, personal/company isolation and immediate denial after company-grant revocation. Controlled scanner results are injected only in API tests; the application still fails closed when ClamAV is unavailable.
- Real SQL Server simultaneous activation test for personal and company memberships across different products. It verifies the existing one-active-membership-per-subject database constraint.
- Approval, renewal and reinstatement now check that same rule before writing, with a clear message to end the existing membership first. The SQL constraint remains the protection against simultaneous writes. The personal browser journey checks denial, cancellation of the old membership, then successful activation.
- Fresh-database migration repair: `RemoveSubmittedApplicationGuardsFinal` previously dropped indexes already removed by its predecessor. Conditional drops preserve the repair behavior and allow clean replay. Its rollback leaves the preceding schema unchanged; the earlier migration owns index restoration. No development database was modified for verification.

## Acceptance evidence

Full API regression: **114 passed, zero failed or skipped**, including disposable SQL Server migration replay, model checks, tenant isolation and the new personal/company activation race. Command: `dotnet test backend/tests/Nexora.Api.Tests --configuration PortalVerify -v minimal`, with `NEXORA_TEST_SQLSERVER` targeting integrated-auth SQL Express. Log: `artifacts/portal-finish-api-verified.log`.

TypeScript and targeted ESLint passed for the changed portal components. The final production build and all three browser journeys passed (55.3 seconds): company grant/application/approval/revocation, personal approval with the one-active-membership guard, and personal rejection. The personal journeys cover correspondence persistence and notification read state across reload. Final desktop/mobile screenshots were visually reviewed with no overflow. Command: `node scripts/Test-Website.mjs --portal --production`, with `NEXORA_TEST_SQLSERVER_INSTANCE=.\SQLEXPRESS`. Log: `artifacts/portal-finish-browser-complete.log`; isolated run: `artifacts/website-f9321e87fffc41039aa22a89deaef8ff`. The runner removed its processes and disposable database.

## Operational boundaries

The production identity/provider decision remains deferred. External email delivery is not configured. A live ClamAV deployment and operational scan verification remain required before claiming production document readiness; controlled test scan results do not establish antivirus installation or signature freshness. No public deployment is included.

Run the existing **Portal (API + Web)** debugger compound to use `/portal`; restart an existing session to load the rebuilt API. Staff access management remains under `/settings`. Keep the shared Website/Portal debugger ports available to one compound at a time.
