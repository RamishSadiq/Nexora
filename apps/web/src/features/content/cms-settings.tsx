"use client";
import { useEffect, useState } from "react";
import { moduleApi } from "../shared/module-api";
import type { Media, PageRecord, Site } from "./types";
const input = "mt-1 block w-full rounded border border-slate-300 bg-white p-2 text-slate-900";
const button = "rounded border border-slate-300 px-4 py-2 text-sm disabled:opacity-50";
const api = <T,>(path: string, method = "GET", body?: unknown) => moduleApi<T>("content", path, method, body);
export function CmsSettings({ site, media, canEdit, canPublish, changed }: { site: Site; media: Media[]; canEdit: boolean; canPublish: boolean; changed: () => void }) {
  const [error, setError] = useState(""); const [message, setMessage] = useState(""); const [busy, setBusy] = useState(false);
  async function run(work: () => Promise<void>) { setBusy(true); setError(""); setMessage(""); try { await work(); changed(); } catch (e) { setError(e instanceof Error ? e.message : "Unable to save."); } finally { setBusy(false); } }
  return <details className="rounded-xl bg-white p-5 text-slate-900"><summary className="cursor-pointer font-semibold">Site branding and media</summary>
    {error && <p role="alert" className="mt-3 text-red-800">{error}</p>}{message && <p role="status" className="mt-3">{message}</p>}
    <form key={site.version} className="mt-5 grid gap-4 sm:grid-cols-2" onSubmit={e => { e.preventDefault(); const data = new FormData(e.currentTarget); void run(async () => { await api("/site", "PUT", { version: site.version, name: data.get("name"), tagline: data.get("tagline"), brandColor: data.get("brandColor"), logoMediaId: data.get("logo") || null }); setMessage("Site settings published."); }); }}>
      <label>Site name<input name="name" className={input} defaultValue={site.name} required maxLength={100} /></label>
      <label>Site tagline<input name="tagline" className={input} defaultValue={site.tagline} maxLength={200} /></label>
      <label>Brand colour<input name="brandColor" className={input} type="color" defaultValue={site.brandColor ?? "#183a31"} /></label>
      <label>Site logo<select name="logo" className={input} defaultValue={site.logoMediaId ?? ""}><option value="">Association initial</option>{media.filter(m => m.isPublic && m.scanStatus === "clean" && m.contentType.startsWith("image/")).map(m => <option key={m.id} value={m.id}>{m.name}</option>)}</select></label>
      <p className="text-sm sm:col-span-2">These settings change the public website immediately. Choose a dark brand colour for readable text.</p>
      <button className={button} disabled={busy || !canPublish}>Publish site settings</button>
    </form>
    <section aria-label="Media library" className="mt-6 border-t pt-5"><h2 className="font-semibold">Media library</h2><p className="mt-2 text-sm">PNG, JPEG, PDF or UTF-8 text, up to 5 MB each. Files need a clean security scan and an explicit public grant before visitors can see them.</p>
      {canEdit && <form className="my-4 flex flex-wrap items-end gap-3" onSubmit={e => { e.preventDefault(); const form = e.currentTarget; const data = new FormData(form); data.set("version", site.version!); void run(async () => {
        const token = await fetch("/api/v1/auth/csrf", { cache: "no-store" }); if (!token.ok) throw new Error("Sign in again.");
        const response = await fetch("/api/v1/content/media", { method: "POST", headers: { "X-CSRF-TOKEN": (await token.json()).token }, body: data });
        if (!response.ok) throw new Error((await response.json()).title ?? "Upload failed."); form.reset(); setMessage("File uploaded. Review its scan status before granting public access.");
      }); }}><label>Upload file<input className={input} type="file" name="file" accept="image/png,image/jpeg,application/pdf,text/plain" required /></label><button disabled={busy} className={button}>Upload selected file</button></form>}
      {media.length === 0 && <p className="mt-3 text-sm">No files uploaded.</p>}
      <ul className="mt-3 space-y-3">{media.map(m => <li key={m.id} className="flex flex-wrap items-center gap-3 rounded border p-3"><span className="min-w-0 flex-1 break-all">{m.name} · {Math.ceil(m.size / 1024)} KB · {m.scanStatus} · {m.isPublic ? "Public" : "Private"}</span>
        {m.scanStatus === "quarantined" && canEdit && <button className={button} disabled={busy} onClick={() => void run(async () => { await api(`/media/${m.id}/scan`, "POST", { version: m.version }); })}>Retry scan</button>}
        {canPublish && <button className={button} disabled={busy || !m.isPublic && m.scanStatus !== "clean"} onClick={() => void run(async () => { await api(`/media/${m.id}/publication`, "PUT", { version: m.version, isPublic: !m.isPublic }); setMessage(m.isPublic ? "Public file access revoked." : "File is now public."); })}>{m.isPublic ? "Revoke public access" : "Make file public"}</button>}
        {canEdit && !m.isPublic && <button className={button} disabled={busy} onClick={() => { if (window.confirm("Delete this unused private file?")) void run(async () => { await api(`/media/${m.id}`, "DELETE", { version: m.version }); }); }}>Delete file</button>}
      </li>)}</ul>
    </section>
  </details>;
}
type Schedule = { id: string; pageId: string; status: string; publishAtUtc: string; failure?: string };
export function PagePublishingSettings({ page, disabled, changed }: { page: PageRecord; disabled: boolean; changed: () => Promise<void> }) {
  const [jobs, setJobs] = useState<Schedule[]>([]); const [error, setError] = useState(""); const [busy, setBusy] = useState(false);
  useEffect(() => { let active = true; api<Schedule[]>("/schedules").then(j => { if (active) setJobs(j.filter(x => x.pageId === page.id)); }).catch(e => { if (active) setError(e.message); }); return () => { active = false; }; }, [page.id, page.version]);
  async function run(work: () => Promise<unknown>) { setBusy(true); setError(""); try { await work(); await changed(); setJobs((await api<Schedule[]>("/schedules")).filter(j => j.pageId === page.id)); } catch (e) { setError(e instanceof Error ? e.message : "Unable to update publication."); } finally { setBusy(false); } }
  return <details className="border-t pt-4"><summary className="cursor-pointer font-semibold">Navigation, address and scheduled publishing</summary>{error && <p role="alert" className="mt-3 text-red-800">{error}</p>}
    <form key={page.version} className="mt-4 grid gap-3 sm:grid-cols-2" onSubmit={e => { e.preventDefault(); const data = new FormData(e.currentTarget); void run(() => api(`/pages/${page.id}/navigation`, "PUT", { version: page.version, label: data.get("label"), order: Number(data.get("order")), visible: data.has("visible") })); }}>
      <label>Menu label<input name="label" className={input} defaultValue={page.navigationLabel ?? ""} maxLength={80} placeholder="Published page title" /></label><label>Menu position<input name="order" type="number" min={0} max={999} className={input} defaultValue={page.navigationOrder ?? 0} /></label>
      <label><input name="visible" type="checkbox" defaultChecked={page.showInNavigation ?? true} /> Show in navigation</label><button className={button} disabled={disabled || busy}>Publish navigation</button>
    </form>
    {page.slug !== "home" && <form className="mt-5 flex flex-wrap items-end gap-3" onSubmit={e => { e.preventDefault(); const slug = new FormData(e.currentTarget).get("slug"); void run(() => api(`/pages/${page.id}/address`, "PUT", { version: page.version, slug })); }}><label>New page address<input name="slug" className={input} required maxLength={100} pattern="[a-z0-9]+(-[a-z0-9]+)*" /></label><button className={button} disabled={disabled || busy}>Change address and redirect old links</button></form>}
    <form className="mt-5 flex flex-wrap items-end gap-3" onSubmit={e => { e.preventDefault(); const value = String(new FormData(e.currentTarget).get("when")); void run(() => api(`/pages/${page.id}/schedule`, "POST", { version: page.version, revisionId: page.draftId, publishAtUtc: new Date(value).toISOString() })); }}><label>Publish saved draft at (your local time)<input name="when" className={input} type="datetime-local" required /></label><button className={button} disabled={disabled || busy || jobs.some(j => j.status === "pending")}>Schedule publication</button></form>
    <button className={`${button} mt-3`} disabled={disabled || busy} onClick={() => void run(async () => {})}>Refresh publishing status</button>
    <p className="mt-3 text-sm">The saved revision is fixed when scheduled. Manual publishing or unpublishing cancels its pending schedule.</p>
    <ul className="mt-3 space-y-2">{jobs.map(j => <li key={j.id} className="text-sm">{new Date(j.publishAtUtc).toLocaleString()} · {j.status}{j.failure && ` · ${j.failure}`}{j.status === "pending" && <button className={`${button} ml-3`} disabled={disabled || busy} onClick={() => void run(() => api(`/pages/${page.id}/cancel-schedule`, "POST", { version: page.version }))}>Cancel schedule</button>}</li>)}</ul>
  </details>;
}
