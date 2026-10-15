"use client";

import { useEffect, useId, useRef, useState, type ReactNode } from "react";
import { ChevronDown, ListFilter } from "lucide-react";

const action = "block w-full rounded-lg px-3 py-2 text-left text-sm font-medium text-slate-700 hover:bg-indigo-50 focus-visible:outline-2 focus-visible:outline-indigo-600 disabled:opacity-50";

export function FilterViewToolbar({ search, actions, filters, onAllRecords, onMyRecords, disabled = false }: {
  search: ReactNode; actions?: ReactNode; filters: ReactNode;
  onAllRecords: () => void; onMyRecords?: () => void; disabled?: boolean;
}) {
  const [open, setOpen] = useState(false);
  const [filtersOpen, setFiltersOpen] = useState(false);
  const container = useRef<HTMLDivElement>(null);
  const trigger = useRef<HTMLButtonElement>(null);
  const id = useId();
  useEffect(() => {
    if (!open) return;
    const outside = (event: PointerEvent) => { if (!container.current?.contains(event.target as Node)) setOpen(false); };
    const escape = (event: KeyboardEvent) => { if (event.key === "Escape") { setOpen(false); trigger.current?.focus(); } };
    document.addEventListener("pointerdown", outside);
    document.addEventListener("keydown", escape);
    return () => { document.removeEventListener("pointerdown", outside); document.removeEventListener("keydown", escape); };
  }, [open]);
  function select(action: () => void) { action(); setOpen(false); trigger.current?.focus(); }
  return <section aria-label="Listing controls" className="flex min-w-0 flex-wrap items-center justify-between gap-3 rounded-xl border border-blue-200 bg-blue-50 p-4">
    <div ref={container} className="relative">
      <button ref={trigger} type="button" aria-expanded={open} aria-controls={id} disabled={disabled} onClick={() => { setOpen(value => !value); setFiltersOpen(false); }} className="flex items-center gap-2 rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-800 hover:bg-slate-50 focus-visible:outline-2 focus-visible:outline-indigo-600 disabled:opacity-50"><ListFilter className="size-4" />Filter view<ChevronDown className="size-4" /></button>
      {open && <div id={id} className="absolute left-0 top-full z-40 mt-2 w-[min(34rem,calc(100vw-4rem))] max-h-[65vh] overflow-y-auto rounded-xl border border-slate-200 bg-white p-2 shadow-xl" aria-label="Filter view options">
        <div className="grid gap-1 sm:grid-cols-2">
          <button type="button" className={action} onClick={() => select(onAllRecords)}>All records</button>
          <button type="button" className={action} disabled={!onMyRecords} title={!onMyRecords ? "Record ownership is unavailable for this listing" : undefined} onClick={() => onMyRecords && select(onMyRecords)}>My records</button>
          <button type="button" className={action} aria-expanded={filtersOpen} onClick={() => setFiltersOpen(value => !value)}>Filters</button>
        </div>
        {filtersOpen && <div className="mt-2 space-y-4 border-t border-slate-200 p-3" aria-label="Filters panel">{filters}</div>}
      </div>}
    </div>
    <div className="flex w-full min-w-0 flex-wrap items-center justify-end gap-2 sm:ml-auto sm:w-auto"><div className="w-full min-w-0 sm:w-80">{search}</div>{actions}</div>
  </section>;
}
