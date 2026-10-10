"use client";
import { useEffect, useState } from "react";
import { moduleApi } from "../shared/module-api";

export type MembershipAdministrativeDetails = { legacyMemberReference: string | null; membershipPackSent: boolean | null; membershipPackSentDate: string | null; remarks: string | null };
type Result = { version: string; details: MembershipAdministrativeDetails };
const input = "mt-2 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm disabled:bg-slate-50";

export function MembershipAdministration({ id, editable, busy, onChanged }: { id: string; editable: boolean; busy: boolean; onChanged: () => void }) {
  const [result, setResult] = useState<Result | null>(null);
  const [error, setError] = useState("");
  const [pending, setPending] = useState(false);
  const [revision, setRevision] = useState(0);
  useEffect(() => {
    let current = true;
    moduleApi<Result>("membership", `/members/${id}/administration`).then(value => { if (current) setResult(value); }).catch(e => { if (current) setError(e.message); });
    return () => { current = false; };
  }, [id, revision]);
  function change(fields: Partial<MembershipAdministrativeDetails>) { setResult(value => value && { ...value, details: { ...value.details, ...fields } }); }
  async function save() {
    if (!result) return;
    setPending(true); setError("");
    try { const saved = await moduleApi<Result>("membership", `/members/${id}/administration`, "PUT", result); setResult(saved); onChanged(); }
    catch (e) { setError(e instanceof Error ? e.message : "Unable to save administrative details."); }
    finally { setPending(false); }
  }
  const disabled = !editable || busy || pending;
  return <section aria-label="Membership administrative details" className="my-5 rounded-lg border border-slate-200 p-4">
    <h3 className="mb-3 font-semibold">Membership administrative details</h3>
    {error && <p role="alert" className="mb-3 text-sm text-red-700">{error} <button type="button" className="underline" disabled={pending} onClick={() => { setError(""); setResult(null); setRevision(n => n + 1); }}>Reload details</button></p>}
    {!result ? !error && <p role="status">Loading administrative details…</p> : <form className="space-y-4" onSubmit={e => { e.preventDefault(); void save(); }}>
      <fieldset disabled={disabled} className="grid gap-4 sm:grid-cols-2"><legend className="sr-only">Administrative fields</legend>
        <label className="text-sm">Legacy member reference<input className={input} maxLength={160} value={result.details.legacyMemberReference ?? ""} onChange={e => change({ legacyMemberReference: e.target.value })} /></label>
        <label className="text-sm">Membership pack sent<select className={input} value={result.details.membershipPackSent === null ? "unknown" : result.details.membershipPackSent ? "yes" : "no"} onChange={e => change({ membershipPackSent: e.target.value === "unknown" ? null : e.target.value === "yes", ...(e.target.value !== "yes" ? { membershipPackSentDate: null } : {}) })}><option value="unknown">Unknown</option><option value="yes">Yes</option><option value="no">No</option></select></label>
        <label className="text-sm">Membership pack sent date<input className={input} type="date" max={new Date().toISOString().slice(0, 10)} disabled={disabled || result.details.membershipPackSent !== true} value={result.details.membershipPackSentDate ?? ""} onChange={e => change({ membershipPackSentDate: e.target.value || null })} /></label>
        <label className="text-sm sm:col-span-2">Membership Remarks<textarea className={input} maxLength={4000} rows={3} value={result.details.remarks ?? ""} onChange={e => change({ remarks: e.target.value })} /></label>
      </fieldset>
      <p className="text-xs text-slate-500">The reference is retained separately from the Nexora membership ID. Pack tracking records your confirmation that it was sent.</p>
      {editable && <button className="rounded-lg bg-indigo-700 px-4 py-2 text-sm font-semibold text-white disabled:opacity-50" disabled={disabled}>Save administrative details</button>}
    </form>}
  </section>;
}
