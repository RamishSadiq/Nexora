"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { portal } from "./api";
import { ApplicationEvidence } from "@/features/shared/application-evidence";

type Receipt = {
  id: string; product: string; status: string; submittedAtUtc: string;
  requestedStartUtc: string; requestedEndUtc: string | null; quotedTotal: number; currency: string;
  termsAcceptedAtUtc: string | null; acceptedTerms: string | null; submittedNotes: string | null;
  membership: { status: string; startsAtUtc: string; endsAtUtc: string } | null;
};
export const portalDate = (value: string) => new Intl.DateTimeFormat("en-GB", { dateStyle: "medium", timeZone: "UTC" }).format(new Date(value));
export const portalMoney = (value: number, currency: string) => new Intl.NumberFormat("en-GB", { style: "currency", currency }).format(value);

export function ApplicationReceipt({ id, accountId }: { id: string; accountId?: string }) {
  const [receipt, setReceipt] = useState<Receipt | null>(null);
  const [error, setError] = useState("");
  const [attempt, setAttempt] = useState(0);
  useEffect(() => {
    let active = true;
    portal<Receipt>(`${accountId ? `/organisations/${accountId}` : ""}/applications/${encodeURIComponent(id)}`).then(value => { if (active) setReceipt(value); })
      .catch(e => { if (active) setError(e.message); });
    return () => { active = false; };
  }, [id, accountId, attempt]);
  function refresh() { setReceipt(null); setError(""); setAttempt(n => n + 1); }
  return <section className="min-w-0 rounded-2xl border border-slate-200 bg-white p-6 sm:p-8" aria-label="Application receipt">
    <div className="flex flex-wrap items-center justify-between gap-4 print:hidden">
      <Link className="underline underline-offset-4" href="/portal">Back to my portal</Link>
      <button className="underline underline-offset-4" onClick={refresh}>Refresh application status</button>
    </div>
    <h2 className="mt-6 text-2xl font-semibold">Application receipt</h2>
    {error ? <div role="alert" className="mt-5 rounded-xl bg-red-50 p-4 text-red-900">{error} <button className="underline" onClick={refresh}>Try again</button></div> : !receipt ? <p role="status" className="mt-5">Loading your application…</p> : <>
      <h3 className="mt-5 text-xl font-semibold">{receipt.product}</h3>
      <p className="mt-2 text-sm text-slate-600">Application reference</p><p className="break-all font-mono text-sm">{receipt.id}</p>
      <div className="mt-6 rounded-xl bg-[#eef3df] p-5">
        <p className="font-semibold capitalize">{receipt.status === "submitted" ? "Awaiting review" : receipt.status}</p>
        <p className="mt-2 text-sm leading-6">{receipt.status === "submitted" ? "Your association has received your application. Check this page for its decision. No further submission is needed." : receipt.status === "approved" ? "Your application has been approved. Your current membership details are shown below." : receipt.status === "rejected" ? "Your application was not approved. Contact your association if you would like to discuss the decision." : "Contact your association for help with this application."}</p>
      </div>
      <dl className="mt-6 grid gap-5 sm:grid-cols-2">
        <div><dt className="text-sm text-slate-600">Submitted</dt><dd className="mt-1 font-medium">{portalDate(receipt.submittedAtUtc)}</dd></div>
        <div><dt className="text-sm text-slate-600">Quoted total at submission</dt><dd className="mt-1 font-medium">{portalMoney(receipt.quotedTotal, receipt.currency)}</dd></div>
        <div><dt className="text-sm text-slate-600">Requested start</dt><dd className="mt-1 font-medium">{portalDate(receipt.requestedStartUtc)}</dd></div>
        <div><dt className="text-sm text-slate-600">Quoted end</dt><dd className="mt-1 font-medium">{receipt.requestedEndUtc ? portalDate(receipt.requestedEndUtc) : "Not recorded"}</dd></div>
      </dl>
      <p className="mt-4 text-sm text-slate-600">This confirms your application, not a payment. No payment was collected through this portal.</p>
      {receipt.membership && <section className="mt-6 rounded-xl border border-slate-200 p-5" aria-label="Resulting membership"><h4 className="font-semibold">Your membership</h4><p className="mt-2 capitalize">{receipt.membership.status}</p><p className="mt-2 text-sm">{portalDate(receipt.membership.startsAtUtc)} – {portalDate(receipt.membership.endsAtUtc)}</p></section>}
      {receipt.submittedNotes && <section className="mt-6"><h4 className="font-semibold">Your submitted notes</h4><p className="mt-2 whitespace-pre-wrap break-words text-sm leading-7">{receipt.submittedNotes}</p></section>}
      <section className="mt-6"><h4 className="font-semibold">Accepted membership terms</h4>
        {receipt.acceptedTerms ? <><p className="mt-2 whitespace-pre-wrap break-words text-sm leading-7">{receipt.acceptedTerms}</p>{receipt.termsAcceptedAtUtc && <p className="mt-3 text-sm text-slate-600">Accepted {portalDate(receipt.termsAcceptedAtUtc)}</p>}</> : <p className="mt-2 text-sm text-slate-600">A portal terms snapshot was not recorded for this application. Contact your association for a copy.</p>}
      </section>
      <ApplicationEvidence path={`/api/v1/portal${accountId ? `/organisations/${accountId}` : ""}/applications/${id}/evidence`} />
      <button className="mt-7 rounded-full border border-[#183a31] px-5 py-3 font-semibold print:hidden" onClick={() => window.print()}>Print application receipt</button>
    </>}
  </section>;
}

