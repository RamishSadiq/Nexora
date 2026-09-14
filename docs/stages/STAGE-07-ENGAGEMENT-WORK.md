# Stage 7 — Engagement and work core

Implemented separate tenant-scoped Engagement and Work modules, migrations, permissions and workspaces.

- Groups and committees, Contact membership and roles, meetings and agendas.
- Campaign draft and audience preparation. Only Contacts with evidenced email consent qualify; any denied email suppresses the Contact. Preparation is versioned and replay-protected. No message is sent; future delivery must revalidate consent at send time.
- Funds, separate donation and pledge totals, immutable contribution records and unique references. These record externally confirmed activity; no money moves.
- Tasks with tenant-valid assignees and teams, related Contacts, priorities, due dates and completion. Assignment notifications are visible only to their recipient.
- Validated task imports: up to 500 JSON rows, persisted preview, explicit transactional commit, and concurrency protection against duplicate commit. CSV task exports neutralize spreadsheet formula prefixes.
- Tenant and user query filters, persistence write validation, composite foreign keys, optimistic concurrency and append-only activity.

Verification: endpoint integration tests cover consent exclusion, contribution duplicates, invalid import rollback, commit replay, CSV escaping, permission denial and forged tenant writes. SQL Server migrations were applied and replayed in an isolated platform test catalog. Web lint and production build are included in the final gate.

Remaining breadth: campaign provider delivery, preferences centre, meeting minutes/attendance, group self-service, recurring tasks, background reminders, general-purpose CSV mapping, pledge settlement and finance integration. Lists are bounded operational views, not an unlimited archive browser. No external providers or credentials are required for the implemented local workflows.
