import { EntityHeading } from "@/components/ui/entity-heading";
import Link from "next/link";
import type { Route } from "next";
import { requireSession } from "@/lib/auth/session";
import { setupKinds } from "@/features/membership/setup-kinds";
export default async function Page() {
 const session = await requireSession();
 if (!session.permissions.includes("membership.read")) return <main className="p-8">Ask your administrator for access.</main>;
 return <main className="space-y-5 p-7"><EntityHeading>Setup</EntityHeading><p>Membership lookup values</p><div className="grid gap-3 sm:grid-cols-2">{setupKinds.map(([kind,label]) => <Link className="rounded-xl border border-blue-200 bg-white p-4 text-blue-700" href={`/membership/setup/${kind}` as Route} key={kind}>{label}</Link>)}</div></main>;
}
