import { notFound, redirect } from "next/navigation";
import { requireSession } from "@/lib/auth/session";
import { MembershipWorkspace } from "@/features/membership/membership-workspace";

export default async function Page({ params }: { params: Promise<{ section?: string }> }) {
  const session = await requireSession();
  const { section } = await params;
  if (section === "products") redirect("/membership/setup");
  if (section && !["members", "applications", "products"].includes(section)) notFound();
  if (!session.permissions.includes("membership.read")) return <main className="p-8">Ask your administrator for access.</main>;
  if (!section) redirect("/membership/members");
  return <MembershipWorkspace key={section} permissions={session.permissions} section={section} />;
}
