"use client";
import { useEffect, useState } from "react";
import Link from "next/link";
import type { Route } from "next";
import { portal } from "./api";
import { ApplicationWorkspace } from "./application-workspace";
import { portalDate } from "./application-receipt";

type Organisation = { id: string; name: string; canSubmitApplications: boolean };
type Overview = { memberships: { id: string; product: string; status: string; startsAtUtc: string; endsAtUtc: string }[]; applications: { id: string; product: string; status: string }[] };
export function OrganisationWorkspace() {
  const [organisations, setOrganisations] = useState<Organisation[] | null>(null);
  const [selected, setSelected] = useState("");
  const [error, setError] = useState("");
  const [reload, setReload] = useState(0);
  useEffect(() => {
    let active = true;
    portal<Organisation[]>("/organisations").then(value => { if (active) setOrganisations(value); }).catch(e => { if (active) setError(e.message); });
    return () => { active = false; };
  }, [reload]);
  const organisation = organisations?.find(o => o.id === selected);
  return <section id="organisations" className="min-w-0 space-y-5 rounded-2xl border border-slate-200 bg-white p-7" aria-label="My organisations">
    <div className="flex flex-wrap items-center justify-between gap-3"><h2 className="text-2xl font-semibold">My organisations</h2><button className="text-sm underline" onClick={() => { setSelected(""); setOrganisations(null); setError(""); setReload(n => n + 1); }}>Refresh organisations</button></div>
    <p className="text-sm text-slate-600">Company memberships are separate from your personal membership. Your association controls representative access.</p>
    {error ? <p role="alert" className="text-red-800">{error}</p> : !organisations ? <p role="status">Loading organisations…</p> : !organisations.length ? <p>No organisation access has been granted.</p> : <>
      <div><label htmlFor="portal-organisation" className="block">Organisation</label><select id="portal-organisation" className="mt-2 w-full rounded-lg border border-slate-300 p-3" value={selected} onChange={e => setSelected(e.target.value)}><option value="">Choose an organisation</option>{organisations.map(o => <option key={o.id} value={o.id}>{o.name}</option>)}</select></div>
      {organisation && <OrganisationDetails key={organisation.id} organisation={organisation} />}
    </>}
  </section>;
}
function OrganisationDetails({ organisation }: { organisation: Organisation }) {
  const [overview, setOverview] = useState<Overview | null>(null);
  const [error, setError] = useState("");
  const [reload, setReload] = useState(0);
  useEffect(() => {
    let active = true;
    portal<Overview>(`/organisations/${organisation.id}/memberships`).then(value => { if (active) setOverview(value); }).catch(e => { if (active) setError(e.message); });
    return () => { active = false; };
  }, [organisation.id, reload]);
  return <div className="space-y-5">
    <h3 className="text-xl font-semibold">{organisation.name}</h3>
    <button className="text-sm underline" onClick={() => { setOverview(null); setError(""); setReload(n => n + 1); }}>Refresh company status</button>
    {error ? <p role="alert" className="text-red-800">{error}</p> : !overview ? <p role="status">Loading company status…</p> : <>
      <h4 className="font-semibold">Company memberships</h4>
      {!overview.memberships.length && <p>No company memberships.</p>}
      {overview.memberships.map(m => <article key={m.id} className="rounded-lg bg-[#eef3df] p-4"><p className="font-semibold">{m.product}</p><p className="capitalize">{m.status}</p><p className="text-sm">{portalDate(m.startsAtUtc)} – {portalDate(m.endsAtUtc)}</p></article>)}
      <h4 className="font-semibold">Company applications</h4>
      {!overview.applications.length && <p>No company applications.</p>}
      {overview.applications.map(a => <article key={a.id} className="rounded-lg border border-slate-200 p-4"><p className="font-semibold">{a.product}</p><p className="capitalize">{a.status === "submitted" ? "Awaiting review" : a.status}</p><Link className="text-sm underline" href={`/portal/applications/${a.id}?organisation=${organisation.id}` as Route}>View company application receipt</Link></article>)}
      {organisation.canSubmitApplications ? <ApplicationWorkspace accountId={organisation.id} onSubmitted={() => setReload(n => n + 1)} /> : <p className="text-sm">You have viewing access. Contact your association to request application permission.</p>}
    </>}
  </div>;
}
