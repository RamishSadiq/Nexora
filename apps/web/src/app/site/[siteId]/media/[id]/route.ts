import { headers } from "next/headers";
export async function GET(_request: Request, { params }: { params: Promise<{ siteId: string; id: string }> }) {
  const { siteId, id } = await params; const host = (await headers()).get("host");
  if (!host || /[\s/\\]/.test(host) || ![siteId, id].every(v => /^[a-f0-9-]{36}$/i.test(v))) return new Response(null, { status: 404 });
  const response = await fetch(`${process.env.NEXORA_API_URL ?? "http://localhost:5080"}/api/v1/public/sites/${siteId}/media/${id}`, { headers: { Host: host }, cache: "no-store" });
  if (!response.ok) return new Response(null, { status: response.status });
  const output = new Headers({ "Content-Type": response.headers.get("content-type") ?? "application/octet-stream", "Cache-Control": "no-store", "X-Content-Type-Options": "nosniff", "Content-Security-Policy": "sandbox" });
  const disposition = response.headers.get("content-disposition"); if (disposition) output.set("Content-Disposition", disposition);
  return new Response(response.body, { headers: output });
}
