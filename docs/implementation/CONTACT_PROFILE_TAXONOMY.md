# Contact Profile taxonomy and assignments

4 October 2026. Implements CONTACT DE2:DI2 (`Profile_1` through `Profile_5`) and the shared two-level definitions represented by Profile List B3:C10 (`Parent_Node`, `Child_Node`). The same assignments support ACCOUNT CZ2:DD2 without duplicating the taxonomy. No workbook data has been imported or seeded.

## Model and meaning

`ProfileDefinition` stores a parent/child pair independently from its record assignments. This slice uses the template's two-level hierarchy; it does not infer recursive levels. Child names are unique within a parent and tenant, rather than globally unique. Names are trimmed, limited to 160 characters and reject control characters. Uppercase comparison keys and a unique SQL index enforce case-insensitive parent/child identity. The same child name may appear under different parents. Renaming a definition changes its displayed label on all assigned records while preserving its identity.

`RecordProfileAssignment` links a Contact or Account to a definition using a numbered slot from 1 to 5. Empty slots remain empty, including gaps between populated slots. SQL enforces slot bounds, one assignment per record/slot and tenant-bound foreign keys to both the record and definition. Domain validation additionally rejects duplicate definitions within a record. Updating the definition reference on an existing slot allows swapping selections without conflicting with the slot index. Existing flat `RecordTag` values remain separate and unchanged; Contact Category and Membership Category remain separate concepts.

Definitions can be archived and restored, with concurrency-checked updates. Archive retains existing assignments and their labels. An archived definition may remain in its existing slot or be removed; it cannot be newly assigned or moved to another slot. Definitions cannot be deleted through this feature. Record archival prevents assignment changes. Parent record and slot identity are immutable on existing assignment rows.

## API and interface

`GET /api/v1/crm/profile-definitions` returns tenant definitions, including archived definitions. `POST` creates a definition; `PUT /profile-definitions/{definitionId}` requires its current version and supports rename/archive/restore. These writes require `crm.manage`; reads require `crm.read`. Definition changes use the existing data audit infrastructure.

Record detail returns a `profiles` array ordered by slot, with definition IDs, parent/child labels and archive status. `PUT /records/{id}/profiles` requires the current record version and an explicit `assignments` array of `{ slot, definitionId }`. It replaces the complete five-slot selection. An empty array clears it; a missing/null array is rejected. Stale saves conflict. Unavailable and foreign-tenant definitions are rejected. Saves update record version/time, activity (`profiles.updated`) and assignment data audit. Name/profile updates through older staff clients and Portal leave these assignments unchanged. Portal does not expose or accept them.

The Contact and Account detail screens have five Profile selectors grouped by parent. A collapsible Manage profile definitions area allows creation, rename, archive and restoration. Assigned archived definitions remain visible. Record search also matches assigned parent and child names, alongside the existing tag search.

## Migration and verification

`20261004083944_ProfileTaxonomy` creates ProfileDefinitions, ProfileAssignments, their constraints/indexes and data audit tables. It preserves existing Contacts, Accounts, profiles and tags. This task has not migrated the shared development database; apply this migration through the normal migration procedure before using the feature against an existing database.

Two API tests and one isolated SQL Server migration test passed, with no skipped tests. Coverage includes parent-scoped uniqueness, trimming/invalid names, sparse slots, swaps, duplicate and invalid selections, concurrency, renaming/archive retention, search, clearing, Portal isolation, permissions, Account reuse, tenant boundaries, repeat migration, existing-tag preservation and SQL slot/foreign-key enforcement. TypeScript, focused ESLint and the production build passed; the build retained seven existing generated CSS warnings. Authenticated browser appearance remains unverified following the preceding unavailable local-sign-in check. Source changes remain uncommitted.

The following [Contact membership projection and field mapping](CONTACT_MEMBERSHIP_PROJECTION.md) now supplies the detailed view of existing Membership data and records its remaining gaps. Restricted EDI and public Directory publication remain documented future scope.
