"use client";
import { use, useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { moduleApi } from "@/features/shared/module-api";
import { attributeEntities, entityLabel } from "@/features/shared/attribute-entities";
import type { Field } from "@/features/crm/types";
import { buttonClass, inputClass, primaryClass } from "@/features/shared/module-ui";
import type { AutomationSettings } from "@/features/shared/automation-controller";

export default function AttributesControllerPage({ params }: { params: Promise<{ module: string }> }) {
  const { module } = use(params);
  const config = attributeEntities[module];
  const [selectedType, setSelectedType] = useState("");
  const kind = config?.types.includes(selectedType) ? selectedType : config?.types[0];
  return <main className="mx-auto max-w-3xl space-y-6 p-5 text-slate-800 sm:p-8">
    <Link href={`/automations/${module}`} className="text-indigo-700 underline">Back to automations</Link>
    <h1 className="text-3xl font-semibold">Manage {module.replaceAll("-", " ")} attributes</h1>
    {!kind ? <p role="alert">This entity is unavailable.</p> : <>
      <label className="block">Entity<select className={inputClass} value={kind} onChange={e => setSelectedType(e.target.value)}>{config.types.map(t => <option key={t} value={t}>{entityLabel(t)}</option>)}</select></label>
      <AttributesEditor key={`${config.api}-${kind}`} backend={config.api} kind={kind} />
    </>}
  </main>;
}

function AttributesEditor({ backend, kind }: { backend: string; kind: string }) {
  const base = backend === "crm" ? "/fields" : `/attributes/${kind}/definitions`;
  const crm = useCallback(<T,>(path: string, method = "GET", body?: unknown) => moduleApi<T>(backend, path, method, body), [backend]);
  const [fields, setFields] = useState<Field[]>([]);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true);
  const [canManage, setCanManage] = useState(false);
  const [retry, setRetry] = useState(0);
  async function reload() {
    const rows = await crm<Field[]>(base);
    setFields(rows.filter(f => backend !== "crm" || f.kind === kind));
  }
  useEffect(() => {
    let active = true;
    void Promise.all([crm<Field[]>(base), crm<AutomationSettings>(`/automations/${kind}`)]).then(([rows, settings]) => { if (active) { setFields(rows.filter(f => backend !== "crm" || f.kind === kind)); setCanManage(settings.canManage); setError(""); setLoading(false); } }).catch(e => { if (active) { setError(e.message); setLoading(false); } });
    return () => { active = false; };
  }, [kind, backend, base, crm, retry]);
  async function change(action: () => Promise<unknown>) {
    setBusy(true); setError("");
    try { await action(); await reload(); const channel = new BroadcastChannel("nexora-attributes"); channel.postMessage("changed"); channel.close(); }
    catch (e) { setError(e instanceof Error ? e.message : "Unable to save attributes."); }
    finally { setBusy(false); }
  }
  function move(index: number, offset: number) {
    const ids = fields.map(f => f.id);
    [ids[index], ids[index + offset]] = [ids[index + offset], ids[index]];
    void change(() => crm(backend === "crm" ? "/fields/order" : `/attributes/${kind}/order`, "PUT", { kind, ids }));
  }
  return <>
      <p className="text-sm text-slate-600">Changes are saved immediately. Deleting an attribute also removes its saved values.</p>
      {error && <div role="alert" className="text-red-700">{error} <button className={buttonClass} onClick={() => setRetry(r => r + 1)}>Reload attributes</button></div>}
      {loading ? <p role="status">Loading attributes…</p> : <fieldset disabled={busy || !canManage} className="space-y-4">
        {!canManage && <p>Editing requires module management permission.</p>}
        <form className="flex flex-wrap items-end gap-3" onSubmit={event => {
          event.preventDefault(); const form = event.currentTarget; const data = new FormData(form);
          void change(async () => { await crm(base, "POST", { kind, name: data.get("name"), dataType: data.get("dataType") }); form.reset(); });
        }}>
          <label>Name<input name="name" required maxLength={80} className={inputClass} /></label>
          <label>Data type<select name="dataType" className={inputClass}>{["text","number","boolean","date"].map(t => <option key={t}>{t}</option>)}</select></label>
          <button disabled={busy} className={primaryClass}>Add attribute</button>
        </form>
        <ul className="space-y-3">{fields.map((field, index) => <li key={field.id} className="rounded-xl border bg-white p-4">
          <form className="flex flex-wrap items-end gap-3" onSubmit={event => {
            event.preventDefault(); const data = new FormData(event.currentTarget);
            void change(() => crm(`${base}/${field.id}`, "PATCH", { kind, name: data.get("name"), dataType: data.get("dataType") }));
          }}>
            <label>Name<input key={field.name} name="name" defaultValue={field.name} required maxLength={80} className={inputClass} /></label>
            <label>Data type<select key={field.dataType} name="dataType" defaultValue={field.dataType} className={inputClass}>{["text","number","boolean","date"].map(t => <option key={t}>{t}</option>)}</select></label>
            <button disabled={busy} className={buttonClass}>Save</button>
            <button type="button" disabled={busy || index === 0} className={buttonClass} onClick={() => move(index, -1)} aria-label={`Move ${field.name} up`}>Up</button>
            <button type="button" disabled={busy || index === fields.length - 1} className={buttonClass} onClick={() => move(index, 1)} aria-label={`Move ${field.name} down`}>Down</button>
            <button type="button" disabled={busy} className={buttonClass} onClick={() => { if (window.confirm(`Delete ${field.name} and its saved values?`)) void change(() => crm(`${base}/${field.id}`, "DELETE")); }}>Delete</button>
          </form>
        </li>)}</ul>
      </fieldset>}
  </>;
}



