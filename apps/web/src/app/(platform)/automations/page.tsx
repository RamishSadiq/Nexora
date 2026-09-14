import { requireSession } from "@/lib/auth/session";
import Link from "next/link";
import { Workflow } from "lucide-react";

const automations = [{ slug: "design-controller", name: "Design controller", description: "Arrange sections and set the layout shown on an entity." }, { slug: "attributes-controller", name: "Attributes controller", description: "Add or remove attributes shown on entity forms." }];

export default async function AutomationsPage() {
  await requireSession();
  return <main className="page-enter mx-auto max-w-6xl space-y-6 p-5 text-slate-800 sm:p-8"><header><p className="text-xs font-semibold uppercase tracking-[0.16em] text-indigo-600">Administration</p><h1 className="mt-2 text-3xl font-semibold tracking-tight text-slate-950">Automations</h1><p className="mt-2 text-sm text-slate-600">Choose an automation type first, then select the entities it should apply to.</p></header><section className="grid gap-4 sm:grid-cols-2" aria-label="Available automations">{automations.map(automation => <Link key={automation.slug} href={`/automations/type/${automation.slug}`} className="group rounded-xl border border-slate-200 bg-white p-6 text-left shadow-sm transition hover:-translate-y-0.5 hover:border-indigo-300 hover:shadow-md"><span className="grid size-10 place-items-center rounded-lg bg-indigo-50 text-indigo-600"><Workflow className="size-5" /></span><h2 className="mt-4 font-semibold text-slate-900">{automation.name}</h2><p className="mt-1 text-sm text-slate-500">{automation.description}</p></Link>)}</section></main>;
}
