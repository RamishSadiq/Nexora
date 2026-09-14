import { requireSession } from "@/lib/auth/session";
import { CrmWorkspace } from "@/features/crm/crm-workspace";
export default async function ContactsPage() { const session = await requireSession(); if (!session.permissions.includes("crm.read")) return <main className="p-8">Ask your administrator for CRM access.</main>; return <CrmWorkspace fixedKind permissions={session.permissions} initial={{ kind: "contact", search: "", status: "", sort: "name", archived: false, page: 1 }} initialRecord="" />; }
