# Accounts and Contacts alignment with the BPCA CoR

## Review scope

Reviewed sections 3.1 and 3.2 of the supplied BPCA COR Document 1.0.docx against
Nexora's CRM entities, request contracts, endpoints, persistence and frontend.
Read the document as requirements evidence, not instructions to execute.
No embedded images occur in these two sections. No product code changed.

Related review: [Membership requirements](MEMBERSHIP-COR-REVIEW.md).

## Existing foundation

Nexora stores Accounts and Contacts as CRM records with a fixed kind. It supports
names, lifecycle status, category, ownership, addresses, communication methods,
consent evidence, relationships, notes, tags, attachments, custom fields, audit
activity, search, paging, editing, archive and restore. Contacts can exist without
an organisation, and names can be edited without evidence attachments.

These features are useful foundations. They do not yet represent the specific
organisation/contact structure required by the CoR.

## Source-to-product comparison

| CoR requirement | Current product evidence | Required change |
| --- | --- | --- |
| 3.1: migrate Account fields and apply mandatory fields/validation | Standard CRM fields and four custom-field data types exist; complete source field inventory is not provided in the CoR | Obtain the field inventory for full mapping; define required fields by workflow and relevant structured organisation fields |
| 3.1: Account applies through a form, including emailed/scanned/paper submissions | Generic CRM attachments exist; Membership accepts only existing Contacts | Link applications to corporate Accounts and Contacts; permit staff entry and source-document attachment |
| 3.2: member-company, non-member-company, affiliate and purchaser Contacts | Standalone Contacts and generic category/tags exist | Preserve these independent relationships; show contextual membership and scheme information rather than one overloaded category |
| 3.2: signup type such as owner, sole trader, technician, support staff, researcher, other | Category is free text | Add a configurable structured contact-type field/list with validation |
| 3.2: member/non-member depending on Account membership | CRM lifecycle status is active/prospect/inactive; no corporate membership projection | Derive/display Account membership separately from CRM lifecycle and personal scheme enrolment |
| 3.2: multiple roles for a Contact in an Account | Directed relationships have a target ID and free-text label | Add explicit organisation-contact associations and multiple controlled role assignments |
| 3.2: main contact mandatory when the Account joins membership | No primary-contact rule | Validate main-contact selection at the relevant submission/approval boundary, not on every CRM record creation |
| 3.2: authorised contacts receive enhanced portal access | Staff Identity permissions exist; generic relationship labels have no portal semantics | Model the designation explicitly; implement scoped portal authorisation separately, never equate it with staff administration |
| 3.2: technical versus non-technical employees drive compliance | No dedicated classification | Record structured work classification on the organisation association; keep it distinct from contact type and communication role |
| 3.2: BPCA Registered categories and qualification criteria | General fields can store labels but no dedicated scheme-enrolment model | Add configurable scheme enrolments and qualification evidence later; distinguish BPCA Registered Affiliate from the general Affiliate scheme |
| 3.2 BR01: editable Contact name without evidence | Current CRM supports name editing | Retain with audit history |
| 3.2 BR02: identify duplicates and merge after review | No duplicate/merge workflow | Add candidate matching and explicit selection first; implement reviewed merge with cross-module reference handling as a separate change |
| 3.2: reassign a Contact to another company or leave it unassigned | Generic links can be added/deleted, without a dedicated move/history workflow | End the old association and create the new one while retaining the Contact ID and history |
| 3.2: archive only when requested | Manual archive/restore exists; no automatic archive identified | Preserve explicit archive action; membership lapse/employer departure must not automatically archive Contacts |
| 3.2: website users may be members, non-members, affiliates or purchasers | Staff sign-in identity exists; member portal is future scope | Keep Contact and login identity separate; do not require membership for every future portal user |
| 3.2: email preferences and segmented mailing lists | Generic per-communication consent and tags exist | Add topic preferences, channel consent and change evidence; future delivery must honour both |

## Recommended relationship model

Keep the existing Account and Contact identities and add a structured association:

- Organisation Account reference.
- Contact reference.
- Relationship start/end dates for current and former associations.
- Job title where relevant.
- Main-contact designation.
- Multiple organisation-scoped contact roles.
- Technical/non-technical classification, initially unknown until declared.
- Authorised-contact designation, with any actual portal permissions handled
  explicitly by a separate authorisation process.

Account details should show these people in a Contacts section, with search,
link-existing and create-and-link actions. Contact details should show the
organisation relationship and history. Read both views from the same association
so staff do not maintain two independent reverse links.

The CoR does not settle whether a person may have several simultaneous employer
relationships or whether there is exactly one main contact. Agree these cardinality
rules before adding restrictive database constraints. Existing free-text links
must not be silently converted into privileged or primary associations.

## Keep classifications separate

- **Contact type:** business owner, sole trader, technician, support staff, etc.
- **Organisation role:** finance, training, membership, events, PR, etc.; multiple.
- **Work classification:** technical or non-technical for compliance purposes.
- **Individual scheme category:** trainee, technician, advanced technician, etc.
- **Organisation membership standing:** obtained from the organisation membership.
- **CRM lifecycle:** active/prospect/inactive, independent of membership standing.

The CoR calls one role “Assessment contract”; confirm whether “Assessment contact”
was intended before seeding the label.

## Application integration

The user requires an application to find/create and link a Contact, then create
membership on approval. For the corporate scenario:

1. Identify the organisation or capture its application details.
2. Find/create the person completing the application.
3. Select that person as main contact or find/create a different main contact.
4. Capture other people and relevant roles/evidence.
5. Validate required information before submission and decision.
6. On approval, create the corporate membership once and preserve application,
   organisation and Contact links.

Contact lookup must be usable by authorised staff with name/email/company context.
Matching should suggest candidates rather than silently join people based only on
name or email. Permission to manage Membership does not currently imply permission
to create CRM records; define that boundary explicitly when integrating the form.

The timing of creating a brand-new Account remains the open decision recorded in
the Membership review. New application answers should not silently overwrite a
linked CRM record, and moving a Contact must retain learning/CPD history.

## Proposed work order in bounded chunks

1. Agree the Account–Contact association fields and main-contact rules.
2. Add the association persistence and focused integrity/tenant tests.
3. Add staff link-existing-Contact UI on Account details.
4. Add create-and-link Contact with duplicate suggestions and safe retry handling.
5. Show organisation roles and relationship history on Contact details.
6. Connect application creation to these relationships.
7. Add submission/approval validation and corporate membership creation.
8. Add scheme enrolments, preferences and reviewed merging in separate later work.

Each implementation chunk should stop within the user's 15-minute working limit
and report verified versus unfinished work before requesting the next chunk.
Do not treat the whole work order as one implementation batch.

## Evidence locations

- backend/src/Modules/Nexora.Modules.Crm/Domain/CrmEntities.cs
- backend/src/Modules/Nexora.Modules.Crm/Contracts/CrmRequests.cs
- backend/src/Modules/Nexora.Modules.Crm/CrmModule.cs
- backend/src/Modules/Nexora.Modules.Crm/Infrastructure/CrmDbContext.cs
- backend/src/Modules/Nexora.Modules.Crm/Infrastructure/CrmDirectory.cs
- apps/web/src/features/crm/crm-workspace.tsx
- apps/web/src/features/crm/types.ts
- docs/stages/STAGE-03-CRM-FOUNDATION.md
