import { notFound } from "next/navigation";
import { MemberPortal, type PortalSection } from "@/features/portal/member-portal";

const sections = ["memberships", "applications", "organisations", "profile", "notifications"];
export const metadata = { title: "Member portal", robots: { index: false, follow: false } };
export default async function PortalSectionPage({ params }: { params: Promise<{ section: string }> }) {
  const { section } = await params;
  if (!sections.includes(section)) notFound();
  return <MemberPortal key={section} section={section as PortalSection} />;
}
