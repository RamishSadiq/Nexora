# Stage 14 — Organisation portal access

Resumed 22 September 2026 from the interrupted organisation browser verification.

## Delivered

Staff can grant and revoke explicit company access for a portal Contact with a current CRM Account relationship. Viewing and application submission are separate permissions; CRM roles alone do not grant portal access. Requests recheck the grant, active Account and current relationship.

Members can select an authorised company, view its memberships and applications, and, with submission permission, save/review/submit company drafts and open retained application receipts. Personal records remain separate. Company application availability is a separate product setting, defaulting off. Drafts are scoped to their author and company.

The existing migrations add organisation grants and company draft scope, and separate active personal-membership uniqueness from company-membership uniqueness. These constraints are a foundation, not proof of all concurrent corporate entitlement workflows.

## Resumed fix and verification

Explicit label associations now give both organisation selects stable accessible names. The staff select uses the portal access ID so multiple access cards have distinct element IDs.

- Targeted ESLint passed for both changed components.
- Production frontend build, TypeScript and route generation passed.
- All three browser journeys passed against `next start`: organisation permission/application/receipt/approval/revocation, personal approval and personal rejection (1.1 minutes).
- Browser run: `artifacts/website-ed74a4288c6843e9b081fc1eaa6be1b2`.
- Company mobile receipt screenshot visually reviewed: `artifacts/portal-company-receipt-mobile.png`; no horizontal overflow.
- Browser checks use a disposable SQL Express catalog and local-development identity. No public deployment or production identity verification is claimed.

## Next unfinished scope

Secure application evidence upload and authorised document access, durable notifications through a test sink, and deeper corporate entitlement/concurrency verification remain. Production identity, invitations, recovery and payment integrations remain deferred. The full portal is not complete.

Full backend regression also passed on this resumed run: 105 passed, zero failed, zero skipped (PortalNext; SQL Express enabled; 2 minutes 31 seconds), including module migration replay/model checks. The browser runner confirmed cleanup of its processes and disposable catalog.

## Evidence work started — 22 September 2026

The existing application-evidence lifecycle now has focused safety regression coverage. The checks verify filename/path normalisation, matching file signatures for accepted types, rejection of unsupported or mismatched content, and the no-store/nosniff/sandbox headers used for downloads. Downloads remain gated on a clean scanner result, with tenant, draft-owner and active organisation-grant filters preserved.

Focused safety result: **5 passed, zero failed** (`ApplicationEvidenceSafetyTests`, configuration `PortalEvidence`). The authenticated portal journey also covers member upload, owner isolation, quarantined-download denial and unknown-application staff denial: **1 passed, zero failed** (`Evidence_upload_is_owner_scoped_and_quarantined_files_cannot_be_downloaded`). Clean-scan staff review and organisation-grant revocation remain open because the local test scanner intentionally quarantines when ClamAV is unavailable.

## Notification sink started — 22 September 2026

Portal submission now writes a tenant-scoped, durable `application.submitted` notification in the same transaction as the application and its history. Members can list their own notifications and mark their own entries read; the response exposes delivery state (`queued`) without exposing tenant or staff data. This is a local test sink, not an external email claim.

The receipt journey verifies queue creation and read-state persistence. Focused result: **1 passed, zero failed** under `PortalNotifications`. Approval/rejection, access-grant/revocation and evidence events still need corresponding notification types and delivery handling.

The local test sink now includes a staff delivery transition (`queued` → `delivered`) with an incremented attempt count. The existing submission journey still passes after the migration and delivery endpoint was added. Approval/rejection and access-grant/revocation remain intentionally unclaimed until recipient binding is wired to the portal account/contact boundary.

Approval and rejection now resolve the active portal access for the application Contact and enqueue a member-targeted notification in the same membership decision save. No email or external provider is implied; an absent active portal access produces no recipient notification. The focused receipt/decision regression remains green under `PortalNotifyDecision`.

Portal access grant, local-member provisioning and revocation now enqueue recipient-specific notifications through the same local sink. The access-list regression passed after this wiring under `PortalNotifyAccess`. These notifications are queued test-sink records; delivery remains an explicit local transition.

Evidence upload and scan/retry now enqueue member-targeted scan-result notifications (`clean`, `rejected` or `quarantined`) using the active portal Contact binding. The evidence ownership regression passed under `PortalNotifyEvidence`; the local scanner still intentionally quarantines without ClamAV, so clean-file delivery remains an environment-dependent check.

## Concurrency hardening started — 22 September 2026

Membership applications now have SQL filtered unique indexes for one submitted personal application per Contact/product and one submitted organisation application per Account/product. This closes the check-then-insert race at the database boundary; a concurrent loser is surfaced through the existing conflict response. The application journey regression passed under `PortalConcurrency`, including migration/model replay in the test configuration.

Active personal and organisation memberships now have matching SQL uniqueness guards per Contact/product and Account/product. Approval and revocation regressions passed under `PortalConcurrency2` (2/2). This prevents duplicate active rows; overlapping date ranges across separate products or policy-specific limits still require an explicit business constraint.

The membership rule was clarified: a Contact may hold only one active membership total, and an organisation may hold only one active membership total, regardless of offering. The guards were re-scoped from `(subject, product)` to Contact-only and Account-only and a follow-up migration was generated. Approval and revocation checks passed 2/2 under `OneMembership`.
