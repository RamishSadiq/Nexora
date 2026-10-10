# Membership Application template

4 October 2026. The source sheet contains 23 fields. This slice provides application representation for 18, partial support for lifecycle purpose, and leaves four bank fields for protected provider-backed capture. No workbook rows have been imported.

| Source cells | Representation and limits |
| --- | --- |
| B-E: Member_ID, Member_Name, Contact_ID, Account_ID | Approved subscription link and subject Contact/Account links and recorded names. Member linkage exists only after approval. Source identifiers still require import resolution. |
| F-H: Member_Type, Member_Category, Subscription_Band | Application-owned requested lookup IDs and recorded names. Tenant-bound links and correct lookup kinds are validated; changing a lookup name does not rewrite an existing application snapshot. Approval creates the initial classification receipts from these snapshots. |
| I-L: Payment_Method, Payment_Frequency, Invoice_to_Account, Invoicing_Account | Nullable requested payment and invoicing arrangement. Yes requires an active tenant-local Account; No/Unknown clears the recipient. Approval rechecks its availability and copies the arrangement without changing entitlement ownership. |
| M: Amount | Existing submitted quote and currency snapshot. Requested classification/payment edits do not recalculate it, issue invoices or collect funds. Historical amounts still need reconciliation. |
| N: Application_Date | Existing submission timestamp; historical date import remains separate. |
| O: Application_Type(New, Upgrade, Reinstate, Rejoin) | Separate LifecyclePurpose records New or Rejoin, derived from validated predecessor linkage. Existing ApplicationType remains the corporate/individual/affiliate applicant dimension. Upgrade and Reinstate application transitions remain missing; unsupported or contradictory purposes are rejected. Existing direct membership reinstatement is a different operation. |
| P-Q: Application_Status, Status_Date | Submitted/approved/rejected plus a nullable operational effective date. Submission and decision record the current UTC date; editing requested details does not change it. Existing records remain unknown until explicit historical evidence is imported. |
| R: Exempt_Joining_Fee | Existing nullable staff exemption and fee quote snapshot. |
| S-T: Remarks, Description | Separate application-owned staff fields, up to 4,000 characters each, including multiline text. They do not overwrite applicant notes, decision reasons or Membership Remarks and are excluded from Portal receipts. |
| U-X: Bank, Account_Title, Sort_Code, Account_Number | Deferred protected/provider-backed capture and migration. GoCardless and Stripe are the selected providers; live connections and authorisations are not implemented by this slice. |

Staff can submit these requested details and review them in Membership Application. The details GET endpoint requires Membership read access. PUT requires Membership manage access, a current application version and a nonblank change reason; only submitted applications can be edited. Changes append an audit receipt. Decided application snapshots stay fixed. Approval uses the shared decision path, including workflow decisions, and preserves the existing one-active-membership rule.

Migration `20261004150349_MembershipApplicationRequestedDetails` adds nullable columns and three tenant-bound lookup foreign keys. It does not invent historical purpose, dates or requested classifications. Shared development migrations remain unapplied and source changes are uncommitted. Authenticated browser appearance has not been verified.

Remaining work: Upgrade/Reinstate application transitions, four protected banking fields, historical identity/date/status translation and source-row import. The Membership Rate sheet and term-linked Finance/Direct Debit work remain separate slices.

Membership Application verification: 55 selected backend API/SQL tests passed with no skips, covering requested-detail submission/edit/clear, stale-version rejection, lookup-kind validation, preserved quote and name snapshots, approval handoff, unavailable invoicing Accounts, New/Rejoin purposes, existing membership lifecycle rules, and Portal privacy. The isolated SQL Server check validates nullable legacy defaults, repeat upgrade, retained values, 4,000-character text, tenant-bound lookup foreign keys and restricted lookup deletion. TypeScript, focused ESLint and the production frontend build passed; seven pre-existing generated CSS warnings remain. Build-generated configuration changes were restored while retaining other workspace edits. This evidence does not establish historical import, live-provider readiness or authenticated browser appearance.
Final focused rerun: the isolated SQL upgrade test and API permission/foreign-tenant test both passed after the final test refinements, with no skips or compiler warnings.
