# Nexora CRM accounts and associations

Document 6 of the Nexora learning guides  
Source snapshot 13 October 2026 at 17:12 UTC


Understand company records and the history linking people to organisations.

### Recognise the screen and its scope

Open `/relationships/accounts`. CRM routes use port 3017; member routes use 3018; public website routes can use 3019. Brackets name a route parameter, not text to paste into a browser. Choose a real record or site id from the application.

This guide covers Account forms and general information; Contact to Account links; Start and end dates; Department job title classification and roles; Main and authorised contacts; Transfer and association history.

![File journey for Nexora CRM accounts and associations](../assets/platform/guide-06-map.png)

The diagram shows the file roles. The full paths below are what you paste into Ctrl+P.

### Find the files in VS Code

| Role | File and source ID | What it does |
| --- | --- | --- |
| Screen | S0080 crm-workspace.tsx | Main UI or route for this topic |
| Server | S0239 AccountContactEndpoints.cs | Endpoints or server rules |
| Data | S0253 AccountContact.cs | Data shape or persistence structure |

**Screen S0080**  
[apps/web/src/features/crm/crm-workspace.tsx](<D:/Projects new/Nexora/apps/web/src/features/crm/crm-workspace.tsx>)

**Server S0239**  
[backend/src/Modules/Nexora.Modules.Crm/AccountContactEndpoints.cs](<D:/Projects new/Nexora/backend/src/Modules/Nexora.Modules.Crm/AccountContactEndpoints.cs>)

**Data S0253**  
[backend/src/Modules/Nexora.Modules.Crm/Domain/AccountContact.cs](<D:/Projects new/Nexora/backend/src/Modules/Nexora.Modules.Crm/Domain/AccountContact.cs>)

The complete HTML manual opens these source IDs inside its Source library. The Word manual shows short labelled extracts so you can learn the flow before reading large files. Each individual Markdown guide includes its complete primary files after the walkthrough.

### Follow the request and the records

1. The accounts route selects account as the record kind.
2. Opening an Account loads its saved detail.
3. The association panel asks for currently linked Contacts and optional history.
4. A link or change sends dates, role information and the expected version.
5. The backend checks tenant boundaries and preserves association history.

### Read the code with an explanation

A Contact is a person and an Account is an organisation. Their association is a separate record, so a person can change organisation without rewriting their identity. Ending an association stores its end date. Authorised-contact flags describe the relationship, but explicit portal grants control member access to company data.

Source reference S0080 apps/web/src/features/crm/crm-workspace.tsx

Selected opening lines with their original numbers. The complete file is in the HTML source library.

```tsx
0001  "use client";
0002  import styles from "./record-detail.module.css";
0003  import listingStyles from "./record-listing.module.css";
0004  import { RecordSection } from "./record-section";
0005  import { RecordTimeline } from "./record-timeline";
0006  import { RecordDataAudit } from "./record-data-audit";
```


The import lines lead to shared components and helpers. Start at the exported component or route function; follow the request helper to the server rather than assuming the whole feature lives in this file.

Source reference S0239 backend/src/Modules/Nexora.Modules.Crm/AccountContactEndpoints.cs

Extract from source line 17. A fragment from a long line is shown without the rest of that line.

```cs
0017          api.MapGet("/account-contact-options", () => new {
```


On the server, look for MapGet to load records, MapPost to create or perform an action, MapPut or MapPatch to change data, and RequireAuthorization for permissions. Some shared methods register endpoints indirectly. Find SaveChangesAsync to see the database write. A request method alone does not tell you every side effect.

Source reference S0253 backend/src/Modules/Nexora.Modules.Crm/Domain/AccountContact.cs

Selected opening lines with their original numbers. The complete file is in the HTML source library.

```cs
0001  namespace Nexora.Modules.Crm.Domain;
0003  // A person's association with an organisation; it is not a login or membership.
0004  public sealed class AccountContact : TenantRow
0005  {
0006      public Guid AccountId { get; set; }
0007      public Guid ContactId { get; set; }
```


Follow model fields to their request names and database mapping. An id points to a particular record. A tenant id limits the organisation. A version or concurrency stamp protects newer edits. Some model files describe contracts rather than tables; check the owning DbContext before assuming storage.

### Make or trace your first change

1. Press Ctrl+P and open [apps/web/src/features/crm/crm-workspace.tsx](<D:/Projects new/Nexora/apps/web/src/features/crm/crm-workspace.tsx>).
2. Press Ctrl+F and search for `Account Name`. In this snapshot it appears at line 107.
3. Replace this visible Account label only. Open the Account form and check the Contact form still uses its own label. Leave kind and the input name unchanged.
4. Replace only `Account Name` with `Organisation name` in that intended location. Keep quotes, tags, JSON keys and routes intact.
5. Save with Ctrl+S. Inspect Problems and the local screen. Review the diff and restore the original text when you finish practising.

Extract from source line 107. A fragment from a long line is shown without the rest of that line.

```tsx
0107      <Label title={kind === "contact" ? "Label Name" : "Account Name"}><input className={input} name="name" defaultValue={row?.name} maxLength={160} required /></Label>
```


These exercises have not been applied to the application. Their search phrases are verified against the captured source. Line numbers can move; the phrase and file path are your main navigation aids.

### Check a change and diagnose problems

Confirm the original screen still loads, the changed words appear in the intended place, and another screen using the same component still behaves correctly. If an action fails, inspect the browser Network response, the relevant helper and server validation message. Use the common error table at the beginning of the manual. Reload on a version conflict instead of removing the version check.

A syntax error after a text edit usually means a quote, brace or closing tag changed. Compare your diff with the original. An unchanged browser result can mean the wrong port, a saved published configuration overriding a default, or a running API that has not reloaded.

### Understand the current limits

Do not assume an association grants portal access. Ending or transferring links has operational consequences; the beginner exercise changes wording only. Existing history must be preserved.

### Continue learning

Related guides [05 Nexora CRM contacts](#guide_05), [08 Nexora record profiles and related information](#guide_08), [20 Nexora administration and access](#guide_20), [25 Nexora member portal journeys](#guide_25).

## Complete primary source files

These are the complete captured files, with numbered source lines. Other supporting files are included in the complete HTML manual. Configured local passwords, if any, are explicitly omitted. Do not paste the display line numbers into code.

### S0080 crm-workspace.tsx

[apps/web/src/features/crm/crm-workspace.tsx](<D:/Projects new/Nexora/apps/web/src/features/crm/crm-workspace.tsx>)

SHA256 `aeea3421ba16171ca13f3778d1ad68dc9eb640303291a9569f5a89993d4cc5a6`; 243 source lines; complete captured text.

```tsx
0001  "use client";
0002  import styles from "./record-detail.module.css";
0003  import listingStyles from "./record-listing.module.css";
0004  import { RecordSection } from "./record-section";
0005  import { RecordTimeline } from "./record-timeline";
0006  import { RecordDataAudit } from "./record-data-audit";
0007  import { RecordPhoto, RecordPhotoField } from "./record-photo";
0008  import { WorkspaceHeading } from "@/components/ui/workspace-heading";
0009  import { EntityHeading } from "@/components/ui/entity-heading";
0010  import { attributeValues } from "../shared/attribute-fields";
0011  import { RecordAttributeSections } from "../shared/record-attribute-sections";
0012  import { RecordProfiles } from "./record-profiles";
0013  import { RecordMemberships } from "./record-memberships";
0014  import { RecordProfile } from "./record-profile";
0015  import { CommunicationMethods } from "./communication-methods";
0016  import { RecordAddresses } from "./record-addresses";
0017  import { RecordTags } from "./record-tags";
0018  import { PanelToggle, useCollapsedPanel } from "../shared/panel-toggle";
0019  import { ConfiguredSections } from "../shared/configured-sections";
0020  
0021  import { useEffect, useId, useRef, useState, type FormEvent, type ReactNode } from "react";
0022  import { ArrowLeft, Building2, Plus, Eye, MoreHorizontal, Pencil, Trash2, UserRound } from "lucide-react";
0023  import { crm } from "./api";
0024  import { useCrmListing } from "./use-crm-listing";
0025  import { ConnectedRecord } from "./connected-record";
0026  import { RecordTransfer } from "./record-transfer";
0027  import { ColumnChooser } from "../shared/column-chooser";
0028  import { FilterViewToolbar } from "../shared/filter-view-toolbar";
0029  import { SortableHeader, parseTableSort } from "../shared/table-sort";
0030  import { SavedViewControls } from "./saved-view-controls";
0031  import type { Detail, Directory, Field, Item, ListState, RecordRow } from "./types";
0032  
0033  const input = "mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 focus:outline-none focus:ring-2 focus:ring-indigo-500";
0034  const button = "rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-800 hover:bg-slate-50 focus-visible:outline-2 focus-visible:outline-indigo-600 disabled:opacity-50";
0035  const primary = "rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600 disabled:opacity-50";
0036  function Label({ title, children }: { title: string; children: ReactNode }) { return <label className="block text-sm font-medium text-slate-700">{title}{children}</label>; }
0037  function data(event: FormEvent<HTMLFormElement>) { event.preventDefault(); return Object.fromEntries(new FormData(event.currentTarget).entries()) as Record<string, string>; }
0038  function Card({ title, children }: { title: string; children: ReactNode }) { return <section className="rounded-xl border border-slate-200 bg-white p-5"><h2 className="mb-4 text-lg font-semibold text-slate-900">{title}</h2>{children}</section>; }
0039  
0040  function ConfirmDialog({ message, onConfirm, onCancel }: { message: string; onConfirm: () => void; onCancel: () => void }) {
0041    const dialog = useRef<HTMLDialogElement>(null);
0042    const titleId = useId();
0043    const messageId = useId();
0044    useEffect(() => { dialog.current?.showModal(); }, []);
0045    return <dialog ref={dialog} aria-labelledby={titleId} aria-describedby={messageId} onCancel={event => { event.preventDefault(); onCancel(); }} className="m-auto w-[calc(100%-2rem)] max-w-md rounded-xl border border-slate-200 bg-white p-6 text-slate-800 shadow-xl backdrop:bg-slate-900/40">
0046      <h2 id={titleId} className="text-lg font-semibold">Confirm change</h2><p id={messageId} className="my-4 text-sm leading-6">{message}</p>
0047      <div className="flex justify-end gap-2"><button className={button} onClick={onCancel}>Cancel</button><button className={primary} onClick={onConfirm}>Confirm</button></div>
0048    </dialog>;
0049  }
0050  
0051  export function CrmWorkspace({ permissions, initial, initialRecord, fixedKind = false, currentUserId }: { currentUserId?: string; permissions: string[]; initial: ListState; initialRecord: string; fixedKind?: boolean }) {
0052    const [selectedReadOnly, setSelectedReadOnly] = useState(true);
0053    const [items, setItems] = useState<RecordRow[]>([]);
0054    const [checked, setChecked] = useState<{key:string; ids:string[]}>({key:"", ids:[]});
0055    const [total, setTotal] = useState(0);
0056    const [refresh, setRefresh] = useState(0);
0057    const listing = useCrmListing(initial, initialRecord, fixedKind, refresh);
0058    const { query, search, setSearch, selected, setSelected, directory, fields, columns, setColumns, filter } = listing;
0059    const requestKey = JSON.stringify([query, refresh, selected]);
0060    const [loadedRequest, setLoadedRequest] = useState("");
0061    const selectedIds = checked.key === requestKey ? checked.ids : [];
0062    const loading = !listing.ready || loadedRequest !== requestKey;
0063    const [error, setError] = useState("");
0064    const [busy, setBusy] = useState(false);
0065    const [creating, setCreating] = useState(false);
0066    const canManage = permissions.includes("crm.manage");
0067    useEffect(() => {
0068      let current = true;
0069      const params = new URLSearchParams(Object.entries(query).map(([k, v]) => [k, String(v)]));
0070      if (!listing.ready) return;
0071      crm<{ items: RecordRow[]; total: number }>(`/records?${params}`).then(result => { if (current) { setItems(result.items); setTotal(result.total); setError(""); setLoadedRequest(requestKey); } }).catch(e => { if (current) { setError(e.message); setLoadedRequest(requestKey); } });
0072      return () => { current = false; };
0073    }, [query, refresh, selected, listing.ready, requestKey]);
0074    async function perform(action: () => Promise<void>) { setBusy(true); setError(""); try { await action(); setRefresh(r => r + 1); } catch (e) { setError(e instanceof Error ? e.message : "Unable to save."); } finally { setBusy(false); } }
0075    const columnControls = <>{[{ id: "name", label: "Name" }, { id: "status", label: "Status" }, { id: "category", label: "Category" }, { id: "updatedAtUtc", label: "Updated" }, ...fields.filter(f => f.kind === query.kind).map(f => ({ id: `field:${f.id}`, label: `${f.name} (${f.dataType})` }))].map(column => <label key={column.id} className="flex items-center gap-3 rounded-lg border border-slate-200 p-3 text-sm"><input type="checkbox" disabled={column.id === "name"} checked={columns.includes(column.id)} onChange={() => setColumns(current => current.includes(column.id) ? current.filter(x => x !== column.id) : [...current, column.id])} />{column.label}</label>)}</>;
0076    if (creating) return <main className="page-enter mx-auto max-w-4xl space-y-6 p-5 sm:p-8"><header className="flex items-start justify-between gap-4"><div><EntityHeading className="mt-2">Create {query.kind}</EntityHeading><p className="mt-2 text-sm text-slate-600">Complete the available details for this {query.kind} before saving.</p></div><button className={button} onClick={() => setCreating(false)}>Back to {query.kind === "contact" ? "contacts" : "accounts"}</button></header><Card title={`${query.kind === "contact" ? "Contact" : "Account"} details`}><RecordForm kind={query.kind} directory={directory} busy={busy} onCancel={() => setCreating(false)} onSave={r => perform(async () => { const saved = await crm<RecordRow>("/records", "POST", r); setCreating(false); setSelected(saved.id); })} /></Card></main>;
0077    if (selected) return <main className={`${styles.page} page-enter mx-auto max-w-7xl space-y-5 p-5 sm:p-8`}><WorkspaceHeading>{query.kind === "contact" ? "Contacts" : "Accounts"}</WorkspaceHeading><button className={styles.back} aria-label="Back to listing" title="Back" onClick={() => setSelected("")}><ArrowLeft size={16} aria-hidden="true" /></button><RecordDetail key={`${selected}-${selectedReadOnly}`} id={selected} directory={directory} fields={fields} permissions={permissions} readOnly={selectedReadOnly} onChanged={() => setRefresh(r => r + 1)} /></main>;
0078    return <main className={`${listingStyles.listing} mx-auto max-w-7xl space-y-5 p-4 text-slate-800 sm:p-7`}>
0079      <WorkspaceHeading actions={<div className="flex items-center gap-2">{canManage && <button type="button" className={listingStyles.create} aria-label={`Create ${query.kind}`} title={`Create ${query.kind}`} onClick={() => { setCreating(true); setSelected(""); }}><Plus size={20} strokeWidth={1.8} aria-hidden="true" /></button>}</div>}>{query.kind === "contact" ? "Contacts" : "Accounts"}</WorkspaceHeading>
0080      {error && <div role="alert" className="flex flex-wrap items-center justify-between gap-2 rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800">{error}<button className={button} onClick={() => { setError(""); setRefresh(r => r + 1); }}>Retry loading</button></div>}
0081      <FilterViewToolbar onAllRecords={() => listing.scope()} onMyRecords={currentUserId ? () => listing.scope(currentUserId) : undefined}
0082        search={<form className="flex items-end gap-2" onSubmit={e => { e.preventDefault(); filter({ search }); }}><label className="min-w-0 flex-1"><span className="sr-only">Search records</span><input className="w-full rounded-lg border border-blue-300 bg-white px-3 py-2 text-sm text-slate-900 placeholder:text-blue-400 focus:outline-none focus:ring-2 focus:ring-blue-500" type="search" value={search} maxLength={160} onChange={e => setSearch(e.target.value)} placeholder="Search records…" /></label><button className="rounded-lg bg-blue-600 px-3 py-2 text-sm font-semibold text-white hover:bg-blue-700 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-600">Search</button></form>}
0083        actions={canManage ? <RecordTransfer kind={query.kind} changed={() => setRefresh(r => r + 1)} /> : undefined}
0084        filters={<>
0085        <div className="grid gap-3 sm:grid-cols-2">
0086          <Label title="Status"><select className={input} value={query.status} onChange={e => filter({ status: e.target.value })}><option value="">All statuses</option>{["active", "prospect", "inactive", "lapsed"].map(s => <option key={s}>{s}</option>)}</select></Label>
0087          <Label title="Owner user"><select className={input} value={query.ownerUserId ?? ""} onChange={e => filter({ ownerUserId: e.target.value })}><option value="">All users</option>{directory.users.map(u => <option key={u.id} value={u.id}>{u.name}</option>)}</select></Label>
0088          <Label title="Owner team"><select className={input} value={query.ownerTeamId ?? ""} onChange={e => filter({ ownerTeamId: e.target.value })}><option value="">All teams</option>{directory.teams.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}</select></Label>
0089          <Label title="Category"><input className={input} maxLength={100} value={query.category ?? ""} onChange={e => filter({ category: e.target.value })} /></Label>
0090          <Label title="Sort"><select className={input} value={query.sort} onChange={e => filter({ sort: e.target.value })}><option value="name">Name A–Z</option><option value="-name">Name Z–A</option><option value="updated">Recently updated</option></select></Label>
0091          <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={query.archived} onChange={e => filter({ archived: e.target.checked })} />Archived records</label>
0092        </div>
0093          <SavedViewControls listing={listing} />
0094        </>}
0095      />
0096      <section aria-label="CRM records" className="overflow-hidden rounded-xl border border-slate-200 bg-white" aria-busy={loading}>
0097        <div className="overflow-x-auto"><table className="w-full text-left text-sm"><caption className="sr-only">{query.kind === "contact" ? "Contacts" : "Accounts"} matching current filters</caption><thead className="bg-slate-50 text-slate-600"><tr><th scope="col" className="w-12 p-4"><span className="sr-only">Select</span><input type="checkbox" aria-label="Select all records" disabled={loading || !items.length} checked={items.length > 0 && items.every(row=>selectedIds.includes(row.id))} onChange={e=>setChecked({key:requestKey,ids:e.target.checked?items.map(row=>row.id):[]})} /></th><th scope="col" className="p-4">Actions</th>{columns.includes("name") && <SortableHeader className="p-4" label="Name" column="name" sort={parseTableSort(query.sort)} disabled={loading} onSort={sort=>filter({sort})}/>}{columns.includes("status") && <SortableHeader className="p-4" label="Status" column="status" sort={parseTableSort(query.sort)} disabled={loading} onSort={sort=>filter({sort})}/>}{columns.includes("category") && <SortableHeader className="p-4" label="Category" column="category" sort={parseTableSort(query.sort)} disabled={loading} onSort={sort=>filter({sort})}/>}{columns.includes("updatedAtUtc") && <SortableHeader className="p-4" label="Updated" column="updatedAtUtc" sort={parseTableSort(query.sort)} disabled={loading} onSort={sort=>filter({sort})}/>}{fields.filter(f => f.kind === query.kind && columns.includes(`field:${f.id}`)).map(f => <SortableHeader key={f.id} className="p-4" label={f.name} column={`field:${f.id}`} sort={parseTableSort(query.sort)} disabled={loading} onSort={sort=>filter({sort})}/>)}</tr></thead><tbody>{!loading && items.map(row => <tr key={row.id} className="border-t border-slate-100 hover:bg-indigo-50/30"><td className="p-4"><input type="checkbox" aria-label={`Select ${row.name}`} checked={selectedIds.includes(row.id)} onChange={e=>setChecked({key:requestKey,ids:e.target.checked?[...selectedIds,row.id]:selectedIds.filter(id=>id!==row.id)})} /></td><td className="p-4"><div className="flex justify-start gap-1"><button className="rounded-lg p-2 text-slate-500 hover:bg-indigo-50 hover:text-indigo-700" aria-label={`View ${row.name}`} title="View" onClick={() => { setSelected(row.id); setSelectedReadOnly(true); setCreating(false); }}><Eye className="size-4" /></button>{canManage && <button className="rounded-lg p-2 text-slate-500 hover:bg-indigo-50 hover:text-indigo-700" aria-label={`Edit ${row.name}`} title="Edit" onClick={() => { setSelected(row.id); setSelectedReadOnly(false); setCreating(false); }}><Pencil className="size-4" /></button>}{canManage && <button className="rounded-lg p-2 text-rose-500 hover:bg-rose-50 hover:text-rose-700" aria-label={`Delete ${row.name}`} title="Delete" onClick={() => void perform(async () => { await crm(`/records/${row.id}/archive`, "POST", { version: row.version }); })}><Trash2 className="size-4" /></button>}</div></td>{columns.includes("name") && <th scope="row" className="p-4"><button className={listingStyles.recordName} onClick={() => { setSelected(row.id); setSelectedReadOnly(true); setCreating(false); }}><span className="grid size-8 place-items-center rounded-full bg-slate-100 text-slate-500">{query.kind === "account" ? <Building2 className="size-4" aria-hidden="true" /> : <UserRound className="size-4" aria-hidden="true" />}</span><span>{row.name}</span></button></th>}{columns.includes("status") && <td className="p-4">{row.status}</td>}{columns.includes("category") && <td className="p-4">{row.category ?? "—"}</td>}{columns.includes("updatedAtUtc") && <td className="p-4">{new Date(row.updatedAtUtc).toLocaleString()}</td>}{fields.filter(f => f.kind === query.kind && columns.includes(`field:${f.id}`)).map(f => { const v = row.customFields?.find(x => x.definitionId === f.id); return <td key={f.id} className="p-4">{v?.textValue ?? v?.numberValue ?? (v?.dateValue ? new Date(v.dateValue).toLocaleDateString() : v?.booleanValue == null ? "—" : v.booleanValue ? "Yes" : "No")}</td>; })}</tr>)}</tbody></table></div>
0098        {loading ? <p className="p-8 text-center" role="status">Loading relationships…</p> : items.length === 0 && <p className="p-8 text-center text-slate-600">No records match these filters. Adjust your search or create the first {query.kind}.</p>}
0099        <footer className="flex flex-wrap gap-3 items-center justify-between border-t border-slate-200 p-4"><div className="flex items-center gap-3"><ColumnChooser>{columnControls}</ColumnChooser><p className="text-sm" aria-live="polite">{total} records · Page {query.page}</p></div><label className="text-sm">Rows per page <select aria-label="Rows per page" className="ml-2 rounded border p-1" value={query.pageSize ?? 20} onChange={e=>filter({pageSize:Number(e.target.value)})}>{[10,20,50,100].map(n=><option key={n}>{n}</option>)}</select></label><div className="flex gap-2"><button className={button} disabled={query.page <= 1 || loading} onClick={() => filter({ page: query.page - 1 })}>Previous</button><button className={button} disabled={query.page * (query.pageSize ?? 20) >= total || loading} onClick={() => filter({ page: query.page + 1 })}>Next</button></div></footer>
0100      </section>
0101      {selected && <RecordDetail key={selected} id={selected} directory={directory} fields={fields} permissions={permissions} readOnly={selectedReadOnly} onChanged={() => setRefresh(r => r + 1)} />}
0102    </main>;
0103  }
0104  
0105  function RecordForm({ row, kind, directory, busy, onSave, onCancel }: { row?: RecordRow; kind: string; directory: Directory; busy: boolean; onSave: (r: unknown) => Promise<void>; onCancel: () => void }) {
0106    return <form className="grid gap-4 sm:grid-cols-2" onSubmit={e => { const r = data(e); const attributes = attributeValues(r); void onSave({ ...r, attributes, kind, category: r.category || null, ownerUserId: r.ownerUserId || null, ownerTeamId: r.ownerTeamId || null, version: row?.version }); }}>
0107      <Label title={kind === "contact" ? "Label Name" : "Account Name"}><input className={input} name="name" defaultValue={row?.name} maxLength={160} required /></Label>
0108      <Label title="Status"><select className={input} name="status" defaultValue={row?.status ?? "active"}>{["active", "prospect", "inactive", "lapsed"].map(s => <option key={s}>{s}</option>)}</select></Label>
0109      <Label title="Category"><input className={input} name="category" defaultValue={row?.category ?? ""} maxLength={100} /></Label>
0110      <Label title="Owner"><select className={input} name="ownerUserId" defaultValue={row?.ownerUserId ?? ""}><option value="">Unassigned</option>{directory.users.map(u => <option key={u.id} value={u.id}>{u.name}</option>)}</select></Label>
0111      <Label title="Team"><select className={input} name="ownerTeamId" defaultValue={row?.ownerTeamId ?? ""}><option value="">Unassigned</option>{directory.teams.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}</select></Label>
0112  
0113      <div className="flex items-end gap-2"><button className={primary} disabled={busy}>Save {kind}</button><button type="button" className={button} onClick={onCancel}>Cancel</button></div>
0114    </form>;
0115  }
0116  
0117  type AccountContactList = { items: Array<{ association: { id: string; contactId: string; isMainAccount: boolean; department?: string | null; jobTitle?: string | null; workClassification: string; isMainContact: boolean; isAuthorisedContact: boolean; startDate: string; endDate?: string | null; endReason?: string | null; version?: string }; accountName: string; contactName: string; accountArchived: boolean; contactArchived: boolean; contactVersion: string; roles: string[] }>; totalCount: number; page: number; pageSize: number };
0118  
0119  function AccountContactsPanel({ recordId, refresh, editable, busy, recordKind, onLink, onEnd, onUpdate, onMain }: { recordId: string; refresh: number; editable: boolean; busy: boolean; recordKind: "account" | "contact"; onMain: (contactId: string, version: string, associationId: string | null) => void; onLink: (contactId: string, values: Record<string, string>) => void; onEnd: (id: string, version: string | undefined, values: Record<string, string>) => void; onUpdate: (id: string, version: string | undefined, values: { department: string | null; jobTitle: string | null; workClassification: string; isMainContact: boolean; isAuthorisedContact: boolean; roles: string[] }) => void }) {
0120    const [associationPage, setAssociationPage] = useState(1);
0121    const [includeEnded, setIncludeEnded] = useState(false);
0122    const [retry, setRetry] = useState(0);
0123    const [loaded, setLoaded] = useState<{ key: string; result?: AccountContactList; error?: string } | null>(null);
0124    const requestKey = `${recordId}/${associationPage}/${includeEnded}/${refresh}/${retry}`;
0125    const result = loaded?.key === requestKey ? loaded.result : undefined;
0126    const loadError = loaded?.key === requestKey ? loaded.error : undefined;
0127    useEffect(() => {
0128      let active = true;
0129      crm<AccountContactList>(`/records/${recordId}/account-contacts?page=${associationPage}&includeEnded=${includeEnded}`)
0130        .then(result => {
0131          if (!active) return;
0132          const lastPage = Math.max(1, Math.ceil(result.totalCount / result.pageSize));
0133          if (associationPage > lastPage) { setAssociationPage(lastPage); return; }
0134          setLoaded({ key: requestKey, result });
0135        }).catch(() => { if (active) setLoaded({ key: requestKey, error: "Associations could not be loaded." }); });
0136      return () => { active = false; };
0137    }, [recordId, associationPage, includeEnded, requestKey]);
0138    const [contactSearch, setContactSearch] = useState("");
0139    const [contacts, setContacts] = useState<RecordRow[]>([]);
0140    const [roles, setRoles] = useState<string[]>([]);
0141    const [ending, setEnding] = useState<{ id: string; version?: string } | null>(null);
0142    const [editing, setEditing] = useState<string | null>(null);
0143    useEffect(() => { crm<{ roles: string[] }>("/account-contact-options").then(x => setRoles(x.roles)).catch(() => undefined); }, []);
0144    const historyControl = <label className="mb-4 flex items-center gap-2 text-sm"><input type="checkbox" checked={includeEnded} disabled={busy} onChange={event => { setIncludeEnded(event.target.checked); setAssociationPage(1); setEditing(null); setEnding(null); }} />Include ended associations</label>;
0145    if (loadError) return <>{historyControl}<div role="alert"><p>{loadError}</p><button className={button} onClick={() => setRetry(x => x + 1)}>Retry associations</button></div></>;
0146    if (!result) return <>{historyControl}<p className="text-sm text-slate-500" role="status">Loading associations…</p></>;
0147    return <>
0148      {historyControl}
0149      {result.items.length === 0 && <p className="text-sm text-slate-500">No {includeEnded ? "" : "current "}{recordKind === "account" ? "contacts are linked to this account" : "employers are linked to this contact"}.</p>}
0150      <ul className="space-y-3">
0151        {result.items.map(item => <li key={item.association.id} className="min-w-0 rounded-xl border border-slate-200 bg-slate-50 p-4 text-sm break-words">
0152          <div className="flex flex-wrap items-center justify-between gap-2"><span className="flex min-w-0 items-center gap-2 font-semibold text-slate-900">{recordKind === "account" ? <UserRound aria-hidden="true" className="size-5 shrink-0 text-indigo-600" /> : <Building2 aria-hidden="true" className="size-5 shrink-0 text-indigo-600" />}{recordKind === "account" ? item.contactName : item.accountName}</span><span className="flex flex-wrap gap-1 text-xs">{item.association.isMainAccount && <span className="rounded-full bg-sky-100 px-2 py-1 text-sky-800">{item.association.endDate ? "Main account at end" : "Main account"}</span>}{item.association.isMainContact && <span className="rounded-full bg-indigo-100 px-2 py-1 text-indigo-800">Main contact</span>}{item.association.isAuthorisedContact && <span className="rounded-full bg-emerald-100 px-2 py-1 text-emerald-800">Authorised</span>}</span></div>
0153          <p className="mt-1 text-xs text-slate-600">{item.association.department && <span>Department: {item.association.department} · </span>}{item.association.jobTitle || "No job title"} · {item.association.workClassification.replace("-", " ")}</p>
0154          <p className="mt-2 text-xs text-slate-600">Started {item.association.startDate.slice(0, 10)} · {item.association.endDate ? `Ended ${item.association.endDate.slice(0, 10)}` : "Current association"}</p>
0155          {item.association.endDate && item.association.endReason && <p className="mt-1 whitespace-pre-wrap break-words text-xs text-slate-600">Reason: {item.association.endReason}</p>}
0156          {(item.accountArchived || item.contactArchived) && <p className="mt-1 text-xs text-amber-800">Linked record is archived.</p>}
0157          {editing === item.association.id && !item.association.endDate && !item.accountArchived && !item.contactArchived ? <form aria-label="Edit association details" className="mt-3 space-y-3 rounded-lg border border-indigo-200 bg-white p-4" onSubmit={e => { e.preventDefault(); const form = new FormData(e.currentTarget); onUpdate(item.association.id, item.association.version, { department: String(form.get("department") || "") || null, jobTitle: String(form.get("jobTitle") || "") || null, workClassification: String(form.get("workClassification")), isMainContact: form.get("isMainContact") === "on", isAuthorisedContact: form.get("isAuthorisedContact") === "on", roles: form.getAll("roles").map(String) }); setEditing(null); }}><p className="font-semibold text-slate-900">Edit association with {recordKind === "account" ? item.contactName : item.accountName}</p><Label title="Department"><input className={input} name="department" defaultValue={item.association.department ?? ""} maxLength={160} /></Label><Label title="Job title"><input className={input} name="jobTitle" defaultValue={item.association.jobTitle ?? ""} maxLength={160} /></Label><Label title="Work classification"><select className={input} name="workClassification" defaultValue={item.association.workClassification}><option>unknown</option><option>technical</option><option>non-technical</option></select></Label><div className="flex flex-wrap gap-3 text-xs"><label><input type="checkbox" name="isMainContact" defaultChecked={item.association.isMainContact} /> Main contact</label><label><input type="checkbox" name="isAuthorisedContact" defaultChecked={item.association.isAuthorisedContact} /> Authorised contact</label></div><Label title="Roles"><select multiple className={`${input} min-h-24`} name="roles" defaultValue={item.roles}>{roles.map(role => <option key={role} value={role}>{role}</option>)}</select></Label><div className="flex gap-2"><button className={primary} disabled={busy}>Save</button><button type="button" className={button} onClick={() => setEditing(null)}>Cancel</button></div></form> : <><p className="mt-2 text-xs text-slate-700">{item.roles.length ? item.roles.join(", ") : "No roles recorded"}</p>{editable && !item.association.endDate && <div className="mt-3 flex flex-wrap gap-2"><button className={button} disabled={busy || item.accountArchived || item.contactArchived} onClick={() => setEditing(item.association.id)}>Edit association</button><button className={button} disabled={busy || item.contactArchived || (!item.association.isMainAccount && item.accountArchived)} onClick={() => onMain(item.association.contactId, item.contactVersion, item.association.isMainAccount ? null : item.association.id)}>{item.association.isMainAccount ? "Clear main account" : "Set as main account"}</button><button className={button} disabled={busy} onClick={() => setEnding({ id: item.association.id, version: item.association.version })}>End association</button></div>}</>}
0158        </li>)}
0159      </ul>
0160      <p className="mt-3 text-xs text-slate-500">{result.totalCount} {includeEnded ? "current and ended" : "current"} {recordKind === "account" ? "contact" : "employer"}{result.totalCount === 1 ? "" : "s"}</p>
0161      <nav aria-label="Association pages" className="mt-3 flex flex-wrap items-center gap-3 text-sm">
0162        <button className={button} disabled={busy || result.page <= 1} onClick={() => { setAssociationPage(p => p - 1); setEditing(null); setEnding(null); }}>Previous associations</button>
0163        <span>Page {result.page} of {Math.max(1, Math.ceil(result.totalCount / result.pageSize))}</span>
0164        <button className={button} disabled={busy || result.page * result.pageSize >= result.totalCount} onClick={() => { setAssociationPage(p => p + 1); setEditing(null); setEnding(null); }}>Next associations</button>
0165      </nav>
0166      {ending && <form aria-label="End association details" className="mt-5 space-y-3 rounded-lg border border-amber-200 bg-amber-50 p-4" onSubmit={e => { const values = data(e); onEnd(ending.id, ending.version, values); setEnding(null); }}><p className="text-sm font-semibold text-slate-900">End association with {result.items.filter(item => item.association.id === ending.id).map(item => recordKind === "account" ? item.contactName : item.accountName).join("")}</p><Label title="End date"><input className={input} type="date" name="endDate" defaultValue={new Date().toISOString().slice(0, 10)} required /></Label><Label title="Reason"><input className={input} name="reason" maxLength={500} defaultValue="Association ended" required /></Label><div className="flex gap-2"><button className={primary} disabled={busy}>Confirm end</button><button type="button" className={button} onClick={() => setEnding(null)}>Cancel</button></div></form>}
0167      {editable && recordKind === "account" && <form className="mt-5 space-y-3 border-t border-slate-200 pt-4" onSubmit={e => { const values = data(e); onLink(values.contactId, values); }}>
0168        <p className="text-sm font-semibold text-slate-900">Link an existing contact</p>
0169        <div className="flex items-end gap-2"><Label title="Search contacts"><input className={input} value={contactSearch} onChange={e => setContactSearch(e.target.value)} placeholder="Name" /></Label><button type="button" className={button} disabled={busy || !contactSearch.trim()} onClick={() => crm<{ items: RecordRow[] }>(`/records?kind=contact&search=${encodeURIComponent(contactSearch)}&pageSize=25`).then(x => setContacts(x.items)).catch(() => setContacts([]))}>Find</button></div>
0170        <Label title="Contact"><select name="contactId" className={input} required><option value="">Choose a contact</option>{contacts.map(contact => <option key={contact.id} value={contact.id}>{contact.name}</option>)}</select></Label>
0171        <div className="grid gap-3 sm:grid-cols-2"><Label title="Start date"><input className={input} type="date" name="startDate" defaultValue={new Date().toISOString().slice(0, 10)} required /></Label><Label title="Job title"><input className={input} name="jobTitle" maxLength={160} /></Label><Label title="Department"><input className={input} name="department" maxLength={160} /></Label></div>
0172        <div className="grid gap-3 sm:grid-cols-2"><Label title="Work classification"><select className={input} name="workClassification" defaultValue="unknown"><option>unknown</option><option>technical</option><option>non-technical</option></select></Label><Label title="Role"><select className={input} name="role"><option value="">No role</option>{roles.map(role => <option key={role} value={role}>{role}</option>)}</select></Label></div>
0173        <button className={primary} disabled={busy}>Link contact</button>
0174      </form>}
0175    </>;
0176  }
0177  
0178  function RecordDetail({ id, directory, permissions, readOnly, onChanged }: { id: string; directory: Directory; fields: Field[]; permissions: string[]; readOnly: boolean; onChanged: () => void }) {
0179    const [sidePanelCollapsed, toggleSidePanel] = useCollapsedPanel("nexora.layout.record-side-panel-collapsed");
0180    const sidePanelId = useId();
0181    const [confirmation, setConfirmation] = useState<{ message: string; action: () => Promise<void> } | null>(null);
0182    const recordHeading = useRef<HTMLDivElement>(null);
0183    const focused = useRef(false);
0184    const [detail, setDetail] = useState<Detail | null>(null);
0185    const [refresh, setRefresh] = useState(0);
0186    const [busy, setBusy] = useState(false);
0187    const [error, setError] = useState("");
0188    const [editing, setEditing] = useState(false);
0189    const [actionsOpen, setActionsOpen] = useState(false);
0190    const [targets, setTargets] = useState<RecordRow[]>([]);
0191    const manage = permissions.includes("crm.manage");
0192    useEffect(() => {
0193      let current = true;
0194      crm<Detail>(`/records/${id}`).then(d => { if (current) setDetail(d); }).catch(e => { if (current) setError(e.message); });
0195      return () => { current = false; };
0196    }, [id, refresh]);
0197    useEffect(() => {
0198      if (detail && !focused.current) {
0199        focused.current = true;
0200        recordHeading.current?.focus();
0201      }
0202    }, [detail]);
0203    async function perform(action: () => Promise<void>) { setBusy(true); setError(""); try { await action(); setRefresh(r => r + 1); onChanged(); } catch (e) { setError(e instanceof Error ? e.message : "Unable to save."); } finally { setBusy(false); } }
0204    function add(collection: string, event: FormEvent<HTMLFormElement>) { const r = data(event); const form = event.currentTarget; void perform(async () => { await crm(`/records/${id}/${collection}`, "POST", r); form.reset(); }); }
0205    async function addTag(name: string) {
0206      setBusy(true);
0207      setError("");
0208      try {
0209        const saved = await crm<{ id: string }>(`/records/${id}/tags`, "POST", { name });
0210        setDetail(current => current ? { ...current, tags: [...current.tags, { id: saved.id, name: name.toLowerCase() }] } : current);
0211        setRefresh(value => value + 1);
0212        onChanged();
0213      } finally { setBusy(false); }
0214    }
0215    const editable = manage && !readOnly && detail && !detail.record.isArchived;
0216    const contactName = [detail?.contactDetails?.firstName, detail?.contactDetails?.middleName, detail?.contactDetails?.lastName].map(part => part?.trim()).filter(Boolean).join(" ");
0217    const displayName = detail?.record.kind === "contact" ? contactName || detail.record.name || "Unnamed contact" : detail?.record.name ?? "";
0218    function section(title: string, collection: keyof Detail, removable = false) {
0219      const rows = detail?.[collection] as Item[];
0220      return <ul className="mb-4 space-y-3">{rows.length === 0 && <li className="text-sm text-slate-500">No {title.toLowerCase()} yet.</li>}{rows.map(row => <li key={row.id} className="flex items-start justify-between gap-2 rounded-lg bg-slate-50 p-3 text-sm"><div className="whitespace-pre-wrap break-words">{row.name || row.body || row.label || row.value}{row.line1 && <p>{[row.line1, row.city, row.postalCode, row.country].filter(Boolean).join(", ")}</p>}{row.consent && <p className="mt-1 text-xs text-slate-600">{row.kind} · Consent: {row.consent}{row.consentEvidence ? ` · ${row.consentEvidence}` : ""}</p>}{row.targetRecordId && <p className="text-xs text-slate-600">{row.label}</p>}</div>{removable && editable && <button className={button} disabled={busy} aria-label={`Remove ${row.name || row.label || row.value}`} onClick={() => setConfirmation({ message: `Remove this item from ${detail?.record.name}?`, action: async () => { await crm(`/records/${id}/${collection}/${row.id}`, "DELETE"); } })}>Remove</button>}</li>)}</ul>;
0221    }
0222    return <section className="space-y-4" aria-label="Selected record">
0223      {confirmation && <ConfirmDialog message={confirmation.message} onCancel={() => setConfirmation(null)} onConfirm={() => { const action = confirmation.action; setConfirmation(null); void perform(action); }} />}
0224      {error && <div role="alert" className="rounded-lg bg-red-50 p-4 text-sm text-red-800">{error} <button className={button} onClick={() => { setError(""); setRefresh(r => r + 1); }}>Reload record</button></div>}
0225      {!detail ? <p role="status">Loading record…</p> : <RecordAttributeSections record={detail.record} values={detail.fields} editable={Boolean(editable)} busy={busy} onSave={attributes => void perform(async () => { await crm(`/records/${id}`, "PATCH", { ...detail.record, attributes }); })}>
0226        {editing && editable && <div className={styles.recordEdit}><RecordForm key={detail.record.version} row={detail.record} kind={detail.record.kind} directory={directory} busy={busy} onCancel={() => setEditing(false)} onSave={r => perform(async () => { await crm(`/records/${id}`, "PATCH", r); setEditing(false); })} /></div>}
0227        <div className={styles.panelTools}><PanelToggle label="record side panel" collapsed={sidePanelCollapsed} controls={sidePanelId} onToggle={toggleSidePanel}>Side panel</PanelToggle>{sidePanelCollapsed && <h2>{displayName}</h2>}</div><div className={styles.layout} data-side-panel-collapsed={sidePanelCollapsed}><div id={sidePanelId} hidden={sidePanelCollapsed} className={styles.rail} role="complementary" aria-label="Record tags"><div ref={recordHeading} tabIndex={-1} aria-label={displayName} className={`${styles.identity} scroll-mt-24 outline-none`}><h2>{displayName}</h2><div className="mb-4 flex flex-wrap items-center gap-2"><div className="relative ml-auto"><button className={button} aria-label="Record actions" aria-expanded={actionsOpen} onClick={() => setActionsOpen(open => !open)}><MoreHorizontal className="size-4" /></button>{actionsOpen && <div className="absolute right-0 z-20 mt-2 w-44 rounded-lg border border-slate-200 bg-white p-1 shadow-lg">{editable && <button className="block w-full rounded-md px-3 py-2 text-left text-sm hover:bg-slate-50" onClick={() => { setEditing(true); setActionsOpen(false); }}>Edit details</button>}{manage && <button className="block w-full rounded-md px-3 py-2 text-left text-sm hover:bg-slate-50" disabled={busy} onClick={() => { setActionsOpen(false); setConfirmation({ message: detail.record.isArchived ? "Restore this record to current lists?" : "Archive this record? It will leave current lists and remain available for restoration.", action: async () => { await crm(`/records/${id}/${detail.record.isArchived ? "restore" : "archive"}`, "POST", { version: detail.record.version }); } }); }}>{detail.record.isArchived ? "Restore" : "Archive"}</button>}</div>}</div></div></div><div className={styles.statusBanner} data-status={detail.record.isArchived?"archived":detail.record.status} aria-hidden="true" /><RecordPhoto record={detail.record} name={displayName} canRead={permissions.includes("crm.files")} /><div className={styles.recordStatus} aria-label="Record status" data-status={detail.record.isArchived ? "archived" : detail.record.status}>{detail.record.isArchived ? "Archived" : detail.record.status}</div>{detail.record.kind === "account" && permissions.includes("crm.files") && <RecordPhotoField record={detail.record} disabled={!editable || busy} onChanged={()=>{setRefresh(n=>n+1);onChanged();}}/>}<RecordTags tags={detail.tags} editable={Boolean(editable)} busy={busy} onAdd={addTag} onRemove={tag => setConfirmation({ message: `Remove "${tag.name}" from ${displayName}?`, action: async () => { await crm(`/records/${id}/tags/${tag.id}`, "DELETE"); } })} /></div><ConfiguredSections module="crm" kind={detail.record.kind} className={styles.content}>
0228        <RecordProfile data-section={detail.record.kind === "contact" ? "Contact details" : "Organisation details"} key={`details-${id}-${detail.record.version}`} kind={detail.record.kind} photoRecord={detail.record} canReadPhoto={permissions.includes("crm.files")} onPhotoChanged={()=>{setRefresh(n=>n+1);onChanged();}} contactDetails={detail.contactDetails} generalInformation={detail.generalInformation} communicationPermissions={detail.communicationPermissions} socialMediaDetails={detail.socialMediaDetails} accountDetails={detail.accountDetails} accountGeneralInformation={detail.accountGeneralInformation} editable={Boolean(editable)} busy={busy} onSave={values => void perform(async () => { await crm(`/records/${id}/profile`, "PUT", { version: detail.record.version, ...values }); })} />
0229        <RecordProfiles data-section="Profile" key={`profiles-${id}-${detail.record.version}`} id={id} version={detail.record.version} assignments={detail.profiles} editable={Boolean(editable)} busy={busy} onChanged={()=>{setRefresh(n=>n+1);onChanged();}} />
0230        {permissions.includes("membership.read") && <RecordMemberships data-section="Membership details" key={`${id}-${refresh}`} id={id} kind={detail.record.kind} />}
0231          {(detail.record.kind === "account" || detail.record.kind === "contact") && <RecordSection title={detail.record.kind === "account" ? "Contacts" : "Employers"}><p className="mb-4 text-sm text-slate-600">Structured associations · Employment, roles and account responsibilities, with current and ended history.</p><AccountContactsPanel onMain={(contactId, version, associationId) => void perform(async () => { await crm(`/contacts/${contactId}/main-account`, "PUT", { version, associationId }); })} recordKind={detail.record.kind} recordId={id} refresh={refresh} editable={Boolean(editable)} busy={busy} onLink={(contactId, values) => void perform(async () => { await crm(`/accounts/${id}/contacts`, "POST", { contactId, startDate: `${values.startDate}T00:00:00Z`, department: values.department || null, jobTitle: values.jobTitle || null, workClassification: values.workClassification, isMainContact: false, isAuthorisedContact: false, roles: values.role ? [values.role] : [] }); })} onEnd={(associationId, version, values) => void perform(async () => { await crm(`/account-contacts/${associationId}/end`, "POST", { version, endDate: `${values.endDate}T00:00:00Z`, reason: values.reason }); })} onUpdate={(associationId, version, values) => void perform(async () => { await crm(`/account-contacts/${associationId}`, "PATCH", { version, ...values }); })} /></RecordSection>}
0232          <RecordSection data-section="Addresses" title="Addresses" onLoad={async () => { setDetail(await crm<Detail>(`/records/${id}`)); }}><RecordAddresses key={`${id}-${detail.record.version}`} items={detail.addresses} billingUsesMailingAddress={Boolean(detail.record.billingUsesMailingAddress)} effectiveBillingAddress={detail.effectiveBillingAddress} editable={Boolean(editable)} busy={busy} onSave={(values, addressId) => void perform(async () => { await crm(`/records/${id}/addresses${addressId ? `/${addressId}` : ""}`, addressId ? "PUT" : "POST", { ...values, version: detail.record.version }); })} onBillingSource={useMailingAddress => void perform(async () => { await crm(`/records/${id}/billing-address`, "PUT", { version: detail.record.version, useMailingAddress }); })} onRemove={item => setConfirmation({ message: `Remove ${item.label} from ${detail.record.name}?`, action: async () => { await crm(`/records/${id}/addresses/${item.id}`, "DELETE"); } })} /></RecordSection>
0233          <RecordSection data-section="Communication methods" title="Communication Methods" onLoad={async () => { setDetail(await crm<Detail>(`/records/${id}`)); }}><CommunicationMethods key={`${id}-${detail.record.version}`} items={detail.communications} editable={Boolean(editable)} busy={busy} onSave={(values, communicationId) => void perform(async () => { await crm(`/records/${id}/communications${communicationId ? `/${communicationId}` : ""}`, communicationId ? "PUT" : "POST", { ...values, version: detail.record.version }); })} onRemove={item => setConfirmation({ message: `Remove ${item.value} from ${detail.record.name}?`, action: async () => { await crm(`/records/${id}/communications/${item.id}`, "DELETE"); } })} /></RecordSection>
0234          <RecordSection data-section="Related records" title="Related Records" onLoad={async () => { setDetail(await crm<Detail>(`/records/${id}`)); }}><p className="mb-4 text-sm text-slate-600">Other connections · General links between records, separate from employment and account responsibilities.</p>{section("Relationships", "relationships", true)}{editable && <><form className="mb-3 flex items-end gap-2" onSubmit={e => { const r = data(e); void perform(async () => { const result = await crm<{ items: RecordRow[] }>(`/records?kind=${r.kind}&search=${encodeURIComponent(r.search)}&pageSize=100`); setTargets(result.items.filter(x => x.id !== id)); }); }}><Label title="Find related"><input name="search" className={input} maxLength={160} required /></Label><Label title="Type"><select name="kind" className={input}><option value="account">Accounts</option><option value="contact">Contacts</option></select></Label><button className={button} disabled={busy}>Find</button></form><form className="space-y-3" onSubmit={e => add("relationships", e)}><Label title="Related record"><select className={input} name="targetRecordId" required><option value="">Choose a search result</option>{targets.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}</select></Label><Label title="Relationship (for example, works at)"><input className={input} name="label" maxLength={100} required /></Label><button className={button} disabled={busy}>Link record</button></form></>}</RecordSection>
0235          <RecordSection data-section="Notes" title="Notes" onLoad={async () => { setDetail(await crm<Detail>(`/records/${id}`)); }}>{section("Notes", "notes")}{editable && <form className="space-y-3" onSubmit={e => add("notes", e)}><Label title="New note"><textarea name="body" className={input} rows={4} maxLength={4000} required /></Label><button className={button} disabled={busy}>Add note</button></form>}</RecordSection>
0236          <RecordSection data-section="Files" title="Files" onLoad={async () => { setDetail(await crm<Detail>(`/records/${id}`)); }}><ul className="mb-4 space-y-2">{detail.files.length === 0 && <li className="text-sm text-slate-500">No files attached.</li>}{detail.files.map(f => <li key={f.id}>{permissions.includes("crm.files") && f.scanStatus === "clean" ? <a className="text-sm font-medium text-indigo-700 underline" href={`/api/v1/crm/records/${id}/files/${f.id}`}>{f.name}</a> : <span className="text-sm">{f.name} · {permissions.includes("crm.files") ? f.scanStatus : "File access required"}</span>}{editable && permissions.includes("crm.files") && <span className="ml-3 inline-flex gap-2">{f.scanStatus === "quarantined" && <button className={button} disabled={busy} onClick={() => void perform(async () => { await crm(`/records/${id}/files/${f.id}/scan`, "POST", { version: f.version }); })}>Retry scan</button>}<button className={button} disabled={busy} onClick={() => setConfirmation({ message: `Delete ${f.name}? This removes the stored file and retains its activity history.`, action: async () => { await crm(`/records/${id}/files/${f.id}`, "DELETE", { version: f.version }); } })}>Delete file</button></span>}</li>)}</ul>{editable && permissions.includes("crm.files") && <form className="space-y-3" onSubmit={e => { e.preventDefault(); const form = e.currentTarget; const body = new FormData(form); body.set("version", detail.record.version); void perform(async () => { await crm(`/records/${id}/files`, "POST", body); form.reset(); }); }}><Label title="Attach file (up to 5 MB)"><input className={input} type="file" name="file" accept=".pdf,.txt,.png,.jpg,.jpeg" required /></Label><p className="text-xs text-slate-600">PDF, text, PNG or JPEG. Files require a clean scan and download as attachments.</p><button className={button} disabled={busy}>Upload file</button></form>}</RecordSection>
0237          <RecordTimeline data-section="Activity timeline" id={id} refresh={refresh} directory={directory} />
0238  
0239        <ConnectedRecord data-section="Connected Activity" id={id} refresh={refresh} />
0240        </ConfiguredSections><div hidden={sidePanelCollapsed} className={styles.auditFooter}><RecordDataAudit id={id} refresh={refresh} createdAtUtc={detail.record.createdAtUtc}/></div></div>
0241      </RecordAttributeSections>}
0242    </section>;
0243  }
```

### S0239 AccountContactEndpoints.cs

[backend/src/Modules/Nexora.Modules.Crm/AccountContactEndpoints.cs](<D:/Projects new/Nexora/backend/src/Modules/Nexora.Modules.Crm/AccountContactEndpoints.cs>)

SHA256 `a492277fbac524092498f19ef511af3d7bd7bdca928170a0b39b96bb7b7b6c23`; 196 source lines; complete captured text.

```cs
0001  using System.ComponentModel.DataAnnotations;
0002  using System.Text.Json;
0003  using Microsoft.AspNetCore.Builder;
0004  using Microsoft.AspNetCore.Http;
0005  using Microsoft.AspNetCore.Routing;
0006  using Microsoft.EntityFrameworkCore;
0007  using Nexora.BuildingBlocks.Security;
0008  using Nexora.Modules.Crm.Domain;
0009  using Nexora.Modules.Crm.Infrastructure;
0010  
0011  namespace Nexora.Modules.Crm;
0012  
0013  internal static class AccountContactEndpoints
0014  {
0015      public static void MapAccountContacts(this RouteGroupBuilder api)
0016      {
0017          api.MapGet("/account-contact-options", () => new {
0018              roles = AccountContactChoices.Roles, workClassifications = AccountContactChoices.WorkClassifications
0019          });
0020          api.MapGet("/records/{id:guid}/account-contacts", ListAsync);
0021          api.MapPut("/contacts/{contactId:guid}/main-account", SetMainAccountAsync).RequireAuthorization("crm.manage");
0022          api.MapPost("/accounts/{accountId:guid}/contacts", CreateAsync).RequireAuthorization("crm.manage");
0023          api.MapPatch("/account-contacts/{id:guid}", UpdateAsync).RequireAuthorization("crm.manage");
0024          api.MapPost("/account-contacts/{id:guid}/end", EndAsync).RequireAuthorization("crm.manage");
0025      }
0026  
0027      private static async Task<IResult> ListAsync(Guid id, string? page, string? includeEnded, CrmDbContext db, CancellationToken ct)
0028      {
0029          if (!await db.Records.AnyAsync(x => x.Id == id, ct)) throw new KeyNotFoundException();
0030          var number = 1;
0031          Check(page == null || int.TryParse(page, out number), "Page must be a whole number.");
0032          Check(number is >= 1 and <= 1000000, "Page must be between 1 and 1000000.");
0033          var history = false;
0034          Check(includeEnded == null || bool.TryParse(includeEnded, out history), "Include ended must be true or false.");
0035          const int size = 25;
0036          var query = db.Set<AccountContact>().AsNoTracking().Where(x => x.AccountId == id || x.ContactId == id);
0037          if (!history) query = query.Where(x => x.EndDate == null);
0038          var totalCount = await query.CountAsync(ct);
0039          var items = await (from link in query
0040              join account in db.Records on link.AccountId equals account.Id
0041              join contact in db.Records on link.ContactId equals contact.Id
0042              orderby account.Name, contact.Name, link.Id
0043              select new { association = link, accountName = account.Name, contactName = contact.Name,
0044                  accountArchived = account.IsArchived, contactArchived = contact.IsArchived, contactVersion = contact.Version })
0045              .Skip((number - 1) * size).Take(size).ToListAsync(ct);
0046          var ids = items.Select(x => x.association.Id).ToArray();
0047          var roles = await db.Set<AccountContactRole>().AsNoTracking().Where(x => ids.Contains(x.AssociationId))
0048              .OrderBy(x => x.RoleCode).ToListAsync(ct);
0049          return Results.Ok(new { items = items.Select(x => new {
0050              x.association, x.accountName, x.contactName, x.accountArchived, x.contactArchived, x.contactVersion,
0051              roles = roles.Where(r => r.AssociationId == x.association.Id).Select(r => r.RoleCode)
0052          }), totalCount, page = number, pageSize = size });
0053      }
0054  
0055      private static async Task<IResult> SetMainAccountAsync(Guid contactId, MainAccountRequest r,
0056          CrmDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct)
0057      {
0058          await using var transaction = await db.Database.BeginTransactionAsync(ct);
0059          var contact = await Parent(db, contactId, "contact", true, ct);
0060          if (contact.Version != r.Version) return Conflict();
0061          var current = await db.Set<AccountContact>().SingleOrDefaultAsync(x => x.ContactId == contactId && x.EndDate == null && x.IsMainAccount, ct);
0062          AccountContact? target = null;
0063          CrmRecord? account = null;
0064          if (r.AssociationId.HasValue)
0065          {
0066              target = await db.Set<AccountContact>().SingleOrDefaultAsync(x => x.Id == r.AssociationId && x.ContactId == contactId && x.EndDate == null, ct)
0067                  ?? throw new KeyNotFoundException();
0068              account = await Parent(db, target.AccountId, "account", true, ct);
0069          }
0070          if (current?.Id == target?.Id) return Results.Ok(new { contact.Version });
0071          if (current != null)
0072          {
0073              current.IsMainAccount = false; current.Version = Guid.NewGuid(); current.UpdatedAtUtc = DateTime.UtcNow;
0074              var previousAccount = await Parent(db, current.AccountId, "account", false, ct);
0075              Audit(db, who, http, previousAccount, contact, current.Id, "main-account-cleared");
0076              // Release the filtered unique slot before selecting another link, within one transaction.
0077              await db.SaveChangesAsync(ct);
0078          }
0079          if (target != null)
0080          {
0081              target.IsMainAccount = true; target.Version = Guid.NewGuid(); target.UpdatedAtUtc = DateTime.UtcNow;
0082              Audit(db, who, http, account!, contact, target.Id, "main-account-selected");
0083              await db.SaveChangesAsync(ct);
0084          }
0085          await transaction.CommitAsync(ct);
0086          return Results.Ok(new { contact.Version });
0087      }
0088  
0089      private static async Task<IResult> CreateAsync(Guid accountId, CreateAccountContactRequest r,
0090          CrmDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct)
0091      {
0092          var account = await Parent(db, accountId, "account", true, ct);
0093          var contact = await Parent(db, r.ContactId, "contact", true, ct);
0094          Validate(r.JobTitle, r.WorkClassification, r.Roles);
0095          Check(r.StartDate != default && r.StartDate.TimeOfDay == TimeSpan.Zero && r.StartDate.Date <= DateTime.UtcNow.Date,
0096              "Choose a start date no later than today.");
0097          if (await db.Set<AccountContact>().AnyAsync(x => x.AccountId == accountId && x.ContactId == r.ContactId && x.EndDate == null, ct))
0098              return Results.Problem(statusCode: 409, title: "This Contact already has a current link to this Account.");
0099          var link = new AccountContact { TenantId = who.TenantId!.Value, AccountId = accountId, ContactId = r.ContactId,
0100              StartDate = DateTime.SpecifyKind(r.StartDate.Date, DateTimeKind.Utc), Department = Department(r.Department) };
0101          Apply(link, r.JobTitle, r.WorkClassification, r.IsMainContact, r.IsAuthorisedContact);
0102          db.Add(link);
0103          AddRoles(db, link, r.Roles!);
0104          Audit(db, who, http, account, contact, link.Id, "created");
0105          await db.SaveChangesAsync(ct);
0106          return Results.Ok(link);
0107      }
0108  
0109      private static async Task<IResult> UpdateAsync(Guid id, UpdateAccountContactRequest r,
0110          CrmDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct)
0111      {
0112          var link = await db.Set<AccountContact>().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
0113          if (link.Version != r.Version) return Conflict();
0114          Check(link.EndDate == null, "Ended relationships are retained as history. Create a new link to rejoin.");
0115          var account = await Parent(db, link.AccountId, "account", true, ct);
0116          var contact = await Parent(db, link.ContactId, "contact", true, ct);
0117          Validate(r.JobTitle, r.WorkClassification, r.Roles);
0118          Apply(link, r.JobTitle, r.WorkClassification, r.IsMainContact, r.IsAuthorisedContact);
0119          if (r.Department.ValueKind != JsonValueKind.Undefined)
0120          {
0121              Check(r.Department.ValueKind is JsonValueKind.String or JsonValueKind.Null, "Department must be text or null.");
0122              link.Department = Department(r.Department.GetString());
0123          }
0124          var previous = await db.Set<AccountContactRole>().Where(x => x.AssociationId == id).ToListAsync(ct);
0125          db.RemoveRange(previous.Where(x => !r.Roles!.Contains(x.RoleCode)));
0126          AddRoles(db, link, r.Roles!.Except(previous.Select(x => x.RoleCode)));
0127          Audit(db, who, http, account, contact, link.Id, "updated");
0128          await db.SaveChangesAsync(ct);
0129          return Results.Ok(link);
0130      }
0131  
0132      private static async Task<IResult> EndAsync(Guid id, EndAccountContactRequest r,
0133          CrmDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct)
0134      {
0135          var link = await db.Set<AccountContact>().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
0136          if (link.Version != r.Version) return Conflict();
0137          Check(link.EndDate == null, "This relationship has already ended.");
0138          Check(r.EndDate.TimeOfDay == TimeSpan.Zero && r.EndDate.Date >= link.StartDate.Date && r.EndDate.Date <= DateTime.UtcNow.Date,
0139              "End date must be between the start date and today.");
0140          Check(!string.IsNullOrWhiteSpace(r.Reason) && r.Reason.Length <= 500, "Enter an end reason of 1–500 characters.");
0141          var account = await Parent(db, link.AccountId, "account", false, ct);
0142          var contact = await Parent(db, link.ContactId, "contact", false, ct);
0143          link.EndDate = DateTime.SpecifyKind(r.EndDate.Date, DateTimeKind.Utc);
0144          link.EndReason = r.Reason.Trim(); link.Version = Guid.NewGuid(); link.UpdatedAtUtc = DateTime.UtcNow;
0145          Audit(db, who, http, account, contact, link.Id, "ended");
0146          await db.SaveChangesAsync(ct);
0147          return Results.Ok(link);
0148      }
0149  
0150      private static async Task<CrmRecord> Parent(CrmDbContext db, Guid id, string kind, bool requireUnarchived, CancellationToken ct)
0151      {
0152          var record = await db.Records.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
0153          Check(record.Kind == kind, $"Select a CRM {kind}.");
0154          Check(!requireUnarchived || !record.IsArchived, "Restore the archived record before changing its relationships.");
0155          return record;
0156      }
0157      private static string? Department(string? value)
0158      {
0159          Check(value == null || (value.Length <= 160 && !value.Any(char.IsControl)), "Department must be at most 160 characters without control characters.");
0160          return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
0161      }
0162      private static void Validate(string? title, string classification, string[]? roles)
0163      {
0164          Check(title == null || title.Length <= 160, "Job title must not exceed 160 characters.");
0165          Check(AccountContactChoices.WorkClassifications.Contains(classification), "Choose a valid work classification.");
0166          Check(roles != null && roles.Length <= AccountContactChoices.Roles.Count && roles.Distinct().Count() == roles.Length &&
0167              roles.All(AccountContactChoices.Roles.Contains), "Choose distinct supported contact roles.");
0168      }
0169      private static void Apply(AccountContact link, string? title, string classification, bool main, bool authorised)
0170      {
0171          link.JobTitle = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
0172          link.WorkClassification = classification; link.IsMainContact = main; link.IsAuthorisedContact = authorised;
0173          link.Version = Guid.NewGuid(); link.UpdatedAtUtc = DateTime.UtcNow;
0174      }
0175      private static void AddRoles(CrmDbContext db, AccountContact link, IEnumerable<string> roles) =>
0176          db.AddRange(roles.Select(role => new AccountContactRole { TenantId = link.TenantId, AssociationId = link.Id, RoleCode = role }));
0177      private static void Audit(CrmDbContext db, IRequestIdentity who, HttpContext http, CrmRecord account, CrmRecord contact, Guid id, string action)
0178      {
0179          foreach (var record in new[] { account, contact })
0180          {
0181              // Both parent tokens participate in the transaction to detect concurrent archive/edit.
0182              record.Version = Guid.NewGuid(); record.UpdatedAtUtc = DateTime.UtcNow;
0183              db.Add(new Activity { TenantId = who.TenantId!.Value, RecordId = record.Id, ActorUserId = who.UserId!.Value,
0184                  Action = $"account-contact.{action}:{id}", CorrelationId = http.TraceIdentifier });
0185          }
0186      }
0187      private static void Check(bool valid, string message) { if (!valid) throw new ValidationException(message); }
0188      private static IResult Conflict() => Results.Problem(statusCode: 409, title: "This relationship changed. Reload it before saving.");
0189  }
0190  
0191  public sealed record CreateAccountContactRequest(Guid ContactId, DateTime StartDate, string? JobTitle,
0192      string WorkClassification, bool IsMainContact, bool IsAuthorisedContact, string[]? Roles, string? Department = null);
0193  public sealed record UpdateAccountContactRequest(Guid Version, string? JobTitle,
0194      string WorkClassification, bool IsMainContact, bool IsAuthorisedContact, string[]? Roles, JsonElement Department = default);
0195  public sealed record MainAccountRequest(Guid Version, Guid? AssociationId);
0196  public sealed record EndAccountContactRequest(Guid Version, DateTime EndDate, string Reason);
```

### S0253 AccountContact.cs

[backend/src/Modules/Nexora.Modules.Crm/Domain/AccountContact.cs](<D:/Projects new/Nexora/backend/src/Modules/Nexora.Modules.Crm/Domain/AccountContact.cs>)

SHA256 `cab29e76e0d1be0826f34345cc8569c3cf774bdcb262fa71cff836b276bbb990`; 37 source lines; complete captured text.

```cs
0001  namespace Nexora.Modules.Crm.Domain;
0002  
0003  // A person's association with an organisation; it is not a login or membership.
0004  public sealed class AccountContact : TenantRow
0005  {
0006      public Guid AccountId { get; set; }
0007      public Guid ContactId { get; set; }
0008      public string? Department { get; set; }
0009      public string? JobTitle { get; set; }
0010      public string WorkClassification { get; set; } = "unknown";
0011      public bool IsMainAccount { get; set; }
0012      public bool IsMainContact { get; set; }
0013      public bool IsAuthorisedContact { get; set; }
0014      public DateTime StartDate { get; set; }
0015      public DateTime? EndDate { get; set; }
0016      public string? EndReason { get; set; }
0017      public Guid Version { get; set; } = Guid.NewGuid();
0018      public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
0019      public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
0020  }
0021  
0022  public sealed class AccountContactRole : TenantRow
0023  {
0024      public Guid AssociationId { get; set; }
0025      public string RoleCode { get; set; } = "";
0026  }
0027  
0028  public static class AccountContactChoices
0029  {
0030      public static IReadOnlyList<string> Roles { get; } = Array.AsReadOnly(new[] {
0031          "assessment", "consultancy", "fumigation", "manufacturers-distributors", "pr",
0032          "servicing", "training", "events", "finance", "health-safety", "legal",
0033          "membership", "sales-marketing", "specifier", "technical"
0034      });
0035      public static IReadOnlyList<string> WorkClassifications { get; } =
0036          Array.AsReadOnly(new[] { "unknown", "technical", "non-technical" });
0037  }
```
