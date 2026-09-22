# Automation controllers

## Delivered behavior

All three controllers use the module's authenticated API and tenant-scoped SQL persistence.

- **Attributes:** create, rename, reorder and delete text, number, boolean and date definitions; save typed values with records. Switching entity resets the editor. Calculated inputs are marked in shared forms. Deleting an attribute referenced by a rule is rejected until that rule is removed.
- **Rules:** choose actual built-in source fields or custom attributes and write calculated custom attributes. Supported functions are copy, age, add/subtract a constant, typed equality/inequality, contains and conditional text assignment. Only one rule may write a target. Dependencies are sorted and circular rules, missing fields and incompatible types are rejected.
- **Design:** order Contact/Account detail sections and the Record attributes workspace sections for every other supported entity. Drag-and-drop tracks the dragged item; Up/Down buttons provide keyboard control. The rendered DOM follows the saved order. Settings are shared across users in the same tenant and reload on navigation, window focus or a same-browser change notification.

Configuration writes require module management permission and CSRF protection. The version returned by GET must accompany PUT, preventing one editor from overwriting another editor's changes. Layout and rule saves preserve the other configuration component.

## Rule semantics

Rules execute in the server persistence path when a record or its attribute values are saved, including API clients and task imports. Attribute writes and calculations are committed together; invalid calculated values roll back the operation. Targets are custom attributes. Domain transitions such as posting an invoice or approving an application continue through their module actions.

Existing records are recalculated on their next save. Age uses the UTC save date, treats February 28 as the birthday in a non-leap year for a February 29 birth, and returns empty for future birth dates. There is no daily age refresh or scheduled backfill. Empty sources clear their targets; conditional text assignment also clears its target when the comparison does not match. Equality on text ignores case, and date comparisons use `yyyy-MM-dd`.

For a Contact age rule:

1. Open **Automations → Attributes controller → Contacts**. Add `Date of birth` as a date and `Age` as a number.
2. Open **Rules controller → Contacts**. Select the date attribute, **Calculate age**, and the numeric target. Add and save the rule.
3. Create or edit a Contact and save a birth date. The server fills Age.

The previous browser-only settings are not automatically assigned to the currently signed-in tenant. **Load previous browser settings** reads compatible layouts or age rules into a draft for review. Save that draft to persist it. Previous arithmetic/comparison rules lacked operands and must be recreated using the current editor. The old browser data is retained.

## API and migrations

`GET /api/v1/{module}/automations/{entityType}` returns the version, layout, rules, available sections, field catalogue and management capability. `PUT` accepts the version and either `sections` or `rules`.

The six `AutomationControllers` migrations add `AutomationConfigurations` in CRM, membership, events, finance, engagement and work. Apply these before starting the updated API against an existing database. Use the release runbook's reviewed migration process; production startup does not auto-migrate. The Development seed path applies them when `Identity__SeedAdminPassword` is supplied.

## Verification

- Backend integration tests cover all six business modules, Contact/Account separation, chained calculations, clearing, overflow rollback, referenced attribute deletion, tenant isolation, permissions, CSRF, settings persistence and stale versions.
- SQL Server tests replay migrations for all schemas and check model drift.
- Browser tests exercise attribute CRUD/order, age calculation on a real Contact, a layout shared with a separate browser context, task calculations after updates and Work layout ordering.

Run backend, lint and build checks from the main README. On Windows with SQL Server LocalDB and Microsoft Edge, run `node scripts/Test-Automations.mjs` after `npm ci --prefix apps/web`. The script builds the API, restores the pinned EF tool, creates an isolated test database, starts test servers on 5091/3011, runs Playwright, stops those servers, and drops only its own test database. Test artifacts are under `artifacts/automation-browser-results`; server logs are under `artifacts/automation-browser`.

The browser tests can use another installed Playwright browser channel through `NEXORA_BROWSER_CHANNEL`. They do not use or modify the normal development database.
