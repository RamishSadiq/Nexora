import { requireSession } from "@/lib/auth/session";
import { EventsWorkspace } from "@/features/events/events-workspace";
export default async function EventsPage(){const session=await requireSession();if(!session.permissions.includes("events.read"))return <main className="p-8"><h1 className="text-2xl font-semibold">Events and learning</h1><p className="mt-4">Ask your administrator for access.</p></main>;return <EventsWorkspace permissions={session.permissions}/>;}
