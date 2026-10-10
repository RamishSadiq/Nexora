# Membership type, joining fee and lapse options

4 October 2026. Completes application support for CONTACT EK Member_Type, EW Exempt_Joining_Fee, EX Free_Membership and FB Exclude_from_Lapse.

Member type uses the existing `member-type` setup list and the same immutable, effective-dated classification history as category and subscription band. Assignment, correction and clearing have independent timelines, definition-name snapshots, reasons and parent-version concurrency. Types do not change membership rates. The staff Membership editor and CRM projection show the current type.

Membership options are nullable: unknown remains distinct from Yes and No. `GET /api/v1/membership/members/{id}/options` returns the current options and version; `PUT` requires Membership manage permission, the current version and a reason. Changes preserve existing invoices, recorded subscription rates, status and dates. They may correct archived records without reactivating them.

Product policy now has a joining fee, defaulting to zero. `PUT /products/{id}/joining-fee` requires the existing policy version (null when creating the policy), a reason and a nonnegative amount below 100,000,000 with at most two decimal places. The existing policy endpoint preserves the joining fee when older clients omit it. New application quotes include the configured joining fee unless explicitly exempt; exemption does not waive the ordinary membership rate or processing fee. Staff application options are snapshotted at submission and copied to the approved membership; later policy changes do not rewrite the submitted quote. Portal application quotes include the joining fee without exposing staff exemption controls.

Free Membership = Yes makes new application and subsequent renewal/reinstatement quotes zero, including processing and joining fees. Free renewals bypass only the paid-renewal verification requirement; declaration, compliance, renewal exclusion and lifecycle rules still apply. A zero rate does not infer free-membership status. Changing the option does not retrospectively refund or edit existing financial records.

Exclude from Lapse = Yes prevents the ordinary lapse command. Unknown and No retain the existing lapse rule. This does not extend the term or grant another active membership. Changes to either option append `membership.options.updated` history; joining-fee changes append `policy.joining-fee.updated`. Staff fields remain excluded from Portal membership DTOs.

Migrations: `20261004125422_MembershipMemberTypeHistory` expands the classification-kind constraint; `20261004125747_MembershipFeeAndLapseOptions` adds nullable member/application flags and the zero-default policy joining fee. Neither was applied to the shared development database. A rollback to the previous classification constraint needs member-type history removed or migrated first; use a reviewed forward repair for retained history.

Verification covers type assignment/clearing, wrong lookup kinds, unchanged rates, joining-fee quote snapshots and exemptions, free renewals with payment required, lapse exclusion and nullable clearing, stale versions, permission/tenant boundaries, Portal exclusion and isolated SQL upgrade preservation. See [migration progress](MIGRATION_TEMPLATE_PROGRESS.md) for consolidated validation and remaining scope.
