"use client";
import { useEffect, useState } from "react";
import { crm } from "../crm/api";
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

export function AttributeFields({ kind, recordId }: { kind: string; recordId?: string }) {
  const [fields, setFields] = useState<Field[]>([]);
  const [values, setValues] = useState<FieldValue[]>([]);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  useEffect(() => {
    let active = true;
    const load = () => {
      void Promise.all([crm<Field[]>("/fields"), recordId ? crm<Detail>(`/records/${recordId}`) : Promise.resolve(null)])
        .then(([definitions, detail]) => { if (active) { setFields(definitions.filter(f => f.kind === kind)); setValues(detail?.fields ?? []); setError(""); setLoading(false); } })
        .catch(e => { if (active) { setError(e.message); setLoading(false); } });
    };
    load();
    window.addEventListener("focus", load);
    return () => { active = false; window.removeEventListener("focus", load); };
  }, [kind, recordId]);
  if (loading || error) return <div className="sm:col-span-2" role={error ? "alert" : "status"}>
    {error || "Loading attributes…"}
    <input aria-label="Attributes unavailable" required value="" onChange={() => {}} className="sr-only" />
  </div>;
  return fields.map(field => {
    const value = values.find(v => v.definitionId === field.id);
    const name = `attribute:${field.id}:${field.dataType}`;
    return <Label key={field.id} label={field.name}>
      {field.dataType === "boolean" ? <select className={inputClass} name={name} defaultValue={value?.booleanValue == null ? "" : String(value.booleanValue)}>
        <option value="">Not set</option><option value="true">Yes</option><option value="false">No</option>
      </select> : <input className={inputClass} name={name} maxLength={500} type={field.dataType === "date" ? "date" : field.dataType === "number" ? "number" : "text"} step={field.dataType === "number" ? "0.0001" : undefined}
        defaultValue={field.dataType === "date" ? value?.dateValue?.slice(0, 10) ?? "" : value?.textValue ?? value?.numberValue ?? ""} />}
    </Label>;
  });
}
