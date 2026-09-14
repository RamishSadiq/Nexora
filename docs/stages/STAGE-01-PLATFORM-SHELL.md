# Stage 1: Platform shell

Status: Complete
Completed: 2026-09-11

## Delivered

- Next.js 16 App Router frontend using React 19, TypeScript, Tailwind CSS, typed routes, linting, and production builds
- Nexora visual tokens, brand mark, accessible button primitive, responsive login experience, and reduced-motion support
- Preview login interaction that clearly identifies itself as non-production authentication
- Authenticated application shell with responsive sidebar, role-focused workspace navigation, global search affordance, quick-create affordance, help, notifications, and user context
- Dashboard foundation with KPI cards, relationship-growth visual, and recent activity
- Stable placeholder routes for each future workspace so navigation never leads to an unexplained 404
- .NET 10 solution containing API, worker, shared building blocks, API integration tests, and architecture tests
- JSON health endpoint, platform descriptor endpoint, OpenAPI in development, problem-details middleware, HTTPS redirection, and constrained development CORS policy
- SQL Server Docker Compose service with externally supplied development password
- Pinned .NET SDK and GitHub Actions CI for frontend and backend verification

## Intentional limitations

- Login is a labelled preview interaction; identity, secure cookies, tenants, users, roles, and permissions are Stage 2.
- Dashboard values are presentation fixtures that establish component and layout direction.
- Workspace pages are stable boundaries, not implemented business modules.
- Database dependencies are defined but no business schema is created before the tenancy model.

## Verification evidence

- `npm run lint` — passed
- `npm run build` — passed with all application routes generated
- `dotnet test backend/Nexora.slnx --configuration Release` — 3/3 tests passed
- Browser QA — login rendered with named controls, preview sign-in reached `/dashboard`, all dashboard landmarks and navigation were available, and a fresh run produced no browser warnings or errors
- Workspace navigation — Relationships route rendered successfully

## Stage 2 entry conditions

- Confirm whether workforce authentication will use Microsoft Entra ID in the first deployment.
- Identity implementation must replace the preview submit handler rather than extending it.
- Tenant isolation tests must be added before any tenant-owned business data is introduced.
