"use client";
type Application = { id: string; contactName: string; accountName?: string | null; status: string; requestedStartUtc: string; applicationType?: string; applicantRole?: string | null; applicantNotes?: string | null; termsAcceptedAtUtc?: string | null; termMonths?: number; decisionReason?: string };
export function MembershipReviewSummary({ items }: { items: Application[] }) {
  if (!items.length) return null;
  return <div className="my-3 space-y-3 text-sm">{items.map(item => <dl key={item.id} className="grid gap-x-5 gap-y-2 sm:grid-cols-2">
    <div><dt className="text-slate-500">Organisation</dt><dd>{item.accountName || "Individual applicant"}</dd></div>
    <div><dt className="text-slate-500">Application type and role</dt><dd>{item.applicationType || "Not recorded"} · {item.applicantRole || "No role supplied"}</dd></div>
    <div><dt className="text-slate-500">Requested term</dt><dd>{new Date(item.requestedStartUtc).toLocaleDateString()} · {item.termMonths ?? "—"} months</dd></div>
    <div><dt className="text-slate-500">Terms accepted</dt><dd>{item.termsAcceptedAtUtc ? new Date(item.termsAcceptedAtUtc).toLocaleString() : "Not recorded"}</dd></div>
    <div className="sm:col-span-2"><dt className="text-slate-500">Applicant notes</dt><dd className="whitespace-pre-wrap">{item.applicantNotes || "No notes supplied"}</dd></div>
    {item.decisionReason && <div className="sm:col-span-2"><dt className="text-slate-500">Decision reason</dt><dd>{item.decisionReason}</dd></div>}
  </dl>)}</div>;
}
