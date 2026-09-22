# Account and Contact relationship structure

## Purpose

This chunk implements the backend foundation for the organisation/contact
relationships in BPCA CoR sections 3.1–3.2. It supports the later application flow:
find or create a person, identify the organisation and main contact, submit an
application, and create membership after approval.

The source comparison is in [CRM alignment](../design/CRM-COR-ALIGNMENT.md).
This slice does not yet add Account/Contact screens or change membership approval.

## Records and fields

`AccountContact` links one existing Account to one existing Contact in the same
Nexora tenant. Both must have the correct CRM kind. Its fields are:

| Field | Meaning |
| --- | --- |
| AccountId / ContactId | Stable organisation/person references |
| JobTitle | Optional role title, up to 160 characters |
| WorkClassification | unknown, technical or non-technical |
| IsMainContact | Main-contact designation for this organisation |
| IsAuthorisedContact | Designation only; grants no login or portal permissions |
| StartDate | Actual date the relationship started; not a future scheduled change |
| EndDate / EndReason | Date and reason recorded when ending the relationship |
| Version | Token used to reject stale updates |
| CreatedAtUtc / UpdatedAtUtc | Operational timestamps |

`AccountContactRole` stores multiple distinct role codes per association. The
initial supported list is assessment, consultancy, fumigation,
manufacturers-distributors, pr, servicing, training, events, finance, health-safety,
legal, membership, sales-marketing, specifier and technical.

Main-contact designation is separate from that role list. Technical work
classification is also separate from the technical-contact communication role.
The options endpoint publishes the initial list; tenant-editable role definitions
and contact types are later work. The source's "Assessment contract" label is
interpreted as assessment contact for this initial code list.

## Behaviour and decisions

- An Account/Contact pair can have one current (no EndDate) association. A database
  unique index protects against duplicate creation, including concurrent requests.
- Linking the same current pair returns 409, allowing a client to reload rather
  than creating another row. This is duplicate protection, not response replay.
- Start date, Account and Contact are immutable after creation. Changing employer
  means ending the old relationship and creating another against the same Contact.
- Ended association rows cannot be updated or deleted. Their role assignments are
  retained through the public API. There is no delete endpoint.
- An end action requires a reason and a date between start date and today.
- Rejoining creates a new association. Ending a relationship does not archive the
  Contact or change their qualifications, CPD, membership or login identity.
- Several current organisation associations and several main contacts are
  permitted for now. The CoR does not settle those cardinality restrictions; no
  exclusive-employer or single-main-contact constraint has been guessed.
- A record may have no organisation association. Main-contact completeness is a
  future membership/application validation rule, not a global CRM creation rule.
- Existing generic relationships are preserved. No free-text link is automatically
  converted into a main or authorised Contact.
- Account and Contact views will query the same association, avoiding separately
  maintained forward/reverse links.

## API

All routes are under `/api/v1/crm`; reads require crm.read. Writes also require
crm.manage and the existing CSRF token. There is no public/member-portal endpoint.

| Method and path | Contract |
| --- | --- |
| GET /account-contact-options | Supported role codes and work classifications |
| GET /records/{id}/account-contacts | Works for Account or Contact; page defaults to 1, size 25; includeEnded defaults false |
| POST /accounts/{accountId}/contacts | ContactId, StartDate, JobTitle, WorkClassification, IsMainContact, IsAuthorisedContact, Roles |
| PATCH /account-contacts/{id} | Full editable fields: Version, JobTitle, WorkClassification, IsMainContact, IsAuthorisedContact, Roles |
| POST /account-contacts/{id}/end | Version, EndDate, Reason |

Create/update/end return the association. The list returns
`{ items, totalCount, page, pageSize }`; each item contains the association,
accountName, contactName, accountArchived, contactArchived and roles.

Dates are stored as UTC midnight dates. The list uses offset pagination and stable
name/ID ordering; it is not a snapshot across concurrent changes. Updating roles
replaces the supplied role set. Omitted or unsupported role lists are rejected;
an empty list is valid. Boolean designations must be sent deliberately by clients.

Both parent CRM concurrency tokens participate in writes to detect concurrent
archive/edit. Creating/updating links requires unarchived parent records. Ending
an existing relationship is permitted even when a parent is archived, preserving
an explicit way to record departure.

Tenant filters and composite foreign keys cover associations and roles. Persistence
also validates parent kinds/tenants, immutable association references, dates and
work classification. Each API change appends an activity to BOTH parent records,
with actor, timestamp, action, association ID and request correlation. This is an
action audit; it does not yet retain every prior job-title or role-set snapshot.

## Migration and rollout

Migration: `20260915195336_AccountContactAssociations` in CrmDbContext.
It adds only `crm.AccountContacts`, `crm.AccountContactRoles`, keys and indexes.
No existing CRM records or free-text relationships are transformed or removed.

Apply through the existing controlled CRM migration process before enabling these
endpoints against an existing database. Production startup does not migrate it.
Local test databases are isolated and removed after verification. The development
working database has not been migrated as part of this chunk.

## Verification

Focused tests cover shared reads, multiple roles, updates, stale requests,
duplicate links, ending/rejoining, audit events, invalid kinds/dates/roles,
archived records, CSRF, permissions, tenant boundaries and persistence guards.
A SQL Server test applies/replays migrations and checks the unique current-pair
constraint while retaining historical rows.

Verification on 2026-09-15: 12 tests passed, none skipped, using Release and a
LocalDB NEXORA_TEST_SQLSERVER setting. The filter included AccountContactTests,
CrmEndpointTests and PlatformMigrationTests. The last suite also proves all seven
module migration histories can replay in one isolated SQL Server catalog with
no pending model changes. The build succeeded. Frontend/browser checks were not
run because this chunk does not change the frontend.

## Next chunk

Add the Account's Contacts section using this API: list linked people and roles,
link an existing Contact, edit an association and end it with a reason. Contact
creation and duplicate suggestions follow in another small chunk. Membership
application changes come after this relationship flow is usable.
