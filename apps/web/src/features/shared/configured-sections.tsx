"use client";
import { Children, isValidElement, useEffect, useState, type ReactNode } from "react";
import { moduleApi } from "./module-api";
import type { AutomationSettings } from "./automation-controller";

export function ConfiguredSections({ module, kind, children, className = "grid gap-4" }: { module: string; kind: string; children: ReactNode; className?: string }) {
  const [layout, setLayout] = useState<{ key: string; sections: string[] } | null>(null);
  const [error, setError] = useState("");
  useEffect(() => {
    let active = true;
    const load = () => { void moduleApi<AutomationSettings>(module, `/automations/${kind}`).then(settings => {
      if (active) { setLayout({ key: `${module}/${kind}`, sections: settings.sections }); setError(""); }
    }).catch(() => { if (active) setError("Saved layout could not be loaded. Showing the default order."); }); };
    load(); window.addEventListener("focus", load); window.addEventListener("nexora-automations", load);
    const channel = new BroadcastChannel("nexora-automations"); channel.onmessage = load;
    return () => { active = false; channel.close(); window.removeEventListener("focus", load); window.removeEventListener("nexora-automations", load); };
  }, [module, kind]);
  const sections = layout?.key === `${module}/${kind}` ? layout.sections : [];
  const position = (element: ReactNode) => {
    if (!isValidElement<{ title?: string; "data-section"?: string }>(element)) return sections.length;
    const index = sections.indexOf(element.props["data-section"] ?? element.props.title ?? "");
    return index < 0 ? sections.length : index;
  };
  // Render in saved order so keyboard and visual order agree.
  return <>{error && <p role="status" className="text-sm text-amber-800">{error}</p>}<div className={className}>{Children.toArray(children).sort((a, b) => position(a) - position(b))}</div></>;
}
