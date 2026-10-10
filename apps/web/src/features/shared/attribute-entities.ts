import catalog from "../../../../../shared/automation-catalog.json";
export const attributeEntities: Record<string, { api: string; types: string[] }> = catalog.modules;
export const entityLabel = (type: string) => type.replace(/([a-z])([A-Z])/g, "$1 $2");
export const sectionDefinitions = (kind: string) => catalog.sections[kind as keyof typeof catalog.sections] ?? catalog.sections.default;
export function reconcileSections(saved: string[], available: string[]) {
  return [...new Set([...saved.filter(section => available.includes(section)), ...available])];
}
