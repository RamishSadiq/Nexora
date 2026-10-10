# Workflow operations catalogue — 3 October 2026

The supplied catalogue contains 29 workflow types. Open Admin → WFL Designer → Operations → Save catalogue drafts to persist all 29 primary definitions and five variants. Installation creates inactive drafts and preserves existing definitions and edits. Review configuration, validate, publish and explicitly activate each workflow when ready. Required variables block publication until configured.

## Delivered templates

| Template | Trigger and behavior |
| --- | --- |
| Application review and decision | New submitted application: acknowledge receipt through configured communication, inspect prerequisites, assign review work, require a named human decision, recheck approval prerequisites and invoke the existing approve/reject command with the recorded reason. |
| Missing-information follow-up | New application: inspect requirements, request follow-up work, wait for current prerequisites to pass, then remind/escalate on timeout while stopping reminders after a decision. |
| Member onboarding | New active membership: assign onboarding work and communicate next steps. Portal grants remain governed by existing entitlement rules. |
| Renewal preparation | Relative to current expiry: inspect renewal policy/review requirements and assign preparation or exception work. |
| Renewal reminders | Relative to expiry: communicate, wait, then stop if the term changed or membership was cancelled; recheck eligibility before the next reminder. |
| Expiry/lapse | Relative to expiry plus configurable grace: recheck current expiry/status and invoke the guarded lapse command. |
| Task escalation | Relative to due date: notify the current assignee, wait for done/cancelled state, then create assigned escalation work on timeout. |
| Unpaid-invoice follow-up | Inspect actual invoice balance from credits/allocations; remind, wait for balance clearance, then escalate. No synthetic paid flag or money movement. |
| Renewal completion | Inspect existing renewal requirements, obtain explicit human approval and invoke the existing guarded renewal command. |
| Cancellation review | Manual request: obtain approval/reason, invoke cancellation and communicate follow-up. |
| Reinstatement review | Manual request: obtain approval/reason and invoke reinstatement, preserving existing renewal/payment/compliance and uniqueness rules. |
| New Contact follow-up | Record created: use the CRM update/assignment command and create assigned follow-up work. |
| Invoice preparation/posting | Manual request: require a finance-authorized decision and post the order through the Finance command. |
| Application intake | New submitted application: assign intake work and acknowledge receipt. |
| Organisation application checks | Inspect application prerequisites and existing active membership, then require staff review of organisation authority. |
| Data-quality review | Daily Contact or Account inspection identifies missing details/owner and possible duplicates for staff review; never automatically merges records. |
| Relationship-change review | Inspect current relationship and explicit portal delegations, notify the current owner and require a human confirmation. |
| Main-contact handover | Review the current relationship and access exceptions, then explicitly approve transfer of one configured open Contact task to the current Account owner. |
| Human approval | Manual CRM decision with authorized reviewer, reason, timeout and escalation. |
| Portal-access review | Inspect membership and explicit login/delegation state, raising staff review without changing access grants. |
| Payment-dependent membership completion | Explicit approval to renew the pinned current term against a linked, fully allocated and reconciled matching renewal invoice. Existing membership policy and uniqueness checks remain enforced. |
| Payment exceptions | Inspect recorded receipt reconciliation state, create finance review work and explicitly approve a matching statement reconciliation. |
| Refund/credit approval | Finance-authorized approval invokes the existing credit or refund-recording command with amount, reference and reason. |
| Event registration | Manual enrollment for a configured Contact through existing capacity, waitlist and course rules. |
| Event reminders and changes | Relative event reminder or event-change variant creates participant Contact tasks; cancellation includes registrations cancelled by the event command. Configured emails respect consent. |
| Post-event follow-up | After a published event ends, create participant follow-up tasks and configured consent-aware communications. |
| Community membership | Human-approved join or leave through the existing community membership commands. |
| Content approval and publication | Preview the exact saved draft revision, explicitly approve publication and reject a changed draft rather than publishing unreviewed content. |
| Operational exception reporting | One scheduled tenant report counts overdue applications, memberships approaching expiry, outstanding invoices and failed/overdue workflow processing, creating review work when exceptions exist. |

Reviewer, escalation user, reminder/decision deadline, renewal lead time and lapse grace are configurable. Optional SMTP delivery requires an approved active connector and recipient address/expression. Without it, communication steps create explicitly labelled internal staff follow-up notifications. Application decisions also retain the existing eligible portal-recipient notifications.

## New platform building blocks

- **Human approval:** durable tenant-scoped request, user/team assignment, current source and decision permissions, mandatory decision reason, optimistic concurrency, explicit approved/rejected/timeout routes, deadline escalation and append-only decision history. Replaying a decision does not authorize another action. Cancelling a run closes pending approvals.
- **Wait for business state:** persist a deadline and recheck current record, another permitted record or an explicitly read-only domain inspection. Completion and timeout have separate destinations. Polling does not append duplicate step history or replay domain mutations.
- **Read-only inspections:** application/contact/organisation prerequisites and existing active membership, current renewal quote/review eligibility, and actual invoice balance. Inspections can execute in read-only preview; mutating actions remain planned only.
- **Operations screen:** template configuration, assigned approval inbox and actionable failed/overdue-processing queue. Existing guarded retry/cancel and delivery-reconciliation controls remain in use.

One active membership per Contact or organisation stays enforced by the membership command and database constraints. Pending applications across different offerings remain allowed. Main Contact/Main Account flags never create portal access grants. Organisation authority remains an explicit human review; a populated role label is not proof of authority.

## Configuration and operational boundaries

The five additional definitions cover Account follow-up, Account data-quality review, event changes/cancellation, refund recording and community leave. Required record IDs and financial references must be configured before publication. Main-contact handover transfers the selected open task after explicit approval; broader historical ownership changes remain an administrator decision. Portal reviews flag exceptions; access grants and revocations remain explicit administration actions.

Payment-dependent completion currently renews a current membership term using the linked renewal invoice, matching customer, currency and quote, with reconciled allocated payments and invoice reuse protection. Application payment obligations require a future explicit mapping. Payment exceptions inspect recorded receipts; uncertain external deliveries still require provider reconciliation. Recording a refund records a confirmed external refund and does not transfer provider funds. CPD, qualifications, elections and Direct Debit execution remain outside these 29 catalogue types.

Optional SMTP delivery needs a configured active connector and eligible consent. Internal staff follow-up remains available without SMTP. Reporting uses 7-day application and 30-day renewal thresholds, a 1,000-invoice bound and one tenant Contact as its schedule anchor. Tenants without a Contact do not receive scheduled reports.

Live SMTP/AI/provider acceptance, production deployment, throughput and retention verification remain operational boundaries. Local browser verification uses an exact temporary manual approval workflow ending without business mutations; it is archived afterward with its audit retained.

## Catalogue verification — 3 October 2026

All 63 focused workflow tests passed, including catalogue idempotency/edit preservation, required configuration, Finance balance guards and action receipts, reviewed-revision publication, reconciled-invoice renewal/reuse protection, community leave, cancelled-event recipients and read-only reporting. Frontend TypeScript, workflow ESLint and two architecture checks passed.

The final local API saved all 34 catalogue definitions, with 29 primary entries visible as saved and zero active versions. Live Edge checks passed catalogue installation/reinstallation, saved-workflow opening, desktop/mobile layout, an authorized SQL-backed approval and worker completion with audit, and the exact saved Content revision preview. Catalogue definitions were retained. Only the isolated verification approval definition was archived afterward. No live Content records or external providers were mutated during verification.

The full API regression passed 194 tests with zero failures and eight SQL-only skips. All eight skipped checks subsequently passed with the SQL connection enabled against isolated databases: shared module migrations and model parity, worker lease contention, CRM migration/tenant queries at 10,000 records, saved-view upgrade preservation, relationship history/uniqueness, membership concurrency, readiness and CMS upgrade preservation. Across these two runs, all 202 API checks passed. The local API build succeeded with zero errors; the framework route-handler analyzer emitted ten AD0001 warnings.

Evidence: `artifacts/workflow-catalog-final-focused.log`, `workflow-catalog-full-tests.log`, `workflow-catalog-sql-tests.log`, `workflow-catalog-live.log`, `workflow-catalog-install-result.json`, `workflow-catalog-approval-live.log`, `workflow-content-preview-live.log`, `workflow-catalog-typescript.log`, `workflow-catalog-eslint.log`, `workflow-catalog-architecture.log` and the corresponding screenshots.

## Earlier pack verification

The full API regression run passed 169 tests with eight skipped and no failures. After the final application-acknowledgement and read-only-preview changes, all 38 focused workflow engine, advanced, delivery and operations tests passed. The two SQL Server checks passed separately: shared-catalog migrations replay without pending model changes, and competing workflow workers claim one lease. Two architecture checks, frontend TypeScript and ESLint passed. The latest local API build completed with no warnings or errors and its development database is ready.

Live browser checks against the final build passed configurable template generation, the approval inspector, a SQL-backed assigned approval and explicit decision, completed worker execution with decision and approval history, exception queue rendering and mobile layout without browser errors. The browser test now explicitly polls the completed run before checking its audit, avoiding an early asynchronous assertion. Exact temporary verification definitions were archived and unfinished verification runs cancelled, with audit retained. No CRM business records or external providers were changed by these browser checks.

Evidence logs are in `artifacts/workflow-operations-full-tests.log`, `workflow-operations-final-focused.log`, `workflow-operations-live.log`, `workflow-operations-architecture.log`, `workflow-operations-typescript.log` and `workflow-operations-eslint.log`.
