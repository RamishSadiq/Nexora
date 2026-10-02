import { notFound } from "next/navigation";
import { requireSession } from "@/lib/auth/session";
import { setupKinds } from "@/features/membership/setup-kinds";
import { MembershipSetup } from "@/features/membership/membership-setup";
export default async function Page({params}: {params: Promise<{kind: string}>}) {
 const session = await requireSession(); const {kind} = await params;
 const entry = setupKinds.find(row => row[0] === kind); if (!entry) notFound();
 if (!session.permissions.includes("membership.read")) return <main className="p-8">Ask your administrator for access.</main>;
 return <MembershipSetup key={kind} kind={kind} label={entry[1]} canEdit={session.permissions.includes("membership.manage")} />;
}
