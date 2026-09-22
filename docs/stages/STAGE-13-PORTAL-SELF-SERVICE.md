# Member portal — personal self-service and staff access management

**Delivery date:** 21 September 2026.  
**Identity decision:** The product owner explicitly retained local development for this increment. Production registration, invitations and recovery remain deferred.

## Delivered scope

- Member navigation connects overview, memberships, applications and profile. The overview has an explicit refresh action and links each personal application to its own receipt.
- `/portal/applications/{id}` supports direct links, reload, current decision status, the original submitted quote and requested dates, accepted terms, member-submitted notes and resulting membership. A print action produces a browser-printable receipt; it is clearly not a payment receipt.
- Status copy explains awaiting review, approval and rejection. Internal staff decision/history notes are never used as member-facing messages.
- Unsubmitted drafts can be discarded after an explicit confirmation. Cancelling the confirmation preserves the draft; successful removal frees the product draft slot. A concurrency token prevents stale removal after another save or submission. Submitted evidence cannot be discarded.
- Any protected portal request that receives 401 removes the active member view and returns to sign-in. A normal anonymous first visit does not display a spurious expired-session error.
- Administration now provides a staff UI for finding an active CRM Contact, linking an existing account, creating a local member-only account and revoking access. The paged access list includes active/revoked and inactive-user/Contact states. Local account creation is shown only when the server advertises it as enabled; no invitation email is claimed or sent.

## API and ownership

| Route | Access and behavior |
| --- | --- |
| `GET /api/v1/portal/applications/{id}` | Live portal user/tenant/Contact binding; own personal applications only; unavailable, other-member, other-tenant and organisation records return 404. |
| `DELETE /api/v1/portal/drafts/{id}` | Portal CSRF and exact `{version}` body; own unsubmitted draft; stale version 409; submitted draft 400; unknown/foreign draft 404. |
| `GET /api/v1/admin/portal-access?page=1` | Both `identity.manage` and `crm.manage`; tenant-scoped, 20 rows/page; malformed/out-of-range page 400; no-store. |
| Existing grant/provision/revoke routes | Existing authorization and audit behavior reused by the staff UI. UI contact search additionally requires `crm.read`. |

No submitted Contact ID, email domain or CRM association grants portal rights. No new staff API access is granted to members. Shared Membership/CRM/Identity persistence is reused; no new schema or migrations are required for this increment.

Receipt quote/date values come from the stored application, not recalculated pricing. Accepted terms and submitted notes come only from the member's retained portal draft snapshot. Applications created through staff workflows can still show their safe status/quote, but do not expose staff-entered notes or invent an accepted-terms snapshot. Product names are current product labels. Current membership status/dates may differ from the original application after later staff actions; both are labelled separately.

Draft removal writes `application.draft.discarded` to Membership history in the same save as deletion. This internal audit is not exposed through receipt DTOs.

## Verification

Full API suite: **104 passed, none skipped**, using `PortalComplete` configuration and `NEXORA_TEST_SQLSERVER` targeting integrated-auth SQL Express with disposable test catalogs. No development database was migrated by these checks. TypeScript and targeted ESLint passed.

The two development-server browser journeys both passed on the resumed run (`artifacts/website-92e0dfb272a34c51b7e219e07cf00403`): approval and rejection, including staff UI provisioning/revocation. The original browser failure was corrected by giving the access selects explicit labels independent of their option text. Desktop overview and mobile receipt screenshots were visually reviewed; this led to a small layout correction placing the application workspace beneath the profile beside the membership panel.

The final **production frontend build passed**, including TypeScript and all route generation. Both journeys then **passed against `next start`** (2 tests, 30.1 seconds) in `artifacts/website-f887c70914f04d039b5b8d085dfad1ef`. Command: bundled Node running `scripts/Test-Website.mjs --portal --production`, with `NEXORA_TEST_SQLSERVER_INSTANCE=.\SQLEXPRESS`. Final desktop/mobile overview screenshots were visually reviewed. The runner removed its temporary API/web processes and disposable SQL catalog. This verifies a production frontend artifact with the local-development API adapter, not a production identity integration or public deployment.

Screenshots: `artifacts/portal-desktop.png`, `artifacts/portal-mobile.png`, `artifacts/portal-receipt-approve-mobile.png`, `artifacts/portal-receipt-reject-mobile.png`. These are generated verification artifacts, not application source.

Coverage:

- Snapshot stability after staff edits product terms; safe approval status and linked membership.
- Other-member, other-tenant and organisation receipt denial; absence of staff decision and applicant notes on legacy receipts.
- Draft owner, CSRF, payload allowlist, stale version, submitted-snapshot retention and reusable product slot.
- Tenant-isolated staff access listing, revoked history and member/staff cookie separation.
- Browser journeys for staff provisioning, discard/cancel, save/reopen/submit, receipt reload, approve/reject, private-note exclusion, shared profile updates and live revocation; desktop/mobile layout checks.

The browser runner now accepts `NEXORA_TEST_SQLSERVER_INSTANCE` to select an accessible SQL Server instance while always generating and cleaning up its own `NexoraWebsiteTest_*` database. LocalDB remains its default. Its readiness timeout follows the API's bounded readiness check.

## Local use

Restart **Portal (API + Web)** in VS Code to rebuild the API. Open `/portal` for members and `/settings` for staff access management. Existing development credentials remain local; this document does not contain passwords. Website and Portal debug compounds share ports and cannot run simultaneously.

## Remaining plan work

This increment completes the personal application receipt/draft lifecycle and adds operational staff access management. It does not mark the entire portal or master-plan MVP complete. Remaining work includes:

1. Explicit organisation representative grants, company self-service and corporate entitlement/concurrency foundations.
2. Application evidence upload and permitted document access with the shared secure document lifecycle.
3. Durable notifications and delivery status, initially through a test sink.
4. Broader member-editable profile fields once field ownership/verification rules are defined.
5. Production identity, invitations and recovery when the deferred provider decision is revisited.
6. Paid workflows and later event/service capabilities in their master-plan phases.

The local-development choice defers production identity; it does not remove company, document or notification requirements from the full portal scope.
