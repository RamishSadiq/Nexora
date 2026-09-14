import { requireSession } from "@/lib/auth/session";
import { CrmWorkspace } from "@/features/crm/crm-workspace";
export default async function RelationshipsPage() {
  const session = await requireSession();
  if (!session.permissions.includes("crm.read")) return <main className="p-8"><h1 className="text-2xl font-semibold">Relationships</h1><p className="mt-4">Ask your administrator for CRM access.</p></main>;
  return <CrmWorkspace fixedKind permissions={session.permissions} initial={{ kind: "contact", search: "", status: "", sort: "name", archived: false, page: 1 }} initialRecord="" />;
}
