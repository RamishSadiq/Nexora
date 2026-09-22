"use client";
import { useEffect, useRef, useState } from "react";
import { portal } from "./api";
import { ApplicationWorkspace } from "./application-workspace";
import Link from "next/link";
import type { Route } from "next";
import { ApplicationReceipt } from "./application-receipt";
import { OrganisationWorkspace } from "./organisation-workspace";

type Profile = { name: string; version: string };
type Overview = { memberships: { id: string; product: string; status: string; startsAtUtc: string; endsAtUtc: string }[]; applications: { id: string; product: string; status: string; createdAtUtc: string }[] };
const input = "mt-2 w-full rounded-lg border border-slate-300 bg-white px-4 py-3 text-slate-900";
const button = "rounded-full bg-[#183a31] px-6 py-3 font-semibold text-white disabled:opacity-50";
export function MemberPortal({ applicationId, accountId }: { applicationId?: string; accountId?: string }) {
  const [profile, setProfile] = useState<Profile | null>(null);
  const [name, setName] = useState("");
  const [ready, setReady] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [overviewVersion, setOverviewVersion] = useState(0);
  const hadSession = useRef(false);
  async function load() {
    const current = await portal<Profile>("/profile"); hadSession.current = true; setProfile(current); setName(current.name);
  }
  useEffect(() => {
    let active = true;
    const expired = () => { setProfile(null); setName(""); setMessage(""); if (hadSession.current) setError("Sign in with an account that has active member access."); hadSession.current = false; };
    window.addEventListener("portal-session-expired", expired);
    portal<Profile>("/profile").then(p => { if (active) { hadSession.current = true; setProfile(p); setName(p.name); } })
      .catch(e => { if (active && e.status !== 401) setError(e.message); }).finally(() => { if (active) setReady(true); });
    return () => { active = false; window.removeEventListener("portal-session-expired", expired); };
  }, []);
  async function action(work: () => Promise<void>) {
    setBusy(true); setError(""); setMessage("");
    try { await work(); } catch (e) {
      setError(e instanceof Error ? e.message : "Unable to complete the request.");
      if ((e as { status?: number }).status === 401) setProfile(null);
    } finally { setBusy(false); }
  }
  return <div className="min-h-screen bg-[#f7f7f2] text-[#183a31]">
    <a href="#portal-main" className="sr-only focus:not-sr-only focus:block focus:p-4">Skip to main content</a>
    <header className="border-b border-[#183a31]/15 px-6 py-6 print:hidden"><div className="mx-auto flex max-w-5xl items-center justify-between gap-4"><Link href="/portal" className="text-xl font-semibold">Member portal</Link>{profile && <button disabled={busy} className="underline" onClick={() => void action(async () => { await portal("/logout", "POST"); setProfile(null); setName(""); })}>Sign out</button>}</div>
      {profile && <nav aria-label="Member navigation" className="mx-auto mt-5 flex max-w-5xl flex-wrap gap-x-6 gap-y-3 text-sm font-semibold"><Link href="/portal">Overview</Link><Link href="/portal#memberships">My memberships</Link><Link href="/portal#applications">My applications</Link><Link href="/portal#organisations">My organisations</Link><Link href="/portal#profile">My profile</Link></nav>}
    </header>
    <main id="portal-main" className="mx-auto max-w-5xl px-6 py-10">
      <p className="text-sm font-semibold uppercase tracking-widest">Your community. Your space.</p>
      <h1 className="mt-4 break-words text-3xl font-semibold sm:text-4xl">{profile ? applicationId ? "Your application" : `Welcome, ${profile.name}` : "Welcome to your member portal"}</h1>
      {profile && !applicationId && <p className="mt-4 max-w-2xl leading-7 text-slate-600">Manage your membership, follow your applications and keep your profile up to date.</p>}
      {error && <p role="alert" className="mt-6 rounded-xl bg-red-50 p-4 text-red-900">{error}</p>}
      {message && <p role="status" className="mt-6 rounded-xl bg-emerald-50 p-4">{message}</p>}
      {!ready ? <p role="status" className="mt-8">Loading your portal…</p> : !profile ? <section className="mt-8 max-w-lg rounded-2xl border border-slate-200 bg-white p-7">
        <h2 className="text-2xl font-semibold">Sign in</h2><p className="mt-3 text-sm text-slate-600">Local development access is available to accounts explicitly linked to a member profile by an administrator. Registration and password recovery are not available yet.</p>
        <form className="mt-6 space-y-5" onSubmit={e => { e.preventDefault(); const data = new FormData(e.currentTarget); void action(async () => { await portal("/login", "POST", { email: data.get("email"), password: data.get("password"), rememberMe: false }); await load(); }); }}>
          <label className="block">Email<input className={input} name="email" type="email" autoComplete="username" required /></label>
          <label className="block">Password<input className={input} name="password" type="password" autoComplete="current-password" required /></label>
          <button className={button} disabled={busy}>{busy ? "Signing in…" : "Sign in"}</button>
      </form></section> : applicationId ? <div className="mt-8"><ApplicationReceipt key={applicationId} id={applicationId} accountId={accountId} /></div> : <div className="mt-8 grid items-start gap-6 md:grid-cols-[2fr_1fr]">
        <div className="min-w-0 space-y-6">
        <section id="profile" className="min-w-0 scroll-mt-6 rounded-2xl border border-slate-200 bg-white p-7"><h2 className="text-2xl font-semibold">My profile</h2><p className="mt-3 text-sm text-slate-600">Your name is shared with the association’s membership team.</p>
          <form className="mt-6 space-y-5" onSubmit={e => { e.preventDefault(); void action(async () => { const updated = await portal<Profile>("/profile", "PUT", { name, version: profile.version }); setProfile(updated); setName(updated.name); setMessage("Your profile has been saved."); }); }}>
            <label className="block">Display name<input className={input} value={name} onChange={e => setName(e.target.value)} required maxLength={160} /></label>
            <div className="flex flex-wrap gap-4"><button className={button} disabled={busy}>Save profile</button><button type="button" className="underline" disabled={busy} onClick={() => void action(load)}>Reload profile</button></div>
          </form></section>
        <ApplicationWorkspace onSubmitted={() => setOverviewVersion(n => n + 1)} />
        <OrganisationWorkspace />
        </div>
        <MembershipOverview key={overviewVersion} />
      </div>}
    </main>
  </div>;
}

function MembershipOverview() {
  const [data, setData] = useState<Overview | null>(null);
  const [error, setError] = useState("");
  const [attempt, setAttempt] = useState(0);
  useEffect(() => {
    let current = true;
    portal<Overview>("/memberships").then(value => { if (current) { setData(value); setError(""); } })
      .catch(e => { if (current) setError(e.message); });
    return () => { current = false; };
  }, [attempt]);
  const date = (value: string) => new Intl.DateTimeFormat("en-GB", { dateStyle: "medium", timeZone: "UTC" }).format(new Date(value));
  return <aside id="memberships" className="min-w-0 scroll-mt-6 space-y-6 rounded-2xl bg-[#ddebab] p-7">
    <div className="flex flex-wrap items-center justify-between gap-3"><h2 className="text-xl font-semibold">My memberships</h2><button className="text-sm underline" onClick={() => { setError(""); setData(null); setAttempt(n => n + 1); }}>Refresh overview</button></div>
    {error ? <div role="alert"><p>{error}</p><button className="mt-3 underline" onClick={() => { setError(""); setData(null); setAttempt(n => n + 1); }}>Try again</button></div> : !data ? <p role="status">Loading membership details…</p> : <>
      {data.memberships.length === 0 ? <p className="text-sm">No personal memberships are linked to your profile.</p> : data.memberships.map(m => <article key={m.id} className="rounded-xl bg-white/75 p-4"><h3 className="font-semibold">{m.product}</h3><p className="mt-2 text-sm capitalize">{m.status}</p><p className="mt-2 text-sm">{date(m.startsAtUtc)} – {date(m.endsAtUtc)}</p></article>)}
      <h2 className="border-t border-[#183a31]/20 pt-5 text-xl font-semibold">My applications</h2>
      {data.applications.length === 0 ? <p className="text-sm">No personal applications have been submitted.</p> : data.applications.map(a => <article key={a.id} className="rounded-xl bg-white/75 p-4"><h3 className="break-words font-semibold">{a.product}</h3><p className="mt-2 text-sm capitalize">{a.status === "submitted" ? "Awaiting review" : a.status}</p><p className="mt-2 text-sm">Submitted {date(a.createdAtUtc)}</p><Link className="mt-3 inline-block text-sm font-semibold underline" href={`/portal/applications/${a.id}` as Route}>View application receipt<span className="sr-only"> for {a.product}</span></Link></article>)}
      <p className="text-xs leading-6">Organisation memberships require separately approved representative access.</p>
    </>}
  </aside>;
}

