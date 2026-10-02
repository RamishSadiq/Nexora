"use client";

import { useEffect, useState, type ReactNode } from "react";
import { moduleApi } from "../shared/module-api";
import { FilterViewToolbar } from "../shared/filter-view-toolbar";
import { ListingSearch } from "../shared/entity-listing-controls";
import { Field, inputClass, buttonClass } from "../shared/module-ui";

type Query = { page: number; pageSize: number; search: string; status: string };
type PagedResult<T> = { items: T[]; totalCount: number; page: number; pageSize: number };

export function useMembershipList<T>(path: "members" | "applications", refresh: number) {
  const [query, setQuery] = useState<Query>({ page: 1, pageSize: 25, search: "", status: "" });
  const params = new URLSearchParams({ page: String(query.page), pageSize: String(query.pageSize), search: query.search, status: query.status });
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
  }, [url, key]);

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
  const { query, change, loading, error, totalCount, items } = list;
  const pages = Math.max(1, Math.ceil(totalCount / query.pageSize));
  const first = items.length ? (query.page - 1) * query.pageSize + 1 : 0;
  return <div className="mb-4 space-y-3">
    <FilterViewToolbar actions={actions} onAllRecords={() => change({search:"",status:""})} search={<ListingSearch label={`Search ${label}`} value={query.search} onChange={value => change({search:value})} onSearch={() => change({search:query.search})} />} filters={<div className="grid gap-3 sm:grid-cols-2"><Field label={`${label} status`}><select className={inputClass} value={query.status} onChange={e => change({ status: e.target.value })}><option value="">All statuses</option>{statuses.map(status => <option key={status}>{status}</option>)}</select></Field><Field label={`${label} page size`}><select className={inputClass} value={query.pageSize} onChange={e => change({ pageSize: Number(e.target.value) })}>{[10,25,50,100].map(size => <option key={size}>{size}</option>)}</select></Field></div>} columns={columns} />
    {error ? <div role="alert">{error} <button className={buttonClass} onClick={retry}>Retry {label}</button></div> : <p role="status" aria-live="polite" className="text-sm text-slate-600">{loading ? `Loading ${label}…` : `Showing ${first}–${items.length ? first + items.length - 1 : 0} of ${totalCount}`}</p>}
    <nav aria-label={`${label} pages`} className="flex items-center gap-3 text-sm">
      <button className={buttonClass} disabled={loading || !!error || query.page <= 1} onClick={() => change({ page: query.page - 1 })}>Previous</button>
      {!loading && !error && <span>Page {query.page} of {pages}</span>}
      <button className={buttonClass} disabled={loading || !!error || query.page >= pages} onClick={() => change({ page: query.page + 1 })}>Next</button>
    </nav>
  </div>;
}
