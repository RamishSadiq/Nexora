"use client";

import { useState, type FormEvent } from "react";
import type { Item } from "./types";

const fields = [["label", "Label", 80, true], ["addressType", "Address type", 80, false], ["accountName", "Account name", 160, false], ["jobTitle", "Job title", 160, false], ["line1", "Address line 1", 200, true], ["line2", "Address line 2", 200, false], ["line3", "Address line 3", 200, false], ["line4", "Address line 4", 200, false], ["city", "Town / City", 100, true], ["county", "County", 100, false], ["region", "Region", 100, false], ["postalCode", "Postcode", 30, true], ["country", "Country", 80, true]] as const;
export type AddressValues = { [K in typeof fields[number][0]]: string } & { purpose: string };
const empty: AddressValues = { purpose: "other", label: "", addressType: "", accountName: "", jobTitle: "", line1: "", line2: "", line3: "", line4: "", city: "", county: "", region: "", postalCode: "", country: "" };
const input = "mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 focus:outline-none focus:ring-2 focus:ring-indigo-500";
const button = "rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-800 hover:bg-slate-50 disabled:opacity-50";

export function RecordAddresses({ items, billingUsesMailingAddress, effectiveBillingAddress, editable, busy, onSave, onRemove, onBillingSource }: { items: Item[]; billingUsesMailingAddress: boolean; effectiveBillingAddress?: Item | null; editable: boolean; busy: boolean; onSave: (values: AddressValues, id?: string) => void; onRemove: (item: Item) => void; onBillingSource: (useMailing: boolean) => void }) {
  const [editingId, setEditingId] = useState<string>();
  const [values, setValues] = useState<AddressValues>(empty);
  function edit(item: Item) {
    setEditingId(item.id);
    setValues({ purpose: item.purpose ?? "other", label: item.label ?? "", addressType: item.addressType ?? "", accountName: item.accountName ?? "", jobTitle: item.jobTitle ?? "", line1: item.line1 ?? "", line2: item.line2 ?? "", line3: item.line3 ?? "", line4: item.line4 ?? "", city: item.city ?? "", county: item.county ?? "", region: item.region ?? "", postalCode: item.postalCode ?? "", country: item.country ?? "" });
  }
  function submit(event: FormEvent<HTMLFormElement>) { event.preventDefault(); onSave(values, editingId); }
  const mailing = items.find(item => item.purpose === "mailing");
  return <div className="space-y-5">
    <label className="flex items-center gap-2 text-sm font-medium"><input type="checkbox" checked={billingUsesMailingAddress} disabled={!editable || busy || (!mailing && !billingUsesMailingAddress)} onChange={e => onBillingSource(e.target.checked)} />Mailing and billing addresses are the same</label>
    {!mailing && <p className="text-xs text-slate-600">Add or designate a mailing address to use it for billing.</p>}
    {(["mailing", "billing", "other"] as const).map(purpose => {
      const linkedBilling = purpose === "billing" && billingUsesMailingAddress;
      const rows = linkedBilling ? (effectiveBillingAddress ? [effectiveBillingAddress] : []) : items.filter(item => (item.purpose ?? "other") === purpose);
      const title = purpose === "other" ? "Other addresses" : `${purpose[0].toUpperCase() + purpose.slice(1)} address`;
      return <section key={purpose} aria-label={title}><h3 className="mb-2 text-sm font-semibold text-slate-900">{title}</h3>
        {linkedBilling && <p className="mb-2 text-xs text-slate-600">Uses the mailing address and follows its updates. Turn off the setting to use a separate billing address.</p>}
        {rows.length === 0 ? <p className="text-sm text-slate-500">No {title.toLowerCase()} recorded.</p> : <ul className="space-y-2">{rows.map(item => <li key={item.id} className="flex flex-wrap items-start justify-between gap-3 rounded-lg bg-slate-50 p-3 text-sm">
          <div className="min-w-0 break-words"><p className="font-medium">{item.label}{item.addressType ? ` · ${item.addressType}` : ""}</p><address className="mt-1 not-italic">{[item.accountName, item.jobTitle, item.line1, item.line2, item.line3, item.line4, item.city, item.county, item.region, item.postalCode, item.country].filter(Boolean).map((line, index) => <span key={index} className="block">{line}</span>)}</address></div>
          {editable && <div className="flex gap-2"><button type="button" className={button} disabled={busy} aria-label={`Edit ${linkedBilling ? "mailing address used for billing" : item.label}`} onClick={() => edit(item)}>{linkedBilling ? "Edit mailing address" : "Edit"}</button>{!linkedBilling && <button type="button" className={button} disabled={busy || (item.purpose === "mailing" && billingUsesMailingAddress)} aria-label={`Remove ${item.label}`} onClick={() => onRemove(item)}>Remove</button>}</div>}
        </li>)}</ul>}
      </section>;
    })}
    {editable && <form className="space-y-3 border-t border-slate-200 pt-4" onSubmit={submit}>
      <h3 className="text-sm font-semibold">{editingId ? "Edit address" : "Add address"}</h3>
      <fieldset disabled={busy} className="grid gap-3 sm:grid-cols-2">
        <label className="block text-sm sm:col-span-2">Purpose<select className={input} value={values.purpose} onChange={e => setValues({ ...values, purpose: e.target.value })}>{["mailing", "billing", "other"].map(purpose => <option key={purpose} value={purpose} disabled={billingUsesMailingAddress && (purpose === "billing" || (values.purpose === "mailing" && purpose !== "mailing"))}>{purpose[0].toUpperCase() + purpose.slice(1)}</option>)}</select></label>
        {fields.map(([key, label, limit, required]) => <label key={key} className="block text-sm">{label}<input className={input} maxLength={limit} required={required} value={values[key]} onChange={e => setValues({ ...values, [key]: e.target.value })} /></label>)}
      </fieldset>
      <div className="flex gap-2"><button className={button} disabled={busy}>{editingId ? "Save address" : "Add address"}</button>{editingId && <button type="button" className={button} disabled={busy} onClick={() => { setEditingId(undefined); setValues(empty); }}>Cancel edit</button>}</div>
    </form>}
  </div>;
}
