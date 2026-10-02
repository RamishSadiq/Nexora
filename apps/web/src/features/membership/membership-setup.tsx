"use client";
import { EntityHeading } from "@/components/ui/entity-heading";
import { useEffect, useState } from "react";
import { moduleApi } from "../shared/module-api";
import { useEntityListing } from "../shared/entity-listing-controls";
import { Field, Panel, inputClass, primaryClass, buttonClass } from "../shared/module-ui";
type Value = {id: string; name: string; description: string; version: string};
export function MembershipSetup({kind, label, canEdit}: {kind: string; label: string; canEdit: boolean}) {
 const [rows, setRows] = useState<Value[]>([]);
 const [editing, setEditing] = useState<Value | null>(null);
 const [error, setError] = useState(""); const [busy, setBusy] = useState(false);
 const [loading, setLoading] = useState(true); const [refresh, setRefresh] = useState(0);
 const listing = useEntityListing({records: rows, columns: ["Name", "Description"]});
 useEffect(() => {let active = true; moduleApi<Value[]>("membership", `/setup/${kind}`).then(result => {if(active) {setRows(result); setError("");}}).catch(e => {if(active) setError(e.message);}).finally(() => {if(active) setLoading(false);}); return () => {active = false;};}, [kind, refresh]);
 return <main className="mx-auto max-w-7xl space-y-5 p-4 sm:p-7"><header><p className="text-sm text-blue-600">Membership / Setup</p><EntityHeading className="mt-2">{label}</EntityHeading></header>
 {error && <div role="alert" className="rounded-lg bg-red-50 p-4 text-red-800">{error} <button className={buttonClass} onClick={() => setRefresh(v => v+1)}>Reload</button></div>}
 <Panel title="Lookup values">{listing.toolbar}{loading ? <p role="status">Loading lookup values…</p> : <div className="overflow-x-auto"><table className="w-full text-left text-sm"><thead><tr><th className="p-3">Name</th><th className="p-3" hidden={!listing.show("Description")}>Description</th>{canEdit && <th className="p-3">Actions</th>}</tr></thead><tbody>{rows.filter(listing.matches).map(row => <tr key={row.id} className="border-t border-slate-200"><td className="p-3">{row.name}</td><td className="p-3 whitespace-pre-wrap" hidden={!listing.show("Description")}>{row.description}</td>{canEdit && <td className="p-3"><button className={buttonClass} onClick={() => setEditing(row)} aria-label={`Edit ${row.name}`}>Edit</button></td>}</tr>)}</tbody></table>{!rows.filter(listing.matches).length && <p className="p-3 text-sm">No matching lookup values.</p>}</div>}</Panel>
 {canEdit && <Panel title={editing ? "Edit lookup value" : "Add lookup value"}><form key={editing?.version ?? "new"} className="space-y-3" onSubmit={async event => {event.preventDefault(); const form = event.currentTarget; const data = new FormData(form); setBusy(true);setError("");try {await moduleApi("membership", `/setup/${kind}${editing ? `/${editing.id}` : ""}`, editing ? "PATCH" : "POST", {name: data.get("name"), description: data.get("description"), version: editing?.version});setEditing(null);form.reset();setRefresh(v => v+1);}catch(e){setError(e instanceof Error ? e.message : "Unable to save.");}finally{setBusy(false);}}}><Field label="Name"><input name="name" required maxLength={160} defaultValue={editing?.name} className={inputClass} /></Field><Field label="Description"><textarea name="description" maxLength={2000} defaultValue={editing?.description} className={inputClass} rows={3} /></Field><div className="flex gap-2"><button className={primaryClass} disabled={busy}>Save lookup value</button>{editing && <button type="button" className={buttonClass} disabled={busy} onClick={() => setEditing(null)}>Cancel edit</button>}</div></form></Panel>}
 </main>;
}
