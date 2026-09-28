"use client";
import { useEffect, useState } from "react";
import { portal } from "./api";

type Notice = { id: string; subject: string; body: string; isRead: boolean; createdAtUtc: string };
export function NotificationInbox({ revision }: { revision: number }) {
  const [items, setItems] = useState<Notice[] | null>(null);
  const [error, setError] = useState("");
  const [refresh, setRefresh] = useState(0);
  const [busy, setBusy] = useState<string | null>(null);
  useEffect(() => {
    let active = true;
    portal<Notice[]>("/notifications").then(data => { if (active) { setItems(data); setError(""); } })
      .catch(e => { if (active) setError(e.message); });
    return () => { active = false; };
  }, [refresh, revision]);
  async function markRead(id: string) {
    setBusy(id); setError("");
    try { await portal(`/notifications/${id}/read`, "POST"); setItems(current => current?.map(n => n.id === id ? { ...n, isRead: true } : n) ?? null); }
    catch (e) { setError(e instanceof Error ? e.message : "Unable to mark notification as read."); }
    finally { setBusy(null); }
  }
  return <section id="notifications" aria-label="Notifications" className="min-w-0 rounded-2xl border border-slate-200 bg-white p-7">
    <div className="flex flex-wrap justify-between gap-3"><h2 className="text-2xl font-semibold">Notifications</h2><button className="underline" onClick={() => setRefresh(n => n + 1)}>Refresh notifications</button></div>
    <p className="mt-3 text-sm text-slate-600">Your latest 100 updates. These messages are available here; email delivery is not enabled.</p>
    {error && <p role="alert" className="mt-4 text-red-800">{error}</p>}
    {!items ? <p role="status" className="mt-4">Loading notifications…</p> : items.length === 0 ? <p className="mt-4">No notifications yet.</p> : <ul className="mt-5 space-y-4">{items.map(n => <li key={n.id} className="break-words rounded-xl border border-slate-200 p-4">
      <h3 className="font-semibold">{n.subject}{!n.isRead && <span className="ml-2 text-xs">Unread</span>}</h3><p className="mt-2 whitespace-pre-wrap text-sm">{n.body}</p>
      <p className="mt-2 text-xs text-slate-600">{new Date(n.createdAtUtc).toLocaleString("en-GB")}</p>
      {!n.isRead && <button className="mt-3 text-sm underline disabled:opacity-50" disabled={busy !== null} onClick={() => void markRead(n.id)}>Mark as read<span className="sr-only">: {n.subject}</span></button>}
    </li>)}</ul>}
  </section>;
}
