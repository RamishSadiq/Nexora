"use client";
import { useEffect, useState } from "react";
import { moduleApi } from "@/features/shared/module-api";
import type { OrganisationRequest, RequestPage } from "./organisation-requests";

type Review = { request: OrganisationRequest; contactId: string; contactName: string | null; email: string; accessIsActive: boolean };
type Account = { id: string; name: string };
const input = "mt-2 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-slate-900";

export function OrganisationRequestReview({ onDecision }: { onDecision: () => void }) {
  const [data, setData] = useState<RequestPage<Review> | null>(null);
  const [page, setPage] = useState(1), [reload, setReload] = useState(0);
  const [selected, setSelected] = useState<Review | null>(null);
  const [search, setSearch] = useState(""), [accounts, setAccounts] = useState<Account[]>([]), [accountId, setAccountId] = useState("");
  const [canApply, setCanApply] = useState(false), [decisionMessage, setDecisionMessage] = useState("");
  const [busy, setBusy] = useState(false), [error, setError] = useState(""), [message, setMessage] = useState("");
  useEffect(() => {
    let active = true;
    moduleApi<RequestPage<Review>>("admin/portal-access", `/organisation-requests?page=${page}`).then(value => { if (active) setData(value); }).catch(e => { if (active) setError(e.message); });
    return () => { active = false; };
  }, [page, reload]);
  async function run(work: () => Promise<void>) { setBusy(true); setError(""); setMessage(""); try { await work(); } catch (e) { setError(e instanceof Error ? e.message : "Unable to review request."); } finally { setBusy(false); } }
  async function decide(decision: "approved" | "rejected") {
    if (!selected) return;
    await moduleApi("admin/portal-access", `/organisation-requests/${selected.request.id}/decision`, "POST", { version: selected.request.version, decision, accountId: decision === "approved" ? accountId : null, canSubmitApplications: decision === "approved" && canApply, decisionMessage });
    setSelected(null); setReload(n => n + 1); onDecision(); setMessage(`Company access request ${decision}.`);
  }
  return <section aria-label="Review company access requests" className="mt-8 space-y-4 border-t border-slate-200 pt-6">
    <div className="flex flex-wrap items-center justify-between gap-3"><h3 className="text-xl font-semibold">Company access requests</h3><button className="text-sm underline" disabled={busy} onClick={() => { setSelected(null); setError(""); setReload(n => n + 1); }}>Refresh company requests</button></div>
    <p className="text-sm text-slate-600">Verify the member’s authority, then match an active CRM Account. Approval creates the Contact–Account relationship and explicit portal permission. Choose application permission separately.</p>
    {error && <p role="alert" className="rounded-lg bg-red-50 p-3 text-red-900">{error}</p>}{message && <p role="status" className="rounded-lg bg-emerald-50 p-3">{message}</p>}
    {!data ? <p role="status">Loading company requests…</p> : <>{!data.items.length && <p>No company access requests.</p>}{data.items.map(row => <article key={row.request.id} className="space-y-2 rounded-xl border border-slate-200 p-4">
      <h4 className="break-words font-semibold">{row.request.organisationName}</h4><p className="break-words text-sm">{row.contactName ?? "Inactive Contact"} · {row.email}</p><p className="text-sm capitalize">{row.request.status === "pending" ? "Awaiting review" : row.request.status}</p>
      {row.request.jobTitle && <p className="text-sm">Job title supplied: {row.request.jobTitle}</p>}<p className="whitespace-pre-wrap break-words text-sm">{row.request.message}</p>
      {row.request.decisionMessage && <p className="whitespace-pre-wrap break-words text-sm">Member-facing decision: {row.request.decisionMessage}</p>}
      {row.request.status === "pending" && <button disabled={busy} className="text-sm font-semibold underline disabled:opacity-40" onClick={() => { setSelected(row); setSearch(row.request.organisationName); setAccounts([]); setAccountId(""); setCanApply(false); setDecisionMessage(""); setError(""); }}>Review request<span className="sr-only"> for {row.request.organisationName} from {row.email}</span></button>}
    </article>)}<div className="flex items-center gap-4 text-sm"><button className="underline disabled:opacity-40" disabled={busy || page === 1} onClick={() => { setSelected(null); setData(null); setPage(n => n - 1); }}>Previous review page</button><span>Page {page} · {data.totalCount} requests</span><button className="underline disabled:opacity-40" disabled={busy || page * data.pageSize >= data.totalCount} onClick={() => { setSelected(null); setData(null); setPage(n => n + 1); }}>Next review page</button></div></>}
    {selected && <div role="group" aria-label="Company access decision" className="space-y-4 rounded-xl bg-slate-50 p-5">
      <h4 className="font-semibold">Review {selected.request.organisationName} for {selected.contactName}</h4>
      {(!selected.accessIsActive || !selected.contactName) && <p className="text-sm text-red-800">This member’s account or Contact is inactive, or their portal access was revoked. You can reject this request; approval requires active member access.</p>}
      <form className="space-y-3" onSubmit={e => { e.preventDefault(); void run(async () => { const result = await moduleApi<{ items: Account[] }>("crm", `/records?kind=account&status=active&pageSize=20&search=${encodeURIComponent(search)}`); setAccounts(result.items); setAccountId(""); if (!result.items.length) setMessage("No active matching Account. Check the company name or create the Account in CRM first."); }); }}>
        <label className="block">Find matching CRM company<input className={input} maxLength={160} value={search} onChange={e => setSearch(e.target.value)} /></label><button disabled={busy} className="text-sm underline">Search matching companies</button>
      </form>
      <div><label className="block" htmlFor={`review-company-${selected.request.id}`}>Approve access to</label><select id={`review-company-${selected.request.id}`} className={input} value={accountId} onChange={e => setAccountId(e.target.value)} disabled={busy}><option value="">Select a matching active Account</option>{accounts.map(account => <option key={account.id} value={account.id}>{account.name} · {account.id.slice(0, 8)}</option>)}</select></div>
      <label className="block"><input type="checkbox" checked={canApply} disabled={busy} onChange={e => setCanApply(e.target.checked)} /> Allow company membership applications</label>
      <label className="block">Message to member<textarea className={input} rows={3} maxLength={1000} value={decisionMessage} disabled={busy} onChange={e => setDecisionMessage(e.target.value)} /></label><p className="text-sm text-slate-600">This message is visible to the member. Keep private review notes in CRM.</p>
      <div className="flex flex-wrap gap-4"><button className="rounded-lg bg-[#405bd8] px-4 py-2 font-semibold text-white disabled:opacity-50" disabled={busy || !accountId || !selected.accessIsActive || !selected.contactName} onClick={() => void run(() => decide("approved"))}>Approve company access</button><button className="rounded-lg border border-red-300 px-4 py-2 text-red-800" disabled={busy} onClick={() => void run(() => decide("rejected"))}>Reject request</button><button className="underline" disabled={busy} onClick={() => setSelected(null)}>Close review</button></div>
    </div>}
  </section>;
}
