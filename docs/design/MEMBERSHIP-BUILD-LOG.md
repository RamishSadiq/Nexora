# Membership rebuild log

## Working agreement

The primary AI implements the product directly. Work is divided into small chunks
with a maximum of 15 minutes per working turn. After each chunk, report actual
changes, checks, limitations and the proposed next chunk, then wait for the user.
Keep project requirements, decisions and implementation notes in this repository.
Production authentication is paused while CRM/membership design is corrected.

## Requirements baseline

- User: applications find an existing Contact or create one, then link it;
  approval creates membership.
- BPCA CoR 3.1–3.2: organisation/person relationships, multiple contact roles,
  main contacts, classifications, reassignment, preferences and scheme enrolment.
- BPCA CoR 4–4.13: application details, review, membership types, fees, renewals,
  compliance, certificates and communications.
- See CRM-COR-ALIGNMENT.md and MEMBERSHIP-COR-REVIEW.md for source mapping and
  unresolved rules. The complete source Account field inventory was not supplied.

## Chunk 1 — Account and Contact relationship backend

- Added structured association and role records, guarded endpoints and an additive
  migration. Existing CRM records and generic relationships are retained.
- Main/authorised flags, work classification, dates and multiple roles are separate.
- Employer changes retain Contact identity and ended association history.
- No screen changes, portal permission grants or membership activation changes.
- Verification: 12 focused tests passed with no skips (AccountContactTests,
  CrmEndpointTests and PlatformMigrationTests) in Release with SQL Server LocalDB
  enabled. This includes additive migration replay and unique current-pair checks.
- Migration was reviewed and exercised only in isolated test databases; it has not
  been applied to the working development database. UI rollout is the next chunk.
- Detailed contract: ../stages/STAGE-03A-ACCOUNT-CONTACT-STRUCTURE.md.

## Planned next chunks

1. Account Contacts section with link-existing/edit/end actions.
2. Create-and-link Contact with duplicate suggestions.
3. Contact organisation/role/history view.
4. Application details and organisation/main-contact links.
5. Application review and corporate membership creation.

Each item may be split again to stay within the working limit. Later work covers
fee rules, scheme enrolment, compliance, renewal and communications after the core
application flow is usable.
