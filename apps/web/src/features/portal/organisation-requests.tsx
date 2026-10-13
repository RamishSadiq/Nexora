"use client";
import { useEffect, useState } from "react";
import { portal } from "./api";

export type OrganisationRequest = { id: string; organisationName: string; jobTitle: string; message: string; status: string; version: string; createdAtUtc: string; decidedAtUtc: string | null; decisionMessage: string; canSubmitApplications: boolean };
export type RequestPage<T> = { items: T[]; totalCount: number; pageSize: number };
const input = "mt-2 w-full rounded-lg border border-slate-300 bg-white px-4 py-3 text-slate-900";

export function OrganisationRequests({ enabled }: { enabled: boolean }) {
  const [data, setData] = useState<RequestPage<OrganisationRequest> | null>(null);
  const [page, setPage] = useState(1), [reload, setReload] = useState(0);
  const [error, setError] = useState(""), [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);
  useEffect(() => {
    let active = true;
    portal<RequestPage<OrganisationRequest>>(`/organisation-requests?page=${page}`).then(value => { if (active) setData(value); }).catch(e => { if (active) setError(e.message); });
    return () => { active = false; };
  }, [page, reload]);
  async function run(work: () => Promise<void>) {
    setBusy(true); setError(""); setMessage("");
    try { await work(); setReload(n => n + 1); } catch (e) { setError(e instanceof Error ? e.message : "Unable to update request."); } finally { setBusy(false); }
  }
  return <section aria-label="Company access requests" className="space-y-5 border-t border-slate-200 pt-6">
    <div className="flex flex-wrap items-center justify-between gap-3"><h3 className="text-xl font-semibold">Company access requests</h3><button className="text-sm underline" disabled={busy} onClick={() => { setError(""); setReload(n => n + 1); }}>Refresh requests</button></div>
    <p className="text-sm text-slate-600">Tell the membership team which company you represent. They will verify your relationship and approve access. A request does not give you access to company records.</p>
    {error && <p role="alert" className="rounded-lg bg-red-50 p-3 text-red-900">{error}</p>}{message && <p role="status" className="rounded-lg bg-emerald-50 p-3">{message}</p>}
    {enabled ? <form onSubmit={e => { e.preventDefault(); const form = e.currentTarget; const fields = new FormData(form); void run(async () => {
      await portal("/organisation-requests", "POST", { organisationName: fields.get("organisationName"), jobTitle: fields.get("jobTitle"), message: fields.get("message") });
      form.reset(); setPage(1); setMessage("Your request has been sent to the membership team for review.");
    }); }}><fieldset className="space-y-4" disabled={busy}>
      <label className="block">Company name<input className={input} name="organisationName" maxLength={160} required /></label>
      <label className="block">Your job title<input className={input} name="jobTitle" maxLength={160} /></label>
      <label className="block">How are you connected to this company?<textarea className={input} name="message" rows={3} maxLength={2000} required /></label>
      <button className="portal-action rounded-full bg-[var(--portal-brand,#183a31)] px-6 py-3 font-semibold text-white disabled:opacity-50">{busy ? "Sending…" : "Request company access"}</button>
    </fieldset></form> : <p className="text-sm">New company access requests are currently closed. Contact your membership team.</p>}
    <h4 className="font-semibold">Your requests</h4>
    {!data ? <p role="status">Loading requests…</p> : <>{!data.items.length && <p>No company access requests yet.</p>}{data.items.map(row => <article key={row.id} className="portal-subcard space-y-2 rounded-xl border border-slate-200 p-4">
      <h5 className="break-words font-semibold">{row.organisationName}</h5><p className="text-sm capitalize">{row.status === "pending" ? "Awaiting review" : row.status}</p>
      {row.jobTitle && <p className="text-sm">{row.jobTitle}</p>}<p className="whitespace-pre-wrap break-words text-sm">{row.message}</p>
      {row.decisionMessage && <p className="whitespace-pre-wrap break-words text-sm">Membership team: {row.decisionMessage}</p>}
      {row.status === "approved" && <p className="text-sm">{row.canSubmitApplications ? "Viewing and application access approved." : "Viewing access approved."} Refresh organisations above to load your current access.</p>}
      {row.status === "pending" && <button className="text-sm underline" disabled={busy} onClick={() => void run(async () => { await portal(`/organisation-requests/${row.id}/cancel`, "POST", { version: row.version }); setMessage("Request cancelled."); })}>Cancel request<span className="sr-only"> for {row.organisationName}</span></button>}
    </article>)}<div className="flex items-center gap-4 text-sm"><button className="underline disabled:opacity-40" disabled={busy || page === 1} onClick={() => { setData(null); setPage(n => n - 1); }}>Previous request page</button><span>Page {page} · {data.totalCount} requests</span><button className="underline disabled:opacity-40" disabled={busy || page * data.pageSize >= data.totalCount} onClick={() => { setData(null); setPage(n => n + 1); }}>Next request page</button></div></>}
  </section>;
}
