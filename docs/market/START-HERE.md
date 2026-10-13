# Nexora UK market launch pack

**Latest campaign instruction, 12 October:** [Australia, US, New Zealand and Canada](MULTI-MARKET-CAMPAIGN.md) are the only active countries, every five hours, aiming for one suitable fresh prospect per country and four total per run. The user authorised sends without repeated approval. New Middle East pitches stop; all historical replies and suppressions remain monitored. This replaces earlier dual-market/regional/hourly scope below.

Current campaign improvements, 10 October 2026: [client acquisition plan](CLIENT-ACQUISITION-PLAN.md), [focused offers](OFFER-BRIEFS.md) and [Middle East revised pitches](middle-east/PITCH-V2.md). New outreach follows the international policy above and country-specific rules; earlier UK/US/Middle East materials below remain historical reference where their scope or cadence conflicts.

Prepared 7 October 2026. Target: UK professional and trade associations with company memberships, a small administration team, and roughly 200–2,000 member organisations or individuals. These are targeting hypotheses, not verified capacity limits.

**Positioning:** Nexora connects organisation and contact records, membership administration, member self-service and association website content, so staff can follow the same member journey across those areas.

Start with the [sales playbook](SALES-PLAYBOOK.md), [prospect list](UK-PROSPECTS.md) and [pricing model](PRICING.md). Current verification and release limitations are in [product health](PRODUCT-HEALTH.md).

The [first five email drafts](FIRST-FIVE-EMAILS.md), revised 8 October, introduce the working product with “We've built Nexora” and refer to the attached MP4 demonstration. Replace the signature placeholders, attach the video and check the contact route before sending. No public demo hosting is needed for this route.

The drafts and narration also describe access to an AI assistant within the CRM when enabled. This is a configurable embedded workspace; automatic CRM record access is disabled. Verify the upstream assistant before a live AI demonstration or contracted scope. The video covers the core membership product and does not show an AI chat interaction.

Attach the [90-second narrated MP4 demonstration](pitch/nexora-product-demo-male.mp4) to the first email. The recipient can download and play it without a public link or product sign-in. It has a male American voiceover and on-screen captions. The [original WebM](pitch/nexora-product-tour.webm) and [interactive product tour](pitch/index.html) remain available as silent alternatives; the HTML works offline in Edge/Chrome. All use actual local screenshots with named demonstration data. Workflow footage is explicitly a designer preview. A hosted public URL has not been deployed. See [demo sharing](DEMO-SHARING.md) for attachment instructions and [narration notes](pitch/NARRATION.md) for the script and regeneration steps.

To refresh the artifacts after changing the product, start the three Nexora local frontends and shared API, prepare the named local test data, then run `node scripts/Capture-MarketDemo.mjs`, `node scripts/Build-MarketPitch.mjs`, `node scripts/Export-MarketPitchAttachment.mjs --ffmpeg=<absolute-encoder-path>` and `node scripts/Verify-MarketPitch.mjs`. Capture uses the local development identities from the launch configuration and does not place credentials in the pitch.

The first commercial objective is **three qualified discovery calls and one agreed pilot**, not selling every module immediately. Keep the first pilot to one association, one membership product, one administrator journey and one member journey. Agree the production dependencies and acceptance criteria before onboarding live member data.

All eleven eligible personalised pitches were confirmed in Gmail Sent on 8 October, each with the narrated video attachment verified. BASA remains held without prior permission. The [outreach log](outreach/2026-10-08/STATUS.md) contains the recipients and Sent links; [Google quota guidance and the applied policy](outreach/2026-10-08/GMAIL-QUOTA-POLICY.md) document pacing and backoff. Do not resend confirmed messages. Sending does not establish recipient delivery, viewing or purchasing intent.

## US market campaign — 8 October 2026

The latest search focuses on first CRM adoption. [Prospect qualification rules](us/CRM-FIRST-TARGETING.md) require current manual-membership evidence or an explicit no-CRM statement, exclude confirmed existing-system users and hold unknown cases for research. The hourly automation has been updated accordingly; no-CRM status is never inferred from website appearance.

The [9 October morning batch](us/outreach/2026-10-09/STATUS.md) adds 10 new personalised US pitches, all verified in Gmail Sent with the narrated video and no duplicates. The user subsequently requested hourly sending, which replaces the daily schedule.

On 9 October the user authorised US outreach to fresh prospects until positive replies arrive, then changed its cadence to hourly. The active chat task attempts one new qualified organisation at minute 00 each hour, with at most 24 per UK date including manual sends, reply/suppression checks, duplicate prevention, narrated video attachment and a shorter tailored pitch. The first attempt under the revised schedule is 9 October at 12:00 UK time. See the [hourly playbook](us/daily/PLAYBOOK.md) and [revised pitch](us/US-PITCH-V2.md).

The US expansion screened 118 candidates, shortlisted 50 and selected 30 for individual first pitches. See the [US launch pack](us/README.md), [ranked shortlist](us/SHORTLIST-50.md), [all candidates](us/ALL-118-CANDIDATES.md), [personalised pitches](us/outreach/2026-10-08/PITCHES.md) and [US send ledger](us/outreach/2026-10-08/campaign.json). The US emails include the narrated MP4, optional CRM AI wording, the user-confirmed postal address and a reply-based opt-out. Company-membership fit is inferred; buyer need and budget are unverified. Do not resend confirmed messages. BAFSA's refusal is recorded in the [global suppression list](OUTREACH-SUPPRESSIONS.json).


## Market change — 9 October 2026

The user shifted new hourly prospect research to Middle East. See docs/market/middle-east/PLAYBOOK.md and its CRM qualification/country-rule files. Historical UK/US sends remain intact. Shared state and confirmed-send ledgers retain the legacy US paths for global caps and duplicate checks.
