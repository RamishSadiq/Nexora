"use client";
import styles from "./record-detail.module.css";
import listingStyles from "./record-listing.module.css";
import { RecordSection } from "./record-section";
import { RecordTimeline } from "./record-timeline";
import { RecordDataAudit } from "./record-data-audit";
import { RecordPhoto, RecordPhotoField } from "./record-photo";
import { WorkspaceHeading } from "@/components/ui/workspace-heading";
import { EntityHeading } from "@/components/ui/entity-heading";
import { attributeValues } from "../shared/attribute-fields";
import { RecordAttributeSections } from "../shared/record-attribute-sections";
import { RecordProfiles } from "./record-profiles";
import { RecordMemberships } from "./record-memberships";
import { RecordProfile } from "./record-profile";
import { CommunicationMethods } from "./communication-methods";
import { RecordAddresses } from "./record-addresses";
import { RecordTags } from "./record-tags";
import { PanelToggle, useCollapsedPanel } from "../shared/panel-toggle";
import { ConfiguredSections } from "../shared/configured-sections";

import { useEffect, useId, useRef, useState, type FormEvent, type ReactNode } from "react";
import { ArrowLeft, Building2, Plus, Eye, MoreHorizontal, Pencil, Trash2, UserRound } from "lucide-react";
import { crm } from "./api";
import { useCrmListing } from "./use-crm-listing";
import { ConnectedRecord } from "./connected-record";
import { RecordTransfer } from "./record-transfer";
import { ColumnChooser } from "../shared/column-chooser";
import { FilterViewToolbar } from "../shared/filter-view-toolbar";
import { SortableHeader, parseTableSort } from "../shared/table-sort";
import { SavedViewControls } from "./saved-view-controls";
import type { Detail, Directory, Field, Item, ListState, RecordRow } from "./types";

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

export function CrmWorkspace({ permissions, initial, initialRecord, fixedKind = false, currentUserId }: { currentUserId?: string; permissions: string[]; initial: ListState; initialRecord: string; fixedKind?: boolean }) {
  const [selectedReadOnly, setSelectedReadOnly] = useState(true);
  const [items, setItems] = useState<RecordRow[]>([]);
  const [checked, setChecked] = useState<{key:string; ids:string[]}>({key:"", ids:[]});
  const [total, setTotal] = useState(0);
  const [refresh, setRefresh] = useState(0);
  const listing = useCrmListing(initial, initialRecord, fixedKind, refresh);
  const { query, search, setSearch, selected, setSelected, directory, fields, columns, setColumns, filter } = listing;
  const requestKey = JSON.stringify([query, refresh, selected]);
  const [loadedRequest, setLoadedRequest] = useState("");
  const selectedIds = checked.key === requestKey ? checked.ids : [];
  const loading = !listing.ready || loadedRequest !== requestKey;
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const [creating, setCreating] = useState(false);
  const canManage = permissions.includes("crm.manage");
  useEffect(() => {
    let current = true;
    const params = new URLSearchParams(Object.entries(query).map(([k, v]) => [k, String(v)]));
    if (!listing.ready) return;
    crm<{ items: RecordRow[]; total: number }>(`/records?${params}`).then(result => { if (current) { setItems(result.items); setTotal(result.total); setError(""); setLoadedRequest(requestKey); } }).catch(e => { if (current) { setError(e.message); setLoadedRequest(requestKey); } });
    return () => { current = false; };
  }, [query, refresh, selected, listing.ready, requestKey]);
  async function perform(action: () => Promise<void>) { setBusy(true); setError(""); try { await action(); setRefresh(r => r + 1); } catch (e) { setError(e instanceof Error ? e.message : "Unable to save."); } finally { setBusy(false); } }
  const columnControls = <>{[{ id: "name", label: "Name" }, { id: "status", label: "Status" }, { id: "category", label: "Category" }, { id: "updatedAtUtc", label: "Updated" }, ...fields.filter(f => f.kind === query.kind).map(f => ({ id: `field:${f.id}`, label: `${f.name} (${f.dataType})` }))].map(column => <label key={column.id} className="flex items-center gap-3 rounded-lg border border-slate-200 p-3 text-sm"><input type="checkbox" disabled={column.id === "name"} checked={columns.includes(column.id)} onChange={() => setColumns(current => current.includes(column.id) ? current.filter(x => x !== column.id) : [...current, column.id])} />{column.label}</label>)}</>;
  if (creating) return <main className="page-enter mx-auto max-w-4xl space-y-6 p-5 sm:p-8"><header className="flex items-start justify-between gap-4"><div><EntityHeading className="mt-2">Create {query.kind}</EntityHeading><p className="mt-2 text-sm text-slate-600">Complete the available details for this {query.kind} before saving.</p></div><button className={button} onClick={() => setCreating(false)}>Back to {query.kind === "contact" ? "contacts" : "accounts"}</button></header><Card title={`${query.kind === "contact" ? "Contact" : "Account"} details`}><RecordForm kind={query.kind} directory={directory} busy={busy} onCancel={() => setCreating(false)} onSave={r => perform(async () => { const saved = await crm<RecordRow>("/records", "POST", r); setCreating(false); setSelected(saved.id); })} /></Card></main>;
  if (selected) return <main className={`${styles.page} page-enter mx-auto max-w-7xl space-y-5 p-5 sm:p-8`}><WorkspaceHeading>{query.kind === "contact" ? "Contacts" : "Accounts"}</WorkspaceHeading><button className={styles.back} aria-label="Back to listing" title="Back" onClick={() => setSelected("")}><ArrowLeft size={16} aria-hidden="true" /></button><RecordDetail key={`${selected}-${selectedReadOnly}`} id={selected} directory={directory} fields={fields} permissions={permissions} readOnly={selectedReadOnly} onChanged={() => setRefresh(r => r + 1)} /></main>;
  return <main className={`${listingStyles.listing} mx-auto max-w-7xl space-y-5 p-4 text-slate-800 sm:p-7`}>
    <WorkspaceHeading actions={<div className="flex items-center gap-2">{canManage && <button type="button" className={listingStyles.create} aria-label={`Create ${query.kind}`} title={`Create ${query.kind}`} onClick={() => { setCreating(true); setSelected(""); }}><Plus size={20} strokeWidth={1.8} aria-hidden="true" /></button>}</div>}>{query.kind === "contact" ? "Contacts" : "Accounts"}</WorkspaceHeading>
    {error && <div role="alert" className="flex flex-wrap items-center justify-between gap-2 rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800">{error}<button className={button} onClick={() => { setError(""); setRefresh(r => r + 1); }}>Retry loading</button></div>}
    <FilterViewToolbar onAllRecords={() => listing.scope()} onMyRecords={currentUserId ? () => listing.scope(currentUserId) : undefined}
      search={<form className="flex items-end gap-2" onSubmit={e => { e.preventDefault(); filter({ search }); }}><label className="min-w-0 flex-1"><span className="sr-only">Search records</span><input className="w-full rounded-lg border border-blue-300 bg-white px-3 py-2 text-sm text-slate-900 placeholder:text-blue-400 focus:outline-none focus:ring-2 focus:ring-blue-500" type="search" value={search} maxLength={160} onChange={e => setSearch(e.target.value)} placeholder="Search records…" /></label><button className="rounded-lg bg-blue-600 px-3 py-2 text-sm font-semibold text-white hover:bg-blue-700 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-600">Search</button></form>}
      actions={canManage ? <RecordTransfer kind={query.kind} changed={() => setRefresh(r => r + 1)} /> : undefined}
      filters={<>
      <div className="grid gap-3 sm:grid-cols-2">
        <Label title="Status"><select className={input} value={query.status} onChange={e => filter({ status: e.target.value })}><option value="">All statuses</option>{["active", "prospect", "inactive", "lapsed"].map(s => <option key={s}>{s}</option>)}</select></Label>
        <Label title="Owner user"><select className={input} value={query.ownerUserId ?? ""} onChange={e => filter({ ownerUserId: e.target.value })}><option value="">All users</option>{directory.users.map(u => <option key={u.id} value={u.id}>{u.name}</option>)}</select></Label>
        <Label title="Owner team"><select className={input} value={query.ownerTeamId ?? ""} onChange={e => filter({ ownerTeamId: e.target.value })}><option value="">All teams</option>{directory.teams.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}</select></Label>
        <Label title="Category"><input className={input} maxLength={100} value={query.category ?? ""} onChange={e => filter({ category: e.target.value })} /></Label>
        <Label title="Sort"><select className={input} value={query.sort} onChange={e => filter({ sort: e.target.value })}><option value="name">Name A–Z</option><option value="-name">Name Z–A</option><option value="updated">Recently updated</option></select></Label>
        <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={query.archived} onChange={e => filter({ archived: e.target.checked })} />Archived records</label>
      </div>
        <SavedViewControls listing={listing} />
      </>}
    />
    <section aria-label="CRM records" className="overflow-hidden rounded-xl border border-slate-200 bg-white" aria-busy={loading}>
      <div className="overflow-x-auto"><table className="w-full text-left text-sm"><caption className="sr-only">{query.kind === "contact" ? "Contacts" : "Accounts"} matching current filters</caption><thead className="bg-slate-50 text-slate-600"><tr><th scope="col" className="w-12 p-4"><span className="sr-only">Select</span><input type="checkbox" aria-label="Select all records" disabled={loading || !items.length} checked={items.length > 0 && items.every(row=>selectedIds.includes(row.id))} onChange={e=>setChecked({key:requestKey,ids:e.target.checked?items.map(row=>row.id):[]})} /></th><th scope="col" className="p-4">Actions</th>{columns.includes("name") && <SortableHeader className="p-4" label="Name" column="name" sort={parseTableSort(query.sort)} disabled={loading} onSort={sort=>filter({sort})}/>}{columns.includes("status") && <SortableHeader className="p-4" label="Status" column="status" sort={parseTableSort(query.sort)} disabled={loading} onSort={sort=>filter({sort})}/>}{columns.includes("category") && <SortableHeader className="p-4" label="Category" column="category" sort={parseTableSort(query.sort)} disabled={loading} onSort={sort=>filter({sort})}/>}{columns.includes("updatedAtUtc") && <SortableHeader className="p-4" label="Updated" column="updatedAtUtc" sort={parseTableSort(query.sort)} disabled={loading} onSort={sort=>filter({sort})}/>}{fields.filter(f => f.kind === query.kind && columns.includes(`field:${f.id}`)).map(f => <SortableHeader key={f.id} className="p-4" label={f.name} column={`field:${f.id}`} sort={parseTableSort(query.sort)} disabled={loading} onSort={sort=>filter({sort})}/>)}</tr></thead><tbody>{!loading && items.map(row => <tr key={row.id} className="border-t border-slate-100 hover:bg-indigo-50/30"><td className="p-4"><input type="checkbox" aria-label={`Select ${row.name}`} checked={selectedIds.includes(row.id)} onChange={e=>setChecked({key:requestKey,ids:e.target.checked?[...selectedIds,row.id]:selectedIds.filter(id=>id!==row.id)})} /></td><td className="p-4"><div className="flex justify-start gap-1"><button className="rounded-lg p-2 text-slate-500 hover:bg-indigo-50 hover:text-indigo-700" aria-label={`View ${row.name}`} title="View" onClick={() => { setSelected(row.id); setSelectedReadOnly(true); setCreating(false); }}><Eye className="size-4" /></button>{canManage && <button className="rounded-lg p-2 text-slate-500 hover:bg-indigo-50 hover:text-indigo-700" aria-label={`Edit ${row.name}`} title="Edit" onClick={() => { setSelected(row.id); setSelectedReadOnly(false); setCreating(false); }}><Pencil className="size-4" /></button>}{canManage && <button className="rounded-lg p-2 text-rose-500 hover:bg-rose-50 hover:text-rose-700" aria-label={`Delete ${row.name}`} title="Delete" onClick={() => void perform(async () => { await crm(`/records/${row.id}/archive`, "POST", { version: row.version }); })}><Trash2 className="size-4" /></button>}</div></td>{columns.includes("name") && <th scope="row" className="p-4"><button className={listingStyles.recordName} onClick={() => { setSelected(row.id); setSelectedReadOnly(true); setCreating(false); }}><span className="grid size-8 place-items-center rounded-full bg-slate-100 text-slate-500">{query.kind === "account" ? <Building2 className="size-4" aria-hidden="true" /> : <UserRound className="size-4" aria-hidden="true" />}</span><span>{row.name}</span></button></th>}{columns.includes("status") && <td className="p-4">{row.status}</td>}{columns.includes("category") && <td className="p-4">{row.category ?? "—"}</td>}{columns.includes("updatedAtUtc") && <td className="p-4">{new Date(row.updatedAtUtc).toLocaleString()}</td>}{fields.filter(f => f.kind === query.kind && columns.includes(`field:${f.id}`)).map(f => { const v = row.customFields?.find(x => x.definitionId === f.id); return <td key={f.id} className="p-4">{v?.textValue ?? v?.numberValue ?? (v?.dateValue ? new Date(v.dateValue).toLocaleDateString() : v?.booleanValue == null ? "—" : v.booleanValue ? "Yes" : "No")}</td>; })}</tr>)}</tbody></table></div>
      {loading ? <p className="p-8 text-center" role="status">Loading relationships…</p> : items.length === 0 && <p className="p-8 text-center text-slate-600">No records match these filters. Adjust your search or create the first {query.kind}.</p>}
      <footer className="flex flex-wrap gap-3 items-center justify-between border-t border-slate-200 p-4"><div className="flex items-center gap-3"><ColumnChooser>{columnControls}</ColumnChooser><p className="text-sm" aria-live="polite">{total} records · Page {query.page}</p></div><label className="text-sm">Rows per page <select aria-label="Rows per page" className="ml-2 rounded border p-1" value={query.pageSize ?? 20} onChange={e=>filter({pageSize:Number(e.target.value)})}>{[10,20,50,100].map(n=><option key={n}>{n}</option>)}</select></label><div className="flex gap-2"><button className={button} disabled={query.page <= 1 || loading} onClick={() => filter({ page: query.page - 1 })}>Previous</button><button className={button} disabled={query.page * (query.pageSize ?? 20) >= total || loading} onClick={() => filter({ page: query.page + 1 })}>Next</button></div></footer>
    </section>
    {selected && <RecordDetail key={selected} id={selected} directory={directory} fields={fields} permissions={permissions} readOnly={selectedReadOnly} onChanged={() => setRefresh(r => r + 1)} />}
  </main>;
}

function RecordForm({ row, kind, directory, busy, onSave, onCancel }: { row?: RecordRow; kind: string; directory: Directory; busy: boolean; onSave: (r: unknown) => Promise<void>; onCancel: () => void }) {
  return <form className="grid gap-4 sm:grid-cols-2" onSubmit={e => { const r = data(e); const attributes = attributeValues(r); void onSave({ ...r, attributes, kind, category: r.category || null, ownerUserId: r.ownerUserId || null, ownerTeamId: r.ownerTeamId || null, version: row?.version }); }}>
    <Label title={kind === "contact" ? "Label Name" : "Account Name"}><input className={input} name="name" defaultValue={row?.name} maxLength={160} required /></Label>
    <Label title="Status"><select className={input} name="status" defaultValue={row?.status ?? "active"}>{["active", "prospect", "inactive", "lapsed"].map(s => <option key={s}>{s}</option>)}</select></Label>
    <Label title="Category"><input className={input} name="category" defaultValue={row?.category ?? ""} maxLength={100} /></Label>
    <Label title="Owner"><select className={input} name="ownerUserId" defaultValue={row?.ownerUserId ?? ""}><option value="">Unassigned</option>{directory.users.map(u => <option key={u.id} value={u.id}>{u.name}</option>)}</select></Label>
    <Label title="Team"><select className={input} name="ownerTeamId" defaultValue={row?.ownerTeamId ?? ""}><option value="">Unassigned</option>{directory.teams.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}</select></Label>

    <div className="flex items-end gap-2"><button className={primary} disabled={busy}>Save {kind}</button><button type="button" className={button} onClick={onCancel}>Cancel</button></div>
  </form>;
}

type AccountContactList = { items: Array<{ association: { id: string; contactId: string; isMainAccount: boolean; department?: string | null; jobTitle?: string | null; workClassification: string; isMainContact: boolean; isAuthorisedContact: boolean; startDate: string; endDate?: string | null; endReason?: string | null; version?: string }; accountName: string; contactName: string; accountArchived: boolean; contactArchived: boolean; contactVersion: string; roles: string[] }>; totalCount: number; page: number; pageSize: number };

function AccountContactsPanel({ recordId, refresh, editable, busy, recordKind, onLink, onEnd, onUpdate, onMain }: { recordId: string; refresh: number; editable: boolean; busy: boolean; recordKind: "account" | "contact"; onMain: (contactId: string, version: string, associationId: string | null) => void; onLink: (contactId: string, values: Record<string, string>) => void; onEnd: (id: string, version: string | undefined, values: Record<string, string>) => void; onUpdate: (id: string, version: string | undefined, values: { department: string | null; jobTitle: string | null; workClassification: string; isMainContact: boolean; isAuthorisedContact: boolean; roles: string[] }) => void }) {
  const [associationPage, setAssociationPage] = useState(1);
  const [includeEnded, setIncludeEnded] = useState(false);
  const [retry, setRetry] = useState(0);
  const [loaded, setLoaded] = useState<{ key: string; result?: AccountContactList; error?: string } | null>(null);
  const requestKey = `${recordId}/${associationPage}/${includeEnded}/${refresh}/${retry}`;
  const result = loaded?.key === requestKey ? loaded.result : undefined;
  const loadError = loaded?.key === requestKey ? loaded.error : undefined;
  useEffect(() => {
    let active = true;
    crm<AccountContactList>(`/records/${recordId}/account-contacts?page=${associationPage}&includeEnded=${includeEnded}`)
      .then(result => {
        if (!active) return;
        const lastPage = Math.max(1, Math.ceil(result.totalCount / result.pageSize));
        if (associationPage > lastPage) { setAssociationPage(lastPage); return; }
        setLoaded({ key: requestKey, result });
      }).catch(() => { if (active) setLoaded({ key: requestKey, error: "Associations could not be loaded." }); });
    return () => { active = false; };
  }, [recordId, associationPage, includeEnded, requestKey]);
  const [contactSearch, setContactSearch] = useState("");
  const [contacts, setContacts] = useState<RecordRow[]>([]);
  const [roles, setRoles] = useState<string[]>([]);
  const [ending, setEnding] = useState<{ id: string; version?: string } | null>(null);
  const [editing, setEditing] = useState<string | null>(null);
  useEffect(() => { crm<{ roles: string[] }>("/account-contact-options").then(x => setRoles(x.roles)).catch(() => undefined); }, []);
  const historyControl = <label className="mb-4 flex items-center gap-2 text-sm"><input type="checkbox" checked={includeEnded} disabled={busy} onChange={event => { setIncludeEnded(event.target.checked); setAssociationPage(1); setEditing(null); setEnding(null); }} />Include ended associations</label>;
  if (loadError) return <>{historyControl}<div role="alert"><p>{loadError}</p><button className={button} onClick={() => setRetry(x => x + 1)}>Retry associations</button></div></>;
  if (!result) return <>{historyControl}<p className="text-sm text-slate-500" role="status">Loading associations…</p></>;
  return <>
    {historyControl}
    {result.items.length === 0 && <p className="text-sm text-slate-500">No {includeEnded ? "" : "current "}{recordKind === "account" ? "contacts are linked to this account" : "employers are linked to this contact"}.</p>}
    <ul className="space-y-3">
      {result.items.map(item => <li key={item.association.id} className="min-w-0 rounded-xl border border-slate-200 bg-slate-50 p-4 text-sm break-words">
        <div className="flex flex-wrap items-center justify-between gap-2"><span className="flex min-w-0 items-center gap-2 font-semibold text-slate-900">{recordKind === "account" ? <UserRound aria-hidden="true" className="size-5 shrink-0 text-indigo-600" /> : <Building2 aria-hidden="true" className="size-5 shrink-0 text-indigo-600" />}{recordKind === "account" ? item.contactName : item.accountName}</span><span className="flex flex-wrap gap-1 text-xs">{item.association.isMainAccount && <span className="rounded-full bg-sky-100 px-2 py-1 text-sky-800">{item.association.endDate ? "Main account at end" : "Main account"}</span>}{item.association.isMainContact && <span className="rounded-full bg-indigo-100 px-2 py-1 text-indigo-800">Main contact</span>}{item.association.isAuthorisedContact && <span className="rounded-full bg-emerald-100 px-2 py-1 text-emerald-800">Authorised</span>}</span></div>
        <p className="mt-1 text-xs text-slate-600">{item.association.department && <span>Department: {item.association.department} · </span>}{item.association.jobTitle || "No job title"} · {item.association.workClassification.replace("-", " ")}</p>
        <p className="mt-2 text-xs text-slate-600">Started {item.association.startDate.slice(0, 10)} · {item.association.endDate ? `Ended ${item.association.endDate.slice(0, 10)}` : "Current association"}</p>
        {item.association.endDate && item.association.endReason && <p className="mt-1 whitespace-pre-wrap break-words text-xs text-slate-600">Reason: {item.association.endReason}</p>}
        {(item.accountArchived || item.contactArchived) && <p className="mt-1 text-xs text-amber-800">Linked record is archived.</p>}
        {editing === item.association.id && !item.association.endDate && !item.accountArchived && !item.contactArchived ? <form aria-label="Edit association details" className="mt-3 space-y-3 rounded-lg border border-indigo-200 bg-white p-4" onSubmit={e => { e.preventDefault(); const form = new FormData(e.currentTarget); onUpdate(item.association.id, item.association.version, { department: String(form.get("department") || "") || null, jobTitle: String(form.get("jobTitle") || "") || null, workClassification: String(form.get("workClassification")), isMainContact: form.get("isMainContact") === "on", isAuthorisedContact: form.get("isAuthorisedContact") === "on", roles: form.getAll("roles").map(String) }); setEditing(null); }}><p className="font-semibold text-slate-900">Edit association with {recordKind === "account" ? item.contactName : item.accountName}</p><Label title="Department"><input className={input} name="department" defaultValue={item.association.department ?? ""} maxLength={160} /></Label><Label title="Job title"><input className={input} name="jobTitle" defaultValue={item.association.jobTitle ?? ""} maxLength={160} /></Label><Label title="Work classification"><select className={input} name="workClassification" defaultValue={item.association.workClassification}><option>unknown</option><option>technical</option><option>non-technical</option></select></Label><div className="flex flex-wrap gap-3 text-xs"><label><input type="checkbox" name="isMainContact" defaultChecked={item.association.isMainContact} /> Main contact</label><label><input type="checkbox" name="isAuthorisedContact" defaultChecked={item.association.isAuthorisedContact} /> Authorised contact</label></div><Label title="Roles"><select multiple className={`${input} min-h-24`} name="roles" defaultValue={item.roles}>{roles.map(role => <option key={role} value={role}>{role}</option>)}</select></Label><div className="flex gap-2"><button className={primary} disabled={busy}>Save</button><button type="button" className={button} onClick={() => setEditing(null)}>Cancel</button></div></form> : <><p className="mt-2 text-xs text-slate-700">{item.roles.length ? item.roles.join(", ") : "No roles recorded"}</p>{editable && !item.association.endDate && <div className="mt-3 flex flex-wrap gap-2"><button className={button} disabled={busy || item.accountArchived || item.contactArchived} onClick={() => setEditing(item.association.id)}>Edit association</button><button className={button} disabled={busy || item.contactArchived || (!item.association.isMainAccount && item.accountArchived)} onClick={() => onMain(item.association.contactId, item.contactVersion, item.association.isMainAccount ? null : item.association.id)}>{item.association.isMainAccount ? "Clear main account" : "Set as main account"}</button><button className={button} disabled={busy} onClick={() => setEnding({ id: item.association.id, version: item.association.version })}>End association</button></div>}</>}
      </li>)}
    </ul>
    <p className="mt-3 text-xs text-slate-500">{result.totalCount} {includeEnded ? "current and ended" : "current"} {recordKind === "account" ? "contact" : "employer"}{result.totalCount === 1 ? "" : "s"}</p>
    <nav aria-label="Association pages" className="mt-3 flex flex-wrap items-center gap-3 text-sm">
      <button className={button} disabled={busy || result.page <= 1} onClick={() => { setAssociationPage(p => p - 1); setEditing(null); setEnding(null); }}>Previous associations</button>
      <span>Page {result.page} of {Math.max(1, Math.ceil(result.totalCount / result.pageSize))}</span>
      <button className={button} disabled={busy || result.page * result.pageSize >= result.totalCount} onClick={() => { setAssociationPage(p => p + 1); setEditing(null); setEnding(null); }}>Next associations</button>
    </nav>
    {ending && <form aria-label="End association details" className="mt-5 space-y-3 rounded-lg border border-amber-200 bg-amber-50 p-4" onSubmit={e => { const values = data(e); onEnd(ending.id, ending.version, values); setEnding(null); }}><p className="text-sm font-semibold text-slate-900">End association with {result.items.filter(item => item.association.id === ending.id).map(item => recordKind === "account" ? item.contactName : item.accountName).join("")}</p><Label title="End date"><input className={input} type="date" name="endDate" defaultValue={new Date().toISOString().slice(0, 10)} required /></Label><Label title="Reason"><input className={input} name="reason" maxLength={500} defaultValue="Association ended" required /></Label><div className="flex gap-2"><button className={primary} disabled={busy}>Confirm end</button><button type="button" className={button} onClick={() => setEnding(null)}>Cancel</button></div></form>}
    {editable && recordKind === "account" && <form className="mt-5 space-y-3 border-t border-slate-200 pt-4" onSubmit={e => { const values = data(e); onLink(values.contactId, values); }}>
      <p className="text-sm font-semibold text-slate-900">Link an existing contact</p>
      <div className="flex items-end gap-2"><Label title="Search contacts"><input className={input} value={contactSearch} onChange={e => setContactSearch(e.target.value)} placeholder="Name" /></Label><button type="button" className={button} disabled={busy || !contactSearch.trim()} onClick={() => crm<{ items: RecordRow[] }>(`/records?kind=contact&search=${encodeURIComponent(contactSearch)}&pageSize=25`).then(x => setContacts(x.items)).catch(() => setContacts([]))}>Find</button></div>
      <Label title="Contact"><select name="contactId" className={input} required><option value="">Choose a contact</option>{contacts.map(contact => <option key={contact.id} value={contact.id}>{contact.name}</option>)}</select></Label>
      <div className="grid gap-3 sm:grid-cols-2"><Label title="Start date"><input className={input} type="date" name="startDate" defaultValue={new Date().toISOString().slice(0, 10)} required /></Label><Label title="Job title"><input className={input} name="jobTitle" maxLength={160} /></Label><Label title="Department"><input className={input} name="department" maxLength={160} /></Label></div>
      <div className="grid gap-3 sm:grid-cols-2"><Label title="Work classification"><select className={input} name="workClassification" defaultValue="unknown"><option>unknown</option><option>technical</option><option>non-technical</option></select></Label><Label title="Role"><select className={input} name="role"><option value="">No role</option>{roles.map(role => <option key={role} value={role}>{role}</option>)}</select></Label></div>
      <button className={primary} disabled={busy}>Link contact</button>
    </form>}
  </>;
}

function RecordDetail({ id, directory, permissions, readOnly, onChanged }: { id: string; directory: Directory; fields: Field[]; permissions: string[]; readOnly: boolean; onChanged: () => void }) {
  const [sidePanelCollapsed, toggleSidePanel] = useCollapsedPanel("nexora.layout.record-side-panel-collapsed");
  const sidePanelId = useId();
  const [confirmation, setConfirmation] = useState<{ message: string; action: () => Promise<void> } | null>(null);
  const recordHeading = useRef<HTMLDivElement>(null);
  const focused = useRef(false);
  const [detail, setDetail] = useState<Detail | null>(null);
  const [refresh, setRefresh] = useState(0);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [editing, setEditing] = useState(false);
  const [actionsOpen, setActionsOpen] = useState(false);
  const [targets, setTargets] = useState<RecordRow[]>([]);
  const manage = permissions.includes("crm.manage");
  useEffect(() => {
    let current = true;
    crm<Detail>(`/records/${id}`).then(d => { if (current) setDetail(d); }).catch(e => { if (current) setError(e.message); });
    return () => { current = false; };
  }, [id, refresh]);
  useEffect(() => {
    if (detail && !focused.current) {
      focused.current = true;
      recordHeading.current?.focus();
    }
  }, [detail]);
  async function perform(action: () => Promise<void>) { setBusy(true); setError(""); try { await action(); setRefresh(r => r + 1); onChanged(); } catch (e) { setError(e instanceof Error ? e.message : "Unable to save."); } finally { setBusy(false); } }
  function add(collection: string, event: FormEvent<HTMLFormElement>) { const r = data(event); const form = event.currentTarget; void perform(async () => { await crm(`/records/${id}/${collection}`, "POST", r); form.reset(); }); }
  async function addTag(name: string) {
    setBusy(true);
    setError("");
    try {
      const saved = await crm<{ id: string }>(`/records/${id}/tags`, "POST", { name });
      setDetail(current => current ? { ...current, tags: [...current.tags, { id: saved.id, name: name.toLowerCase() }] } : current);
      setRefresh(value => value + 1);
      onChanged();
    } finally { setBusy(false); }
  }
  const editable = manage && !readOnly && detail && !detail.record.isArchived;
  const contactName = [detail?.contactDetails?.firstName, detail?.contactDetails?.middleName, detail?.contactDetails?.lastName].map(part => part?.trim()).filter(Boolean).join(" ");
  const displayName = detail?.record.kind === "contact" ? contactName || detail.record.name || "Unnamed contact" : detail?.record.name ?? "";
  function section(title: string, collection: keyof Detail, removable = false) {
    const rows = detail?.[collection] as Item[];
    return <ul className="mb-4 space-y-3">{rows.length === 0 && <li className="text-sm text-slate-500">No {title.toLowerCase()} yet.</li>}{rows.map(row => <li key={row.id} className="flex items-start justify-between gap-2 rounded-lg bg-slate-50 p-3 text-sm"><div className="whitespace-pre-wrap break-words">{row.name || row.body || row.label || row.value}{row.line1 && <p>{[row.line1, row.city, row.postalCode, row.country].filter(Boolean).join(", ")}</p>}{row.consent && <p className="mt-1 text-xs text-slate-600">{row.kind} · Consent: {row.consent}{row.consentEvidence ? ` · ${row.consentEvidence}` : ""}</p>}{row.targetRecordId && <p className="text-xs text-slate-600">{row.label}</p>}</div>{removable && editable && <button className={button} disabled={busy} aria-label={`Remove ${row.name || row.label || row.value}`} onClick={() => setConfirmation({ message: `Remove this item from ${detail?.record.name}?`, action: async () => { await crm(`/records/${id}/${collection}/${row.id}`, "DELETE"); } })}>Remove</button>}</li>)}</ul>;
  }
  return <section className="space-y-4" aria-label="Selected record">
    {confirmation && <ConfirmDialog message={confirmation.message} onCancel={() => setConfirmation(null)} onConfirm={() => { const action = confirmation.action; setConfirmation(null); void perform(action); }} />}
    {error && <div role="alert" className="rounded-lg bg-red-50 p-4 text-sm text-red-800">{error} <button className={button} onClick={() => { setError(""); setRefresh(r => r + 1); }}>Reload record</button></div>}
    {!detail ? <p role="status">Loading record…</p> : <RecordAttributeSections record={detail.record} values={detail.fields} editable={Boolean(editable)} busy={busy} onSave={attributes => void perform(async () => { await crm(`/records/${id}`, "PATCH", { ...detail.record, attributes }); })}>
      {editing && editable && <div className={styles.recordEdit}><RecordForm key={detail.record.version} row={detail.record} kind={detail.record.kind} directory={directory} busy={busy} onCancel={() => setEditing(false)} onSave={r => perform(async () => { await crm(`/records/${id}`, "PATCH", r); setEditing(false); })} /></div>}
      <div className={styles.panelTools}><PanelToggle label="record side panel" collapsed={sidePanelCollapsed} controls={sidePanelId} onToggle={toggleSidePanel}>Side panel</PanelToggle>{sidePanelCollapsed && <h2>{displayName}</h2>}</div><div className={styles.layout} data-side-panel-collapsed={sidePanelCollapsed}><div id={sidePanelId} hidden={sidePanelCollapsed} className={styles.rail} role="complementary" aria-label="Record tags"><div ref={recordHeading} tabIndex={-1} aria-label={displayName} className={`${styles.identity} scroll-mt-24 outline-none`}><h2>{displayName}</h2><div className="mb-4 flex flex-wrap items-center gap-2"><div className="relative ml-auto"><button className={button} aria-label="Record actions" aria-expanded={actionsOpen} onClick={() => setActionsOpen(open => !open)}><MoreHorizontal className="size-4" /></button>{actionsOpen && <div className="absolute right-0 z-20 mt-2 w-44 rounded-lg border border-slate-200 bg-white p-1 shadow-lg">{editable && <button className="block w-full rounded-md px-3 py-2 text-left text-sm hover:bg-slate-50" onClick={() => { setEditing(true); setActionsOpen(false); }}>Edit details</button>}{manage && <button className="block w-full rounded-md px-3 py-2 text-left text-sm hover:bg-slate-50" disabled={busy} onClick={() => { setActionsOpen(false); setConfirmation({ message: detail.record.isArchived ? "Restore this record to current lists?" : "Archive this record? It will leave current lists and remain available for restoration.", action: async () => { await crm(`/records/${id}/${detail.record.isArchived ? "restore" : "archive"}`, "POST", { version: detail.record.version }); } }); }}>{detail.record.isArchived ? "Restore" : "Archive"}</button>}</div>}</div></div></div><div className={styles.statusBanner} data-status={detail.record.isArchived?"archived":detail.record.status} aria-hidden="true" /><RecordPhoto record={detail.record} name={displayName} canRead={permissions.includes("crm.files")} /><div className={styles.recordStatus} aria-label="Record status" data-status={detail.record.isArchived ? "archived" : detail.record.status}>{detail.record.isArchived ? "Archived" : detail.record.status}</div>{detail.record.kind === "account" && permissions.includes("crm.files") && <RecordPhotoField record={detail.record} disabled={!editable || busy} onChanged={()=>{setRefresh(n=>n+1);onChanged();}}/>}<RecordTags tags={detail.tags} editable={Boolean(editable)} busy={busy} onAdd={addTag} onRemove={tag => setConfirmation({ message: `Remove "${tag.name}" from ${displayName}?`, action: async () => { await crm(`/records/${id}/tags/${tag.id}`, "DELETE"); } })} /></div><ConfiguredSections module="crm" kind={detail.record.kind} className={styles.content}>
      <RecordProfile data-section={detail.record.kind === "contact" ? "Contact details" : "Organisation details"} key={`details-${id}-${detail.record.version}`} kind={detail.record.kind} photoRecord={detail.record} canReadPhoto={permissions.includes("crm.files")} onPhotoChanged={()=>{setRefresh(n=>n+1);onChanged();}} contactDetails={detail.contactDetails} generalInformation={detail.generalInformation} communicationPermissions={detail.communicationPermissions} socialMediaDetails={detail.socialMediaDetails} accountDetails={detail.accountDetails} accountGeneralInformation={detail.accountGeneralInformation} editable={Boolean(editable)} busy={busy} onSave={values => void perform(async () => { await crm(`/records/${id}/profile`, "PUT", { version: detail.record.version, ...values }); })} />
      <RecordProfiles data-section="Profile" key={`profiles-${id}-${detail.record.version}`} id={id} version={detail.record.version} assignments={detail.profiles} editable={Boolean(editable)} busy={busy} onChanged={()=>{setRefresh(n=>n+1);onChanged();}} />
      {permissions.includes("membership.read") && <RecordMemberships data-section="Membership details" key={`${id}-${refresh}`} id={id} kind={detail.record.kind} />}
        {(detail.record.kind === "account" || detail.record.kind === "contact") && <RecordSection title={detail.record.kind === "account" ? "Contacts" : "Employers"}><p className="mb-4 text-sm text-slate-600">Structured associations · Employment, roles and account responsibilities, with current and ended history.</p><AccountContactsPanel onMain={(contactId, version, associationId) => void perform(async () => { await crm(`/contacts/${contactId}/main-account`, "PUT", { version, associationId }); })} recordKind={detail.record.kind} recordId={id} refresh={refresh} editable={Boolean(editable)} busy={busy} onLink={(contactId, values) => void perform(async () => { await crm(`/accounts/${id}/contacts`, "POST", { contactId, startDate: `${values.startDate}T00:00:00Z`, department: values.department || null, jobTitle: values.jobTitle || null, workClassification: values.workClassification, isMainContact: false, isAuthorisedContact: false, roles: values.role ? [values.role] : [] }); })} onEnd={(associationId, version, values) => void perform(async () => { await crm(`/account-contacts/${associationId}/end`, "POST", { version, endDate: `${values.endDate}T00:00:00Z`, reason: values.reason }); })} onUpdate={(associationId, version, values) => void perform(async () => { await crm(`/account-contacts/${associationId}`, "PATCH", { version, ...values }); })} /></RecordSection>}
        <RecordSection data-section="Addresses" title="Addresses" onLoad={async () => { setDetail(await crm<Detail>(`/records/${id}`)); }}><RecordAddresses key={`${id}-${detail.record.version}`} items={detail.addresses} billingUsesMailingAddress={Boolean(detail.record.billingUsesMailingAddress)} effectiveBillingAddress={detail.effectiveBillingAddress} editable={Boolean(editable)} busy={busy} onSave={(values, addressId) => void perform(async () => { await crm(`/records/${id}/addresses${addressId ? `/${addressId}` : ""}`, addressId ? "PUT" : "POST", { ...values, version: detail.record.version }); })} onBillingSource={useMailingAddress => void perform(async () => { await crm(`/records/${id}/billing-address`, "PUT", { version: detail.record.version, useMailingAddress }); })} onRemove={item => setConfirmation({ message: `Remove ${item.label} from ${detail.record.name}?`, action: async () => { await crm(`/records/${id}/addresses/${item.id}`, "DELETE"); } })} /></RecordSection>
        <RecordSection data-section="Communication methods" title="Communication Methods" onLoad={async () => { setDetail(await crm<Detail>(`/records/${id}`)); }}><CommunicationMethods key={`${id}-${detail.record.version}`} items={detail.communications} editable={Boolean(editable)} busy={busy} onSave={(values, communicationId) => void perform(async () => { await crm(`/records/${id}/communications${communicationId ? `/${communicationId}` : ""}`, communicationId ? "PUT" : "POST", { ...values, version: detail.record.version }); })} onRemove={item => setConfirmation({ message: `Remove ${item.value} from ${detail.record.name}?`, action: async () => { await crm(`/records/${id}/communications/${item.id}`, "DELETE"); } })} /></RecordSection>
        <RecordSection data-section="Related records" title="Related Records" onLoad={async () => { setDetail(await crm<Detail>(`/records/${id}`)); }}><p className="mb-4 text-sm text-slate-600">Other connections · General links between records, separate from employment and account responsibilities.</p>{section("Relationships", "relationships", true)}{editable && <><form className="mb-3 flex items-end gap-2" onSubmit={e => { const r = data(e); void perform(async () => { const result = await crm<{ items: RecordRow[] }>(`/records?kind=${r.kind}&search=${encodeURIComponent(r.search)}&pageSize=100`); setTargets(result.items.filter(x => x.id !== id)); }); }}><Label title="Find related"><input name="search" className={input} maxLength={160} required /></Label><Label title="Type"><select name="kind" className={input}><option value="account">Accounts</option><option value="contact">Contacts</option></select></Label><button className={button} disabled={busy}>Find</button></form><form className="space-y-3" onSubmit={e => add("relationships", e)}><Label title="Related record"><select className={input} name="targetRecordId" required><option value="">Choose a search result</option>{targets.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}</select></Label><Label title="Relationship (for example, works at)"><input className={input} name="label" maxLength={100} required /></Label><button className={button} disabled={busy}>Link record</button></form></>}</RecordSection>
        <RecordSection data-section="Notes" title="Notes" onLoad={async () => { setDetail(await crm<Detail>(`/records/${id}`)); }}>{section("Notes", "notes")}{editable && <form className="space-y-3" onSubmit={e => add("notes", e)}><Label title="New note"><textarea name="body" className={input} rows={4} maxLength={4000} required /></Label><button className={button} disabled={busy}>Add note</button></form>}</RecordSection>
        <RecordSection data-section="Files" title="Files" onLoad={async () => { setDetail(await crm<Detail>(`/records/${id}`)); }}><ul className="mb-4 space-y-2">{detail.files.length === 0 && <li className="text-sm text-slate-500">No files attached.</li>}{detail.files.map(f => <li key={f.id}>{permissions.includes("crm.files") && f.scanStatus === "clean" ? <a className="text-sm font-medium text-indigo-700 underline" href={`/api/v1/crm/records/${id}/files/${f.id}`}>{f.name}</a> : <span className="text-sm">{f.name} · {permissions.includes("crm.files") ? f.scanStatus : "File access required"}</span>}{editable && permissions.includes("crm.files") && <span className="ml-3 inline-flex gap-2">{f.scanStatus === "quarantined" && <button className={button} disabled={busy} onClick={() => void perform(async () => { await crm(`/records/${id}/files/${f.id}/scan`, "POST", { version: f.version }); })}>Retry scan</button>}<button className={button} disabled={busy} onClick={() => setConfirmation({ message: `Delete ${f.name}? This removes the stored file and retains its activity history.`, action: async () => { await crm(`/records/${id}/files/${f.id}`, "DELETE", { version: f.version }); } })}>Delete file</button></span>}</li>)}</ul>{editable && permissions.includes("crm.files") && <form className="space-y-3" onSubmit={e => { e.preventDefault(); const form = e.currentTarget; const body = new FormData(form); body.set("version", detail.record.version); void perform(async () => { await crm(`/records/${id}/files`, "POST", body); form.reset(); }); }}><Label title="Attach file (up to 5 MB)"><input className={input} type="file" name="file" accept=".pdf,.txt,.png,.jpg,.jpeg" required /></Label><p className="text-xs text-slate-600">PDF, text, PNG or JPEG. Files require a clean scan and download as attachments.</p><button className={button} disabled={busy}>Upload file</button></form>}</RecordSection>
        <RecordTimeline data-section="Activity timeline" id={id} refresh={refresh} directory={directory} />

      <ConnectedRecord data-section="Connected Activity" id={id} refresh={refresh} />
      </ConfiguredSections><div hidden={sidePanelCollapsed} className={styles.auditFooter}><RecordDataAudit id={id} refresh={refresh} createdAtUtc={detail.record.createdAtUtc}/></div></div>
    </RecordAttributeSections>}
  </section>;
}
