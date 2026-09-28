# Personal portal application drafts and submission

> Current VS Code setup (24 September): use **CRM**, **Portal** or **CMS**. [Local testing instructions](../LOCAL-TESTING.md) supersede the older launch names and LocalDB guidance below.


**Latest follow-up:** Receipt/detail pages, draft discard and staff access management are now implemented; the expanded approval/rejection browser journeys passed. See [Stage 13](STAGE-13-PORTAL-SELF-SERVICE.md) for current scope and verification. The earlier interrupted-run notes below are retained as history.

Delivered locally on 21 September 2026. Members can save and reopen personal application drafts, review the current quoted total and membership terms, accept those terms and submit to the existing staff decision workflow. Submission does not take payment or activate membership.

## Shared workflow and access boundaries

`MembershipApplicationCommand` now contains the existing application validation and creation used by both staff and portal callers. Staff behaviour, quote snapshots, history and attribute handling are retained. Portal callers always supply the Contact resolved by the live portal binding; they cannot choose a Contact, Account, applicant role, application type, status or quoted rate. The portal submits only individual applications.

Products are closed to online applications by default. Staff with `membership.manage` can configure availability and member-facing terms under Membership > Products > Portal application settings. The UI uses PUT `/api/v1/membership/products/{id}/portal` with `{version,enabled,terms}`. Enabling requires nonempty member-facing terms, bounded to 4000 characters. The product must also be active. Saves retain optimistic concurrency and refresh the shared product data.

Drafts live in `membership.PortalApplicationDrafts`, retain tenant isolation and optimistic concurrency, and store the quoted total, currency, terms and product/policy versions. The member supplies an opaque draft ID and a restricted field set; ownership always comes from the session. One unsubmitted draft per Contact/product is enforced by a filtered unique index, with a limit of 20 active drafts per Contact. Notes are limited to 4000 characters and dates to the next two years. Draft deletion is not implemented in this slice.

Saving refreshes the quote and terms. Changes to the product or policy require saving and reviewing again before submission. Submission requires terms acceptance, rejects an existing pending application or active membership for that product, and atomically saves the application, history and draft's application pointer. Repeating a successful submission returns its original application ID. Concurrent updates use the draft version; a losing SaveChanges transaction rolls back its application insert. The automated retry check is sequential; no SQL Server parallel-submit load test is claimed.

Submitted draft snapshots remain stored as acceptance evidence. The own-drafts list shows only unsubmitted drafts. Members see application status through the existing restricted overview; staff decision reasons remain private. Staff approval remains responsible for creating membership.

## Routes

- GET `/api/v1/portal/application-products`
- GET `/api/v1/portal/drafts`
- PUT `/api/v1/portal/drafts/{id}` with `{version,productId,startsAtUtc,notes}`; use null version for a new draft.
- POST `/api/v1/portal/drafts/{id}/submit` with `{version,termsAccepted}`.

All routes use the existing portal scheme, live access checks, no-store responses and antiforgery protection on writes. The local-development-only identity restriction remains unchanged.

## Local preview and verification

Apply `PortalApplicationDrafts` using the Membership EF design-time factory. The local `NexoraDev` migration is applied, the API preview is running the `PortalApply` build on port 5098 and the frontend remains at `http://localhost:3018/portal`. The generated demo credentials remain in ignored `artifacts/portal-local/DEMO-ACCESS.md`. `Demo portal application` is enabled with explicitly local demo terms and a zero quote. A saved, unsubmitted demo draft is available for review.

VS Code **Portal (API + Web)** continues to build and run current source. For future standalone preview restarts, build the `Portal` configuration before invoking `Start-WebsiteLocal.ps1 -Portal`.

Validation:

- Full API suite: 91 passed, 4 opt-in SQL Server tests skipped.
- SQL Server migration replay and pending-model check passed separately.
- Desktop/mobile Playwright journey passed: save, refresh, reopen, accept terms, submit, see status, shared-profile update and access revocation.
- API tests cover changed terms, stale drafts, mandatory acceptance, identical retry result, one created application, other-member draft isolation and rejected identity-field injection. Existing staff application journey tests pass after the shared-command extraction.
- TypeScript and targeted portal/test ESLint passed. Live preview desktop/mobile draft-review screenshots were visually reviewed; the draft was left saved for the user.
- Build reports the existing ASP.NET route-analyzer AD0001 warnings; no compilation errors. Production web build and production provider integration remain outside this local slice.

## Remaining

### Staff settings follow-up, 21 September 2026

The staff product settings UI is implemented and copied to the local preview. Browser checks confirmed saving enabled status and terms and retaining them after reload. The existing staff decision form is reused. The expanded Playwright suite covers approval and rejection, membership creation only on approval, private decision-note exclusion, shared profile updates and revocation. Its final run remains pending: earlier runs exposed test locator and stale-page issues, now corrected; after resuming under the restricted Windows account, SQL LocalDB could not create its automatic instance, so the isolated API exited before browser execution. This is not a passing end-to-end result. The current PortalTest build, TypeScript check and targeted ESLint pass.

Resume verification with `node scripts/Test-Website.mjs --portal` in an environment with working SQL LocalDB access.

Draft discard and dedicated application receipt/detail views are delivered in Stage 13. Notification delivery, production identity and recovery remain. Organisation applications require explicit representative grants. Payment collection needs an agreed provider and separate payment workflow.
