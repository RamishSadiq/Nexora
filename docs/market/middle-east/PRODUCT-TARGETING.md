# Nexora product targeting — corrected 9 October 2026

The user clarified that membership is part of Nexora, not the whole product. Apply this policy to NEW Middle East prospect selection. This supersedes campaign-wide first-CRM/membership-only exclusions. Keep the earlier first-CRM file and findings as dated history and as criteria for that specific offer.

## Offers and qualification

| offerType | Suitable need | Current product basis | CRM status gate |
|---|---|---|---|
| website_cms | A business or organisation seeking a branded public information website with staff-managed pages/content | Content editor, preview, publishing/unpublishing, branding, navigation, media controls, sitemap/domain configuration | Existing or unknown CRM is not a disqualification |
| portal | A business or organisation needing controlled signed-in access, profile self-service or private information pages | Explicit user/contact access grants, branding/navigation, published custom plain-text pages and profiles | Existing or unknown CRM is not a disqualification; required workflow must fit implemented functions |
| crm | Organisation seeking its first dedicated company/contact CRM | Company/contact records and staff workspace; optional CRM AI assistant when enabled | Retain current confirmed_no_crm or manual_records_evidence gate for first-system pitches |
| membership | Association/council requiring membership applications, staff review, records and self-service | Built membership workflow connected with CRM/portal | Retain current first-system gate where pitching system adoption; do not assume every membership organisation needs replacement |

Website-only delivery can be scoped without membership applications or subscriptions. Portal-only delivery can be scoped around existing access/profile/content functions, without creating a paid membership; existing UI/module labels may need presentation configuration. Internal tenant, staff and contact dependencies remain part of the platform. This is a commercial offer split, not proof of separate turnkey deployable packages.

## Required candidate evidence

Record organisation, country/local presence, official contact source and role, offerType(s), needEvidence with URLs/source/check dates, capabilityFit, crmStatus (known/unknown), unsupportedRequirements, confidence, outreachDecision, consentStatus, permissionEvidence and outreachLegalBasis. Existing CRM status is informational for website/portal offers. Unknown website/portal need is still a research lead, not established buying intent. Prefer official stated projects, supplier requests, digital-service needs or observed public workflows that genuinely match the offered functions; never invent dissatisfaction, budget or a broken process. A dated/basic website alone is not proof the organisation wants a rebuild.

Independently verify ordinary businesses from their own official websites. Do not harvest an association's member directory as a lead/contact list. Manufacturers, service businesses and other commercial organisations can qualify for public website or a supported portal need; having no membership model is no longer an exclusion. Skip a portal request whose essential function is not implemented. Arbitrary forms, custom workflows, ecommerce/checkout, booking, third-party CRM integrations, Arabic localisation and live local hosting are not automatically covered by the current baseline.

Keep country-specific permission checks, all suppression/prior-company exclusions, complete live reply checks, global hourly/daily caps and positive-response stop. No new email, form submission or message is authorised merely by changing an offerType. Requalify unsent research records using this policy, preserving prior classifications/history. Do not re-email old prospects.

## Capability evidence reviewed in this correction

- backend/src/Modules/Nexora.Modules.Content/ContentModule.cs: public content/page draft, preview and publication API; not dependent on a paid membership.
- backend/src/Modules/Nexora.Modules.Content/PortalPresentation.cs: branding, navigation, bounded custom pages and separate draft/published settings.
- backend/src/Modules/Nexora.Modules.Identity/PortalEndpoints.cs and Domain/PortalAccess.cs: explicit user/tenant/contact grants; membership records are a separate route rather than a prerequisite to granting portal access.
- docs/stages/STAGE-16-CMS-PUBLISHING.md and STAGE-19-PORTAL-CONFIGURATION.md: recorded delivery and verification scope.
- docs/strategy/NEXORA_PROGRESS_REPORT.md: live domains/TLS, production identity, operational services and deployment acceptance remain open. This correction is a source review, not a fresh runtime test or production deployment.
