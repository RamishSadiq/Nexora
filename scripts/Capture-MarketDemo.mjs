// Capture real local screens using named demonstration records. Never captures a login form.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { createRequire } from 'node:module';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const require = createRequire(path.join(root, 'apps/web/package.json'));
const { chromium, expect: baseExpect } = require('@playwright/test');
const expect = baseExpect.configure({ timeout: 60000 });
const launch = JSON.parse(fs.readFileSync(path.join(root, '.vscode/launch.json')));
const password = launch.configurations.find(c => c.name === 'Nexora shared API').env.Identity__SeedAdminPassword;
const demo = JSON.parse(fs.readFileSync(path.join(root, 'artifacts/local-test-data.json')));
const output = path.join(root, 'docs/market/pitch');
fs.mkdirSync(path.join(output, 'screens'), { recursive: true });
const report = { checkedAt: new Date().toISOString(), checks: [], captures: [] };
const browser = await chromium.launch({ channel: 'msedge', headless: true });
async function capture(page, name, description) {
  await expect(page.locator('main').getByText('Loading record…', { exact: true })).toHaveCount(0);
  await expect(page.locator('main [role="status"]').filter({ hasText: /^Loading / }).filter({ visible: true })).toHaveCount(0);
  await page.evaluate(() => document.fonts.ready);
  await page.screenshot({ path: path.join(output, 'screens', `${name}.png`), animations: 'disabled' });
  report.captures.push({ name, description, url: page.url() });
  console.log(`Captured ${name}`);
}
try {
  for (const port of [3017, 3018, 3019]) {
    const response = await fetch(`http://localhost:${port}/api/v1/auth/providers`, { signal: AbortSignal.timeout(60000) });
    if (!response.ok || !(await response.json()).localDevelopmentEnabled) throw new Error(`API proxy unavailable on ${port}`);
    report.checks.push(`Port ${port}: shared authentication provider proxy passed`);
  }
  const staff = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await staff.newPage();
  page.setDefaultTimeout(60000); page.setDefaultNavigationTimeout(180000);
  const errors = [];
  page.on('pageerror', e => errors.push(e.message));
  await page.goto('http://localhost:3017/login');
  await page.getByLabel('Work email').fill('admin@nexora.local');
  await page.getByLabel('Password', { exact: true }).fill(password);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(page).not.toHaveURL(/\/login/);
  const detail = await (await page.request.get(`http://localhost:3017/api/v1/crm/records/${demo.contactId}`)).json();
  if (!detail.contactDetails?.firstName && !detail.contactDetails?.lastName) {
    const { token } = await (await page.request.get('http://localhost:3017/api/v1/auth/csrf')).json();
    const response = await page.request.put(`http://localhost:3017/api/v1/crm/records/${demo.contactId}/profile`, { headers: { 'X-CSRF-TOKEN': token }, data: { version: detail.record.version, contactDetails: { ...detail.contactDetails, firstName: 'Nexora', lastName: 'Local Test Member' } } });
    if (!response.ok()) throw new Error('Unable to prepare the empty named demo profile');
  }
  await page.goto('http://localhost:3017/relationships/contacts?search=Nexora%20Local%20Test');
  await expect(page.getByRole('button', { name: 'View Nexora Local Test Member', exact: true })).toBeVisible();
  await capture(page, 'contacts', 'Named demonstration contacts in the real CRM');
  await page.getByRole('button', { name: 'View Nexora Local Test Member', exact: true }).click();
  await expect(page.getByRole('region', { name: 'Selected record', exact: true })).toBeVisible();
  await capture(page, 'contact', 'Real selected-contact profile');
  await page.goto('http://localhost:3017/membership/applications/new');
  const productSelect = page.locator('select[name="productId"]');
  await expect(productSelect.locator('option').filter({ hasText: 'Nexora Local Test Membership' })).toHaveCount(1);
  const demoProduct = await productSelect.locator('option').filter({ hasText: 'Nexora Local Test Membership' }).getAttribute('value');
  await productSelect.selectOption(demoProduct);
  await capture(page, 'membership', 'Real application form with the named demo membership product; no submission');
  await page.goto('http://localhost:3017/workflow-designer');
  await expect(page.locator('main')).toContainText(/Workflow|workflow/);
  await capture(page, 'workflow', 'Workflow designer preview; no activation or publication');
  for (const route of ['/dashboard', '/relationships/accounts', '/membership/members', '/membership/applications', '/events/events', '/events/courses', '/events/exams', '/finance/orders', '/finance/invoices', '/finance/receipts', '/finance/catalogue', '/engagement/communities', '/engagement/campaigns', '/engagement/funds', '/work/items', '/work/notifications', '/work/imports', '/website', '/portal-management', '/settings']) {
    const response = await page.goto(`http://localhost:3017${route}`);
    if (!response?.ok() || new URL(page.url()).pathname === '/login') throw new Error(`Staff route failed: ${route} (${response?.status()})`);
    if ((await page.locator('main').innerText()).includes('Internal Server Error')) throw new Error(`Server error on ${route}`);
    report.checks.push(`Authenticated route ${route}: HTTP ${response.status()}`);
  }
  const member = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const portal = await member.newPage(); portal.setDefaultTimeout(60000); portal.setDefaultNavigationTimeout(180000);
  portal.on('pageerror', e => errors.push(e.message));
  await portal.goto('http://localhost:3018/portal');
  await portal.getByLabel('Email', { exact: true }).fill('local.member@nexora.local');
  await portal.getByLabel('Password', { exact: true }).fill(password);
  await portal.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(portal.getByRole('heading', { name: 'Dashboard', exact: true })).toBeVisible();
  await capture(portal, 'portal', 'Signed-in local demo member dashboard');
  const profile = await portal.request.get('http://localhost:3018/api/v1/portal/profile');
  if (!profile.ok()) throw new Error('Portal profile failed');
  if ((await portal.request.get('http://localhost:3018/api/v1/crm/records')).status() !== 401) throw new Error('Portal-only member accessed staff CRM');
  report.checks.push('Member sign-in and profile passed; staff CRM denied to Portal-only member');
  await portal.goto('http://localhost:3018/portal/profile');
  await expect(portal.getByLabel('Display name', { exact: true })).toHaveValue('Nexora Local Test Member');
  await capture(portal, 'profile', 'Member profile using the named demo contact');
  const cms = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  cms.setDefaultNavigationTimeout(180000);
  const response = await cms.goto(`http://localhost:3019${demo.publicPath}`);
  if (!response.ok()) throw new Error('Published demo website unavailable');
  await expect(cms.getByRole('heading', { level: 1 }).first()).toBeVisible();
  await capture(cms, 'website', 'Real published local association website');
  report.checks.push('Published demo CMS visitor page passed alongside CRM/Portal');
  if (errors.length) throw new Error(`Browser runtime errors: ${errors.join('; ')}`);
  report.checks.push('No browser pageerror events in observed CRM/Portal pages');
} finally {
  fs.writeFileSync(path.join(root, 'artifacts/market-readiness-local.json'), JSON.stringify(report, null, 2));
  await browser.close();
}
console.log(`Local route checks: ${report.checks.length}`);
