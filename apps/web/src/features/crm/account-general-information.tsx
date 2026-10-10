"use client";

const fields = [["sourceAccountId", "Source Account ID"], ["tradingStatus", "Trading status"], ["accountType", "Account type"], ["industrySector", "Industry sector"], ["venue", "Recorded venue"], ["marketingSource", "Marketing source"], ["turnoverRange", "Turnover range"], ["employeesRange", "Employees range"], ["region", "Region"], ["branch", "Branch"], ["oldReference", "Old reference"]] as const;
export type AccountGeneralInformation = { [K in typeof fields[number][0]]: string | null } & { numberOfEmployees: number | null };
export const emptyAccountGeneralInformation: AccountGeneralInformation = { sourceAccountId: "", tradingStatus: "", accountType: "", industrySector: "", venue: "", marketingSource: "", turnoverRange: "", employeesRange: "", region: "", branch: "", oldReference: "", numberOfEmployees: null };

export function AccountGeneralFields({ value, onChange, disabled, inputClass }: { value: AccountGeneralInformation; onChange: (value: AccountGeneralInformation) => void; disabled: boolean; inputClass: string }) {
  return <fieldset disabled={disabled} className="grid gap-4 sm:grid-cols-2"><legend className="mb-3 font-semibold">Account general information</legend>
    {fields.map(([key, label]) => <label key={key} className="block text-sm">{label}<input className={inputClass} maxLength={160} value={value[key] ?? ""} onChange={e => onChange({ ...value, [key]: e.target.value })}/></label>)}
    <label className="block text-sm">Number of employees<input className={inputClass} type="number" min="0" max="2147483647" step="1" value={value.numberOfEmployees ?? ""} onChange={e => onChange({ ...value, numberOfEmployees: e.target.value === "" ? null : Number(e.target.value) })}/></label>
    <p className="text-sm text-slate-600 sm:col-span-2">Source Account ID and old reference retain previous-system identifiers. Trading status is separate from CRM status. Ranges and recorded venue preserve source values; they do not infer employee counts or create event venues. Owner and main Contact are managed separately.</p>
  </fieldset>;
}
