import Link from "next/link";
import { requireSession } from "@/lib/auth/session";
import { Workflow, ArrowLeft } from "lucide-react";

export default async function AutomationModulePage({ params }: { params: Promise<{ module: string }> }) {
  await requireSession();
  const moduleName = (await params).module.replaceAll("-", " ").replace(/\b\w/g, c => c.toUpperCase());
  return <main className="page-enter mx-auto max-w-5xl space-y-6 p-5 text-slate-800 sm:p-8"><Link href="/automations" className="inline-flex items-center gap-2 text-sm font-medium text-indigo-700 hover:underline"><ArrowLeft className="size-4" />All automations</Link><header><p className="text-xs font-semibold uppercase tracking-[0.16em] text-indigo-600">{moduleName}</p><h1 className="mt-2 text-3xl font-semibold tracking-tight text-slate-950">{moduleName} automations</h1><p className="mt-2 text-sm text-slate-600">Choose an automation to configure.</p></header><Link href={`/automations/${(await params).module}/design-controller`} className="block max-w-xl rounded-xl border border-indigo-200 bg-white p-6 shadow-sm transition hover:-translate-y-0.5 hover:border-indigo-400 hover:shadow-md"><span className="grid size-10 place-items-center rounded-lg bg-indigo-600 text-white"><Workflow className="size-5" /></span><h2 className="mt-4 text-lg font-semibold text-slate-900">Design controller</h2><p className="mt-1 text-sm text-slate-600">Arrange the sections shown in this module by dragging them into your preferred order.</p></Link></main>;
}
