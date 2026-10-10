"use client";
import { useEffect, useState } from "react";
import Link from "next/link";
import { moduleApi } from "../shared/module-api";

export type Classification = { definitionId: string | null; name: string | null; effectiveOn: string | null };
export type Classifications = { memberType: Classification; category: Classification; subscriptionBand: Classification };
type Change = { id: string; kind: string; nameSnapshot: string | null; effectiveOn: string; sequence: number; reason: string; createdAtUtc: string };
type Result = { version: string; current: Classifications; total: number; page: number; pageSize: number; items: Change[] };
type Definition = { id: string; name: string };
const input = "mt-2 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm disabled:bg-slate-50";
const button = "rounded-lg border border-slate-300 px-3 py-2 text-sm disabled:opacity-50";
export const classificationLabel = (value: Classification) => value.name ?? (value.effectiveOn ? "Unassigned" : "Not recorded");

export function MembershipClassifications({ id, editable, busy, onChanged }: { id: string; editable: boolean; busy: boolean; onChanged: () => void }) {
  const [result, setResult] = useState<Result | null>(null);
  const [definitions, setDefinitions] = useState<{ type: Definition[]; category: Definition[]; band: Definition[] }>({ type: [], category: [], band: [] });
  const [page, setPage] = useState(1);
  const [revision, setRevision] = useState(0);
  const [kind, setKind] = useState("category");
  const [choice, setChoice] = useState("");
  const [error, setError] = useState("");
  const [pending, setPending] = useState(false);
  useEffect(() => {
    let current = true;
    Promise.all([moduleApi<Result>("membership", `/members/${id}/classifications?page=${page}`), moduleApi<Definition[]>("membership", "/setup/member-category"), moduleApi<Definition[]>("membership", "/setup/member-subscription-band"), moduleApi<Definition[]>("membership", "/setup/member-type")]).then(([value, category, band, type]) => { if (current) { setResult(value); setDefinitions({ category, band, type }); } }).catch(e => { if (current) setError(e.message); });
    return () => { current = false; };
  }, [id, page, revision]);
  function reload(next: number) { setError(""); setResult(null); setPage(next); setRevision(n => n + 1); }
  const latest = result?.current[kind === "member-type" ? "memberType" : kind === "category" ? "category" : "subscriptionBand"];
  return <section aria-label="Membership classification history" className="my-5 rounded-lg border border-slate-200 p-4">
    <h3 className="mb-3 font-semibold">Membership type, category and subscription band</h3>
    {error && <p role="alert" className="mb-3 text-sm text-red-700">{error} <button className={button} disabled={pending} onClick={() => reload(page)}>Reload classifications</button></p>}
    {!result ? !error && <p role="status">Loading classifications…</p> : <>
      <dl className="mb-4 grid gap-3 text-sm sm:grid-cols-2">{([["memberType", "Member type"], ["category", "Member category"], ["subscriptionBand", "Subscription band"]] as const).map(([key, label]) => <div key={key}><dt className="text-slate-500">{label}</dt><dd>{classificationLabel(result.current[key])}{result.current[key].effectiveOn && <span className="block text-xs text-slate-500">Effective {result.current[key].effectiveOn}</span>}</dd></div>)}</dl>
      {editable && <form className="mb-5 space-y-3" onSubmit={e => {
        e.preventDefault(); const form = e.currentTarget; const data = new FormData(form); setPending(true); setError("");
        void moduleApi("membership", `/members/${id}/classifications/${kind}`, "POST", { version: result.version, definitionId: choice || null, effectiveOn: data.get("effectiveOn"), reason: data.get("reason") }).then(() => { form.reset(); setChoice(""); reload(1); onChanged(); }).catch(e => setError(e.message)).finally(() => setPending(false));
      }}><fieldset disabled={busy || pending} className="grid gap-3 sm:grid-cols-2"><legend className="sr-only">Record a classification change</legend>
        <label className="text-sm">Classification<select className={input} value={kind} onChange={e => { setKind(e.target.value); setChoice(""); }}><option value="member-type">Member type</option><option value="category">Member category</option><option value="subscription-band">Subscription band</option></select></label>
        <label className="text-sm">New value<select className={input} value={choice} onChange={e => setChoice(e.target.value)}><option value="">Unassigned (clear)</option>{definitions[kind === "member-type" ? "type" : kind === "category" ? "category" : "band"].map(d => <option key={d.id} value={d.id}>{d.name}</option>)}</select></label>
        <label className="text-sm">Effective date<input className={input} name="effectiveOn" type="date" min={latest?.effectiveOn ?? undefined} max={new Date().toISOString().slice(0, 10)} defaultValue={new Date().toISOString().slice(0, 10)} required /></label>
        <label className="text-sm">Change reason<input className={input} name="reason" maxLength={500} required /></label>
      </fieldset><p className="text-xs text-slate-500">Changes preserve earlier assignments. A same-day correction becomes the current value. Types, categories and bands do not change the recorded fee.</p><button className={button} disabled={busy || pending}>Record classification change</button><Link className="ml-3 text-sm text-indigo-700 underline" href="/membership/setup/member-type">Manage member types</Link><Link className="ml-3 text-sm text-indigo-700 underline" href="/membership/setup/member-category">Manage categories</Link><Link className="ml-3 text-sm text-indigo-700 underline" href="/membership/setup/member-subscription-band">Manage bands</Link></form>}
      <h4 className="mb-2 text-sm font-semibold">Assignment history</h4>
      {result.total === 0 && <p className="text-sm text-slate-500">No classification changes recorded.</p>}
      <ul className="space-y-2">{result.items.map(change => <li key={change.id} className="rounded-lg bg-slate-50 p-3 text-sm"><p>{change.kind === "member-type" ? "Member type" : change.kind === "category" ? "Member category" : "Subscription band"}: {change.nameSnapshot ?? "Unassigned"} · Effective {change.effectiveOn}</p><p className="mt-1 break-words text-slate-600">{change.reason}</p><p className="mt-1 text-xs text-slate-500">Recorded {new Date(change.createdAtUtc).toLocaleString("en-GB")}</p></li>)}</ul>
      {result.total > 0 && <nav aria-label="Classification history pages" className="mt-3 flex flex-wrap items-center gap-3 text-sm"><button className={button} disabled={pending || page <= 1} onClick={() => reload(page - 1)}>Previous history</button><span>Page {page} of {Math.max(1, Math.ceil(result.total / result.pageSize))}</span><button className={button} disabled={pending || page * result.pageSize >= result.total} onClick={() => reload(page + 1)}>Next history</button></nav>}
    </>}
  </section>;
}
