import {requireSession} from "@/lib/auth/session";
import {OperationsWorkspace} from "@/features/operations/operations-workspace";
export default async function Page(){const s=await requireSession();if(!s.permissions.includes("work.read"))return <main className="p-8">Ask your administrator for access.</main>;return <OperationsWorkspace module="work" permissions={s.permissions}/>;}
