import { requireSession } from "@/lib/auth/session";
import { MembershipWorkspace } from "@/features/membership/membership-workspace";
export default async function MembershipPage() {
 const session = await requireSession();
 if (!session.permissions.includes("membership.read")) return <main className="p-8"><h1 className="text-2xl font-semibold">Membership</h1><p className="mt-4">Ask your administrator for Membership access.</p></main>;
 return <MembershipWorkspace permissions={session.permissions} />;
}
