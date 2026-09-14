"use client";

import { useEffect, useId, useRef, useState, type FormEvent, type ReactNode } from "react";
import { Eye, Pencil, SlidersHorizontal, MoreHorizontal, UserRound } from "lucide-react";
import { crm } from "./api";
import type { Activity, Detail, Directory, Field, Item, ListState, RecordRow, View } from "./types";

const input = "mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 focus:outline-none focus:ring-2 focus:ring-indigo-500";
const button = "rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-800 hover:bg-slate-50 focus-visible:outline-2 focus-visible:outline-indigo-600 disabled:opacity-50";
const primary = "rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600 disabled:opacity-50";
function Label({ title, children }: { title: string; children: ReactNode }) { return <label className="block text-sm font-medium text-slate-700">{title}{children}</label>; }
function data(event: FormEvent<HTMLFormElement>) { event.preventDefault(); return Object.fromEntries(new FormData(event.currentTarget).entries()) as Record<string, string>; }
function Card({ title, children }: { title: string; children: ReactNode }) { return <section className="rounded-xl border border-slate-200 bg-white p-5"><h2 className="mb-4 text-lg font-semibold text-slate-900">{title}</h2>{children}</section>; }

function ConfirmDialog({ message, onConfirm, onCancel }: { message: string; onConfirm: () => void; onCancel: () => void }) {
  const dialog = useRef<HTMLDialogElement>(null);
  const titleId = useId();
  const messageId = useId();
  useEffect(() => { dialog.current?.showModal(); }, []);
  return <dialog ref={dialog} aria-labelledby={titleId} aria-describedby={messageId} onCancel={event => { event.preventDefault(); onCancel(); }} className="m-auto w-[calc(100%-2rem)] max-w-md rounded-xl border border-slate-200 bg-white p-6 text-slate-800 shadow-xl backdrop:bg-slate-900/40">
    <h2 id={titleId} className="text-lg font-semibold">Confirm change</h2><p id={messageId} className="my-4 text-sm leading-6">{message}</p>
    <div className="flex justify-end gap-2"><button className={button} onClick={onCancel}>Cancel</button><button className={primary} onClick={onConfirm}>Confirm</button></div>
  </dialog>;
}

export function CrmWorkspace({ permissions, initial, initialRecord, fixedKind = false }: { permissions: string[]; initial: ListState; initialRecord: string; fixedKind?: boolean }) {
  const [query, setQuery] = useState(initial);
  const [search, setSearch] = useState(initial.search);
  const [selected, setSelected] = useState(initialRecord);
  const [items, setItems] = useState<RecordRow[]>([]);
  const [total, setTotal] = useState(0);
  const [directory, setDirectory] = useState<Directory>({ users: [], teams: [] });
  const [fields, setFields] = useState<Field[]>([]);
  const [views, setViews] = useState<View[]>([]);
  const [deleteView, setDeleteView] = useState(false);
  const [activeView, setActiveView] = useState("");
  const [columns, setColumns] = useState(["name", "status", "category"]);
  const [refresh, setRefresh] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const [creating, setCreating] = useState(false);
  const [fieldsOpen, setFieldsOpen] = useState(false);
  const canManage = permissions.includes("crm.manage");
  useEffect(() => {
    let current = true;
    const params = new URLSearchParams(Object.entries(query).map(([k, v]) => [k, String(v)]));
    const url = new URL(window.location.href); url.search = params.toString(); if (selected) url.searchParams.set("record", selected);
    window.history.replaceState(null, "", url);
    crm<{ items: RecordRow[]; total: number }>(`/records?${params}`).then(result => { if (current) { setItems(result.items); setTotal(result.total); setLoading(false); } }).catch(e => { if (current) { setError(e.message); setLoading(false); } });
    return () => { current = false; };
  }, [query, refresh, selected]);
  useEffect(() => {
    let current = true;
    Promise.all([crm<Directory>("/directory"), crm<Field[]>("/fields"), crm<View[]>("/views")]).then(([d, f, v]) => { if (current) { setDirectory(d); setFields(f); setViews(v); } }).catch(e => { if (current) setError(e.message); });
    return () => { current = false; };
  }, [refresh]);
  function filter(change: Partial<ListState>) { setLoading(true); setError(""); setQuery(q => ({ ...q, ...change, page: change.page ?? 1 })); }
  async function perform(action: () => Promise<void>) { setBusy(true); setError(""); try { await action(); setRefresh(r => r + 1); } catch (e) { setError(e instanceof Error ? e.message : "Unable to save."); } finally { setBusy(false); } }
  return <main className="mx-auto max-w-7xl space-y-5 p-4 text-slate-800 sm:p-7">
    {deleteView && <ConfirmDialog message="Delete this saved view? Records will remain unchanged." onCancel={() => setDeleteView(false)} onConfirm={() => { setDeleteView(false); void perform(async () => { await crm(`/views/${activeView}`, "DELETE"); setActiveView(""); }); }} />}
    <header className="flex flex-wrap items-end justify-between gap-4"><div><p className="text-[11px] font-semibold uppercase tracking-[0.16em] text-indigo-600">CRM workspace</p><h1 className="mt-1 text-3xl font-semibold tracking-tight text-slate-950">{query.kind === "contact" ? "Contacts" : "Accounts"}</h1><p className="mt-1 text-sm text-slate-600">People, organisations and the history that connects them.</p></div><div className="flex items-center gap-2">{canManage && <button className={primary} onClick={() => { setCreating(true); setSelected(""); }}>Create {query.kind}</button>}{permissions.includes("crm.configure") && <button className={button} onClick={() => setFieldsOpen(true)}><SlidersHorizontal className="size-4" />Custom fields</button>}</div></header>
    {error && <div role="alert" className="flex flex-wrap items-center justify-between gap-2 rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800">{error}<button className={button} onClick={() => { setError(""); setLoading(true); setRefresh(r => r + 1); }}>Retry loading</button></div>}
    <section aria-label="Search and list preferences" className="space-y-4 rounded-xl border border-slate-200 bg-white p-4">
      {!fixedKind && <div className="flex flex-wrap gap-2">{["contact", "account"].map(kind => <button key={kind} aria-pressed={query.kind === kind} className={query.kind === kind ? primary : button} onClick={() => { filter({ kind }); setSelected(""); setCreating(false); }}>{kind === "contact" ? "Contacts" : "Accounts"}</button>)}</div>}
      <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-5">
        <form className="flex items-end gap-2 sm:col-span-2" onSubmit={e => { e.preventDefault(); filter({ search }); }}><Label title="Search by name"><input className={input} value={search} maxLength={160} onChange={e => setSearch(e.target.value)} placeholder="Find a person or organisation" /></Label><button className={button}>Search</button></form>
        <Label title="Status"><select className={input} value={query.status} onChange={e => filter({ status: e.target.value })}><option value="">All statuses</option>{["active", "prospect", "inactive"].map(s => <option key={s}>{s}</option>)}</select></Label>
        <Label title="Sort"><select className={input} value={query.sort} onChange={e => filter({ sort: e.target.value })}><option value="name">Name A–Z</option><option value="-name">Name Z–A</option><option value="updated">Recently updated</option></select></Label>
        <Label title="Record state"><select className={input} value={String(query.archived)} onChange={e => filter({ archived: e.target.value === "true" })}><option value="false">Current</option><option value="true">Archived</option></select></Label>
      </div>
      <div className="flex flex-wrap items-end gap-4"><Label title="Saved views"><select className={input} value={activeView} onChange={e => { setActiveView(e.target.value); const v = views.find(x => x.id === e.target.value); if (v) { filter({ kind: v.kind, search: v.search, status: v.status, sort: v.sort, archived: v.archived }); setSearch(v.search); setColumns(v.columns.split(",")); } }}><option value="">Choose a personal view</option>{views.map(v => <option key={v.id} value={v.id}>{v.name}</option>)}</select></Label>
        {activeView && <button className={button} disabled={busy} onClick={() => setDeleteView(true)}>Delete view</button>}
        <form className="flex items-end gap-2" onSubmit={e => { const r = data(e); const form = e.currentTarget; void perform(async () => { await crm("/views", "POST", { ...query, columns: columns.join(","), name: r.name }); form.reset(); }); }}><Label title="Save filters and columns"><input className={input} name="name" placeholder="View name" maxLength={80} required /></Label><button className={button} disabled={busy}>Save view</button></form>
        <fieldset className="flex flex-wrap gap-3 text-sm"><legend className="mb-1 font-medium">Columns</legend>{["status", "category", "updatedAtUtc"].map(c => <label key={c} className="flex items-center gap-1"><input type="checkbox" checked={columns.includes(c)} onChange={e => setColumns(old => e.target.checked ? [...old, c] : old.filter(x => x !== c))} />{c === "updatedAtUtc" ? "Updated" : c}</label>)}</fieldset>
      </div>
    </section>
    {fieldsOpen && permissions.includes("crm.configure") && <aside className="fixed inset-y-0 right-0 z-40 w-full max-w-md overflow-y-auto border-l border-slate-200 bg-white p-6 shadow-2xl" aria-label="Custom fields panel"><div className="flex items-center justify-between"><div><p className="text-xs font-semibold uppercase tracking-wider text-indigo-600">Custom fields</p><h2 className="mt-1 text-xl font-semibold">Define custom fields</h2></div><button className={button} onClick={() => setFieldsOpen(false)} aria-label="Close custom fields">Close</button></div><p className="mt-3 text-sm text-slate-600">Fields have a fixed data type. Create up to 50 definitions for your tenant.</p><form className="mt-6 space-y-4" onSubmit={e => { const r = data(e); const form = e.currentTarget; void perform(async () => { await crm("/fields", "POST", r); form.reset(); }); }}><Label title="Field name"><input name="name" className={input} maxLength={80} placeholder="LinkedIn URL" required /></Label><Label title="Applies to"><select name="kind" className={input}><option value="contact">Contacts</option><option value="account">Accounts</option></select></Label><Label title="Type"><select name="dataType" className={input}>{["text", "number", "boolean", "date"].map(t => <option key={t}>{t}</option>)}</select></Label><button className={`${primary} w-full`} disabled={busy}>Add new custom field</button></form><hr className="my-6 border-slate-200" /><h3 className="font-semibold">Existing custom fields</h3><ul className="mt-3 divide-y divide-slate-100">{fields.filter(f => f.kind === query.kind).map(f => <li key={f.id} className="py-3"><p className="font-medium">{f.name}</p><p className="text-xs text-slate-500">{query.kind === "contact" ? "Contact" : "Account"}, {f.dataType}</p></li>)}{fields.filter(f => f.kind === query.kind).length === 0 && <li className="py-3 text-sm text-slate-500">No custom fields yet.</li>}</ul></aside>}
    {creating && <Card title={`Create ${query.kind}`}><RecordForm kind={query.kind} directory={directory} busy={busy} onCancel={() => setCreating(false)} onSave={r => perform(async () => { const saved = await crm<RecordRow>("/records", "POST", r); setCreating(false); setSelected(saved.id); })} /></Card>}
    <section aria-label="CRM records" className="overflow-hidden rounded-xl border border-slate-200 bg-white" aria-busy={loading}>
      <div className="overflow-x-auto"><table className="w-full text-left text-sm"><caption className="sr-only">{query.kind === "contact" ? "Contacts" : "Accounts"} matching current filters</caption><thead className="bg-slate-50 text-slate-600"><tr><th scope="col" className="w-12 p-4"><span className="sr-only">Select</span><input type="checkbox" aria-label="Select all records" /></th><th scope="col" className="p-4">Name</th>{columns.includes("status") && <th scope="col" className="p-4">Status</th>}{columns.includes("category") && <th scope="col" className="p-4">Category</th>}{columns.includes("updatedAtUtc") && <th scope="col" className="p-4">Updated</th>}<th scope="col" className="p-4 text-right">Actions</th></tr></thead><tbody>{!loading && items.map(row => <tr key={row.id} className="border-t border-slate-100 hover:bg-indigo-50/30"><td className="p-4"><input type="checkbox" aria-label={`Select ${row.name}`} /></td><th scope="row" className="p-4"><button className="flex items-center gap-3 text-left font-semibold text-slate-800 hover:text-indigo-700" onClick={() => { setSelected(row.id); setCreating(false); }}><span className="grid size-8 place-items-center rounded-full bg-slate-100 text-slate-500"><UserRound className="size-4" /></span>{row.name}</button></th>{columns.includes("status") && <td className="p-4"><span className={`inline-flex rounded-full px-2.5 py-1 text-xs font-medium ${row.status === "active" ? "bg-emerald-100 text-emerald-800" : row.status === "prospect" ? "bg-amber-100 text-amber-800" : "bg-rose-100 text-rose-800"}`}>{row.status}</span></td>}{columns.includes("category") && <td className="p-4"><span className="inline-flex rounded-full bg-slate-100 px-2.5 py-1 text-xs font-medium text-slate-700">{row.category ?? "—"}</span></td>}{columns.includes("updatedAtUtc") && <td className="p-4">{new Date(row.updatedAtUtc).toLocaleDateString()}</td>}<td className="p-4"><div className="flex justify-end gap-1"><button className="rounded-lg p-2 text-slate-500 hover:bg-indigo-50 hover:text-indigo-700" aria-label={`View ${row.name}`} title="View" onClick={() => { setSelected(row.id); setCreating(false); }}><Eye className="size-4" /></button>{canManage && <button className="rounded-lg p-2 text-slate-500 hover:bg-indigo-50 hover:text-indigo-700" aria-label={`Edit ${row.name}`} title="Edit" onClick={() => { setSelected(row.id); setCreating(false); }}><Pencil className="size-4" /></button>}{canManage && <button className="rounded-lg p-2 text-slate-500 hover:bg-slate-100" aria-label={`More actions for ${row.name}`} title="More actions" onClick={() => { setSelected(row.id); setCreating(false); }}><MoreHorizontal className="size-4" /></button>}</div></td></tr>)}</tbody></table></div>
      {loading ? <p className="p-8 text-center" role="status">Loading relationships…</p> : items.length === 0 && <p className="p-8 text-center text-slate-600">No records match these filters. Adjust your search or create the first {query.kind}.</p>}
      <footer className="flex items-center justify-between border-t border-slate-200 p-4"><p className="text-sm" aria-live="polite">{total} records · Page {query.page}</p><div className="flex gap-2"><button className={button} disabled={query.page <= 1 || loading} onClick={() => filter({ page: query.page - 1 })}>Previous</button><button className={button} disabled={query.page * 20 >= total || loading} onClick={() => filter({ page: query.page + 1 })}>Next</button></div></footer>
    </section>
    {selected && <RecordDetail key={selected} id={selected} directory={directory} fields={fields} permissions={permissions} onChanged={() => setRefresh(r => r + 1)} onClose={() => setSelected("")} />}
  </main>;
}

function RecordForm({ row, kind, directory, busy, onSave, onCancel }: { row?: RecordRow; kind: string; directory: Directory; busy: boolean; onSave: (r: unknown) => Promise<void>; onCancel: () => void }) {
  return <form className="grid gap-4 sm:grid-cols-2" onSubmit={e => { const r = data(e); void onSave({ ...r, kind, category: r.category || null, ownerUserId: r.ownerUserId || null, ownerTeamId: r.ownerTeamId || null, version: row?.version }); }}>
    <Label title={kind === "contact" ? "Full name" : "Account name"}><input className={input} name="name" defaultValue={row?.name} maxLength={160} required /></Label>
    <Label title="Status"><select className={input} name="status" defaultValue={row?.status ?? "active"}>{["active", "prospect", "inactive"].map(s => <option key={s}>{s}</option>)}</select></Label>
    <Label title="Category"><input className={input} name="category" defaultValue={row?.category ?? ""} maxLength={100} /></Label>
    <Label title="Owner"><select className={input} name="ownerUserId" defaultValue={row?.ownerUserId ?? ""}><option value="">Unassigned</option>{directory.users.map(u => <option key={u.id} value={u.id}>{u.name}</option>)}</select></Label>
    <Label title="Team"><select className={input} name="ownerTeamId" defaultValue={row?.ownerTeamId ?? ""}><option value="">Unassigned</option>{directory.teams.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}</select></Label>
    <div className="flex items-end gap-2"><button className={primary} disabled={busy}>Save {kind}</button><button type="button" className={button} onClick={onCancel}>Cancel</button></div>
  </form>;
}

function RecordDetail({ id, directory, fields, permissions, onChanged, onClose }: { id: string; directory: Directory; fields: Field[]; permissions: string[]; onChanged: () => void; onClose: () => void }) {
  const [confirmation, setConfirmation] = useState<{ message: string; action: () => Promise<void> } | null>(null);
  const recordHeading = useRef<HTMLDivElement>(null);
  const focused = useRef(false);
  const [detail, setDetail] = useState<Detail | null>(null);
  const [timeline, setTimeline] = useState<Activity[]>([]);
  const [timelinePage, setTimelinePage] = useState(1);
  const [refresh, setRefresh] = useState(0);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [editing, setEditing] = useState(false);
  const [targets, setTargets] = useState<RecordRow[]>([]);
  const manage = permissions.includes("crm.manage");
  useEffect(() => {
    let current = true;
    Promise.all([crm<Detail>(`/records/${id}`), crm<Activity[]>(`/records/${id}/timeline?page=${timelinePage}`)]).then(([d, a]) => { if (current) { setDetail(d); setTimeline(a); } }).catch(e => { if (current) setError(e.message); });
    return () => { current = false; };
  }, [id, refresh, timelinePage]);
  useEffect(() => {
    if (detail && !focused.current) {
      focused.current = true;
      recordHeading.current?.focus();
    }
  }, [detail]);
  async function perform(action: () => Promise<void>) { setBusy(true); setError(""); try { await action(); setRefresh(r => r + 1); onChanged(); } catch (e) { setError(e instanceof Error ? e.message : "Unable to save."); } finally { setBusy(false); } }
  function add(collection: string, event: FormEvent<HTMLFormElement>) { const r = data(event); const form = event.currentTarget; void perform(async () => { await crm(`/records/${id}/${collection}`, "POST", r); form.reset(); }); }
  const editable = manage && detail && !detail.record.isArchived;
  function section(title: string, collection: keyof Detail, removable = false) {
    const rows = detail?.[collection] as Item[];
    return <ul className="mb-4 space-y-3">{rows.length === 0 && <li className="text-sm text-slate-500">No {title.toLowerCase()} yet.</li>}{rows.map(row => <li key={row.id} className="flex items-start justify-between gap-2 rounded-lg bg-slate-50 p-3 text-sm"><div className="whitespace-pre-wrap break-words">{row.name || row.body || row.label || row.value}{row.line1 && <p>{[row.line1, row.city, row.postalCode, row.country].filter(Boolean).join(", ")}</p>}{row.consent && <p className="mt-1 text-xs text-slate-600">{row.kind} · Consent: {row.consent}{row.consentEvidence ? ` · ${row.consentEvidence}` : ""}</p>}{row.targetRecordId && <p className="text-xs text-slate-600">{row.label}</p>}</div>{removable && editable && <button className={button} disabled={busy} aria-label={`Remove ${row.name || row.label || row.value}`} onClick={() => setConfirmation({ message: `Remove this item from ${detail?.record.name}?`, action: async () => { await crm(`/records/${id}/${collection}/${row.id}`, "DELETE"); } })}>Remove</button>}</li>)}</ul>;
  }
  return <section className="space-y-4" aria-label="Selected record">
    {confirmation && <ConfirmDialog message={confirmation.message} onCancel={() => setConfirmation(null)} onConfirm={() => { const action = confirmation.action; setConfirmation(null); void perform(action); }} />}
    {error && <div role="alert" className="rounded-lg bg-red-50 p-4 text-sm text-red-800">{error} <button className={button} onClick={() => { setError(""); setRefresh(r => r + 1); }}>Reload record</button></div>}
    {!detail ? <p role="status">Loading record…</p> : <>
      <div ref={recordHeading} tabIndex={-1} aria-label={detail.record.name} className="scroll-mt-24 outline-none"><Card title={detail.record.name}><div className="mb-4 flex flex-wrap items-center gap-2"><span className="rounded-full bg-indigo-50 px-3 py-1 text-sm text-indigo-800">{detail.record.kind} · {detail.record.isArchived ? "archived" : detail.record.status}</span><span className="text-sm text-slate-600">Owner: {directory.users.find(u => u.id === detail.record.ownerUserId)?.name ?? "Unassigned"}</span><button className={button} onClick={onClose}>Close record</button>{editable && <button className={button} onClick={() => setEditing(true)}>Edit details</button>}{manage && <button className={button} disabled={busy} onClick={() => setConfirmation({ message: detail.record.isArchived ? "Restore this record to current lists?" : "Archive this record? It will leave current lists and remain available for restoration.", action: async () => { await crm(`/records/${id}/${detail.record.isArchived ? "restore" : "archive"}`, "POST", { version: detail.record.version }); } })}>{detail.record.isArchived ? "Restore" : "Archive"}</button>}</div>{editing && editable && <RecordForm key={detail.record.version} row={detail.record} kind={detail.record.kind} directory={directory} busy={busy} onCancel={() => setEditing(false)} onSave={r => perform(async () => { await crm(`/records/${id}`, "PATCH", r); setEditing(false); })} />}</Card></div>
      <div className="grid gap-4 xl:grid-cols-2">
        <Card title="Addresses">{section("Addresses", "addresses", true)}{editable && <form className="grid gap-3 sm:grid-cols-2" onSubmit={e => add("addresses", e)}>{[["label", "Label"], ["line1", "Address line 1"], ["line2", "Address line 2"], ["city", "City"], ["region", "Region"], ["postalCode", "Postal code"], ["country", "Country"]].map(([name, label]) => <Label key={name} title={label}><input className={input} name={name} maxLength={name === "postalCode" ? 30 : name === "label" || name === "country" ? 80 : name === "city" || name === "region" ? 100 : 200} required={!["line2", "region"].includes(name)} /></Label>)}<button className={button} disabled={busy}>Add address</button></form>}</Card>
        <Card title="Communication methods">{section("Communications", "communications", true)}{editable && <form className="space-y-3" onSubmit={e => add("communications", e)}><Label title="Method"><select name="kind" className={input}>{["email", "phone", "website"].map(s => <option key={s}>{s}</option>)}</select></Label><Label title="Value"><input className={input} name="value" maxLength={320} required /></Label><Label title="Consent"><select className={input} name="consent">{["unknown", "granted", "denied"].map(s => <option key={s}>{s}</option>)}</select></Label><Label title="Consent evidence (required for granted or denied)"><input className={input} name="consentEvidence" maxLength={500} /></Label><button className={button} disabled={busy}>Add method</button></form>}</Card>
        <Card title="Related records">{section("Relationships", "relationships", true)}{editable && <><form className="mb-3 flex items-end gap-2" onSubmit={e => { const r = data(e); void perform(async () => { const result = await crm<{ items: RecordRow[] }>(`/records?kind=${r.kind}&search=${encodeURIComponent(r.search)}&pageSize=100`); setTargets(result.items.filter(x => x.id !== id)); }); }}><Label title="Find related"><input name="search" className={input} maxLength={160} required /></Label><Label title="Type"><select name="kind" className={input}><option value="account">Accounts</option><option value="contact">Contacts</option></select></Label><button className={button} disabled={busy}>Find</button></form><form className="space-y-3" onSubmit={e => add("relationships", e)}><Label title="Related record"><select className={input} name="targetRecordId" required><option value="">Choose a search result</option>{targets.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}</select></Label><Label title="Relationship (for example, works at)"><input className={input} name="label" maxLength={100} required /></Label><button className={button} disabled={busy}>Link record</button></form></>}</Card>
        <Card title="Notes">{section("Notes", "notes")}{editable && <form className="space-y-3" onSubmit={e => add("notes", e)}><Label title="New note"><textarea name="body" className={input} rows={4} maxLength={4000} required /></Label><button className={button} disabled={busy}>Add note</button></form>}</Card>
        <Card title="Tags">{section("Tags", "tags", true)}{editable && <form className="flex items-end gap-2" onSubmit={e => add("tags", e)}><Label title="New tag"><input name="name" className={input} maxLength={80} required /></Label><button className={button} disabled={busy}>Add tag</button></form>}</Card>
        <Card title="Files"><ul className="mb-4 space-y-2">{detail.files.length === 0 && <li className="text-sm text-slate-500">No files attached.</li>}{detail.files.map(f => <li key={f.id}>{permissions.includes("crm.files") ? <a className="text-sm font-medium text-indigo-700 underline" href={`/api/v1/crm/records/${id}/files/${f.id}`}>{f.name}</a> : <span className="text-sm">{f.name} · File access required</span>}</li>)}</ul>{editable && permissions.includes("crm.files") && <form className="space-y-3" onSubmit={e => { e.preventDefault(); const form = e.currentTarget; const body = new FormData(form); void perform(async () => { await crm(`/records/${id}/files`, "POST", body); form.reset(); }); }}><Label title="Attach file (up to 5 MB)"><input className={input} type="file" name="file" accept=".pdf,.txt,.csv,.png,.jpg,.jpeg" required /></Label><p className="text-xs text-slate-600">PDF, text, CSV, PNG or JPEG. Files download as attachments.</p><button className={button} disabled={busy}>Upload file</button></form>}</Card>
        <Card title="Custom fields">{fields.filter(f => f.kind === detail.record.kind).length === 0 && <p className="text-sm text-slate-500">No custom fields defined for this record type.</p>}{fields.filter(f => f.kind === detail.record.kind).map(f => { const v = detail.fields.find(x => x.definitionId === f.id); return <form key={`${f.id}-${refresh}`} className="mb-3 flex items-end gap-2" onSubmit={e => { const r = data(e); const payload = { textValue: f.dataType === "text" ? r.value || null : null, numberValue: f.dataType === "number" && r.value !== "" ? Number(r.value) : null, booleanValue: f.dataType === "boolean" && r.value !== "" ? r.value === "true" : null, dateValue: f.dataType === "date" && r.value ? `${r.value}T00:00:00Z` : null }; void perform(async () => { await crm(`/records/${id}/fields/${f.id}`, "PUT", payload); }); }}><Label title={`${f.name} (${f.dataType})`}>{f.dataType === "boolean" ? <select className={input} name="value" disabled={!editable} defaultValue={v?.booleanValue === null || v?.booleanValue === undefined ? "" : String(v.booleanValue)}><option value="">Not set</option><option value="true">Yes</option><option value="false">No</option></select> : <input className={input} name="value" maxLength={500} disabled={!editable} type={f.dataType === "number" ? "number" : f.dataType === "date" ? "date" : "text"} step={f.dataType === "number" ? "0.0001" : undefined} defaultValue={f.dataType === "date" ? v?.dateValue?.slice(0, 10) ?? "" : v?.textValue ?? v?.numberValue ?? ""} />}</Label>{editable && <button className={button} disabled={busy}>Save field</button>}</form>; })}</Card>
        <Card title="Activity timeline"><ol className="space-y-3">{timeline.map(a => <li key={a.id} className="border-l-2 border-indigo-200 pl-3 text-sm"><p className="font-medium">{a.action.replaceAll(".", " ")}</p><p className="text-xs text-slate-600">{directory.users.find(u => u.id === a.actorUserId)?.name ?? "Former user"} · {new Date(a.createdAtUtc).toLocaleString()}</p></li>)}</ol><div className="mt-4 flex gap-2"><button className={button} disabled={timelinePage <= 1} onClick={() => setTimelinePage(p => p - 1)}>Newer activity</button><button className={button} disabled={timeline.length < 50} onClick={() => setTimelinePage(p => p + 1)}>Older activity</button></div></Card>
      </div>
    </>}
  </section>;
}
