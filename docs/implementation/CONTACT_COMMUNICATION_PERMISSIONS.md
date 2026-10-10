# Contact communication permissions

4 October 2026. Implements the eight CONTACT Communication Permissions fields in CH2:CO2.

| Template source | ContactProfile property | Type |
| --- | --- | --- |
| CH2 Third_Party_Email_Permission | ThirdPartyEmailPermission | Nullable Boolean |
| CI2 Disclosure_Data_To_Third_Party | DisclosureDataToThirdParty | Nullable Boolean |
| CJ2 Third_Party Disclosure_Consent_Date | ThirdPartyDisclosureConsentDate | Nullable calendar date |
| CK2 Type_Of_Third_Party | TypeOfThirdParty | Optional text |
| CL2 Stop_All_Communication | StopAllCommunication | Nullable Boolean |
| CM2 Date_Stop_All_Communication | DateStopAllCommunication | Nullable calendar date |
| CN2 Bounced_Invalid_Email_Address | BouncedInvalidEmailAddress | Nullable Boolean |
| CO2 Date_Email_Bounced | DateEmailBounced | Nullable calendar date |

## Representation and validation

The Contact view/edit form adds a Communication Permissions section. Each flag has Unknown, Yes and No states. Existing records retain Unknown on migration, avoiding an invented grant or refusal. Dates are optional because the workbook does not define mandatory-date rules. They accept valid dates on or before the current UTC date; dates have no time-zone conversion. Changing a flag does not automatically invent or erase its recorded date. Staff may explicitly clear dates. Third-party type is trimmed, allows up to 160 characters without control characters and becomes null when blank.

Staff detail responses expose a separate `communicationPermissions` object. The existing version-checked profile update accepts the optional object alongside Contact name details. An omitted/null object preserves stored permissions; a supplied object replaces its fields, so null means Unknown/unset. Account updates reject this Contact-specific object. CRM manage/read permissions, tenant/profile integrity, archive guards, activity and data audit apply. Portal's existing profile contract and allowlist exclude these staff fields; Portal and older staff name updates retain them.

## Communication behaviour

The existing marketing-email selector returns no email while StopAllCommunication or BouncedInvalidEmailAddress is Yes. This affects campaign audience preparation and workflow actions that resolve recipients through `crm.contact.inspect-email`. Existing granted-consent/evidence requirements, denied-email suppression, and primary/secondary preference still apply. Clearing these flags does not override missing or denied email consent.

Third-party email permission and disclosure permission are distinct stored requirements; they do not grant first-party email consent or initiate disclosure. A generic type/date does not replace the consent evidence on a communication-method record. Third-party distribution is not implemented by this field slice.

The current integration checks permissions when selecting a marketing recipient. It does not revalidate already-prepared campaign snapshots or already-resolved workflow recipients at transport time. Direct-recipient email actions and service/security communications have separate routing and remain outside this selector. A transport-wide communication policy and service-message exceptions require a separate scoped delivery change before claiming universal Stop All Communication enforcement.

## Migration and next scope

`20261004081534_ContactCommunicationPermissions` adds eight nullable columns to `crm.ContactProfiles`, preserving all existing profile and channel data. Apply it after Address Details and Billing Source through the normal migration procedure before using the updated API with an existing database. Isolated tests do not migrate the shared development database.

[Contact Social Media Details](CONTACT_SOCIAL_MEDIA_FIELDS.md) implements the next field slice. Managed Profile taxonomy follows it. Restricted EDI and directory publication retain the future-scope boundaries in the migration-field review.

## Verification

Four focused API tests passed without skips: new permission round-trip/validation/clearing/staff-boundary coverage, General Information regression, structured name/Portal regression and Portal cookie/profile concurrency. The isolated SQL Server upgrade test passed without skips, checking legacy Unknown values, date persistence, flag transitions, marketing suppression/restoration, unchanged per-email consent evidence, unknown/denied consent, tenant isolation, repeated migration and model alignment. TypeScript, focused ESLint and the frontend production build passed. The build retained the seven generated-CSS optimisation warnings from the preceding slices. A live browser attempt reached the login page, where Local sign-in is unavailable and Microsoft SSO is not connected, so authenticated Contact appearance/save behaviour remains unverified. This task has not applied the migration to the shared development database. Changes remain uncommitted.
