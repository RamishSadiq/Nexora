"use client";
import { useEffect, useId, useRef, useState } from "react";
import { ChevronDown, Download } from "lucide-react";
import styles from "./record-listing.module.css";
import { crm } from "./api";
type Result = { valid: boolean; committed: boolean; rows: { row: number; name: string; action: string; error?: string }[] };
export function RecordTransfer({ kind, changed }: { kind: string; changed: () => void }) {
  const [csv, setCsv] = useState(""); const [result, setResult] = useState<Result | null>(null); const [error, setError] = useState(""); const [busy, setBusy] = useState(false);
  const [open, setOpen] = useState(false);
  const container = useRef<HTMLDivElement>(null);
  const trigger = useRef<HTMLButtonElement>(null);
  const panelId = useId();
  useEffect(() => {
    if (!open) return;
    const outside = (event: PointerEvent) => { if (!container.current?.contains(event.target as Node)) setOpen(false); };
    const escape = (event: KeyboardEvent) => { if (event.key === "Escape") { setOpen(false); trigger.current?.focus(); } };
    document.addEventListener("pointerdown", outside); document.addEventListener("keydown", escape);
    return () => { document.removeEventListener("pointerdown", outside); document.removeEventListener("keydown", escape); };
  }, [open]);
  async function run(commit: boolean) { setBusy(true); setError(""); try { const r = await crm<Result>("/import", "POST", { csv, commit }); setResult(r); if (r.committed) changed(); } catch (e) { setError(e instanceof Error ? e.message : "Import failed."); setResult(null); } finally { setBusy(false); } }
  return <div ref={container} className="relative"><button ref={trigger} type="button" aria-expanded={open} aria-controls={panelId} onClick={() => setOpen(value => !value)} className={styles.transfer} aria-label="Import and export"><Download size={14} aria-hidden="true" />Import and export<ChevronDown size={13} aria-hidden="true" /></button>{open && <section id={panelId} aria-label="Import and export options" className="absolute right-0 top-full z-40 mt-2 max-h-[65vh] w-[min(34rem,calc(100vw-4rem))] overflow-y-auto rounded-xl border border-blue-200 bg-white p-4 shadow-xl"><p className="my-3 text-sm">Export current {kind}s, or import up to 500 records. Preview first; every row must be valid. Existing records require their exported ID and version. Duplicate names or emails need manual reconciliation. Existing email consent and omitted emails are preserved.</p><a href={`/api/v1/crm/export?kind=${kind}`} className="text-sm font-medium text-indigo-700 underline">Export current {kind}s as CSV</a><label className="my-3 block text-sm">Choose CSV<input type="file" accept=".csv,text/csv" disabled={busy} className="mt-1 block" onChange={async e => { setResult(null); setCsv(""); setError(""); const file = e.target.files?.[0]; if (!file) return; if (file.size > 1024 * 1024) { setError("Choose a CSV up to 1 MB."); return; } try { setCsv(await file.text()); } catch { setError("Could not read this file."); } }} /></label><label className="block text-sm">CSV data<textarea className="mt-1 block w-full rounded border p-3 font-mono text-xs" rows={5} disabled={busy} value={csv} placeholder="id,version,kind,name,status,category,email" onChange={e => { setCsv(e.target.value); setResult(null); }} /></label><div className="my-3 flex flex-wrap gap-3"><button className="rounded border px-3 py-2 text-sm" disabled={busy || !csv.trim()} onClick={() => void run(false)}>Preview import</button><button className="rounded bg-indigo-600 px-3 py-2 text-sm text-white disabled:opacity-50" disabled={busy || !result?.valid || result.committed} onClick={() => void run(true)}>Import validated records</button></div>{error && <p role="alert">{error}</p>}{result && <><p role="status">{result.committed ? "Import complete." : result.valid ? "Preview valid. Ready to import." : "Nothing imported. Resolve the blocked rows."}</p><ul className="mt-3 max-h-64 space-y-2 overflow-y-auto text-sm">{result.rows.map(r => <li key={r.row}>Row {r.row}: {r.name} · {r.action}{r.error && ` · ${r.error}`}</li>)}</ul></>}</section>}</div>;
}
