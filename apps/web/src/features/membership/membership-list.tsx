"use client";

import { useCallback, useEffect, useState, useSyncExternalStore, type ReactNode } from "react";
import { moduleApi } from "../shared/module-api";
import { FilterViewToolbar } from "../shared/filter-view-toolbar";
import { ListingSearch } from "../shared/entity-listing-controls";
import { Field, inputClass, buttonClass } from "../shared/module-ui";

type Query = { page: number; pageSize: number; search: string; status: string; archive: string };
type PagedResult<T> = { items: T[]; totalCount: number; page: number; pageSize: number };
const defaultQuery = JSON.stringify({ page: 1, pageSize: 20, search: "", status: "", archive: "current" });
const queryEvent = "nexora-membership-query-change";
const memoryQueries = new Map<string, string>();
const subscribeQuery = (notify: () => void) => {
  window.addEventListener(queryEvent, notify);
  window.addEventListener("storage", notify);
  return () => { window.removeEventListener(queryEvent, notify); window.removeEventListener("storage", notify); };
};
function querySnapshot(key: string) {
  try {
    const raw = sessionStorage.getItem(key) ?? memoryQueries.get(key) ?? defaultQuery;
    const saved = JSON.parse(raw) as Query;
    if (Number.isInteger(saved.page) && saved.page > 0 && [10,20,50,100].includes(saved.pageSize) && typeof saved.search === "string" && typeof saved.status === "string" && ["current","archived","all"].includes(saved.archive)) return raw;
  } catch { /* Browser storage is optional. */ }
  return memoryQueries.get(key) ?? defaultQuery;
}

export function useMembershipList<T>(path: "members" | "applications", refresh: number) {
  const storageKey = `nexora-membership-${path}-query`;
  const snapshot = useSyncExternalStore(subscribeQuery, () => querySnapshot(storageKey), () => defaultQuery);
  const query = JSON.parse(snapshot) as Query;
  const setQuery = useCallback((update: (previous: Query) => Query) => {
    const next = JSON.stringify(update(JSON.parse(querySnapshot(storageKey)) as Query));
    memoryQueries.set(storageKey, next);
    try { sessionStorage.setItem(storageKey, next); } catch { /* Browser storage is optional. */ }
    window.dispatchEvent(new Event(queryEvent));
  }, [storageKey]);
  const params = new URLSearchParams({ page: String(query.page), pageSize: String(query.pageSize), search: query.search, status: query.status });
  if (path === "members") params.set("archive", query.archive);
  const url = `/${path}?${params}`;
  const key = `${url}:${refresh}`;
  const [result, setResult] = useState<{ key: string; data?: PagedResult<T>; error?: string }>();

  useEffect(() => {
    let active = true;
    moduleApi<PagedResult<T>>("membership", url).then(data => {
      if (!active) return;
      const lastPage = Math.max(1, Math.ceil(data.totalCount / data.pageSize));
      if (data.page > lastPage) {
        setQuery(current => ({ ...current, page: lastPage }));
        return;
      }
      setResult({ key, data });
    }).catch(error => {
      if (active) setResult({ key, error: error instanceof Error ? error.message : "Unable to load records." });
    });
    return () => { active = false; };
  }, [url, key, setQuery]);

  const current = result?.key === key ? result : undefined;
  return {
    query,
    items: current?.data?.items ?? [],
    totalCount: current?.data?.totalCount ?? 0,
    loading: !current,
    error: current?.error,
    change: (changes: Partial<Query>) => setQuery(previous => ({ ...previous, ...changes, page: changes.page ?? 1 })),
  };
}

type ListControls = ReturnType<typeof useMembershipList>;

export function MembershipListControls({ label, statuses, list, retry, actions, columns }: {
  actions?: ReactNode; columns?: ReactNode; label: string; statuses: string[]; list: ListControls; retry: () => void;
}) {
  const { query, change, error } = list;
  return <div className="mb-4 space-y-3">
    <FilterViewToolbar actions={actions} onAllRecords={() => change({search:"",status:"",...(label==="members"?{archive:"all"}:{})})} search={<ListingSearch label={`Search ${label}`} value={query.search} onChange={value => change({search:value})} onSearch={() => change({search:query.search})} />} filters={<div className="grid gap-3 sm:grid-cols-2"><Field label={`${label} status`}><select className={inputClass} value={query.status} onChange={e => change({ status: e.target.value })}><option value="">All statuses</option>{statuses.map(status => <option key={status}>{status}</option>)}</select></Field><Field label={`${label} page size`}><select className={inputClass} value={query.pageSize} onChange={e => change({ pageSize: Number(e.target.value) })}>{[10,20,50,100].map(size => <option key={size}>{size}</option>)}</select></Field>{label === "members" && <Field label="Archive state"><select className={inputClass} value={query.archive} onChange={e => change({ archive: e.target.value })}><option value="current">Current records</option><option value="archived">Archived records</option><option value="all">All records</option></select></Field>}</div>} columns={columns} />
    {error && <div role="alert">{error} <button className={buttonClass} onClick={retry}>Retry {label}</button></div>}
  </div>;
}
export function MembershipListFooter({label,list}:{label:string;list:ListControls}) {
 const {query,change,loading,error,totalCount,items}=list;
 const pages=Math.max(1,Math.ceil(totalCount/query.pageSize));const first=items.length?(query.page-1)*query.pageSize+1:0;
 return <footer className="flex flex-wrap items-center justify-between gap-3 border-t border-slate-200 px-4 py-3 text-sm"><p role="status">{loading?'Loading…':`Showing ${first}–${items.length?first+items.length-1:0} of ${totalCount}`}</p><label>Rows per page <select aria-label="Rows per page" className="ml-2 rounded border p-1" value={query.pageSize} onChange={e=>change({pageSize:Number(e.target.value)})}>{[10,20,50,100].map(n=><option key={n}>{n}</option>)}</select></label><nav aria-label={`${label} pages`} className="flex items-center gap-3"><button className={buttonClass} disabled={loading||!!error||query.page<=1} onClick={()=>change({page:query.page-1})}>Previous</button><span>Page {query.page} of {pages}</span><button className={buttonClass} disabled={loading||!!error||query.page>=pages} onClick={()=>change({page:query.page+1})}>Next</button></nav></footer>;
}
