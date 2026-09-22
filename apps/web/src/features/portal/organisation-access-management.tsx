"use client";
import { useEffect, useState } from "react";
import { moduleApi } from "@/features/shared/module-api";

type Grant = { id: string; accountId: string; name: string; canSubmitApplications: boolean; isRevoked: boolean };
type Account = { id: string; name: string };
export function OrganisationAccessManagement({ accessId }: { accessId: string }) {
  const [grants, setGrants] = useState<Grant[] | null>(null);
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [search, setSearch] = useState("");
  const [accountId, setAccountId] = useState("");
  const [submit, setSubmit] = useState(false);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const [reload, setReload] = useState(0);
  const [revoking, setRevoking] = useState<Grant | null>(null);
  useEffect(() => {
    let active = true;
    moduleApi<Grant[]>("admin/portal-access", `/${accessId}/organisations`).then(value => { if (active) setGrants(value); }).catch(e => { if (active) setError(e.message); });
    return () => { active = false; };
  }, [accessId, reload]);
  async function run(work: () => Promise<void>) { setBusy(true); setError(""); try { await work(); } catch(e) { setError(e instanceof Error ? e.message : "Unable to update organisation access."); } finally { setBusy(false); } }
  return <section aria-label="Organisation permissions" className="mt-4 space-y-4 border-t border-slate-200 pt-4">
    <h5 className="font-semibold">Organisation permissions</h5>
    <p className="text-sm text-slate-600">Link the Contact to the Account in CRM first. Grant viewing access, with optional application permission.</p>
    {error && <p role="alert" className="text-sm text-red-800">{error} <button className="underline" onClick={() => { setError(""); setReload(n => n + 1); }}>Reload permissions</button></p>}
    {!grants ? <p role="status">Loading permissions…</p> : grants.map(g => <div key={g.id} className="rounded-lg bg-slate-50 p-3 text-sm"><p className="font-semibold">{g.name}</p><p>{g.isRevoked ? "Revoked" : g.canSubmitApplications ? "View and apply" : "View only"}</p>{!g.isRevoked && <button disabled={busy} className="text-red-800 underline" onClick={() => setRevoking(g)}>Revoke organisation permission</button>}</div>)}
    {revoking && <div className="rounded-lg bg-red-50 p-3 text-sm"><p>Revoke access to {revoking.name}?</p><button className="mr-4 underline" disabled={busy} onClick={() => void run(async () => { await moduleApi("admin/portal-access", `/${accessId}/organisations/${revoking.id}`, "DELETE"); setRevoking(null); setReload(n => n + 1); })}>Confirm organisation revocation</button><button disabled={busy} className="underline" onClick={() => setRevoking(null)}>Keep permission</button></div>}
    <form className="space-y-3" onSubmit={e => { e.preventDefault(); void run(async () => { const result = await moduleApi<{ items: Account[] }>("crm", `/records?kind=account&status=active&pageSize=20&search=${encodeURIComponent(search)}`); setAccounts(result.items); setAccountId(""); }); }}>
      <label className="block text-sm">Find an organisation<input className="mt-1 w-full rounded border border-slate-300 p-2" value={search} onChange={e => setSearch(e.target.value)} maxLength={160} /></label>
      <button disabled={busy} className="text-sm underline">Search organisations</button>
    </form>
    {!!accounts.length && <form className="space-y-3" onSubmit={e => { e.preventDefault(); void run(async () => { await moduleApi("admin/portal-access", `/${accessId}/organisations`, "POST", { accountId, canSubmitApplications: submit }); setAccountId(""); setSubmit(false); setReload(n => n + 1); }); }}>
      <div><label htmlFor={`grant-organisation-${accessId}`} className="block text-sm">Grant organisation</label><select id={`grant-organisation-${accessId}`} className="mt-1 w-full rounded border border-slate-300 p-2" value={accountId} onChange={e => setAccountId(e.target.value)} required><option value="">Choose an organisation</option>{accounts.map(a => <option key={a.id} value={a.id}>{a.name}</option>)}</select></div>
      <label className="flex items-start gap-2 text-sm"><input type="checkbox" checked={submit} onChange={e => setSubmit(e.target.checked)} />Allow membership applications</label>
      <button disabled={busy || !accountId} className="rounded bg-[#405bd8] px-3 py-2 text-sm font-semibold text-white disabled:opacity-50">Grant organisation permission</button>
    </form>}
  </section>;
}
