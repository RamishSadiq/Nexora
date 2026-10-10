"""Generate locally narrated, precisely timed audio for the existing nine-scene tour.

Use an isolated Python environment with kokoro-onnx 0.5.0 and the upstream
Kokoro v1.0 model/voices. These are media tools, not customer app dependencies.
"""

import argparse
import json
import os
from pathlib import Path
import subprocess
import wave

os.environ.setdefault("OMP_NUM_THREADS", "2")

import numpy as np
import onnxruntime as ort
from kokoro_onnx import Kokoro

parser = argparse.ArgumentParser()
parser.add_argument("--model", required=True)
parser.add_argument("--voices", required=True)
parser.add_argument("--ffmpeg", required=True)
args = parser.parse_args()
root = Path(__file__).resolve().parent.parent
pitch = root / "docs" / "market" / "pitch"
work = root / "artifacts" / "market-narration"
work.mkdir(parents=True, exist_ok=True)
script = json.loads((pitch / "narration-script.json").read_text(encoding="utf-8"))
session_options = ort.SessionOptions()
session_options.intra_op_num_threads = 2
session_options.inter_op_num_threads = 1
session = ort.InferenceSession(args.model, sess_options=session_options, providers=["CPUExecutionProvider"])
kokoro = Kokoro.from_session(session, args.voices)
sample_rate = 24000
slot_samples = int(script["sceneSeconds"] * sample_rate)
timeline = []
ledger = []


def write_wave(filename, samples):
    pcm = (np.clip(samples, -1, 1) * 32767).astype("<i2")
    with wave.open(str(filename), "wb") as output:
        output.setnchannels(1)
        output.setsampwidth(2)
        output.setframerate(sample_rate)
        output.writeframes(pcm.tobytes())


for index, text in enumerate(script["scenes"]):
    samples, rate = kokoro.create(text, voice=script["voice"], speed=1.05, lang=script["language"])
    if rate != sample_rate:
        raise RuntimeError(f"Unexpected sample rate: {rate}")
    samples = np.asarray(samples, dtype=np.float32).reshape(-1)
    audible = np.flatnonzero(np.abs(samples) > 0.002)
    if not audible.size:
        raise RuntimeError(f"Scene {index + 1} has no speech")
    # Keep breathing room around the speech; trim only leading/trailing silence.
    margin = int(0.06 * sample_rate)
    samples = samples[max(0, audible[0] - margin):min(len(samples), audible[-1] + margin + 1)]
    duration = len(samples) / sample_rate
    raw_path = work / f"scene-{index + 1}-raw.wav"
    write_wave(raw_path, samples)
    available = script["sceneSeconds"] - 0.55
    tempo = max(1.0, duration / available)
    if tempo > 1.2:
        raise RuntimeError(f"Scene {index + 1} needs shorter wording: {duration:.2f}s")
    fitted_path = work / f"scene-{index + 1}.wav"
    subprocess.run([
        args.ffmpeg, "-hide_banner", "-loglevel", "error", "-i", str(raw_path),
        "-af", f"atempo={tempo:.6f},loudnorm=I=-16:TP=-1.5:LRA=9",
        "-ar", str(sample_rate), "-ac", "1", "-c:a", "pcm_s16le", "-y", str(fitted_path)
    ], check=True)
    with wave.open(str(fitted_path), "rb") as fitted:
        spoken = np.frombuffer(fitted.readframes(fitted.getnframes()), dtype="<i2").astype(np.float32) / 32768
    slot = np.zeros(slot_samples, dtype=np.float32)
    start = int(0.2 * sample_rate)
    if len(spoken) + start > len(slot):
        raise RuntimeError(f"Scene {index + 1} would cut off speech")
    slot[start:start + len(spoken)] = spoken
    timeline.append(slot)
    ledger.append({"scene": index + 1, "startSeconds": index * script["sceneSeconds"] + 0.2,
                   "speechSeconds": round(len(spoken) / sample_rate, 3), "tempo": round(tempo, 4), "text": text})
    print(f"Scene {index + 1}: {len(spoken) / sample_rate:.2f}s speech in a 10s scene", flush=True)

narration = np.concatenate(timeline)
wave_path = work / "nexora-narration.wav"
write_wave(wave_path, narration)
subprocess.run([
    args.ffmpeg, "-hide_banner", "-loglevel", "error", "-i", str(wave_path),
    "-c:a", "libmp3lame", "-b:a", "96k", "-map_metadata", "-1", "-y", str(pitch / "nexora-narration.mp3")
], check=True)
(work / "timing.json").write_text(json.dumps({"voice": script["voice"], "language": script["language"],
    "durationSeconds": len(narration) / sample_rate, "peak": float(np.max(np.abs(narration))),
    "rms": float(np.sqrt(np.mean(narration ** 2))), "scenes": ledger}, indent=2), encoding="utf-8")
print("Created 90-second scene-aligned British narration.", flush=True)
