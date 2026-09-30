# Standard functionality comparison and delivery plan

29 September 2026. Source baseline: `8c82222`, initially clean working tree. Reference: `C:/Users/ramish/Desktop/Standard Functionality.docx`. This is a source-level comparison, not a claim that every existing workflow has passed runtime acceptance.

The existing modules cover much of the basic record and transaction lifecycle. The largest documented differences are configurable lookups, richer CRM related lists, communication preferences and history, and relational reporting. Portal and CMS requirements cannot be inferred from their headings alone.

## Scope and interpretation

- Compare existing capabilities module by module, then implement discrete gaps. Entirely absent capabilities remain future work; they are listed separately and are not commitments to build them now.
- Treat the attachment as reference requirements, not operational instructions. Its opening title says “oomi Standard Functionality”; its body refers to Nexora. Compare behaviours rather than copying product claims or naming.
- Portal, CMS, Setup, Third Party Integrations, Marketing and Committees contain only “Types” placeholders. Membership, Events and Finance mainly contain lists or placeholders. These are not sufficiently detailed acceptance criteria for new workflows.
- Preserve one active membership per Contact or Account. The reference's multiple-membership statement may describe historical records, but must not override the established active-entitlement rule.
- Keep file size limits and scanning. The reference's unlimited-attachment statement is a design difference to resolve, not a reason to remove operational safeguards.
- Do not interpret the reference's GDPR claims as proof of legal compliance. The comparison below is functional only.

## Existing module comparison

Paths below are repository-relative evidence locations. “Partial” means source exists but does not cover the documented behaviour; “unspecified” means the document cannot establish a gap.

| Module or area | Existing source evidence | Difference from the reference | Disposition |
| --- | --- | --- | --- |
| Navigation | `apps/web/src/components/app-shell/app-shell.tsx` has grouped navigation and a collapse button | Collapse button has no action | Existing UI gap |
| Record layout | `features/shared/configured-sections.tsx` orders sections using automation settings | No hide/collapse/expand preferences in this component | Partial; implement within current layout system |
| Accounts and Contacts | `CrmEntities.cs`, `CrmRequests.cs`, `crm-workspace.tsx` provide record identity, status, category, owners, addresses and communication methods | Structured title, first/last name, salutation, account type and separate mailing/billing semantics are not first-class fields; generic custom fields do not establish those behaviours | Partial; agree field mapping before schema changes |
| Account and Contact associations | `AccountContactEndpoints.cs` stores roles, job title, dates and history; list API supports pagination/history | CRM detail requests page 1 with history disabled; no page controls. Main Contact flag is not the Contact's Main Account; department is absent | First implementation: expose existing history and pages. Main Account/department need a separate model change |
| Duplicate merge | No merge endpoint in CRM module or transfer handlers; import detects conflicts | Reference describes account merge | Future capability; excluded from initial work |
| Account relationships | Generic `RecordRelationship` links records with labels | No managed relationship-type lookup | Partial; depends on lookup foundation |
| Notes | `Note` stores body, actor and creation time; record UI can add notes | No type, chosen note date, keywords, chosen owner, important alert, or account roll-up of associated Contact notes | Existing module gap; stage metadata and alerts before cross-record aggregation |
| Attachments | `RecordFile`, `CrmCompletion.cs` and upload handlers provide bounded upload, quarantine, scan and delete | No type, chosen date/owner or attachment shared across one record per entity | Partial. Retain size and scan rules; shared-file model is later work |
| Addresses | `Address` supports multiple record addresses | No cease/history state, managed address types, county-to-region mapping or separate billing designation | Existing address gap; geographic lookup catalogue is future foundation |
| Communication methods and history | Communication methods hold values and consent evidence; activity timeline records actions; membership has communication drafts | Timeline is not a searchable communication log with category/direction/mode/nature; no general automatic sent-email logging | Partial; no external email delivery claim |
| Profiles/tags | `RecordTag`, CRM search and UI support individual tags | No bulk or scheduled tag assignment and no tag taxonomy across all entities | Keep existing tagging; bulk operation can follow query scope. Scheduler is future |
| Import/global update | `CrmTransfer.cs` validates and commits fixed-column CSV; Work has preview/commit imports | No Excel column mapping or membership-rate import | Partial; do not describe current import as general global update |
| Saved views/query builder | CRM saved views and Insights query/export support bounded filters | No relational criteria editor, public/shared queries, mail merge or scheduled result actions | Improve existing filters/views separately; relational builder and scheduler are future capabilities |
| User rights | Identity permission catalogue provides module/action permissions and tenant boundaries | No general per-field or per-record ACL editor; add/edit/delete commonly share manage permission; MFA option not evidenced in current local adapter | Preserve current enforcement. Fine-grained rights/MFA are future work |
| Consent | `CrmDirectory.MarketingEmailAsync` excludes contacts with denied email consent; campaign preparation uses it | No purpose-specific mailing preferences/list subscriptions on Account/Contact, no global Stop Mailings field, no corresponding portal preference editor | Existing consent model is partial; design suppression consistently before extending UI |
| Data portability/removal | CRM CSV export returns selected columns for current records | Not a complete per-subject export; no dedicated audited subject erasure workflow | Future capability; do not equate archive with erasure |
| Audit | CRM `Activity` and module histories record actor/action/time/correlation; identity has audit controls | Reference promises what changed for every data change; action summaries are not a universal field-level before/after audit | Partial; define covered operations before claiming completeness |
| Membership | Products, policies, applications, decisions, active subscriptions, renewal reviews, certificates and communication drafts exist | Listed bands/resign reasons/upgrades/downgrades/add-ons lack detailed definitions; category/rate/status are already represented in existing models | Preserve working lifecycle. Unimplemented listed capabilities are future; clarify semantics before expanding |
| Events | Offerings, sessions, enrollment, capacity/status and results exist | Document lists rate/delegate/special-requirement/table/venue/rule capabilities with no workflows; venue is currently text | Do not build absent submodules from headings. Existing registrations can be exposed in appropriate related lists |
| Account event registrations | Events `CrmContextProvider` only selects Contact enrollment | Account record does not aggregate events for associated Contacts as documented | Existing integration gap; decide current versus historical association inclusion first |
| Finance related lists | Finance stores orders, invoices, receipts, credits, refunds and allocations; CRM context provider selects invoices only | Receipts, credit notes, refunds, sales history and ledger are not all presented on CRM records | Existing integration gap; extend provider with permission checks and correct customer joins |
| Marketing and Committees | Engagement has communities, memberships, meetings and campaign audience preparation | Source document supplies no meaningful workflow beyond “Types” | Unspecified; no invented redesign |
| Tasks | Work supports person/team assignment, priorities/statuses, due dates, notifications and Contact linking | Status/priority choices are fixed; no configured task-type lookup; due dates alone do not establish reminder scheduling | Partial; lookup configuration later, reminder scheduling future |
| Reports | `ReportingEndpoints.cs` exposes dataset query/export and counts | Not the 11 membership, 4 finance and 11 event reports listed. Many need unavailable fields (bands, delegate types, abstracts, payment methods, Xero reconciliation) | Only plan reports backed by current models; skip reports dependent on absent capabilities |
| Workflow/templates | Membership quote/renewal policy and automation rules exist; Content has publishing scheduling | No generic document/email template library, selection rules or Query Builder scheduler | Future capabilities; CMS scheduling does not satisfy generic workflow scheduling |

Backend shorthand refers to `backend/src/Modules/Nexora.Modules.<Module>/`; frontend shorthand refers to `apps/web/src/`. Reporting evidence is under `backend/src/Nexora.Api/`.

## Three-platform boundaries

| Platform | Existing implementation | Reference coverage | Action |
| --- | --- | --- | --- |
| CRM | Staff operations, identity permissions, records and connected activity | Detailed CRM behaviours above | Start here, retaining existing module boundaries |
| Portal | Profile/address, memberships, personal and organisation applications, evidence, organisation grants, notifications; `features/portal/` and Identity portal endpoints | Only “Types”; general text mentions web-visible lookups, limited uploads and mailing preferences | Preserve current self-service. Carry explicit lookup/consent requirements forward when their shared models exist; do not expose staff-only fields automatically |
| CMS/Website | Sites/pages, media, navigation, publishing and portal presentation editing; Content module and `features/content/` | Only “Types” | No supported CMS redesign requirement; await a behavioural specification |

## Work sequence and acceptance

1. **Accounts and Contacts — existing associations.** Expose current/all association selection, start/end dates and reason, pagination, and explicit retry on failure. Ended links remain read-only; preserve all existing association APIs/rules. Check both Account and Contact views, more than 25 links, empty data, error recovery and archived counterparts.
2. **Common UI.** Wire sidebar collapse and extend record section controls through the existing layout mechanism. Keep navigation accessible and section preferences separate from authorization.
3. **CRM notes.** Add metadata and important-note display with backwards-compatible defaults, migration and permission/tenant tests. Define note-owner and date semantics. Use a static accessible alert instead of flashing text.
4. **CRM addresses and lookups.** First introduce cease/history semantics without deleting historical addresses. Define tenant-managed lookup ownership, inactive-value preservation and portal visibility before reusing it for types/statuses. This foundation is a separately scoped future capability, not a blanket conversion of every status state machine.
5. **Existing connected activity.** Expose receipts/credits/refunds and sales history already available in Finance. Design Account event and note roll-ups with explicit association rules. Maintain permission filtering and bounded pages.
6. **Existing consent and views.** Specify stop-mailing precedence and purpose-level consent; extend the current consent source of truth. Improve existing saved-view/query/report features only where stored data supports the requested output. Keep absent scheduler, template, integration and compliance workflows out of this delivery.
7. **Portal and CMS.** Compare against detailed requirements when supplied. For any shared CRM change, separately verify the existing member journey and website publishing behaviour; frontend route presence is not acceptance evidence.

This sequence is not an estimate or a commitment to implement all partial features in one change. Each slice needs source review, focused verification and a recorded outcome before the next. No new production identity, payment or external delivery integration is included.

## Delivery record

- Analysis and plan written before application edits.
- Slice 1 implemented in `apps/web/src/features/crm/crm-workspace.tsx`: current/all associations, start/end dates, end reasons, 25-row pagination, last-page recovery, explicit failure/retry, archived-counterpart indication and immutable ended-link controls. Existing APIs, persistence and business rules are unchanged.
- Browser testing initially caught the history checkbox disappearing on a failed request. The control now remains mounted through loading and error states.
- TypeScript (`npx tsc --noEmit`) and focused ESLint passed. Release backend build passed. Targeted `AccountContactTests` passed 4/4 with SQL Express, including fresh migration replay, no pending CRM model changes and preservation of ended relationships. The earlier run without SQL configuration had one skip; the configured run supersedes it.
- Browser verification: **2/2 passed** in Edge against a development frontend and disposable SQL Express catalog. `apps/web/tests/crm-association-history.spec.ts` covers both Account and Contact views, empty lists, 26 associations, history dates/reason, ended links without mutation buttons, archived counterparts, page reset, simulated 503/retry and mobile width. Runner: task-local adaptation of `scripts/Test-Automations.mjs` (API 5196, frontend 3116, isolated Next output). Test services were stopped and the disposable catalog removed.
- No production frontend build, whole-system regression, external-service acceptance or deployment is claimed by this slice.
- Other gaps remain planned or future as labelled above. No completion percentages changed.
