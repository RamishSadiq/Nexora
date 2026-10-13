# Portal onboarding — 13 October 2026

## Gaps and scope

The portal already supports personal and company membership applications, but only staff can provision logins. Members cannot request company access. Existing relationships do not themselves grant portal access.

Implement self-registration for the existing local identity adapter, followed by the existing membership application journey. Create a new CRM Contact and portal login together; never claim an existing Contact by email. Resolve the association from the trusted website host, not request payloads. Keep production identity-provider integration, email verification and password recovery as future scope.

Members can request company access by supplying a company name, role and explanation. CRM reviewers match an active Account, approve viewing or application access, or reject the request. Approval creates the CRM relationship and explicit portal grant together. Pending requests expose no company records. Members can cancel pending requests and see review outcomes.

## Plan

1. Add published CRM switches for self-registration and company requests, disabled by default.
2. Add host-bound, CSRF-protected and rate-limited registration with atomic CRM/Identity persistence.
3. Add tenant-scoped request lifecycle, ownership checks, staff approval and concurrency protection.
4. Add neon-compatible signup, member request/history and CRM review screens.
5. Add the schema migration, API security/rollback tests and a browser journey. Enable the switches in the local CRM and show signup in the browser.

## Implemented

- `/portal/signup` creates a portal-only login, a new CRM Contact, name profile and primary email together. Marketing consent stays unknown; no existing Contact is claimed, staff role assigned or membership activated.
- Host resolution accepts the configured association domain, or one unambiguous localhost site in development. Only published self-registration settings apply. Registration requires CSRF and allows five attempts per IP in 15 minutes.
- Members submit company names, roles and explanations under My organisations, inspect their own paginated request history, and cancel pending requests. Duplicate pending requests are blocked.
- CRM Settings → Member portal access → Company access requests lets staff match an active Account and approve viewing or application access, or reject with a member-facing message. Approval saves the relationship, grant and decision in one transaction. Review versions prevent a stale decision or cancellation.
- CRM Portal management → Portal settings → Member onboarding controls both availability switches through the existing draft/publish flow. Both default to disabled. The existing theme and animation controls style the new screens.
- Added Identity migration for requests and their data audit. Existing manual grants and revocations remain available.

## Verification, 13 October 2026

- The portal API regression group passed 40 tests before the final request-message/accessibility refinements. The five affected onboarding tests then passed on the final backend, covering registration boundaries, CRM persistence, membership separation, company ownership/tenant checks, stale review/revocation and rollback of both registration and approval.
- Scoped ESLint, TypeScript and whitespace checks passed. Backend builds passed.
- Four existing SQL Server browser journeys passed against a production frontend, including CRM publication, membership approval/rejection, organisation revocation and the persistent navigation/white-flash regressions.
- Initial onboarding browser checks exposed an assertion that also matched Next's hidden announcer and an ambiguous company-selector label. Both were corrected; request and review messages now support paragraphs. The final SQL Server onboarding browser retest passed in 38.6 seconds against a fresh production frontend: draft/publication gating, mobile signup, password confirmation, registration, personal membership submission, pending company isolation, CRM approval, company viewing/application access and reload persistence.

The final browser run used `NexoraWebsiteTest_aac8c7dd35394668bcdb08ba6de9a918`, stopped its owned servers and confirmed database cleanup. Its evidence is in `artifacts/portal-onboarding-browser-final.log`. The four earlier regression journeys are in `artifacts/portal-onboarding-browser.log`; the final scoped API results are in `artifacts/portal-onboarding-api/portal-onboarding-final.trx`. These are scoped results across runs, not a claim that the complete repository suite passed.

## Local enablement

The shared API was rebuilt, migrated and confirmed ready on 5097. Its LocalDebug build had zero errors and ten non-blocking AD0001 warnings from the ASP.NET route-handler analyzer. Both onboarding switches were published in the local CRM while preserving the existing neon theme and any saved unpublished draft changes. The live portal returned `allowSelfRegistration: true`; `/portal/signup` was opened and its empty account form was verified in the in-app browser. The screenshot is `artifacts/portal-signup.jpg`. No test members or requests were created in the persistent local CRM.

The API fixtures share the CRM/Identity SQLite connection for atomic transaction tests. Browser tests use isolated SQL Server databases and leave persistent local CRM records untouched. No production deployment is included. Production identity-provider integration, email verification and password recovery remain future scope.
