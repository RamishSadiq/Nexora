"use client";

import { useEffect, useState } from "react";
import { OrganisationAccessManagement } from "./organisation-access-management";
import { OrganisationRequestReview } from "./organisation-request-review";
import { moduleApi } from "@/features/shared/module-api";

type Grant = { id: string; contactId: string; contactName: string | null; email: string; displayName: string; isRevoked: boolean; userIsActive: boolean; contactIsActive: boolean };
type Access = { items: Grant[]; totalCount: number; pageSize: number; localProvisioningEnabled: boolean };
type Contact = { id: string; name: string };
const input = "mt-2 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-slate-900";

export function PortalAccessManagement({ users }: { users: { id: string; displayName: string; email: string; isActive: boolean }[] }) {
  const [data, setData] = useState<Access | null>(null);
  const [contacts, setContacts] = useState<Contact[]>([]);
  const [search, setSearch] = useState("");
  const [contactId, setContactId] = useState("");
  const [mode, setMode] = useState("existing");
  const [page, setPage] = useState(1);
  const [reload, setReload] = useState(0);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [revoking, setRevoking] = useState<Grant | null>(null);
  useEffect(() => {
    let active = true;
    moduleApi<Access>("admin/portal-access", `?page=${page}`).then(value => { if (active) setData(value); }).catch(e => { if (active) setError(e.message); });
    return () => { active = false; };
  }, [page, reload]);
  async function run(work: () => Promise<void>) {
    setBusy(true); setError(""); setMessage("");
    try { await work(); } catch (e) { setError(e instanceof Error ? e.message : "Unable to update member access."); } finally { setBusy(false); }
  }
  return <section aria-label="Member portal access" className="rounded-2xl border border-[#e5e9ef] bg-white p-5 shadow-sm">
    <h2 className="text-xl font-semibold text-[#263244]">Member portal access</h2>
    <p className="mt-2 text-sm text-slate-600">Explicitly link a login to an active Contact. Access does not grant staff permissions or access to an organisation.</p>
    {error && <p role="alert" className="mt-4 rounded-lg bg-red-50 p-3 text-red-900">{error} <button className="underline" onClick={() => { setError(""); setReload(n => n + 1); }}>Refresh access list</button></p>}
    {message && <p role="status" className="mt-4 rounded-lg bg-emerald-50 p-3 text-emerald-900">{message}</p>}
    {!data ? <p role="status" className="mt-4">Loading member access…</p> : <>
      <div className="mt-6 grid gap-6 lg:grid-cols-2">
        <div>
          <h3 className="font-semibold">Grant member access</h3>
          <form className="mt-3 flex flex-wrap items-end gap-3" onSubmit={e => { e.preventDefault(); void run(async () => {
            const result = await moduleApi<{ items: Contact[] }>("crm", `/records?kind=contact&status=active&pageSize=20&search=${encodeURIComponent(search)}`);
            setContacts(result.items); setContactId(""); if (!result.items.length) setMessage("No matching active contacts. Create or activate the Contact in CRM first.");
          }); }}>
            <label className="min-w-0 flex-1 text-sm">Find a Contact<input className={input} value={search} onChange={e => setSearch(e.target.value)} maxLength={160} /></label>
            <button disabled={busy} className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-semibold">Search contacts</button>
          </form>
          <form className="mt-4 space-y-4" onSubmit={e => { e.preventDefault(); const form = e.currentTarget; const fields = new FormData(form); void run(async () => {
            if (mode === "local") await moduleApi("admin/portal-access", "/local-member", "POST", { contactId, email: fields.get("email"), password: fields.get("password") });
            else await moduleApi("admin/portal-access", "", "POST", { contactId, userId: fields.get("userId") });
            form.reset(); setContactId(""); setPage(1); setReload(n => n + 1); setMessage("Member access granted. The account can now sign in to the local portal.");
          }); }}>
            <fieldset disabled={busy} className="space-y-4">
              <div><label htmlFor="portal-member-contact" className="block text-sm">Member Contact</label><select id="portal-member-contact" className={input} value={contactId} onChange={e => setContactId(e.target.value)} required><option value="">Select a search result</option>{contacts.map(c => <option key={c.id} value={c.id}>{c.name} · {c.id.slice(0, 8)}</option>)}</select></div>
              <div><label htmlFor="portal-account-option" className="block text-sm">Account option</label><select id="portal-account-option" className={input} value={mode} onChange={e => setMode(e.target.value)}><option value="existing">Link an existing account</option>{data.localProvisioningEnabled && <option value="local">Create a local member account</option>}</select></div>
              {mode === "local" ? <><p className="text-sm text-slate-600">Local development only. No invitation email is sent. Use a test account and a password of at least 12 characters with uppercase, lowercase, a number and a symbol.</p><label className="block text-sm">Member login email<input className={input} name="email" type="email" autoComplete="off" maxLength={254} required /></label><label className="block text-sm">Temporary member password<input className={input} name="password" type="password" autoComplete="new-password" minLength={12} required /></label></> : <div><label htmlFor="portal-existing-login" className="block text-sm">Existing login</label><select id="portal-existing-login" name="userId" className={input} required><option value="">Choose an active account</option>{users.filter(u => u.isActive).map(u => <option key={u.id} value={u.id}>{u.displayName} · {u.email}</option>)}</select></div>}
              <button className="rounded-lg bg-[#405bd8] px-5 py-2.5 font-semibold text-white disabled:opacity-50" disabled={!contactId}>Grant portal access</button>
            </fieldset>
          </form>
        </div>
        <div><h3 className="font-semibold">Access records</h3>
          <div className="mt-3 space-y-3">{data.items.length ? data.items.map(g => <article key={g.id} className="rounded-xl border border-slate-200 p-4"><h4 className="break-words font-semibold">{g.contactName ?? "Inactive or unavailable Contact"}</h4><p className="mt-1 break-all text-sm text-slate-600">{g.email}</p><p className="mt-2 text-sm">{g.isRevoked ? "Revoked" : !g.userIsActive || !g.contactIsActive ? "Blocked by inactive account or Contact" : "Active access"}</p>{!g.isRevoked && <button className="mt-3 text-sm font-semibold text-red-800 underline" disabled={busy} onClick={() => setRevoking(g)}>Revoke member access<span className="sr-only"> for {g.email}</span></button>}{!g.isRevoked && g.userIsActive && g.contactIsActive && <OrganisationAccessManagement key={`${g.id}/${reload}`} accessId={g.id} />}</article>) : <p className="text-sm text-slate-600">No access records on this page.</p>}</div>
          <div className="mt-4 flex flex-wrap items-center gap-4 text-sm"><button disabled={busy || page === 1} className="underline disabled:opacity-40" onClick={() => { setData(null); setPage(p => p - 1); }}>Previous access page</button><span>Page {page} · {data.totalCount} records</span><button disabled={busy || page * data.pageSize >= data.totalCount} className="underline disabled:opacity-40" onClick={() => { setData(null); setPage(p => p + 1); }}>Next access page</button></div>
        </div>
      </div>
      {revoking && <div role="group" aria-label="Confirm member revocation" className="mt-5 rounded-xl bg-red-50 p-4"><p className="break-words text-sm text-red-900">Revoke access for {revoking.email}? Their existing portal session will stop working on its next request.</p><div className="mt-3 flex gap-4"><button disabled={busy} className="rounded-lg bg-red-800 px-4 py-2 font-semibold text-white" onClick={() => void run(async () => { await moduleApi("admin/portal-access", `/${revoking.id}`, "DELETE"); setRevoking(null); setReload(n => n + 1); setMessage("Member access revoked."); })}>Confirm revocation</button><button disabled={busy} className="underline" onClick={() => setRevoking(null)}>Keep access</button></div></div>}
    </>}
    <OrganisationRequestReview onDecision={() => setReload(n => n + 1)} />
  </section>;
}

