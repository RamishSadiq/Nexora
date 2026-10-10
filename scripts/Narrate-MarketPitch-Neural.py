"""Create a male American alternative to the existing British pitch narration.

An isolated media-tool environment needs edge-tts 7.2.8 and numpy. Only the
public demonstration script is submitted for speech synthesis. Customer data,
model weights and production tools are not included in the exported attachment.
"""

import argparse
import asyncio
import hashlib
import json
from pathlib import Path
import subprocess
import wave

import edge_tts
import numpy as np

parser = argparse.ArgumentParser()
parser.add_argument("--ffmpeg", required=True)
args = parser.parse_args()
root = Path(__file__).resolve().parent.parent
pitch = root / "docs" / "market" / "pitch"
work = root / "artifacts" / "market-narration-male"
work.mkdir(parents=True, exist_ok=True)
script = json.loads((pitch / "narration-script-male.json").read_text(encoding="utf-8"))
sample_rate = 24000
slot_samples = int(script["sceneSeconds"] * sample_rate)


def encode(arguments):
    subprocess.run([args.ffmpeg, "-hide_banner", "-loglevel", "error", *arguments],
                   check=True, creationflags=subprocess.CREATE_NO_WINDOW)


def write_wave(filename, samples):
    with wave.open(str(filename), "wb") as output:
        output.setnchannels(1)
        output.setsampwidth(2)
        output.setframerate(sample_rate)
        output.writeframes((np.clip(samples, -1, 1) * 32767).astype("<i2").tobytes())


def read_wave(filename):
    with wave.open(str(filename), "rb") as source:
        if source.getframerate() != sample_rate or source.getnchannels() != 1:
            raise RuntimeError("Unexpected audio format")
        return np.frombuffer(source.readframes(source.getnframes()), dtype="<i2").astype(np.float32) / 32768


async def main():
    timeline, ledger = [], []
    for index, text in enumerate(script["scenes"]):
        speech = work / f"scene-{index + 1}-source.mp3"
        cache = work / f"scene-{index + 1}-source.json"
        identity = {"text": text, "voice": script["voice"], "rate": script["rate"]}
        # Reuse a successfully downloaded clip only when its exact input matches.
        if not (speech.exists() and cache.exists() and json.loads(cache.read_text(encoding="utf-8")) == identity):
            await edge_tts.Communicate(text, script["voice"], rate=script["rate"]).save(str(speech))
            cache.write_text(json.dumps(identity, indent=2), encoding="utf-8")
        decoded = work / f"scene-{index + 1}-decoded.wav"
        encode(["-i", str(speech), "-ar", str(sample_rate), "-ac", "1", "-c:a", "pcm_s16le", "-y", str(decoded)])
        samples = read_wave(decoded)
        audible = np.flatnonzero(np.abs(samples) > 0.002)
        if not audible.size:
            raise RuntimeError(f"Scene {index + 1} has no speech")
        margin = int(0.08 * sample_rate)
        samples = samples[max(0, audible[0] - margin):min(len(samples), audible[-1] + margin + 1)]
        duration = len(samples) / sample_rate
        if duration > script["sceneSeconds"] - 0.5:
            raise RuntimeError(f"Shorten scene {index + 1}: {duration:.2f}s exceeds the natural-speed budget")
        trimmed = work / f"scene-{index + 1}-trimmed.wav"
        write_wave(trimmed, samples)
        fitted = work / f"scene-{index + 1}.wav"
        encode(["-i", str(trimmed), "-af", "loudnorm=I=-16:TP=-1.5:LRA=9", "-ar", str(sample_rate),
                "-ac", "1", "-c:a", "pcm_s16le", "-y", str(fitted)])
        spoken = read_wave(fitted)
        slot = np.zeros(slot_samples, dtype=np.float32)
        start = int(0.22 * sample_rate)
        if len(spoken) + start > len(slot):
            raise RuntimeError(f"Scene {index + 1} would cut off speech")
        slot[start:start + len(spoken)] = spoken
        timeline.append(slot)
        ledger.append({"scene": index + 1, "startSeconds": index * script["sceneSeconds"] + 0.22,
                       "speechSeconds": round(len(spoken) / sample_rate, 3), "tempo": 1,
                       "sourceSha256": hashlib.sha256(speech.read_bytes()).hexdigest(), "text": text})
        print(f"Scene {index + 1}: {duration:.2f}s, original speaking pace", flush=True)
    narration = np.concatenate(timeline)
    combined = work / "nexora-narration-male.wav"
    write_wave(combined, narration)
    output = pitch / "nexora-narration-male.mp3"
    encode(["-i", str(combined), "-c:a", "libmp3lame", "-b:a", "96k", "-map_metadata", "-1", "-y", str(output)])
    (work / "timing.json").write_text(json.dumps({"engine": script["engine"], "voice": script["voice"],
        "language": script["language"], "rate": script["rate"], "durationSeconds": len(narration) / sample_rate,
        "peak": float(np.max(np.abs(narration))), "rms": float(np.sqrt(np.mean(narration ** 2))),
        "output": str(output), "scenes": ledger}, indent=2), encoding="utf-8")
    print("Created 90-second male American narration.", flush=True)


if __name__ == "__main__":
    # The selector loop avoids Windows Proactor socket-shutdown callback noise.
    asyncio.set_event_loop_policy(asyncio.WindowsSelectorEventLoopPolicy())
    asyncio.run(main())
