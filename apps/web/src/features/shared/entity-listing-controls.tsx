"use client";

import { useState, type ReactNode } from "react";
import { FilterViewToolbar } from "./filter-view-toolbar";

export function ListingSearch({ value, onChange, onSearch, label = "Search records", disabled = false }: { value: string; onChange: (value: string) => void; onSearch: () => void; label?: string; disabled?: boolean }) {
  return <form className="flex items-end gap-2" onSubmit={event => { event.preventDefault(); onSearch(); }}><label className="min-w-0 flex-1"><span className="sr-only">{label}</span><input type="search" value={value} maxLength={160} onChange={event => onChange(event.target.value)} placeholder="Search records…" className="w-full rounded-lg border border-blue-300 bg-white px-3 py-2 text-sm text-slate-900 placeholder:text-blue-400 focus:outline-none focus:ring-2 focus:ring-blue-500" /></label><button disabled={disabled} className="rounded-lg bg-blue-600 px-3 py-2 text-sm font-semibold text-white hover:bg-blue-700 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-600 disabled:opacity-50">Search</button></form>;
}

export function useListingColumns(labels: string[]) {
  const [hidden, setHidden] = useState<string[]>([]);
  const controls = <div className="space-y-2">{labels.map((label, index) => <label key={label} className="flex items-center gap-3 rounded-lg border border-slate-200 p-3 text-sm"><input type="checkbox" disabled={index === 0} checked={!hidden.includes(label)} onChange={() => setHidden(current => current.includes(label) ? current.filter(value => value !== label) : [...current, label])} />{label}</label>)}</div>;
  return { controls, show: (label: string) => !hidden.includes(label) };
}

const searchable = ["name", "displayName", "email", "subject", "purpose", "description", "title", "contactName", "accountName", "customerName", "number", "reference", "sku", "message", "category", "status", "kind", "venue", "priority", "slug"];
function recordStatus(row: Record<string, unknown>) {
  if (typeof row.status === "string") return row.status;
  if (typeof row.isActive === "boolean") return row.isActive ? "active" : "inactive";
  if (typeof row.isRead === "boolean") return row.isRead ? "read" : "unread";
  if ("statementReference" in row) return row.statementReference ? "reconciled" : "unreconciled";
  return "";
}
export function useEntityListing({ records, actions, currentUserId, columns = ["Name", "Details"] }: { records: object[]; actions?: ReactNode; currentUserId?: string; columns?: string[] }) {
  const [draft, setDraft] = useState("");
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");
  const [mine, setMine] = useState(false);
  const selection = useListingColumns(columns);
  const statuses = [...new Set(records.map(record => recordStatus(record as Record<string, unknown>)).filter((value): value is string => value.length > 0))];
  function matches(record: object) {
    const row = record as Record<string, unknown>;
    return (!status || recordStatus(row) === status) && (!mine || row.assigneeUserId === currentUserId || row.ownerUserId === currentUserId) && (!search || searchable.some(key => String(row[key] ?? "").toLocaleLowerCase().includes(search.toLocaleLowerCase())));
  }
  const toolbar = <FilterViewToolbar search={<ListingSearch value={draft} onChange={setDraft} onSearch={() => setSearch(draft.trim())} />} actions={actions} onAllRecords={() => { setDraft(""); setSearch(""); setStatus(""); setMine(false); }} onMyRecords={currentUserId ? () => { setMine(true); setStatus(""); setSearch(""); setDraft(""); } : undefined} filters={statuses.length ? <label className="block text-sm font-medium">Status<select value={status} onChange={event => setStatus(event.target.value)} className="mt-1 w-full rounded-lg border border-slate-300 bg-white p-2"><option value="">All statuses</option>{statuses.map(value => <option key={value}>{value}</option>)}</select></label> : <p className="text-sm text-slate-600">No additional filters for this listing.</p>} columns={selection.controls} />;
  return { toolbar, matches, show: selection.show, filterKey: JSON.stringify([search,status,mine]) };
}
