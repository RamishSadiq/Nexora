# Membership effective lifecycle dates

4 October 2026. Implements CONTACT EQ `Status_Date`, ES `Resign_Reason`, ET `Resign_Date` and operational EU `Reinstatement_Date` through Membership lifecycle commands.

`MemberSubscription` now has nullable `StatusEffectiveOn`, `ResignedOn`, `ResignationReason` (500 characters) and `ReinstatedOn`. Append-only `MembershipHistory` has nullable `EffectiveOn`; its existing `CreatedAtUtc` remains the recording timestamp. Existing records are not backfilled from timestamps or subscription dates.

New approval records active status effective on UTC today and a dated activation receipt. This records when approval grants stored active status; it does not assert an original join date or alter a future requested subscription start. Existing approval rules and the one-active-membership rule remain intact.

The existing `POST /members/{id}/{action}` supports an optional `effectiveOn`; omitted/null dates default to UTC today, preserving older clients and workflow commands. Dates must be valid and nonfuture, at or after recorded original joining and current status dates, and at or after the latest dated lifecycle receipt. Same-day transitions are permitted and retain separate immutable receipts. Actions apply immediately; there is no scheduler or retrospective entitlement reconstruction.

`resign` is a new explicit action from active status. It uses the existing cancelled entitlement state and records resignation reason/date, without changing rates or terms. Ordinary cancellation remains distinct and does not create or overwrite resignation evidence. Renewal/reinstatement preserve the latest resignation information, with all prior events retained in history. Repeated resignation from cancelled status is rejected. Existing cancellation is not relabelled as resignation.

Renewal and reinstatement require today's effective date because their existing quote, compliance and term logic runs under current policy. Reinstatement records its effective date and changes status to active. An active-to-active renewal creates a dated lifecycle receipt without changing the original active status date; lapsed-to-active renewal changes the status date. Lapse retains the existing expired-term guard and additionally cannot have an effective date before the term end.

All actions retain current member-version checks and Membership manage authorization. Workflow callers use the same domain method and default date without changing their published input contract. Administrative or classification updates do not replace lifecycle dates. [Distinct operational rejoin history](MEMBERSHIP_REJOIN_HISTORY.md) is now implemented. Historical imported reinstatement, original legacy status-date corrections and standalone event import remain future scope.

Membership detail includes an effective date input, status/resignation/reinstatement values and distinct effective/recorded labels in history. CRM Contact/Account membership cards show these values and the latest lifecycle's effective date separately from its timestamp. The latest resignation is retained after reinstatement rather than implying the member is still resigned. Portal contracts do not expose the new staff metadata.

Migration `20261004094442_MembershipLifecycleEffectiveDates` adds four nullable subscription columns and one nullable history date column. No shared development database migration was applied by this task. Source changes remain uncommitted.

Verification covers approval dates, explicit resignation, cancellation distinction, reinstatement, invalid/out-of-order dates, stale versions, unchanged-status renewal, bounded historical resignation on an undated legacy fixture, permissions, CRM projection and Portal isolation. Isolated SQL checks exercise migration replay/defaults, exact date persistence/projection and SQL translation of the latest effective date query. Browser appearance remains unverified.

Verification completed: 12 distinct API checks passed across the initial 11-check regression run and the final administration/lifecycle rerun, including the added legacy fixture and final chronology guards. One isolated SQL test passed with no skips. TypeScript, focused ESLint, production build and focused diff checks passed. The build retained seven existing generated CSS warnings.
