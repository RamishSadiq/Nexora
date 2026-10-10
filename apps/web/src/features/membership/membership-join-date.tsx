"use client";
import { useEffect, useState } from "react";
import { moduleApi } from "../shared/module-api";

type Result = { version: string; originalJoinDate: string | null };
export function MembershipJoinDate({ id, editable, busy, onChanged }: { id: string; editable: boolean; busy: boolean; onChanged: () => void }) {
  const [result, setResult] = useState<Result | null>(null);
  const [reason, setReason] = useState("");
  const [error, setError] = useState("");
  const [pending, setPending] = useState(false);
  const [revision, setRevision] = useState(0);
  useEffect(() => {
    let current = true;
    moduleApi<Result>("membership", `/members/${id}/join-date`).then(value => { if (current) setResult(value); }).catch(e => { if (current) setError(e.message); });
    return () => { current = false; };
  }, [id, revision]);
  async function save() {
    if (!result) return;
    setPending(true); setError("");
    try { setResult(await moduleApi<Result>("membership", `/members/${id}/join-date`, "PUT", { ...result, reason })); setReason(""); onChanged(); }
    catch (e) { setError(e instanceof Error ? e.message : "Unable to save original join date."); }
    finally { setPending(false); }
  }
  const disabled = !editable || busy || pending;
  const input = "mt-2 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm disabled:bg-slate-50";
  return <section aria-label="Original join date" className="my-5 rounded-lg border border-slate-200 p-4">
    <h3 className="mb-3 font-semibold">Original join date</h3>
    <p className="mb-3 text-xs text-slate-500">The original joining date for this membership, on or before the subscription start. Leave blank when unknown. Corrections preserve the subscription term and status.</p>
    {error && <p role="alert" className="mb-3 text-sm text-red-700">{error} <button type="button" className="underline" disabled={pending} onClick={() => { setError(""); setResult(null); setReason(""); setRevision(n => n + 1); }}>Reload join date</button></p>}
    {!result ? !error && <p role="status">Loading join date…</p> : editable ? <form className="space-y-3" onSubmit={e => { e.preventDefault(); void save(); }}>
      <fieldset disabled={disabled} className="grid gap-3 sm:grid-cols-2"><legend className="sr-only">Join date correction</legend>
        <label className="text-sm">Original join date<input className={input} type="date" max={new Date().toISOString().slice(0, 10)} value={result.originalJoinDate ?? ""} onChange={e => setResult({ ...result, originalJoinDate: e.target.value || null })} /></label>
        <label className="text-sm">Reason for recording, correcting or clearing<input className={input} required maxLength={500} value={reason} onChange={e => setReason(e.target.value)} /></label>
      </fieldset>
      <button className="rounded-lg bg-indigo-700 px-4 py-2 text-sm font-semibold text-white disabled:opacity-50" disabled={disabled}>Save original join date</button>
    </form> : <p className="text-sm">{result.originalJoinDate ?? "Not recorded"}</p>}
  </section>;
}
