"use client";

import { useEffect, useId, useState } from "react";
import { ChevronDown, History } from "lucide-react";
import { crm } from "./api";
import type { Activity, Directory } from "./types";
import styles from "./record-detail.module.css";

export function RecordTimeline({ id, refresh, directory }: { id: string; refresh: number; directory: Directory }) {
  const [open, setOpen] = useState(false);
  const [page, setPage] = useState(1);
  const [retry, setRetry] = useState(0);
  const panelId = useId();
  return <section className={styles.timeline}>
    <h2><button className={styles.timelineToggle} aria-expanded={open} aria-controls={panelId} onClick={() => { setPage(1); setOpen(value => !value); }}>
      <History size={18} aria-hidden="true" /><span>Activity timeline</span><ChevronDown size={17} aria-hidden="true" className={open ? styles.expanded : ""} />
    </button></h2>
    <div id={panelId} hidden={!open}>{open && <TimelineEntries key={`${id}-${refresh}-${page}-${retry}`} id={id} page={page} directory={directory} onPage={setPage} onRetry={() => setRetry(value => value + 1)} />}</div>
  </section>;
}

function TimelineEntries({ id, page, directory, onPage, onRetry }: { id: string; page: number; directory: Directory; onPage: (page: number) => void; onRetry: () => void }) {
  const [items, setItems] = useState<Activity[] | null>(null);
  const [error, setError] = useState("");
  useEffect(() => {
    let active = true;
    crm<Activity[]>(`/records/${id}/timeline?page=${page}`).then(result => { if (active) setItems(result); }).catch(error => { if (active) setError(error instanceof Error ? error.message : "Unable to load activity."); });
    return () => { active = false; };
  }, [id, page]);
  return <div className={styles.timelineBody}>
    {error ? <div role="alert"><p>{error}</p><button onClick={onRetry}>Retry activity</button></div> : items === null ? <p role="status">Loading activity…</p> : <>
      {items.length === 0 && <p>No activity to show.</p>}
      <ol className={styles.events}>{items.map(item => <li key={item.id}><p>{item.action.replaceAll(".", " ")}</p><span>{directory.users.find(user => user.id === item.actorUserId)?.name ?? "Former user"}</span><time dateTime={item.createdAtUtc}>{new Date(item.createdAtUtc).toLocaleString("en-GB", { dateStyle: "medium", timeStyle: "short" })}</time></li>)}</ol>
      <div className={styles.pagination}><button disabled={page <= 1} onClick={() => onPage(page - 1)}>Newer activity</button><button disabled={items.length < 50} onClick={() => onPage(page + 1)}>Older activity</button></div>
    </>}
  </div>;
}
