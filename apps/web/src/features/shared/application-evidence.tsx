"use client";

import { useEffect, useState } from "react";

type Evidence = { id: string; name: string; size: number; scanStatus: string };
type Change = { draftVersion: string };
type Props = { path: string; staff?: boolean; version?: string; disabled?: boolean; canScan?: boolean;
  onChanged?: (version: string) => void; onBusy?: (busy: boolean) => void };

export function ApplicationEvidence({ path, staff = false, version, disabled = false, canScan = false, onChanged, onBusy }: Props) {
  const [items, setItems] = useState<Evidence[] | null>(null);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const [reload, setReload] = useState(0);
  const [removing, setRemoving] = useState<string | null>(null);
  async function request(suffix = "", method = "GET", body?: FormData | object) {
    const headers: Record<string, string> = {};
    if (method !== "GET") {
      const token = await fetch(`/api/v1/${staff ? "auth" : "portal"}/csrf`, { cache: "no-store" });
      if (!token.ok) throw new Error("Unable to obtain a security token. Refresh and try again.");
      headers["X-CSRF-TOKEN"] = (await token.json()).token;
    }
    if (body && !(body instanceof FormData)) headers["Content-Type"] = "application/json";
    const response = await fetch(`${path}${suffix}`, { method, cache: "no-store", headers,
      body: body instanceof FormData ? body : body ? JSON.stringify(body) : undefined });
    if (!response.ok) {
      if (!staff && response.status === 401) window.dispatchEvent(new Event("portal-session-expired"));
      const problem = await response.json().catch(() => null);
      throw new Error(response.status === 404 || response.status === 403 ? "These documents are unavailable or you no longer have access." : problem?.title ?? "Unable to load documents. Please try again.");
    }
    return response;
  }
  useEffect(() => {
    let active = true;
    fetch(path, { cache: "no-store" }).then(async response => {
      if (!response.ok) {
        if (!staff && response.status === 401) window.dispatchEvent(new Event("portal-session-expired"));
        throw new Error("These documents are unavailable or you no longer have access.");
      }
      return response.json() as Promise<Evidence[]>;
    }).then(value => { if (active) setItems(value); }).catch(e => { if (active) { setItems(null); setError(e.message); } });
    return () => { active = false; };
  }, [path, staff, reload]);
  async function run(work: () => Promise<void>) {
    setBusy(true); onBusy?.(true); setError("");
    try { await work(); } catch (e) { setError(e instanceof Error ? e.message : "Unable to update evidence."); }
    finally { setBusy(false); onBusy?.(false); }
  }
  function refresh() { setItems(null); setError(""); setReload(n => n + 1); }
  const locked = busy || disabled;
  return <section className="mt-6 min-w-0 space-y-4 rounded-xl border border-slate-200 p-4 print:hidden" aria-label="Application evidence">
    <div className="flex flex-wrap items-center justify-between gap-3"><h3 className="font-semibold">Application evidence</h3><button type="button" className="text-sm underline" disabled={locked} onClick={refresh}>Refresh documents</button></div>
    <p className="text-sm text-slate-600">PDF, PNG, JPEG or UTF-8 text. Up to 10 files, 5 MB each. Downloads stay blocked until a security scan passes. Submitted files are retained with the application.</p>
    {error && <p role="alert" className="break-words text-sm text-red-800">{error}</p>}
    {!items && !error && <p role="status">Loading documents…</p>}
    {items?.length === 0 && <p className="text-sm">No evidence files attached.</p>}
    {items?.map(item => <article key={item.id} className="space-y-2 rounded-lg bg-slate-50 p-3 text-sm">
      <p className="break-all font-medium">{item.name}</p><p>{Math.ceil(item.size / 1024)} KB · {item.scanStatus === "clean" ? "Scan passed" : item.scanStatus === "rejected" ? "Blocked by security scan" : "Awaiting security scan"}</p>
      {item.scanStatus === "quarantined" && <p className="text-slate-600">The scanner has not cleared this file. Your association can retry the check when scanning is available.</p>}
      <div className="flex flex-wrap gap-4">
        {item.scanStatus === "clean" && <button type="button" className="underline" disabled={locked} onClick={() => void run(async () => {
          const response = await request(`/${item.id}`); const url = URL.createObjectURL(await response.blob());
          const link = document.createElement("a"); link.href = url; link.download = item.name; link.click(); setTimeout(() => URL.revokeObjectURL(url), 1000);
        })}>Download {item.name}</button>}
        {item.scanStatus === "quarantined" && (version || canScan) && <button type="button" className="underline" disabled={locked} onClick={() => void run(async () => {
          const response = await request(`/${item.id}/scan`, "POST", version ? { version } : undefined);
          if (version) onChanged?.(((await response.json()) as Change).draftVersion); refresh();
        })}>Retry security scan</button>}
        {version && <button type="button" className="text-red-800 underline" disabled={locked} onClick={() => setRemoving(item.id)}>Remove {item.name}</button>}
      </div>
      {removing === item.id && <div className="space-y-2"><p>Remove this file from the draft?</p><button type="button" className="mr-4 text-red-800 underline" disabled={locked} onClick={() => void run(async () => {
        const change: Change = await (await request(`/${item.id}`, "DELETE", { version })).json(); onChanged?.(change.draftVersion); setRemoving(null); refresh();
      })}>Confirm removal</button><button type="button" className="underline" disabled={locked} onClick={() => setRemoving(null)}>Keep file</button></div>}
    </article>)}
    {version && <form className="space-y-3" onSubmit={e => {
      e.preventDefault(); const form = e.currentTarget; const data = new FormData(form); data.set("version", version);
      const file = data.get("file");
      if (!(file instanceof File) || file.size === 0 || file.size > 5 * 1024 * 1024) { setError("Choose a file up to 5 MB."); return; }
      void run(async () => { const change: Change = await (await request("", "POST", data)).json(); onChanged?.(change.draftVersion); form.reset(); refresh(); });
    }}><label className="block text-sm">Evidence file<input type="file" name="file" accept=".pdf,.png,.jpg,.jpeg,.txt" required disabled={locked} className="mt-2 block w-full min-w-0 text-sm" /></label><button disabled={locked || (items?.length ?? 0) >= 10} className="rounded-lg bg-[#183a31] px-4 py-2 text-sm font-semibold text-white disabled:opacity-50">Upload evidence</button></form>}
  </section>;
}
