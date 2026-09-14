import {requireSession} from "@/lib/auth/session";
import {FinanceWorkspace} from "@/features/finance/finance-workspace";
export default async function FinancePage(){const s=await requireSession();if(!s.permissions.includes("finance.read"))return <main className="p-8"><h1 className="text-2xl font-semibold">Sales and finance</h1><p className="mt-4">Ask your administrator for access.</p></main>;return <FinanceWorkspace permissions={s.permissions}/>;}
