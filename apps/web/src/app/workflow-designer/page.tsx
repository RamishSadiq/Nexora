import Link from "next/link";
import type { Metadata } from "next";
import { requireSession } from "@/lib/auth/session";
import { WorkflowDesignerWorkspace } from "@/features/workflows/workflow-designer-workspace";

export const metadata: Metadata = { title: "WFL Designer" };

export default async function WorkflowDesignerPage() {
  const session = await requireSession();
  if (!session.permissions.includes("workflows.read")) {
    return <main className="mx-auto max-w-xl p-8 text-slate-800"><h1 className="text-2xl font-semibold">WFL Designer</h1><p className="mt-3">Workflow access is required. Ask your administrator to update your security group.</p><Link href="/dashboard" className="mt-6 inline-block text-blue-700 underline">Return to Nexora</Link></main>;
  }
  return <WorkflowDesignerWorkspace />;
}
