import { AutomationController } from "@/features/shared/automation-controller";
export default async function DesignControllerPage({ params }: { params: Promise<{ module: string }> }) {
  const { module } = await params;
  return <AutomationController module={module} mode="design" />;
}
