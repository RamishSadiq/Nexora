"use client";
/* eslint-disable @next/next/no-img-element -- Media URLs enforce tenant, scan and public-grant checks. */
import { useEffect, useId, useRef, useState, type CSSProperties, type ReactNode } from "react";
import type { BlockItem, PublicPage, WebsiteMotion } from "./types";
import { defaultDesign, defaultLayout } from "./website-design";
import "./site.css";

function MotionSection({ children, motion, enabled, ...props }: { children: ReactNode; motion: WebsiteMotion; enabled: boolean; className: string; style: CSSProperties; "data-span": string; "data-alignment": string; "data-surface": string; "data-spacing": string; "data-block": string }) {
  const ref = useRef<HTMLElement>(null);
  useEffect(() => {
    const element = ref.current;
    if (!element || !enabled || motion.effect === "none" || !element.animate) return;
    const reduced = window.matchMedia("(prefers-reduced-motion: reduce)");
    if (reduced.matches) return;
    let animation: Animation | undefined;
    const play = () => {
      if (reduced.matches) return;
      const from: Keyframe = { opacity: 0, transform: "none", filter: "none" };
      if (motion.effect === "rise") from.transform = "translateY(" + motion.distance + "px)";
      if (motion.effect === "slide") from.transform = "translateX(" + motion.distance + "px)";
      if (motion.effect === "scale") from.transform = "scale(" + (1 - motion.distance / 500) + ")";
      if (motion.effect === "blur") from.filter = "blur(" + motion.distance / 3.5 + "px)";
      animation = element.animate([from, { opacity: 1, transform: "none", filter: "none" }], { duration: motion.durationMs, delay: motion.delayMs, easing: motion.easing, fill: "backwards" });
    };
    const observer = motion.trigger === "enter" && "IntersectionObserver" in window ? new IntersectionObserver(entries => {
      if (entries.some(entry => entry.isIntersecting)) { play(); observer?.disconnect(); }
    }, { threshold: .08 }) : null;
    if (observer) observer.observe(element); else play();
    const stop = () => { if (reduced.matches) { animation?.cancel(); observer?.disconnect(); } };
    reduced.addEventListener("change", stop);
    return () => { observer?.disconnect(); animation?.cancel(); reduced.removeEventListener("change", stop); };
  }, [enabled, motion]);
  return <section ref={ref} {...props} data-motion={enabled ? motion.effect : "none"} data-hover={enabled ? motion.hover : "none"}>{children}</section>;
}

function Showcase({ items, mediaUrl, pageLink }: { items: BlockItem[]; mediaUrl: (id: string) => string; pageLink: (url: string) => string }) {
  const [active, setActive] = useState(0);
  const id = useId();
  const index = Math.min(active, items.length - 1);
  return <div className="site-showcase">
    <div className="site-tabs" role="tablist" aria-label="Explore this section">{items.map((item, i) => <button key={i} type="button" role="tab" id={id + "-tab-" + i} aria-controls={id + "-panel-" + i} aria-selected={i === index} tabIndex={i === index ? 0 : -1} onClick={() => setActive(i)} onKeyDown={event => {
      let next = index;
      if (event.key === "ArrowRight") next = (index + 1) % items.length;
      else if (event.key === "ArrowLeft") next = (index - 1 + items.length) % items.length;
      else if (event.key === "Home") next = 0;
      else if (event.key === "End") next = items.length - 1;
      else return;
      event.preventDefault(); setActive(next); event.currentTarget.parentElement?.querySelectorAll<HTMLButtonElement>("[role=tab]")[next]?.focus();
    }}>{item.heading}</button>)}</div>
    {items.map((item, i) => <div key={i} role="tabpanel" id={id + "-panel-" + i} aria-labelledby={id + "-tab-" + i} tabIndex={0} hidden={i !== index} className="site-showcase-panel">
      {item.mediaId && <img src={mediaUrl(item.mediaId)} alt={item.text || item.heading} loading="lazy" />}
      <div><h3>{item.heading}</h3><p>{item.text}</p>{item.linkUrl && <a className="site-button" href={pageLink(item.linkUrl)}>{item.linkLabel}<span aria-hidden>↗</span></a>}</div>
    </div>)}
  </div>;
}

export function SiteRenderer({ content, preview = false }: { content: PublicPage; preview?: boolean }) {
  const { site, page, navigation } = content;
  const design = { ...defaultDesign, ...content.design };
  const base = content.basePath ?? "/site/" + site.id;
  const mediaUrl = (id: string) => preview ? "/api/v1/content/media/" + id : "/site/" + site.id + "/media/" + id;
  const pageLink = (url: string) => url.startsWith("/site/" + site.id + "/") ? base + "/" + url.slice(("/site/" + site.id + "/").length) : url;
  const home = base ? base + "/home" : "/";
  const style = {
    "--site-brand": site.brandColor ?? "#183a31", "--site-bg": design.backgroundColor, "--site-fg": design.textColor, "--site-accent": design.accentColor, "--site-accent-secondary": design.secondaryAccentColor,
    "--site-width": design.contentWidth + "px", "--site-gap": design.sectionGap + "px", "--site-radius": design.cornerRadius + "px",
    "--site-font": design.font === "serif" ? "Georgia, serif" : design.font === "mono" ? "ui-monospace, monospace" : "var(--font-geist-sans, Arial), sans-serif",
    "--site-heading-size": design.headingSize === "small" ? "3.5rem" : design.headingSize === "medium" ? "5rem" : "6.5rem",
    "--site-ambient-duration": design.ambientDurationMs + "ms", "--site-motion-duration": design.motion.durationMs + "ms", "--site-motion-easing": design.motion.easing,
    "--site-hover-distance": -design.motion.distance / 7 + "px", "--site-ambient-distance": -design.motion.distance / 2 + "px",
  } as CSSProperties;
  return <div style={style} className="association-site" data-theme={design.theme} data-motion-enabled={design.motionEnabled} data-ambient={design.ambientEffect} data-pause-hover={design.pauseAmbientOnHover}>
    <a href="#site-content" className="site-skip">Skip to content</a>
    {preview && <div className="site-preview-note">Draft preview — only published versions appear on your public website.</div>}
    <header className="site-header" data-sticky={design.stickyHeader}>
      <div className="site-header-inner"><a href={home} className="site-brand"><span className="site-logo" aria-hidden>{site.logoMediaId ? <img src={mediaUrl(site.logoMediaId)} alt="" /> : site.name.slice(0, 1)}</span>{site.name}</a>
        <nav aria-label="Website navigation" className="site-navigation">{navigation.map(item => <a key={item.slug} aria-current={item.slug === content.slug ? "page" : undefined} href={item.slug === "home" ? home : base + "/" + item.slug}>{item.title}</a>)}</nav>
        <div className="site-header-actions">{design.showPortalLink && <a className="site-button site-button-small site-portal-link" href={pageLink(design.portalUrl)}>{design.portalLabel}<span aria-hidden>↗</span></a>}
          {!!navigation.length && <details className="site-mobile-menu"><summary aria-label="Open website menu"><span aria-hidden>☰</span></summary><nav aria-label="Mobile website navigation">{navigation.map(item => <a key={item.slug} href={item.slug === "home" ? home : base + "/" + item.slug}>{item.title}</a>)}</nav></details>}
        </div>
      </div>
    </header>
    <main id="site-content" className="site-main">
      <div className="site-masthead">{design.showTagline && <p className="site-eyebrow"><span aria-hidden />{site.tagline || site.name}</p>}<h1>{page.title}</h1>
        {page.description && <p className="site-introduction">{page.description}</p>}{content.slug !== "home" && <a className="site-back-link" href={home}>← Back to home</a>}
      </div>
      <div className="site-blocks">{page.blocks.filter(block => !block.layout?.hidden).map((block, index) => {
        const layout = block.layout ?? defaultLayout(block);
        const motion = block.motion ?? design.motion;
        const items = block.items ?? [];
        return <MotionSection key={page.id + "-" + index} motion={motion} enabled={design.motionEnabled} className={"site-section site-block-" + block.type} data-block={block.type} data-span={layout.span} data-alignment={layout.alignment} data-surface={layout.surface} data-spacing={layout.spacing} style={{ "--site-motion-duration": motion.durationMs + "ms", "--site-motion-easing": motion.easing, "--site-hover-distance": -motion.distance / 7 + "px", "--site-ambient-distance": -motion.distance / 2 + "px" } as CSSProperties}>
          {layout.eyebrow && <p className="site-eyebrow">{layout.eyebrow}</p>}
          {block.heading && <h2>{block.heading}</h2>}
          {block.type !== "image" && block.text && <p className="site-section-description">{block.text}</p>}
          {block.mediaId && block.type !== "download" && <figure className="site-media-frame"><img src={mediaUrl(block.mediaId)} alt={block.text || block.heading} loading={block.type === "hero" ? "eager" : "lazy"} />{block.type === "image" && block.heading && <figcaption>{block.heading}</figcaption>}</figure>}
          {block.type === "hero" && !block.mediaId && <div className="site-art" aria-hidden><div className="site-art-orbit" /><div className="site-art-orbit" /><div className="site-art-panel"><span>{site.name.slice(0, 1)}</span><i /><i /><i /></div></div>}
          {block.type === "feature-grid" && <div className="site-feature-grid">{items.map((item, i) => <article className="site-feature-card" key={i}>{item.mediaId && <img src={mediaUrl(item.mediaId)} alt={item.text || item.heading} loading="lazy" />}<span className="site-item-number" aria-hidden>{String(i + 1).padStart(2, "0")}</span><h3>{item.heading}</h3><p>{item.text}</p>{item.linkUrl && <a className="site-text-link" href={pageLink(item.linkUrl)}>{item.linkLabel} <span aria-hidden>↗</span></a>}</article>)}</div>}
          {block.type === "showcase" && <Showcase items={items} mediaUrl={mediaUrl} pageLink={pageLink} />}
          {block.type === "gallery" && <div className="site-gallery" aria-label={block.heading || "Gallery"} tabIndex={0}>{items.map((item, i) => <figure key={i}>{item.mediaId && <img src={mediaUrl(item.mediaId)} alt={item.text || item.heading} loading="lazy" />}<figcaption><h3>{item.heading}</h3><p>{item.text}</p>{item.linkUrl && <a href={pageLink(item.linkUrl)}>{item.linkLabel} ↗</a>}</figcaption></figure>)}</div>}
          {block.type === "faq" && <div className="site-faq">{items.map((item, i) => <details key={i}><summary>{item.heading}<span aria-hidden>+</span></summary><p>{item.text}</p>{item.linkUrl && <a className="site-text-link" href={pageLink(item.linkUrl)}>{item.linkLabel} ↗</a>}</details>)}</div>}
          {block.type === "download" && block.mediaId && <a className="site-button" href={mediaUrl(block.mediaId)} download>{block.linkLabel || "Download file"}<span aria-hidden>↓</span></a>}
          {block.linkUrl && <a className="site-button" href={pageLink(block.linkUrl)}>{block.linkLabel}<span aria-hidden>↗</span></a>}
        </MotionSection>;
      })}</div>
    </main>
    {design.showFooter && <footer className="site-footer"><div className="site-footer-inner"><div><p className="site-footer-name">{site.name}</p><p>{design.footerText || site.tagline}</p></div><nav aria-label="Footer navigation">{navigation.map(item => <a key={item.slug} href={item.slug === "home" ? home : base + "/" + item.slug}>{item.title}</a>)}</nav><p className="site-copyright">© {new Date().getFullYear()} {site.name}</p></div></footer>}
  </div>;
}
