"use client";
import { useEffect, useState } from "react";
import Link from "next/link";
import type { Route } from "next";
import { ArrowUpRight, BadgeCheck, FileText, UserRound, Building2, Bell } from "lucide-react";
import { portal } from "./api";
import { portalDate } from "./application-receipt";

type Summary = {
  memberships: { id: string; product: string; status: string; endsAtUtc: string }[];
  applications: { id: string; product: string; status: string; createdAtUtc: string }[];
};
const card = "rounded-2xl border border-slate-200 bg-white p-6";
export function MemberDashboard({ sections = ["summary", "membership", "applications", "links"] }: { sections?: string[] }) {
  const [data, setData] = useState<Summary | null>(null);
  const [error, setError] = useState("");
  const [attempt, setAttempt] = useState(0);
  useEffect(() => {
    let active = true;
    portal<Summary>("/memberships").then(value => { if (active) setData(value); }).catch(e => { if (active) setError(e.message); });
    return () => { active = false; };
  }, [attempt]);
  const active = data?.memberships.filter(m => m.status === "active") ?? [];
  const pending = data?.applications.filter(a => a.status === "submitted") ?? [];
  const latest = data ? [...data.applications].sort((a, b) => b.createdAtUtc.localeCompare(a.createdAtUtc)).slice(0, 3) : [];
  return <div className="grid items-start gap-6 md:grid-cols-2">
    {error ? <div role="alert" className={card}><p>{error}</p><button className="mt-3 underline" onClick={() => { setError(""); setAttempt(n => n + 1); }}>Retry dashboard</button></div> : !data ? <p role="status">Loading your dashboard…</p> : <>
      {sections.includes("summary") && <div style={{ order: sections.indexOf("summary") }} className="grid gap-4 sm:grid-cols-3 md:col-span-2">
        {[
          { label: "Active memberships", count: active.length, detail: "Your personal memberships", href: "/portal/memberships", icon: BadgeCheck },
          { label: "Awaiting review", count: pending.length, detail: "Applications with your association", href: "/portal/applications", icon: FileText },
          { label: "Submitted applications", count: data.applications.length, detail: "Track decisions and view receipts", href: "/portal/applications", icon: FileText },
        ].map(item => <Link key={item.label} href={item.href as Route} className={`portal-summary-card ${card} transition-colors hover:border-[var(--portal-brand,#183a31)]`}><div className="flex items-center justify-between"><item.icon aria-hidden="true" size={22} /><ArrowUpRight aria-hidden="true" size={18} /></div><p className="mt-5 text-4xl font-semibold">{item.count}</p><h2 className="mt-2 font-semibold">{item.label}</h2><p className="mt-2 text-sm text-slate-500">{item.detail}</p></Link>)}
      </div>}

        {sections.includes("membership") && <section style={{ order: sections.indexOf("membership") }} className="portal-tinted rounded-2xl bg-[#ddebab] p-7"><h2 className="text-xl font-semibold">Your membership</h2>{active.length ? <ul className="mt-5 space-y-4">{active.map(m => <li key={m.id}><span className="portal-subcard rounded-full bg-white/70 px-3 py-1 text-xs font-semibold">Active</span><h3 className="mt-3 break-words text-lg font-semibold">{m.product}</h3><p className="mt-2 text-sm">Current term ends {portalDate(m.endsAtUtc)}</p></li>)}</ul> : <><p className="mt-4 leading-7">You have no active personal membership yet.</p><p className="mt-2 text-sm">Explore the available memberships and start an application when you are ready.</p></>}<Link href={(active.length ? "/portal/memberships" : "/portal/applications") as Route} className="portal-action mt-6 inline-flex rounded-full bg-[var(--portal-brand,#183a31)] px-5 py-3 text-sm font-semibold text-white">{active.length ? "View membership details" : "Explore memberships"}</Link></section>}
        {sections.includes("applications") && <section style={{ order: sections.indexOf("applications") }} className={card}><div className="flex flex-wrap items-center justify-between gap-3"><h2 className="text-xl font-semibold">Recent applications</h2><Link href={"/portal/applications" as Route} className="text-sm underline">View all applications</Link></div>{latest.length ? <ul className="mt-4 divide-y divide-slate-100">{latest.map(a => <li key={a.id} className="py-4"><Link href={`/portal/applications/${a.id}` as Route} className="break-words font-semibold underline decoration-slate-300 underline-offset-4">{a.product}</Link><p className="mt-2 text-sm capitalize">{a.status === "submitted" ? "Awaiting review" : a.status}</p><p className="mt-1 text-xs text-slate-500">Submitted {portalDate(a.createdAtUtc)}</p></li>)}</ul> : <p className="mt-5 text-sm leading-7 text-slate-600">No applications submitted yet. Saved drafts and new applications are available in My applications.</p>}</section>}
    </>}
    {sections.includes("links") && <section style={{ order: sections.indexOf("links") }} className={`${card} md:col-span-2`}><h2 className="text-xl font-semibold">Quick links</h2><div className="mt-5 grid gap-4 sm:grid-cols-3">{[
      { label: "Update your profile", detail: "Keep your contact details current", href: "/portal/profile", icon: UserRound },
      { label: "Manage organisations", detail: "View your representative access", href: "/portal/organisations", icon: Building2 },
      { label: "Read notifications", detail: "Catch up on your association updates", href: "/portal/notifications", icon: Bell },
    ].map(item => <Link key={item.label} href={item.href as Route} className="portal-subcard rounded-xl bg-[#f7f7f2] p-4 hover:bg-[#ddebab]/40"><item.icon aria-hidden="true" size={20} /><h3 className="mt-3 font-semibold">{item.label}</h3><p className="mt-2 text-sm text-slate-600">{item.detail}</p></Link>)}</div></section>}
  </div>;
}
