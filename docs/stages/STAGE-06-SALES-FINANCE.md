# Stage 6: Sales and finance

Delivered: catalogue, multi-line orders with server-calculated price/VAT snapshots, posting to immutable invoice amounts, external receipt records, allocations/reversals, credits, external refund records, statement reconciliation and formula-safe invoice CSV export. Currency and customer must match for allocation; balances are derived from append-only financial entries. Invoice/receipt concurrency tokens serialize balance changes. Unique source references prevent duplicate receipts, credits, refunds and reversals.

No payment provider is connected: records never charge or refund money. Operators record transactions performed externally. The UI explains this boundary. Credits and reversing entries correct posted records; posted amounts cannot be edited through persistence. Rate/VAT configuration is entered by staff, not jurisdictional tax advice.

Finance owns a SQL schema/migration history. SQL Server initial migration applied in the isolated review database. API tests prove tax calculation, posting, balanced receipt/credit/reversal/refund flows, over-allocation and over-refund rejection, duplicate protection, posted amount immutability, statement matching and tenant export permissions. Production reconciliation import formats, general-ledger integration, payment providers and independent accounting acceptance remain follow-ups.
