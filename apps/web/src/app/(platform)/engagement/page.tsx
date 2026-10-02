import { notFound, redirect } from "next/navigation";
import { requireSession } from "@/lib/auth/session";
import { OperationsWorkspace } from "@/features/operations/operations-workspace";

export default async function Page({ params }: { params: Promise<{ section?: string }> }) {
  const session = await requireSession();
  const { section } = await params;
  if (section && !["communities", "campaigns", "funds"].includes(section)) notFound();
  if (!session.permissions.includes("engagement.read")) return <main className="p-8">Ask your administrator for access.</main>;
  if (!section) redirect("/engagement/communities");
  return <OperationsWorkspace key={section} permissions={session.permissions} module="engagement" section={section} />;
}
