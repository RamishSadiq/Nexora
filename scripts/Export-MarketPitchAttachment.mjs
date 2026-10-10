import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawn } from 'node:child_process';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const source = path.join(root, 'docs/market/pitch/nexora-product-tour.webm');
const maleNarration = path.join(root, 'docs/market/pitch/nexora-narration-male.mp3');
const narration = fs.existsSync(maleNarration) ? maleNarration : path.join(root, 'docs/market/pitch/nexora-narration.mp3');
const includeNarration = !process.argv.includes('--silent') && fs.existsSync(narration);
const output = path.join(root, 'docs/market/pitch', includeNarration
  ? narration === maleNarration ? 'nexora-product-demo-male.mp4' : 'nexora-product-demo-narrated.mp4'
  : 'nexora-product-demo.mp4');
const encoder = process.argv.find(value => value.startsWith('--ffmpeg='))?.slice('--ffmpeg='.length)
  || process.env.NEXORA_PITCH_FFMPEG;
if (!encoder || !fs.existsSync(encoder)) throw new Error('Supply a trusted H.264-capable FFmpeg path using --ffmpeg=... or NEXORA_PITCH_FFMPEG.');
if (!fs.existsSync(source)) throw new Error('Build the market product tour before exporting the attachment.');

async function run(args) {
  return new Promise((resolve, reject) => {
    const child = spawn(encoder, args, { windowsHide: true, stdio: ['ignore', 'ignore', 'pipe'] });
    let diagnostics = '';
    child.stderr.on('data', chunk => { diagnostics = (diagnostics + chunk.toString()).slice(-8000); });
    child.on('error', reject);
    child.on('exit', code => code === 0 ? resolve(diagnostics) : reject(new Error(`Encoder exited ${code}: ${diagnostics}`)));
  });
}

await run([
  '-hide_banner', '-loglevel', 'error', '-i', source,
  ...(includeNarration ? ['-i', narration] : []),
  '-map', '0:v:0',
  ...(includeNarration ? ['-map', '1:a:0', '-c:a', 'aac', '-b:a', '96k', '-af', 'apad'] : ['-an']),
  '-t', '90', '-r', '24',
  '-c:v', 'libx264', '-preset', 'medium', '-tune', 'stillimage', '-crf', '22',
  '-pix_fmt', 'yuv420p', '-movflags', '+faststart', '-map_metadata', '-1',
  '-metadata', 'title=Nexora — 90-second product demonstration', '-y', output,
]);
// Decode the entire exported video: metadata alone cannot establish that all frames work.
await run(['-hide_banner', '-loglevel', 'error', '-i', output, '-map', '0', '-f', 'null', '-']);
let audio = null;
if (includeNarration) {
  const diagnostic = await run(['-hide_banner', '-i', output, '-map', '0:a:0', '-af', 'volumedetect', '-f', 'null', '-']);
  const meanDb = Number(diagnostic.match(/mean_volume: ([\d.-]+) dB/)?.[1]);
  const peakDb = Number(diagnostic.match(/max_volume: ([\d.-]+) dB/)?.[1]);
  if (!Number.isFinite(meanDb) || !Number.isFinite(peakDb) || meanDb < -35 || peakDb >= 0) throw new Error(`Invalid narration levels: ${diagnostic}`);
  audio = { codec: 'aac', meanDb, peakDb };
}
const bytes = fs.statSync(output).size;
if (bytes === 0 || bytes > 5 * 1024 * 1024) throw new Error(`Attachment exceeds the 5 MiB target or is empty: ${bytes} bytes.`);
const result = { output, narration: includeNarration ? narration : null, bytes, mebibytes: Number((bytes / 1024 / 1024).toFixed(2)), fullDecodePassed: true, audio };
fs.writeFileSync(path.join(root, 'artifacts/market-pitch-export.json'), JSON.stringify(result, null, 2));
console.log(JSON.stringify(result));
