"use client";

import { useEffect, useRef, useState } from "react";
import { crm } from "./api";
import type { Directory, Field, ListState, View } from "./types";

const defaults = ["name", "status", "category"];
type State = { query: ListState; columns: string[]; selected: string; viewId: string };

export function useCrmListing(initial: ListState, initialRecord: string, fixedKind: boolean, refresh: number) {
  const [state, setState] = useState<State>({ query: initial, columns: defaults, selected: initialRecord, viewId: "" });
  const [fields, setFields] = useState<Field[]>([]);
  const [directory, setDirectory] = useState<Directory>({ users: [], teams: [] });
  const [views, setViews] = useState<View[]>([]);
  const availableViews = useRef<View[]>([]);
  const [metadataError, setMetadataError] = useState(false);
  const [retry, setRetry] = useState(0);
  const [ready, setReady] = useState(false);
  const [notice, setNotice] = useState("");
  const [search, setSearch] = useState(initial.search);
  const booted = useRef(false);
  const current = useRef(state);

  function cleanColumns(columns: string[], kind: string, definitions = fields) {
    const allowed = new Set([...defaults, "updatedAtUtc", ...definitions.filter(f => f.kind === kind).map(f => `field:${f.id}`)]);
    const valid = [...new Set(columns)].filter(c => allowed.has(c));
    return ["name", ...valid.filter(c => c !== "name")];
  }
  function writeUrl(next: State, push: boolean) {
    const url = new URL(window.location.href);
    for (const [key, value] of Object.entries(next.query)) url.searchParams.set(key, String(value ?? ""));
    url.searchParams.set("columns", next.columns.join(","));
    for (const [key, value] of [["record", next.selected], ["viewId", next.viewId]]) {
      if (value) url.searchParams.set(key, value); else url.searchParams.delete(key);
    }
    if (url.href !== window.location.href) window.history[push ? "pushState" : "replaceState"](null, "", url);
  }
  function commit(next: State, push = true) {
    current.current = next;
    setState(next);
    writeUrl(next, push);
  }
  function viewQuery(view: View): ListState {
    return { kind: view.kind, search: view.search, status: view.status, sort: view.sort, archived: view.archived, page: 1,
      ownerUserId: view.ownerUserId ?? "", ownerTeamId: view.ownerTeamId ?? "", category: view.category ?? "" };
  }

  useEffect(() => {
    let cancelled = false;
    function restore(available: View[], definitions: Field[]) {
      const params = new URLSearchParams(window.location.search);
      const kind = fixedKind ? initial.kind : params.get("kind") === "account" ? "account" : initial.kind;
      const requestedView = params.get("viewId") ?? "";
      const view = available.find(v => v.id === requestedView && v.kind === kind);
      const base = view ? viewQuery(view) : { ...initial, kind, ownerUserId: "", ownerTeamId: "", category: "" };
      const query: ListState = { ...base, kind };
      for (const key of ["search", "status", "sort", "ownerUserId", "ownerTeamId", "category"] as const) {
        if (params.has(key)) query[key] = params.get(key)!;
      }
      if (!["", "active", "prospect", "inactive", "lapsed"].includes(query.status)) query.status = "";
      if (!["name", "-name", "updated"].includes(query.sort)) query.sort = "name";
      if (params.has("archived")) query.archived = params.get("archived") === "true";
      const page = Number(params.get("page") ?? 1);
      const size=Number(params.get("pageSize")??20); query.pageSize=[10,20,50,100].includes(size)?size:20;
      query.page = Number.isInteger(page) && page > 0 ? Math.min(page, 10000) : 1;
      const requestedColumns = (params.get("columns") ?? view?.columns ?? defaults.join(",")).split(",");
      const columns = cleanColumns(requestedColumns, kind, definitions);
      setNotice(requestedView && !view ? "Saved view is unavailable. Showing the URL filters instead." :
        requestedColumns.some(c => !columns.includes(c)) ? "Unavailable columns were removed from this listing." : "");
      const next = { query, columns, selected: params.get("record") ?? initialRecord, viewId: view?.id ?? "" };
      current.current = next; setState(next); setSearch(query.search); writeUrl(next, false);
    }
    let onPop: (() => void) | undefined;
    Promise.all([crm<Directory>("/directory"), crm<Field[]>("/fields"), crm<View[]>("/views")]).then(([d, f, v]) => {
      if (cancelled) return;
      setDirectory(d); setFields(f); setViews(v); availableViews.current = v; setMetadataError(false);
      if (!booted.current) { restore(v, f); booted.current = true; }
      else {
        const previous = current.current;
        const columns = cleanColumns(previous.columns, previous.query.kind, f);
        if (columns.join() !== previous.columns.join()) {
          setNotice("Unavailable columns were removed from this listing.");
          commit({ ...previous, columns }, false);
        }
      }
      setReady(true);
      onPop = () => restore(availableViews.current, f);
      window.addEventListener("popstate", onPop);
    }).catch(e => { if (!cancelled) { setNotice(e.message); setMetadataError(true); } });
    return () => { cancelled = true; if (onPop) window.removeEventListener("popstate", onPop); };
    // Metadata refreshes must not reapply a saved view over unsaved listing changes.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [refresh, initial.kind, fixedKind, retry]);

  return {
    ...state, fields, directory, views, ready, notice, search, setSearch, metadataError,
    retry() { setRetry(n => n + 1); },
    filter(change: Partial<ListState>) {
      const previous = current.current;
      const query = { ...previous.query, ...change, page: change.page ?? 1 };
      const changedKind = query.kind !== previous.query.kind;
      commit({ ...previous, query, ...(changedKind ? { columns: defaults, viewId: "", selected: "" } : {}) });
    },
    scope(ownerUserId = "") {
      const previous = current.current;
      const query = { ...initial, kind: previous.query.kind, search: "", status: "", archived: false, page: 1, ownerUserId, ownerTeamId: "", category: "" };
      setSearch(""); commit({ ...previous, query, selected: "", viewId: "" });
    },
    setSelected(selected: string) { commit({ ...current.current, selected }); },
    setColumns(update: (columns: string[]) => string[]) {
      commit({ ...current.current, columns: cleanColumns(update(current.current.columns), current.current.query.kind) });
    },
    apply(viewId: string) {
      const view = views.find(v => v.id === viewId && v.kind === current.current.query.kind);
      const query = view ? viewQuery(view) : { ...initial, kind: current.current.query.kind, ownerUserId: "", ownerTeamId: "", category: "" };
      const requested = view?.columns.split(",") ?? defaults;
      const columns = cleanColumns(requested, query.kind);
      setNotice(requested.some(c => !columns.includes(c)) ? "Unavailable columns were removed from this listing." : "");
      setSearch(query.search); commit({ query, columns, selected: "", viewId: view?.id ?? "" });
    },
    async save(name: string, overwrite: boolean) {
      const previous = current.current;
      const payload = { ...previous.query, name, columns: previous.columns.join(","),
        ownerUserId: previous.query.ownerUserId || null, ownerTeamId: previous.query.ownerTeamId || null };
      const view = await crm<View>(overwrite ? `/views/${previous.viewId}` : "/views", overwrite ? "PUT" : "POST", payload);
      availableViews.current = [...availableViews.current.filter(x => x.id !== view.id), view];
      setViews(availableViews.current);
      setNotice("");
      commit({ ...current.current, viewId: view.id }, false);
    },
    async remove() {
      const id = current.current.viewId;
      await crm(`/views/${id}`, "DELETE");
      availableViews.current = availableViews.current.filter(x => x.id !== id);
      setViews(availableViews.current);
      commit({ ...current.current, viewId: "" }, false);
    },
  };
}
