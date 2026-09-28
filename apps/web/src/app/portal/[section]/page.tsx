import { MemberPortal } from "@/features/portal/member-portal";

export const metadata = { title: "Member portal", robots: { index: false, follow: false } };
export default async function PortalSectionPage({ params }: { params: Promise<{ section: string }> }) {
  const { section } = await params;
  return <MemberPortal key={section} section={section} />;
}
