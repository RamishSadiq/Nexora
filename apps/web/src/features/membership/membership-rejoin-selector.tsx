"use client";
import { useEffect, useState } from "react";
import { moduleApi } from "../shared/module-api";

type Candidate = { id: string; name: string; status: string; endsAtUtc: string };
type Result = { items: Candidate[]; total: number; page: number; pageSize: number };
export function MembershipRejoinSelector({ contactId, accountId, disabled }: { contactId: string; accountId: string; disabled: boolean }) {
  const [page, setPage] = useState(1);
  const [selected, setSelected] = useState("");
  const [result, setResult] = useState<Result | null>(null);
  const [error, setError] = useState("");
  useEffect(() => {
    let current = true;
    if (contactId) moduleApi<Result>("membership", `/rejoin-candidates?contactId=${encodeURIComponent(contactId)}${accountId ? `&accountId=${encodeURIComponent(accountId)}` : ""}&page=${page}`)
      .then(value => { if (current) { setResult(value); setError(""); } }).catch(e => { if (current) setError(e.message); });
    return () => { current = false; };
  }, [contactId, accountId, page]);
  function move(next: number) { setSelected(""); setResult(null); setPage(next); }
  return <div className="sm:col-span-2 rounded-lg border border-slate-200 p-3">
    <label className="text-sm">Previous membership for rejoin<select name="previousMembershipId" className="mt-2 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm" disabled={disabled || !result || !!error} value={selected} onChange={e => setSelected(e.target.value)}>
      <option value="">New application without a rejoin link</option>
      {result?.items.map(item => <option key={item.id} value={item.id}>{item.name} · {item.status} · term end {new Date(item.endsAtUtc).toLocaleDateString()} · {item.id}</option>)}
    </select></label>
    <p className="mt-2 text-xs text-slate-500">Select an inactive previous membership to record a distinct rejoin upon approval. The previous membership stays unchanged. Eligibility is checked again at approval.</p>
    {!contactId && <p className="mt-2 text-xs text-slate-500">Choose the main Contact to load previous memberships.</p>}
    {error && <p role="alert" className="mt-2 text-sm text-red-700">{error}</p>}
    {result && result.total > 25 && <nav aria-label="Previous membership pages" className="mt-2 flex gap-3 text-sm"><button type="button" disabled={disabled || page <= 1} onClick={() => move(page - 1)}>Previous</button><span>Page {page} of {Math.ceil(result.total / result.pageSize)}</span><button type="button" disabled={disabled || page * 25 >= result.total} onClick={() => move(page + 1)}>Next</button></nav>}
  </div>;
}
