# Stage 8 — Insights and operational foundation



Implemented a report builder and dashboard over six published datasets: Contacts, memberships, events/learning offerings, sales orders, campaigns and tasks. Each module owns its projection. The host composes these contracts without reading module internals. Dataset discovery, dashboard counts, queries and exports require both Insights access and the dataset's module read permission. Persistence tenant filters remain active.



Reports support name search, exact status, creation date bounds, deterministic pagination and CSV export. Queries accept a bounded typed request; arbitrary tables, expressions and SQL are unavailable. Exports reject more than 10,000 matches, escape formula prefixes and log dataset, row count, actor, tenant and trace identifiers without record contents.



Operational additions: `/health/ready` checks all module connections and pending SQL Server migrations; `/health` remains liveness. Responses include a request ID and nosniff header. Structured request logs use route templates rather than raw URLs, query strings or payloads. The `Nexora.Api` meter emits request duration with bounded route/status dimensions.



`Export-NexoraMigrations.ps1` builds and exports all seven idempotent migration scripts with a SHA-256 manifest. It does not apply migrations. `Test-Nexora.ps1` runs backend, web and diff gates. CI includes Windows LocalDB migration replay alongside ordinary backend and web jobs.



Local verification: 46 backend tests passed with SQL Server checks enabled (44 API/integration, 2 architecture). This includes all seven schemas sharing one fresh catalog, idempotent migration replay and no model drift, 10,000-record CRM paging, permission-aware reports and cross-tenant query isolation. See the release runbook for deployment acceptance.



Stage 8 is an operational foundation, not production release approval. Saved reports, arbitrary field selection/joins, scheduled report delivery, production-scale load tests, an external telemetry collector, real Entra OIDC, penetration testing, recovery exercises and deployment-specific hardening remain release work. Local identity is deliberately disabled outside Development.


Browser smoke checks passed for local sign-in, live Contact reporting and task creation/completion. The running SQL-backed API returned ready. The .NET vulnerability check reported no known vulnerable packages; npm audit remains pending because npm is unavailable in the local runtime.
