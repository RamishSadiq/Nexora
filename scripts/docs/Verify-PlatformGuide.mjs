import fs from 'node:fs';
import path from 'node:path';
import { createRequire } from 'node:module';
import assert from 'node:assert/strict';
const root = path.resolve(import.meta.dirname, '../..');
const runtime = process.env.NEXORA_DOCS_NODE_MODULES;
if (!runtime) throw new Error('Set NEXORA_DOCS_NODE_MODULES to the bundled Node package directory.');
const require = createRequire(path.join(runtime, 'package.json'));
const { chromium } = require('playwright');
const out = path.join(root, 'artifacts/platform-guide-qa');
const source = JSON.parse(fs.readFileSync(path.join(out, 'source-data.json'), 'utf8'));
const errors = [];
const browser = await chromium.launch({ channel: 'msedge', headless: true });
try {
  const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
  page.on('pageerror', error => errors.push(error.message));
  const requests=[]; page.on('request', r => { if (/^https?:/.test(r.url())) requests.push(r.url()); });
  await page.goto(new URL('file:///' + path.join(root, 'docs/beginner-guides/Nexora complete beginner guide.html').replaceAll('\\', '/')).href);
  assert.equal(await page.locator('.chapter-nav').count(), 30);
  const ids = await page.locator('[id]').evaluateAll(nodes => nodes.map(n => n.id));
  assert.equal(new Set(ids).size, ids.length, 'Duplicate navigation IDs');
  assert.equal(await page.locator('[data-source]').count(), 90);
  assert.equal(await page.locator('#source-results button').count(), source.length);
  await page.screenshot({ path: path.join(out, 'manual-desktop.png'), fullPage: false });
  // Every chapter's three buttons must select the correct complete file.
  const buttons = await page.locator('[data-source]').evaluateAll(nodes => nodes.map(n => n.dataset.source));
  for (const id of buttons) {
    await page.evaluate(value => openSource(value), id);
    const expected = source.find(s => s.id === id);
    const displayed = await page.locator('#source-code').textContent();
    const lines = expected.text.split(/\r?\n/);
    assert.equal(displayed, lines.map((t, i) => String(i + 1).padStart(5, '0') + '  ' + t).join('\n'), 'Source mismatch ' + id);
  }
  await page.getByLabel('Find a file or code phrase').fill('member-portal.tsx');
  assert.equal(await page.locator('#source-results button').count(), 1);
  await page.locator('#source-results button').click();
  assert.match(await page.locator('#source-title').textContent(), /member-portal\.tsx/);
  const downloadPending=page.waitForEvent('download');
  await page.getByRole('button',{name:'Download source',exact:true}).click();
  const download=await downloadPending;
  const downloadPath=path.join(out,'downloaded-member-portal.tsx');
  await download.saveAs(downloadPath);
  assert.equal(fs.readFileSync(downloadPath,'utf8'),source.find(s=>s.path.endsWith('/member-portal.tsx')).text);
  await page.screenshot({ path: path.join(out, 'source-desktop.png'), fullPage: false });
  await page.getByLabel('Find a file or code phrase').fill('');
  await page.getByLabel('Narrow to a guide').selectOption('26');
  assert.equal(await page.locator('#source-results button').count(), source.filter(s => s.chapters.includes(26)).length);
  await page.getByLabel('Narrow to a guide').selectOption('0');
  await page.getByRole('button', { name: 'Learning guides', exact: true }).click();
  await page.locator('.chapter-nav').filter({ hasText: '26 Nexora portal management' }).click();
  await page.waitForFunction(() => {
    const top = document.getElementById('guide_26').getBoundingClientRect().top;
    return top >= 70 && top < 150;
  });
  await page.screenshot({ path: path.join(out, 'portal-guide-desktop.png'), fullPage: false });
  const sourceLink = page.locator('#guide_26').locator('..').locator('a[href="#source"]').first();
  if (await sourceLink.count()) await sourceLink.click();
  await page.getByRole('button', { name: 'Learning guides', exact: true }).click();
  await page.getByLabel('Find a guide').fill('portal');
  assert.equal(await page.locator('.chapter-nav:visible').count(), 3);
  await page.getByLabel('Find a guide').fill('');
  await page.setViewportSize({ width: 390, height: 844 });
  await page.evaluate(() => window.scrollTo(0,0));
  await page.screenshot({ path: path.join(out, 'manual-mobile.png'), fullPage: false });
  const overflow = await page.evaluate(() => [...document.querySelectorAll('body *')].filter(n => {
    if (!n.getClientRects().length) return false;
    return n.getBoundingClientRect().right > innerWidth + 1;
  }).slice(0,12).map(n=>({ tag:n.tagName, cls:n.className, text:n.textContent.slice(0,90), width:n.getBoundingClientRect().width, right:n.getBoundingClientRect().right })));
  if(overflow.length)console.log(JSON.stringify({overflow}));
  assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false, 'Mobile overflow');
  assert.deepEqual(requests, [], 'The manual must work without external requests');
  assert.deepEqual(errors, [], 'Browser errors');
  const report = { status: 'passed', chapters: 30, code_buttons: 90, full_source_files: source.length, raw_source_download: true, desktop_and_mobile: true, external_requests: requests.length, browser_errors: errors.length };
  fs.writeFileSync(path.join(out,'browser-evidence.json'),JSON.stringify(report,null,2));
  console.log(JSON.stringify(report));
} finally { await browser.close(); }
