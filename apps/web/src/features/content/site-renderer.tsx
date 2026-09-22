import type { PublicPage } from "./types";
import "./site.css";

export function SiteRenderer({ content, preview = false }: { content: PublicPage; preview?: boolean }) {
  const { site, page, navigation } = content;
  const base = `/site/${site.id}`;
  const labels: Record<string, string> = { home: "Home", about: "About us", membership: "Membership", events: "Events", news: "News", contact: "Contact" };
  const order = Object.keys(labels);
  const links = [...navigation].sort((a, b) => (order.includes(a.slug) ? order.indexOf(a.slug) : 99) - (order.includes(b.slug) ? order.indexOf(b.slug) : 99));
  return <div className="association-site min-h-screen bg-[#f7f7f2] text-[#183a31] selection:bg-[#d6ed8d]">
    <a href="#site-content" className="sr-only focus:not-sr-only focus:block focus:p-4">Skip to content</a>
    {preview && <div className="bg-amber-100 p-3 text-center text-sm text-amber-950">Draft preview — only published versions appear on your public website.</div>}
    <header className="border-b border-[#183a31]/15 bg-[#f7f7f2]">
      <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-6 px-6 py-7">
        <a href={`${base}/home`} className="flex items-center gap-3 text-xl font-semibold tracking-tight"><span aria-hidden className="grid size-10 place-items-center rounded-full bg-[#183a31] text-lg text-[#deefa0]">{site.name.slice(0, 1)}</span>{site.name}</a>
        <nav aria-label="Website navigation" className="flex flex-wrap gap-6 text-sm font-medium">{links.map(item => <a key={item.slug} aria-current={item.slug === content.slug ? "page" : undefined} className="border-b border-transparent pb-1 hover:border-[#183a31] aria-[current=page]:border-[#183a31]" href={`${base}/${item.slug}`}>{labels[item.slug] ?? item.title}</a>)}</nav>
      </div>
    </header>
    <main id="site-content" className="mx-auto max-w-6xl px-6 pb-20">
      <div className="flex items-center gap-3 pt-10 text-xs font-semibold uppercase tracking-[0.2em]"><span className="h-px w-8 bg-[#183a31]" />{site.tagline || site.name}</div>
      <h1 className="max-w-4xl py-8 text-4xl font-medium leading-[1.05] tracking-[-0.04em] sm:text-6xl">{page.title}</h1>
      <div className="site-blocks">{page.blocks.map((block, index) => {
        const hero = block.type === "hero";
        const cta = block.type === "cta";
        return <section key={index} data-block={block.type} className={hero ? "site-hero relative overflow-hidden rounded-[2rem] bg-[#183a31] px-7 py-14 text-[#f8faef] sm:px-14 sm:py-20" : cta ? "rounded-[2rem] bg-[#ddebab] px-7 py-10 sm:px-12" : "rounded-2xl border border-[#183a31]/15 bg-white p-7 sm:p-10"}>
          {hero && <div className="site-art" aria-hidden="true"><span /><span /><span /><i>✳</i></div>}
          <div className="site-block-copy relative max-w-3xl">
            {block.type === "feature" && <span className="site-feature-mark" aria-hidden="true">✳</span>}
            {block.heading && <h2 className={hero ? "mb-6 text-4xl font-medium leading-tight tracking-tight sm:text-5xl" : "mb-4 text-2xl font-medium tracking-tight"}>{block.heading}</h2>}
            {block.text && <p className={`whitespace-pre-line text-base leading-8 sm:text-lg ${hero ? "text-[#eef3df]/85" : "text-[#486157]"}`}>{block.text}</p>}
            {block.linkUrl && <a className={`mt-8 inline-flex items-center gap-6 rounded-full px-6 py-3 text-sm font-semibold ${hero ? "bg-[#ddebab] text-[#183a31]" : "bg-[#183a31] text-white"}`} href={block.linkUrl}>{block.linkLabel}<span aria-hidden>↗</span></a>}
          </div>
        </section>;
      })}</div>
    </main>
    <footer className="border-t border-[#183a31]/15 px-6 py-10"><div className="mx-auto max-w-6xl"><div className="site-footer-top"><div><p className="text-2xl font-semibold">{site.name}</p><p className="mt-3 text-sm">{site.tagline}</p></div><nav aria-label="Footer navigation" className="flex flex-wrap gap-5 text-sm">{links.map(item => <a key={item.slug} href={`${base}/${item.slug}`}>{labels[item.slug] ?? item.title}</a>)}</nav></div><p className="mt-10 border-t border-[#183a31]/15 pt-6 text-xs">© {new Date().getFullYear()} {site.name}</p></div></footer>
  </div>;
}
