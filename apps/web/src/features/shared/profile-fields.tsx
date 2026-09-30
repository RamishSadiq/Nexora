"use client";

export const contactFields = [["title", "Title"], ["firstName", "First name"], ["middleName", "Middle name"], ["lastName", "Last name"], ["previousLastName", "Previous last name"], ["postNominal", "Post-nominal letters"]] as const;
export type ContactDetails = { [K in typeof contactFields[number][0]]: string | null };
export type AccountDetails = { fullName: string | null; tradingAs: string | null; dateBusinessCommenced: string | null };
export const emptyContactDetails: ContactDetails = { title: "", firstName: "", middleName: "", lastName: "", previousLastName: "", postNominal: "" };

export function ContactNameFields({ value, onChange, disabled = false, inputClass }: { value: ContactDetails; onChange: (value: ContactDetails) => void; disabled?: boolean; inputClass: string }) {
  return <fieldset disabled={disabled} className="grid gap-4 sm:grid-cols-2"><legend className="mb-3 font-semibold">Personal name details</legend>{contactFields.map(([key, label]) => <label key={key} className="block text-sm">{label}<input className={inputClass} value={value[key] ?? ""} maxLength={160} onChange={e => onChange({ ...value, [key]: e.target.value })} /></label>)}</fieldset>;
}
