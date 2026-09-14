import Link from "next/link";`r`nimport type { Route } from "next";
import { requireSession } from "@/lib/auth/session";
import { ArrowLeft, Workflow } from "lucide-react";
const entities = ["Contacts", "Accounts", "Membership", "Applications", "Events and learning", "Sales and finance", "Engagement", "Work"];
export default async function AutomationTypePage({ params }: { params: Promise<{ automation: string }> }) { await requireSession(); const { automation } = await params; const title = automation.replaceAll("-", " ").replace(/\b\w/g, c => c.toUpperCase()); return <main className="page-enter mx-auto max-w-6xl space-y-6 p-5 text-slate-800 sm:p-8"><Link href="/automations" className="inline-flex items-center gap-2 text-sm font-medium text-indigo-700 hover:underline"><ArrowLeft className="size-4" />All automations</Link><header><p className="text-xs font-semibold uppercase tracking-[0.16em] text-indigo-600">{title}</p><h1 className="mt-2 text-3xl font-semibold tracking-tight text-slate-950">Choose entities</h1><p className="mt-2 text-sm text-slate-600">Select the entity whose layout or attributes you want to configure.</p></header><section className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">{entities.map(entity => <Link key={entity} href={`/automations/${entity.toLowerCase().replaceAll(" ", "-")}/${automation}` as Route} className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm hover:border-indigo-300"><Workflow className="size-5 text-indigo-600" /><h2 className="mt-3 font-semibold">{entity}</h2></Link>)}</section></main>; }



