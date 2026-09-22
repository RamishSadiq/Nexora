export type Block = { type: "hero" | "text" | "feature" | "cta"; heading: string; text: string; linkLabel?: string; linkUrl?: string };
export type Site = { id: string; name: string; tagline: string };
export type Revision = { createdAtUtc?: string; id: string; title: string; description: string; blocks: Block[] };
export type PageRecord = { id: string; siteId: string; slug: string; version: string; draftId: string; publishedId: string | null };
export type PageDetail = { page: PageRecord; revision: Revision; history?: { id: string; title: string; createdAtUtc: string }[] };
export type SiteState = { site: Site | null; pages: (PageRecord & { title: string })[] };
export type PublicPage = { site: Site; slug: string; page: Revision; navigation: { slug: string; title: string }[] };
export type Draft = { slug: string; title: string; description: string; blocks: Block[] };
