"use client";
import type { ReactNode } from "react";

export const generalFields = [["sourcePersonId", "Source Person ID"], ["contactType", "Contact type"], ["nationality", "Nationality"], ["oldReference", "Old reference"], ["region", "Region"], ["branch", "Branch"]] as const;
export type ContactGeneralInformation = { [K in typeof generalFields[number][0]]: string | null } & { remarks: string | null };
export const emptyGeneralInformation: ContactGeneralInformation = { sourcePersonId: "", contactType: "", nationality: "", oldReference: "", region: "", branch: "", remarks: "" };

export function ContactGeneralFields({ value, onChange, disabled, inputClass, imageField, hideLegend = false }: { value: ContactGeneralInformation; onChange: (value: ContactGeneralInformation) => void; disabled: boolean; inputClass: string; imageField?: ReactNode; hideLegend?: boolean }) {
  return <fieldset disabled={disabled} className="grid gap-4 sm:grid-cols-2">
    <legend className={hideLegend ? "sr-only" : "mb-3 font-semibold"}>General Information</legend>
    {imageField && <div className="sm:col-span-2">{imageField}</div>}
    {generalFields.map(([key, label]) => <label key={key} className="block text-sm">{label}<input className={inputClass} maxLength={160} value={value[key] ?? ""} onChange={e => onChange({ ...value, [key]: e.target.value })} /></label>)}
    <label className="block text-sm sm:col-span-2">Remarks<textarea className={inputClass} rows={4} maxLength={4000} value={value.remarks ?? ""} onChange={e => onChange({ ...value, remarks: e.target.value })} /></label>
  </fieldset>;
}
