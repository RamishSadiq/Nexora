# Stage 19 — CRM-managed member portal

CRM **Portal** (`/portal-management`) now manages the signed-in member experience without changing the existing membership, application, receipt, organisation, notification or profile workflows.

## Configuration

- Portal name, tagline, dashboard welcome text and accessible dark brand colour.
- Built-in and custom menu labels, order and visibility; Dashboard remains visible.
- Dashboard section visibility and order: summary cards, current membership, recent applications and quick links.
- Up to 30 custom information pages with a stable address, title and multiline plain text. Page addresses cannot replace built-in workflows. Removing a page and publishing withdraws it.
- Save draft, preview, publish and restore the currently published settings into the editor. Publishing applies the saved draft, not unsaved form changes. Members receive published changes on their next page load.

The existing portal is the default when no configuration has been published. Anonymous sign-in retains its neutral branding because no authenticated tenant is selected yet. Custom pages require active member access; menu hiding is presentation, not an authorization rule. Hidden menu destinations can still be opened directly. HTML/scripts are displayed as text, never executed. This release does not provide arbitrary forms, custom workflow code, media blocks or historical revision rollback.

## Implementation and boundaries

The Content module stores one versioned `PortalConfigurations` row per tenant, with separate draft/published JSON and publication actor/time. The `PortalPresentation` migration adds a table without rewriting existing website or member records. Website and portal content are independent.

CRM endpoints reuse `content.read`, `content.manage`, `content.publish`, CSRF validation and optimistic concurrency. Member configuration is delivered through `/api/v1/portal/configuration`, behind existing session, tenant, Contact and active-grant checks, with `no-store`. A BuildingBlocks contract connects Identity and Content without a direct module dependency.

## Verification

- Three configuration API tests cover defaults, publication isolation, concurrency, CSRF, revocation, tenant isolation, permission separation and invalid configurations.
- Two architecture tests passed. EF reports no pending model changes.
- Production frontend build and TypeScript/lint checks passed.
- Browser configuration journey covers CRM editing/preview/save/reload/publish, menu/dashboard changes, custom-page deep links, literal script text, mobile overflow, sign-out and page withdrawal.
- The existing three personal/organisation portal journeys run alongside the configuration journey against a disposable SQL Express database using `node scripts/Test-Website.mjs --portal --production`.
- Local CRM editor and default member dashboard were checked in Edge and visually inspected. Local database migration applied successfully. No custom content was published into the user's local portal by these checks.

Existing ASP.NET analyzer AD0001 warnings occurred during backend builds; they did not prevent compilation.

Final integrated result: all four production browser journeys passed (configuration, organisation application, personal approval and rejection) against a clean disposable database. The initial combined run hit the existing eight-logins-per-minute limit; removing a redundant test login resolved it without changing application security. Final build: zero warnings/errors. Test processes and database were cleaned up.
