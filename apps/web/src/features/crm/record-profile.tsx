"use client";
import { useState } from "react";
import { AccountGeneralFields, emptyAccountGeneralInformation, type AccountGeneralInformation } from "./account-general-information";
import { ContactNameFields, emptyContactDetails, type ContactDetails, type AccountDetails } from "../shared/profile-fields";
import { ContactGeneralFields, emptyGeneralInformation, type ContactGeneralInformation } from "./contact-general-information";
import { ContactPermissionFields, emptyCommunicationPermissions, permissionPayload, type ContactCommunicationPermissions } from "./contact-communication-permissions";
import { ContactSocialFields, emptySocialMedia, type ContactSocialMediaDetails } from "./contact-social-media";
import { ConfiguredSections } from "../shared/configured-sections";
import { RecordSection } from "./record-section";
import { SectionAttributes } from "../shared/record-attribute-sections";
import { RecordPhotoField } from "./record-photo";
import type { RecordRow } from "./types";

export function RecordProfile({ kind, contactDetails, generalInformation, communicationPermissions, socialMediaDetails, accountDetails, accountGeneralInformation, editable, busy, onSave, photoRecord, canReadPhoto, onPhotoChanged }: { photoRecord?: RecordRow; canReadPhoto?: boolean; onPhotoChanged?: () => void; kind: string; contactDetails: ContactDetails | null; generalInformation?: ContactGeneralInformation | null; communicationPermissions?: ContactCommunicationPermissions | null; socialMediaDetails?: ContactSocialMediaDetails | null; accountDetails: AccountDetails | null; accountGeneralInformation?: AccountGeneralInformation | null; editable: boolean; busy: boolean; onSave: (details: { contactDetails: ContactDetails; generalInformation: ContactGeneralInformation; communicationPermissions: ContactCommunicationPermissions; socialMediaDetails: ContactSocialMediaDetails } | { accountDetails: AccountDetails; accountGeneralInformation: AccountGeneralInformation }) => void }) {
  const [uploading, setUploading] = useState(false);
  const pending = busy || uploading;
  const [contact, setContact] = useState(contactDetails ?? emptyContactDetails);
  const [general, setGeneral] = useState(generalInformation ?? emptyGeneralInformation);
  const [communication, setCommunication] = useState(communicationPermissions ?? emptyCommunicationPermissions);
  const [social, setSocial] = useState(socialMediaDetails ?? emptySocialMedia);
  const [accountGeneral, setAccountGeneral] = useState(accountGeneralInformation ?? emptyAccountGeneralInformation);
  const [account, setAccount] = useState<AccountDetails>(accountDetails ?? { fullName: "", tradingAs: "", dateBusinessCommenced: "" });
  const hasChanges = JSON.stringify([contact, general, communication, social]) !== JSON.stringify([contactDetails ?? emptyContactDetails, generalInformation ?? emptyGeneralInformation, communicationPermissions ?? emptyCommunicationPermissions, socialMediaDetails ?? emptySocialMedia]);
  const input = "mt-2 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-slate-900 disabled:bg-slate-50";
  return <section aria-label={kind === "contact" ? "Personal Details" : "Organisation details"} className="rounded-xl border border-slate-200 bg-white p-5">
    <form className="space-y-4" onSubmit={e => { e.preventDefault(); onSave(kind === "contact" ? { contactDetails: contact, generalInformation: general, communicationPermissions: permissionPayload(communication), socialMediaDetails: social } : { accountDetails: { ...account, dateBusinessCommenced: account.dateBusinessCommenced || null }, accountGeneralInformation: accountGeneral }); }}>
      <ConfiguredSections module="crm" kind={kind} className="space-y-4">
      {kind === "contact" ? <div data-section="Personal Details"><ContactNameFields value={contact} onChange={setContact} disabled={!editable || pending} inputClass={input} /><SectionAttributes section="Personal Details" /></div> : <fieldset data-section="Organisation name" disabled={!editable || pending} className="grid gap-4 sm:grid-cols-2"><legend className="mb-3 font-semibold">Organisation details</legend>
        {([["fullName", "Full organisation name"], ["tradingAs", "Trading as"], ["dateBusinessCommenced", "Business commencement date"]] as const).map(([key, label]) => <label key={key} className="block text-sm">{label}<input className={input} type={key === "dateBusinessCommenced" ? "date" : "text"} maxLength={160} value={account[key] ?? ""} onChange={e => setAccount({ ...account, [key]: e.target.value })} /></label>)}
        <SectionAttributes section="Organisation name" />
      </fieldset>}
      {kind === "account" && <div data-section="General Information"><AccountGeneralFields value={accountGeneral} onChange={setAccountGeneral} disabled={!editable || pending} inputClass={input} /><SectionAttributes section="General Information" /></div>}
      {kind === "contact" && <RecordSection data-section="General Information" title="General Information" keepMounted><ContactGeneralFields hideLegend value={general} onChange={setGeneral} disabled={!editable || pending} inputClass={input} imageField={photoRecord && canReadPhoto && onPhotoChanged ? <RecordPhotoField record={photoRecord} disabled={!editable || pending} hasChanges={hasChanges} onChanged={onPhotoChanged} onBusyChange={setUploading} /> : undefined} /></RecordSection>}
      {kind === "contact" && <RecordSection title="Social Media Details" keepMounted><ContactSocialFields hideLegend value={social} onChange={setSocial} disabled={!editable || pending} inputClass={input} /></RecordSection>}
      {kind === "contact" && <RecordSection title="Communication Permissions" keepMounted><ContactPermissionFields hideLegend value={communication} onChange={setCommunication} disabled={!editable || pending} inputClass={input} /></RecordSection>}
      </ConfiguredSections>
      {kind === "account" && <p className="text-sm text-slate-600">The organisation’s name and business commencement details also appear in the member portal. Additional Account general information is staff-only. Its display name is maintained separately, and its websites are listed under Communication Methods.</p>}
      {editable && <button disabled={pending} className="rounded-lg bg-indigo-700 px-4 py-2 text-sm font-semibold text-white disabled:opacity-50">Save {kind === "contact" ? "contact" : "organisation"} details</button>}
    </form>
  </section>;
}
