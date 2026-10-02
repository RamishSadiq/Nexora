# Workflow operations delivery — 2 October 2026

This is the first business workflow pack from the supplied catalogue. Open Admin → WFL Designer → Operations. Generate a configurable draft, review it, save and validate it, publish a version and explicitly activate it. Generating a template never starts a workflow.

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

Reviewer, escalation user, reminder/decision deadline, renewal lead time and lapse grace are configurable. Optional SMTP delivery requires an approved active connector and recipient address/expression. Without it, communication steps create explicitly labelled internal staff follow-up notifications. Application decisions also retain the existing eligible portal-recipient notifications.

## New platform building blocks

- **Human approval:** durable tenant-scoped request, user/team assignment, current source and decision permissions, mandatory decision reason, optimistic concurrency, explicit approved/rejected/timeout routes, deadline escalation and append-only decision history. Replaying a decision does not authorize another action. Cancelling a run closes pending approvals.
- **Wait for business state:** persist a deadline and recheck current record, another permitted record or an explicitly read-only domain inspection. Completion and timeout have separate destinations. Polling does not append duplicate step history or replay domain mutations.
- **Read-only inspections:** application/contact/organisation prerequisites and existing active membership, current renewal quote/review eligibility, and actual invoice balance. Inspections can execute in read-only preview; mutating actions remain planned only.
- **Operations screen:** template configuration, assigned approval inbox and actionable failed/overdue-processing queue. Existing guarded retry/cancel and delivery-reconciliation controls remain in use.

One active membership per Contact or organisation stays enforced by the membership command and database constraints. Pending applications across different offerings remain allowed. Main Contact/Main Account flags never create portal access grants. Organisation authority remains an explicit human review; a populated role label is not proof of authority.

## Remaining catalogue work

This pack does not claim every requested workflow has been delivered. Relationship-change/main-contact handover adapters, richer duplicate/data-quality checks, dedicated portal access-review/change actions, registration/reminder/post-event adapters, wider community actions and Content approval-to-publication command wiring remain separate module work.

Payment-dependent completion needs an explicit verified-obligation mapping. Refund/credit/reconciliation workflow adapters must retain the existing Finance commands and external-provider boundaries; recording a refund is not executing a payment-provider refund. CPD, qualifications, elections and Direct Debit execution remain future module/integration requirements as requested.

Live SMTP/AI/provider acceptance, production deployment, throughput and retention verification remain operational boundaries. Local browser verification uses an exact temporary manual approval workflow ending without business mutations; it is archived afterward with its audit retained.

## Verification

The full API regression run passed 169 tests with eight skipped and no failures. After the final application-acknowledgement and read-only-preview changes, all 38 focused workflow engine, advanced, delivery and operations tests passed. The two SQL Server checks passed separately: shared-catalog migrations replay without pending model changes, and competing workflow workers claim one lease. Two architecture checks, frontend TypeScript and ESLint passed. The latest local API build completed with no warnings or errors and its development database is ready.

Live browser checks against the final build passed configurable template generation, the approval inspector, a SQL-backed assigned approval and explicit decision, completed worker execution with decision and approval history, exception queue rendering and mobile layout without browser errors. The browser test now explicitly polls the completed run before checking its audit, avoiding an early asynchronous assertion. Exact temporary verification definitions were archived and unfinished verification runs cancelled, with audit retained. No CRM business records or external providers were changed by these browser checks.

Evidence logs are in `artifacts/workflow-operations-full-tests.log`, `workflow-operations-final-focused.log`, `workflow-operations-live.log`, `workflow-operations-architecture.log`, `workflow-operations-typescript.log` and `workflow-operations-eslint.log`.
