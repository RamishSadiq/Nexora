"use client";
import { RecordSection } from "./record-section";
import styles from "./record-detail.module.css";
import Link from "next/link";
import { useEffect, useState } from "react";
import { crm } from "./api";
import type { FinancialArrangement } from "../membership/membership-financial-arrangement";
import { optionLabel, type MembershipOptionsDetails } from "../membership/membership-options";
import type { MembershipAdministrativeDetails } from "../membership/membership-administration";
import { classificationLabel, type Classifications } from "../membership/membership-classifications";

type Membership = { financialArrangement?:FinancialArrangement|null; options?:MembershipOptionsDetails|null; isArchived?:boolean|null; archivedAtUtc:string|null; archiveReason:string|null; previousMembershipId:string|null; rejoinedOn:string|null; lifecycleDates?: { statusDate:string|null; resignDate:string|null; resignReason:string|null; reinstatementDate:string|null } | null; latestLifecycleEffectiveOn:string|null; originalJoinDate: string | null; classifications?: Partial<Classifications> | null; administration?: MembershipAdministrativeDetails | null; id: string; scope: string; accountId: string | null; accountName: string | null; productName: string; productCategory: string; status: string; startsAtUtc: string; endsAtUtc: string; rate: number; currency: string; applicationId: string; applicationDateUtc: string | null; applicationType: string | null; excludedFromRenewal: boolean | null; reviewNotes: string | null; latestLifecycleAction: string | null; latestLifecycleAtUtc: string | null; latestLifecycleReason: string | null };
type Page = { total: number; page: number; pageSize: number; items: Membership[] };
const button = "rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm disabled:opacity-50";
const date = (value: string) => new Date(value).toLocaleDateString("en-GB", { timeZone: "UTC", dateStyle: "medium" });
const unrecordedClassification = { definitionId: null, name: null, effectiveOn: null };
const lifecycleLabels: Record<string, string> = { "membership.activated": "Activated", "membership.renew": "Renewed", "membership.cancel": "Cancelled", "membership.lapse": "Lapsed", "membership.reinstate": "Reinstated" };

export function RecordMemberships({ id, kind }: { id: string; kind: string }) {
  const [result, setResult] = useState<Page | null>(null);
  const [page, setPage] = useState(1);
  const [revision, setRevision] = useState(0);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  useEffect(() => {
    let current = true;
    crm<Page>(`/records/${id}/memberships?page=${page}&pageSize=10`).then(value => { if (current) { setResult(value); setLoading(false); } }).catch(e => { if (current) { setError(e.message); setLoading(false); } });
    return () => { current = false; };
  }, [id, page, revision]);
  function load(next: number) { setLoading(true); setError(""); setPage(next); setRevision(n => n + 1); }
  return <RecordSection title="Membership details" keepMounted>
    <div className="mb-3 flex justify-end"><button className={button} disabled={loading} onClick={() => load(page)}>Refresh memberships</button></div>
    <p className="mb-4 text-sm text-slate-600">Membership records and renewal reviews are maintained in Membership.{kind === "contact" ? " Organisation entries show applications made by this Contact; the membership belongs to the organisation." : ""}</p>
    {loading ? <p role="status">Loading membershipsâ€¦</p> : error ? <p role="alert" className="text-sm text-red-700">{error}</p> : result && <>
      {result.total === 0 && <p className="text-sm text-slate-500">No memberships recorded.</p>}
      <ul className="space-y-4">{result.items.map(member => <li key={member.id}><details className={styles.membershipCard}>
        <summary className={styles.membershipSummary}><span className="font-semibold">{member.productName}</span><span className="rounded-full bg-slate-100 px-3 py-1 text-xs">{member.status}</span></summary><div className="p-4"><Link className="mb-4 inline-block text-sm text-indigo-700 underline" href={`/membership/members/${member.id}`}>Open membership record</Link>
        <dl className="grid gap-3 text-sm sm:grid-cols-2">
          <div><dt className="text-slate-500">Payment provider preference</dt><dd>{member.financialArrangement?.paymentProvider === "gocardless" ? "GoCardless" : member.financialArrangement?.paymentProvider === "stripe" ? "Stripe" : "Not recorded"}</dd></div>
          <div><dt className="text-slate-500">Recorded renewal month</dt><dd>{member.financialArrangement?.renewalMonth ?? "Not recorded"}</dd></div>
          <div><dt className="text-slate-500">Recorded renewal date</dt><dd>{member.financialArrangement?.renewalDate ?? "Not recorded"}</dd></div>
          <div><dt className="text-slate-500">Planned next renewal date</dt><dd>{member.financialArrangement?.nextRenewalDate ?? "Not recorded"}</dd></div>
          <div><dt className="text-slate-500">Recorded promotion code</dt><dd>{member.financialArrangement?.promotionCode ?? "Not recorded"}</dd></div>
          <div><dt className="text-slate-500">Payment arrangement</dt><dd>{member.financialArrangement?.paymentMethod ?? "Not recorded"} Â· {member.financialArrangement?.paymentFrequency ?? "Not recorded"}</dd></div>
          <div><dt className="text-slate-500">Paid by</dt><dd>{member.financialArrangement?.paidByName ?? "Not recorded"}</dd></div>
          <div><dt className="text-slate-500">Invoice to Account</dt><dd>{optionLabel(member.financialArrangement?.invoiceToAccount)} Â· {member.financialArrangement?.invoicingAccountName ?? "No Account recorded"}</dd></div>
          <div><dt className="text-slate-500">Recorded primary membership</dt><dd>{optionLabel(member.options?.primaryMembership)}</dd></div>
          <div><dt className="text-slate-500">Exempt joining fee</dt><dd>{optionLabel(member.options?.exemptJoiningFee)}</dd></div>
          <div><dt className="text-slate-500">Free membership</dt><dd>{optionLabel(member.options?.freeMembership)}</dd></div>
          <div><dt className="text-slate-500">Exclude from lapse</dt><dd>{optionLabel(member.options?.excludedFromLapse)}</dd></div>
          <div><dt className="text-slate-500">Member type</dt><dd>{classificationLabel(member.classifications?.memberType ?? unrecordedClassification)}</dd></div>
          <div><dt className="text-slate-500">Membership archive</dt><dd>{member.isArchived == null ? "Not recorded" : member.isArchived ? "Archived" : "Current record"}</dd></div>
          {member.archivedAtUtc && <div><dt className="text-slate-500">Archived at</dt><dd>{new Date(member.archivedAtUtc).toLocaleString()} Â· {member.archiveReason}</dd></div>}
          <div><dt className="text-slate-500">Rejoin date</dt><dd>{member.rejoinedOn ? date(member.rejoinedOn) : "Not recorded"}</dd></div>
          <div><dt className="text-slate-500">Previous membership</dt><dd className="break-all">{member.previousMembershipId ? <Link className="text-indigo-700 underline" href={`/membership/members/${member.previousMembershipId}`}>{member.previousMembershipId}</Link> : "Not recorded"}</dd></div>
          <div><dt className="text-slate-500">Status effective date</dt><dd>{member.lifecycleDates?.statusDate ? date(member.lifecycleDates?.statusDate) : "Not recorded"}</dd></div>
          <div><dt className="text-slate-500">Latest resignation date</dt><dd>{member.lifecycleDates?.resignDate ? date(member.lifecycleDates?.resignDate) : "Not recorded"}</dd></div>
          <div><dt className="text-slate-500">Resignation reason</dt><dd>{member.lifecycleDates?.resignReason ?? "Not recorded"}</dd></div>
          <div><dt className="text-slate-500">Latest reinstatement date</dt><dd>{member.lifecycleDates?.reinstatementDate ? date(member.lifecycleDates?.reinstatementDate) : "Not recorded"}</dd></div>
          <div><dt className="text-slate-500">Latest lifecycle effective date</dt><dd>{member.latestLifecycleEffectiveOn ? date(member.latestLifecycleEffectiveOn) : "Not recorded"}</dd></div>
          <div><dt className="text-slate-500">Original join date</dt><dd>{member.originalJoinDate ? date(member.originalJoinDate) : "Not recorded"}</dd></div>
          <div><dt className="text-slate-500">Membership ID (Nexora)</dt><dd className="break-all">{member.id}</dd></div>
          <div><dt className="text-slate-500">Legacy member reference</dt><dd>{member.administration?.legacyMemberReference ?? "Not recorded"}</dd></div>
          <div><dt className="text-slate-500">Membership pack sent</dt><dd>{member.administration?.membershipPackSent == null ? "Unknown" : member.administration?.membershipPackSent ? "Yes" : "No"}</dd></div>
          <div><dt className="text-slate-500">Membership pack sent date</dt><dd>{member.administration?.membershipPackSentDate ? date(member.administration?.membershipPackSentDate) : "Not recorded"}</dd></div>
          <div><dt className="text-slate-500">Membership belongs to</dt><dd>{member.scope === "personal" ? "This Contact" : member.accountName || "Organisation"}</dd></div>
          <div><dt className="text-slate-500">Product category (current)</dt><dd>{member.productCategory || "Not recorded"}</dd></div>
          <div><dt className="text-slate-500">Member category</dt><dd>{classificationLabel(member.classifications?.category ?? unrecordedClassification)}</dd></div>
          <div><dt className="text-slate-500">Category date</dt><dd>{member.classifications?.category?.effectiveOn ? date(member.classifications?.category?.effectiveOn) : "Not recorded"}</dd></div>
          <div><dt className="text-slate-500">Subscription band</dt><dd>{classificationLabel(member.classifications?.subscriptionBand ?? unrecordedClassification)}</dd></div>
          <div><dt className="text-slate-500">Subscription band date</dt><dd>{member.classifications?.subscriptionBand?.effectiveOn ? date(member.classifications?.subscriptionBand?.effectiveOn) : "Not recorded"}</dd></div>
          <div><dt className="text-slate-500">Application type</dt><dd>{member.applicationType ?? "Not recorded"}</dd></div>
          <div><dt className="text-slate-500">Application date</dt><dd>{member.applicationDateUtc ? date(member.applicationDateUtc) : "Not recorded"}</dd></div>
          <div><dt className="text-slate-500">Term start</dt><dd>{date(member.startsAtUtc)}</dd></div>
          <div><dt className="text-slate-500">Term end (exclusive)</dt><dd>{date(member.endsAtUtc)}</dd></div>
          <div><dt className="text-slate-500">Recorded membership rate</dt><dd>{new Intl.NumberFormat("en-GB", { style: "currency", currency: member.currency }).format(member.rate)}</dd></div>
          <div><dt className="text-slate-500">Excluded from renewal (current term)</dt><dd>{member.excludedFromRenewal == null ? "No review recorded" : member.excludedFromRenewal ? "Yes" : "No"}</dd></div>
          <div><dt className="text-slate-500">Latest recorded lifecycle action</dt><dd>{member.latestLifecycleAction ? `${lifecycleLabels[member.latestLifecycleAction] ?? member.latestLifecycleAction}${member.latestLifecycleAtUtc ? ` Â· ${date(member.latestLifecycleAtUtc)}` : ""}` : "Not recorded"}</dd></div>
        </dl>
        {member.latestLifecycleReason && <p className="mt-3 whitespace-pre-wrap break-words text-sm"><span className="text-slate-500">Action reason: </span>{member.latestLifecycleReason}</p>}
        {member.reviewNotes && <p className="mt-3 whitespace-pre-wrap break-words text-sm"><span className="text-slate-500">Current renewal review notes: </span>{member.reviewNotes}</p>}
        {member.administration?.remarks && <p className="mt-3 whitespace-pre-wrap break-words text-sm"><span className="text-slate-500">Membership Remarks: </span>{member.administration?.remarks}</p>}
        <a className="mt-3 inline-block text-sm text-indigo-700 underline" href={`/membership/applications/${member.applicationId}`}>Open membership application</a>
      </div></details></li>)}</ul>
      {result.total > 0 && <nav aria-label="Membership pages" className="mt-4 flex flex-wrap items-center gap-3 text-sm"><button className={button} disabled={page <= 1} onClick={() => load(page - 1)}>Previous memberships</button><span>Page {page} of {Math.max(1, Math.ceil(result.total / result.pageSize))} Â· {result.total} memberships</span><button className={button} disabled={page * result.pageSize >= result.total} onClick={() => load(page + 1)}>Next memberships</button></nav>}
    </>}
  </RecordSection>;
}
