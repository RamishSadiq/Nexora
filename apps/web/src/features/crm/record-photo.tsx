"use client";
import Image from "next/image";
import { useState } from "react";
import { crm } from "./api";
import type { RecordRow } from "./types";

export function RecordPhoto({ record, canRead, name = record.name }: { record: RecordRow; canRead: boolean; name?: string }) {
  return <div className="relative -mt-6 mb-3 text-center"><div className="mx-auto grid size-20 place-items-center overflow-hidden rounded-full border-4 border-white bg-slate-100 text-xl font-semibold text-slate-600">{record.photoFileId && canRead ? <Image unoptimized width={80} height={80} className="size-full object-cover" src={`/api/v1/crm/records/${record.id}/photo?v=${record.photoFileId}`} alt={`${name} profile`} /> : name.split(/\s+/).map(p => p[0]).slice(0, 2).join("").toUpperCase()}</div></div>;
}

export function RecordPhotoField({ record, disabled, hasChanges = false, onChanged, onBusyChange }: { record: RecordRow; disabled: boolean; hasChanges?: boolean; onChanged: () => void; onBusyChange?: (busy: boolean) => void }) {
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  return <div><label className="block text-sm">{record.kind === "contact" ? "Contact image" : "Organisation image"}<input className="mt-2 block w-full text-sm" type="file" accept="image/png,image/jpeg" disabled={disabled || busy || hasChanges} onChange={async e => {
    const field = e.currentTarget;
    const file = field.files?.[0];
    if (!file) return;
    setBusy(true); onBusyChange?.(true); setError("");
    try {
      if (!['image/png', 'image/jpeg'].includes(file.type) && (file.type || !/\.(png|jpe?g)$/i.test(file.name))) throw new Error("Choose a PNG or JPEG image.");
      if (file.size > 5 * 1024 * 1024) throw new Error("Choose an image up to 5 MB.");
      const body = new FormData(); body.set("file", file); body.set("version", record.version);
      await crm(`/records/${record.id}/photo`, "POST", body); onChanged();
    } catch (err) { setError(err instanceof Error ? err.message : "Unable to save image."); }
    finally { field.value = ""; setBusy(false); onBusyChange?.(false); }
  }} /></label><p className="mt-2 text-xs text-slate-500">{busy ? "Saving image…" : hasChanges ? "Save your details before uploading an image." : "PNG or JPEG, up to 5 MB. Any image dimensions; displayed in an 80 × 80 pixel circular frame."}</p>{error && <p role="alert" className="mt-2 text-sm text-red-700">{error}</p>}</div>;
}
