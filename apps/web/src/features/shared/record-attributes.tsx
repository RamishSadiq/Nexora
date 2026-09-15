"use client";
import { useEffect, useState } from "react";
import { AttributeFields } from "./attribute-fields";
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
    moduleApi<{ id: string; name: string }[]>(module, `/attributes/${kind}/records?page=${page}`).then(rows => { if (active) setRows(rows); }).catch(e => { if (active) setNotice(e.message); });
    return () => { active = false; };
  }, [module, kind, page, refresh]);
  return <section className="space-y-4 rounded-xl border border-slate-200 bg-white p-5">
    <h2 className="text-lg font-semibold">Record attributes</h2>
    <div className="flex flex-wrap items-end gap-3">
      <label>Entity<select className={inputClass} value={kind} onChange={e => { setKind(e.target.value); setId(""); setPage(1); setRows([]); }}>{types.map(t => <option key={t} value={t}>{entityLabel(t)}</option>)}</select></label>
      <label>Record<select className={inputClass} value={id} onChange={e => setId(e.target.value)}><option value="">Choose a record</option>{rows.map(r => <option key={r.id} value={r.id}>{r.name}</option>)}</select></label>
      <button className={buttonClass} onClick={() => setRefresh(r => r + 1)}>Refresh records</button>
      <button className={buttonClass} disabled={page === 1} onClick={() => { setId(""); setPage(p => p - 1); }}>Previous</button>
      <button className={buttonClass} disabled={rows.length < 50} onClick={() => { setId(""); setPage(p => p + 1); }}>Next</button>
    </div>
    {notice && <p role="status">{notice}</p>}
    {id && <form key={`${kind}-${id}-${refresh}`} onSubmit={async e => {
      const values = formData(e); setBusy(true); setNotice("");
      try { await moduleApi(module, `/attributes/${kind}/records/${id}`, "PUT", values); setNotice("Attributes saved."); }
      catch (e) { setNotice(e instanceof Error ? e.message : "Unable to save."); }
      finally { setBusy(false); }
    }}><fieldset className="grid gap-4 sm:grid-cols-2" disabled={!canEdit || busy}><AttributeFields module={module} kind={kind} recordId={id} />{canEdit && <button className={primaryClass}>Save attributes</button>}</fieldset></form>}
  </section>;
}
