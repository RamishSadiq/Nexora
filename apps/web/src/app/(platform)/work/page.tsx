import { notFound, redirect } from "next/navigation";
import { requireSession } from "@/lib/auth/session";
import { OperationsWorkspace } from "@/features/operations/operations-workspace";

export default async function Page({ params }: { params: Promise<{ section?: string }> }) {
  const session = await requireSession();
  const { section } = await params;
  if (section && !["items", "notifications", "imports"].includes(section)) notFound();
  if (!session.permissions.includes("work.read")) return <main className="p-8">Ask your administrator for access.</main>;
  if (!section) redirect("/work/items");
  return <OperationsWorkspace currentUserId={session.userId} key={section} permissions={session.permissions} module="work" section={section} />;
}
