"use client";
import { useEffect, useState } from "react";
import { attributeEntities } from "./attribute-entities";
import { moduleApi } from "./module-api";

export function useAttributeEntities(module: string, initial: string[] = []) {
  const [catalog, setCatalog] = useState<{ module: string; types: string[] } | null>(null);
  useEffect(() => {
    if (!module) return;
    let active = true;
    let request = 0;
    const load = () => {
      const current = ++request;
      void moduleApi<string[]>(module, "/attribute-entities").then(types => {
        if (active && current === request) setCatalog({ module, types });
      }).catch(() => { /* The shared catalogue remains available when the request fails. */ });
    };
    load(); window.addEventListener("focus", load);
    return () => { active = false; window.removeEventListener("focus", load); };
  }, [module]);
  return catalog?.module === module ? catalog.types : initial.length ? initial : [...new Set(Object.values(attributeEntities).filter(item => item.api === module).flatMap(item => item.types))];
}
