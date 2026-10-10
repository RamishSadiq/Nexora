# Contact address fields

4 October 2026. Implements the CONTACT Addresses section using related address records, shared with Account records.

## Workbook mapping

| CONTACT source group | CRM representation |
| --- | --- |
| AI2:AS2 Mailing address | Address with Purpose=mailing; address type, account name, job title, postcode, lines 1–4, town/city, county and country |
| AT2 Mailing_And_Billing_Address_Are_Same | CrmRecord.BillingUsesMailingAddress |
| AU2:BE2 Billing address | Address with Purpose=billing and the same structured fields |
| BF2:BM2 Other address | Address with Purpose=other |
| BN2:BU2 Other 2 address | A second Address with Purpose=other |

The existing Region field is retained independently of County. The old City and PostalCode storage properties are labelled Town / City and Postcode in the editor. Address Type is separate from Purpose and accepts user-entered values; no work/home vocabulary is inferred from samples. Account name and job title are address presentation fields, independent of employer relationships. Numbered Other source groups become separate records, with additional Other addresses supported under the existing 100-item limit.

## Mailing and billing source

There is at most one Mailing and one Billing address per record, enforced by a filtered unique tenant/record/purpose index. Existing addresses default to Other; labels, contents and consent-independent address meaning are retained without guessing purposes. The ordinary tenant/record lookup index remains.

Billing same as mailing is a live reference to the current Mailing address, resolved by the staff detail endpoint as `effectiveBillingAddress`; no copied address is created. Mailing edits immediately affect that projection. A separately saved Billing address is retained while the flag is enabled and restored as the effective source when the flag is disabled. Enabling requires a Mailing address. Staff must disable the flag before changing/removing the Mailing purpose or editing/adding a separate Billing address.

The address section groups Mailing, Billing and Other addresses and renders all four lines plus County and Region. Edit mode supports creating and updating rows, toggling the source and confirmed removal. The source flag is visible in read mode. The billing projection offers an Edit mailing address action while linked.

## API and Portal

POST `/api/v1/crm/records/{id}/addresses` keeps previous clients compatible: new fields are optional, Purpose defaults to Other and the optional parent Version is checked when supplied. PUT `/api/v1/crm/records/{id}/addresses/{addressId}` requires the current parent Version and replaces that address's values. PUT `/api/v1/crm/records/{id}/billing-address` accepts Version and UseMailingAddress. Tenant, permission, parent ownership and archive checks apply; saves update the parent version, activity and data audit. Existing generic address removal protects a Mailing row used for billing.

Portal retains its existing six-field correspondence contract and allowlist. It updates the existing correspondence row instead of replacing it, preserving staff-only fields and purpose. Clearing a correspondence row used as linked Mailing is rejected until staff changes the billing source. These changes prevent Portal saves from discarding extended address fields or breaking the billing link. Portal editing of the other new address fields is not added by this slice.

## Migration and remaining scope

`20261004080156_AddressDetailsAndBillingSource` adds the source flag and seven address properties: Purpose, AddressType, AccountName, JobTitle, Line3, Line4 and County. Existing data remains. Apply it after Communication Priority through the normal migration procedure before running the new API against an existing database. This work does not migrate the shared development database.

Activity/data audit records edits; effective-dated address history and ceased-address import require a separate future slice. Directory publication remains separate from address storage. [Contact communication permissions](CONTACT_COMMUNICATION_PERMISSIONS.md) implements the next CONTACT CH2:CO2 fields.

## Verification

Five API tests passed without skips across the focused runs: new address-detail/billing-source coverage, existing Contact/Account lifecycle, new Portal preservation coverage, existing postal-address allowlist/clearing coverage, and Portal cookie/profile concurrency. One isolated SQL Server upgrade test passed without skips, checking preservation of old address values, default Other purpose, separate County and Region, all new fields, one Mailing/Billing slot, multiple Other addresses, persisted source flag, tenant isolation, repeated migration and no pending model changes. TypeScript, focused ESLint and frontend production build passed. The build retained the seven generated-CSS optimisation warnings seen before this slice. Live browser appearance was not verified; no API listener was available on 5097 at the final check. The shared development database has not been migrated; changes are uncommitted.
