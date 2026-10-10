"use client";
import { useEffect, useRef, useState } from "react";
import { ArrowDown, ArrowUp, GripVertical, RefreshCw } from "lucide-react";
import styles from "./record-detail.module.css";
import { crm } from "./api";
import { RecordSection } from "./record-section";
type Section = { title: string; total: number; items: { id: string; label: string; status: string; atUtc: string; url: string }[] };

export function ConnectedRecord({ id, refresh }: { id: string; refresh: number }) {
  return <RecordSection title="Connected Activity"><ConnectedContents key={`${id}-${refresh}`} id={id} /></RecordSection>;
}

function ConnectedContents({ id }: { id: string }) {
  const [sections, setSections] = useState<Section[] | null>(null);
  const [error, setError] = useState("");
  const [retry, setRetry] = useState(0);
  const [notice, setNotice] = useState("");
  const [dropTarget, setDropTarget] = useState<string | null>(null);
  const dragging = useRef<string | null>(null);
  const storageKey = `nexora.crm.connected-order.${id}`;
  useEffect(() => {
    let active = true;
    crm<Section[]>(`/records/${id}/context`).then(result => {
      let order: string[] = [];
      try { const stored: unknown = JSON.parse(localStorage.getItem(storageKey) ?? "[]"); if (Array.isArray(stored) && stored.every(item => typeof item === "string")) order = stored; } catch { /* Layout remains usable when browser storage is unavailable. */ }
      const position = (title: string) => { const index = order.indexOf(title); return index < 0 ? order.length : index; };
      if (active) { setSections([...result].sort((a, b) => position(a.title) - position(b.title))); setError(""); }
    }).catch(error => { if (active) setError(error instanceof Error ? error.message : "Connected activity could not be loaded."); });
    return () => { active = false; };
  }, [id, retry, storageKey]);
  function move(title: string, target: string) {
    if (!sections || title === target) return;
    const from = sections.findIndex(section => section.title === title);
    const to = sections.findIndex(section => section.title === target);
    if (from < 0 || to < 0) return;
    const reordered = [...sections]; reordered.splice(to, 0, reordered.splice(from, 1)[0]);
    setSections(reordered);
    let saved = true;
    try { localStorage.setItem(storageKey, JSON.stringify(reordered.map(section => section.title))); } catch { saved = false; }
    setNotice(`${title} moved to position ${to + 1}.${saved ? "" : " The order could not be saved in this browser."}`);
  }
  return <>
    <div className="flex flex-wrap items-start justify-between gap-3"><p className="text-sm text-slate-600">The record’s memberships, portal access and recent updates are shown below.</p><button className={styles.refresh} onClick={() => { setSections(null); setError(""); setRetry(value => value + 1); }}><RefreshCw size={14} aria-hidden="true" />Refresh connected activity</button></div>
    <p className={styles.reorderHint}>Cards can be dragged into order or moved using the arrow controls. The order is saved for this record in this browser.</p>
    <p className="sr-only" role="status">{notice}</p>
    {error ? <p role="alert">{error}</p> : sections === null ? <p role="status">Loading connected activity…</p> : sections.length === 0 ? <p>No connected modules are available for the current access level.</p> : <div className={styles.connectedGrid}>{sections.map((section, index) => <section key={section.title} aria-label={section.title} className={`${styles.connectedCard} ${dropTarget === section.title ? styles.dropTarget : ""}`} onDragOver={event => { if (dragging.current) { event.preventDefault(); event.dataTransfer.dropEffect = "move"; setDropTarget(section.title); } }} onDrop={event => { event.preventDefault(); if (dragging.current) move(dragging.current, section.title); dragging.current = null; setDropTarget(null); }}>
      <div className={styles.cardControls}><button className={styles.dragHandle} draggable aria-label={`Drag ${section.title}`} title={`Drag ${section.title} to reorder`} onDragStart={event => { dragging.current = section.title; event.dataTransfer.effectAllowed = "move"; event.dataTransfer.setData("text/plain", section.title); }} onDragEnd={() => { dragging.current = null; setDropTarget(null); }}><GripVertical size={16} aria-hidden="true" /></button><div><button disabled={index === 0} aria-label={`Move ${section.title} earlier`} onClick={() => move(section.title, sections[index - 1].title)}><ArrowUp size={14} aria-hidden="true" /></button><button disabled={index === sections.length - 1} aria-label={`Move ${section.title} later`} onClick={() => move(section.title, sections[index + 1].title)}><ArrowDown size={14} aria-hidden="true" /></button></div></div>
      <h3 aria-label={`${section.title} (${section.total})`}>{section.title}<span className={styles.count}>{section.total}</span></h3>
      {section.total === 0 && <p className="mt-2 text-sm text-slate-600">No records.</p>}
      <ul className="mt-3 space-y-3">{section.items.map(item => <li key={item.id} className="break-words text-sm"><a href={item.url}>{item.label}</a><p>{item.status} · {new Date(item.atUtc).toLocaleString("en-GB", { dateStyle: "medium", timeStyle: "short" })}</p></li>)}</ul>
      {section.total > section.items.length && <p className="mt-3 text-xs">The latest {section.items.length} entries are shown. The full list is available in the corresponding module.</p>}
    </section>)}</div>}
  </>;
}
