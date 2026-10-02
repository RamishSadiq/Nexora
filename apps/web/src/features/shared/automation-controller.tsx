"use client";
import { EntityHeading } from "@/components/ui/entity-heading";
import { useEffect, useRef, useState } from "react";
import Link from "next/link";
import { attributeEntities, entityLabel } from "./attribute-entities";
import { moduleApi } from "./module-api";
import { buttonClass, inputClass, primaryClass } from "./module-ui";

export type Rule = { source: string; operation: string; target: string; operand: string | null; value: string | null };
type Field = { id: string; name: string; dataType: string; writable: boolean };
export type AutomationSettings = { version: string | null; sections: string[]; availableSections: string[]; rules: Rule[]; fields: Field[]; canManage: boolean };
const functions = [{ id: "copy", name: "Copy value" }, { id: "age", name: "Calculate age" }, { id: "add", name: "Add" }, { id: "subtract", name: "Subtract" }, { id: "equals", name: "Equals" }, { id: "not-equal", name: "Not equal" }, { id: "contains", name: "Contains" }, { id: "set-when-equal", name: "Set text when equal" }];
const emptyRule: Rule = { source: "", operation: "copy", target: "", operand: null, value: null };
export function notifyAutomationChanged() {
  window.dispatchEvent(new Event("nexora-automations"));
  const channel = new BroadcastChannel("nexora-automations"); channel.postMessage("changed"); channel.close();
}
export function AutomationController({ module, mode }: { module: string; mode: "rules" | "design" }) {
  const config = attributeEntities[module];
  const [selected, setSelected] = useState("");
  const kind = config?.types.includes(selected) ? selected : config?.types[0];
  return <main className="mx-auto max-w-4xl space-y-6 p-5 text-slate-800 sm:p-8">
    <Link href={`/automations/${module}`} className="text-indigo-700 underline">Back to automations</Link>
    <EntityHeading>{mode === "rules" ? "Rules" : "Design"} controller</EntityHeading>
    {!config ? <p role="alert">This entity is unavailable.</p> : <>
      <label className="block text-sm font-medium">Entity<select className={inputClass} value={kind} onChange={e => setSelected(e.target.value)}>{config.types.map(t => <option key={t} value={t}>{entityLabel(t)}</option>)}</select></label>
      <ControllerEditor key={`${config.api}-${kind}-${mode}`} module={module} api={config.api} kind={kind} mode={mode} />
    </>}
  </main>;
}
function ControllerEditor({ module, api, kind, mode }: { module: string; api: string; kind: string; mode: "rules" | "design" }) {
  const [settings, setSettings] = useState<AutomationSettings | null>(null);
  const [notice, setNotice] = useState("");
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const [reload, setReload] = useState(0);
  const dragged = useRef<number | null>(null);
  const [rule, setRule] = useState<Rule>(emptyRule);
  useEffect(() => {
    let active = true;
    moduleApi<AutomationSettings>(api, `/automations/${kind}`).then(result => { if (active) { setSettings(result); setError(""); } }).catch(e => { if (active) setError(e.message); });
    return () => { active = false; };
  }, [api, kind, reload]);
  function update(value: Partial<AutomationSettings>) { setSettings(current => current && { ...current, ...value }); setNotice(""); }
  function move(from: number, to: number) {
    if (!settings || from === to || from < 0 || to < 0 || from >= settings.sections.length || to >= settings.sections.length) return;
    const sections = [...settings.sections]; const [item] = sections.splice(from, 1); sections.splice(to, 0, item); update({ sections });
  }
  async function save() {
    if (!settings) return;
    setBusy(true); setError(""); setNotice("");
    try {
      const result = await moduleApi<{ version: string }>(api, `/automations/${kind}`, "PUT", { version: settings.version, ...(mode === "rules" ? { rules: settings.rules } : { sections: settings.sections }) });
      setSettings({ ...settings, version: result.version }); setNotice("Changes saved for your organisation."); notifyAutomationChanged();
    } catch (e) { setError(e instanceof Error ? e.message : "Unable to save."); }
    finally { setBusy(false); }
  }
  function importPrevious() {
    if (!settings) return;
    setError(""); setNotice("");
    try {
      const raw = localStorage.getItem(`nexora-${mode === "rules" ? "rules" : "layout"}-${module}`);
      if (!raw) throw new Error("No previous settings were found in this browser.");
      const previous: unknown = JSON.parse(raw);
      if (!Array.isArray(previous)) throw new Error("Previous settings could not be read.");
      if (mode === "design") {
        const sections = [...new Set(previous.filter((s): s is string => typeof s === "string" && settings.availableSections.includes(s)))];
        if (sections.length === 0) throw new Error("The previous layout does not match this entity’s available sections.");
        update({ sections: [...sections, ...settings.availableSections.filter(s => !sections.includes(s))] });
      } else {
        const operations: Record<string, string> = { "Calculate age": "age", "Equals": "equals", "Not equal": "not-equal", "Contains": "contains", "Add": "add", "Subtract": "subtract", "Set status": "set-when-equal" };
        const rules = previous.map((r: { source?: string; fn?: string; target?: string }) => {
          const source = settings.fields.filter(f => f.name.toLowerCase() === r?.source?.toLowerCase());
          const target = settings.fields.filter(f => f.writable && f.name.toLowerCase() === r?.target?.toLowerCase());
          const operation = operations[r?.fn ?? ""];
          if (source.length !== 1 || target.length !== 1 || operation !== "age") throw new Error("A previous rule needs field mapping or an operand. Recreate it using the current fields and functions.");
          return { source: source[0].id, operation, target: target[0].id, operand: null, value: null };
        });
        update({ rules });
      }
      setNotice("Previous browser settings loaded as a draft. Review the entity and settings, then save to share them with your organisation.");
    } catch (e) { setError(e instanceof Error ? e.message : "Previous settings could not be read."); }
  }
  const source = settings?.fields.find(f => f.id === rule.source);
  const targetType = ["age", "add", "subtract"].includes(rule.operation) ? "number" : ["equals", "not-equal", "contains"].includes(rule.operation) ? "boolean" : rule.operation === "set-when-equal" ? "text" : source?.dataType;
  return <>
    {error && <p role="alert" className="text-red-700">{error}</p>}
    {!settings ? <><p role="status">{error ? "Settings could not be loaded." : "Loading settings…"}</p><button className={buttonClass} onClick={() => setReload(r => r + 1)}>Retry</button></> : <>
      {!settings.canManage && <p>You can view these settings. Editing requires module management permission.</p>}
      <fieldset disabled={busy || !settings.canManage} className="space-y-5 disabled:opacity-70">
        {mode === "design" ? <>
          <p className="text-sm text-slate-600">{api === "crm" ? "Arrange the sections in the record details view." : "Arrange sections in this entity’s Record attributes workspace."} Drag a section or use Up and Down. Changes apply across browsers after saving.</p>
          <ol className="space-y-2">{settings.sections.map((section, i) => <li key={section} draggable={settings.canManage && !busy}
            onDragStart={e => { dragged.current = i; e.dataTransfer.effectAllowed = "move"; e.dataTransfer.setData("text/plain", section); }}
            onDragEnd={() => { dragged.current = null; }} onDragOver={e => e.preventDefault()}
            onDrop={e => { e.preventDefault(); if (dragged.current !== null && settings.canManage && !busy) move(dragged.current, i); dragged.current = null; }}
            className="flex items-center gap-3 rounded-xl border bg-white p-4">
            <span className="mr-auto font-medium">{section}</span>
            <button type="button" className={buttonClass} disabled={i === 0} aria-label={`Move ${section} up`} onClick={() => move(i, i - 1)}>Up</button>
            <button type="button" className={buttonClass} disabled={i === settings.sections.length - 1} aria-label={`Move ${section} down`} onClick={() => move(i, i + 1)}>Down</button>
          </li>)}</ol>
        </> : <>
          <p className="text-sm text-slate-600">Rules calculate custom attributes whenever a record or its attributes are saved. Age is recalculated on save. Existing records update on their next save. Chained rules run in dependency order.</p>
          <form className="grid gap-4 rounded-xl border bg-white p-5 sm:grid-cols-2" onSubmit={e => { e.preventDefault(); update({ rules: [...settings.rules, rule] }); setRule(emptyRule); }}>
            <label>Source field<select required className={inputClass} value={rule.source} onChange={e => setRule({ ...rule, source: e.target.value, target: "" })}><option value="">Choose a field</option>{settings.fields.map(f => <option key={f.id} value={f.id}>{f.name} ({f.dataType})</option>)}</select></label>
            <label>Function<select className={inputClass} value={rule.operation} onChange={e => setRule({ ...rule, operation: e.target.value, target: "", operand: null, value: null })}>{functions.map(f => <option key={f.id} value={f.id}>{f.name}</option>)}</select></label>
            <label>Target attribute<select required className={inputClass} value={rule.target} onChange={e => setRule({ ...rule, target: e.target.value })}><option value="">Choose an attribute</option>{settings.fields.filter(f => f.writable && f.dataType === targetType && f.id !== rule.source).map(f => <option key={f.id} value={f.id}>{f.name} ({f.dataType})</option>)}</select></label>
            {!["copy", "age"].includes(rule.operation) && <label>{["add", "subtract"].includes(rule.operation) ? "Amount" : "Compare with"}<input required maxLength={500} className={inputClass} value={rule.operand ?? ""} placeholder={source?.dataType === "date" ? "yyyy-mm-dd" : source?.dataType === "boolean" ? "true or false" : undefined} onChange={e => setRule({ ...rule, operand: e.target.value })} /></label>}
            {rule.operation === "set-when-equal" && <label>Text when matched<input required maxLength={500} className={inputClass} value={rule.value ?? ""} onChange={e => setRule({ ...rule, value: e.target.value })} /></label>}
            <p className="text-xs text-slate-500 sm:col-span-2">Create target attributes in Attributes controller first. Empty sources clear their calculated targets. “Set text when equal” clears the target when it does not match.</p>
            <button className={buttonClass} disabled={settings.rules.length >= 50}>Add rule</button>
          </form>
          <ul className="space-y-2" aria-label="Configured rules">{settings.rules.map((r, i) => <li key={i} className="flex items-center justify-between gap-4 rounded-xl border bg-white p-4 text-sm">
            <span>{settings.fields.find(f => f.id === r.source)?.name ?? "Missing field"} → {functions.find(f => f.id === r.operation)?.name} {r.operand ?? ""}{r.value != null ? `: ${r.value}` : ""} → {settings.fields.find(f => f.id === r.target)?.name ?? "Missing attribute"}</span>
            <button type="button" className={buttonClass} aria-label={`Remove rule ${i + 1}`} onClick={() => update({ rules: settings.rules.filter((_, index) => index !== i) })}>Remove</button>
          </li>)}</ul>
          {settings.rules.length === 0 && <p>No rules configured.</p>}
        </>}
        <div className="flex flex-wrap justify-end gap-3"><button type="button" className={buttonClass} onClick={importPrevious}>Load previous browser settings</button><button type="button" className={buttonClass} onClick={() => { setSettings(null); setNotice(""); setReload(r => r + 1); }}>Cancel / reload</button><button type="button" className={primaryClass} onClick={() => void save()}>{busy ? "Saving…" : mode === "rules" ? "Save rules" : "Save layout"}</button></div>
      </fieldset>
      <p role="status" className="text-sm text-emerald-700">{notice}</p>
    </>}
  </>;
}
