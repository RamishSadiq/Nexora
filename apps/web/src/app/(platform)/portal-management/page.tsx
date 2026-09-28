import { requireSession } from "@/lib/auth/session";
import { PortalEditor } from "@/features/content/portal-editor";
export const metadata = { title: "Portal management", robots: { index: false, follow: false } };
export default async function PortalManagementPage() {
  const session = await requireSession();
  if (!session.permissions.includes("content.read")) return <main className="rounded-xl bg-white p-8">Ask your administrator for portal content access.</main>;
  return <PortalEditor permissions={session.permissions} />;
}
