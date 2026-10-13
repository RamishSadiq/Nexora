"use client";
import { useState } from "react";
import { moduleApi } from "../shared/module-api";
import { SiteRenderer } from "./site-renderer";
import { blockTypes, defaultDesign, defaultLayout, defaultMotion, isCollection } from "./website-design";
import type { Block, BlockItem, Media, PublicPage, Site, WebsiteDesign, WebsiteDesignState, WebsiteMotion } from "./types";

const input = "mt-1 block w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900";
const button = "rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 disabled:opacity-50";
function Select({ label, value, options, change }: { label: string; value: string; options: string[]; change: (value: string) => void }) { return <label className="text-sm">{label}<select className={input} value={value} onChange={e => change(e.target.value)}>{options.map(option => <option key={option} value={option}>{option}</option>)}</select></label>; }
function NumberField({ label, value, min, max, change }: { label: string; value: number; min: number; max: number; change: (value: number) => void }) { return <label className="text-sm">{label}<input className={input} type="number" min={min} max={max} value={value} required onChange={e => change(Number(e.target.value))} /></label>; }

export function MotionFields({ value, change }: { value: WebsiteMotion; change: (value: WebsiteMotion) => void }) {
  const patch = (part: Partial<WebsiteMotion>) => change({ ...value, ...part });
  return <div className="grid gap-3 sm:grid-cols-3">
    <Select label="Entrance effect" value={value.effect} options={["none", "fade", "rise", "slide", "scale", "blur"]} change={effect => patch({ effect: effect as WebsiteMotion["effect"] })} />
    <Select label="Animation trigger" value={value.trigger} options={["enter", "load"]} change={trigger => patch({ trigger: trigger as WebsiteMotion["trigger"] })} />
    <Select label="Hover effect" value={value.hover} options={["none", "lift", "glow"]} change={hover => patch({ hover: hover as WebsiteMotion["hover"] })} />
    <NumberField label="Duration (ms)" value={value.durationMs} min={100} max={2000} change={durationMs => patch({ durationMs })} />
    <NumberField label="Delay (ms)" value={value.delayMs} min={0} max={2000} change={delayMs => patch({ delayMs })} />
    <NumberField label="Movement distance (px)" value={value.distance} min={0} max={100} change={distance => patch({ distance })} />
    <Select label="Easing" value={value.easing} options={["ease", "ease-in", "ease-out", "ease-in-out", "linear"]} change={easing => patch({ easing: easing as WebsiteMotion["easing"] })} />
  </div>;
}

export function WebsiteDesignEditor({ site, configuration, canEdit, canPublish, changed }: { site: Site; configuration?: WebsiteDesignState; canEdit: boolean; canPublish: boolean; changed: () => void }) {
  const [server, setServer] = useState(configuration);
  const [value, setValue] = useState<WebsiteDesign>(configuration?.draft ?? defaultDesign);
  const [saved, setSaved] = useState(JSON.stringify(configuration?.draft ?? defaultDesign));
  const [preview, setPreview] = useState<PublicPage | null>(null);
  const [error, setError] = useState(""); const [message, setMessage] = useState(""); const [busy, setBusy] = useState(false);
  const dirty = JSON.stringify(value) !== saved;
  const patch = (part: Partial<WebsiteDesign>) => setValue({ ...value, ...part });
  async function run(work: () => Promise<void>) { setBusy(true); setError(""); setMessage(""); try { await work(); } catch (e) { setError(e instanceof Error ? e.message : "Unable to update website design."); } finally { setBusy(false); } }
  return <details className="mt-6 border-t pt-5"><summary className="cursor-pointer font-semibold">Website design, layout and animations</summary>
    <p className="my-3 text-sm text-slate-600">Save and preview a design draft, then publish when ready. Existing page content is retained. Visitors’ reduced-motion preferences always take priority.</p>
    {error && <p role="alert" className="my-3 text-red-800">{error}</p>}{message && <p role="status" className="my-3">{message}</p>}
    <form onSubmit={e => { e.preventDefault(); void run(async () => { const result = await moduleApi<WebsiteDesignState>("content", "/site/design", "PUT", { version: server?.version ?? null, configuration: value }); setServer(result); setValue(result.draft); setSaved(JSON.stringify(result.draft)); changed(); setMessage("Website design draft saved. Public design is unchanged."); }); }}>
      <fieldset disabled={busy || !canEdit} className="space-y-5">
        <div className="grid gap-3 sm:grid-cols-3">
          <Select label="Website theme" value={value.theme} options={["studio", "classic"]} change={theme => patch({ theme: theme as WebsiteDesign["theme"] })} />
          {(["backgroundColor", "textColor", "accentColor", "secondaryAccentColor"] as const).map((key, i) => {
            const label = ["Background colour", "Text colour", "Accent colour", "Secondary accent colour"][i];
            return <div key={key}><label className="text-sm">{label}<input className={input} type="color" value={value[key]} onChange={e => patch({ [key]: e.target.value })} /></label><label><span className="sr-only">{label} hex</span><input className={input + " font-mono"} type="text" value={value[key]} pattern="#[0-9a-fA-F]{6}" maxLength={7} required spellCheck={false} onChange={e => patch({ [key]: e.target.value })} /></label></div>;
          })}
          <Select label="Website font" value={value.font} options={["sans", "serif", "mono"]} change={font => patch({ font: font as WebsiteDesign["font"] })} />
          <Select label="Headline size" value={value.headingSize} options={["small", "medium", "large"]} change={headingSize => patch({ headingSize: headingSize as WebsiteDesign["headingSize"] })} />
          <NumberField label="Content width (px)" value={value.contentWidth} min={900} max={1600} change={contentWidth => patch({ contentWidth })} />
          <NumberField label="Section gap (px)" value={value.sectionGap} min={12} max={120} change={sectionGap => patch({ sectionGap })} />
          <NumberField label="Corner radius (px)" value={value.cornerRadius} min={0} max={48} change={cornerRadius => patch({ cornerRadius })} />
        </div>
        <div className="flex flex-wrap gap-4 text-sm">{(["stickyHeader", "showTagline", "showPortalLink", "showFooter", "motionEnabled", "pauseAmbientOnHover"] as const).map((key, i) => <label key={key}><input type="checkbox" checked={value[key]} onChange={e => patch({ [key]: e.target.checked })} /> {["Sticky header", "Show tagline", "Show header button", "Show footer", "Enable website animations", "Pause ambient motion on hover"][i]}</label>)}</div>
        <div className="grid gap-3 sm:grid-cols-2"><label className="text-sm">Header button label<input className={input} maxLength={80} required={value.showPortalLink} value={value.portalLabel} onChange={e => patch({ portalLabel: e.target.value })} /></label><label className="text-sm">Header button URL<input className={input} maxLength={2000} required={value.showPortalLink} value={value.portalUrl} onChange={e => patch({ portalUrl: e.target.value })} /></label><label className="text-sm sm:col-span-2">Footer description<textarea className={input} maxLength={500} placeholder="Use site tagline" value={value.footerText} onChange={e => patch({ footerText: e.target.value })} /></label></div>
        <h3 className="font-semibold">Default section motion</h3><MotionFields value={value.motion} change={motion => patch({ motion })} />
        <div className="grid gap-3 sm:grid-cols-2"><Select label="Hero ambient effect" value={value.ambientEffect} options={["none", "float", "glow"]} change={ambientEffect => patch({ ambientEffect: ambientEffect as WebsiteDesign["ambientEffect"] })} /><NumberField label="Ambient cycle duration (ms)" value={value.ambientDurationMs} min={2000} max={30000} change={ambientDurationMs => patch({ ambientDurationMs })} /></div>
        <button className={button} type="submit">Save website design draft</button>
      </fieldset>
    </form>
    <div className="my-4 flex flex-wrap gap-3"><button type="button" className={button} disabled={busy} onClick={() => void run(async () => { if (preview) { setPreview(null); return; } const state = await moduleApi<{ pages: { id: string; slug: string }[] }>("content", "/site"); const home = state.pages.find(p => p.slug === "home") ?? state.pages[0]; if (!home) throw new Error("Add and save a page before previewing its design."); const page = await moduleApi<{ page: { slug: string }; revision: PublicPage["page"] }>("content", "/pages/" + home.id); setPreview({ site, slug: page.page.slug, page: page.revision, navigation: [] }); })}>{preview ? "Close design preview" : "Preview website design"}</button>
      <button type="button" className={button} disabled={busy || !canPublish || dirty || !server?.version || !server.hasUnpublishedChanges} onClick={() => void run(async () => { const result = await moduleApi<WebsiteDesignState>("content", "/site/design/publish", "POST", { version: server!.version }); setServer(result); changed(); setMessage("Website design published."); })}>Publish saved website design</button>
      {dirty && <span className="self-center text-sm">Save design changes before publishing.</span>}
    </div>
    {preview && <SiteRenderer preview content={{ ...preview, design: value }} />}
  </details>;
}

export function BlockEditor({ block, index, count, media, patch, move, remove }: { block: Block; index: number; count: number; media: Media[]; patch: (value: Partial<Block>) => void; move: (direction: number) => void; remove: () => void }) {
  const layout = block.layout ?? defaultLayout(block); const items = block.items ?? [];
  const patchItem = (i: number, change: Partial<BlockItem>) => patch({ items: items.map((item, n) => n === i ? { ...item, ...change } : item) });
  const imageOptions = media.filter(m => block.type === "download" || m.contentType.startsWith("image/"));
  return <fieldset className="space-y-3 rounded-lg border border-slate-200 p-4"><legend className="px-2 text-sm font-semibold">Block {index + 1}</legend>
    <div className="flex flex-wrap items-end gap-2"><label className="grow text-sm">Block style<select className={input} value={block.type} onChange={e => { const type = e.target.value as Block["type"]; patch({ type, ...(isCollection(type) && !items.length ? { items: [{ heading: "New item", text: "" }] } : {}) }); }}>{blockTypes.map(type => <option key={type.value} value={type.value}>{type.label}</option>)}</select></label><button type="button" className={button} disabled={index === 0} onClick={() => move(-1)}>Move up</button><button type="button" className={button} disabled={index === count - 1} onClick={() => move(1)}>Move down</button><button type="button" className={button} disabled={count === 1} onClick={remove}>Remove block</button></div>
    <label className="block text-sm">Heading<input className={input} maxLength={160} value={block.heading} onChange={e => patch({ heading: e.target.value })} /></label>
    <label className="block text-sm">Text<textarea className={input} rows={4} maxLength={3000} value={block.text} onChange={e => patch({ text: e.target.value })} /></label>
    <label className="block text-sm">{block.type === "download" ? "Download file" : "Section image"}<select className={input} value={block.mediaId ?? ""} required={block.type === "image" || block.type === "download"} onChange={e => patch({ mediaId: e.target.value || null })}><option value="">{block.type === "hero" ? "Use editable hero decoration" : "No file"}</option>{imageOptions.map(m => <option key={m.id} value={m.id}>{m.name} ({m.scanStatus}, {m.isPublic ? "public" : "private"})</option>)}</select><span className="text-xs">Image descriptions use the Text field. Files must pass their scan and have public access before publication.</span></label>
    <div className="grid gap-3 sm:grid-cols-2"><label className="text-sm">Link label<input className={input} maxLength={80} value={block.linkLabel ?? ""} onChange={e => patch({ linkLabel: e.target.value })} /></label><label className="text-sm">Link URL<input className={input} maxLength={2000} value={block.linkUrl ?? ""} onChange={e => patch({ linkUrl: e.target.value })} /></label></div>
    {isCollection(block.type) && <div className="space-y-3"><h3 className="font-semibold">Section items</h3>{items.map((item, i) => <fieldset key={i} className="space-y-3 rounded border p-3"><legend className="px-2 text-sm">Item {i + 1}</legend><label className="block text-sm">Item heading<input className={input} value={item.heading} maxLength={160} required onChange={e => patchItem(i, { heading: e.target.value })} /></label><label className="block text-sm">Item text / image description<textarea className={input} value={item.text} maxLength={1500} onChange={e => patchItem(i, { text: e.target.value })} /></label><label className="block text-sm">Item image<select className={input} value={item.mediaId ?? ""} onChange={e => patchItem(i, { mediaId: e.target.value || null })}><option value="">No image</option>{media.filter(m => m.contentType.startsWith("image/")).map(m => <option key={m.id} value={m.id}>{m.name} ({m.scanStatus})</option>)}</select></label><div className="grid gap-3 sm:grid-cols-2"><label className="text-sm">Item link label<input className={input} value={item.linkLabel ?? ""} maxLength={80} onChange={e => patchItem(i, { linkLabel: e.target.value })} /></label><label className="text-sm">Item link URL<input className={input} value={item.linkUrl ?? ""} maxLength={2000} onChange={e => patchItem(i, { linkUrl: e.target.value })} /></label></div><div className="flex gap-2"><button type="button" className={button} disabled={i === 0} onClick={() => { const reordered = [...items]; [reordered[i - 1], reordered[i]] = [reordered[i], reordered[i - 1]]; patch({ items: reordered }); }}>Move item up</button><button type="button" className={button} disabled={items.length === 1} onClick={() => patch({ items: items.filter((_, n) => n !== i) })}>Remove item</button></div></fieldset>)}<button type="button" className={button} disabled={items.length >= 12} onClick={() => patch({ items: [...items, { heading: "New item", text: "" }] })}>Add section item</button></div>}
    <details className="border-t pt-3"><summary className="cursor-pointer text-sm font-semibold">Section layout and animation</summary><div className="mt-3 grid gap-3 sm:grid-cols-2">
      <Select label="Section width" value={layout.span} options={["full", "half", "third"]} change={span => patch({ layout: { ...layout, span: span as typeof layout.span } })} /><Select label="Text alignment" value={layout.alignment} options={["left", "center"]} change={alignment => patch({ layout: { ...layout, alignment: alignment as typeof layout.alignment } })} /><Select label="Section surface" value={layout.surface} options={["plain", "card", "accent", "glass"]} change={surface => patch({ layout: { ...layout, surface: surface as typeof layout.surface } })} /><Select label="Section spacing" value={layout.spacing} options={["compact", "normal", "spacious"]} change={spacing => patch({ layout: { ...layout, spacing: spacing as typeof layout.spacing } })} /><label className="text-sm">Section eyebrow<input className={input} value={layout.eyebrow} maxLength={80} onChange={e => patch({ layout: { ...layout, eyebrow: e.target.value } })} /></label><label className="self-center text-sm"><input type="checkbox" checked={layout.hidden} onChange={e => patch({ layout: { ...layout, hidden: e.target.checked } })} /> Hide section</label>
    </div><label className="my-4 block text-sm"><input type="checkbox" checked={!!block.motion} onChange={e => patch({ motion: e.target.checked ? { ...defaultMotion } : null })} /> Override website animation defaults</label>{block.motion && <MotionFields value={block.motion} change={motion => patch({ motion })} />}</details>
  </fieldset>;
}
