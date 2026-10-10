# Membership archive behaviour

4 October 2026. Implements CONTACT ER `Archive` as a reversible Membership-owned retention flag, separate from CRM record archive and membership status.

`PUT /api/v1/membership/members/{id}/archive` requires inherited Membership read access and Membership manage access. Its body contains the current `version`, explicit nullable-contract `isArchived` (a supplied boolean is required), and a required reason. Reasons are trimmed, limited to 500 characters and reject control characters. Missing state, unchanged state and invalid reasons return validation errors; stale versions conflict. Foreign tenant records remain inaccessible.

Only cancelled or lapsed memberships can be archived. Active memberships must first use their existing cancellation, resignation or lapse command. Archiving never changes status, effective lifecycle dates, term, rate, subject, application, original join date or predecessor links. `IsArchived`, `ArchivedAtUtc`, `ArchivedByUserId` and `ArchiveReason` record the current archive state; `membership.archived` history records its actor and reason. Restoring clears current archive metadata and appends `membership.restored` with its reason. It does not reactivate a cancelled/lapsed membership or grant entitlements.

Archived records remain readable through direct Membership details and history, operations overview and CRM Contact/Account projections. No global archive query filter is installed: tenant filtering and history visibility remain intact. Archive/restore receipts do not replace the latest lifecycle receipt or its effective date in CRM. Application/predecessor links and original histories remain retained.

Normal `GET /members` listings exclude archived rows. Optional `archive=current|archived|all` controls the filter and applies before pagination/counting; current is the default. The staff listing includes this selector and archive labels. Membership details include versioned archive/restore controls and show the current archive reason/time. Archived records hide lifecycle actions; historic administration, original join-date and classification corrections remain available with their existing permissions and version checks.

Lifecycle commands and renewal/reinstatement quoting reject archived memberships, including callers through the shared workflow command methods. Operational reviews, certificate issuance and communication drafting require restoration. Existing operational history remains readable. New communication drafts now touch the membership version so drafting and archiving cannot silently race; the draft and receipt share the same save transaction. Review/certificate operations already touch that version.

Archived predecessors are excluded from rejoin candidates. Rejoin submission and approval both require an unarchived predecessor; a pending application cannot bypass a subsequent archive. Restoration makes an otherwise eligible inactive predecessor available again. Existing successor checks and one-active-membership indexes remain unchanged.

Portal contracts and grants are unchanged. Archiving only inactive memberships does not revoke or create login bindings. Historical archive import and external-provider publication/communication changes remain future scope.

Migration `20261004101230_MembershipArchiveState` adds `IsArchived` with default false and nullable archive timestamp, actor ID and reason (500 characters). Existing rows remain unarchived. This task has not applied the migration to the shared development database. Changes remain uncommitted.

Verification covers active archive rejection, reason/state validation, archived/current/all listings, retained history and CRM projection, stale versions, archived operational guards, historical corrections, pending rejoin approval and restoring without reactivation. Permission checks cover foreign manager and read-only caller rejection; Portal assertions exclude staff archive metadata. Isolated SQL verification covers migration replay, existing row preservation, default values, stored archive metadata/projection and restoring the flag without changing inactive status. Browser appearance remains unverified.

Verification completed: 19 API checks passed (15 lifecycle/archive/projection/Portal checks, three application journey regressions and one draft/archive version check). One isolated SQL migration/persistence test passed without skips. TypeScript, focused ESLint, production build and focused diff checks passed. The build retained seven previously observed generated CSS warnings.
