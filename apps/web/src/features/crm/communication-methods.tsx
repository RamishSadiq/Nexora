"use client";

import { useState, type FormEvent } from "react";
import type { Item } from "./types";

export type CommunicationValues = { kind: string; priority: string; value: string; consent: string; consentEvidence: string };
const empty: CommunicationValues = { kind: "email", priority: "other", value: "", consent: "unknown", consentEvidence: "" };
const kinds = [["email", "Email"], ["phone", "Telephone"], ["mobile", "Mobile"], ["fax", "Fax"], ["website", "Website"]] as const;
const input = "mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 focus:outline-none focus:ring-2 focus:ring-indigo-500";
const button = "rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-800 hover:bg-slate-50 disabled:opacity-50";
const priorityOrder = (value?: string) => value === "primary" ? 0 : value === "secondary" ? 1 : 2;

export function CommunicationMethods({ items, editable, busy, onSave, onRemove }: { items: Item[]; editable: boolean; busy: boolean; onSave: (values: CommunicationValues, id?: string) => void; onRemove: (item: Item) => void }) {
  const [editingId, setEditingId] = useState<string>();
  const [values, setValues] = useState<CommunicationValues>(empty);
  function edit(item: Item) {
    setEditingId(item.id);
    setValues({ kind: item.kind ?? "email", priority: item.priority ?? "other", value: item.value ?? "", consent: item.consent ?? "unknown", consentEvidence: item.consentEvidence ?? "" });
  }
  function submit(event: FormEvent<HTMLFormElement>) { event.preventDefault(); onSave(values, editingId); }
  return <div className="space-y-5">
    {([{ title: "Emails", channels: ["email"] }, { title: "Mobile, Telephone and Fax", channels: ["mobile", "phone", "fax"] }, { title: "Websites", channels: ["website"] }]).map(group => {
      const rows = items.filter(item => group.channels.includes(item.kind ?? "")).sort((a, b) => priorityOrder(a.priority) - priorityOrder(b.priority));
      return <section key={group.title} aria-label={group.title}><h3 className="mb-2 text-sm font-semibold text-slate-900">{group.title}</h3>
        {rows.length === 0 ? <p className="text-sm text-slate-500">No {group.title.toLowerCase()} recorded.</p> : <ul className="space-y-2">{rows.map(item => <li key={item.id} className="flex flex-wrap items-start justify-between gap-3 rounded-lg bg-slate-50 p-3 text-sm">
          <div className="min-w-0 break-words"><p className="font-medium">{item.value}</p><p className="mt-1 text-xs capitalize text-slate-600">{item.priority ?? "other"} {kinds.find(([kind]) => kind === item.kind)?.[1] ?? item.kind}</p><p className="mt-1 text-xs text-slate-600">Consent: {item.consent}{item.consentEvidence ? ` · ${item.consentEvidence}` : ""}</p></div>
          {editable && <div className="flex gap-2"><button type="button" className={button} disabled={busy} aria-label={`Edit ${item.value}`} onClick={() => edit(item)}>Edit</button><button type="button" className={button} disabled={busy} aria-label={`Remove ${item.value}`} onClick={() => onRemove(item)}>Remove</button></div>}
        </li>)}</ul>}
      </section>;
    })}
    {editable && <form className="space-y-3 border-t border-slate-200 pt-4" onSubmit={submit}>
      <h3 className="text-sm font-semibold">{editingId ? "Edit communication method" : "Add communication method"}</h3>
      <fieldset disabled={busy} className="grid gap-3 sm:grid-cols-2">
        <label className="block text-sm">Method<select className={input} value={values.kind} onChange={e => setValues({ ...values, kind: e.target.value })}>{kinds.map(([kind, label]) => <option key={kind} value={kind}>{label}</option>)}</select></label>
        <label className="block text-sm">Designation<select className={input} value={values.priority} onChange={e => setValues({ ...values, priority: e.target.value })}>{["primary", "secondary", "other"].map(priority => <option key={priority} value={priority}>{priority[0].toUpperCase() + priority.slice(1)}</option>)}</select></label>
        <label className="block text-sm sm:col-span-2">Value<input className={input} type={values.kind === "email" ? "email" : values.kind === "website" ? "url" : "tel"} maxLength={320} required value={values.value} onChange={e => setValues({ ...values, value: e.target.value })} /></label>
        <label className="block text-sm">Consent<select className={input} value={values.consent} onChange={e => setValues({ ...values, consent: e.target.value })}>{["unknown", "granted", "denied"].map(consent => <option key={consent}>{consent}</option>)}</select></label>
        <label className="block text-sm">Consent evidence<input className={input} maxLength={500} required={values.consent !== "unknown"} value={values.consentEvidence} onChange={e => setValues({ ...values, consentEvidence: e.target.value })} /></label>
      </fieldset>
      <p className="text-xs text-slate-600">Each method can have one primary and one secondary entry, plus multiple other entries. Granted or denied consent requires evidence.</p>
      <div className="flex gap-2"><button className={button} disabled={busy}>{editingId ? "Save communication method" : "Add method"}</button>{editingId && <button type="button" className={button} disabled={busy} onClick={() => { setEditingId(undefined); setValues(empty); }}>Cancel edit</button>}</div>
    </form>}
  </div>;
}
