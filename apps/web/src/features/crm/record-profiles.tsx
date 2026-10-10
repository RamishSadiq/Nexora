"use client";
import { RecordSection } from "./record-section";
import { useEffect, useState } from "react";
import { crm } from "./api";

export type ProfileAssignment = { slot: number; definitionId: string; parentNode: string; childNode: string; isArchived: boolean };
type Definition = { id: string; parentNode: string; childNode: string; isArchived: boolean; version: string };
const input = "mt-2 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 disabled:bg-slate-50";
const button = "rounded-lg border border-slate-300 px-3 py-2 text-sm font-medium disabled:opacity-50";

export function RecordProfiles({ id, version, assignments, editable, busy, onChanged }: { id: string; version: string; assignments?: ProfileAssignment[] | null; editable: boolean; busy: boolean; onChanged: () => void }) {
  const assignmentsAvailable = Array.isArray(assignments);
  const assignedProfiles = assignments ?? [];
  const [definitions, setDefinitions] = useState<Definition[]>([]);
  const [slots, setSlots] = useState(() => Array.from({ length: 5 }, (_, i) => assignedProfiles.find(x => x.slot === i + 1)?.definitionId ?? ""));
  const [pending, setPending] = useState(false);
  const [error, setError] = useState("");
  const [loaded, setLoaded] = useState(false);
  const [revision, setRevision] = useState(0);
  const [editing, setEditing] = useState<Definition | null>(null);
  const [parent, setParent] = useState("");
  const [child, setChild] = useState("");
  const disabled = !editable || busy || pending || !loaded || !assignmentsAvailable;
  useEffect(() => {
    let current = true;
    crm<Definition[]>("/profile-definitions").then(rows => { if (current) { setDefinitions(rows); setLoaded(true); } }).catch(e => { if (current) setError(e.message); });
    return () => { current = false; };
  }, [revision]);
  async function save(action: () => Promise<unknown>) {
    if (!assignmentsAvailable) { setError("Profile assignments are unavailable. Reload this record before editing profiles."); return; }
    setPending(true); setError("");
    try { await action(); onChanged(); setRevision(n => n + 1); }
    catch (e) { setError(e instanceof Error ? e.message : "Unable to save profiles."); }
    finally { setPending(false); }
  }
  const parents = [...new Set(definitions.map(x => x.parentNode))];
  return <RecordSection title="Profile" keepMounted>
    {!assignmentsAvailable && <p role="status" className="mb-3 text-sm text-slate-600">Profile assignments are unavailable. Reload this record before editing profiles.</p>}
    {error && <p role="alert" className="mb-3 text-sm text-red-700">{error} <button type="button" className={button} disabled={pending} onClick={() => { setError(""); setRevision(n => n + 1); }}>Reload definitions</button></p>}
    {!loaded && !error && <p role="status" className="mb-3 text-sm text-slate-500">Loading profiles…</p>}
    <form className="space-y-4" onSubmit={e => { e.preventDefault(); void save(() => crm(`/records/${id}/profiles`, "PUT", { version, assignments: slots.flatMap((definitionId, i) => definitionId ? [{ slot: i + 1, definitionId }] : []) })); }}>
      <fieldset disabled={disabled} className="grid gap-4 sm:grid-cols-2"><legend className="sr-only">Assigned profiles</legend>
        {slots.map((selected, i) => <label className="block text-sm" key={i}>Profile {i + 1}<select className={input} value={selected} onChange={e => setSlots(rows => rows.map((value, index) => index === i ? e.target.value : value))}>
          <option value="">None</option>
          {selected && !definitions.some(d => d.id === selected) && <option value={selected}>{assignedProfiles.find(x => x.definitionId === selected)?.childNode ?? "Unavailable profile"}</option>}
          {parents.map(group => <optgroup key={group} label={group}>{definitions.filter(d => d.parentNode === group && (!d.isArchived || d.id === selected)).map(d => <option key={d.id} value={d.id} disabled={slots.some((value, index) => value === d.id && index !== i)}>{d.childNode}{d.isArchived ? " (archived)" : ""}</option>)}</optgroup>)}
        </select></label>)}
      </fieldset>
      {editable && <button className="rounded-lg bg-indigo-700 px-4 py-2 text-sm font-semibold text-white disabled:opacity-50" disabled={disabled}>Save profiles</button>}
    </form>
    {editable && <details className="mt-5 border-t border-slate-200 pt-4"><summary className="cursor-pointer text-sm font-semibold">Manage profile definitions</summary>
      <p className="mt-3 text-sm text-slate-600">Definitions are shared by Contacts and Accounts in your organisation. Renaming a definition updates its displayed name on every assigned record.</p>
      <form className="mt-4 space-y-3" onSubmit={e => { e.preventDefault(); void save(async () => { await crm(`/profile-definitions${editing ? `/${editing.id}` : ""}`, editing ? "PUT" : "POST", { parentNode: parent, childNode: child, ...(editing ? { version: editing.version, isArchived: editing.isArchived } : {}) }); setEditing(null); setParent(""); setChild(""); }); }}>
        <div className="grid gap-3 sm:grid-cols-2"><label className="text-sm">Parent node<input className={input} value={parent} onChange={e => setParent(e.target.value)} required maxLength={160} disabled={disabled} /></label><label className="text-sm">Child node<input className={input} value={child} onChange={e => setChild(e.target.value)} required maxLength={160} disabled={disabled} /></label></div>
        <div className="flex gap-2"><button className={button} disabled={disabled}>{editing ? "Save definition" : "Add definition"}</button>{editing && <button type="button" className={button} disabled={pending} onClick={() => { setEditing(null); setParent(""); setChild(""); }}>Cancel</button>}</div>
      </form>
      <ul className="mt-4 space-y-2">{definitions.map(d => <li key={d.id} className="flex flex-wrap items-center justify-between gap-2 rounded-lg bg-slate-50 p-3 text-sm"><span>{d.parentNode} / {d.childNode}{d.isArchived ? " · Archived" : ""}</span><div className="flex gap-2"><button className={button} disabled={disabled} onClick={() => { setEditing(d); setParent(d.parentNode); setChild(d.childNode); }}>Edit</button><button className={button} disabled={disabled} onClick={() => void save(() => crm(`/profile-definitions/${d.id}`, "PUT", { ...d, isArchived: !d.isArchived }))}>{d.isArchived ? "Restore" : "Archive"}</button></div></li>)}</ul>
    </details>}
  </RecordSection>;
}
