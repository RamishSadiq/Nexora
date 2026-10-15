"use client";

import { useSyncExternalStore, type ReactNode } from "react";
import { ChevronsLeft, ChevronsRight } from "lucide-react";
import styles from "./panel-toggle.module.css";

const fallback = new Map<string, boolean>();
const changeEvent = "nexora-panel-layout";
function subscribe(update: () => void) {
  const storageChanged = () => { fallback.clear(); update(); };
  window.addEventListener("storage", storageChanged);
  window.addEventListener(changeEvent, update);
  return () => {
    window.removeEventListener("storage", storageChanged);
    window.removeEventListener(changeEvent, update);
  };
}

export function useCollapsedPanel(key: string) {
  const collapsed = useSyncExternalStore(subscribe, () => {
    if (fallback.has(key)) return fallback.get(key)!;
    try { return localStorage.getItem(key) === "true"; }
    catch { return fallback.get(key) ?? false; }
  }, () => false);
  function toggle() {
    const next = !collapsed;
    try { localStorage.setItem(key, String(next)); fallback.delete(key); }
    catch { fallback.set(key, next); }
    window.dispatchEvent(new Event(changeEvent));
  }
  return [collapsed, toggle] as const;
}

export function PanelToggle({ label, collapsed, onToggle, controls, className = "", side = "left", children }: {
  label: string;
  collapsed: boolean;
  onToggle: () => void;
  controls: string;
  className?: string;
  side?: "left" | "right";
  children?: ReactNode;
}) {
  const title = `${collapsed ? "Show" : "Hide"} ${label}`;
  const Icon = collapsed === (side === "left") ? ChevronsRight : ChevronsLeft;
  return <button type="button" className={`${styles.toggle} ${className}`} aria-label={title} title={title}
    aria-expanded={!collapsed} aria-controls={controls} onClick={onToggle}>
    <Icon size={17} aria-hidden="true" />{children}
  </button>;
}
