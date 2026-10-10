"use client";
import { useId, useRef, useState, type ReactNode } from "react";
import { ChevronDown } from "lucide-react";
import styles from "./record-detail.module.css";
import { SectionAttributes } from "../shared/record-attribute-sections";

export function RecordSection({ title, children, onLoad, keepMounted = false, "data-section": sectionId }: { title: string; children: ReactNode; onLoad?: () => Promise<void>; keepMounted?: boolean; "data-section"?: string }) {
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const request = useRef(0);
  const panelId = useId();
  async function load() {
    const current = ++request.current;
    setLoading(true); setError("");
    try { await onLoad?.(); }
    catch (error) { if (current === request.current) setError(error instanceof Error ? error.message : "The section could not be loaded."); }
    finally { if (current === request.current) setLoading(false); }
  }
  return <section data-section={sectionId ?? title} className={styles.timeline} aria-label={title}>
    <h2><button type="button" className={styles.timelineToggle} aria-expanded={open} aria-controls={panelId} onClick={() => {
      if (open) { request.current++; setOpen(false); }
      else { setOpen(true); if (onLoad) void load(); }
    }}><span>{title}</span><ChevronDown size={17} aria-hidden="true" className={open ? styles.expanded : ""} /></button></h2>
    <div id={panelId} hidden={!open}>{(open || keepMounted) && <div className={styles.timelineBody}>{loading ? <p role="status">Loading {title.toLowerCase()}…</p> : error ? <div role="alert"><p>{error}</p><button type="button" onClick={() => void load()}>Retry {title.toLowerCase()}</button></div> : children}<SectionAttributes section={sectionId ?? title} /></div>}</div>
  </section>;
}
