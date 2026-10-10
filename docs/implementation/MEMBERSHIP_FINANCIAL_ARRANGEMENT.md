# Membership financial arrangement

4 October 2026. Adds recorded support for CONTACT FD–FI, FK and FL–FM: renewal month, recorded renewal date, planned next renewal date, promotion code, payment method/frequency, payer and invoicing Account. Account memberships use the same owning-module model. The selected payment providers are GoCardless and Stripe.

`GET /api/v1/membership/members/{id}/financial-arrangement` requires Membership read permission. `PUT` also requires manage permission, the current membership version, details and a reason. The staff editor and Contact/Account Membership projection show the arrangement. Portal membership DTOs exclude it.

Dates and text preserve explicit historical evidence. Renewal month is 1–12; a recorded renewal date cannot be in the future; a planned next date cannot precede a supplied recorded date. Dates do not schedule or perform a renewal. Promotion code, payment method and frequency are trimmed, limited to 160 characters and reject control characters; recording a code does not apply a discount. Unknown values stay null.

Paid By references a tenant-local active Contact or Account when assigning a new payer. Invoice to Account = Yes requires a tenant-local active Account; otherwise the invoicing Account must be absent. Display names and payer kind are taken from CRM, never trusted from submitted snapshots. Existing unchanged references retain their stored names when records later become inactive. These relationships do not change membership ownership or entitlements.

The settled-renewal workflow validates the invoice customer against the invoicing Account when explicitly selected, otherwise the recorded payer, otherwise the membership Account/Contact. Existing currency, exact quote, reconciled allocation, reviewed-term and invoice reuse checks still apply. Saving an arrangement does not issue invoices or collect payments. It also does not reassign existing invoices.

Payment provider preference accepts only `gocardless`, `stripe` or null, normalising casing and whitespace. This preference does not prove a connected provider account, bank authorisation, successful collection or active mandate. No credentials, bank account numbers or sort codes are accepted by this endpoint.

Migrations `20261004130953_MembershipFinancialArrangement` and `20261004131853_MembershipPaymentProviderPreference` add nullable subscription metadata; neither was applied to the shared development database. Changes append `membership.financial-arrangement.updated` history and touch the parent version. Archived records may receive metadata corrections without lifecycle changes.

Still required: payer-based rating rules (FJ); invoice-to-membership-term links and settlement projections for paid-through date, last payment amount/date/method and outstanding balance (FN–FR); provider connections and Direct Debit mandate/collection lifecycle. No customer-wide balance or subscription end date is used to invent those fields.

Provider implementation will use hosted authorisation and verified provider events: [GoCardless hosted pages](https://docs.gocardless.com/docs/collect-payments/integration-types/gocardless-hosted-pages), [Stripe Bacs Direct Debit](https://docs.stripe.com/payments/bacs-debit), and [Stripe webhook guidance](https://docs.stripe.com/webhooks). The provider workflow needs tenant-bound connection/account and environment identifiers, secret-store references, provider customer/mandate/payment IDs, persisted signature-verified event receipts, duplicate protection and reconciliation. Finance owns settlement and mandate evidence; Membership owns arrangement and term rules; CRM projects their published contracts.

Source bank fields FS–FV need masked provider-backed information and an explicit protected migration path, rather than plaintext bank fields on Contact. FW–FZ and GA–GE need genuine mandate, collection, suspension and instalment evidence. A Stripe card payment must not be labelled a Direct Debit mandate. Provider setup, collection and suspension actions remain unimplemented; recorded preferences never claim these actions occurred.
