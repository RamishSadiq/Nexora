import type { Entity } from "./workflow-types";

export function workflowConditionFields(entity?: Entity, entities: Entity[] = []) {
  return [...(entity?.fields ?? []), ...(entity?.relations ?? []).filter(relation => !relation.many).flatMap(relation =>
    (entities.find(item => item.key === relation.entityKey)?.fields ?? []).map(field => ({
      ...field, key: `related.${relation.key}.${field.key}`, title: `${relation.key} · ${field.title}`
    })))];
}
