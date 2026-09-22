import Link from "next/link";
import type { Route } from "next";
import { notFound } from "next/navigation";
import { requireSession } from "@/lib/auth/session";
import { attributeEntities } from "@/features/shared/attribute-entities";
const controllers = [
  { slug: "design-controller", title: "Design controller", description: "Arrange record sections." },
  { slug: "attributes-controller", title: "Attributes controller", description: "Manage typed custom attributes." },
  { slug: "rules-controller", title: "Rules controller", description: "Calculate attributes when records are saved." },
];
export default async function AutomationModulePage({ params }: { params: Promise<{ module: string }> }) {
  await requireSession();
  const { module } = await params;
  if (!attributeEntities[module]) notFound();
  return <main className="mx-auto max-w-5xl space-y-6 p-5 sm:p-8">
    <Link href="/automations" className="text-indigo-700 underline">All automations</Link>
    <h1 className="text-3xl font-semibold capitalize">{module.replaceAll("-", " ")} automations</h1>
    <div className="grid gap-4 md:grid-cols-3">{controllers.map(c => <Link key={c.slug} href={`/automations/${module}/${c.slug}` as Route} className="rounded-xl border bg-white p-6 hover:border-indigo-400"><h2 className="font-semibold">{c.title}</h2><p className="mt-2 text-sm text-slate-600">{c.description}</p></Link>)}</div>
  </main>;
}
