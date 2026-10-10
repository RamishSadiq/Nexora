# Sharing the demo in your first pitch

Prepared 8 October 2026. The selected 90-second MP4 now includes male American narration explaining the product, plus the existing on-screen captions. It uses demonstration data; the workflow scene is a designer preview. It is not a recording of a complete applicant-to-approval transaction. Offer a focused live walkthrough on the call for that process. The original WebM/HTML versions remain silent alternatives. See the [narration script and production notes](pitch/NARRATION.md).

The revised emails and narration mention Nexora's configurable in-CRM AI assistant workspace. The video does not show an AI chat interaction. Do not label it an AI demonstration or imply that it shows automatic CRM-data analysis. Before adding an AI scene, verify the configured upstream assistant, actual sign-in and a successful interaction using demonstration inputs; agree the capabilities and costs included in an evaluation. CRM record access remains disabled as documented in [the integration notes](../nexorchestr-integration.md).

The chosen first-pitch route is a video attachment. Attach [nexora-product-demo-male.mp4](pitch/nexora-product-demo-male.mp4) to the email; the recipient receives a downloadable copy and does not need a public URL or access to your computer. The file is a 90-second, 1280x720 H.264 MP4 with AAC narration and on-screen captions. Use the MP4 rather than sending a Windows path, an HTML attachment or a ZIP. All eleven eligible pitches were confirmed in Gmail Sent with this attachment on 8 October; see the [outreach ledger](outreach/2026-10-08/STATUS.md). No public viewing link has been deployed.

Male American revision checked 8 October 2026: **1,945,313 bytes (about 2 MB)** before email encoding, 90 seconds, 1280x720. Full video/audio decode passed, with narration mean −17.9 dB and peak −1.8 dB. Edge decoded beginning/middle/end frames and non-silent audio in all nine scenes. The rewritten script uses the voice's original speaking pace, with no pitch alteration or speech acceleration. Evidence is in `artifacts/market-pitch-export.json`, `artifacts/market-pitch-verification.json` and `artifacts/market-narration-male/timing.json`. The earlier British narrated and silent exports remain separate files.

## Send the MP4 as an attachment

1. Open the revised [first five drafts](FIRST-FIVE-EMAILS.md), select the relevant message and replace the real business signature details.
2. In your mail application's attachment picker, choose `docs/market/pitch/nexora-product-demo-male.mp4` from this Nexora checkout. Check the compose window lists it as a file attachment; if your mail service offers a cloud link, select the actual attachment option.
3. The message already says: “I've attached a 90-second narrated demonstration of Nexora. It also has on-screen captions, so you can watch it with or without sound.” Check the file is attached before sending.
4. Send a test to your own other inbox and download/play the received copy, including on a phone if available. Some recipient systems may block video attachments; if a prospect reports this, offer a hosted viewing link or a live walkthrough instead.

The exported file is kept below a 5 MiB target. Email transport adds encoding overhead, so check the sender's actual attachment limit. No login, installation, or connection to the product backend is needed to play the file in a compatible player. Do not imply all recipients' mail security policies have been tested.

To refresh it after rebuilding the WebM tour, generate scene-aligned narration as described in [the production notes](pitch/NARRATION.md), run `node scripts/Export-MarketPitchAttachment.mjs --ffmpeg=<absolute-encoder-path>` with a trusted H.264-capable FFmpeg executable, then `node scripts/Verify-MarketPitch.mjs`. The exporter includes narration automatically when its MP3 is present, checks size and decodes the complete MP4. FFmpeg is an offline media tool; it is not a dependency of the customer application.

## Optional alternative: an unlisted video

Upload [nexora-product-tour.webm](pitch/nexora-product-tour.webm) to the chosen Nexora/business YouTube channel and select **Unlisted**, not Private. YouTube [supports WebM](https://support.google.com/youtube/troubleshooter/2888402?hl=en-gb); [unlisted videos](https://support.google.com/youtube/answer/157177?co=GENIE.Platform%3DDesktop&hl=en-1) can be viewed by anyone with the link without a Google account. Anyone receiving the link can also share it. An unlisted video is not access-controlled customer content. Use the demonstration data already in the file.

Suggested title:

> Nexora | Membership management for associations — 90-second product tour

Suggested description:

> We've built Nexora to connect company and contact records, membership applications, staff review, member self-service and association website content.
>
> This narrated, captioned tour shows the working product using demonstration data. The workflow designer is shown as a preview; configuration and live services are agreed for each evaluation.
>
> Interested in seeing how Nexora could fit your association? Reply to the email that shared this demonstration to arrange a 15-minute conversation.

Use [cover.png](pitch/cover.png) as the preview image if the channel offers custom thumbnails. Follow the host's required upload settings according to the actual audience and content. If using this alternative, replace the attachment sentence with “Here's a 90-second demonstration using the working product: [actual viewing URL]”. Do not copy the creator/editor URL or leave a placeholder in a sent message.

## Alternative: your existing business website

Publish the self-contained [interactive tour](pitch/index.html) at an agreed HTTPS path on the real business website. It embeds its own screenshot assets, so this tour file does not need the screenshot directory or product backend to run. Visitors can use “Play tour” or the scene controls. Use the resulting tour URL in the email, with “Here's a short interactive demonstration” in place of the attachment sentence.

If the website instead hosts a video player, publish the WebM with the correct `video/webm` content type, playback controls and a preview image; test the actual recipient devices before using it. The normal hosted-video service is the simpler initial route and handles playback conversion. Do not link to the live local application or create unrestricted access to member records.

## Final checks before the first send

1. Check the compose window lists `nexora-product-demo-male.mp4` as an attachment and the message says it is attached. No demo-link placeholder should remain.
2. Play the full 90 seconds and check the caption text is readable; check a phone as well.
3. Send the finished email to your own second/test inbox and download/play the received attachment there. For a hosted alternative, test the actual viewing URL while signed out.
4. Replace every signature placeholder. Use your real sender name, business identity, reply address and website if you have one; omit a website you do not yet have.
5. Verify the prospect contact route and legal entity, record the source and honour opt-outs as described in the sales playbook.
6. Read it once as the recipient: why this association, what Nexora does, proof via the demo, and one invitation to a 15-minute call.

Keep detailed module lists, pricing and contractual production scope for the relevant follow-up or discovery call. Do not add customer logos, testimonials, uptime guarantees or measured savings that have not been established.
