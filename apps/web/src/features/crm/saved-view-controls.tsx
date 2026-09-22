"use client";

import { useState } from "react";
import type { useCrmListing } from "./use-crm-listing";

const control = "rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm disabled:opacity-50";
export function SavedViewControls({ listing }: { listing: ReturnType<typeof useCrmListing> }) {
  const [mode, setMode] = useState<"create" | "update" | "delete" | null>(null);
  const [name, setName] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const active = listing.views.find(v => v.id === listing.viewId);
  return <section aria-label="Saved views" className="space-y-3 rounded-xl border border-slate-200 bg-white p-4 text-slate-800">
    <fieldset disabled={busy || !listing.ready} className="flex flex-wrap items-end gap-2">
      <label className="text-sm">Saved view<select className={`${control} ml-2`} value={listing.viewId} onChange={e => { listing.apply(e.target.value); setMode(null); setError(""); }}>
        <option value="">Default view</option>
        {listing.views.filter(v => v.kind === listing.query.kind).map(v => <option key={v.id} value={v.id}>{v.name}</option>)}
      </select></label>
      <button className={control} onClick={() => { setMode("create"); setName(""); setError(""); }}>Save as new view</button>
      {active && <><button className={control} onClick={() => { setMode("update"); setName(active.name); setError(""); }}>Save or rename view</button>
        <button className={control} onClick={() => { setMode("delete"); setError(""); }}>Delete view</button></>}
    </fieldset>
    {mode && <form className="flex flex-wrap items-end gap-2" aria-label={mode === "delete" ? "Delete saved view" : "Save saved view"} onSubmit={async e => {
      e.preventDefault(); setBusy(true); setError("");
      try { if (mode === "delete") await listing.remove(); else await listing.save(name, mode === "update"); setMode(null); }
      catch (e) { setError(e instanceof Error ? e.message : "Unable to save view."); }
      finally { setBusy(false); }
    }}>
      {mode === "delete" ? <p className="text-sm">Delete “{active?.name}”?</p> : <label className="text-sm">View name<input className={`${control} ml-2`} required maxLength={80} value={name} onChange={e => setName(e.target.value)} /></label>}
      <button className={control} disabled={busy}>{mode === "delete" ? "Confirm delete" : "Save view"}</button>
      <button className={control} type="button" disabled={busy} onClick={() => setMode(null)}>Cancel</button>
    </form>}
    {error && <p role="alert" className="text-sm text-red-700">{error}</p>}
    {listing.notice && <p role="status" className="text-sm text-amber-800">{listing.notice}</p>}
    {listing.metadataError && <button className={control} onClick={() => listing.retry()}>Retry loading views</button>}
  </section>;
}
