import { test, expect } from '@playwright/test';
import { submitLogin } from './browser-login';

test('section fields save in their assigned section and preserve unopened fields', async ({ page }) => {
  await page.goto('/login');
  await page.getByLabel('Work email').fill('admin@nexora.local');
  await page.getByLabel('Password', { exact: true }).fill(process.env.NEXORA_BROWSER_PASSWORD!);
  await submitLogin(page);
  await expect(page).not.toHaveURL(/\/login/);
  const { token } = await (await page.request.get('/api/v1/auth/csrf')).json();
  const post = async (url: string, data: object) => {
    const response = await page.request.post(url, { data, headers: { 'X-CSRF-TOKEN': token } });
    expect(response.ok(), await response.text()).toBeTruthy(); return response.json();
  };
  const suffix = Date.now();
  const general = await post('/api/v1/crm/fields', { kind: 'contact', name: `Pilot region ${suffix}`, dataType: 'text', section: 'General Information' });
  const note = await post('/api/v1/crm/fields', { kind: 'contact', name: `Pilot context ${suffix}`, dataType: 'text', section: 'Notes' });
  const record = await post('/api/v1/crm/records', { kind: 'contact', name: `Field placement ${suffix}`, status: 'active', attributes: { [note.id]: { textValue: 'Preserve this value' } } });
  await page.goto(`/relationships/contacts?search=${encodeURIComponent(record.name)}`);
  await page.getByRole('button', { name: `Edit ${record.name}`, exact: true }).click();
  const section = page.getByRole('region', { name: 'General Information', exact: true });
  await section.getByRole('button', { name: 'General Information', exact: true }).click();
  await section.getByLabel(general.name, { exact: true }).fill('North West');
  await section.getByRole('button', { name: 'Save fields', exact: true }).click();
  await expect.poll(async () => {
    const result = await (await page.request.get(`/api/v1/crm/records/${record.id}`)).json();
    return result.fields.find((value: { definitionId: string }) => value.definitionId === general.id)?.textValue;
  }).toBe('North West');
  const detail = await (await page.request.get(`/api/v1/crm/records/${record.id}`)).json();
  expect(detail.fields.find((value: { definitionId: string }) => value.definitionId === note.id)?.textValue).toBe('Preserve this value');
  const notes = page.getByRole('region', { name: 'Notes', exact: true });
  await notes.getByRole('button', { name: 'Notes', exact: true }).click();
  await expect(notes.getByLabel(note.name, { exact: true })).toHaveValue('Preserve this value');
  await expect(page.getByRole('button', { name: 'Custom fields', exact: true })).toHaveCount(0);
});
