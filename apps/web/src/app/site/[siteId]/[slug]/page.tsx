import { headers } from "next/headers";
import { notFound } from "next/navigation";
import { cache } from "react";
import type { Metadata } from "next";
import { SiteRenderer } from "@/features/content/site-renderer";
import type { PublicPage } from "@/features/content/types";

const read = cache(async (siteId: string, slug: string): Promise<PublicPage> => {
  if (!/^[a-f0-9-]{36}$/i.test(siteId) || !/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(slug)) notFound();
  const host = (await headers()).get("host");
  if (!host || /[\s/\\]/.test(host)) notFound();
  const response = await fetch(`${process.env.NEXORA_API_URL ?? "http://localhost:5080"}/api/v1/public/sites/${siteId}/${slug}`, { cache: "no-store", headers: { Host: host } });
  if (response.status === 404) notFound();
  if (!response.ok) throw new Error("The website could not be loaded. Please try again.");
  return response.json();
});
type Props = { params: Promise<{ siteId: string; slug: string }> };
export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { siteId, slug } = await params; const data = await read(siteId, slug);
  return { title: { absolute: `${data.page.title} | ${data.site.name}` }, description: data.page.description };
}
export default async function PublicSitePage({ params }: Props) {
  const { siteId, slug } = await params;
  return <SiteRenderer content={await read(siteId, slug)} />;
}
