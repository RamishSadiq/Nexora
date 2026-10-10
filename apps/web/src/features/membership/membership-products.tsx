"use client";
import { useEffect, useState } from "react";
import { Plus } from "lucide-react";
import { WorkspaceHeading } from "@/components/ui/workspace-heading";
import { RecordListing, useRecordPage } from "../shared/record-listing";
import { useEntityListing } from "../shared/entity-listing-controls";
import { Field, Panel, formData, inputClass, buttonClass, primaryClass } from "../shared/module-ui";
import { moduleApi } from "../shared/module-api";
import { PortalProductSettings } from "./portal-product-settings";
type Product = { id: string; name: string; category: string; termMonths: number; rate: number; currency: string; isActive: boolean; version: string; portalApplicationsEnabled: boolean; portalOrganisationApplicationsEnabled: boolean; portalTerms: string };
export function MembershipProducts({ permissions }: { permissions: string[] }) {
  const navigation = useRecordPage();
  const [products, setProducts] = useState<Product[]>([]);
  const [reload, setReload] = useState(0);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const listing = useEntityListing({ records: products.map(p => ({ ...p, status: p.isActive ? "active" : "inactive" })), columns: ["Product", "Category", "Term", "Rate", "Status"] });
  const product = products.find(p => p.id === navigation.recordId);
  const manage = permissions.includes("membership.manage");
  const editable = manage && (navigation.creating || navigation.mode === "edit");
  useEffect(() => { let active = true; moduleApi<Product[]>("membership", "/products").then(rows => { if (active) { setProducts(rows); setError(""); setLoading(false); } }).catch(e => { if (active) { setError(e.message); setLoading(false); } }); return () => { active = false; }; }, [reload]);
  return <main className="mx-auto max-w-7xl space-y-5 p-4 sm:p-7">
    <WorkspaceHeading actions={navigation.isListing && manage ? <button className={buttonClass} aria-label="Add membership product" onClick={() => navigation.open("new", "edit")}><Plus size={18} /></button> : undefined}>Membership products</WorkspaceHeading>
    {error && <p role="alert" className="rounded-xl bg-red-50 p-4 text-red-800">{error} <button className={buttonClass} onClick={() => setReload(n => n + 1)}>Reload products</button></p>}
    {navigation.isListing ? <>{listing.toolbar}<RecordListing label="Membership products" loading={loading} filterKey={listing.filterKey} rows={products.filter(p => listing.matches({ ...p, status: p.isActive ? "active" : "inactive" }))} name={p => p.name} onView={p => navigation.open(p.id)} onEdit={manage ? p => navigation.open(p.id, "edit") : undefined} columns={[
      { key: "name", label: "Product", render: p => p.name }, { key: "category", label: "Category", hidden: !listing.show("Category"), render: p => p.category }, { key: "term", label: "Term", hidden: !listing.show("Term"), render: p => `${p.termMonths} months` }, { key: "rate", label: "Rate", hidden: !listing.show("Rate"), render: p => `${p.currency} ${p.rate.toFixed(2)}` }, { key: "status", label: "Status", hidden: !listing.show("Status"), render: p => p.isActive ? "Active" : "Inactive" },
    ]} /></> : <>
      <button className={buttonClass} aria-label="Back to listing" onClick={navigation.back}>Back to products</button>
      {loading ? <p role="status">Loading product…</p> : product || navigation.creating ? <>
        <Panel title={product?.name ?? "New membership product"}><form key={product?.version ?? "new"} className="grid items-end gap-4 sm:grid-cols-2" onSubmit={async e => { const data = formData(e); setBusy(true); setError(""); try { const saved = await moduleApi<Product>("membership", product ? `/products/${product.id}` : "/products", product ? "PATCH" : "POST", { name: data.name, category: data.category, termMonths: Number(data.termMonths), rate: Number(data.rate), currency: data.currency, isActive: data.isActive === "on", version: product?.version }); setReload(n => n + 1); navigation.open(saved.id, "edit"); } catch (e) { setError(e instanceof Error ? e.message : "Unable to save product."); } finally { setBusy(false); } }}>
          <fieldset disabled={!editable || busy} className="contents">
            <Field label="Product name"><input name="name" className={inputClass} defaultValue={product?.name ?? ""} maxLength={160} required /></Field>
            <Field label="Category"><input name="category" className={inputClass} defaultValue={product?.category ?? ""} maxLength={80} required /></Field>
            <Field label="Term months"><input name="termMonths" type="number" className={inputClass} min={1} max={60} defaultValue={product?.termMonths ?? 12} required /></Field>
            <Field label="Membership rate"><input name="rate" type="number" className={inputClass} min={0} max={99999999.99} step="0.01" defaultValue={product?.rate ?? 0} required /></Field>
            <Field label="Currency"><select name="currency" className={inputClass} defaultValue={product?.currency ?? "GBP"}>{["GBP", "EUR", "USD"].map(c => <option key={c}>{c}</option>)}</select></Field>
            <label><input name="isActive" type="checkbox" defaultChecked={product?.isActive ?? true} /> Product is active</label>
          </fieldset>
          {editable && <button className={primaryClass} disabled={busy}>Save membership product</button>}
        </form><p className="mt-4 text-sm text-slate-500">Changes apply to future quotes. Existing application quotes are retained.</p>{manage && !editable && <button className={buttonClass} onClick={() => navigation.open(product!.id, "edit")}>Edit product</button>}</Panel>
        {product && manage && <PortalProductSettings key={product.id} products={[product]} initialProductId={product.id} onSaved={() => setReload(n => n + 1)} />}
      </> : <p role="alert">This product is unavailable.</p>}
    </>}
  </main>;
}
