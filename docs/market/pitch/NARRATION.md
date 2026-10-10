# Narrated Nexora pitch

Selected revision, 8 October 2026: a male American voice with a warm, conversational delivery. Shorter sentences, contractions and punctuation replace the earlier formal British read. The intended effect is engaging and easy to follow; perceived naturalness and audience retention have not been measured.

The [current script](narration-script-male.json) opens with “Too much member admin? Meet Nexora. We've built it…” and covers company/contact records, profile context, membership applications, member self-service, website content, workflow design and configurable AI assistant access. The final invitation is a fifteen-minute demonstration.

Each passage starts 0.22 seconds into its ten-second scene. The generator trims only leading/trailing silence and normalises speech. The voice retains its original speaking pace and pitch; long passages must be shortened rather than accelerated. No words are cut off to fit. This is synthetic narration, not a human recording or customer endorsement.

The working product screens use demonstration data. The workflow scene is a designer preview. AI access is described as available when enabled; the narration does not claim automatic access to CRM records or execution of CRM actions. There is no AI-chat footage in the current tour.

The selected email attachment is [nexora-product-demo-male.mp4](nexora-product-demo-male.mp4). The standalone [male voiceover](nexora-narration-male.mp3) is available for review. The [earlier British version](nexora-product-demo-narrated.mp4) is preserved for comparison. The original MP4, WebM and interactive HTML tour remain silent alternatives.

## Regeneration

Use an isolated Python environment with `edge-tts==7.2.8` and NumPy; these are media production tools, separate from Nexora's runtime. The speech service needs network access. Only the public demonstration script is submitted, with no customer records or credentials. Run:

```text
python scripts/Narrate-MarketPitch-Neural.py --ffmpeg=<absolute-encoder-path>
node scripts/Export-MarketPitchAttachment.mjs --ffmpeg=<absolute-encoder-path>
node scripts/Verify-MarketPitch.mjs
```

The exporter selects `nexora-narration-male.mp3` when present and writes `nexora-product-demo-male.mp4`. Otherwise it falls back to the earlier narration and filename. `--silent` exports `nexora-product-demo.mp4`, preserving narrated versions. Select the explicitly named male version as the email attachment.

## Sources and production evidence

Current voice: `en-US-AndrewMultilingualNeural`, male American English, through the Microsoft Edge speech service using the [edge-tts client](https://github.com/rany2/edge-tts). The voice catalogue labels this voice conversational and warm. These labels describe the provider's intended voice character; they do not establish that listeners cannot distinguish it from human speech. No impersonation or voice cloning was used. Raw generated clips and their input metadata are retained under `artifacts/market-narration-male` for reproducibility.

The Runway speech connector was also checked for an expressive alternative but returned `UNAUTHORIZED`, requiring app reconnection. No Runway speech task was generated. The selected attachment uses the working alternative above.

`artifacts/market-narration-male/timing.json` records per-scene speech duration, original tempo, input text and generated-clip hashes. `artifacts/market-pitch-export.json` records the selected filename, full-stream decode, size and narration levels. `artifacts/market-pitch-verification.json` checks duration, frames and audible audio for all nine scenes in Edge. These checks establish media integrity and timing, not subjective voice quality.

Earlier version: `bf_emma`, British English, generated locally using `kokoro-onnx==0.5.0` and upstream Kokoro v1.0 assets. Its [script](narration-script.json), MP3, MP4, generation script and `artifacts/market-narration/timing.json` remain available. [Kokoro voice documentation](https://huggingface.co/hexgrad/Kokoro-82M/blob/main/VOICES.md), [model and licence](https://huggingface.co/hexgrad/Kokoro-82M), [ONNX inference tool](https://github.com/thewh1teagle/kokoro-onnx). Tool/model downloads were recorded with source URLs, byte counts and hashes; production tools are not shipped with the attachment.
