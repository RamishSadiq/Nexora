import Link from "next/link";
import { requireSession } from "@/lib/auth/session";
import { ArrowLeft, GripVertical } from "lucide-react";

export default async function DesignControllerPage({ params }: { params: Promise<{ module: string }> }) {
  await requireSession();
  const { module } = await params;
  const name = module.replaceAll("-", " ").replace(/\b\w/g, c => c.toUpperCase());
  const sections = module === "contacts" ? ["Contact details", "Addresses", "Communication methods", "Related records", "Notes", "Tags", "Files", "Custom fields", "Activity timeline"] : ["Account details", "Addresses", "Communication methods", "Related records", "Notes", "Tags", "Files", "Custom fields", "Activity timeline"];
  return <main className="page-enter mx-auto max-w-3xl space-y-6 p-5 text-slate-800 sm:p-8"><Link href={`/automations/${module}`} className="inline-flex items-center gap-2 text-sm font-medium text-indigo-700 hover:underline"><ArrowLeft className="size-4" />{name} automations</Link><header><p className="text-xs font-semibold uppercase tracking-[0.16em] text-indigo-600">Design controller</p><h1 className="mt-2 text-3xl font-semibold tracking-tight text-slate-950">Arrange {name} sections</h1><p className="mt-2 text-sm text-slate-600">Drag sections to set the order people see on this module. Saving will be enabled when the layout rules are connected.</p></header><ol className="space-y-2 rounded-xl border border-slate-200 bg-white p-4">{sections.map(section => <li key={section} draggable className="flex cursor-grab items-center gap-3 rounded-lg border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-800 active:cursor-grabbing"><GripVertical className="size-4 text-slate-400" />{section}</li>)}</ol></main>;
}
