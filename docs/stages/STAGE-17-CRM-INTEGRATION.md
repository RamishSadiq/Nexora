# Stage 17 — CRM integration and unified local debugging

24 September 2026.

## Audit outcome

The earlier CRM estimate covered core records, dated Account–Contact associations, fields and saved views. Portal already writes names/correspondence details to those records and uses explicit access grants. CMS owns tenant presentation and links to Portal; it does not collect per-person website activity. No parallel identity store, CMS contact copy or anonymous visitor-to-CRM capture was needed.

The missing CRM surfaces were connected record context/search, reconciled data transfer, scan-gated record documents and a coherent local debug entry point.

## Delivered

- Contact/Account connected sections for applications, memberships, membership history, portal notifications, recorded personal/company access grants and submitted evidence. Existing events, invoices, groups and tasks also appear where linked. Each module owns its query through a shared read contract; CRM does not read sibling module DbContexts directly. Source permissions are checked before calling each provider. No unsubmitted portal draft or internal decision narrative is projected.
- CRM search covers names, communication values, tags, custom text values and matching related membership products, invoice numbers, event names, groups and task titles. Source permissions apply to search as well as display. Broad connected matches over 1,000 are rejected with a refinement message rather than silently truncated.
- CSV export and preview/commit import with bounded rows, exact ID/version updates, duplicate name/email reconciliation, preserved existing email consent, atomic all-valid commits, serializable transaction checks and spreadsheet formula escaping. Import uses seven explicit columns and does not create logins or merge records. Existing owners/associations/custom attributes are preserved. CSV is a bounded core-record transfer, not a full database backup.
- CRM attachments reuse shared signature/size validation and scanner, require a clean status plus matching SHA-256 for downloads, expose retry/delete with versions and retain audit history after removal. Legacy files migrate to quarantine without losing bytes. Files stay private; CMS public grants do not expose CRM files.
- Exactly three visible VS Code debug compounds: **CRM**, **Portal**, **CMS**, with hidden internal services, one SQL Express database, safe port checks and source/browser debugging. See [local testing](../LOCAL-TESTING.md).

## Verification

- Full API suite: **122 passed, zero failed/skipped** (3m1s), `CrmComplete`, SQL Express enabled. Includes upgrade retention/quarantine, pending-model and volume checks. `artifacts/crm-api-verified.log`.
- Production frontend build and **all 5 CRM/Portal/CMS journeys passed** (1.7m). `artifacts/crm-browser-final.log`. Runner-owned database/processes cleaned up.
- Full TypeScript and targeted CRM/test ESLint passed. CRM desktop/mobile screenshots visually reviewed.
- Local source-debug verification passed against SQL Express `NexoraDev`: the API applied pending migrations, the test-data helper was repeatable, CRM and CMS staff routes loaded, and the demonstration member signed into Portal. The helper used installed Edge, matching the VS Code profiles. All owned processes were stopped afterward, freeing 5097/3017 for F5. Log: `artifacts/local-debug-verification.log`. This verifies the launch commands and source routes; no claim of manually stepping through the VS Code debugger UI is made.

Earlier runs exposed an ambiguous browser selector, a test-only permission setup/session issue and SQL volume timeout under competing builds. Those were resolved before the passing final results above.

## Operational boundary

Local functionality is distinct from production readiness. Live ClamAV, production identity, external notifications and DNS/TLS remain explicit deployment dependencies. No CRM data is published to the public CMS. Context sections show at most their latest 50 rows with total counts and links to the owning module.
