# Contact General Information

4 October 2026. Continues the migration-template Contact field delivery after the name, Department and Main Account slices.

## Field mapping

| CONTACT source | Staff CRM field | Storage |
| --- | --- | --- |
| B2 Person_ID | Source Person ID | ContactProfile.SourcePersonId |
| I2 Contact_Type | Contact type | ContactProfile.ContactType |
| N2 Nationality | Nationality | ContactProfile.Nationality |
| O2 Old_Reference | Old reference | ContactProfile.OldReference |
| P2 Region | Region | ContactProfile.Region |
| Q2 Branch | Branch | ContactProfile.Branch |
| R2 Remarks | Remarks | ContactProfile.Remarks |

The existing name fields cover C2:H2. J2 Contact_Status uses the existing record Status. Main Account, Department and Job Title (K2:M2) remain properties of the Contact's Account relationships, shown under Employers. Source Person ID records a legacy-system identifier independently of the internal CRM GUID; it does not automatically resolve related sheets or enforce source-system uniqueness. Contact type remains distinct from Category and membership type.

## Behaviour

The Contact detail/edit screen renders General Information below Personal Details. The existing profile save sends both name details and general information in one version-checked transaction. Existing permissions, archive guards, activity and data audit apply. Text is trimmed and blank values become null. General fields accept up to 160 characters without control characters; Remarks accepts 4000 characters including line breaks and tabs. No lookup vocabulary is inferred from sample values.

Staff GET `/api/v1/crm/records/{id}` returns a separate `generalInformation` object. PUT `/api/v1/crm/records/{id}/profile` accepts that optional object alongside `contactDetails`. An omitted or null object preserves existing general information; empty values within a supplied object clear fields. Account updates reject Contact general information. The existing Portal name contract is unchanged: staff general information is neither exposed nor accepted there, and Portal name updates retain staff fields.

## Migration and validation

`20261004074245_ContactGeneralInformation` adds seven nullable columns to `crm.ContactProfiles`; existing records are retained. Apply the migration through the normal development/release migration procedure before running the updated API against an existing database. Source changes and isolated tests do not update the shared development database.

API verification covers all seven fields, trimming, multiline remarks, stale writes, invalid lengths/control characters, Portal allowlists, older-client preservation, clearing, activity and archive protection. The SQL Server upgrade test creates its own database, inserts an existing Contact/profile before this migration, upgrades twice, checks the model snapshot and persists 4000-character Remarks while verifying tenant isolation; it deletes only its generated test database.

Verification completed: four focused API tests passed without skips (new General Information, existing structured names, organisation Portal projection and profile tenant/kind boundaries); one SQL Server upgrade test passed without skips. TypeScript, focused ESLint and frontend production build passed. The build emitted seven CSS optimisation warnings concerning generated variable expressions outside the new component. Browser appearance was not verified; no API or CRM listener was running on the configured local ports. The shared development database has not been migrated or restarted. Changes are uncommitted.

## Next Contact slices

1. Communication channel purpose and priority: primary/secondary email, mobile, telephone and fax — delivered in [Contact communication fields](CONTACT_COMMUNICATION_FIELDS.md).
2. Addresses: mailing/billing purpose, lines 3–4, County and same-address handling — delivered in [Contact address fields](CONTACT_ADDRESS_FIELDS.md).
3. Record-level communication preferences, suppression and bounce metadata — delivered in [Contact communication permissions](CONTACT_COMMUNICATION_PERMISSIONS.md), with enforcement through the existing marketing-recipient selector.
4. Social profiles and managed profile taxonomy.

Directory publication, restricted EDI detail and absent profile submodules retain the future-scope boundaries recorded in the migration-field review. Membership and finance details remain owned by their existing modules and require linked projections rather than copies of their data in Contact profiles.
