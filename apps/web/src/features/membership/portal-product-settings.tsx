"use client";
import { useState } from "react";
import { moduleApi } from "../shared/module-api";
type Product = { id: string; name: string; isActive: boolean; portalApplicationsEnabled: boolean; portalOrganisationApplicationsEnabled?: boolean; portalTerms: string; version: string };
export function PortalProductSettings({ products, onSaved, initialProductId = "" }: { products: Product[]; onSaved: () => void; initialProductId?: string }) {
  const [id, setId] = useState(initialProductId);
  const [message, setMessage] = useState("");
  const product = products.find(p => p.id === id);
  return <section className="rounded-xl border border-slate-200 bg-white p-5" aria-label="Portal application settings">
    <h2 className="text-xl font-semibold">Portal application settings</h2>
    <p className="mt-2 text-sm text-slate-600">Choose which products members can apply for online and publish their membership terms. Products must also be available. Changes require members to refresh saved quotes before submission.</p>
    {message && <p role="status" className="mt-3 text-emerald-800">{message}</p>}
    <label htmlFor="portal-product" className="mt-4 block text-sm">Portal product</label>
    <select id="portal-product" className="mt-1 block w-full rounded-lg border p-3" value={id} onChange={e => { setId(e.target.value); setMessage(""); }}><option value="">Choose a product</option>{products.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}</select>
    {product && <SettingsForm key={`${product.id}:${product.version}`} product={product} onSaved={() => { setMessage("Portal settings saved."); onSaved(); }} />}
  </section>;
}
function SettingsForm({ product, onSaved }: { product: Product; onSaved: () => void }) {
  const [enabled, setEnabled] = useState(product.portalApplicationsEnabled);
  const [organisationEnabled, setOrganisationEnabled] = useState(product.portalOrganisationApplicationsEnabled ?? false);
  const [terms, setTerms] = useState(product.portalTerms);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  return <form className="mt-5 space-y-4" onSubmit={async e => {
    e.preventDefault(); setBusy(true); setError("");
    try { await moduleApi("membership", `/products/${product.id}/portal`, "PUT", { version: product.version, enabled, organisationEnabled, terms }); onSaved(); }
    catch (e) { setError(e instanceof Error ? e.message : "Unable to save portal settings."); } finally { setBusy(false); }
  }}>
    {error && <p role="alert" className="rounded-lg bg-red-50 p-3 text-red-800">{error}</p>}
    {!product.isActive && <p className="text-sm text-amber-800">This product is inactive. Members cannot apply until its availability is restored.</p>}
    <label className="flex items-center gap-3"><input type="checkbox" checked={enabled} disabled={busy} onChange={e => setEnabled(e.target.checked)} />Allow personal applications in the member portal</label>
    <label className="flex items-center gap-3"><input type="checkbox" checked={organisationEnabled} disabled={busy} onChange={e => setOrganisationEnabled(e.target.checked)} />Allow organisation applications in the member portal</label>
    <label className="block text-sm">Member-facing terms<textarea className="mt-1 block w-full rounded-lg border p-3" rows={7} value={terms} maxLength={4000} required={enabled || organisationEnabled} disabled={busy} onChange={e => setTerms(e.target.value)} /></label>
    <p className="text-xs text-slate-500">Plain text, up to 4000 characters. Submitted applications retain the terms accepted at submission.</p>
    <button className="rounded-lg bg-indigo-600 px-4 py-2 text-white disabled:opacity-50" disabled={busy}>Save portal settings</button>
  </form>;
}

