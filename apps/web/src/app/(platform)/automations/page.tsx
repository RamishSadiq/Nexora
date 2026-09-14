import { requireSession } from "@/lib/auth/session";
import Link from "next/link";
import { Workflow } from "lucide-react";

const modules = ["Contacts", "Accounts", "Membership", "Applications", "Events and learning", "Sales and finance", "Engagement", "Work"];

export default async function AutomationsPage() {
  await requireSession();
  return <main className="page-enter mx-auto max-w-6xl space-y-6 p-5 text-slate-800 sm:p-8"><header><p className="text-xs font-semibold uppercase tracking-[0.16em] text-indigo-600">Administration</p><h1 className="mt-2 text-3xl font-semibold tracking-tight text-slate-950">Automations</h1><p className="mt-2 text-sm text-slate-600">Choose a module to design rules that keep your workspace moving.</p></header><section className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3" aria-label="Available automation modules">{modules.map(module => <Link key={module} href={`/automations/${module.toLowerCase().replaceAll(" ", "-")}`} className="group rounded-xl border border-slate-200 bg-white p-5 text-left shadow-sm transition hover:-translate-y-0.5 hover:border-indigo-300 hover:shadow-md"><span className="grid size-10 place-items-center rounded-lg bg-indigo-50 text-indigo-600"><Workflow className="size-5" /></span><h2 className="mt-4 font-semibold text-slate-900">{module}</h2><p className="mt-1 text-sm text-slate-500">Design controller and other {module.toLowerCase()} workflows.</p></Link>)}</section></main>;
}
