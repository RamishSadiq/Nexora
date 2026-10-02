# Retained end-to-end review data — 1 October 2026

Status: in progress. This report will be updated with measured results.

## Scope and preservation

The user requested at least ten realistic records for each existing entity, retained in the local application for review, including Portal input, CMS publication, CRM verification, sales and review tasks. Use the configured local `NexoraDev` database and existing tenant. Prefix added records with `Journey 20261001`. Do not delete existing records, reset the database, change existing credentials, commit unrelated source changes, or send external messages.

Repository already contained extensive uncommitted CRM, administration, membership setup and workflow changes before this audit. Those changes are preserved. Runtime verification must distinguish the running binary from newer source.

## Inventory and execution plan

1. Check running API/frontends, schema and available routes.
2. Retain ten Contacts, Accounts and meaningful associated profile/address/contact/relationship records; provision ten local member identities.
3. Retain ten membership products, policies, Portal submissions and resulting memberships, reviews and certificates where supported.
4. Retain ten events, courses and exams with sessions, enrollments and exam results; publish ten dedicated CMS event-information pages.
5. Retain ten catalogue products, sales orders/lines, invoices, receipts, allocations, credits, refunds and reconciliations using local demonstration amounts.
6. Retain ten groups and committees, members, meetings, campaign drafts/audiences, funds, donations and pledges; ten review tasks and import batches.
7. Exercise real browser forms for Portal profiles and applications, CMS publishing, staff creation and cross-module review; check reload persistence and access boundaries.
8. Test Company Dash availability and its actual connection to this checkout without treating it as the CRM database.
9. Record failures, repair critical existing-feature breakage, run focused regression checks, and reconcile retained counts and record IDs.

Singleton configuration (tenant/site/Portal configuration), fixed permission catalogues and generated audit/history rows are not multiplied artificially. Missing or unavailable entities and paths are explicitly reported.

## Initial gaps confirmed in source

- Public self-registration and recovery are absent; local member accounts require staff provisioning.
- Portal event discovery/booking endpoints and pages are absent. Staff enrollment exists; it must not be reported as member self-booking.
- CMS publishes editorial content, with no existing event-to-CMS publication binding. Dedicated event information pages can be created and published, but are not live booking widgets.
- Finance records external receipts/refunds; it does not charge a payment provider. Local sample transactions are not real payments.
- Company Dash is a separate maintenance application, not a shared CRM data store.

## Evidence

The retained-data runner writes checkpointed record IDs and outcomes to `artifacts/retained-e2e-20261001/`. No passwords or authentication tokens are written to those artifacts.
