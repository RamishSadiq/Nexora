# Association website — first delivery slice

On 18 September 2026 the product owner confirmed the sequence: **association website first, member portal next**. This changes implementation priority from the earlier week-by-week proposal, which placed portal P3 before website P4. It does not remove the identity, privacy or deployment acceptance gates. The website belongs to the tenant organisation, not Nexora's marketing brand.

## Implemented scope

- A Content module in the existing .NET modular monolith, with its own `content` SQL schema.
- One website per tenant, up to 100 pages. Staff entry: `/website`.
- Plain-text hero, text, feature and call-to-action blocks, with bounded text and validated links. No raw HTML or scripts.
- Immutable saved page versions. Draft edits do not change the published version.
- Authenticated in-editor preview, explicit publish/unpublish, stale-version conflict handling and republishing a previous saved version.
- Publication audit records committed with the published pointer in the same database transaction.
- Separate `content.read`, `content.manage` and `content.publish` permissions. Existing system Platform Administrator roles receive these through an idempotent identity data migration; other roles need explicit assignment.
- Anonymous rendering at `/site/{siteId}/{slug}`, with `/site/{siteId}` redirecting to `home`. Navigation contains published pages only; titles and descriptions come from the published revision.
- Public DTOs exclude tenant IDs, authors, internal page state and drafts. Suspended tenants return 404. Staff endpoints retain tenant guards, active-user authorization and CSRF checks.

## Hosting boundary

Development accepts `localhost`, `127.0.0.1` and `::1` for local previews. Outside Development, the operator must configure exact `Content:Domains` entries with `Host` and `SiteId`. The public API validates the real Host header against this allowlist and ignores forwarded-host headers. The Next.js renderer uses the incoming Host for the corresponding backend request. Unregistered host/site combinations return 404.

Domain ownership verification and DNS/TLS provisioning are operator responsibilities in this slice. Staff cannot claim a domain through the editor. The public URL still includes `/site/{siteId}`; custom-domain root routing is a later website increment. All content reads use `no-store` initially, so publish and unpublish take effect without a cache invalidation worker.

## Deliberately remaining website work

Media upload/publish grants, dedicated navigation ordering, editable site branding, redirects and slug changes, sitemap/canonical URL configuration, scheduled publication, transactional outbox dispatch and custom-domain root routing remain subsequent increments. This first slice does not claim the full master-plan CMS acceptance gate. No member login or application button is advertised until the actual portal journey exists.

## Next: portal foundation

### Published local multi-page demo — 18 September 2026

Northstar now has Home, About us, Membership, Events, News and Contact pages, published as immutable content revisions. The shared renderer adds compact navigation labels for these standard slugs, footer navigation, responsive feature-card grids, decorative hero artwork and visible keyboard focus. Other page slugs retain their published title in navigation. All page copy remains editable through the existing CMS.

`node scripts/Publish-AssociationDemo.mjs` publishes the sample content only to the known Northstar site in local `NexoraDev`; it requires the expected site and demo administrator and commits revisions, publication pointers and audit rows together. It preserves previous revisions, but rerunning deliberately replaces the current demo drafts and published pointers. It is a development fixture, not a production publishing tool.

The six-page anonymous browser check passed at desktop and 390px widths, including all internal links, header navigation and absence of runtime errors. Desktop/mobile homepage screenshots were visually reviewed. TypeScript and Content ESLint checks passed. Event formats, membership options and contact information are explicitly demonstration content; no fabricated bookings, fees, contact channels or member login are advertised as working. Editable branding, media management and portal journeys remain pending.

Keep the separate portal experience in the same Next.js app. Implement verified external-principal → tenant → Contact binding, invitation/recovery and restricted own-profile contracts before membership/application self-service. Reuse shared CRM and Membership commands; never grant members staff API access or infer company rights from an association flag, email domain or submitted ID. Identity-provider and email-delivery choices are prerequisites to a production portal registration journey.

## Verification and local operation

### VS Code debugging

**21 September startup fix:** Readiness polling now allows the database check to finish and logs context/server/database failures. See [readiness investigation](READINESS-DATABASE-DIAGNOSTICS.md). Restart the compound to rebuild the updated API; do not run Website and Portal compounds simultaneously on their shared debug ports.

Open the repository root in VS Code, select **Website (API + Web)** in Run and Debug, then press **F5**. The compound builds the API with portable symbols, starts it on port 5097 against LocalDB `NexoraDev`, waits for readiness and launches Next.js from `apps/web` on port 3017. Edge opens the published Northstar homepage with browser debugging. Set C# breakpoints in the Content module and TSX breakpoints in the website renderer/editor. Frontend source edits hot reload; restart the compound to rebuild C# changes.

The C# extension/debugger is required. The Node executable follows this machine's existing bundled-runtime path in `.vscode/launch.json`. Use the compound rather than starting its Web configuration alone. No password reset is performed. The existing CRM debug profiles and the port-3016 preview remain separate. Build output is isolated under `WebsiteDebug` and `.next-website-debug`; occupied debug ports produce an error rather than terminating another process. The published demo must already exist in LocalDB; the staff editor is available at `http://localhost:3017/website`.

```powershell
dotnet test backend/tests/Nexora.Api.Tests --configuration Website --filter FullyQualifiedName~ContentEndpointTests
node scripts/Test-Website.mjs
# Optional production build and browser journey (requires Google Fonts access):
node scripts/Test-Website.mjs --production
```

The browser runner uses isolated SQL Server LocalDB data, generated test credentials, ports 5095/3015, copied web sources, separate output directories and the `Website` build configuration. It refuses occupied ports and leaves existing development/debug sessions alone. Tests cover draft isolation, publish/update/rollback/unpublish, unsafe links, host spoofing, suspended tenants and the staff-to-anonymous browser journey. Read actual command results for pass/fail status.

Apply both the Identity `ContentPermissions` migration and Content `InitialContent` migration before running the updated API. Existing signed-in staff must sign out and back in to refresh permission claims. No public deployment or production content publication is part of this initial development work.

### Resume verification — 18 September 2026

- Fixed the interrupted browser check by compiling the anonymous route before editing begins. Compiling it for the first time during the draft journey caused the development server to reload the editor and clear its selected page.
- Staff preview, draft isolation, publish, subsequent draft isolation, republish and unpublish now pass in the anonymous browser journey. The 390px viewport overflow check passes; the desktop screenshot was visually reviewed at `artifacts/association-website.png`.
- Release backend suite: 80 API tests and 2 architecture tests passed; 4 opt-in SQL Server tests skipped. The platform migration replay test was then extended to include Content and passed separately against LocalDB, including replay and pending-model checks.
- TypeScript and targeted website ESLint checks passed.
- Production frontend build remains unverified: Google Fonts DNS/download failures blocked compilation; the stalled build was stopped. The optional `--production` runner preserves this check for an environment with working font access.
- Local editor relaunched at `http://localhost:3016/website`. Portal implementation is the next phase, not part of these completed website checks.
