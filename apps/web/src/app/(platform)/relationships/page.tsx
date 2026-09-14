import { requireSession } from "@/lib/auth/session";
import { CrmWorkspace } from "@/features/crm/crm-workspace";
export default async function RelationshipsPage({ searchParams }: { searchParams: Promise<Record<string, string | string[] | undefined>> }) {
  const session = await requireSession();
  if (!session.permissions.includes("crm.read")) return <main className="p-8"><h1 className="text-2xl font-semibold">Relationships</h1><p className="mt-4">Ask your administrator for CRM access.</p></main>;
  const params = await searchParams;
  const value = (key: string) => typeof params[key] === "string" ? params[key] as string : "";
  return <CrmWorkspace permissions={session.permissions} initial={{ kind: value("kind") === "account" ? "account" : "contact", search: value("search").slice(0, 160), status: ["active", "prospect", "inactive"].includes(value("status")) ? value("status") : "", sort: ["-name", "updated"].includes(value("sort")) ? value("sort") : "name", archived: value("archived") === "true", page: Math.max(1, Math.min(10000, Number(value("page")) || 1)) }} initialRecord={value("record")} />;
}
