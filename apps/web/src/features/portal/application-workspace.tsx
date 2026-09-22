"use client";
import { useEffect, useState } from "react";
import { portal } from "./api";
import Link from "next/link";
import type { Route } from "next";
import { ApplicationEvidence } from "@/features/shared/application-evidence";
type Product = { id: string; name: string };
type Draft = { id: string; version: string; productId: string; startsAtUtc: string; notes: string; total: number; currency: string; terms: string; applicationId: string | null };
type Edit = { id: string; version: string | null; productId: string; date: string; notes: string };
const input = "mt-2 w-full rounded-lg border border-slate-300 bg-white px-4 py-3 text-slate-900";
const button = "rounded-full bg-[#183a31] px-6 py-3 font-semibold text-white disabled:opacity-50";
export function ApplicationWorkspace({ onSubmitted, accountId }: { onSubmitted: () => void; accountId?: string }) {
  const scope = accountId ? `/organisations/${accountId}` : "";
  const [products, setProducts] = useState<Product[]>([]);
  const [drafts, setDrafts] = useState<Draft[]>([]);
  const [edit, setEdit] = useState<Edit | null>(null);
  const [review, setReview] = useState<Draft | null>(null);
  const [accepted, setAccepted] = useState(false);
  const [ready, setReady] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [reload, setReload] = useState(0);
  const [discard, setDiscard] = useState<Draft | null>(null);
  const [submittedId, setSubmittedId] = useState<string | null>(null);
  useEffect(() => {
    let active = true;
    Promise.all([portal<Product[]>(`${scope}/application-products`), portal<Draft[]>(`${scope}/drafts`)]).then(([p, d]) => {
      if (active) { setProducts(p); setDrafts(d); setReady(true); setError(""); }
    }).catch(e => { if (active) setError(e.message); });
    return () => { active = false; };
  }, [reload, scope]);
  function change(patch: Partial<Edit>) { if (edit) setEdit({ ...edit, ...patch }); setReview(null); setAccepted(false); }
  function open(d: Draft) { setEdit({ id: d.id, version: d.version, productId: d.productId, date: d.startsAtUtc.slice(0, 10), notes: d.notes }); setReview(null); setAccepted(false); setError(""); setMessage(""); }
  async function run(work: () => Promise<void>) {
    setBusy(true); setError(""); setMessage("");
    try { await work(); } catch (e) { setError(e instanceof Error ? e.message : "Unable to save application."); } finally { setBusy(false); }
  }
  return <section id={accountId ? undefined : "applications"} className="min-w-0 scroll-mt-6 rounded-2xl border border-slate-200 bg-white p-7" aria-label={accountId ? "Organisation membership application" : "Membership application"}>
    <h2 className="text-2xl font-semibold">Apply for membership</h2>
    <p className="mt-3 text-sm text-slate-600">Save an application, review its quote and terms, then submit it to your association. Submission does not take payment or activate membership.</p>
    {error && <div role="alert" className="mt-4 rounded-lg bg-red-50 p-4 text-red-900">{error} <button className="underline" disabled={busy} onClick={() => { setEdit(null); setReview(null); setDiscard(null); setError(""); setReady(false); setReload(n => n + 1); }}>Reload saved drafts</button></div>}
    {message && <p role="status" className="mt-4 rounded-lg bg-emerald-50 p-4">{message}</p>}
    {submittedId && <Link className="mt-3 inline-block font-semibold underline" href={`/portal/applications/${submittedId}${accountId ? `?organisation=${accountId}` : ""}` as Route}>View submitted application receipt</Link>}
    {!ready && !error && <p role="status" className="mt-4">Loading application options…</p>}
    {ready && <div className="mt-6 space-y-5">
      {!!drafts.length && <div><h3 className="font-semibold">Saved drafts</h3><div className="mt-3 space-y-3">{drafts.map(d => <div key={d.id} className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-slate-300 p-3"><button className="min-w-0 break-words text-left font-medium underline" disabled={busy} onClick={() => open(d)}>{products.find(p => p.id === d.productId)?.name ?? "Unavailable product"} · {d.startsAtUtc.slice(0, 10)}</button><button className="text-sm text-red-800 underline" disabled={busy} onClick={() => setDiscard(d)} aria-label={`Discard draft for ${products.find(p => p.id === d.productId)?.name ?? "unavailable product"}`}>Discard</button></div>)}</div></div>}
      {discard && <section aria-label="Confirm draft removal" className="rounded-xl border border-red-200 bg-red-50 p-5"><h3 className="font-semibold">Discard this saved draft?</h3><p className="mt-2 text-sm">Its notes and saved quote will be removed. Submitted applications are kept.</p><div className="mt-4 flex flex-wrap gap-4"><button className="rounded-full bg-red-800 px-5 py-3 font-semibold text-white disabled:opacity-50" disabled={busy} onClick={() => void run(async () => {
        await portal(`${scope}/drafts/${discard.id}`, "DELETE", { version: discard.version });
        setDrafts(values => values.filter(d => d.id !== discard.id));
        if (edit?.id === discard.id) { setEdit(null); setReview(null); setAccepted(false); }
        setDiscard(null); setMessage("Draft discarded. You can start a new application whenever you are ready.");
      })}>Discard draft</button><button className="underline" disabled={busy} onClick={() => setDiscard(null)}>Keep draft</button></div></section>}
      {!products.length ? <p>No membership products are currently open for online applications.</p> : <button className={button} disabled={busy} onClick={() => { setEdit({ id: crypto.randomUUID(), version: null, productId: products[0].id, date: new Date().toISOString().slice(0, 10), notes: "" }); setReview(null); setAccepted(false); setMessage(""); }}>New application</button>}
      {edit && <form className="max-w-2xl space-y-5" onSubmit={e => { e.preventDefault(); void run(async () => {
        const saved = await portal<Draft>(`${scope}/drafts/${edit.id}`, "PUT", { version: edit.version, productId: edit.productId, startsAtUtc: `${edit.date}T00:00:00Z`, notes: edit.notes });
        setEdit({ ...edit, version: saved.version }); setReview(saved); setAccepted(false);
        setDrafts(d => [saved, ...d.filter(x => x.id !== saved.id)]); setMessage("Draft saved. Review the quote and terms before submitting.");
      }); }}>
        <fieldset disabled={busy} className="space-y-5">
          <label className="block">Membership product<select className={input} value={edit.productId} onChange={e => change({ productId: e.target.value })} required>{products.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}</select></label>
          <label className="block">Requested start date<input type="date" className={input} value={edit.date} onChange={e => change({ date: e.target.value })} required /></label>
          <label className="block">Application notes<textarea className={input} rows={4} maxLength={4000} value={edit.notes} onChange={e => change({ notes: e.target.value })} /></label>
          <button className={button}>Save and review draft</button>
        </fieldset>
      </form>}
      {edit?.version && <ApplicationEvidence key={edit.id} path={`/api/v1/portal${scope}/drafts/${edit.id}/evidence`} version={edit.version} disabled={busy} onBusy={setBusy} onChanged={version => {
        setEdit(value => value ? { ...value, version } : value); setReview(value => value ? { ...value, version } : value);
        setDrafts(values => values.map(d => d.id === edit.id ? { ...d, version } : d)); setAccepted(false);
      }} />}
      {review && <section className="max-w-2xl space-y-4 rounded-xl bg-[#f7f7f2] p-6" aria-label="Review application">
        <h3 className="text-xl font-semibold">Review your application</h3>
        <p>Quoted total: <strong>{new Intl.NumberFormat("en-GB", { style: "currency", currency: review.currency }).format(review.total)}</strong></p>
        <h4 className="font-semibold">Membership terms</h4><p className="whitespace-pre-wrap text-sm leading-7">{review.terms}</p>
        <label className="flex items-start gap-3"><input type="checkbox" className="mt-1" checked={accepted} onChange={e => setAccepted(e.target.checked)} disabled={busy} />I accept these membership terms.</label>
        <button className={button} disabled={busy || !accepted} onClick={() => void run(async () => {
          const submitted = await portal<{ applicationId: string }>(`${scope}/drafts/${review.id}/submit`, "POST", { version: review.version, termsAccepted: accepted });
          setSubmittedId(submitted.applicationId); setDrafts(d => d.filter(x => x.id !== review.id)); setEdit(null); setReview(null); setAccepted(false); setMessage("Application submitted for review. You can track its status above."); onSubmitted();
        })}>Submit application</button>
      </section>}
    </div>}
  </section>;
}


