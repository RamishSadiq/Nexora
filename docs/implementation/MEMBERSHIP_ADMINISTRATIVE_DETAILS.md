# Membership administrative details

4 October 2026. Adds four Membership-owned fields and their Contact/Account projection.

| CONTACT workbook field | API field | Storage |
| --- | --- | --- |
| EH2 Member_ID | legacyMemberReference | MemberSubscription.LegacyMemberReference |
| EY2 Membership_Pack_Sent | membershipPackSent | MemberSubscription.MembershipPackSent |
| EZ2 Membership_Pack_Sent_Date | membershipPackSentDate | MemberSubscription.MembershipPackSentDate |
| FC2 Remarks | remarks | MemberSubscription.Remarks |

The legacy reference is separate from the internal Nexora membership GUID, and preserves supplied case and leading zeros. This slice does not invent global uniqueness for legacy references or import workbook records. Each subscription retains its own administrative values. Account memberships use the same model; no CRM copies are created.

## Validation and editing

Legacy reference is optional, trimmed, limited to 160 characters and rejects control characters. Remarks is optional, trimmed and limited to 4000 characters; internal line breaks and tabs are supported, other control characters are rejected. Blank values become null.

Pack sent is nullable: unknown, explicitly not sent or explicitly sent. Its optional DateOnly value must be valid and no later than the current UTC date; a date is accepted only when the flag is true. True without a date is allowed for incomplete historical evidence. False/unknown with a supplied date is rejected. Nothing is inferred from welcome drafts or notifications, and changing these fields does not send a pack.

`GET /api/v1/membership/members/{id}/administration` requires Membership read permission and returns `{ version, details }`. `PUT` additionally requires Membership manage permission and the current membership version. It replaces this four-field object; a missing/null object is rejected, and an empty object explicitly clears all four fields. A no-op save still touches the membership version/time. Stale administration and lifecycle saves conflict against the same version.

The Membership detail page provides the editor and reload-on-conflict flow. Read-only staff see disabled fields. Changing the pack flag to No or Unknown clears the date in the form before save. Existing lifecycle actions preserve the metadata, and metadata edits leave ownership, application/product links, status, term dates and rates unchanged. Administrative metadata remains editable on cancelled/lapsed subscriptions; it does not reinstate them. Saves create a `membership.administration.updated` receipt with actor/time/correlation and use the existing field data audit. Administrative receipts do not replace the latest lifecycle action in the CRM projection.

Contact and Account Membership details display the saved values, including Unknown rather than a default No. General membership Remarks stays distinct from renewal review notes. Portal's own-membership contract remains unchanged and does not include this staff administration data.

## Migration and next scope

`20261004090543_MembershipAdministrativeDetails` adds four nullable columns to Membership Subscriptions. Existing rows retain their IDs, status, dates, rates and versions, with unknown administrative values. The shared development database has not been migrated by this task; apply the migration through the normal procedure before using these fields against an existing database. Source changes remain uncommitted.

## Verification

Eight API checks passed: three new administration/permission/Portal checks, three existing Contact membership projection checks and two Membership lifecycle/boundary regressions. One isolated SQL Server migration test passed, covering existing-row preservation through upgrade, repeat migration, nullable values, 4000-character Remarks, DateOnly storage, the CRM projection and tenant isolation. No selected tests were skipped. The API checks cover invalid inputs, clearing, unknown versus false, incomplete historical pack dates, stale saves, lifecycle preservation and read-only/foreign-tenant access.

TypeScript, focused ESLint, the production frontend build and focused whitespace checks passed. The build retained seven existing generated CSS warnings. Authenticated browser appearance remains unverified following the preceding unavailable local-sign-in check.

[Membership-specific category and subscription-band assignments with effective-date history](MEMBERSHIP_CLASSIFICATION_HISTORY.md) is now implemented as the next slice. Effective join/status/resignation/rejoin dates, fee/lapse policies, finance relationships and Direct Debit retain the boundaries documented in [the Contact membership field mapping](CONTACT_MEMBERSHIP_PROJECTION.md).
