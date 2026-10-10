# Market-readiness verification

Assessment started 7 October 2026 against the current working tree. Existing uncommitted product changes are preserved. This is an evidence report, not a statement that every feature is production ready.

## Reproduced startup failure and repair

VS Code `local-ensure-api-crm` exited with code 1, matching the user's screenshot. The API crashed with `PendingModelChangesWarning` for `MembershipDbContext`. The shared `AttributeDefinition.Section` field was absent from migrations/snapshots for Membership, Events, Finance, Engagement and Work. Added additive migrations for all five; existing definitions receive `General Information` rather than a blank invalid section. No database reset is required.

API build tasks now use one MSBuild worker, no retained build nodes and no shared compiler. API integration tests run serially because each test may host an application/database; the workstation had very little free memory during diagnosis.

The isolated browser runner now preserves `apps/web` plus the versioned `shared` catalogue layout and `--all` selects the whole browser suite. Previously it copied only frontend files and selected four test files. Disposable test databases remain separate from `NexoraDev`.

The broader check also repaired incomplete custom-field placement: CRM fields now render and save in their assigned existing section, preserving values in unopened sections. Contact profiles fall back to their stored display name when structured name fields are empty. Membership products and portal product settings are reachable again through Setup, with a list-first product page and separate record form. Membership lists retain search, status and pagination when returning from details or reloading. Organisation/contact choices wait while lookups are working, preventing a search response from clearing a selection made before it completed. Native module controls now have explicit accessible names independent of their option text.

Browser tests were updated for the existing list-first UI without removing their business assertions: application failure recovery, approval, cancellation, permission revocation, private-note isolation, draft/public separation and mobile overflow checks remain covered.

## Verification ledger

| Check | Current outcome |
| --- | --- |
| API LocalDebug build after migrations | Passed; zero warnings/errors; prelaunch script subsequently returned ready with exit 0 |
| SQL-enabled full backend suite after repair | 260 API tests and 2 architecture tests passed; no failures/skips |
| TypeScript / ESLint | Passed after final functional and dependency repairs, with no reported errors/warnings |
| Concurrent CRM / Portal / CMS startup | Passed using the launch runtime and settings on 3017/3018/3019 with shared API 5097; 26 recorded checks. After the Next 16.3.6 update, all three proxies and login pages passed again |
| Production frontend build | Passed on pinned Next 16.3.6 after security updates; current isolated API build had zero warnings/errors |
| Isolated production browser coverage | All 17 enabled journeys passed across the patched whole-suite run and focused membership rerun; 3 existing skips. Whole suite: 16 passed / 1 failed on an ambiguous history assertion. Final focused run: both membership journeys passed after the selector and lookup-timing repairs |
| Real UI tour | Seven screens captured; nine interactive scenes fit at 1280x720; decoded video metadata confirms 90 seconds / 1280x720; no tour runtime errors |
| Narrated email attachment, male American revision, 8 October | H.264/AAC MP4: 1,945,313 bytes (about 2 MB); 90 seconds / 1280x720. Full video/audio decode passed; narration mean −17.9 dB / peak −1.8 dB. Edge decoded beginning/middle/end frames and non-silent audio in all nine scenes. Original speaking pace; subjective voice quality remains for listener review. No hosted link is needed |
| .NET dependency advisory check | No vulnerable packages reported, including transitive dependencies |
| Production npm dependency advisory check | Initial audit: one critical / two high advisories. After Next 16.3.6, sharp 0.35.5 and source-map-js 1.2.2 updates: zero production advisories |

Detailed outputs are under `artifacts/market-readiness*`; local smoke evidence is `artifacts/market-readiness-local.json` and tour evidence is `artifacts/market-pitch-verification.json`. Backend Release compilation emitted the known ASP.NET route-handler analyzer `AD0001` warnings; tests still completed successfully. The console log preserves both backend totals; the fixed-name TRX was overwritten by the architecture project.

Final browser evidence is `artifacts/market-readiness-browser-release.log` plus `artifacts/market-readiness-membership-final.log`. These establish coverage across runs; a single final green whole-suite run is not claimed. The focused run rebuilt the current frontend in production mode and verified existing/new-contact linking, failed submission recovery, approval, the membership history note and stored API results. Disposable browser databases were dropped by their runners. Post-patch simultaneous frontend checks are in `artifacts/market-readiness-patched-local.json`.

Local smoke checks include 20 authenticated staff-route HTTP checks, the three proxy checks, portal-only CRM denial, member profile and the published visitor site. They are not complete browser business-journey coverage of every module. The three existing browser skips are two broader automation editor journeys and navigation relying on an externally provisioned member login; the new field-placement journey covers section persistence separately. Company Dash is a separate project: its screenshot showed a successful launch, and its source was not changed here. The temporary frontend processes used for verification were stopped to release the VS Code ports; the shared API was left ready.

The dependency repair follows the published [Next.js advisory and patched version](https://github.com/advisories/GHSA-vcvr-r3jv-pc5j), [sharp advisory](https://github.com/advisories/GHSA-wq5f-xc86-pv6w) and [source-map-js advisory](https://github.com/advisories/GHSA-68fv-2mgg-jv7q). Advisory presence does not itself prove that Nexora exposes an exploitable route; no `next/og` / `ImageResponse` usage was found in app source.

The full development-dependency audit still reports five high entries from one [unpatched braces advisory](https://github.com/advisories/GHSA-vfj7-8cjw-p6xm) propagated through the ESLint/fast-glob dependency chain. The registry's latest braces release is 3.0.3 and the advisory lists no patched version. Keep lint/build patterns controlled by repository maintainers, keep development tools out of deployed runtime dependencies, and resolve or explicitly review this advisory before release. This is documented as open, not silently accepted. `npm audit fix --force` proposes downgrading the Next ESLint configuration to 14.2.35; that was not applied. Next and its ESLint configuration remain pinned to matching 16.3.6 versions. The stalled native sharp download was retrieved directly and checked against registry SHA-512 integrity before populating npm's cache.

The patched sharp native module was also loaded and successfully generated a PNG; this checks the Windows binary rather than only its package metadata. The automation catalogue check passed with 21 rendered section identities and 8 module groups.

## Commercial boundary

Existing release documentation identifies production authentication, external email/payment services, live file scanning, domain/TLS, deployment operations and recovery as open acceptance areas. Local development authentication is not a customer-facing production sign-in solution. An in-app notification does not prove email delivery; a finance record does not prove settled money; an inactive workflow draft does not prove a live automation.

Begin market discovery and synthetic-data demonstrations now. Scope a pilot around the verified core journey. Before live customer onboarding, agree and verify production identity/session behavior, a hosted environment, backups and restore, role/tenant isolation, migration reconciliation, and any email/payment/scanning services included in the quote. The existing [release runbook](../operations/RELEASE-RUNBOOK.md) remains the authoritative operational checklist.
