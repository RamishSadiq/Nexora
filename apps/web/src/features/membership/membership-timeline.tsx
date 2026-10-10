"use client";
import { useEffect, useState } from "react";
import { moduleApi } from "../shared/module-api";

type Entry = { id: string; type: string; value: string | null; effectiveDate: string | null; ceasedDate: string | null; days: number | null; resignReason: string | null; description: string; recordedAtUtc: string; source: string; sequence: number | null };
type Page = { total: number; page: number; pageSize: number; items: Entry[] };
const labels: Record<string, string> = { "member-type": "Member type", category: "Category", "subscription-band": "Subscription band", status: "Status" };
const button = "rounded-lg border border-slate-300 px-3 py-2 text-sm disabled:opacity-50";

export function MembershipTimeline({ id }: { id: string }) {
  const [page, setPage] = useState(1); const [type, setType] = useState(""); const [revision, setRevision] = useState(0);
  const [result, setResult] = useState<Page | null>(null); const [error, setError] = useState("");
  useEffect(() => { let current = true; moduleApi<Page>("membership", `/members/${id}/timeline?page=${page}${type ? `&type=${type}` : ""}`).then(value => { if (current) setResult(value); }).catch(e => { if (current) setError(e.message); }); return () => { current = false; }; }, [id, page, type, revision]);
  function load(next: number) { setResult(null); setError(""); setPage(next); setRevision(n => n + 1); }
  return <section aria-label="Membership history timeline" className="my-5 rounded-lg border border-slate-200 p-4">
    <div className="flex flex-wrap items-center justify-between gap-3"><h3 className="font-semibold">Membership history timeline</h3><label className="text-sm">History type <select className="ml-2 rounded-lg border border-slate-300 px-3 py-2" value={type} onChange={e => { setType(e.target.value); load(1); }}><option value="">All types</option>{Object.entries(labels).map(([key, label]) => <option key={key} value={key}>{label}</option>)}</select></label><button type="button" className={button} onClick={() => load(page)}>Refresh history</button></div>
    <p className="my-3 text-xs text-slate-500">Ceased date is the next recorded effective change for that type, exclusive. Days are calculated only for closed, dated intervals. Unknown dates and open intervals stay unrecorded. Same-day corrections can have zero days.</p>
    {error ? <p role="alert" className="text-sm text-red-700">{error}</p> : !result ? <p role="status">Loading membership history…</p> : <>
      {result.total === 0 ? <p className="text-sm text-slate-500">No history recorded for this type.</p> : <div className="overflow-x-auto"><table className="w-full text-left text-sm"><caption className="sr-only">Effective membership classification and status history</caption><thead><tr>{["Type", "Value", "Effective date", "Ceased date (exclusive)", "Days", "Description"].map(label => <th key={label} scope="col" className="p-3">{label}</th>)}</tr></thead><tbody>{result.items.map(row => <tr key={row.id} className="border-t border-slate-200"><th scope="row" className="p-3">{labels[row.type]}</th><td className="p-3">{row.value ?? "Unassigned"}</td><td className="p-3">{row.effectiveDate ?? "Not recorded"}</td><td className="p-3">{row.ceasedDate ?? "Not recorded"}</td><td className="p-3">{row.days ?? "Not recorded"}</td><td className="p-3"><p>{row.description}</p>{row.resignReason && <p>Resignation: {row.resignReason}</p>}<p className="mt-1 text-xs text-slate-500">Recorded {new Date(row.recordedAtUtc).toLocaleString()}</p></td></tr>)}</tbody></table></div>}
      {result.total > 0 && <nav aria-label="Membership history pages" className="mt-3 flex flex-wrap items-center gap-3 text-sm"><button className={button} disabled={page === 1} onClick={() => load(page - 1)}>Previous</button><span>Page {page} of {Math.ceil(result.total / result.pageSize)} · {result.total} entries</span><button className={button} disabled={page >= 1000 || page * result.pageSize >= result.total} onClick={() => load(page + 1)}>Next</button></nav>}
    </>}
  </section>;
}
