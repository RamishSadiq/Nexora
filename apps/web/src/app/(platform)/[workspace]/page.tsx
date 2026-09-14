import { ArrowLeft, Boxes, Construction } from "lucide-react";
import Link from "next/link";
import { notFound } from "next/navigation";

const workspaces: Record<string, { name: string; description: string }> = {
  relationships: { name: "Relationships", description: "Contacts, accounts and the complete relationship picture." },
  membership: { name: "Membership", description: "Applications, subscriptions, renewals and member journeys." },
  events: { name: "Events & learning", description: "Events, registrations, training, examinations and outcomes." },
  finance: { name: "Sales & finance", description: "Products, orders, invoices, receipts and reconciliation." },
  engagement: { name: "Engagement", description: "Marketing, committees, groups and fundraising." },
  work: { name: "Work", description: "Tasks, imports, exports and operational workflows." },
  insights: { name: "Insights", description: "Queries, dashboards and governed reporting." },
  settings: { name: "Administration", description: "People, access, configuration and platform controls." },
};

export function generateStaticParams() {
  return Object.keys(workspaces).map((workspace) => ({ workspace }));
}

export default async function WorkspacePage({ params }: PageProps<"/[workspace]">) {
  const { workspace } = await params;
  const details = workspaces[workspace];
  if (!details) notFound();

  return (
    <main className="page-enter mx-auto grid min-h-[calc(100vh-76px)] max-w-5xl place-items-center p-6">
      <section className="w-full rounded-[1.5rem] border border-[#e3e7ed] bg-white p-8 text-center shadow-[0_14px_40px_rgba(20,34,66,.06)] sm:p-12">
        <span className="mx-auto grid size-14 place-items-center rounded-2xl bg-[#edf1ff] text-[#405bd8]"><Boxes className="size-6" /></span>
        <span className="mt-6 inline-flex items-center gap-2 rounded-full bg-[#fff4df] px-3 py-1.5 text-xs font-semibold text-[#a86919]"><Construction className="size-3.5" />Scheduled module</span>
        <h1 className="mt-4 text-3xl font-semibold tracking-[-0.045em] text-[#17213a]">{details.name}</h1>
        <p className="mx-auto mt-3 max-w-lg text-sm leading-6 text-[#748093]">{details.description} This workspace boundary is ready and will be implemented in its delivery stage.</p>
        <Link href="/dashboard" className="mt-7 inline-flex h-11 items-center gap-2 rounded-xl border border-[#dfe4ec] px-4 text-sm font-semibold text-[#405bd8] hover:bg-[#f7f8fc]"><ArrowLeft className="size-4" />Back to home</Link>
      </section>
    </main>
  );
}
