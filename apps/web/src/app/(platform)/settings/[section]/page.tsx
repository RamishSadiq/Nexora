import { notFound } from "next/navigation";
import { requireSession } from "@/lib/auth/session";
import { AccessWorkspace } from "@/features/administration/access-workspace";
export default async function Page({params}: {params: Promise<{section: string}>}) {
 const session = await requireSession(); const {section} = await params;
 if (section !== "users" && section !== "security-groups") notFound();
 if (!session.permissions.includes("identity.read")) return <main className="p-8">Ask your administrator for access.</main>;
 return <AccessWorkspace key={section} section={section} canManage={session.permissions.includes("identity.manage")} currentUserId={session.userId} />;
}
