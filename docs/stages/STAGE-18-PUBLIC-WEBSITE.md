# Stage 18 — Complete local public website

24 September 2026.

## Delivered

The single-page local sample is now a nine-page association website: Home, About, Membership, Events, Resources, Contact, FAQs, Member guide and Organisation guide. Six main navigation items keep the header compact; Resources and Membership link to the supporting guides and FAQs. Every page has its own title, description, saved CMS revision and published content. All copy remains editable in the existing staff CMS.

The renderer adds an always-available member portal link, page descriptions and a return-home link on inner pages. Same-site content links follow the configured public base path, including configured-domain hosting. Image blocks now span the content grid correctly. CMS management has an **Open public website** shortcut.

`Prepare-LocalTestData.mjs` uses `scripts/lib/association-website.mjs` to create missing pages and upgrade only the original unchanged sample homepage. Existing custom pages, drafts, branding and passwords are preserved. The first verified run published nine pages; the second published zero and preserved all nine. Publication uses the existing authenticated, version-checked CMS API.

## Verification

- Local debugger smoke: staff CRM/CMS access, member sign-in and repeatable preparation passed.
- Anonymous public browser checks: all nine pages at 1440px and 390px, page metadata, current navigation, ten internal destinations, membership-to-guide-to-portal navigation and all nine sitemap entries passed.
- Desktop/mobile screenshots for Home, Membership and Resources saved under `artifacts/public-*.png`; Home reviewed visually at both widths.
- Full TypeScript and targeted CMS renderer/editor ESLint passed.
- `node scripts/Test-Website.mjs --production` with SQL Express: production build passed and the staff draft/preview/publish/unpublish regression passed (1 test, 13.8s). The runner removed its disposable database and stopped its processes. Log: `artifacts/public-site-publishing-regression.log`.

Public acceptance log: `artifacts/public-site-verification.log`.

## How to open and edit

In VS Code choose **CMS**, then F5. This opens the public homepage. Use `/website` with staff access to edit pages, save a draft, preview and publish. The complete local address is in [LOCAL-TESTING.md](../LOCAL-TESTING.md).

This is a local association demonstration, not a deployed customer website. Events accurately state that none are announced; Contact provides help routes and identifies that no public inbox is configured. No invented event dates, customer statistics, contact details or payment capability are presented. Real association copy/contact details, live identity/email/scanning and domain/deployment acceptance remain production dependencies.
