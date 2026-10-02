import { notFound, redirect } from "next/navigation";
import { requireSession } from "@/lib/auth/session";
import { FinanceWorkspace } from "@/features/finance/finance-workspace";

export default async function Page({ params }: { params: Promise<{ section?: string }> }) {
  const session = await requireSession();
  const { section } = await params;
  if (section && !["orders", "invoices", "receipts", "catalogue"].includes(section)) notFound();
  if (!session.permissions.includes("finance.read")) return <main className="p-8">Ask your administrator for access.</main>;
  if (!section) redirect("/finance/orders");
  return <FinanceWorkspace permissions={session.permissions} section={section} />;
}
