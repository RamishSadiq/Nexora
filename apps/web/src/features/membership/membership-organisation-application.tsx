"use client";
import { ApplicationRequestedFields, emptyApplicationDetails } from "./membership-application-details";
import { MembershipRejoinSelector } from "./membership-rejoin-selector";
import { useEffect, useState } from "react";
import { moduleApi } from "../shared/module-api";
import { Field, Panel, formData, inputClass as input, primaryClass as primary, buttonClass as button } from "../shared/module-ui";

type Choice = { id: string; name: string };
type Product = { id: string; name: string; rate: number; currency: string; isActive: boolean };
export function MembershipOrganisationApplication({ products, busy, canManageCrm, onSubmit }: { products: Product[]; busy: boolean; canManageCrm: boolean; onSubmit: (payload: unknown) => Promise<void> }) {
  const [requestedDetails, setRequestedDetails] = useState(emptyApplicationDetails);
  const [contacts, setContacts] = useState<Choice[]>([]);
  const [accounts, setAccounts] = useState<Choice[]>([]);
  const [accountId, setAccountId] = useState("");
  const [contactId, setContactId] = useState("");
  const [applicationType, setApplicationType] = useState("corporate");
  const [accountSearch, setAccountSearch] = useState("");
  const [contactSearch, setContactSearch] = useState("");
  const [newName, setNewName] = useState("");
  const [association, setAssociation] = useState<{ accountId: string; contactId: string; linked: boolean } | null>(null);
  const linked = association?.accountId === accountId && association?.contactId === contactId && association.linked;
  const [working, setWorking] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const disabled = busy || working;
  const today = new Date().toISOString().slice(0, 10);
  useEffect(() => { let active = true; Promise.all([moduleApi<Choice[]>("membership", "/contacts"), moduleApi<Choice[]>("membership", "/accounts")]).then(([c, a]) => { if (active) { setContacts(c); setAccounts(a); } }).catch(e => { if (active) setError(e.message); }); return () => { active = false; }; }, []);
  useEffect(() => {
    let active = true;
    if (accountId && contactId) {
      moduleApi<{ linked: boolean }>("membership", `/account-contact?accountId=${accountId}&contactId=${contactId}`)
        .then(result => { if (active) setAssociation({ accountId, contactId, linked: result.linked }); })
        .catch(e => { if (active) { setAssociation({ accountId, contactId, linked: false }); setError(e.message); } });
    }
    return () => { active = false; };
  }, [accountId, contactId]);
  async function perform(action: () => Promise<void>) { setWorking(true); setError(""); setSuccess(""); try { await action(); } catch (e) { setError(e instanceof Error ? e.message : "Unable to save. Your application details have been kept."); } finally { setWorking(false); } }
  async function search(kind: "contacts" | "accounts") {
    const result = await moduleApi<Choice[]>("membership", `/${kind}?search=${encodeURIComponent(kind === "contacts" ? contactSearch : accountSearch)}`);
    if (kind === "contacts") { setContacts(result); setContactId(""); } else { setAccounts(result); setAccountId(""); }
  }
  return <Panel title="Submit membership application">
    {error && <p role="alert" className="mb-3 text-sm text-red-700">{error}</p>}
    {success && <p role="status" className="mb-3 text-sm text-green-700">{success}</p>}
    <form className="grid items-end gap-3 sm:grid-cols-2" onSubmit={e => { const r = formData(e); const form = e.currentTarget; void perform(async () => { await onSubmit({ ...r, requestedDetails, contactId, accountId: accountId || null, applicationType, startsAtUtc: `${r.startsAtUtc}T00:00:00Z`, applicantRole: r.applicantRole || null, applicantNotes: r.applicantNotes || null, termsAccepted: r.termsAccepted === "on", previousMembershipId: r.previousMembershipId || null, exemptJoiningFee: r.exemptJoiningFee === "unknown" ? null : r.exemptJoiningFee === "yes", freeMembership: r.freeMembership === "unknown" ? null : r.freeMembership === "yes" }); form.reset(); setRequestedDetails(emptyApplicationDetails); setAccountId(""); setContactId(""); setSuccess("Application submitted for review."); }); }}>
      <Field label="Applicant type"><select className={input} value={applicationType} onChange={e => { setApplicationType(e.target.value); if (e.target.value === "individual") setAccountId(""); }}><option value="corporate">Corporate</option><option value="individual">Individual</option><option value="affiliate">Affiliate</option></select></Field>
      {applicationType !== "individual" && <>
        <Field label="Find organisation"><input className={input} value={accountSearch} onChange={e => setAccountSearch(e.target.value)} placeholder="Start typing an organisation" /></Field>
        <button type="button" className={button} disabled={disabled} onClick={() => void perform(() => search("accounts"))}>Search organisations</button>
        <Field label="Organisation"><select className={input} value={accountId} onChange={e => setAccountId(e.target.value)} disabled={disabled} required={applicationType === "corporate"}><option value="">Choose organisation</option>{accounts.map(a => <option key={a.id} value={a.id}>{a.name}</option>)}</select></Field>
      </>}
      <Field label="Find main contact"><input className={input} value={contactSearch} onChange={e => setContactSearch(e.target.value)} placeholder="Search before creating a contact" /></Field>
      <button type="button" className={button} disabled={disabled} onClick={() => void perform(() => search("contacts"))}>Search contacts</button>
      <Field label="Main contact"><select className={input} value={contactId} onChange={e => setContactId(e.target.value)} disabled={disabled} required><option value="">Choose contact</option>{contacts.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}</select></Field>
      {canManageCrm && <><Field label="New contact name"><input className={input} value={newName} maxLength={160} onChange={e => setNewName(e.target.value)} /></Field><button type="button" className={button} disabled={disabled || !newName.trim()} onClick={() => void perform(async () => { const matches = await moduleApi<Choice[]>("membership", `/contacts?search=${encodeURIComponent(newName.trim())}`); if (matches.some(c => c.name.toLowerCase() === newName.trim().toLowerCase())) { setContacts(matches); throw new Error("A contact with this name already exists. Choose the existing contact after checking the search results."); } const created = await moduleApi<Choice>("crm", "/records", "POST", { kind: "contact", name: newName.trim(), status: "active", category: "member" }); setContacts(c => [...c, created]); setContactId(created.id); setNewName(""); })}>Create and select contact</button></>}
      {accountId && contactId && <div className="sm:col-span-2">{linked ? <p className="text-sm text-green-700">Contact linked to organisation.</p> : canManageCrm ? <button type="button" className={button} disabled={disabled} onClick={() => void perform(async () => { await moduleApi("crm", `/accounts/${accountId}/contacts`, "POST", { contactId, startDate: `${today}T00:00:00Z`, workClassification: "unknown", isMainContact: true, isAuthorisedContact: false, roles: ["membership"] }); setAssociation({ accountId, contactId, linked: true }); })}>Link contact to organisation</button> : <p className="text-sm">Ask a CRM manager to link this contact to the organisation before submitting.</p>}</div>}
      <MembershipRejoinSelector key={`${contactId}-${accountId}-${applicationType}`} contactId={contactId} accountId={accountId} disabled={disabled} />
      <Field label="Membership product"><select className={input} name="productId" required><option value="">Choose product</option>{products.filter(p => p.isActive).map(p => <option key={p.id} value={p.id}>{p.name} · {p.currency} {p.rate.toFixed(2)}</option>)}</select></Field>
      <Field label="Requested start date"><input className={input} type="date" name="startsAtUtc" min={today} required /></Field>
      <Field label="Exempt joining fee"><select name="exemptJoiningFee" className={input} defaultValue="unknown"><option value="unknown">Unknown</option><option value="yes">Yes</option><option value="no">No</option></select></Field>
      <Field label="Free membership"><select name="freeMembership" className={input} defaultValue="unknown"><option value="unknown">Unknown</option><option value="yes">Yes</option><option value="no">No</option></select></Field>
      <Field label="Applicant role"><input className={input} name="applicantRole" maxLength={80} placeholder="For example, membership manager" /></Field>
      <div className="sm:col-span-2"><ApplicationRequestedFields value={requestedDetails} onChange={setRequestedDetails} disabled={disabled}/></div>
      <p className="text-xs text-slate-500 sm:col-span-2">Lifecycle purpose is New unless a previous membership is selected for Rejoin. Upgrade and Reinstate applications remain unavailable.</p>
      <Field label="Applicant notes"><textarea className={input} name="applicantNotes" maxLength={4000} rows={3} /></Field>
      <label className="flex items-center gap-2 text-sm"><input type="checkbox" name="termsAccepted" required /> I confirm the applicant has accepted the membership terms.</label>

      <button className={primary} disabled={disabled || (!!accountId && !linked)}>Submit application</button>
    </form>
  </Panel>;
}
