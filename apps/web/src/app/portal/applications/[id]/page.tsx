import { MemberPortal } from "@/features/portal/member-portal";

export const metadata = { title: "My application | Member portal", robots: { index: false, follow: false } };

export default async function ApplicationPage({ params, searchParams }: { params: Promise<{ id: string }>; searchParams: Promise<{ organisation?: string | string[] }> }) {
  const { id } = await params;
  const { organisation } = await searchParams;
  return <MemberPortal applicationId={id} accountId={typeof organisation === "string" ? organisation : undefined} />;
}
