"use client";
import { useEffect, useState } from "react";
import { AttributeFields } from "./attribute-fields";
import { ConfiguredSections } from "./configured-sections";
import { attributeEntities, entityLabel } from "./attribute-entities";
import { moduleApi } from "./module-api";
import { buttonClass, inputClass, primaryClass, formData } from "./module-ui";

export function RecordAttributes({ module, canEdit }: { module: string; canEdit: boolean }) {
  const types = Object.values(attributeEntities).filter(c => c.api === module).flatMap(c => c.types);
  const [kind, setKind] = useState(types[0]);
  const [id, setId] = useState("");
  const [rows, setRows] = useState<{ id: string; name: string }[]>([]);
  const [page, setPage] = useState(1);
  const [refresh, setRefresh] = useState(0);
  const [notice, setNotice] = useState("");
  const [busy, setBusy] = useState(false);
  useEffect(() => {
    let active = true;
    let request = 0;
    const load = () => {
      const current = ++request;
      void moduleApi<{ id: string; name: string }[]>(module, `/attributes/${kind}/records?page=${page}`).then(rows => { if (active && request === current) setRows(rows); }).catch(e => { if (active && request === current) setNotice(e.message); });
    };
    const changed = (event: Event) => { if ((event as CustomEvent<{ module: string }>).detail.module === module) load(); };
    load(); window.addEventListener("nexora-records", changed);
    return () => { active = false; window.removeEventListener("nexora-records", changed); };
  }, [module, kind, page, refresh]);
  return <section className="space-y-4 rounded-xl border border-slate-200 bg-white p-5">
    <h2 className="text-lg font-semibold">Record attributes</h2>
    {notice && <p role="status">{notice}</p>}
    <ConfiguredSections module={module} kind={kind}>
    <div data-section="Record selection" className="flex flex-wrap items-end gap-3">
      <label>Entity<select className={inputClass} value={kind} onChange={e => { setKind(e.target.value); setId(""); setPage(1); setRows([]); }}>{types.map(t => <option key={t} value={t}>{entityLabel(t)}</option>)}</select></label>
      <label>Record<select className={inputClass} value={id} onChange={e => setId(e.target.value)}><option value="">Choose a record</option>{rows.map(r => <option key={r.id} value={r.id}>{r.name}</option>)}</select></label>
      <button className={buttonClass} onClick={() => setRefresh(r => r + 1)}>Refresh records</button>
      <button className={buttonClass} disabled={page === 1} onClick={() => { setId(""); setPage(p => p - 1); }}>Previous</button>
      <button className={buttonClass} disabled={rows.length < 50} onClick={() => { setId(""); setPage(p => p + 1); }}>Next</button>
    </div>
    {id ? <form data-section="Custom fields" key={`${kind}-${id}-${refresh}`} onSubmit={async e => {
      const values = formData(e); setBusy(true); setNotice("");
      try { await moduleApi(module, `/attributes/${kind}/records/${id}`, "PUT", values); setNotice("Attributes saved."); setRefresh(r => r + 1); }
      catch (e) { setNotice(e instanceof Error ? e.message : "Unable to save."); }
      finally { setBusy(false); }
    }}><fieldset className="grid gap-4 sm:grid-cols-2" disabled={!canEdit || busy}><AttributeFields module={module} kind={kind} recordId={id} />{canEdit && <button className={primaryClass}>Save attributes</button>}</fieldset></form> : <p data-section="Custom fields">Choose a record to edit its custom fields.</p>}
    </ConfiguredSections>
  </section>;
}
