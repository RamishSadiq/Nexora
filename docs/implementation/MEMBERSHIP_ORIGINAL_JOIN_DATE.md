# Membership original join date

4 October 2026. Implements CONTACT EJ `Join_Date` as an explicit nullable `MemberSubscription.OriginalJoinDate`, owned by Membership and projected read-only to CRM Contact/Account membership cards.

Existing subscriptions and newly approved applications retain null until staff supply evidence. Subscription start, application creation and activation receipts do not establish an original join date. This date belongs to the subscription's subject; organisation applicant Contacts do not inherit a personal join date.

`GET /api/v1/membership/members/{id}/join-date` requires Membership read access. `PUT` additionally requires Membership manage access, the current member version, nullable `originalJoinDate` and a required correction reason (trimmed, up to 500 characters, no control characters). Non-null dates must be valid, on or before UTC today and the subscription start. An unchanged value is rejected. Null explicitly clears a previously recorded date.

The separate write endpoint keeps older administration requests from clearing the join date. Corrections share the parent version with lifecycle, administration, classification and review writes. They generate `membership.join-date.updated` receipts with the supplied reason and participate in entity field audit. They do not alter status, rates, entitlement, application or subscription term. Lifecycle commands preserve the value. These correction receipts do not replace the latest lifecycle receipt in CRM.

The Membership detail page includes an editor with optional date, required reason and conflict reload. Read-only staff see the saved date or “Not recorded”. Contact/Account cards display the current value. Portal contracts do not include this staff field.

Migration `20261004093741_MembershipOriginalJoinDate` adds one nullable SQL `date` column without backfilling or changing existing values. It has not been applied to the shared development database by this task.

[Effective lifecycle dates](MEMBERSHIP_LIFECYCLE_DATES.md) now record status dates, explicit resignation date/reason and operational reinstatement dates. Historical imports and distinct rejoin semantics remain future scope.

Verification: API checks cover unknown values, historical dates, invalid and future dates, correction reasons, clearing, stale versions, term/status preservation, cancellation, CRM projection, permissions and Portal isolation. The isolated SQL test covers migration replay, existing subscription preservation, nullable defaults and exact date projection. Browser appearance remains unverified.

Verification completed: seven API checks passed across the projection, administration, original join-date and Portal boundary filters; the changed administration and join-date tests were rerun after correcting an invalid past application-start fixture. One isolated SQL test passed without skips. TypeScript, focused ESLint, production build and focused diff checks passed. The build retained the seven previously observed generated CSS warnings. Changes remain uncommitted.
