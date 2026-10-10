import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { createRequire } from 'node:module';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const output = path.join(root, 'docs/market/pitch');
const require = createRequire(path.join(root, 'apps/web/package.json'));
const { chromium, expect } = require('@playwright/test');
const browser = await chromium.launch({ channel: 'msedge', headless: true });
try {
  const page = await browser.newPage({ viewport: { width: 1280, height: 720 } });
  const errors = [];
  page.on('pageerror', e => errors.push(e.message));
  await page.goto(pathToFileURL(path.join(output, 'index.html')).href);
  for (let scene = 0; scene < 9; scene++) {
    await page.getByRole('button', { name: `Scene ${scene + 1}`, exact: true }).click();
    await page.locator('#screen').evaluate(img => img.decode());
    await expect(page.locator('#count')).toHaveText(`${scene + 1} / 9`);
    const fits = await page.evaluate(() => {
      const stage = document.querySelector('.stage').getBoundingClientRect();
      const copy = document.querySelector('.copy').getBoundingClientRect();
      const visual = document.querySelector('.frame').getBoundingClientRect();
      return document.documentElement.scrollWidth <= innerWidth && copy.bottom <= stage.bottom && visual.bottom <= stage.bottom;
    });
    if (!fits) throw new Error(`Scene ${scene + 1} overflows`);
  }
  const duration = await page.evaluate(src => new Promise((resolve, reject) => {
    const video = document.createElement('video');
    video.preload = 'metadata';
    video.onloadedmetadata = () => resolve({ duration: video.duration, width: video.videoWidth, height: video.videoHeight });
    video.onerror = () => reject(new Error('Generated video cannot be decoded'));
    video.src = src; document.body.append(video);
  }), pathToFileURL(path.join(output, 'nexora-product-tour.webm')).href);
  if (Math.abs(duration.duration - 90) > 1 || duration.width !== 1280 || duration.height !== 720 || errors.length) throw new Error(`Invalid tour output: ${JSON.stringify({ duration, errors })}`);
  let attachment = null;
  const hasMaleNarration = fs.existsSync(path.join(output, 'nexora-narration-male.mp3'));
  const hasNarration = hasMaleNarration || fs.existsSync(path.join(output, 'nexora-narration.mp3'));
  const attachmentPath = path.join(output, hasMaleNarration ? 'nexora-product-demo-male.mp4'
    : hasNarration ? 'nexora-product-demo-narrated.mp4' : 'nexora-product-demo.mp4');
  if (hasNarration && !fs.existsSync(attachmentPath)) throw new Error('Export the narrated MP4 after generating narration.');
  if (fs.existsSync(attachmentPath)) {
    attachment = await page.evaluate(src => new Promise((resolve, reject) => {
      const video = document.createElement('video');
      const samples = [0.5, 45, 89];
      const decoded = [];
      const timeout = setTimeout(() => finish(new Error('MP4 playback check timed out')), 15000);
      const finish = error => {
        clearTimeout(timeout);
        video.remove();
        if (error) reject(error);
        else resolve({ duration: video.duration, width: video.videoWidth, height: video.videoHeight, decodedSampleSeconds: decoded });
      };
      video.onloadedmetadata = () => { video.currentTime = samples[0]; };
      video.onseeked = () => {
        try {
          const canvas = document.createElement('canvas');
          canvas.width = video.videoWidth; canvas.height = video.videoHeight;
          const context = canvas.getContext('2d');
          context.drawImage(video, 0, 0);
          const pixels = context.getImageData(0, 0, 32, 32).data;
          if (!pixels.some((value, index) => index % 4 !== 3 && value > 0)) throw new Error('Decoded MP4 sample is blank');
          decoded.push(samples[decoded.length]);
          if (decoded.length === samples.length) finish();
          else video.currentTime = samples[decoded.length];
        } catch (error) { finish(error); }
      };
      video.onerror = () => finish(new Error('MP4 cannot be played in Edge'));
      video.src = src; document.body.append(video);
    }), `data:video/mp4;base64,${fs.readFileSync(attachmentPath).toString('base64')}`);
    attachment.path = attachmentPath;
    attachment.bytes = fs.statSync(attachmentPath).size;
    if (Math.abs(attachment.duration - 90) > 0.1 || attachment.width !== 1280 || attachment.height !== 720 || attachment.bytes > 5 * 1024 * 1024) throw new Error(`Invalid MP4 attachment: ${JSON.stringify(attachment)}`);
    if (hasNarration) {
      attachment.audio = await page.evaluate(async base64 => {
        const bytes = Uint8Array.from(atob(base64), character => character.charCodeAt(0));
        const context = new AudioContext();
        try {
          const buffer = await context.decodeAudioData(bytes.buffer);
          const samples = buffer.getChannelData(0);
          const sections = [];
          let peak = 0;
          for (const sample of samples) peak = Math.max(peak, Math.abs(sample));
          for (let scene = 0; scene < 9; scene++) {
            const start = Math.floor(scene * 10 * buffer.sampleRate);
            const end = Math.min(samples.length, Math.floor((scene + 1) * 10 * buffer.sampleRate));
            let sum = 0;
            for (let i = start; i < end; i++) sum += samples[i] ** 2;
            sections.push(Math.sqrt(sum / (end - start)));
          }
          if (sections.some(rms => rms < 0.005) || peak >= 1) throw new Error('MP4 has a silent scene or clipped narration');
          return { duration: buffer.duration, sampleRate: buffer.sampleRate, channels: buffer.numberOfChannels, peak, sceneRms: sections };
        } finally { await context.close(); }
      }, fs.readFileSync(attachmentPath).toString('base64'));
      if (Math.abs(attachment.audio.duration - 90) > 0.2) throw new Error('MP4 narration duration mismatch');
    }
  }
  fs.writeFileSync(path.join(root, 'artifacts/market-pitch-verification.json'), JSON.stringify({ checkedAt: new Date().toISOString(), scenes: 9, allScenesFit: true, runtimeErrors: errors, video: duration, attachment }, null, 2));
  console.log(`Verified nine interactive scenes and the 90-second video${attachment ? ', including MP4 playback at the beginning, middle and end' : ''}${attachment?.audio ? ', plus non-silent narration in all nine scenes' : ''}.`);
} finally { await browser.close(); }
