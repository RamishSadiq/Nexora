# Module listing completion — 4 October 2026

The contact listing is the presentation reference: module heading, separate Add action, Filter view/search toolbar, selection and row actions before data columns, a scrollable table, and bottom counts with 10/20/50/100 rows per page.

## Changes

- Events, Courses and Exams use the shared record listing. Filtering resets pagination; selection and column visibility share the same implementation as Finance, Engagement and Work.
- Website pages now open in a full-width table. Page editing and website settings/media open separately, with a back control and the existing unsaved-page confirmation.
- Portal custom pages now open in a table; portal settings and individual page editing open separately. Draft saving, publishing, permission checks and unsaved-change handling remain in place. An empty custom-page list is valid.
- Administration Users and Security Groups use paginated tables, with Add/Edit forms opened separately. Protected administrator, self-deletion and populated-group restrictions remain intact.
- Finance remounts its workspace when the section changes. Finance and Work/Engagement end loading on failed initial requests. Sidebar child highlighting remains active on detail routes.
- Contact/account selection checkboxes are now controlled and select-all selects the displayed records. Shared selection resets with filters and does not count records removed from the result.

## Verification

- Targeted ESLint and TypeScript passed.
- Production build passed using an isolated `.next-listing-verification` output directory.
- Authenticated browser checks confirmed 17 list entry routes: three Events routes, four Finance routes, three Engagement routes, three Work routes, Website, Portal management, Users and Security Groups. Each rendered a table and pagination; only the search form was visible on list entry. No application error alerts appeared.
- Events: page size 10, next page, submitted search returning to page 1, select-all, hide Venue, record detail and Back to listing passed.
- Website: open page editor and return to table passed. Portal: open settings and return to table passed.
- Mobile Events at 390px: table scrolls horizontally within its container without document-level horizontal overflow.
- Screenshot: `artifacts/listings-events-verified.jpg`.

## Scope

No backend endpoints, domain decisions, record data, publication state or permissions were changed. Browser verification used existing local records and did not save, delete or publish data. Insights remains a reporting workspace and the assistant remains a conversational workspace. No commit, push or deployment was performed.
