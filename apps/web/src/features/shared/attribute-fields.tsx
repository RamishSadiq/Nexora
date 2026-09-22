"use client";
import { useEffect, useState } from "react";
import { crm } from "../crm/api";
import { moduleApi } from "./module-api";
import type { AutomationSettings } from "./automation-controller";
import type { Detail, Field, FieldValue } from "../crm/types";
import { Field as Label, inputClass } from "./module-ui";

export function attributeValues(data: Record<string, string>) {
  const values: Record<string, unknown> = {};
  for (const [key, value] of Object.entries(data)) {
    if (!key.startsWith("attribute:")) continue;
    const [, id, type] = key.split(":");
    values[id] = {
      textValue: type === "text" && value ? value : null,
      numberValue: type === "number" && value !== "" ? Number(value) : null,
      booleanValue: type === "boolean" && value !== "" ? value === "true" : null,
      dateValue: type === "date" && value ? `${value}T00:00:00Z` : null,
    };
    delete data[key];
  }
  return values;
}

export function AttributeFields({ kind, recordId, module = "crm" }: { kind: string; recordId?: string; module?: string }) {
  const [fields, setFields] = useState<Field[]>([]);
  const [values, setValues] = useState<FieldValue[]>([]);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  const [loadedFor, setLoadedFor] = useState("");
  const [targets, setTargets] = useState<string[]>([]);
  useEffect(() => {
    let active = true;
    const load = () => {
      const definitions = module === "crm" ? crm<Field[]>("/fields").then(rows => rows.filter(f => f.kind === kind)) : moduleApi<Field[]>(module, `/attributes/${kind}/definitions`);
      const values = !recordId ? Promise.resolve([]) : module === "crm" ? crm<Detail>(`/records/${recordId}`).then(detail => detail.fields) : moduleApi<FieldValue[]>(module, `/attributes/${kind}/records/${recordId}`);
      void Promise.all([definitions, values, moduleApi<AutomationSettings>(module, `/automations/${kind}`)])
        .then(([definitions, values, settings]) => { if (active) { setFields(definitions); setValues(values); setTargets(settings.rules.map(r => r.target)); setLoadedFor(`${module}/${kind}/${recordId ?? "new"}`); setError(""); setLoading(false); } })
        .catch(e => { if (active) { setError(e.message); setLoading(false); } });
    };
    load();
    window.addEventListener("focus", load);
    const channel = new BroadcastChannel("nexora-attributes");
    channel.onmessage = load;
    const automationChannel = new BroadcastChannel("nexora-automations"); automationChannel.onmessage = load;
    return () => { active = false; window.removeEventListener("focus", load); channel.close(); automationChannel.close(); };
  }, [kind, recordId, module]);
  if (loading || loadedFor !== `${module}/${kind}/${recordId ?? "new"}` || error) return <div className="sm:col-span-2" role={error ? "alert" : "status"}>
    {error || "Loading attributes…"}
    <input aria-label="Attributes unavailable" required value="" onChange={() => {}} className="sr-only" />
  </div>;
  return fields.map(field => {
    const value = values.find(v => v.definitionId === field.id);
    const name = `attribute:${field.id}:${field.dataType}`;
    const calculated = targets.includes(field.id);
    return <Label key={`${field.id}-${field.dataType}-${recordId ?? "new"}-${calculated}`} label={field.name}>
      {field.dataType === "boolean" ? <select aria-label={field.name} disabled={calculated} className={inputClass} name={name} defaultValue={value?.booleanValue == null ? "" : String(value.booleanValue)}>
        <option value="">Not set</option><option value="true">Yes</option><option value="false">No</option>
      </select> : <input aria-label={field.name} readOnly={calculated} className={inputClass} name={name} maxLength={500} type={field.dataType === "date" ? "date" : field.dataType === "number" ? "number" : "text"} step={field.dataType === "number" ? "0.0001" : undefined}
        defaultValue={field.dataType === "date" ? value?.dateValue?.slice(0, 10) ?? "" : value?.textValue ?? value?.numberValue ?? ""} />}
      {calculated && <span className="text-xs text-slate-500">Calculated when saved</span>}
    </Label>;
  });
}
