"use client";
import { useState } from "react";
import { ContactNameFields, emptyContactDetails, type ContactDetails, type AccountDetails } from "../shared/profile-fields";

export function RecordProfile({ kind, contactDetails, accountDetails, editable, busy, onSave }: { kind: string; contactDetails: ContactDetails | null; accountDetails: AccountDetails | null; editable: boolean; busy: boolean; onSave: (details: { contactDetails: ContactDetails } | { accountDetails: AccountDetails }) => void }) {
  const [contact, setContact] = useState(contactDetails ?? emptyContactDetails);
  const [account, setAccount] = useState<AccountDetails>(accountDetails ?? { fullName: "", tradingAs: "", dateBusinessCommenced: "" });
  const input = "mt-2 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-slate-900 disabled:bg-slate-50";
  return <section aria-label={kind === "contact" ? "Personal name details" : "Organisation details"} className="rounded-xl border border-slate-200 bg-white p-5">
    <form className="space-y-4" onSubmit={e => { e.preventDefault(); onSave(kind === "contact" ? { contactDetails: contact } : { accountDetails: { ...account, dateBusinessCommenced: account.dateBusinessCommenced || null } }); }}>
      {kind === "contact" ? <ContactNameFields value={contact} onChange={setContact} disabled={!editable || busy} inputClass={input} /> : <fieldset disabled={!editable || busy} className="grid gap-4 sm:grid-cols-2"><legend className="mb-3 font-semibold">Organisation details</legend>
        {([["fullName", "Full organisation name"], ["tradingAs", "Trading as"], ["dateBusinessCommenced", "Business commencement date"]] as const).map(([key, label]) => <label key={key} className="block text-sm">{label}<input className={input} type={key === "dateBusinessCommenced" ? "date" : "text"} maxLength={160} value={account[key] ?? ""} onChange={e => setAccount({ ...account, [key]: e.target.value })} /></label>)}
      </fieldset>}
      <p className="text-sm text-slate-600">These optional details are shared with the member portal. The record display name is managed separately.{kind === "account" && " Add websites under Communication methods."}</p>
      {editable && <button disabled={busy} className="rounded-lg bg-indigo-700 px-4 py-2 text-sm font-semibold text-white disabled:opacity-50">Save {kind === "contact" ? "name" : "organisation"} details</button>}
    </form>
  </section>;
}
