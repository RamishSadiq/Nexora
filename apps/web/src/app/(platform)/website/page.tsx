import { requireSession } from "@/lib/auth/session";
import { ContentWorkspace } from "@/features/content/content-workspace";
export const metadata = { title: "Website", robots: { index: false, follow: false } };
export default async function WebsitePage() {
  const session = await requireSession();
  if (!session.permissions.includes("content.read")) return <main className="rounded-xl bg-white p-8">Ask your administrator for website access.</main>;
  return <ContentWorkspace permissions={session.permissions} />;
}
