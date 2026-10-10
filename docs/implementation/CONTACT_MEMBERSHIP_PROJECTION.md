# Contact membership projection and remaining template gaps

4 October 2026. Reviews CONTACT EH2:GE2: 22 Membership General Information fields, 15 Membership Finance Information fields and 13 Membership Direct Debit Information fields. This slice implements a detailed read-only Contact/Account view of existing Membership data. It does not claim complete coverage of those 50 workbook fields.

## Delivered behaviour

`GET /api/v1/crm/records/{id}/memberships` requires both `crm.read` and `membership.read`, validates the CRM record within the active tenant and returns a paged projection supplied by the Membership module through `ICrmMembershipDirectory`. CRM has no dependency on Membership's implementation assembly and stores no membership copies. The view is also available on archived CRM records.

The default page size is 10; allowed sizes are 1–50 and page numbers 1–10000. Memberships are ordered by stored active status first, then creation date descending and ID. Unknown records return 404; missing permissions deny access. Tenant filters apply to subscriptions, products, applications, reviews and history.

The new Membership details section shows the Nexora membership ID, product and current product category, subject, stored membership status, linked application date/type, subscription start/end, recorded rate/currency, renewal exclusion and notes for the review matching the current subscription end, and the latest explicit lifecycle receipt with its date/reason. It links directly to the owning Membership and Application detail pages and supports refresh/pagination. Staff without Membership read access do not see the section.

For a Contact, personal subscriptions and organisation subscriptions for which the Contact is the applicant are labelled separately. An Account association alone does not imply personal membership or produce an organisation membership entry on another Contact. Account screens show their organisation subscriptions. This is a staff projection; Portal access rules and DTOs are unchanged.

The application date/type is included only when the linked application has matching Contact and Account subject IDs. Product category reflects current product configuration; rate reflects the subscription's recorded amount. Term end is exclusive. The subscription start is not an inferred original join date or the start of a subsequently renewed term. Missing reviews remain unknown, distinct from a recorded false renewal exclusion. Review notes belong to that review, rather than general membership Remarks. Lifecycle recording timestamps and explicit effective dates are shown separately; undated legacy receipts remain unknown; unrelated review/history events do not replace the latest lifecycle receipt. Reinstatement can retain the same end date and therefore the same review; an extended end date requires its own review.

## Exact workbook mapping

| CONTACT source | Coverage and remaining requirement |
| --- | --- |
| EH Member_ID | Nexora internal subscription ID displayed explicitly; [Membership administration](MEMBERSHIP_ADMINISTRATIVE_DETAILS.md) now retains the legacy reference separately. |
| EI Application_Date | Linked application's recorded creation date displayed. No imported historical application date override. |
| EJ Join_Date | [Explicit original join date](MEMBERSHIP_ORIGINAL_JOIN_DATE.md) implemented; unknown remains null, subscription start stays separate. |
| EK Member_Type | [Member-type assignment and effective history](MEMBERSHIP_FEE_AND_LAPSE_OPTIONS.md) implemented, separate from product and application type. |
| EL Member_Category; EM Category_Date | [Membership category assignment and effective history](MEMBERSHIP_CLASSIFICATION_HISTORY.md) implemented. Current product category remains separate. |
| EN Subscription_Band; EO Subscription_Band_Date | Existing band definitions now support [Membership assignments and effective history](MEMBERSHIP_CLASSIFICATION_HISTORY.md). No automatic rating rule is inferred. |
| EP Member_Status | Existing Membership status displayed; arbitrary legacy status translation is not implemented. |
| EQ Status_Date | [Effective lifecycle dates](MEMBERSHIP_LIFECYCLE_DATES.md) implemented for new owning-module commands; existing undated records remain unknown. |
| ER Archive | [Reversible Membership archive](MEMBERSHIP_ARCHIVE.md) implemented for inactive subscriptions, separate from CRM archive and entitlement status. |
| ES Resign_Reason; ET Resign_Date | [Explicit resignation action and effective history](MEMBERSHIP_LIFECYCLE_DATES.md) implemented, distinct from cancellation. |
| EU Reinstatement_Date | [Reinstatement effective date](MEMBERSHIP_LIFECYCLE_DATES.md) implemented through current-policy lifecycle commands; historical imported reinstatement remains future scope. |
| EV Re_Join_Date | [Explicit rejoin approval date and predecessor history](MEMBERSHIP_REJOIN_HISTORY.md) implemented for new staff rejoin applications. Historical import remains future scope. |
| EW Exempt_Joining_Fee | [Nullable joining-fee exemption](MEMBERSHIP_FEE_AND_LAPSE_OPTIONS.md) implemented for application quotes and approved member tracking. |
| EX Free_Membership | [Explicit free-membership option](MEMBERSHIP_FEE_AND_LAPSE_OPTIONS.md) implemented for application/renewal quotes; a zero recorded rate does not infer it. |
| EY Membership_Pack_Sent; EZ Membership_Pack_Sent_Date | Explicit nullable pack tracking and optional sent date implemented in Membership administration. A welcome communication draft does not prove a sent pack. |
| FA Exclude_from_Renewal | Existing current-term review flag displayed and enforced by the existing renewal command. |
| FB Exclude_from_Lapse | [Nullable lapse exclusion](MEMBERSHIP_FEE_AND_LAPSE_OPTIONS.md) implemented and enforced by the lapse command. |
| FC Remarks | Membership-level Remarks implemented in Membership administration; current review notes remain separate. |
| FD Renewal_Month; FE Renewal_Date; FF Next_Renewal_Date | [Recorded month/date and planned next date](MEMBERSHIP_FINANCIAL_ARRANGEMENT.md) implemented. These do not schedule renewals; subscription end remains separate. |
| FG Promotion_Code | Recorded code implemented in the financial arrangement; promotion application remains future scope. |
| FH Payment_Method; FI Payment_Frequency | Recorded payment method/frequency implemented; no payment collection is inferred. |
| FJ Apply_Rate_to_Paid_By; FK Paid_By | Payer relationship implemented with validated CRM identity and name snapshots; payer-based rating rules remain missing. Entitlement Account/Contact stays separate. |
| FL Invoice_to_Account; FM Invoicing_Account | Validated invoicing Account relationship and nullable Invoice to Account implemented; no invoices are issued by saving the arrangement. |
| FN Paid_Till_Date | Paid-through projection missing. Subscription end does not prove payment coverage. |
| FO Last_Payment_Amount; FP Last_Payment_Date; FQ Last_Payment_Method | Finance-owned settlement projection missing. Membership rate/review payment verification does not supply these values. |
| FR Total_Outstanding | Finance-owned outstanding-balance projection missing; no synthetic zero is returned. |
| FS Bank; FT Current_Bank_Sort_Code; FU Current_Account_Number; FV Current_Account_Title | GoCardless and Stripe selected; provider preference implemented. Provider-backed masked banking data and protected migration remain future scope. |
| FW Mandate_On_Date; FX First_Collection_Date; FY Regular_Collection_Date; FZ Direct_Debit_Suspended | Mandate/collection lifecycle remains future scope. |
| GA Instalment_Recalculated_Date; GB Total_Instalments; GC Instalments_Paid; GD BACS_Reference; GE Direct_Debit_Flag | Instalment/provider lifecycle remains future scope. |

## Verification and next scope

The original read-only projection required no schema change. Subsequent owning-module slices add their documented migrations; see the linked implementation notes. Isolated SQL verification uses its own generated database; this task does not alter the shared development data. Source changes remain uncommitted.

Five API checks passed: three new projection checks and two existing Membership lifecycle/boundary regressions. One isolated SQL Server projection test and one module dependency architecture test passed. No selected tests were skipped. Coverage includes lifecycle updates, no writes on read, review selection after term changes, personal/organisation subject separation, stable pagination, archived CRM records, both permission requirements and foreign-tenant rejection. SQL verification also covers missing history/reviews without invented defaults and preserves the recorded rate independently of the current product rate.

TypeScript, focused ESLint and the production frontend build passed. The build retained seven existing generated CSS warnings. Authenticated browser appearance remains unverified following the preceding unavailable local-sign-in check.

Membership administration, classification/type history, join and lifecycle dates, rejoin/archive, fee/lapse options and recorded payer/invoicing arrangements are implemented. Historical import, term-linked settlement projections and provider-backed Direct Debit remain. See [consolidated progress](MIGRATION_TEMPLATE_PROGRESS.md).
