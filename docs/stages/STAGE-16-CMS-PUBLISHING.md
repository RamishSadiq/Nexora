# Stage 16 — CMS presentation and publishing

23 September 2026. Extends the [association website foundation](STAGE-10-ASSOCIATION-WEBSITE.md).

## Delivered

- Site name, tagline, dark accessible brand colour and clean public logo controls.
- Tenant-owned PNG/JPEG/PDF/text media library, bounded to 100 files of 5 MB each. Uploads remain private; unavailable scans quarantine files. Editors can retry scans and delete unused private files. Publishers grant or revoke anonymous access. Saved revisions retain their referenced assets.
- Image blocks with alternative text and document download blocks. PDFs/text are attachments; public rendering rechecks clean status and grants, including after revocation.
- Published menu labels, ordering and visibility; page address changes preserve old links as redirects to the current published page. Home and platform entry addresses are reserved.
- Published-only XML sitemap and configured-domain canonical metadata.
- Operator-configured domain resolution and Next root/slug routing, with active-tenant checks. Unknown hosts cannot select another tenant through a submitted site ID.
- Durable scheduled publication of a fixed saved revision. The API worker checks due work every 30 seconds and revalidates publisher access and public media grants. Cancellation, manual publication and unpublication cancel pending work. Optimistic concurrency prevents a second publication from competing workers. Failed schedules expose their status for staff review.

## Operations and boundaries

Apply `20260923162840_CmsPublishingControls` through the existing ContentDbContext migration path. The upgrade preserves the previous brand colour and default menu ordering/labels. Verification uses disposable SQL Express databases; it does not migrate the user's development or production database.

Configure the API with `Content:Domains`, an array of unique `{ "Host": "association.example", "SiteId": "<existing-site-guid>" }` entries. Hosts contain no scheme, path or port. Route DNS and TLS to the frontend and preserve the original Host header at the trusted ingress. Set the frontend `NEXORA_API_URL` to its API origin. Production domain and certificate provisioning have not been performed. Development localhost retains `/site/{siteId}/{slug}` access.

The shared scanner uses `Documents:ClamAv:Host` and optional `Documents:ClamAv:Port` (3310). A live scanner has not been provisioned or operationally verified. Clean/infected outcomes are covered through controlled API test scanners; the browser checks quarantine behavior. Never grant access by bypassing scan status.

Scheduling requires the API process to run. Due jobs resume on a later tick after downtime. This bounded scheduler does not implement external publishing webhooks or a general integration outbox. Production identity remains a separate deferred dependency.

## Verification

- `dotnet test backend/tests/Nexora.Api.Tests --configuration CmsFinal -v minimal`, with `NEXORA_TEST_SQLSERVER` pointing to SQL Express: **119 passed, zero failed/skipped** (3m27s). Includes the initial-content-to-current migration upgrade and pending-model assertion. Log: `artifacts/cms-api-verified.log`.
- Full `tsc --noEmit` and targeted ESLint for content, public routes, proxy and website test: passed.
- `node scripts/Test-Website.mjs --production`, with SQL Express: **1 passed** (11.8s), after fixing ambiguous upload labeling and waiting for the menu save response before public reload. Final source includes file terminology and explicit schedule refresh. Log: `artifacts/cms-browser-final.log`.
- Desktop/mobile screenshots visually reviewed; no mobile horizontal overflow. Runner-owned website database and processes cleaned up.
- `node scripts/Test-Website.mjs --portal --production`: **3 passed** (36.9s), covering company grant/application/receipt/revocation and personal approval/rejection, shared profile and inbox. Log: `artifacts/cms-portal-regression.log`. Runner-owned database and processes cleaned up.

 API tests cover permissions, cross-tenant isolation, media grants and revocation, document attachments, redirects, sitemap/domain isolation, pinned scheduled revisions, publisher revocation and upgrade compatibility. The production website journey covers editing, branding, quarantine, navigation, schedule cancellation, mobile layout, draft isolation, republishing and unpublishing.
