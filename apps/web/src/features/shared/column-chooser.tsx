"use client";

import { useCallback, useEffect, useId, useRef, useState, type CSSProperties, type ReactNode } from "react";
import { TableProperties, X } from "lucide-react";
import styles from "./column-chooser.module.css";

export function ColumnChooser({ children }: { children: ReactNode }) {
  const id = useId();
  const titleId = useId();
  const trigger = useRef<HTMLButtonElement>(null);
  const panel = useRef<HTMLDivElement>(null);
  const [open, setOpen] = useState(false);
  const [position, setPosition] = useState<CSSProperties>({});

  const place = useCallback(() => {
    const rect = trigger.current?.getBoundingClientRect();
    if (!rect) return;
    const width = Math.min(320, window.innerWidth - 24);
    const above = rect.top - 16;
    const below = window.innerHeight - rect.bottom - 16;
    const upwards = above >= Math.min(320, below);
    setPosition({
      width,
      left: Math.max(12, Math.min(rect.left, window.innerWidth - width - 12)),
      top: upwards ? "auto" : rect.bottom + 8,
      bottom: upwards ? window.innerHeight - rect.top + 8 : "auto",
      maxHeight: Math.max(80, Math.min(480, upwards ? above : below)),
    });
  }, []);

  useEffect(() => {
    if (!open) return;
    const close = () => panel.current?.hidePopover();
    const scroll = (event: Event) => {
      if (!panel.current?.contains(event.target as Node)) close();
    };
    // Keep the chooser beside its footer when asynchronously loaded rows change the layout.
    const layout = new ResizeObserver(place);
    const table = trigger.current?.closest("footer")?.parentElement;
    const main = trigger.current?.closest("main");
    if (table) layout.observe(table);
    if (main) layout.observe(main);
    window.addEventListener("resize", close);
    document.addEventListener("scroll", scroll, true);
    return () => {
      layout.disconnect();
      window.removeEventListener("resize", close);
      document.removeEventListener("scroll", scroll, true);
    };
  }, [open, place]);

  return <>
    <button ref={trigger} type="button" className={styles.trigger} title="Choose columns" aria-label="Choose columns" aria-haspopup="dialog" aria-expanded={open} aria-controls={id} popoverTarget={id} onClick={place}>
      <TableProperties size={18} aria-hidden="true" />
    </button>
    <div ref={panel} id={id} popover="auto" role="dialog" aria-labelledby={titleId} className={styles.panel} style={position} onToggle={event => setOpen(event.newState === "open")}>
      <div className={styles.heading}><h2 id={titleId}>Columns</h2><button type="button" className={styles.close} aria-label="Close column chooser" popoverTarget={id} popoverTargetAction="hide" onClick={() => trigger.current?.focus()}><X size={16} aria-hidden="true" /></button></div>
      <p className={styles.hint}>Choose the columns shown in this list.</p>
      <div className="space-y-2">{children}</div>
    </div>
  </>;
}
