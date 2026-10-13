"""Editorial catalogue for the Nexora beginner guides.

Paths and search anchors are checked against the captured source at build time.
Keep explanations here so regenerated guides remain editable and reviewable.
"""
WEB = 'apps/web/src/'
APP = WEB + 'app/'
FE = WEB + 'features/'
MOD = 'backend/src/Modules/Nexora.Modules.'
BB = 'backend/src/BuildingBlocks/Nexora.BuildingBlocks/'
GUIDES = []

def guide(number, title, purpose, routes, frontend, backend, model, scope,
          journey, explanation, exercise, limits, related=()):
    GUIDES.append(dict(number=number, title=title, purpose=purpose, routes=routes,
                       frontend=frontend, backend=backend, model=model, scope=scope,
                       journey=journey, explanation=explanation, exercise=exercise,
                       limits=limits, related=list(related)))

guide(1, 'Nexora login page',
      'Understand the staff sign-in page, the password check and the session that opens CRM.',
      ['/login', '/login/help'], APP+'(auth)/login/page.tsx', MOD+'Identity/IdentityModule.cs',
      MOD+'Identity/Domain/NexoraUser.cs',
      ['Staff login and help', 'Provider availability', 'CSRF tokens', 'Session cookie and sign-out', 'Active tenant and permission claims'],
      ['The page draws the heading, form and animated background.', 'The form asks which sign-in provider is enabled.',
       'Submitting requests a CSRF token, then sends credentials to /api/v1/auth/login.',
       'The local identity adapter checks the user, tenant and password.',
       'The returned cookie lets protected CRM routes verify /api/v1/auth/session.'],
      'The browser draws the page but does not decide whether a password is correct. The C# Identity module uses ASP.NET Core Identity for that check. The protected layout reads the browser cookie and requests the session directly from the API. The original approved guide is retained in full as its own file.',
      (APP+'(auth)/login/page.tsx', 'Welcome back', 'Welcome to Nexora', 'Change only the visible welcome heading, save and refresh /login. The password check should behave exactly as before.'),
      'The local password adapter is restricted to Development and its enabled flag. Microsoft Entra, email verification and account recovery are not connected by the current staff login.', [20,24,30])

guide(2, 'Nexora platform setup and VS Code',
      'Open the right project, recognise its folders, and run CRM, Portal and CMS locally.',
      ['CRM on 3017', 'Portal on 3018', 'CMS on 3019', 'API on 5097'], 'apps/web/next.config.ts',
      'backend/src/Nexora.Api/Program.cs', 'backend/Nexora.slnx',
      ['Repository boundaries', 'Frontend and backend languages', 'Three debug profiles', 'Shared API startup', 'Environment variables and local database'],
      ['VS Code opens the Nexora repository folder.', 'A CRM, Portal or CMS debug profile runs its preparation task.',
       'Prepare-LocalDebug.ps1 builds or reuses the shared API and waits for readiness.',
       'Next.js starts with its own port and build output folder.', 'The frontend forwards /api requests to the configured API.'],
      'Nexora uses one Next.js web project and a C# backend made of modules. The three local frontend processes share source but have separate debug outputs. A .tsx file draws a screen, .css changes appearance, .cs runs server rules, and .json supplies configuration. Company Dash appears in the launch menu but belongs to a separate repository; its source is outside this manual.',
      ('apps/web/next.config.ts', 'NEXORA_API_URL', None, 'Read-only exercise: find the environment variable and its fallback. Compare it with the CRM launch profile. Write down why a terminal started without that variable can point to a different API; do not change a port.'),
      'The saved runtime path and SQL Express instance are workstation-specific. F5 dispatch itself is a manual action. Preparation can apply Development database migrations; it is not a production deployment procedure.', [1,30])

guide(3, 'Nexora workspace navigation and theme',
      'Find the shared sidebar, header, logo, responsive menu and staff workspace colours.',
      ['/dashboard', '/relationships', '/settings'], WEB+'components/app-shell/app-shell.tsx',
      MOD+'Identity/Security/NexoraClaimsPrincipalFactory.cs', MOD+'Identity/Domain/RolePermission.cs',
      ['Navigation groups and submenu links', 'Mobile menu', 'User account and sign-out', 'Permission-filtered navigation', 'Shared neon theme and headings'],
      ['The protected platform layout obtains a staff session.', 'AppShell receives the display name and permissions.',
       'The navigation array supplies labels, routes and icons.', 'Permission filtering hides unavailable groups.',
       'The selected route becomes the page content inside the shared shell.'],
      'The shell surrounds many CRM pages. Changing a sidebar label changes every page using that shell. A hidden menu does not secure the API; server authorisation still checks every protected request. Specific routes take precedence over the old [workspace] placeholder route, which still displays a Scheduled module message for its recognised fallback names.',
      (WEB+'components/app-shell/app-shell.tsx', 'Workspace</p>', 'Nexora workspace</p>', 'Change the small sidebar heading only. Keep the closing tag and class names. Refresh CRM and verify two different pages show the same changed heading.'),
      'The staff shell and staff login appearance are source-controlled. Portal and website presentation settings have their own CRM-managed controls. Do not describe staff theme CSS as a stored per-tenant setting.', [7,20,26,27])

guide(4, 'Nexora home dashboard',
      'Understand the welcome screen, period selector, report cards and recent-record summaries.',
      ['/dashboard'], FE+'insights/live-dashboard.tsx', 'backend/src/Nexora.Api/ReportingEndpoints.cs',
      BB+'Reporting/ReportDataset.cs',
      ['Welcome name', 'Report period', 'Permission-based datasets', 'Flip cards', 'Recent records and retry state'],
      ['The dashboard route checks the staff session.', 'LiveDashboard starts with its selected number of days.',
       'It gets available datasets from /api/v1/insights/datasets.', 'It requests matching records for each dataset with fromUtc and toUtc.',
       'Cards display totals and samples from those stored records.'],
      'The name comes from the session rather than a hardcoded person. useState stores the selected period in this mounted screen. useEffect loads data when the period changes; its active flag prevents a late response updating an unmounted screen. The counts describe records created in the period, not revenue or customer validation.',
      (FE+'insights/live-dashboard.tsx', 'Stored records created during the selected period.', 'Records created in your chosen period.', 'Replace only this explanatory sentence. Reload /dashboard and change the period. Check that card loading and totals still work.'),
      'The user needs reporting permission. The reporting service also filters datasets by module permissions and tenant. Counts are a point-in-time result; no automatic real-time stream is implied.', [19,20])

guide(5, 'Nexora CRM contacts',
      'Understand the contact list, create form, detail screen and connected person information.',
      ['/relationships/contacts'], FE+'crm/crm-workspace.tsx', MOD+'Crm/CrmModule.cs',
      MOD+'Crm/Domain/CrmEntities.cs',
      ['Contact creation and editing', 'Status category owner and team', 'Structured names and general information', 'Addresses communication social links and consent', 'Notes tags timeline photo and connected memberships'],
      ['The contacts route passes contact as the record kind.', 'useCrmListing requests /api/v1/crm/records with filters and paging.',
       'Selecting a row loads its detail and related sections.', 'The create or edit form sends JSON with the record kind and version.',
       'The CRM module validates the request and saves tenant-scoped records and activity.'],
      'Contacts and Accounts use the same CrmWorkspace with a different kind. Small components handle general information, communication permissions, addresses and associations. A field label is the readable text; a name or JSON key is the address of the data. Change the label without renaming the key. Version values help detect another user changing the same record.',
      (FE+'crm/crm-workspace.tsx', 'Label Name', 'Contact display name', 'Change the contact label text only inside the kind-based Label. Leave name="name" intact. Open the Contact create form, confirm the new label, then undo without creating a real contact.'),
      'Contact access requires crm.read; writes and files need their own permissions. An archived record remains stored. Editing labels does not migrate data or change matching and consent rules.', [6,7,8,18,29])

guide(6, 'Nexora CRM accounts and associations',
      'Understand company records and the history linking people to organisations.',
      ['/relationships/accounts'], FE+'crm/crm-workspace.tsx', MOD+'Crm/AccountContactEndpoints.cs',
      MOD+'Crm/Domain/AccountContact.cs',
      ['Account forms and general information', 'Contact to Account links', 'Start and end dates', 'Department job title classification and roles', 'Main and authorised contacts', 'Transfer and association history'],
      ['The accounts route selects account as the record kind.', 'Opening an Account loads its saved detail.',
       'The association panel asks for currently linked Contacts and optional history.',
       'A link or change sends dates, role information and the expected version.',
       'The backend checks tenant boundaries and preserves association history.'],
      'A Contact is a person and an Account is an organisation. Their association is a separate record, so a person can change organisation without rewriting their identity. Ending an association stores its end date. Authorised-contact flags describe the relationship, but explicit portal grants control member access to company data.',
      (FE+'crm/crm-workspace.tsx', 'Account Name', 'Organisation name', 'Replace this visible Account label only. Open the Account form and check the Contact form still uses its own label. Leave kind and the input name unchanged.'),
      'Do not assume an association grants portal access. Ending or transferring links has operational consequences; the beginner exercise changes wording only. Existing history must be preserved.', [5,8,20,25])

guide(7, 'Nexora record listings and saved views',
      'Learn the shared search, filters, column chooser, sorting and saved-view code used across modules.',
      ['/relationships/contacts', '/membership/members', '/finance/orders', '/insights'],
      FE+'shared/filter-view-toolbar.tsx', MOD+'Crm/CrmListingSorting.cs', MOD+'Crm/Domain/CrmEntities.cs',
      ['Search and filter toolbar', 'Column chooser', 'Sortable headers', 'Client and server sorting', 'Personal saved views', 'Paging selection retry and all records'],
      ['A workspace passes rows, filters and callbacks to shared listing controls.',
       'Search and filters update query state or local filtering, depending on the module.',
       'A sortable header asks for an ascending or descending key.',
       'CRM and Membership validate supported server sort keys; some smaller lists sort loaded rows in the browser.',
       'The refreshed list renders with the chosen visible columns.'],
      'Shared components are used by several workspaces, so a wording change can affect multiple modules. useCrmListing loads server pages; useEntityListing supplies shared controls and client matching. A saved view is a stored query definition, not a copy of the records. Column visibility and saved filters are different pieces of state; inspect the relevant hook before changing persistence.',
      (FE+'shared/column-chooser.tsx', 'Columns', None, 'Read-only exercise: locate the visible Columns label, then trace where ColumnChooser is imported. Write down two workspaces that would be affected by changing this shared component.'),
      'Sorting behaviour differs between server-paged and locally loaded lists. Keep the API sort allowlists and UI keys aligned. A new column needs a data source and corresponding cell; renaming its label alone cannot create stored data.', [5,6,9,19])

guide(8, 'Nexora record profiles and related information',
      'Understand structured profile fields, taxonomy, audit history and record sections.',
      ['/relationships/contacts', '/relationships/accounts'], FE+'crm/record-profiles.tsx',
      MOD+'Crm/CrmProfiles.cs', MOD+'Crm/Domain/RecordProfiles.cs',
      ['Contact and Account profiles', 'General information', 'Profile definitions and typed fields', 'Taxonomy and departments', 'Communication methods permissions and social details', 'Data audit and connected activity'],
      ['The record detail loads the available profile definitions.', 'A profile component draws fields based on the definition.',
       'Saving sends typed values and a version through the CRM API.',
       'The server validates field definitions and persists values for that record.',
       'Audit and timeline components explain changes and connected module activity.'],
      'A profile definition describes which fields appear and how they are typed. A profile value stores one record\'s answer. This differs from adding a fixed property to a C# class, which may require a database migration. A department lookup and a free-text job title also have different storage rules. Follow the definition, value and UI component together.',
      (FE+'crm/record-profiles.tsx', 'export function', None, 'Read-only exercise: find the exported profile component and its imports. Follow RecordProfiles to its API requests, then find the matching server file. Identify where a displayed profile name comes from data rather than hardcoded JSX.'),
      'The browser may show configured sections, but validation stays on the server. Historical audit rows are evidence of stored changes, not proof that every external system has synchronised.', [5,6,21])

guide(9, 'Nexora member records and subscriptions',
      'Understand the member listing, subscription detail, lifecycle changes and membership administration.',
      ['/membership/members', '/membership/members/[id]'], FE+'membership/membership-workspace.tsx',
      MOD+'Membership/MembershipModule.cs', MOD+'Membership/Domain/MembershipEntities.cs',
      ['Member list and detail', 'Join dates and rejoin selection', 'Renew cancel lapse and archive actions', 'Administration classifications and financial arrangements', 'Options joining fees and timeline'],
      ['The members route loads the staff session and required permissions.',
       'MembershipList builds the member search, paging and sort request.',
       'Opening a subscription loads its connected detail and administration sections.',
       'A lifecycle action includes a version and reason.',
       'The module validates the transition, saves changes and records history.'],
      'A Contact is not the same record as a subscription. The subscription links a subject to a product, term and status. Renewal, rejoin and cancellation rules belong in the server files, while individual frontend components edit dates, classifications and arrangements. Trace the id carefully: a Contact id and a membership id are not interchangeable.',
      (FE+'membership/membership-workspace.tsx', 'Member subscriptions', 'Membership subscriptions', 'Change the accessible table caption text only. Open the Members list and inspect its caption in browser accessibility tools. Leave tab names, routes and status values unchanged.'),
      'A stored membership term is not proof of collected payment. Versions guard against conflicting edits. Do not run cancellation or renewal on an active real member as a learning exercise.', [10,11,15,25])

guide(10, 'Nexora membership applications and decisions',
      'Understand application forms, review details, evidence and staff approval or rejection.',
      ['/membership/applications', '/membership/applications/[id]'], FE+'membership/membership-application-details.tsx',
      MOD+'Membership/MembershipApplicationDetails.cs', MOD+'Membership/Domain/MembershipEntities.cs',
      ['Personal and company applications', 'Application details and review summary', 'Terms quoted rates and submission', 'Evidence files', 'Approve and reject decisions', 'Membership creation and notifications'],
      ['A staff or member form chooses a permitted product and subject.', 'A saved draft retains editable application details.',
       'Submission validates the draft and produces an application record.', 'Staff read the review summary and available clean evidence.',
       'An authorised decision calls server rules which record the outcome and any resulting membership.'],
      'Draft, submitted application and active membership are separate states. Client forms help collect information; the application command and service enforce product, account-link and eligibility rules. Review summaries bring related facts together. Evidence scanning controls which attachments may be opened; the presence of an uploaded file does not mean it is safe or approved.',
      (FE+'membership/membership-application-details.tsx', 'export function', None, 'Read-only exercise: locate the application detail component, identify its request path and expected version, then find the corresponding C# endpoint. Sketch the difference between saving details and deciding an application.'),
      'membership.manage and membership.decide are separate rights. Approval can create real records and notifications. Notification queue or inbox state does not prove external email delivery.', [9,11,24,25,29])

guide(11, 'Nexora membership setup and products',
      'Understand lookup values, membership products, prices and portal product settings.',
      ['/membership/setup', '/membership/setup/[kind]'], FE+'membership/membership-setup.tsx',
      MOD+'Membership/MembershipModule.cs', MOD+'Membership/Domain/MembershipEntities.cs',
      ['Member Type', 'Member Category', 'Member Subscription Band', 'Member Rate Type', 'Member Rate', 'Member Status', 'Member Application Status', 'Membership products and portal eligibility settings'],
      ['The setup route checks that the kind is in setupKinds.', 'MembershipSetup loads /setup/{kind}.',
       'Authorised staff add or patch lookup names and descriptions.', 'Product settings specify term, rate, currency and portal application options.',
       'The server stores these values and validates application requests against the product.'],
      'The seven setup-kind keys are identifiers used by routes and stored records; their labels are the words shown to people. A product connects business settings such as term and rate to an application. Changing a display label differs from changing a key. Existing prices and classifications need a considered migration or business update, not a global text replacement.',
      (FE+'membership/membership-setup.tsx', 'Lookup values', 'Setup values', 'Replace the Panel title only. Open one setup list and confirm its stored names still load. Do not change the kind passed to /setup.'),
      'Lookup edits need configuration permission. A product rate is a stored quote or arrangement; no card processor is connected by changing it. Product portal options determine which applications are offered.', [9,10,15,21])

guide(12, 'Nexora events and bookings',
      'Understand event offerings, sessions, capacity, registrations and attendance.',
      ['/events/events', '/events/events/[id]'], FE+'events/events-workspace.tsx',
      MOD+'Events/EventsModule.cs', MOD+'Events/Domain/EventEntities.cs',
      ['Event creation', 'Venue and dates', 'Sessions', 'Contact bookings and waitlists', 'Attendance transfer promotion and cancellation', 'Activity history'],
      ['The events route sets offeringKind to event.', 'The workspace lists /api/v1/events/offerings.',
       'A create form supplies dates, venue and capacity.', 'Selecting a Contact creates an enrollment or waitlist entry.',
       'The backend validates dates, capacity and permitted booking transitions.'],
      'Events, courses and exams share Offering and Enrollment structures. The kind selects which behaviour and wording apply. Dates entered by the browser are converted to UTC for requests. An enrollment is the participant\'s booking; an Offering is the thing they attend. Server capacity checks remain necessary when several people book at once.',
      (FE+'events/events-workspace.tsx', 'Venue or online location', 'Venue or meeting link', 'Replace only this form label. Open the event create form and verify dates and capacity remain present. Because the component is shared, also inspect a course form.'),
      'Stored registrations are not ticket-payment integration. Cancellation and transfer alter real attendance records; only use approved test data for business actions.', [13,14,15])

guide(13, 'Nexora courses and training',
      'Understand course applications, training approval and attendance through the shared events module.',
      ['/events/courses', '/events/courses/[id]'], FE+'events/events-workspace.tsx',
      MOD+'Events/EventsModule.cs', MOD+'Events/Domain/EventEntities.cs',
      ['Course offerings', 'Training applications', 'Approval rejection and cancellation', 'Course sessions', 'Attendance and transfers'],
      ['The courses route passes course as the offering kind.', 'The shared workspace loads course offerings.',
       'A Contact applies for training through the enrollment endpoint.', 'Course applications can be approved or rejected by authorised staff.',
       'Attendance and history are recorded against the enrollment.'],
      'The course route is a small wrapper. Most of the screen lives in EventsWorkspace and most rules live in EventsModule. Course-specific text and transitions are selected by offering.kind. Before changing a shared conditional, inspect how event and exam branches use it so a training improvement does not alter another offering type.',
      (FE+'events/events-workspace.tsx', 'Apply for training', 'Submit training application', 'Replace this course-specific button wording. Open a course detail and verify the event branch still says Book place. Leave the request endpoint and enrollment fields unchanged.'),
      'The source provides training applications and attendance. A separate learning-content player, third-party learning system or certification delivery must not be inferred from a course record.', [12,14])

guide(14, 'Nexora examinations and results',
      'Understand examination offerings, attendance, score publication and pass or fail outcomes.',
      ['/events/exams', '/events/exams/[id]'], FE+'events/events-workspace.tsx',
      MOD+'Events/EventsModule.cs', MOD+'Events/Domain/EventEntities.cs',
      ['Exam offering and passing score', 'Candidate enrollments', 'Attendance prerequisite', 'Result score and outcome', 'Exam-results permission'],
      ['The exam route selects exam as the offering kind.', 'An enrollment links the candidate Contact to the exam.',
       'Attendance is recorded before a result can be published.', 'An authorised results request sends score and expected version.',
       'Server validation stores the result and calculates its outcome.'],
      'The results permission is separate from general event management. The screen only shows the score form for an attended exam enrollment without an existing result. Those browser conditions help usability, while the C# endpoint checks the rules again. Changing the displayed word Score does not change the passing-score calculation.',
      (FE+'events/events-workspace.tsx', 'Exam score', 'Candidate score', 'Replace only the result field label. Inspect the exam result form using an authorised test enrollment. Do not publish or alter a real result while practising.'),
      'The current module stores exam outcomes; it does not establish a live proctoring service, external examination-board integration or automatic certificate distribution.', [12,13,20])

guide(15, 'Nexora sales and finance',
      'Understand catalogue products, orders, invoices, receipts, allocations and reconciliation.',
      ['/finance/catalogue', '/finance/orders', '/finance/invoices', '/finance/receipts'],
      FE+'finance/finance-workspace.tsx', MOD+'Finance/FinanceModule.cs', MOD+'Finance/Domain/FinanceEntities.cs',
      ['Catalogue and currencies', 'Order lines and posting', 'Invoices and credits', 'Receipts and refunds', 'Allocations and reversals', 'Reconciliation export and history'],
      ['A catalogue product supplies price, currency and VAT basis points.', 'An order holds customer and line items.',
       'Posting produces a stored invoice using validated totals.', 'An external receipt is recorded separately.',
       'Allocations link available receipt money to invoice balances, with reversals and reconciliation history.'],
      'The frontend displays actions but the backend calculates financial balances and validates amounts. A posted invoice is a stored accounting record. Recording a receipt explicitly does not initiate a charge. Credits reduce invoices; refunds reduce available receipts; allocations connect the two. Keep those meanings separate when tracing request fields.',
      (FE+'finance/finance-workspace.tsx', 'export function', None, 'Read-only exercise: find the workspace export and trace the receipts create request to FinanceModule. Find the server history message saying no charge was initiated. Explain why a receipt form is different from taking a card payment.'),
      'No live payment processor or statutory accounting acceptance is established by these records. Calculation, posting and currency changes require targeted financial tests; beginners should start with text-only exercises.', [9,11,16,19])

guide(16, 'Nexora engagement and fundraising',
      'Understand communities, campaigns, consented audiences, funds and donation records.',
      ['/engagement/communities', '/engagement/campaigns', '/engagement/funds'],
      FE+'operations/operations-workspace.tsx', MOD+'Engagement/EngagementModule.cs', MOD+'Engagement/Domain/EngagementEntities.cs',
      ['Communities and members', 'Campaign drafts and prepared audiences', 'Consent checks', 'Funds', 'Donation recording and activity'],
      ['The engagement route selects the OperationsWorkspace module and section.',
       'The shared workspace loads the chosen engagement lists.', 'Forms are generated from the engagement action definitions.',
       'Preparing a campaign asks the backend to build its consented audience.', 'The server persists community, campaign, fund or donation records.'],
      'OperationsWorkspace is shared with Work. Its engagement array describes form labels, paths and fields. A prepared audience is a set of eligible records, not evidence that emails were sent. Consent fields in CRM feed audience selection. Fundraising stores donation information without proving a banking transaction.',
      (FE+'operations/operations-workspace.tsx', 'Prepare consented audience', 'Prepare eligible audience', 'Change this campaign button wording only. Check the campaign detail and a Work task screen; the Work actions should be unaffected.'),
      'Campaign audience preparation does not connect an email delivery provider. A donation record does not charge a donor. Consent and suppression checks remain server-side business rules.', [5,15,17])

guide(17, 'Nexora tasks and notifications',
      'Understand work items, assignees, teams, completion and staff notifications.',
      ['/work/items', '/work/notifications'], FE+'operations/operations-workspace.tsx',
      MOD+'Work/WorkModule.cs', MOD+'Work/Domain/WorkEntities.cs',
      ['Task creation and priority', 'User and team assignment', 'Due dates', 'Complete task', 'Notification lists and read state', 'Task export'],
      ['The Work route chooses items or notifications.', 'OperationsWorkspace loads records through moduleApi.',
       'Task creation sends the fields described in the work actions array.',
       'Completing sends a patch with its current version and updated status.', 'The server persists task or notification changes for the current tenant.'],
      'A task is a stored item with title, priority, status and optional due date and assignee. Staff notifications are separate from the Member Portal inbox. The shared operation form converts number and date fields before it sends JSON. A date label change is safe; changing conversion logic can change stored times.',
      (FE+'operations/operations-workspace.tsx', 'Show assignees and teams', 'Find staff and teams', 'Replace this Work-only button label. Open task creation and verify the directory still loads. Inspect Engagement because it shares the component.'),
      'Marking an item done or a notification read changes persistent records. A listed notification is not proof of a push or email provider delivery.', [16,18,20,25])

guide(18, 'Nexora imports exports and migration',
      'Understand data previews, commit steps, export paths and the difference between importing data and database migrations.',
      ['/work/imports', '/relationships/contacts', '/insights'], FE+'operations/operations-workspace.tsx',
      MOD+'Work/WorkModule.cs', MOD+'Work/Domain/WorkEntities.cs',
      ['Task JSON import preview and commit', 'CRM CSV import and export', 'Report and finance exports', 'Migration workbook field mapping', 'EF database migrations', 'Error review and reconciliation'],
      ['A data import first parses rows into a request.', 'Preview validates rows and records errors without the final business write.',
       'An authorised commit applies a validated import using its version.', 'Exports return the permitted matching records.',
       'Database migrations separately change table structure, not business import row mapping.'],
      'A workbook field inventory is a mapping aid, not a universal working importer. The Work screen specifically accepts up to 500 task rows as JSON. CRM has its own import endpoints and schema. EF migration files change the SQL table design and are normally generated from model changes. Never treat a successful preview as proof a full migration reconciled correctly.',
      (FE+'operations/operations-workspace.tsx', 'Validate and preview', 'Check rows and preview', 'Replace only the task import button label. Open /work/imports in create mode and inspect the form. Do not click Commit validated import or run a database migration for this text exercise.'),
      'Preview, commit and reconciliation are distinct evidence. A complete customer migration still needs source-specific mapping, counts, error review and acceptance. A schema change needs a reviewed migration for the appropriate context.', [5,17,19,30])

guide(19, 'Nexora insights and reporting',
      'Understand dataset selection, query filters, matching totals, paging and CSV export.',
      ['/insights'], FE+'insights/insights-workspace.tsx', 'backend/src/Nexora.Api/ReportingEndpoints.cs',
      BB+'Reporting/ReportDataset.cs',
      ['Available datasets', 'Search exact status and created-date filters', 'Server query paging', 'Sorting', 'CSV export', 'Tenant and role restrictions'],
      ['Insights asks the API for datasets permitted to the signed-in user.',
       'The user selects a dataset and enters filters.', 'A POST query request applies those filters on the server.',
       'The response supplies total and current page items.', 'Export requests the permitted matching records with the same intended filter scope.'],
      'A dataset describes how a module exposes reportable records. The browser has filter state and query state; pressing Run report commits the chosen filter values to a request. Previous and Next change skip while keeping the query. Total is the full matching count, while items is one page. Permission checks protect both queries and exports.',
      (FE+'insights/insights-workspace.tsx', 'Explore records available to your tenant and role. Counts reflect the time this page loaded.', 'Explore the records your organisation and role can access. Counts show this page load.', 'Replace only this paragraph text. Run the same report before and after the change and check the total and results do not change because of the wording.'),
      'An Insights count measures stored records available to that role. It does not establish external analytics integration or broader commercial KPIs. Export rights are separate from read rights.', [4,7,15,18])

guide(20, 'Nexora administration and access',
      'Understand staff users, security groups, permission assignment and tenant protection.',
      ['/settings/users', '/settings/security-groups'], FE+'administration/access-workspace.tsx',
      MOD+'Identity/AdministrationEndpoints.cs', MOD+'Identity/Domain/NexoraRole.cs',
      ['Staff user lists and forms', 'Security groups', 'Permission matrix', 'Protected system administrator', 'User activation and passwords', 'Concurrency stamps and current access'],
      ['The settings route requires the staff session and identity.read.', 'AccessWorkspace loads users, groups and available permissions.',
       'An authorised form sends user or group changes with its concurrency stamp.',
       'AdministrationEndpoints validate protected accounts and group permissions.', 'Fresh claims and active-tenant authorisation determine later request access.'],
      'Security groups are backed by Identity roles with permissions. A user can belong to several groups. Human-friendly permission labels do not change the underlying strings such as crm.read. Server rules protect system accounts and tenant scope. A concurrency stamp is a version marker which prevents overwriting a newer change.',
      (FE+'administration/access-workspace.tsx', 'Files and evidence', 'Documents and evidence', 'Replace this human-readable permission label in permissionNames only. Inspect the permission matrix. Leave the files key and crm.files permission string unchanged.'),
      'Identity management changes can lock people out or grant access. Start with label changes, and keep protected-account safeguards. Production identity and verification remain separate unfinished integration work.', [1,3,24,30])

guide(21, 'Nexora automation controllers',
      'Understand the Attributes, Rules and Design controllers for configuring entity forms.',
      ['/automations', '/automations/type/[automation]', '/automations/[module]/attributes-controller', '/automations/[module]/rules-controller', '/automations/[module]/design-controller'],
      FE+'shared/automation-controller.tsx', BB+'Persistence/AutomationConfiguration.cs', BB+'Persistence/EntityAttributes.cs',
      ['Choose controller then entity', 'Typed custom attributes', 'Formula and conditional rules', 'Section layouts and visibility', 'Metadata and validation', 'Applying configured sections on record forms'],
      ['The automation menu chooses controller type and module.', 'Metadata provides entities and supported fields.',
       'The controller edits attribute, rule or section definitions.', 'Shared backend endpoints validate and store configuration.',
       'Record forms render configured fields, and server save rules apply configured calculations.'],
      'A controller changes configuration for records, while a workflow coordinates a sequence of actions. Attributes define extra data, Rules calculate or condition values, and Design arranges record sections. Several modules reuse the same controller UI and BuildingBlocks storage. Changes to shared metadata can affect every registered module.',
      (APP+'(platform)/automations/page.tsx', 'Choose an automation type first, then select the entities it should apply to.', 'Choose a controller, then choose the records it should apply to.', 'Replace only the introduction sentence. Open the controller menu and follow one entity into each controller without saving configuration.'),
      'Configuration edits can affect many forms and future saves. Formula syntax and entity metadata must remain valid. Styling defaults are separate from per-entity configuration.', [8,11,22])

guide(22, 'Nexora workflow designer and runtime',
      'Understand workflow drafts, nodes, validation, publishing, activation, runs and operational gates.',
      ['/workflow-designer', '/workflow-designer/content-preview/[pageId]/[revisionId]'],
      FE+'workflows/workflow-designer-workspace.tsx', MOD+'Workflow/WorkflowModule.cs', MOD+'Workflow/WorkflowModels.cs',
      ['Catalogue and inactive draft templates', 'Canvas nodes conditions queries and variables', 'Triggers field changes and schedules', 'Validation version publishing and activation', 'Approvals exceptions and run history', 'Connectors delivery reconciliation import export and AI draft'],
      ['The designer loads metadata, definitions and permissions.', 'A draft graph contains typed nodes and connections.',
       'Validation checks graph structure, conditions and action inputs.', 'Publishing creates a version; activation separately enables its triggers.',
       'The processor records runs, steps, approvals, exceptions and delivery outcomes.'],
      'A graph is a set of steps and links. A draft is editable; a published version is the reference used by runs. Activation controls whether eligible triggers start it. Saving a diagram does not execute it. WorkflowProcessor and registered module actions perform server work, with gates and persisted state. Preview, test, publish and execute have different consequences.',
      (FE+'workflows/workflow-editor.tsx', 'Drag nodes to arrange. Set connections in node settings.', 'Drag steps to arrange them. Choose links in step settings.', 'Replace only the canvas help sentence. Open a draft and inspect the canvas. Do not publish, activate, install templates or execute it during this wording exercise.'),
      'A queued email or connector delivery is not confirmed external delivery. Provider availability and reconciliation are separate. Template installation creates records, and execution can alter operational data. Trace permissions and action capabilities before enabling a workflow.', [10,15,17,21,27])

guide(23, 'Nexora AI assistant connection',
      'Understand the NexOrchestr AI page, its configured iframe and CRM context endpoint.',
      ['/assistant'], APP+'(platform)/assistant/page.tsx', MOD+'Crm/CrmModule.cs',
      BB+'Security/CrmContextContracts.cs',
      ['Signed-in assistant page', 'Configured external application URL', 'Allowed URL schemes', 'Iframe sandbox and referrer policy', 'CRM context boundary', 'Unavailable state'],
      ['The route checks the staff session.', 'It reads NEXORCHESTR_APP_URL on the server.',
       'A valid allowed URL becomes an iframe source.', 'Without valid configuration the page shows an unavailable message.',
       'CRM context is exposed through an authorised endpoint, rather than unrestricted database access.'],
      'The repository page is a connection shell. The AI application itself is not implemented by this iframe file. URL validation rejects embedded credentials and restricts schemes. Its sandbox specifies browser capabilities. The CRM context endpoint is a separate API boundary; embedding a page does not automatically authorise it to access Nexora records.',
      (APP+'(platform)/assistant/page.tsx', 'The AI assistant is not connected yet. Contact your administrator to enable it.', 'The AI assistant is awaiting connection. Ask your administrator to configure it.', 'Replace only the unavailable-state sentence. Inspect /assistant without adding an external URL. This wording change should not enable any provider.'),
      'A complete AI service, model configuration and provider behaviour cannot be inferred from this route. The configured external URL and its service availability need separate verification.', [3,5,20,22])

guide(24, 'Nexora member portal login and onboarding',
      'Understand member sign-in, optional self-registration and organisation access requests.',
      ['/portal', '/portal/signup'], FE+'portal/member-portal.tsx', MOD+'Identity/PortalOnboardingEndpoints.cs',
      MOD+'Identity/Domain/PortalAccess.cs',
      ['Member login and logout', 'Published sign-in branding', 'Optional self-registration', 'Association host mapping', 'Contact and personal portal access creation', 'Company access requests and staff review'],
      ['PortalLayout preserves the member shell while the route changes.',
       'The page loads public appearance and onboarding availability for its mapped association host.',
       'Login or enabled registration uses the portal API helper and portal CSRF endpoint.',
       'The Identity module creates or checks permitted member access.',
       'Company access is requested and reviewed separately from personal membership.'],
      'Staff and member sessions have separate purposes and access checks. Self-registration is governed by published onboarding settings and host mapping. A new personal profile does not immediately grant membership or company authority. Organisation requests can be reviewed by staff, who can create the relevant relationship and explicit grant. Failed or late requests are guarded so they do not restore an expired session.',
      (FE+'portal/member-portal.tsx', 'Welcome to your member portal', 'Welcome to your community portal', 'Change this fallback sign-in heading only. Refresh /portal while signed out. Use CRM settings for stored branding; this source change affects the hardcoded heading.'),
      'Current local portal identity is Development-only. Production identity, email verification and password recovery remain future scope. New registration requires enabled published settings and a correctly mapped association domain.', [1,20,25,26])

guide(25, 'Nexora member portal journeys',
      'Understand the signed-in dashboard, profile, applications, memberships, organisations and inbox.',
      ['/portal', '/portal/profile', '/portal/memberships', '/portal/applications', '/portal/applications/[id]', '/portal/organisations', '/portal/notifications'],
      FE+'portal/member-portal.tsx', MOD+'Identity/PortalEndpoints.cs', MOD+'Identity/Domain/PortalOrganisationGrant.cs',
      ['Persistent member navigation', 'Dashboard totals and quick links', 'Structured profile and correspondence address', 'Application drafts submission evidence and receipts', 'Company grants and applications', 'Member notifications and read state'],
      ['The shared portal layout chooses the route section.', 'Profile and published presentation establish the signed-in view.',
       'The selected component loads only its relevant records.', 'Personal or company requests go through /api/v1/portal with access checks.',
       'Saving, submitting or marking read updates the stored record and refreshes the relevant view.'],
      'Portal routes often return null because the shared layout renders MemberPortal; editing the small route wrapper will not change the visible form. Organisation requests use an explicit accountId and grant check. A profile version prevents overwriting newer changes. sessionRevision and the expiry event keep delayed requests from restoring signed-out data.',
      (FE+'portal/member-portal.tsx', 'Save profile', 'Save my profile', 'Replace only the profile button text. Open /portal/profile and inspect it. Navigate to applications and Back, and check the shared shell remains consistent. Do not submit a real profile change for this exercise.'),
      'Member visibility is limited to their Contact and explicit company grants. Application receipts confirm stored submission, not paid membership. Public website visitors are not automatically created as Contacts.', [9,10,24,26,29])

guide(26, 'Nexora portal management',
      'Understand CRM control of member portal branding, themes, menus, pages and onboarding.',
      ['/portal-management'], FE+'content/portal-editor.tsx', MOD+'Content/PortalPresentation.cs',
      MOD+'Content/ContentEntities.cs',
      ['Draft save preview and publish', 'Portal name tagline and welcome', 'Neon and classic appearance controls', 'Motion settings and reduced motion', 'Menu labels order and visibility', 'Dashboard sections and custom pages', 'Self-registration and organisation-request settings'],
      ['PortalEditor loads the draft and published configuration.', 'Staff change draft fields and preview the appearance.',
       'Saving persists a draft with its expected version.', 'Publishing validates and replaces the published configuration.',
       'Member and public sign-in views read the published settings for the association.'],
      'Draft and published settings are separate. The preview shows presentation while member data stays live in the real portal. Labels and page text are stored data, so administrators can change them without coding. presentation.ts supplies fallback defaults; changing a fallback does not overwrite an existing published configuration. Dashboard stays available even if other menu links are hidden.',
      (FE+'content/portal-editor.tsx', 'Branding and welcome', 'Portal branding and welcome', 'Replace this editor section heading only. Open Portal management and inspect Settings. For a no-code exercise, preview a changed draft welcome message, then discard it rather than publishing to members.'),
      'Editing needs content.manage and publication needs content.publish. Custom member pages are plain text, not arbitrary executable HTML. Published onboarding changes can allow new registrations.', [24,25,27])

guide(27, 'Nexora website editor and publishing',
      'Understand CRM-managed pages, design controls, media, revision history and publication.',
      ['/website'], FE+'content/content-workspace.tsx', MOD+'Content/ContentModule.cs', MOD+'Content/ContentEntities.cs',
      ['Website creation and page list', 'Block editor and studio template', 'Layout colours typography and appearance', 'Per-block motion and hover controls', 'Media upload and scanning', 'Draft save preview publish schedules redirects and history'],
      ['The workspace loads the site, page drafts, design settings and media.',
       'BlockEditor collects structured blocks and their layout and motion.', 'Saving creates or updates a revision using the expected version.',
       'An authorised publish or schedule action selects what visitors may see.',
       'Public routes render published revisions and published design settings.'],
      'A page is a stored address and publication pointer; a revision holds title, description and blocks. A draft can change without changing the visitor page. WebsiteDesign and WebsiteMotion describe manageable design fields, not just hardcoded CSS. The editor and public renderer share types so the stored block data can be rendered consistently.',
      (FE+'content/content-workspace.tsx', 'Create your association website', 'Set up your association website', 'Replace this empty-site form heading only. Inspect it only on a local site without saved content. For an existing website, use the block editor to preview draft text and discard it without publishing.'),
      'Changing default template data does not overwrite saved pages. Clean media, permissions, domain configuration and publish state affect visitor availability. A page labelled Contact does not establish delivery of real enquiries.', [26,28,29])

guide(28, 'Nexora public website',
      'Understand visitor routes, published content rendering, navigation, media and sitemap.',
      ['/site/[siteId]', '/site/[siteId]/[slug]', '/site/[siteId]/media/[id]', '/site/[siteId]/sitemap.xml'],
      FE+'content/site-renderer.tsx', MOD+'Content/ContentPublic.cs', MOD+'Content/ContentEntities.cs',
      ['Published home and pages', 'Site navigation and portal link', 'SEO metadata canonical links and sitemap', 'Redirects', 'Public clean media', 'Block rendering theme and motion'],
      ['A public route receives siteId and optional slug.', 'The route requests published page data from the API.',
       'ContentPublic checks the site, publication and media availability.',
       'SiteRenderer draws blocks with published layout and design.', 'Navigation, portal links and sitemap use published page addresses.'],
      'The route is the entry point, but the page text lives in saved revisions. SiteRenderer switches on each block type, so a new block type needs matching types, editor controls, validation and rendering. Motion fields affect how content enters and responds to hover. Respect reduced-motion settings. A media URL is checked by the backend before data is returned.',
      (FE+'content/site-renderer.tsx', 'export function', None, 'Read-only exercise: locate SiteRenderer, find how it chooses block types, then follow a hero block back to the editor types. Use CRM to preview changed text; do not replace a saved page with a hardcoded route.'),
      'The public demo is not evidence of live event booking, payments or enquiry delivery. Only published content is visible to anonymous visitors. Registration and member actions belong to the portal access boundary.', [24,27,29])

guide(29, 'Nexora documents evidence and file safety',
      'Understand CRM files, application evidence, CMS media and quarantined uploads.',
      ['/relationships/contacts', '/membership/applications/[id]', '/portal/applications', '/website'],
      FE+'shared/application-evidence.tsx', BB+'Documents/DocumentSafety.cs', MOD+'Membership/Domain/ApplicationEvidence.cs',
      ['Upload metadata and content', 'Quarantine and scan states', 'ClamAV scanner configuration', 'Clean-file download', 'Tenant and record access', 'Application evidence review and CMS public media'],
      ['A form sends a file and relevant record or draft id.', 'The endpoint validates size, type and access.',
       'The shared safety service stores quarantine and scan state.', 'A scan result controls whether download or publication is allowed.',
       'Record, application and public media endpoints apply their own access rules.'],
      'File metadata is the name, type, size and scan result; file bytes are the actual content. Stored bytes alone do not make a file safe. Missing scanner access deliberately retains quarantine. CRM, portal evidence and CMS media use shared safety concepts but different endpoints and permissions. On Windows, Prepare-LocalDebug.ps1 calls Prepare-LocalDocumentScanner.ps1 before API preparation. The helper can download its configured ClamAV package and signatures, checks the package checksum and starts the local scanner on 127.0.0.1:3310. The Development API configuration points to that address. Inspect its logs under artifacts/local-document-scanner when this preparation fails. Never make the UI treat quarantined as clean to remove an error.',
      (FE+'shared/application-evidence.tsx', 'export function', None, 'Read-only exercise: find the upload, list and download requests. Trace the scan-status rendering to the backend safety code. Explain why a successful upload can still be unavailable for download.'),
      'No live scanner was exercised for this documentation task. A controlled clean-scan test is different from an operational ClamAV deployment. Leave access and quarantine checks in place.', [5,10,25,27,28,30])

guide(30, 'Nexora backend database and maintenance',
      'Understand the shared API, module boundaries, SQL contexts, migrations, health checks and Git maintenance.',
      ['/health', '/health/ready', '/api/v1/platform'], 'scripts/Prepare-LocalDebug.ps1',
      'backend/src/Nexora.Api/Program.cs', BB+'Persistence/TenantDbContext.cs',
      ['Nine modules and shared BuildingBlocks', 'Nine database contexts', 'Identity CRM Membership Events Finance Engagement Work and Content schemas', 'Workflow context and schema', 'Per-context migration histories', 'Readiness cancellation logging testing and release runbook', 'Local Git and documentation updates'],
      ['Program registers module services and their database contexts.', 'Middleware applies errors, CORS, limits, authentication and authorisation.',
       'Each module maps endpoints and tenant-scoped persistence.', 'Development startup and preparation manage the configured local database.',
       'Operational checks report health and readiness; reviewed release steps govern deployment.'],
      'A DbContext is a C# gateway to a group of tables. Different modules can use the same SQL database with separate schemas and migration histories. Identity plus eight business and Workflow contexts totals nine contexts in current source; read the generated context inventory for the exact names. Never apply one context\'s migration to another or drop a database to repair a missing table.',
      ('backend/src/Nexora.Api/Program.cs', 'AddNexora', None, 'Read-only exercise: list the registered modules, then find each DbContext. Compare the generated database inventory with the health/readiness checks. Follow the Git practice instructions using only your own documentation or text change.'),
      'A successful build, a health response and production approval are different results. This manual records source coverage and documentation checks; it does not claim a full platform regression, live provider delivery, backups or production acceptance.', [2,18,20,22,29])

# Prefixes assign every production source file to at least one teaching chapter.
# Multi-topic files can belong to several chapters; the master stores their code once.
PREFIX_OWNERS = [
    (APP+'(auth)/login/', [1]), (APP+'(platform)/dashboard/', [4]),
    (APP+'(platform)/relationships/', [5,6]), (APP+'(platform)/membership/setup/', [11]),
    (APP+'(platform)/membership/', [9,10]), (APP+'(platform)/events/', [12,13,14]),
    (APP+'(platform)/finance/', [15]), (APP+'(platform)/engagement/', [16]),
    (APP+'(platform)/work/', [17,18]), (APP+'(platform)/insights/', [19]),
    (APP+'(platform)/settings/', [20]), (APP+'(platform)/automations/', [21]),
    (APP+'(platform)/assistant/', [23]), (APP+'(platform)/portal-management/', [26]),
    (APP+'(platform)/website/', [27]), (APP+'workflow-designer/', [22]),
    (APP+'portal/', [24,25]), (APP+'site/', [28]),
    (WEB+'components/app-shell/', [3]), (WEB+'components/', [3]), (WEB+'lib/', [1,2]),
    (FE+'crm/', [5,6,8]), (FE+'membership/', [9,10,11]), (FE+'events/', [12,13,14]),
    (FE+'finance/', [15]), (FE+'operations/', [16,17,18]), (FE+'insights/', [4,19]),
    (FE+'administration/', [20]), (FE+'workflows/', [22]), (FE+'portal/', [24,25,26]),
    (FE+'content/', [26,27,28]), (FE+'shared/application-evidence', [10,29]),
    (FE+'shared/automation', [21]), (FE+'shared/attribute', [8,21]),
    (FE+'shared/record-attribute', [8,21]), (FE+'shared/configured', [8,21]),
    (FE+'shared/profile', [8]), (FE+'shared/', [7,21]),
    (MOD+'Identity/Portal', [24,25]), (MOD+'Identity/Administration', [20]),
    (MOD+'Identity/', [1,20,24,25]), (MOD+'Crm/', [5,6,7,8,18]),
    (MOD+'Membership/', [9,10,11,25,29]), (MOD+'Events/', [12,13,14]),
    (MOD+'Finance/', [15]), (MOD+'Engagement/', [16]), (MOD+'Work/', [17,18]),
    (MOD+'Workflow/', [22]), (MOD+'Content/', [26,27,28]),
    (BB+'Documents/', [29]), (BB+'Reporting/', [4,19]),
    (BB+'Persistence/Automation', [21]), (BB+'Persistence/Entity', [8,21]),
    (BB+'Persistence/Workflow', [22]), (BB, [30]),
    ('backend/src/Nexora.Api/Reporting', [4,19]), ('backend/src/Nexora.Api/', [30]),
    ('scripts/Prepare-LocalDocumentScanner.ps1', [2,29,30]),
    (APP, [3]), ('scripts/', [2,18,30]), ('apps/web/', [2]), ('backend/', [30]),
]

def owners(path):
    for prefix, numbers in PREFIX_OWNERS:
        if path.startswith(prefix):
            return numbers
    return [30]
