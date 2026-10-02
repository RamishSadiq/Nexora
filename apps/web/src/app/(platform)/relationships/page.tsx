import { EntityHeading } from "@/components/ui/entity-heading";
import { requireSession } from "@/lib/auth/session";
import { CrmWorkspace } from "@/features/crm/crm-workspace";
export default async function RelationshipsPage() {
  const session = await requireSession();
  if (!session.permissions.includes("crm.read")) return <main className="p-8"><EntityHeading>Relationships</EntityHeading><p className="mt-4">Ask your administrator for CRM access.</p></main>;
  return <CrmWorkspace currentUserId={session.userId} fixedKind permissions={session.permissions} initial={{ kind: "contact", search: "", status: "", sort: "name", archived: false, page: 1 }} initialRecord="" />;
}
