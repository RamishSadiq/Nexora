# Contact communication fields

4 October 2026. Implements the Emails and Mobile, Telephone and Fax sections of the Contact migration template using related communication records.

| CONTACT source fields | Communication kind | Designation |
| --- | --- | --- |
| S2 Primary_Email | email | primary |
| T2 Secondary_Email | email | secondary |
| U2:Y2 Other_Email_1 through Other_Email_5 | email | other, separate records |
| Z2 Mobile | mobile | independently designated |
| AA2 Primary_Telephone | phone | primary |
| AB2 Secondary_Telephone | phone | secondary |
| AC2:AG2 Other_Telephone_1 through Other_Telephone_5 | phone | other, separate records |
| AH2 Fax | fax | independently designated |

Website entries remain available for existing Account/Contact usage. The same communication storage and controls serve both record kinds. Numbered source columns map to child records rather than fixed numbered database columns; this delivery does not import the workbook.

## Storage and behaviour

`CommunicationMethod.Priority` accepts primary, secondary or other. There can be one primary and one secondary record for each communication kind within a Contact/Account. Multiple Other records remain supported under the existing limit of 100 communication entries. A filtered unique tenant/record/kind/priority index enforces the designated slots. The ordinary tenant/record lookup index is preserved.

Existing entries default to Other during migration: no preferred address or telephone is inferred. Staff can add and edit entries with the existing consent/evidence fields. To replace a primary or secondary entry, edit the current entry or first change its designation to Other. Invalid email addresses and non-HTTP(S) websites are rejected. Granted or denied consent requires evidence. Changing designation does not grant consent.

The detail page groups entries under Emails; Mobile, Telephone and Fax; and Websites. Designation, channel and consent are shown in view mode. Edit mode offers creation, editing and confirmed removal. Save refreshes the record and clears the editor only after a successful update/reload.

POST `/api/v1/crm/records/{id}/communications` accepts optional `priority` (default Other) and optional parent `version`, preserving previous clients. The new UI supplies the version. PUT `/api/v1/crm/records/{id}/communications/{communicationId}` requires the current parent version and replaces that entry's value, kind, designation and consent fields. Staff CRM manage permission, tenant boundaries, parent ownership and archive protection apply. Saves update the parent version and append activity/data audit. Existing removal uses the established child-removal route.

The consented marketing-email selector prefers an eligible Primary entry, then Secondary, then Other. Its existing rule that any denied email suppresses delivery remains in force. Portal profile permissions and communication exposure are unchanged.

The existing CSV export's single email column uses Primary, then Secondary, then Other. Its format and consent-preserving import behaviour are unchanged.

## Migration and remaining scope

`20261004074946_CommunicationPriority` adds the designation with default Other and its unique index. Apply it after Contact General Information through the normal migration procedure before running the new API against an existing database. The shared development database is not updated by isolated tests.

Record-level stop-all-communication, third-party permissions and bounce metadata are the next communication-preferences slice. [Address fields](CONTACT_ADDRESS_FIELDS.md) have now been implemented.

## Verification

15 existing API regression checks passed in the broad CRM run. The new communication test passed on its focused rerun and again after adding the CSV preferred-email assertion. It covers legacy default designation, all telephone channels, duplicate slots, edit/consent validation, stale saves, permission denial, wrong parent, archive protection and activity. The SQL Server upgrade test passed without skips, checking legacy values and consent, both unique slots, multiple Other entries, tenant isolation, repeated migration, model alignment, preferred marketing-email selection and denied-consent suppression. TypeScript, focused ESLint and the frontend production build passed; the same seven generated-CSS optimisation warnings seen in the preceding slice remain. No browser appearance check was completed because the local API/CRM listeners were absent. The shared development database has not been migrated; changes are uncommitted.
