# Website and Portal debugger readiness investigation

> Current VS Code setup (24 September): use **CRM**, **Portal** or **CMS**. [Local testing instructions](../LOCAL-TESTING.md) supersede the older launch names and LocalDB guidance below.


**Date:** 21 September 2026.

## Finding and limits

Both VS Code compounds launch `Website API (LocalDB)` and wait through `scripts/Prepare-WebsiteDebug.ps1`. That script cancelled each HTTP readiness request after two seconds. The endpoint passes the request-aborted token into SQL connection and migration operations. A client timeout therefore cancels EF work; `TaskCanceledException` is an `OperationCanceledException` and was deliberately outside the old catch filter.

This timeout mismatch is confirmed in source and runtime timing: a fresh process running the original WebsiteDebug binary against the exact configured database returned HTTP 200 in 4.864 seconds. The instrumented replacement returned HTTP 200 in 5.800 seconds on its first request. Healthy startup requests therefore exceed the old two-second polling allowance.

LocalDB was stopped at the initial inspection. A direct SQL connection auto-started it and confirmed `NexoraDev` ONLINE. LocalDB cold start and first EF model/provider initialization are contributing startup costs. The evidence does not establish that the original exception occurred in a particular context: the original endpoint did not log that information. Content is checked first and was the slowest in the instrumented run, but that does not prove it was the context in the user's exception.

The already-running VS Code API on 5097 returned 503 after approximately 14.5–14.8 seconds even with a longer client timeout. Its old code suppresses the underlying error. This additional failure was not reproduced in a fresh process using the same original binary and explicit launch connection settings. Do not claim that the live debug process's exact failure is diagnosed. Restart it with the new code; the new logs distinguish its context, target, phase, SQL error code and cancellation source if it persists. The existing user debug process was left running.

## Connection and schema evidence

All eight contexts register through scoped `AddDbContext` and use `ConnectionStrings:Nexora`. For these two compounds the launch environment overrides appsettings with:

```text
Server=(localdb)\MSSQLLocalDB;Database=NexoraDev;Trusted_Connection=True;TrustServerCertificate=True
```

This is per-user SQL Server LocalDB, owned by `TARGET\Ramish`, not Docker, remote SQL Server or the separately running `SQLEXPRESS` service. The ordinary `Nexora API` debug profile targets SQLEXPRESS instead; that profile is not used by the Website/Portal compounds. The exact Website/Portal connection string parsed from launch.json also opened successfully in a direct check (91ms when warm).

| Context | Server/database | Migration history schema | Instrumented connection / total context time |
| --- | --- | --- | --- |
| ContentDbContext | `(localdb)\MSSQLLocalDB` / `NexoraDev` | content | 1790 / 2085ms |
| NexoraIdentityDbContext | Same | dbo | 390 / 445ms |
| CrmDbContext | Same | crm | 421 / 441ms |
| MembershipDbContext | Same | membership | 169 / 190ms |
| EventsDbContext | Same | events | 82 / 92ms |
| FinanceDbContext | Same | finance | 111 / 120ms |
| EngagementDbContext | Same | engagement | 68 / 77ms |
| WorkDbContext | Same | work | 53 / 63ms |

Every context connected and had no pending migrations in the instrumented run. Timings are one observation, not a performance benchmark; request startup/model resolution adds work outside these per-context timers.

Connection timeout defaults to 15 seconds. SQL command timeout defaults to 30 seconds. No module-specific retry-on-failure configuration was found in these registrations. `GetPendingMigrationsAsync` does add per-module history queries and latency. It is necessary here because each module owns separate migrations, even though all modules share a catalog. Request `IServiceProvider` is the Minimal API request scope; contexts are not resolved from the root or used concurrently.

## Implemented fix

- Startup polling now allows 45 seconds per HTTP request and retries within a 180-second startup window. It prints the last failure instead of silently discarding every polling error. An in-flight final attempt may finish after the window.
- Readiness has a 30-second total asynchronous database-check budget, shorter than the startup client's timeout. It reports budget expiry as 503 and preserves genuine caller cancellation with explicit logging.
- Replace `CanConnectAsync` with explicit `OpenConnectionAsync`, which exposes provider failures instead of reducing them to false. Keep that connection open for the module's migration check and close it afterward. Retain all module migration checks; avoid an extra explicit database-existence probe before them.
- Log context, DataSource, database, configured/default timeouts, connection success, pending migration IDs, phase, elapsed time, exception type and SQL error number. Never log the full connection string or arbitrary provider exception messages. Detailed diagnostics stay in server logs; the public endpoint remains a minimal 200/503 response.
- Do not change database target, create/repair the development database, apply its migrations, reset credentials, suppress all cancellation, or mark unavailable databases ready.

Example diagnostic format:

```text
Readiness ContentDbContext -> (localdb)\MSSQLLocalDB -> NexoraDev: connected in 1790ms
Readiness ContentDbContext -> (localdb)\MSSQLLocalDB -> NexoraDev: ready in 2085ms
```

A caller cancellation is labelled `caller cancelled`; the server deadline is labelled `readiness budget expired`; database/provider failures include their type and SQL number; schema drift identifies pending migration IDs.

## Validation and next local launch

- API build passed in isolated `ReadinessFix` configuration; existing ASP.NET route-analyzer AD0001 warnings remain.
- Seven targeted tests passed, none skipped: connection traversal/closure, safe failure logs, caller cancellation, server timeout, unexpected provider cancellation, the existing readiness HTTP integration, and a real SQL Server migration-history regression.
- The SQL regression creates a disposable `NexoraReadinessTest_*` catalog: Content migrations alone must not make Membership ready; applying Membership migrations then yields readiness. The catalog is deleted afterward. `NexoraDev` was not migrated or modified by this test.
- Exact command: `dotnet test backend/tests/Nexora.Api.Tests --configuration ReadinessFix --no-restore --filter 'FullyQualifiedName~ReadinessTests|FullyQualifiedName~ReportingTests' -v quiet`, with `NEXORA_TEST_SQLSERVER` set to the local integrated-auth master connection.
- PowerShell startup script parsed without errors. Full frontend/browser regression was not run for this backend/startup change.
- Temporary comparison API processes on ports 5109 and 5110 were stopped. User debugging processes were not stopped.

Stop the existing VS Code compound, then launch either **Portal (API + Web)** or **Website (API + Web)**. Its existing prelaunch task builds the updated WebsiteDebug API. They share ports 5097/3017 and should run one at a time. If the debugger is already paused on an exception, resume or stop it before expecting an HTTP health check to finish. If 503 persists after restart, inspect the new `Readiness` lines rather than increasing timeouts again.
