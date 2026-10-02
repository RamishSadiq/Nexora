# Advanced Workflow Designer and Automation Engine

Updated: 2 October 2026. Status: advanced engine and visual designer implemented. The assessment and proposed phases below preserve the original architecture decisions; the final delivery boundary describes the current code and verification.

This assessment responds to the supplied Advanced Workflow Designer & Automation Engine brief. It precedes production engine work. The requested entry point is **WFL Designer**, inside the signed-in account's **Admin** menu, opening a separate window. The initial planning workspace has now been replaced with a functional workflow editor. Existing Automations remain available.

## 1. Existing architecture assessment

Nexora is a .NET 10 / EF Core / SQL Server modular monolith with a Next.js 16 / React 19 frontend. Keep the existing API, web application, database, authentication and module boundaries. Do not create a second CRM or adopt a new orchestration service at this stage.

The API composition root is `backend/src/Nexora.Api/Program.cs`. Domain modules own contexts and schemas; cross-module dependencies go through BuildingBlocks contracts. Architecture tests explicitly prohibit module-to-module project references. CRM, Membership, Events, Finance, Engagement, Work, Content and Identity are present. A new Workflow module can follow these conventions.

| Area | Current implementation and implications |
| --- | --- |
| Relationships | Contact, Account and relationship/profile records. AccountContact holds association roles and Department; these are relationship fields, not general Contact attributes. CRM files and permissions need their existing access checks. |
| Membership | Applications, subscription lifecycle, products/rates, organisation applications and portal entitlements. Approval, renewal, lapse and cancellation have domain constraints. One active membership per Contact/organisation must survive automated actions. Setup lookup values are currently independent; do not imply they are already linked to membership. |
| Events | Offerings, sessions, enrollments and exam results. Registration/results must use the owning module's validation. |
| Finance | Catalogue, orders, posted invoices, receipts, credits, allocations and refunds. Financial posting must remain a domain command; workflows cannot generically overwrite balances or posted statuses. External payment processing is not established by these records. |
| Engagement | Communities/members, meetings, campaigns, funds and contributions. Relationship and fundraising permissions remain enforced. |
| Work | Tasks, assignment, user-specific notifications, import rows and append-only history. Task endpoints already validate active tenant users/teams and Contact references. |
| Content | Website/portal pages, revisions, publishing, media and scheduled publications. Keep Save draft → Preview draft → Publish semantics. |
| Identity | Tenant users, teams, roles/security groups, permission claims and administration APIs. Current role permissions are refreshed during authenticated requests. Protected system administrator identities/groups remain protected. |

`TenantDbContext` provides tenant query filters, tenant-qualified foreign keys, write guards, optimistic GUID concurrency and shared attributes/configuration. Existing request identity comes from an authenticated HTTP principal; it is not sufficient for a worker without an HTTP request.

Most module mutations remain endpoint-oriented. Published directories and application/portal contracts exist, but a general, authorized record-command API does not. Extract individual reusable commands before offering corresponding workflow actions.

Current Automations (`BuildingBlocks/Persistence/AutomationConfiguration.cs`) store sections and bounded field rules. Rules run inside SaveChanges, use typed operations and dependency ordering, and target custom attributes. They have no durable runs, waits, retries or action history. Preserve this useful field-calculation feature; do not relabel it as the new engine. Recently removed custom-field UI must not be reintroduced incidentally.

Content's registered `ContentScheduler` is a real persisted scheduled publisher. It polls every 30 seconds, discovers due work, constructs tenant-scoped contexts, and rechecks the publisher's permission and media readiness. Reuse those security lessons. Its content-specific job table is not a generic workflow queue. `Nexora.Worker/Worker.cs` currently emits a five-minute heartbeat only.

Work notifications are task-linked and recipient-filtered; portal notifications are application-linked and have queue/delivery-status records. Neither proves a working general outbound email provider/template pipeline. Existing integration contracts and the scoped assistant bridge do not supply an arbitrary outbound REST action registry.

Reporting has `IReportDataset` and `ReportQuery` (search/status/date/page) with permission-governed module contributors. It returns Id, Label, Status and CreatedAtUtc, not arbitrary entity fields or relationships. Frontend entity lists and attribute maps are not a platform metadata registry. Reuse these existing query constraints through adapters; richer queries require explicit typed extensions.

## 2. Gap analysis and reuse decisions

| Capability | Decision |
| --- | --- |
| Tenant isolation, authorization, concurrency, CSRF, audit conventions | Reuse, with an explicit worker identity and current permission checks. |
| Existing module rules/transitions | Reuse through command adapters; extract code from handlers where necessary. Never bypass with generic EF writes. |
| Field rules and page layout configuration | Preserve separately; share typed operator concepts where appropriate. Do not migrate their JSON into workflows. |
| Work tasks and notifications | Extend through module-owned services; preserve assignee/recipient validation and task history. |
| Querying/reporting | Adapt available datasets and queries; add descriptors/cursors only where an owning module supports them. |
| Domain events | New reliable event envelopes and transactional outbox are required. No generic durable event pipeline was found in the inspected source. |
| Definitions, versioning, run/step history | New Workflow-owned persistence. |
| Queue, leases, checkpoints, retry, wait/resume | New durable SQL processing hosted by the existing worker project. |
| Entity/field/action discovery | New permission-filtered registry supplied by modules; do not reflect every EF property into editable metadata. |
| Email/templates, arbitrary webhooks | Provider/integration work required; unavailable actions must remain unavailable. |
| Parallelism, subflows, AI drafting | Later phases; explicit graph/runtime extensions, not simulated delivery. |

Main architectural changes: reusable domain commands for actions; durable event capture; background authorization; a metadata contract; a Workflow module and worker composition. Each fills a demonstrated gap. No message broker, second database, sibling checkout dependency or new frontend framework is initially needed.

## 3. Proposed architecture and module boundaries

Add `Nexora.Modules.Workflow`, with its own `workflow` schema, APIs, validation, interpreter and persistence. It references BuildingBlocks only. API and Worker composition roots register Workflow plus selected module adapters. Action implementations belong to their owning modules and implement BuildingBlocks interfaces; Workflow discovers handlers through DI rather than referencing those modules.

Conceptual contracts (names proposed, not currently present):

- `IWorkflowMetadataContributor`: entity keys, typed fields, allowed operators, choices, relationships, query capabilities and commands.
- `IWorkflowRecordReader` / `IWorkflowQueryProvider`: tenant-scoped, permission-aware reads; bounded projected results and cursor paging.
- `IWorkflowActionHandler`: stable action key/version, input/output schema, required permissions, validation, execution and retry/idempotency classification.
- `IWorkflowTriggerMatcher`: event/schedule/manual trigger applicability.
- `IWorkflowValueResolver` / `IWorkflowConditionEvaluator`: typed, deterministic values and bounded predicate evaluation.
- `IWorkflowExecutionAuthorizer`: validates principal, workflow grant, tenant, current permissions and action/record scope at execution/resumption.
- `IDomainEventOutbox`: events appended in the same transaction as their domain change; owned by the publishing module through a shared storage convention.

Flow: authenticated command → owning module transaction + outbox → dispatcher → matching immutable workflow version → run + work item → worker lease → authorized action → checkpoint/history → subsequent work or persisted wait.

Keep definition interpretation independent of UI layout. No script evaluation, unrestricted SQL, arbitrary reflection updates or user-provided C#/JavaScript.

## 4. Database and data model

All workflow rows have TenantId; tenant-qualified foreign keys and access filters apply. Use UTC timestamps and existing concurrency conventions. JSON is bounded, schema-versioned and validated before storage/publication. Secrets are references to approved secret storage, never definition JSON.

| Table | Proposed contents and constraints |
| --- | --- |
| Definitions | Id, TenantId, Name, Description, entity key, archived flag, active published version reference, created/updated actors/time, Version concurrency token. |
| Versions | DefinitionId, sequential version number, draft/published state, schema version, canonical DefinitionJson, content hash, validated metadata/handler revision, publisher/time. Unique tenant/definition/version. Published versions immutable. Separate UI layout JSON. |
| ExecutionGrants | VersionId, publisher/principal identity, approved permissions and scope, expiry/revocation state. Cannot grant rights publisher lacks. |
| Runs | VersionId, trigger event/manual request key, target entity/id, status, principal/grant, correlation/causation/root ids, start/end/due time, bounded context, lease/checkpoint version. Unique tenant/version/trigger occurrence/target prevents duplicate matching. |
| Tokens | RunId, current node, branch/loop scope, continuation, bounded context and state; prepares for later fork/join without one mutable current-node pointer. |
| WorkItems | Run/token/node/attempt, available time, lease owner/expiry, priority, state and concurrency. Index state/available time; tenant/run index. |
| StepAttempts | Append-only node/action/handler version, attempt, outcome, times, redacted input/output references, created record references, error category/code and correlation. Unique run/token/node occurrence/attempt. |
| ActionReceipts | Stable logical action key, resulting record/provider reference and outcome. Unique tenant/action key. Domain adapters also enforce this key atomically in their own transaction. |
| Schedules | VersionId, timezone, recurrence/anchor descriptor, next UTC occurrence, last occurrence key, concurrency; explicitly handle DST. |
| Outbox (per owning module) | Tenant/event id, type/version, entity/id, selected previous/current values, actor/time, correlation/causation, dispatch status/lease. Index pending/available time. Persist within the domain transaction. |

Do not use hard deletes for published definitions with history. Archive definition, retain versions/runs according to an agreed retention policy. Add migration/model-drift tests before applying any new schema to the shared database.

## 5. Workflow definition model

Use a typed graph with stable node ids and labeled edges, rather than a flat steps array that cannot represent branches/resumes. Example contract draft:

```json
{
  "schemaVersion": 1,
  "name": "Review a new record",
  "entityKey": "module.entity",
  "trigger": { "kind": "record.created", "version": 1 },
  "entryNodeId": "check",
  "variables": [{ "key": "createdTaskId", "type": "recordReference" }],
  "nodes": [
    { "id": "check", "kind": "condition", "predicate": {
      "all": [{ "left": { "source": "trigger.current", "path": "status" },
        "operator": "equals", "right": { "source": "literal", "type": "string", "value": "Pending" } }]
    } },
    { "id": "task", "kind": "action", "actionKey": "work.task.create", "actionVersion": 1,
      "inputs": { "title": { "source": "literal", "type": "string", "value": "Review record" } },
      "outputs": { "taskId": "createdTaskId" } },
    { "id": "done", "kind": "end" }
  ],
  "edges": [
    { "from": "check", "port": "true", "to": "task" },
    { "from": "check", "port": "false", "to": "done" },
    { "from": "task", "port": "success", "to": "done" }
  ]
}
```

This illustrates the proposed shape, not an executable existing API. `module.entity` is deliberately a registry key placeholder, not a built-in entity.

Typed value references: literal, trigger.current/previous, current record, related record, triggering user, execution principal, owner, clock, variable and previous node output. Define snapshot versus live read explicitly: trigger data is captured; `record.current` is reloaded on resume. Missing previous values mean unavailable, not silently false/empty. Time-dependent expressions use an explicit evaluation clock; a retry reuses its logical attempt's inputs.

Predicates support all/any nested groups and the requested equality, text, ordering, empty, set-membership and changed/from/to operators where field types permit. Define null, case sensitivity, decimal precision and UTC/date-only semantics. Allow only metadata-declared relationships with a depth cap. Bound expression length, nesting and computation; no arbitrary code.

Validation rejects unknown kinds/handlers/fields, duplicate ids, dangling edges, unreachable nodes, invalid port counts/types, missing required inputs, undeclared variables, incompatible outputs, unauthorized commands and unsafe cycles. Loop edges belong to a bounded structured loop construct. Ordinary cycles are invalid. Each supported definition schema and action version has explicit compatibility rules.

## 6. Lifecycle and execution semantics

Definition: create draft → server validate → publish immutable version → activate version → deactivate/archive. Editing starts a new draft with optimistic concurrency, never mutates a published version. Validate again at publish against current metadata and publisher permissions. Activation selects one published version atomically.

Run: queued → running → waiting or queued → completed / failed / cancelled. Waiting persists a due time/continuation and releases the worker. A version remains pinned even when a newer version activates. Deactivation prevents new matches; existing runs continue unless explicitly cancelled. Permission/grant revocation stops affected work before its next action. Cancellation is cooperative and cannot undo an already committed action; compensating business commands require explicit support.

Each step has a durable logical occurrence id. Persist branch outcomes and committed action receipts before advancing; recover a crash by reconciling receipts rather than replaying an unknown side effect. No global transaction spans a complete workflow or external network call.

## 7. Triggers

Start with one registered record-created event and manual single-record execution; prove the contract through a Work task adapter without making the core Work-specific. Event envelope includes TenantId, EventId, event type/schema version, entity/id, UTC occurrence, actor, correlation/causation/root ids and a bounded allowlisted before/after projection.

Incrementally add updated/deleted/status/field/assignment/related-change events at owning command boundaries. Deleted record conditions rely on an authorized captured tombstone; current reads cannot magically retrieve the deleted entity. Suppress no-op field changes and repeated matches. Do not start workflows synchronously from SaveChanges callbacks.

Schedules need recurrence timezone, missed-occurrence policy (initially coalesce one overdue occurrence), DST policy and unique occurrence ids. Relative-to-record schedules reschedule/cancel when their anchor changes or record disappears. Bulk/query manual starts require a capped selection preview and per-record authorization; query results use bounded pagination and an explicit snapshot strategy.

External trigger delivery is a later signed, replay-protected, tenant-bound API with rate limits, dedupe keys and integration grants. Never trust an incoming TenantId header as authorization.

## 8. Actions and domain integration

Register actions explicitly. Metadata labels unsupported actions as unavailable; never imply every entity supports create/update/delete. Domain status transitions and financial posting are dedicated commands, not editable status fields.

The first concrete adapter should extract Work's task-create operation into a shared module service used by both HTTP and workflow calls. Preserve title/description bounds, priority/status rules, valid active tenant assignee/team/Contact, WorkHistory and task-linked notification. A logical action id is unique in the Work transaction so crash/retry cannot create another task. Add a recipient-aware notification command only when its current task/application linkage is correctly represented.

Later adapters can expose permitted CRM field updates, assignment and related updates, membership commands, enrollment commands and finance commands individually. Attribute calculation continues using existing rules. Query/retrieve/calculate/value-copy nodes consume authorized typed data and bounded outputs.

Email requires durable provider dispatch, template lifecycle and verified delivery; initial unsupported. Integration actions require registered destinations, approved credentials, timeout/rate-limit controls and SSRF protections. No arbitrary URL with ambient cookies or platform credentials. Retries require provider idempotency or reconciliation. Payment-like actions never auto-retry ambiguous outcomes.

Expose output schemas such as taskId/invoiceId; store minimum references, not full sensitive records. Stop, complete and cancel are engine controls. Subworkflow invocation later requires ancestry/depth limits and explicit authorization/version pinning.

## 9. Background processing, reliability and scaling

Extend the existing Worker host with DI, module configuration, database access, tenant identity scopes, dispatcher and workflow processors. Keep ContentScheduler separate initially. SQL queues suit current scale; broker introduction is optional only after measured contention/backlog demands it.

Use atomic claim with lease expiry and concurrency checks (SQL Server lock/update pattern tested on SQL Server), small batches and bounded parallelism. Renew leases during bounded work. Recover expired leases; never hold DB locks during HTTP calls. Multiple API/worker instances must safely coexist. Checkpoint before waiting and resume from persisted work after restart.

Use at-least-once delivery with deterministic deduplication. Domain outbox and its domain mutation are one transaction; Workflow run/work creation is one Workflow transaction. Cross-module atomicity is not assumed. Source-side idempotency receipts close the crash window after a domain commit and before a Workflow checkpoint.

Transient failure: bounded exponential backoff + jitter, max attempts and deadline. Validation/permission failure: stop without retry. Support skip/continue/escalate only on explicitly configured failure edges and compatible actions. Ambiguous external outcome: needs-reconciliation failure, not blind replay. Operators retry the failed logical action using its original idempotency key, not restart completed steps.

Proposed initial configurable caps: 100 nodes/definition, predicate depth 8, 5 relationship hops, 1,000 loop items/run, 10,000 steps/run, workflow ancestry 8, 256 KB definition/context envelopes, 5 attempts, 30-second external request timeout, per-tenant concurrency quota. Validate these defaults with real workloads; they are proposals, not existing guarantees. Structured bounded loops yield work between items. Parallel forks later use token/join state and merge rules for variables.

## 10. Security model

Propose separate permissions: workflow.read, create, edit, publish, activate, delete, execute, history.read, retry and cancel. Add these to the existing Identity permission catalogue and security-group editor in Phase 1. UI visibility never substitutes for API enforcement. Module/record read and command permissions apply in addition to workflow permission.

Manual execution uses the caller's current tenant-scoped permissions. Event/scheduled runs use an explicitly approved execution grant bound to the published version and an active tenant user/principal. Start with the publisher's approved delegated scope, intersected with their current permissions; never silently run as PlatformAdministrator. A revoked/disabled publisher or expired grant stops execution. A future service principal needs explicit tenancy/grant lifecycle, not synthetic unrestricted claims.

Resolve identity in the worker through a dedicated tenant scope and current authorization directory; no arbitrary HttpContext injection or IgnoreQueryFilters inside handlers. A privileged queue dispatcher may discover tenants but cannot act on their records. Read metadata/history only if both workflow and referenced record permissions allow it. CSRF on cookie-authenticated mutations; external endpoints use separate signed credentials. Secrets, passwords and access tokens never appear in payloads, node outputs, logs or exports.

For the initial plan workspace only, use current identity.manage access. Replace with workflow.read when the actual module/permission catalogue exists. This is not an engine execution grant.

## 11. Audit and monitoring

Keep domain history; add Workflow version/lifecycle audit and append-only run/step attempts. Correlate a domain mutation to workflow run, pinned version, trigger, logical action, actor/execution principal and trace. Record condition outcome and safe field references; redact personal data and never store unrestricted payload dumps.

Monitoring lists queued/running/waiting/completed/failed/cancelled runs with current node, due time, durations, attempts, record references and error category. Detail reconstructs an ordered timeline and links to accessible domain records. Expose authorized retry/cancel (resume only for a defined wait/manual gate) with concurrency checks. “Queued email” and “delivered email” remain different states.

Metrics: queue depth/oldest age, lease recovery, per-action latency/failure, retry/dead-letter counts, waiting due lag and tenant quotas. Structured log fields use correlation ids; tenant ids and run ids belong in logs rather than high-cardinality metric labels. Retention/export policy must cover PII and keep references meaningful without exposing deleted records.

## 12. API design

Proposed `/api/v1/workflows` endpoints, all tenant-scoped and policy-enforced:

| API | Purpose |
| --- | --- |
| GET /metadata | Permission-filtered entities, fields, operators, relationships, action versions and actual supported capabilities. |
| GET/POST /definitions | Paged listing / create draft. |
| GET /definitions/{id} | Definition/version summaries. |
| POST /definitions/{id}/drafts | Create new draft from a selected version. |
| GET/PUT /definitions/{id}/versions/{version} | Read / update draft with concurrency token. |
| POST .../validate | Structured errors with node/path/code; no side effects. |
| POST .../publish | Revalidate, freeze and audit; concurrency required. |
| POST /definitions/{id}/activate or /deactivate | Atomically set/clear active version, audit. |
| DELETE /definitions/{id} | Delete unpublished unused draft or archive retained definition according to policy. |
| POST /definitions/{id}/execute | Authorized target + required client idempotency key; return 202 run reference. |
| POST .../execution-preview | Bounded selection/permission preview; no effects. Later add bulk/query execution. |
| GET /runs and /runs/{id} | Paged monitoring/detail/step timeline with record-aware access. |
| POST /runs/{id}/retry or /cancel | Guarded operator action, audit and concurrency. |
| POST .../test | Later, sandbox/dry-run plan; never invoke production writes or real external delivery. |

Avoid payloads that let callers override TenantId, runtime principal, internal status or action receipts. Return existing problem/error conventions. Request bounds and rate limits apply. Signed integration trigger APIs arrive later, with an explicit versioned envelope.

## 13. Designer architecture

Dedicated `/workflow-designer` route outside the CRM shell, authenticated, opens in a separate window from **Admin → WFL Designer**. Responsive workspace, keyboard-accessible controls, visible status and readable hover colors. Keep the existing Automations navigation.

The first entry workspace shows the assessment and ordered phases. It has no simulated save/publish/run actions. The brief places the production visual designer after a stable engine: build an engine-backed list/version editor first, then a graph canvas and configuration inspector driven by GET metadata. Persist drafts on the server with tenant permissions and optimistic concurrency, not browser storage presented as production storage.

Canvas nodes: trigger, condition, action, wait, branch, query, bounded loop and end, enabled only when supported. Node settings use typed metadata; node outputs become compatible value sources. Store position/layout separately from execution graph. Shared server validation is authoritative; browser validation gives immediate feedback. Show branch labels and per-node issues. Import/export is schema-validated and secrets-free in the later phase.

Test mode starts with a read-only execution plan with synthetic inputs and stubbed external actions. Publishing/activation are distinct authorized actions with impact/version summary. Run history overlays the pinned definition; edited drafts cannot alter a historical timeline. Do not hard-code Contact/Application fields into the UI.

## 14. Implementation phases and exit criteria

1. **Assessment and entry point (this delivery):** checked repository assessment, data/API/runtime/security plan and separate-window WFL entry workspace. Engine and visual editor remain future work.
2. **Phase 1A — persistence and discovery:** Workflow module/schema/migrations, permissions, immutable published versions, draft validation, initial module metadata, tenant/security tests. Exit: two tenants cannot read/write each other's definitions; publish is immutable/concurrent-safe; unsupported capabilities are rejected.
3. **Phase 1B — reliable vertical slice:** one real record event + manual trigger, transactional module outbox, Work task command adapter, asynchronous leased worker, nested basic conditions, sequential action/end, run/step history and atomic idempotency. Exit: demonstrate commit → event → task → timeline; worker crash and duplicate dispatch create one task; revoked permission blocks execution. Register a second module trigger/read adapter before calling the platform cross-module reusable.
4. **Phase 2 — advanced processing:** persistent waits, recurring/relative schedules, live/previous values, variables/outputs, related queries, bounded loops, failure edges, provider-backed notifications/email and approved external actions. Exit: restart during wait, DST/missed schedule, query pagination, limits and ambiguous delivery tests pass.
5. **Phase 3 — production visual designer:** metadata-driven canvas/inspector, server draft save, validation, sandbox testing, publish/version management and monitoring navigation. Exit: keyboard and mobile use, denied actions hidden/rejected, conflict recovery, round-trip definition and historical version display verified.
6. **Phase 4 — enterprise:** fork/join, subflows, SDK contracts/compatibility, templates/import/export, advanced monitoring and throughput work justified by measurements. Exit: join/variable/recursion correctness, upgraded-handler compatibility and bounded tenant-fair load demonstrated.
7. **Phase 5 — AI drafting:** assistant emits schema-valid drafts via the same API, validation and user review. Exit: no direct activation or expanded privileges, generated fields/actions are registry-backed, audit identifies AI assistance.

Dependencies matter: do not expose arbitrary actions before adapters and authorization, publication before immutable definitions, or retry before idempotency. Phase completion means its exit evidence exists, not just routes/screens.

## 15. Testing strategy and acceptance evidence

- Unit: predicate typing/null/change semantics, value resolution, graph validation, action registry compatibility, variable scope and bounded loop logic.
- SQL Server integration: tenant composite keys, immutable versions, concurrent publish/activation, unique event/run/action keys, atomic queue claim, expired leases, source outbox rollback/commit and idempotent domain command transactions. In-memory tests cannot prove these properties.
- API/security: separate workflow permissions, source-record permissions, current group revocation, inactive publisher, cross-tenant references, CSRF, principal spoofing and inaccessible history. Protect existing system admin constraints.
- Runtime fault injection: crash before/after action commit and checkpoint, redelivery, two workers, wait/restart, schedule/DST, cancellation during I/O, transient/permanent/ambiguous failures and stale version grants.
- Existing business regression: membership uniqueness/decisions/quotes, portal entitlements/evidence, finance posting/allocation integrity, Work assignment/history/recipient filters and content publication readiness. Extracted services must retain current results.
- Frontend: actual new-window entry, authentication/access guards, metadata-driven settings, graph round-trip, accessible configuration, validation paths, publish permissions/conflicts and run timeline. Do not substitute mocked UI success for worker/database evidence.
- Operations: queue/backlog/tenant fairness load tests at measured expected scale; retention/redaction, monitoring and recovery documentation. EF has-pending-model-changes after schema changes, appropriate module/architecture tests, web typecheck/lint/build.

Business examples are acceptance scenarios assembled from registered capabilities, never hard-coded engine branches. SAT/lead-specific behavior or a generic activity subsystem is not assumed present. Payment/email/provider scenarios remain deferred until their dependencies actually exist.

## Evidence index and current delivery boundary

Inspected sources include API and Worker Program/Worker, BuildingBlocks TenantDbContext/AutomationConfiguration/Reporting contracts, WorkModule/WorkEntities/WorkDbContext, Membership operations and portal notifications, ContentScheduler/ContentModule, IdentityModule/AdministrationEndpoints/claims factory/directory, API ReportingEndpoints, architecture tests, frontend AppShell/session/attribute maps and the product architecture master plan.

The advanced implementation now includes transactional record-event outboxes, manual/bulk and signed external triggers, interval/daily/weekly/once/relative schedules, nested predicates, authorized relationships and bounded queries, dynamic expressions, variables and action outputs, persistent waits, structured loops, durable parallel branch tokens and joins, pinned subworkflows and configurable failure routes. Parallel branches progress independently through the worker; they do not execute simultaneous threads within one run.

The separate Admin → WFL Designer window supports canvas editing, metadata-driven inspectors and record pickers, server draft persistence, authoritative validation, immutable publication and activation, JSON import/export, templates, read-only test previews, execution monitoring, resolved step inputs/outputs, cancellation and guarded retry. Workflows and domain actions use current tenant/user permissions. Module-owned receipts preserve committed effects across worker checkpoint recovery.

Registered domain actions cover CRM create/update/assign/archive, Work task creation/update and user/team notifications, membership application decisions/member transitions, finance order posting, event enrollment and community membership. Read/event sources also cover Content. Unsupported domain commands are not exposed as arbitrary field writes. Future modules extend the BuildingBlocks entity/action contracts and register their own guarded commands without a dependency on the Workflow module.

Connections support encrypted SMTP, approved HTTPS APIs and OpenAI-compatible AI providers. Email templates resolve dynamic values before delivery. Provider calls are bounded; HTTPS destinations are checked and pinned against private-network access. Ambiguous delivery stops automatic replay until an authorized operator confirms non-delivery. AI returns a draft for review and cannot publish or activate it.

Verification: 19 workflow API/domain/delivery tests passed; the four failures in the earlier broad regression run were corrected and their targeted retest passed. Two isolated SQL Server migration/lease tests and two architecture checks passed. The current local API builds cleanly, its development database migrations applied, and TypeScript/ESLint passed. Live browser checks passed separate-window entry, JSON round-trip, read-only preview, publication/activation, a SQL-backed loop/parallel/join run with step history, mobile layout, anonymous access protection, and connector/email-template create/edit/delete. Temporary verification workflows were archived by exact ID and temporary settings deleted by exact ID, retaining workflow audit history without changing CRM business records or contacting external providers.

Verification boundaries: provider delivery/reconciliation is covered with a controlled transport; live SMTP/HTTPS/AI accounts still require administrator configuration and provider acceptance checks. Preview plans actions and child workflow calls without performing effects; external outputs/generated IDs are placeholders and waits are not elapsed. The workflow BackgroundService runs in the existing API host. Production throughput, retention policy and deployment acceptance remain operational work, not inferred from local checks.

## Business operations pack — 2 October 2026

The first business catalogue delivery adds 13 configurable templates, durable assigned human approvals with current permission checks and audited reasons, deadline/timeout routing, business-state waits, module-owned read-only inspections and an Operations screen with approval inbox and exception queue. Read-only preview now evaluates registered pure inspections using current data; domain mutations and external delivery remain planned only. See [Workflow operations template pack](WORKFLOW_OPERATIONS_TEMPLATE_PACK.md) for delivered behavior and remaining catalogue work.

The broad API regression run passed 169 tests with eight skipped and no failures. Final follow-up changes passed all 38 focused workflow tests. Two isolated SQL Server migration/lease checks and two architecture checks passed; TypeScript, ESLint and the final local API build passed. Live browser verification passed template generation, approval configuration, an actual SQL-backed human decision and completed worker audit, exception queue rendering and mobile layout. This evidence closes the earlier broad-regression failure boundary; skipped checks and production/provider acceptance remain explicit limitations.
