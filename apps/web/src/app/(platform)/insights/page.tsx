import {requireSession} from "@/lib/auth/session";
import {InsightsWorkspace} from "@/features/insights/insights-workspace";
export default async function Page(){const s=await requireSession();if(!s.permissions.includes("insights.read"))return <main className="p-8">Ask your administrator for access.</main>;return <InsightsWorkspace canExport={s.permissions.includes("insights.export")}/>;}
