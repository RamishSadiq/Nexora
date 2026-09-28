/* eslint-disable @next/next/no-img-element -- Media URLs enforce tenant, scan and public-grant checks. */
import type { CSSProperties } from "react";
import Link from "next/link";
import type { PublicPage } from "./types";
import "./site.css";

export function SiteRenderer({ content, preview = false }: { content: PublicPage; preview?: boolean }) {
  const { site, page, navigation } = content;
  const base = content.basePath ?? `/site/${site.id}`;
  const mediaUrl = (id: string) => preview ? `/api/v1/content/media/${id}` : `/site/${site.id}/media/${id}`;
  // Keep editor-authored links within this site when it is served on its own domain.
  const pageLink = (url: string) => url.startsWith(`/site/${site.id}/`) ? `${base}/${url.slice(`/site/${site.id}/`.length)}` : url;
  const links = navigation;
  return <div style={{ "--site-brand": site.brandColor ?? "#183a31" } as CSSProperties} className="association-site min-h-screen bg-[#f7f7f2] text-[var(--site-brand)] selection:bg-[#d6ed8d]">
    <a href="#site-content" className="sr-only focus:not-sr-only focus:block focus:p-4">Skip to content</a>
    {preview && <div className="bg-amber-100 p-3 text-center text-sm text-amber-950">Draft preview — only published versions appear on your public website.</div>}
    <header className="border-b border-[var(--site-brand)]/15 bg-[#f7f7f2]">
      <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-6 px-6 py-7">
        <a href={`${base}/home`} className="flex items-center gap-3 text-xl font-semibold tracking-tight"><span aria-hidden className="grid size-10 place-items-center overflow-hidden rounded-full bg-[var(--site-brand)] text-lg text-[#deefa0]">{site.logoMediaId ? <img src={mediaUrl(site.logoMediaId)} alt="" className="size-10 object-contain" /> : site.name.slice(0, 1)}</span>{site.name}</a>
        <Link className="site-portal-link" href="/portal" prefetch={false}>Member portal <span aria-hidden>↗</span></Link>
        <nav aria-label="Website navigation" className="site-navigation flex flex-wrap gap-6 text-sm font-medium">{links.map(item => <a key={item.slug} aria-current={item.slug === content.slug ? "page" : undefined} className="border-b border-transparent pb-1 hover:border-[var(--site-brand)] aria-[current=page]:border-[var(--site-brand)]" href={`${base}/${item.slug}`}>{item.title}</a>)}</nav>
      </div>
    </header>
    <main id="site-content" className="mx-auto max-w-6xl px-6 pb-20">
      <div className="flex items-center gap-3 pt-10 text-xs font-semibold uppercase tracking-[0.2em]"><span className="h-px w-8 bg-[var(--site-brand)]" />{site.tagline || site.name}</div>
      <h1 className="max-w-4xl py-8 text-4xl font-medium leading-[1.05] tracking-[-0.04em] sm:text-6xl">{page.title}</h1>
      {page.description && <p className="site-introduction">{page.description}</p>}
      {content.slug !== "home" && <a className="site-back-link" href={`${base}/home`}>← Back to home</a>}
      <div className="site-blocks">{page.blocks.map((block, index) => {
        if (block.type === "download") return <section key={index} className="rounded-2xl border bg-white p-7"><h2 className="text-2xl font-medium">{block.heading}</h2><p className="my-3">{block.text}</p>{block.mediaId && <a className="font-semibold underline" href={mediaUrl(block.mediaId)} download>{block.linkLabel || "Download file"}</a>}</section>;
        if (block.type === "image") return <figure key={index} className="overflow-hidden rounded-2xl">{block.mediaId && <img src={mediaUrl(block.mediaId)} alt={block.text} className="h-auto max-h-[600px] w-full object-contain" />}{block.heading && <figcaption className="p-3 text-sm">{block.heading}</figcaption>}</figure>;
        const hero = block.type === "hero";
        const cta = block.type === "cta";
        return <section key={index} data-block={block.type} className={hero ? "site-hero relative overflow-hidden rounded-[2rem] bg-[var(--site-brand)] px-7 py-14 text-[#f8faef] sm:px-14 sm:py-20" : cta ? "rounded-[2rem] bg-[#ddebab] px-7 py-10 sm:px-12" : "rounded-2xl border border-[var(--site-brand)]/15 bg-white p-7 sm:p-10"}>
          {hero && <div className="site-art" aria-hidden="true"><span /><span /><span /><i>✳</i></div>}
          <div className="site-block-copy relative max-w-3xl">
            {block.type === "feature" && <span className="site-feature-mark" aria-hidden="true">✳</span>}
            {block.heading && <h2 className={hero ? "mb-6 text-4xl font-medium leading-tight tracking-tight sm:text-5xl" : "mb-4 text-2xl font-medium tracking-tight"}>{block.heading}</h2>}
            {block.text && <p className={`whitespace-pre-line text-base leading-8 sm:text-lg ${hero ? "text-[#eef3df]/85" : "text-[#486157]"}`}>{block.text}</p>}
            {block.linkUrl && <a className={`mt-8 inline-flex items-center gap-6 rounded-full px-6 py-3 text-sm font-semibold ${hero ? "bg-[#ddebab] text-[var(--site-brand)]" : "bg-[var(--site-brand)] text-white"}`} href={pageLink(block.linkUrl)}>{block.linkLabel}<span aria-hidden>↗</span></a>}
          </div>
        </section>;
      })}</div>
    </main>
    <footer className="border-t border-[var(--site-brand)]/15 px-6 py-10"><div className="mx-auto max-w-6xl"><div className="site-footer-top"><div><p className="text-2xl font-semibold">{site.name}</p><p className="mt-3 text-sm">{site.tagline}</p></div><nav aria-label="Footer navigation" className="flex flex-wrap gap-5 text-sm">{links.map(item => <a key={item.slug} href={`${base}/${item.slug}`}>{item.title}</a>)}</nav></div><p className="mt-10 border-t border-[var(--site-brand)]/15 pt-6 text-xs">© {new Date().getFullYear()} {site.name}</p></div></footer>
  </div>;
}
