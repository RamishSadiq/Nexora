"use client";
import { createContext, useContext, useId, type ReactNode } from "react";
import { AttributeInputs, attributeValues, useAttributeFields } from "./attribute-fields";
import { buttonClass } from "./module-ui";
import type { Field, FieldValue, RecordRow } from "../crm/types";

type Placement = { fields: Field[]; values: FieldValue[]; targets: string[]; record: RecordRow; formId: string; disabled: boolean; editable: boolean };
const PlacementContext = createContext<Placement | null>(null);

export function RecordAttributeSections({ record, values, editable, busy, onSave, children }: { record: RecordRow; values: FieldValue[]; editable: boolean; busy: boolean; onSave: (attributes: Record<string, unknown>) => void; children: ReactNode }) {
  const data = useAttributeFields(record.kind);
  const formId = useId();
  return <PlacementContext.Provider value={{ fields: data.fields, values, targets: data.targets, record, formId, disabled: !editable || busy, editable }}>
    {data.error && <p role="alert" className="text-sm text-red-700">Fields could not be loaded: {data.error}</p>}
    {children}
    <form id={formId} aria-label="Save record fields" onSubmit={event => {
      event.preventDefault();
      if (data.loading || data.error || !editable || busy) return;
      onSave(attributeValues(Object.fromEntries(new FormData(event.currentTarget).entries()) as Record<string, string>));
    }} />
  </PlacementContext.Provider>;
}

// Inputs explicitly belong to the record-fields form, even when a section contains another form.
export function SectionAttributes({ section }: { section: string }) {
  const placement = useContext(PlacementContext);
  if (!placement) return null;
  const fields = placement.fields.filter(field => (field.section || "General Information") === section);
  if (!fields.length) return null;
  return <div className="mt-4 space-y-3 border-t border-slate-200 pt-4" aria-label={`${section} fields`}>
    <div className="grid gap-4 sm:grid-cols-2"><AttributeInputs fields={fields} values={placement.values} targets={placement.targets} recordId={placement.record.id} formId={placement.formId} disabled={placement.disabled} revision={placement.record.version} /></div>
    {placement.editable && <button type="submit" form={placement.formId} className={buttonClass} disabled={placement.disabled}>Save fields</button>}
  </div>;
}
