import { notFound, redirect } from "next/navigation";
import { requireSession } from "@/lib/auth/session";
import { EventsWorkspace } from "@/features/events/events-workspace";

export default async function Page({ params }: { params: Promise<{ section?: string; id?: string }> }) {
  const session = await requireSession();
  const { section, id } = await params;
  if (section && !["events", "courses", "exams"].includes(section)) notFound();
  if (!session.permissions.includes("events.read")) return <main className="p-8">Ask your administrator for access.</main>;
  if (!section) redirect("/events/events");
  return <EventsWorkspace key={`${section}-${id ?? "list"}`} recordId={id} permissions={session.permissions} offeringKind={({ events: "event", courses: "course", exams: "exam" } as Record<string, string>)[section]} />;
}
