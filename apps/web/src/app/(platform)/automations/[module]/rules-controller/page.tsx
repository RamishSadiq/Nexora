import { AutomationController } from "@/features/shared/automation-controller";
export default async function RulesControllerPage({ params }: { params: Promise<{ module: string }> }) {
  const { module } = await params;
  return <AutomationController module={module} mode="rules" />;
}
