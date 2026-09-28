"use client";

import { useEffect, useRef, useState } from "react";
import { moduleApi } from "../shared/module-api";
import { CmsSettings, PagePublishingSettings } from "./cms-settings";
import { SiteRenderer } from "./site-renderer";
import type { Block, Draft, PageDetail, PageRecord, SiteState, Media } from "./types";

const input = "mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900";
const button = "rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-800 disabled:opacity-50";
const primary = "rounded-lg bg-[#183a31] px-4 py-2 text-sm font-medium text-white disabled:opacity-50";
const blank: Draft = { slug: "home", title: "Welcome to our association", description: "Connect with our association and discover how to get involved.", blocks: [{ type: "hero", heading: "A stronger community, together.", text: "Introduce your association, the people you serve, and the difference membership makes." }] };
export function ContentWorkspace({ permissions }: { permissions: string[] }) {
  const [state, setState] = useState<SiteState>({ site: null, pages: [] });
  const [media, setMedia] = useState<Media[]>([]);
  const [ready, setReady] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);
  const [reload, setReload] = useState(0);
  const [detail, setDetail] = useState<PageDetail | null>(null);
  const [draft, setDraft] = useState<Draft>(blank);
  const [saved, setSaved] = useState("");
  const [editing, setEditing] = useState(false);
  const [preview, setPreview] = useState(false);
  const [restoreId, setRestoreId] = useState("");
  const request = useRef(0);
  const canEdit = permissions.includes("content.manage");
  const canPublish = permissions.includes("content.publish");
  const dirty = JSON.stringify(draft) !== saved;
  useEffect(() => {
    let current = true;
    moduleApi<SiteState>("content", "/site").then(s => { if (current) { setState(s); setReady(true); setError(""); } }).catch(e => { if (current) setError(e.message); });
    moduleApi<Media[]>("content", "/media").then(m => { if (current) setMedia(m); }).catch(e => { if (current) setError(e.message); });
    return () => { current = false; };
  }, [reload]);
  async function open(id: string) {
    if (editing && dirty && !window.confirm("Discard your unsaved page changes?")) return;
    const token = ++request.current; setBusy(true); setError(""); setMessage("");
    try {
      const d = await moduleApi<PageDetail>("content", `/pages/${id}`);
      if (token !== request.current) return;
      const next = { slug: d.page.slug, title: d.revision.title, description: d.revision.description, blocks: d.revision.blocks };
      setDetail(d); setDraft(next); setSaved(JSON.stringify(next)); setEditing(true); setPreview(false); setRestoreId("");
    } catch (e) { setError(e instanceof Error ? e.message : "Unable to open page."); }
    finally { if (token === request.current) setBusy(false); }
  }
  async function action(work: () => Promise<void>) {
    setBusy(true); setError(""); setMessage("");
    try { await work(); setReload(n => n + 1); } catch (e) { setError(e instanceof Error ? e.message : "Unable to save."); } finally { setBusy(false); }
  }
  function patchBlock(index: number, change: Partial<Block>) { setDraft(d => ({ ...d, blocks: d.blocks.map((b, i) => i === index ? { ...b, ...change } : b) })); }
  async function publish(unpublish = false, revisionId?: string) {
    if (!detail || !window.confirm(unpublish ? "Remove this page from the public website?" : "Publish this saved version to the public website?")) return;
    await action(async () => {
      const page = await moduleApi<PageRecord>("content", `/pages/${detail.page.id}/${unpublish ? "unpublish" : "publish"}`, "POST", { version: detail.page.version, revisionId });
      setDetail({ ...detail, page }); setMessage(unpublish ? "Page unpublished." : "Saved version published.");
    });
  }
  return <main className="mx-auto max-w-7xl space-y-5 p-5 sm:p-8">
    <header className="rounded-xl bg-white p-6 text-slate-900"><p className="text-xs font-semibold uppercase tracking-widest text-emerald-700">Association website</p><h1 className="mt-2 text-3xl font-semibold">Your website, in your words.</h1><p className="mt-2 text-sm text-slate-600">Build pages, review a draft, then publish when you’re ready.</p></header>
    {error && <div role="alert" className="rounded-lg bg-red-50 p-4 text-red-800">{error} <button className={button} onClick={() => { setReload(n => n + 1); if (detail) void open(detail.page.id); }}>Reload</button></div>}
    {message && <p role="status" className="rounded-lg bg-emerald-50 p-4 text-emerald-900">{message}</p>}
    {!ready && !error && <p role="status" className="rounded-lg bg-white p-4">Loading website…</p>}
    {ready && !state.site && <form className="max-w-xl space-y-4 rounded-xl bg-white p-6 text-slate-900" onSubmit={e => {
      e.preventDefault(); const data = new FormData(e.currentTarget);
      void action(async () => { await moduleApi("content", "/site", "POST", { name: data.get("name"), tagline: data.get("tagline") }); setMessage("Website created. Add your first page and publish it when ready."); });
    }}><h2 className="text-xl font-semibold">Create your association website</h2><label className="block text-sm">Association name<input name="name" className={input} required maxLength={100} /></label><label className="block text-sm">Short tagline<input name="tagline" className={input} maxLength={200} /></label><button className={primary} disabled={busy || !canEdit}>Create website</button></form>}
    {state.site && <CmsSettings site={state.site} media={media} canEdit={canEdit} canPublish={canPublish} changed={() => setReload(n => n + 1)} />}
    {state.site && <div className="grid items-start gap-5 lg:grid-cols-[260px_1fr]">
      <section className="space-y-4 rounded-xl bg-white p-5 text-slate-900" aria-label="Website pages"><h2 className="font-semibold">{state.site.name}</h2>
        {state.pages.some(p => p.slug === "home" && p.publishedId) && <a className={`${button} block text-center`} href={`/site/${state.site.id}/home`} target="_blank" rel="noreferrer">Open public website ↗</a>}
        {state.pages.length === 0 && <p className="text-sm text-slate-500">Start with a home page, then add information about your association and membership.</p>}
        {state.pages.map(p => <button key={p.id} disabled={busy} onClick={() => void open(p.id)} className={`block w-full rounded-lg border p-3 text-left ${p.id === detail?.page.id ? "border-emerald-700 bg-emerald-50" : "border-slate-200"}`}><span className="block font-medium">{p.title}</span><span className="text-xs text-slate-500">/{p.slug} · {p.publishedId ? "Published" : "Draft"}{p.publishedId && p.publishedId !== p.draftId ? " · Unpublished changes" : ""}</span></button>)}
        {canEdit && <button className={button} disabled={busy} onClick={() => { if (editing && dirty && !window.confirm("Discard unsaved changes?")) return; ++request.current; setDetail(null); setDraft({ ...blank, slug: state.pages.length ? "" : "home" }); setSaved(""); setEditing(true); setPreview(false); setMessage(""); }}>New page</button>}
      </section>
      {editing ? <section className="space-y-5 rounded-xl bg-white p-6 text-slate-900">
        <div className="flex flex-wrap items-center gap-2"><h2 className="mr-auto text-xl font-semibold">{detail ? "Edit page" : "New page"}</h2><button className={button} onClick={() => setPreview(!preview)}>{preview ? "Back to editor" : "Preview draft"}</button>{detail?.page.publishedId && <a className={button} href={`/site/${state.site.id}/${detail.page.slug}`} target="_blank" rel="noreferrer">View published page ↗</a>}</div>
        {preview ? <SiteRenderer preview content={{ site: state.site, slug: draft.slug, page: { ...draft, id: detail?.revision.id ?? "preview" }, navigation: [] }} /> : <form className="space-y-4" onSubmit={e => { e.preventDefault(); void action(async () => {
          const result = await moduleApi<PageDetail>("content", detail ? `/pages/${detail.page.id}` : "/pages", detail ? "PUT" : "POST", { ...draft, version: detail?.page.version });
          setDetail({ ...result, history: [{ id: result.revision.id, title: result.revision.title, createdAtUtc: result.revision.createdAtUtc ?? "" }, ...(detail?.history ?? [])].slice(0, 100) }); setSaved(JSON.stringify(draft)); setMessage("Draft saved. The published page is unchanged.");
        }); }}>
          <fieldset disabled={busy || !canEdit} className="space-y-4">
            <div className="grid gap-4 sm:grid-cols-2"><label className="text-sm">Page title<input className={input} required maxLength={120} value={draft.title} onChange={e => setDraft({ ...draft, title: e.target.value })} /></label><label className="text-sm">Page address<input className={input} required pattern="[a-z0-9]+(-[a-z0-9]+)*" maxLength={100} disabled={!!detail} value={draft.slug} onChange={e => setDraft({ ...draft, slug: e.target.value })} /></label></div>
            <label className="block text-sm">Search description<textarea className={input} maxLength={300} value={draft.description} onChange={e => setDraft({ ...draft, description: e.target.value })} /></label>
            {draft.blocks.map((block, index) => <fieldset key={index} className="space-y-3 rounded-lg border border-slate-200 p-4"><legend className="px-2 text-sm font-semibold">Block {index + 1}</legend>
              <div className="flex flex-wrap items-end gap-2"><label className="grow text-sm">Block style<select className={input} value={block.type} onChange={e => patchBlock(index, { type: e.target.value as Block["type"] })}>{["hero", "text", "feature", "cta", "image", "download"].map(type => <option key={type}>{type}</option>)}</select></label><button type="button" className={button} disabled={index === 0} onClick={() => setDraft(d => { const blocks = [...d.blocks]; [blocks[index - 1], blocks[index]] = [blocks[index], blocks[index - 1]]; return { ...d, blocks }; })}>Move up</button><button type="button" className={button} disabled={draft.blocks.length === 1} onClick={() => setDraft({ ...draft, blocks: draft.blocks.filter((_, i) => i !== index) })}>Remove block</button></div>
              {(block.type === "image" || block.type === "download") && <label className="block text-sm">Image<select className={input} value={block.mediaId ?? ""} onChange={e => patchBlock(index, { mediaId: e.target.value || null })}><option value="">Choose an uploaded image</option>{media.map(m => <option key={m.id} value={m.id}>{m.name} ({m.scanStatus}, {m.isPublic ? "public" : "private"})</option>)}</select><span className="text-xs">Use the Text field below for alternative text.</span></label>}
              <label className="block text-sm">Heading<input className={input} maxLength={160} value={block.heading} onChange={e => patchBlock(index, { heading: e.target.value })} /></label>
              <label className="block text-sm">Text<textarea className={input} rows={4} maxLength={3000} value={block.text} onChange={e => patchBlock(index, { text: e.target.value })} /></label>
              <div className="grid gap-3 sm:grid-cols-2"><label className="text-sm">Link label<input className={input} maxLength={80} value={block.linkLabel ?? ""} onChange={e => patchBlock(index, { linkLabel: e.target.value })} /></label><label className="text-sm">Link URL<input className={input} maxLength={2000} placeholder="https://… or /site/…" value={block.linkUrl ?? ""} onChange={e => patchBlock(index, { linkUrl: e.target.value })} /></label></div>
            </fieldset>)}
            <div className="flex gap-2"><button type="button" className={button} disabled={draft.blocks.length >= 20} onClick={() => setDraft({ ...draft, blocks: [...draft.blocks, { type: "text", heading: "", text: "" }] })}>Add block</button><button className={primary}>Save draft</button></div>
          </fieldset>
        </form>}
        {detail && canPublish && <PagePublishingSettings page={detail.page} disabled={busy || dirty} changed={async () => { await open(detail.page.id); setReload(n => n + 1); }} />}
        {detail && canPublish && <div className="space-y-3 border-t border-slate-200 pt-4"><p className="text-sm text-slate-600">{dirty ? "Save draft changes before publishing." : "Publishing makes this saved version visible to everyone."}</p><div className="flex flex-wrap gap-2"><button className={primary} disabled={busy || dirty} onClick={() => void publish()}>Publish saved draft</button>{detail.page.publishedId && <button className={button} disabled={busy || dirty} onClick={() => void publish(true)}>Unpublish page</button>}</div>
          {!!detail.history?.length && <div className="flex flex-wrap items-end gap-2"><label className="text-sm">Previous version<select className={input} value={restoreId} onChange={e => setRestoreId(e.target.value)}><option value="">Choose a saved version</option>{detail.history.map(v => <option key={v.id} value={v.id}>{v.title} · {new Date(v.createdAtUtc).toLocaleString()}</option>)}</select></label><button className={button} disabled={busy || dirty || !restoreId} onClick={() => void publish(false, restoreId)}>Publish selected version</button></div>}
        </div>}
      </section> : <div className="rounded-xl bg-white p-8 text-slate-600">Choose a page to edit, or create your first page.</div>}
    </div>}
  </main>;
}
