import { Suspense } from "react";
import { PortalLayout } from "@/features/portal/portal-layout";

export default function Layout({ children }: { children: React.ReactNode }) {
  return <><Suspense fallback={<p role="status" className="p-6">Loading your portal…</p>}><PortalLayout /></Suspense>{children}</>;
}
