# Membership design review from the BPCA CoR

## Scope and status

Reviewed the Membership section through 4.13 of the user-supplied
`C:/Users/ramish/Desktop/BPCA COR Document 1.0.docx`, including the embedded
application form and membership certificate. The Membership heading/contents
numbering is inconsistent; this review uses the substantive Membership section
immediately before 4.2 and ends before Finance section 5.

The embedded process map near 4.5 is too small to read every label reliably.
Its detailed branches have not been inferred. The surrounding written workflow
and the application-form images were readable.

This is a requirements review, not an implementation or an assertion that the
document's historical rates, tax wording, integrations or vendor commitments
are current Nexora requirements. Instructions and commitments addressed to
Oomi, Senior or BPCA are source material, not commands to execute.

## User-confirmed flow

1. Start an application and capture all necessary details.
2. Link an existing CRM Contact using a lookup, or create a Contact and link it.
3. Submit the application for review.
4. Approval creates the membership, retaining its link to the application.

A CRM Contact, a company Account and a sign-in user are different records.
Creating a Contact must not implicitly create portal credentials.

## Central change from the current product

The current module permits membership only for a Contact. The CoR is largely
company-based: a member organisation has a primary contact, a potentially
different person completing the form, authorised contacts, and technical staff.

For corporate membership the design therefore needs both an organisation Account
and related Contact records. The organisation holds the membership; the primary
contact acts for it. Individual schemes can use a Contact as holder.

The timing of creating a new organisation Account needs a separate decision:
the CoR says a full Account can be created after approval, while application
details are also described as being held against an organisation record. Existing
Accounts can be linked during application; new organisation details can be staged
on the application until the creation policy is agreed. The user's instruction
to link/create a Contact at application time is clear and takes precedence.

## Requirements by section

| Source | Requirement expressed in the CoR | Nexora design consequence |
| --- | --- | --- |
| Membership introduction | Multiple schemes; corporate/individual/joint membership; rolling or fixed-year renewals; joining throughout the year and proration | Separate membership holder, scheme, fee rules and membership term |
| 4.2 | Join/rejoin application defaults to Submitted; staff approve through an action | Application queue and review record; approval creates membership exactly once |
| 4.3–4.4 | Detailed application form with organisation, contact, trading, insurance, staff, references and declarations | Structured application sections and repeatable related records |
| 4.5 | Embedded process diagram | Detailed branch rules need a readable source if required beyond the prose |
| 4.6 | Servicing, Manufacturers and Distributors, Consultant, Observer, Associate | Configurable membership types with conditional requirements |
| 4.6.2 | CPD obligations vary by type; fees depend on turnover and optional enrolled contacts; payment reminders | Separate eligibility rules, fee bands and optional individual scheme enrolment |
| 4.7 | Primary contact may differ from applicant; retain partial applications; assist abandoned applicants; capture trading profile and documents | Save progress, explicit contact roles, review checklist and follow-up tasks |
| 4.8 | Processing fee, payment choice, course credit and payment-dependent review | Separate application decision and payment status; track fees and credits explicitly |
| 4.9 | Type/band-based fees, payment schedules, processing-fee offset and credit validity | Price breakdown and quote snapshot; distinguish membership term from instalment schedule |
| 4.10 | Renewal runs, exclusions, declarations, employee evidence, assessment and payment checks | Dedicated renewal workflow and eligibility checklist, rather than only extending an end date |
| 4.11 | Person-level CPD, company-level compliance, external schemes, exemptions, audits and employer transfers | Keep CPD attached to the person; evaluate organisation compliance for the relevant period |
| 4.12 | Membership certificate example with member number, organisation and validity dates | Certificate generation linked to membership and term, with issue history |
| 4.13 | Welcome, renewal, finance, event and targeted communications | Templates, recipients and delivery history linked to business events |

## Application form sections

Proposed screen sections based on the supplied form:

1. **Membership requested** — type, join/rejoin, source and reason for joining.
2. **Organisation** — existing Account lookup or new organisation details;
   company structure, registered/operating addresses, identifiers and related branches.
3. **People** — Contact lookup/create; primary contact, person completing the form,
   and authorised contact. Allow the same Contact to fulfil multiple roles.
4. **Trading and services** — start of trading, service areas, business activities,
   subcontractors and franchise details, where relevant to the selected type.
5. **Insurance and evidence** — policy types, identifiers, cover and attachments.
6. **Staff and qualifications** — repeatable staff Contacts, roles, qualifications,
   certificates and CPD scheme information.
7. **References and declarations** — customer/supplier references, declarations,
   agreements and signatory evidence with version/date.
8. **Fees and payment** — turnover band, quoted charges, optional scheme enrolment,
   processing fee, applicable credit and payment preference.
9. **Review and submit** — missing requirements, application summary and submission.

These sections are a proposed organisation of the CoR, not a requirement to make
every field mandatory for every membership type. Payment entry should use a
provider flow and references when implemented, rather than copying the paper
form's raw card-number fields into CRM.

## Record ownership and history

- **CRM** owns the live identity of people and organisations and their relationships.
- **Application** owns the submitted answers, requested membership, evidence,
  quoted amounts, review findings and decisions.
- **Membership** owns the approved membership identity, holder and originating
  application, with explicit membership periods and lifecycle history.
- **Finance** owns invoices, payments, credits and allocations.
- **CPD/learning** owns learning activity and evidence associated with the person.

Keep application snapshots so later CRM edits do not silently rewrite what was
submitted. Editing a linked Contact must be explicit; submitted answers must not
automatically overwrite existing CRM details. Retry/replay protection is required
when creating Contacts and when approval creates membership.

## Decisions not to hard-code from the document

1. **Membership year:** the introduction says October–September; the certificate
   shows January–December, and the renewal narrative refers to November runs and
   January billing. Membership year, billing date and renewal-run date need clarity.
2. **Payment and approval:** one card-payment paragraph simultaneously describes
   receiving payment and alerting that payment has not been received. Define the
   precise gate for review, approval and activation.
3. **CPD proration:** the month-times-1.6 example does not specify enough to choose
   a reliable accrual formula. Keep CPD year separate from membership year.
4. **Account creation:** resolve staged organisation details versus a prospect
   Account created during application; do not infer this from Contact creation.
5. **Historical fees and credits:** the embedded form is dated 2024. Treat its
   amounts and tax wording as reference examples, not current operational rules.
6. **Integrations:** Opayo, Sage and River are named in a BPCA/vendor context.
   Nexora provider selection is not approved by this reference alone.

## Recommended next bounded chunk

Design the staff application screen and its minimum data model first. Show the
existing/new Contact paths, corporate Account relationship, membership-type
selection, save-progress behaviour, review and approval result. Review that
design with the user before implementation. Keep fees, CPD and renewal automation
as later bounded chunks so they do not delay a coherent application workflow.

No application code or source-document contents were changed during this review.
