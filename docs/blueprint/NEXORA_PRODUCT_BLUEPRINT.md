# Nexora Product Blueprint

Status: Approved direction, implementation baseline
Product codename: Nexora
Architecture: Modular SaaS operations platform
Primary stack: TypeScript/Next.js, C#/ASP.NET Core, SQL Server

## 1. Product vision

Nexora is a modern operations platform for membership-led and relationship-led organisations. It combines CRM, membership, events, finance, communications, committees, learning, examinations, fundraising, tasks, querying, and reporting in one coherent product.

The product preserves the strong domain coverage observed in the reference system while replacing its dense navigation, dated visual language, page fragmentation, and legacy interaction patterns with a fast, accessible, role-aware experience.

## 2. Problem statement

Operational teams currently depend on systems that expose rich data but make routine work difficult through crowded menus, inconsistent workflows, oversized forms, and weak cross-module context. Staff need to manage a constituent or organisation as one connected relationship rather than repeatedly navigating disconnected lists.

If this is not solved, users continue to spend excessive time finding records, reconciling information between modules, learning system-specific conventions, and correcting avoidable errors.

## 3. Product principles

1. **Relationship first** — Account and Contact are the shared foundation for every operational module.
2. **Progressive disclosure** — Show the information needed for the current task and reveal complexity on demand.
3. **One interaction language** — Lists, forms, timelines, bulk actions, filters, and permissions behave consistently everywhere.
4. **Configurable, not chaotic** — Support custom fields and views without converting the entire domain into an untyped EAV model.
5. **Secure by default** — Tenant isolation, least privilege, auditability, protected sessions, and safe exports are platform concerns.
6. **Accessible by construction** — WCAG 2.2 AA is part of component acceptance, not a later remediation stage.
7. **Modular delivery** — Each module can be delivered independently while remaining part of one coherent platform.

## 4. Goals

- Allow a trained user to find any Contact or Account within five seconds at the 95th percentile.
- Reduce the median clicks required for the top ten operational workflows by at least 40% compared with the reference system.
- Achieve at least a 95% successful completion rate for core create, update, search, and export workflows during usability testing.
- Keep standard authenticated page interactions below 2.5 seconds at the 95th percentile under the agreed production load.
- Give administrators role and permission control over every module, record action, export, and sensitive field.

## 5. Non-goals for the first release

- **Microservices:** operational complexity is not justified before domain boundaries and scale are proven.
- **A no-code application builder:** Nexora will offer controlled custom fields, views, and workflow configuration, not arbitrary application generation.
- **Every legacy field on day one:** fields will be migrated according to workflow and reporting value.
- **A native mobile application:** the web application will be responsive; native clients remain a future option.
- **A general accounting ledger:** Finance covers receivables, payments, credits, refunds, exports, and reconciliation, with integrations to accounting systems.

## 6. Target users

- Front-line CRM and membership staff
- Event, training, examination, and committee administrators
- Finance and fundraising teams
- Marketing and communications users
- Managers and report consumers
- System administrators and tenant owners
- External members or contacts using a future self-service portal

## 7. Information architecture

The sidebar contains a small set of role-aware workspaces rather than every entity. Global search, recently visited records, favourites, saved views, and a command palette provide faster access to secondary functions.

### Primary workspaces

1. Home
2. Relationships
   - Contacts
   - Accounts
   - Relationship explorer
3. Membership
4. Events and learning
5. Sales and finance
6. Engagement
   - Marketing
   - Committees
   - Groups
   - Fundraising
7. Work
   - Tasks
   - Imports and exports
8. Insights
   - Query builder
   - Reports
9. Administration

All reference dictionaries remain available through Administration and context-sensitive links rather than occupying the primary navigation.

## 8. Functional modules

### 8.1 Platform foundation

- Tenants and organisations
- Users, teams, roles, claims, and granular permissions
- Authentication, MFA, SSO, session management, and account recovery
- Audit trail and entity history
- Notes, attachments, tags, and activity timeline
- Notifications and notification preferences
- Custom-field definitions, values, validation, and visibility
- Saved views, column preferences, favourites, and recent items
- Import, export, and background job tracking
- Feature flags and tenant settings

### 8.2 CRM

- Contacts and Accounts
- Contact-to-account and account-to-account relationships
- Addresses and communication methods
- Communication permissions and consent evidence
- Status, lifecycle, ownership, regional assignment, and categorisation
- Duplicates, merging, bulk update, and data quality review
- Directories and controlled profile visibility

### 8.3 Membership

- Members and memberships
- Applications, approvals, rejections, and status history
- Categories, grades, products, rates, packages, and benefits
- Joining, renewal, cancellation, lapse, and reinstatement workflows
- Credentials, licences, certificates, and CPD where applicable

### 8.4 Events, training, and examinations

- Events, programmes, sessions, venues, pricing, and capacity
- Registrations, delegates, waiting lists, transfers, and withdrawals
- Special requirements and attendance
- Training/SAT applications and outcomes
- Exams, sittings, centres, bookings, results, and appeals

### 8.5 Sales and finance

- Products, price books, discounts, and promotion codes
- Orders and order lines
- Invoices, credit notes, receipts, refunds, and allocations
- Payment methods, frequencies, terms, VAT, nominal codes, and cost centres
- Direct debit processing and payment-provider integration
- Accounting export/import and reconciliation

### 8.6 Engagement

- Campaigns and mailing lists
- Communication-provider synchronisation
- Committees, meetings, posts, elections, and attendance
- Groups, meetings, members, relationships, and attendance
- Fundraising campaigns, funds, appeals, donations, regular giving, and Gift Aid

### 8.7 Work and insights

- Tasks, teams, assignees, priorities, due dates, and links to any entity
- Metadata-aware query builder with permission enforcement
- Operational dashboards and scheduled reports
- Export controls, watermarking where required, and audit records

## 9. Core domain model

```text
Tenant
├── Users ── Roles ── Permissions
├── Accounts
│   ├── Contacts
│   ├── Relationships
│   ├── Addresses and Communication Methods
│   ├── Memberships and Applications
│   ├── Orders, Invoices, Payments and Credits
│   └── Activities, Notes, Files and Tasks
├── Events ── Sessions ── Registrations
├── Learning ── Applications and Outcomes
├── Exams ── Sittings ── Bookings ── Results
├── Committees and Groups ── Meetings ── Attendance
├── Campaigns and Funds ── Donations and Regular Giving
└── Audit Events, Jobs, Notifications and Configuration
```

### Data-modelling rules

- Every tenant-owned row carries a `TenantId` and is filtered in the persistence layer.
- Important queryable fields use typed relational columns.
- Repeating concepts such as addresses and communication methods use child entities.
- Custom fields use typed definitions and typed value storage with validation and indexing strategies.
- Financial transactions are immutable after posting; corrections use compensating entries.
- Sensitive changes create append-only audit events containing actor, time, correlation ID, reason, and safe before/after details.
- Cross-module references use stable IDs and published contracts rather than direct access to another module's internal model.

## 10. Technical architecture

### Architecture style

Nexora begins as a modular monolith with independently owned modules, one deployable API, one worker service, and one web application. Modules share infrastructure deliberately but do not share internal domain models.

This provides simple deployment and transactions while leaving clear seams for extracting a service later when scale, ownership, or availability requirements justify it.

### Frontend

- Next.js App Router and React with TypeScript
- Feature-oriented folders with routes kept thin
- Tailwind CSS and token-based theming
- Accessible components based on shadcn/ui and Radix primitives
- TanStack Query for remote state and TanStack Table for data grids
- React Hook Form and Zod for forms and client-side validation
- Generated TypeScript client from the backend OpenAPI document
- Server rendering for the application shell where useful; interactive entity workspaces remain client-driven

### Backend

- ASP.NET Core Web API
- Feature-oriented use cases inside domain modules
- Entity Framework Core with SQL Server
- FluentValidation or equivalent request validation
- OpenAPI contracts and consistent RFC 9457-style problem responses
- Hangfire or Quartz.NET for durable background work
- Serilog and OpenTelemetry for structured logs, traces, and metrics
- Health checks for database, queues, storage, and integrations

### Authentication and session security

- OpenID Connect authorization-code flow with PKCE
- Microsoft Entra ID for workforce SSO when available
- ASP.NET Core Identity for locally managed/external accounts when required
- Backend-for-frontend session pattern using `HttpOnly`, `Secure`, appropriately scoped `SameSite` cookies
- No access or refresh tokens in browser local storage
- MFA/passkey readiness, login throttling, lockout, recovery codes, and security-event auditing

### Data and infrastructure

- SQL Server as the initial transactional database
- Redis only when measured caching, distributed locks, or job coordination requires it
- Azure Blob Storage or an S3-compatible store for attachments and generated exports
- Transactional outbox for reliable integration events
- Docker-based local development
- Infrastructure as code for production environments
- Automated backups, point-in-time recovery, retention policies, and restore tests

## 11. API conventions

```text
GET    /api/v1/crm/contacts
POST   /api/v1/crm/contacts
GET    /api/v1/crm/contacts/{contactId}
PATCH  /api/v1/crm/contacts/{contactId}
GET    /api/v1/crm/contacts/{contactId}/timeline
POST   /api/v1/crm/contacts/{contactId}/notes
GET    /api/v1/crm/contacts/metadata
POST   /api/v1/exports
GET    /api/v1/jobs/{jobId}
```

- Cursor or stable offset pagination depending on workflow needs
- Explicit filtering and sorting syntax with allow-listed fields
- Idempotency keys for payment and other retry-sensitive commands
- Optimistic concurrency for editable records
- Correlation IDs on every request and background job
- Permission checks at both use-case and data-query boundaries

## 12. Repository structure

```text
nexora/
├── apps/
│   └── web/                         # Next.js application
│       ├── src/app/                 # Route groups and layouts
│       ├── src/features/            # Domain-oriented UI features
│       ├── src/components/          # Shared application components
│       ├── src/design-system/       # Tokens and primitives
│       ├── src/lib/                 # API, auth and utilities
│       └── tests/
├── backend/
│   ├── Nexora.sln
│   ├── src/
│   │   ├── Nexora.Api/              # HTTP host
│   │   ├── Nexora.Worker/           # Background processing
│   │   ├── BuildingBlocks/          # Cross-cutting platform capabilities
│   │   └── Modules/
│   │       ├── Crm/
│   │       ├── Membership/
│   │       ├── Events/
│   │       ├── Learning/
│   │       ├── Exams/
│   │       ├── Sales/
│   │       ├── Finance/
│   │       ├── Marketing/
│   │       ├── Committees/
│   │       ├── Groups/
│   │       ├── Fundraising/
│   │       ├── Tasks/
│   │       └── Insights/
│   └── tests/
├── packages/
│   ├── api-client/
│   └── design-tokens/
├── deploy/
├── docs/
└── scripts/
```

Each backend module owns `Domain`, `Application`, `Infrastructure`, `Endpoints`, and `Contracts` folders. A module may be promoted to separate projects only when its size or team ownership justifies the extra project overhead.

## 13. UX blueprint

### Login

- Branded but restrained page with clear hierarchy and no product navigation
- Email/SSO discovery followed by the appropriate authentication method
- Visible password option and Caps Lock warning where passwords are enabled
- Forgot password, support, privacy, and service-status paths
- Clear loading, validation, lockout, expired-link, and provider-failure states
- Responsive layout, full keyboard operation, and screen-reader announcements

### Application shell

- Collapsible workspace sidebar with labels and icons
- Global search/command control in the header
- Contextual create button
- Notifications, help, organisation switcher, and user menu
- Breadcrumbs only where they add hierarchy; avoid duplicating page titles
- Mobile/tablet navigation suitable for urgent lookup and approval work

### Entity list

- Fast search, filter chips, saved views, sort, bulk selection, and export
- Column chooser with remembered personal preferences
- Compact and comfortable density options
- Empty states that explain how to create or import the first record
- Loading skeletons and recoverable inline errors
- URLs preserve filters, views, page, and selected record where appropriate

### Entity workspace

- Summary header with identity, status, ownership, key actions, and alerts
- Overview, Activity, Related, Files, and Audit sections
- Complex information divided into cards and purposeful tabs
- Inline editing for small changes and focused workflows for high-risk changes
- Related modules displayed in context rather than requiring menu navigation

## 14. Security and compliance baseline

- Tenant isolation tests for every tenant-owned query
- Least-privilege role templates plus tenant-defined roles
- Field-level restrictions for financial, identity, and sensitive personal data
- Encryption in transit and managed encryption at rest
- Secrets stored outside source control
- CSRF, XSS, injection, open-redirect, upload, and SSRF protections
- Export permission checks, audit events, expiration, and revocation
- Retention and deletion policies by data category
- Dependency, secret, SAST, and container scanning in CI
- Privacy-impact review before production data migration

## 15. Quality strategy

- Unit tests for domain rules and permission decisions
- Integration tests against a real SQL Server container
- API contract tests and generated-client verification
- Component tests for complex forms and grids
- Playwright tests for login and critical cross-module journeys
- Automated accessibility checks plus manual keyboard/screen-reader review
- Architecture tests preventing forbidden module dependencies
- Performance tests for search, list, export, and high-volume import paths

## 16. Delivery stages

### Stage 0 — Blueprint and engineering decisions

Deliverables:

- Product blueprint
- Architecture decision records
- Repository strategy and staged roadmap

Exit criteria:

- Architecture, scope boundaries, security posture, and initial module order are documented.

### Stage 1 — Platform shell

Deliverables:

- Monorepo structure
- Next.js frontend and ASP.NET Core API
- Design tokens and foundational UI components
- Modern responsive login experience
- Auth boundary and development-session abstraction
- Health endpoint, OpenAPI, structured errors, and test foundations
- Docker-based local dependencies

Exit criteria:

- Web and API build successfully.
- Login and authenticated shell render responsively.
- Automated tests and linting pass.

### Stage 2 — Identity and tenancy

Deliverables:

- Tenant, user, team, role, and permission models
- Secure cookie/OIDC integration boundary
- Administration screens for people and access
- Audit foundation

Exit criteria:

- Tenant isolation and permission tests pass.
- Authentication and recovery error states are covered.

### Stage 3 — CRM foundation

Deliverables:

- Accounts, Contacts, addresses, communication methods, relationships, notes, tags, and files
- List, detail, create, update, timeline, archive, and restore workflows
- Search, filtering, sorting, pagination, saved views, and column preferences
- Controlled custom-field framework

Exit criteria:

- A user can complete the core Contact and Account lifecycle.
- Accessibility and performance thresholds are met on representative data volumes.

### Stage 4 — Membership

- Members, applications, decisions, products, rates, renewals, cancellations, and status history

### Stage 5 — Events, learning, and examinations

- Events, sessions, registrations, transfers, attendance, training applications, exams, bookings, and results

### Stage 6 — Sales and finance

- Catalogue, orders, invoices, credits, receipts, refunds, allocations, exports, and reconciliation

### Stage 7 — Engagement and work

- Marketing, committees, groups, fundraising, tasks, notifications, imports, and exports

### Stage 8 — Insights and production readiness

- Query builder, dashboards, reports, observability, performance hardening, security review, migration tooling, and release runbooks

Every stage is committed separately and pushed only after its exit checks pass.

## 17. Initial user stories

### Staff user

- As a staff user, I want to search across Contacts and Accounts from anywhere so that I can reach the correct record immediately.
- As a staff user, I want a unified activity timeline so that I understand a person's relationship without visiting multiple modules.
- As a staff user, I want saved filters and columns so that recurring work starts in the state I prefer.

### Operational manager

- As a manager, I want role-specific dashboards so that I can identify workload, exceptions, and overdue actions.
- As a manager, I want controlled exports so that teams can use data without bypassing privacy controls.

### Administrator

- As an administrator, I want granular roles and permissions so that users only see data and actions required for their work.
- As an administrator, I want controlled custom fields so that the product fits our organisation without unsupported schema changes.
- As an administrator, I want an audit trail so that I can determine who changed sensitive information and why.

## 18. P0 requirements and acceptance criteria

### Application access

- Given an unauthenticated user visits a protected route, when the route loads, then the user is redirected to login and the intended destination is preserved.
- Given a user has successfully authenticated, when a secure session is established, then no bearer or refresh token is exposed through browser local storage.
- Given a user lacks a required permission, when they request a protected action directly or through the UI, then the action is denied and audited consistently.

### Tenant isolation

- Given two tenants contain records with similar identifiers, when a user queries or changes data, then only records belonging to the active tenant can be accessed.
- Automated integration tests must attempt cross-tenant access for reads, writes, exports, and background jobs.

### Standard entity experience

- Users can search, filter, sort, paginate, select columns, save a view, and export only authorised fields.
- List state can be represented in a shareable URL without exposing confidential filter values.
- Create and edit forms preserve entered values after recoverable validation failures.
- Every destructive action states its effect and requires the appropriate confirmation level.

### Auditability

- Sensitive creates, updates, status transitions, permission changes, imports, and exports capture an audit event.
- Audit events identify the tenant, actor, operation, entity, time, request correlation ID, and reason when required.

## 19. Success metrics

### Leading indicators

- At least 90% task completion for first-round usability testing and 95% before general availability.
- Median Contact lookup below five seconds in usability sessions.
- At least 80% of pilot users create or save a view within their first two weeks when their role uses list workflows.
- Frontend error-free session rate above 99.5% during pilot.

### Lagging indicators

- At least 40% reduction in median completion time across the top ten workflows.
- At least 30% reduction in navigation/search-related support requests within three months.
- At least 80% weekly active usage among licensed pilot staff.
- User satisfaction of at least 4.2/5 for navigation, record comprehension, and perceived speed.

## 20. Risks and mitigations

| Risk | Mitigation |
|---|---|
| Recreating legacy complexity | Migrate workflows by user value, not field-for-field parity. |
| Permission leakage | Central policy engine, query-level tenant filters, field-level tests, and export tests. |
| Over-generalised metadata model | Typed domain models with a bounded custom-field subsystem. |
| Oversized initial release | Stage gates and explicit non-goals; CRM foundation validates the platform first. |
| Reporting blocks delivery | Start with operational views/exports, then add governed reporting in Stage 8. |
| Integration instability | Outbox, idempotency, retries, dead-letter handling, and observable job states. |

## 21. Open decisions

These do not block Stage 1:

- Final public product name and brand identity — product/design owner
- Primary deployment cloud and regional data-residency requirements — stakeholder/engineering
- Whether all customers use Microsoft Entra ID or external accounts are required — stakeholder/security
- Initial migration source and historical retention period — data/stakeholder
- Payments, email, accounting, and reporting providers — stakeholder/engineering
- Formal regulatory scope beyond baseline UK GDPR expectations — legal/stakeholder

## 22. Definition of done for every stage

- Acceptance criteria implemented and demonstrated
- Builds, linting, unit tests, and applicable integration/end-to-end tests pass
- Accessibility checked for new interactive flows
- Security-sensitive changes reviewed against the threat model
- Database migrations are forward-safe and documented
- Operational logging and failure states are present
- Documentation is updated
- A focused Git commit is created and pushed to the project branch
