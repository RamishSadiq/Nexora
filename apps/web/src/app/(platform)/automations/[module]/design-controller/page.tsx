"use client";
import Link from "next/link";
import { useState } from "react";
import { ArrowLeft, GripVertical } from "lucide-react";

export default function DesignControllerPage({ params }: { params: Promise<{ module: string }> }) {
  const [module, setModule] = useState("");
  const [sections, setSections] = useState<string[]>([]);
  useState(() => { void params.then(p => { setModule(p.module); const key = `nexora-layout-${p.module}`; const saved = localStorage.getItem(key); setSections(saved ? JSON.parse(saved) : p.module === "contacts" ? ["Contact details", "Addresses", "Communication methods", "Related records", "Notes", "Tags", "Files", "Custom fields", "Activity timeline"] : ["Account details", "Addresses", "Communication methods", "Related records", "Notes", "Tags", "Files", "Custom fields", "Activity timeline"]); }); });
  if (!module || sections.length === 0) return null;
  const name = module.replaceAll("-", " ").replace(/\b\w/g, c => c.toUpperCase());
  return <main className="page-enter mx-auto max-w-3xl space-y-6 p-5 text-slate-800 sm:p-8"><Link href={`/automations/${module}`} className="inline-flex items-center gap-2 text-sm font-medium text-indigo-700 hover:underline"><ArrowLeft className="size-4" />{name} automations</Link><header><p className="text-xs font-semibold uppercase tracking-[0.16em] text-indigo-600">Design controller</p><h1 className="mt-2 text-3xl font-semibold tracking-tight text-slate-950">Arrange {name} sections</h1><p className="mt-2 text-sm text-slate-600">Drag sections to set the order people see on this module.</p></header><ol className="space-y-2 rounded-xl border border-slate-200 bg-white p-4">{sections.map((section, i) => <li key={section} draggable onDragOver={e => e.preventDefault()} onDrop={() => { const next = [...sections]; const [item] = next.splice(i, 1); next.splice(Math.max(0, i - 1), 0, item); setSections(next); localStorage.setItem(`nexora-layout-${module}`, JSON.stringify(next)); }} className="flex cursor-grab items-center gap-3 rounded-lg border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-medium text-slate-800 active:cursor-grabbing"><GripVertical className="size-4 text-slate-400" />{section}</li>)}</ol></main>;
}
