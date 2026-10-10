import { requireSession } from "@/lib/auth/session";
import { LiveDashboard } from "@/features/insights/live-dashboard";
export default async function Dashboard(){const session=await requireSession();return <LiveDashboard name={session.displayName} allowed={session.permissions.includes("insights.read")} />;}
