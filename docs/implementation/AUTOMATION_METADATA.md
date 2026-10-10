# Automation metadata

Nexora uses `shared/automation-catalog.json` for section identities and module groups. The web application imports this file; BuildingBlocks embeds the same file. Avoid adding independent lists to controllers or record pages.

## Adding a section

Add one catalogue entry with a stable `id`, display `title`, and `region`. Mark the rendered component with that `data-section` ID (or pass the ID as the `RecordSection` title), and render it within `ConfiguredSections`. Contact/Account main cards use `content`; fields inside their shared details form use `details`. The `rail` and `footer` regions retain their own positions. Sections only reorder within their region, so moving a field section cannot split its form or move an audit tile into an editable form.

The Design controller and record layout use this registration immediately. Stored layouts are reconciled on read: retain available saved IDs in order, remove retired IDs, and append new IDs. Saving a valid older layout also adds any newly registered sections. Section IDs should remain stable when labels change.

Run `npm run verify:automation-catalog` at the repository root. The web prebuild runs this check too. It detects rendered section identities missing from the catalogue and catalogue entries without rendered identities.

## Adding a field or entity

Mapped, readable scalar properties on registered entity models are discovered by `EntityFieldCatalog` for Rules and workflow metadata. It handles nullable text, references, booleans, numbers, enums, and dates. New scalar properties do not require a field allowlist. Tenant IDs, concurrency versions, binary content, nonmapped properties, and properties marked `[AutomationIgnore]` are excluded. Sensitive values must use the ignore attribute.

CRM Contact/Account profile properties are available as read-only `profile:PropertyName` rule sources. Contact social identifiers and assigned profile slots have source adapters. Changes to related profiles recalculate applicable rules. A new storage pattern or derived/collection field requires one source adapter; never advertise a field that cannot resolve its values.

User-created custom attributes remain writable Rules targets. Workflow metadata, expressions, record values, predicates, query filters, schedules, and captured change events expose them with stable `attribute_<definition-id-without-hyphens>` keys. Definition names remain labels rather than field IDs. Tenant filters apply to definitions, values, and query subexpressions.

Register a new domain entity through its module's `WithAttributes` and/or `AddWorkflowEntity` registration. Entity selectors load the backend registrations rather than maintaining another list. Field discovery supplies fields to every consumer of those registrations. Related Contact profiles are also offered in workflow condition suggestions and dynamic-value tokens.

Metadata refreshes on focus and attribute-change notifications. Unsaved Rules/Design edits remain drafts when metadata refreshes, and optimistic concurrency still protects saves. Domain mutations continue to use registered, permission-checked commands; metadata discovery does not make built-in fields arbitrary writable targets.

## Verification

`AutomationCatalogTests` covers future scalar discovery, field typing, old-layout reconciliation, profile/social rule execution, custom-attribute workflow metadata and queries, change snapshots, and tenant isolation. Run it alongside `AutomationTests` and workflow tests when changing the metadata or its consumers.
