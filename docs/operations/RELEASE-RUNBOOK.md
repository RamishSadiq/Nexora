# Nexora release and recovery runbook

## Current release status

Local core workflows through Stages 4–8 are implemented. Production deployment is not approved by these checks. The Entra settings are a configuration boundary only: production sign-in needs the actual OIDC adapter and validation. Never enable local Development authentication in production.

## Build and verify

1. Use the versions pinned by `global.json`, the web lockfile and repository tool manifest. Run `npm ci` in `apps/web` and `dotnet tool restore` at repository root.
2. Run `scripts/Test-Nexora.ps1 -SqlServerConnection '<test SQL Server connection>'`. SQL tests always generate and delete their own uniquely named catalogs; the server principal needs database creation rights. Never point ordinary application tests at production.
3. Run `scripts/Export-NexoraMigrations.ps1`. Review the seven SQL scripts and their checksum manifest with the release artifact. CI exports migration artifacts from its SQL verification job.
4. Run dependency vulnerability checks (`npm audit` in the web project and `dotnet list backend/Nexora.slnx package --vulnerable --include-transitive`) and resolve or explicitly accept findings with an owner. Validate artifact provenance and secret scanning before publishing.

## Database rollout

Take and restore-verify a full database backup first. Rehearse the reviewed scripts against a restored staging copy and reconcile table counts and business totals. Set a maintenance window for incompatible changes. Apply scripts in Identity, Crm, Membership, Events, Finance, Engagement, Work order using a migration principal; the runtime principal should not own schema changes. Record each script checksum and execution result. The design-time factories read `NEXORA_CONNECTION_STRING`; the running application reads `ConnectionStrings__Nexora`. Do not mix these settings or assume one configures the other.

No automatic production migration occurs at application startup. Development seeding with `Identity__SeedAdminPassword` does apply migrations and grants current administrator permissions. Remove that environment variable after local setup and sign in again after permissions change.

## Deployment acceptance

Configure HTTPS, trusted proxy handling, narrowly scoped allowed origins, persistent protected cookie data-protection keys shared across replicas, secret storage and least-privilege database access. Complete Entra OIDC sign-in/sign-out, tenant mapping and disabled-user/session tests before enabling production traffic. Connect logs and the `Nexora.Api` meter to the chosen collector; alert on readiness failure, elevated errors and latency. Check `/health/ready` returns 200 and smoke-test signed-out denial, tenant switching attempts, CSRF rejection and each role's module access.

Acceptance requires accounting review of finance examples; membership policy approval; consent retention/delivery policy; attachment scanning and retention; representative load/concurrency tests; accessibility checks; penetration testing; and documented RPO/RTO with an exercised restore. Record owners and evidence rather than treating this checklist as completed by the code build.

## Rollback and recovery

Stop writes if integrity is uncertain. Preserve logs and trace IDs. Roll back the application only when its schema compatibility was verified during rehearsal. Prefer a reviewed forward fix for schema changes; do not run unreviewed down migrations against live financial data. If restoring, restore to a separate catalog, verify tenant counts, posted totals, allocations, credits and refunds, then switch the connection under maintenance control. Replay external transactions only from independently reconciled references. Demonstrate and record recovery time and lost-data window before release.

## Incident triage

Use `X-Request-ID` to correlate reports with structured logs. Route duration metrics exclude raw URLs and record contents. Do not collect cookies, passwords, CSRF tokens or request bodies. Readiness failures indicate connectivity or unapplied migrations; details are intentionally not exposed publicly. Campaign and payment delivery are currently unconnected, so a prepared audience or recorded donation is not evidence of external delivery or settlement.
