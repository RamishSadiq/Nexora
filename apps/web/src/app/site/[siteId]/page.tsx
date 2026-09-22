import { redirect } from "next/navigation";
export default async function SiteHome({ params }: { params: Promise<{ siteId: string }> }) {
  const { siteId } = await params;
  redirect(`/site/${encodeURIComponent(siteId)}/home`);
}
