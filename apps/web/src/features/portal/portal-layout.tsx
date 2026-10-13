"use client";

import { useSearchParams, useSelectedLayoutSegments } from "next/navigation";
import { MemberPortal } from "./member-portal";

// Keep the branded shell mounted while only the active member section changes.
export function PortalLayout() {
  const segments = useSelectedLayoutSegments();
  const searchParams = useSearchParams();
  const applicationId = segments[0] === "applications" ? segments[1] : undefined;
  const organisations = searchParams.getAll("organisation");
  return <MemberPortal section={segments[0] ?? "dashboard"} applicationId={applicationId} accountId={organisations.length === 1 ? organisations[0] : undefined} />;
}
