import { notFound, redirect } from "next/navigation";
import { requireSession } from "@/lib/auth/session";
import { MembershipWorkspace } from "@/features/membership/membership-workspace";
import { MembershipProducts } from "@/features/membership/membership-products";

export default async function Page({ params, searchParams }: { params: Promise<{ section?: string; id?: string }>; searchParams:Promise<{action?:string}> }) {
  const session = await requireSession();
  const { section, id } = await params;
  const { action } = await searchParams;
  if (section && !["members", "applications", "products"].includes(section)) notFound();
  if (!session.permissions.includes("membership.read")) return <main className="p-8">Ask your administrator for access.</main>;
  if (section === "products") return <MembershipProducts permissions={session.permissions} />;
  if (!section) redirect("/membership/members");
  return <MembershipWorkspace key={`${section}-${id ?? "list"}`} recordId={id} initialAction={action} permissions={session.permissions} section={section} />;
}
