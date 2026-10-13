# Public website design and CRM controls — 12 October 2026

## Requested outcome

A Framer-inspired public website: a dark canvas, large typography, generous spacing, glowing media frames, varied feature sections and purposeful motion. The site must be authored and managed through Nexora CRM. Framer is a visual reference, not a hosting or runtime dependency.

## Analysis and gaps

The Content module already provides tenant-owned sites, page revisions, navigation, secure media, preview, publishing, scheduling and restoration of previous page versions. The renderer currently fixes the layout, colours, header, portal link, footer and decoration in code. Its six block types have no presentation or animation settings.

Missing controls are global website design, section layouts, structured cards and galleries, interactive showcases and FAQs, configurable motion, and draft/published separation for global design. Existing public-media grants must also apply to images nested inside new sections.

The user confirmed that this redesign applies to the existing CRM-managed association website and its landing pages. A separate Nexora marketing website is outside this change.

## Implementation plan

1. Add validated global design drafts and publication using existing Content permissions, tenant scope and concurrency checks. Expose only published design to visitors.
2. Extend versioned page blocks with layout, visibility, media, motion and structured items. Validate links, bounds and nested media server-side.
3. Provide CRM controls for all presentation and animation parameters used by the renderer, including effect, trigger, duration, delay, distance, easing, hover effect and ambient motion. Respect visitor reduced-motion preferences.
4. Build a responsive studio theme, feature grids, image showcases, galleries and expandable FAQs. Keep existing page types and content usable.
5. Verify configuration round trips, publishing isolation, stale updates, validation, media revocation, migration replay and representative desktop/mobile browsing.

## Manageability boundary

All content and all layout and motion settings implemented by this renderer must be editable in CRM. Adding a new kind of component or a bespoke effect remains development work; this is not an arbitrary-code animation editor or a full Framer canvas. Video upload is outside the existing scanned PNG/JPEG/PDF/text media pipeline and remains future scope unless separately implemented and verified.

## Editing in CRM

- Open **Website pages → Website settings and media → Site branding and media → Website design, layout and animations** for the palette, font choice, headline size, content width, section gap, corners, header/footer and default motion. Save a design draft, preview it, then publish the saved design.
- Edit a page to manage its text, media, links, section order and structured items. **Section layout and animation** controls width, alignment, surface, spacing, visibility and optional motion overrides.
- **Use studio landing layout** supplies editable hero, feature-grid, showcase, FAQ and call-to-action sections. It replaces draft sections after confirmation. Review the starter copy and links before saving and publishing.
- Page drafts and design drafts publish independently. Restoring a page revision restores that revision's sections and overrides; it does not restore the global website design.
- Brand colour applies to the association logo mark; the primary and secondary design accents control decorative highlights. Each design colour has a picker and a hex field. Logo/media publication keeps its existing scan and public-grant requirements.

## Delivery status

Implemented the studio renderer, structured sections, global design editor and per-section layout/motion controls. The global design has independent saved and published states; page presentation stays inside existing page revisions. The additive migration introduces tenant-owned website configuration and its audit table.

Verified: TypeScript, scoped frontend lint, the production frontend build, and 13 distinct API/migration checks across scoped runs, including SQL Server upgrade preservation and nested media revocation. The ordinary editor permission check uses a non-system role because Nexora system administrators always have every permission.

The extended production browser workflow passed: editable section surface/motion overrides, structured sections, keyboard tab navigation, expandable FAQs, design preview/save/publish, draft isolation, persistence after reload, mobile layout, reduced-motion preferences and the global animation switch. Visual review caught and fixed a section/inner-grid CSS class collision; the workflow now checks desktop feature-card width as well as horizontal overflow. Corrected desktop/mobile screenshots were inspected.

The shared local API was rebuilt using `scripts/Prepare-LocalDebug.ps1`; readiness succeeded and the additive migration was applied to NexoraDev. The running CRM's design controls were inspected in the in-app browser. `node scripts/Verify-PublicWebsite.mjs` passed for all nine existing anonymous pages at 1440px and 390px, ten internal links, the navigation-to-member-portal journey and sitemap. Existing local page content and links were retained.

Screenshots are saved in `artifacts/framer-inspired-association.jpg`, `artifacts/website-crm-design-controls.jpg`, `artifacts/association-website.png`, `artifacts/association-website-mobile.png` and `artifacts/public-*.png`. The association and CRM controls are available at `http://localhost:3017/site/26b20458-a580-40a8-8345-369ad954a54d/home` and `http://localhost:3017/website`.

The requested neon update adds cyan/violet hero lighting, subtle background washes, two-colour card borders and button highlights to the studio theme. The secondary accent is validated and persisted alongside the existing design settings; existing configurations receive its default without a schema migration. The local palette (`#50f5d6` / `#9255ff`) was saved and published through CRM. TypeScript, scoped lint and the shared API build passed, and the read-only public website check passed again for all nine pages at desktop/mobile widths, internal links, the portal journey and sitemap. The updated desktop screenshot is `artifacts/public-home-1440.png`; the in-app browser capture is `artifacts/neon-association.jpg`.

Unrelated market-campaign and progress-report changes already present in the checkout are outside this work. No production deployment or Git delivery was requested.
