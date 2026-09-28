import { headers } from "next/headers";
export async function GET(_request: Request, { params }: { params: Promise<{ siteId: string }> }) {
  const { siteId } = await params; const host = (await headers()).get("host");
  if (!host || /[\s/\\]/.test(host) || !/^[a-f0-9-]{36}$/i.test(siteId)) return new Response(null, { status: 404 });
  const response = await fetch(`${process.env.NEXORA_API_URL ?? "http://localhost:5080"}/api/v1/public/sites/${siteId}/sitemap.xml`, { headers: { Host: host }, cache: "no-store" });
  return new Response(response.ok ? await response.text() : null, { status: response.status, headers: { "Content-Type": "application/xml", "Cache-Control": "no-store" } });
}
