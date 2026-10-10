"use client";

export type ContactCommunicationPermissions = { thirdPartyEmailPermission: boolean | null; disclosureDataToThirdParty: boolean | null; thirdPartyDisclosureConsentDate: string | null; typeOfThirdParty: string | null; stopAllCommunication: boolean | null; dateStopAllCommunication: string | null; bouncedInvalidEmailAddress: boolean | null; dateEmailBounced: string | null };
export const emptyCommunicationPermissions: ContactCommunicationPermissions = { thirdPartyEmailPermission: null, disclosureDataToThirdParty: null, thirdPartyDisclosureConsentDate: null, typeOfThirdParty: null, stopAllCommunication: null, dateStopAllCommunication: null, bouncedInvalidEmailAddress: null, dateEmailBounced: null };
const permissionFields = [["thirdPartyEmailPermission", "Third-party email permission"], ["disclosureDataToThirdParty", "Permission to disclose data to third parties"], ["stopAllCommunication", "Stop all communication"], ["bouncedInvalidEmailAddress", "Bounced / invalid email address"]] as const;
const dateFields = [["thirdPartyDisclosureConsentDate", "Third-party disclosure consent date"], ["dateStopAllCommunication", "Date communication stopped"], ["dateEmailBounced", "Date email bounced"]] as const;

export function permissionPayload(value: ContactCommunicationPermissions): ContactCommunicationPermissions {
  return { ...value, thirdPartyDisclosureConsentDate: value.thirdPartyDisclosureConsentDate || null, dateStopAllCommunication: value.dateStopAllCommunication || null, dateEmailBounced: value.dateEmailBounced || null };
}

export function ContactPermissionFields({ value, onChange, disabled, inputClass, hideLegend = false }: { value: ContactCommunicationPermissions; onChange: (value: ContactCommunicationPermissions) => void; disabled: boolean; inputClass: string; hideLegend?: boolean }) {
  return <fieldset disabled={disabled} className="grid gap-4 sm:grid-cols-2">
    <legend className={hideLegend ? "sr-only" : "mb-3 font-semibold"}>Communication Permissions</legend>
    {permissionFields.map(([key, label]) => <label key={key} className="block text-sm">{label}<select className={inputClass} value={value[key] == null ? "unknown" : value[key] ? "yes" : "no"} onChange={e => onChange({ ...value, [key]: e.target.value === "unknown" ? null : e.target.value === "yes" })}><option value="unknown">Unknown</option><option value="yes">Yes</option><option value="no">No</option></select></label>)}
    <label className="block text-sm">Type of third party<input className={inputClass} maxLength={160} value={value.typeOfThirdParty ?? ""} onChange={e => onChange({ ...value, typeOfThirdParty: e.target.value })} /></label>
    {dateFields.map(([key, label]) => <label key={key} className="block text-sm">{label}<input className={inputClass} type="date" value={value[key] ?? ""} onChange={e => onChange({ ...value, [key]: e.target.value || null })} /></label>)}
    <p className="text-sm text-slate-600 sm:col-span-2">Stopped communication or a bounced / invalid email excludes this contact from marketing emails. Email consent and evidence are managed on each communication method. Leave dates blank when unknown.</p>
  </fieldset>;
}
