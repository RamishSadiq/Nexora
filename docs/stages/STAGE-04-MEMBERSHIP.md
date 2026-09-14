# Stage 4: Membership

Core delivery: products/categories, term lengths and currency/rates; applications with immutable quote snapshots; reasoned approval/rejection; membership activation, renewal, cancellation, lapse and reinstatement; actor/correlation/date status history. The Membership workspace supports catalogue maintenance, applications and decisions, member search and lifecycle actions.

Membership references CRM through ICrmDirectory, not CRM entities. Tenant filters, guarded writes and composite keys enforce isolation. A filtered unique index prevents simultaneous active subscriptions for the same Contact/product; application versions and one-subscription-per-application keys prevent duplicate approval. Renewal uses the current product rate and extends the existing end date; quotes retain the original rate. These are membership records, not payment charges.

The module owns the membership schema and migration history. Its initial migration was applied to the isolated SQL Server review database. API integration coverage proves quote preservation, approval, renewal, cancellation, reinstatement, stale update rejection, overlapping membership rejection, missing CSRF and tenant scoping. Web lint/build pass. Lists are bounded to the latest 200 records; production pagination, policy-specific proration, automated lapse jobs and full assisted-user acceptance remain follow-ups.

Permissions: membership.read, membership.manage and membership.decide. Restart development seeding and sign in again for new administrator claims. Apply controlled migrations with the MembershipDbContext design-time factory and NEXORA_CONNECTION_STRING. No real payment or member communications are sent.
