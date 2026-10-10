import { submitLogin } from "./browser-login";
import { test, expect, type Page } from "@playwright/test";

async function write(page: Page, path: string, data: unknown) {
  const csrf = await (await page.request.get("/api/v1/auth/csrf")).json();
  const response = await page.request.post(`/api/v1/crm${path}`, { data, headers: { "X-CSRF-TOKEN": csrf.token } });
  expect(response.ok(), await response.text()).toBeTruthy();
  return response.json();
}

for (const kind of ["account", "contact"] as const) {
  test(`${kind} associations expose history, pages and load failures`, async ({ page }) => {
    await page.goto("/login");
    await page.getByLabel("Work email").fill("admin@nexora.local");
    await page.getByLabel("Password", { exact: true }).fill(process.env.NEXORA_BROWSER_PASSWORD!);
    await submitLogin(page);
    await expect(page).not.toHaveURL(/\/login/);

    const suffix = `${kind}-${Date.now()}`;
    const record = await write(page, "/records", { kind, name: `History ${suffix}`, status: "active" });
    await page.goto(`/relationships/${kind === "account" ? "accounts" : "contacts"}?record=${record.id}`);
    await page.getByRole("button", { name: kind === "account" ? "Contacts" : "Employers", exact: true }).click();
    await expect(page.getByText(kind === "account" ? "No current contacts are linked to this account." : "No current employers are linked to this contact.", { exact: true })).toBeVisible();
    const links = [];
    for (let i = 0; i < 26; i++) {
      const other = await write(page, "/records", { kind: kind === "account" ? "contact" : "account", name: `Related ${suffix} ${String(i).padStart(2, "0")}`, status: "active" });
      links.push(await write(page, `/accounts/${kind === "account" ? record.id : other.id}/contacts`, {
        contactId: kind === "contact" ? record.id : other.id, startDate: "2026-01-01T00:00:00Z", workClassification: "unknown", roles: [],
      }));
      if (i === 1) {
        const detail = await (await page.request.get(`/api/v1/crm/records/${other.id}`)).json();
        await write(page, `/records/${other.id}/archive`, { version: detail.record.version });
      }
    }
    await write(page, `/account-contacts/${links[0].id}/end`, { version: links[0].version, endDate: "2026-02-01T00:00:00Z", reason: "Term completed" });
    await page.goto(`/relationships/${kind === "account" ? "accounts" : "contacts"}?search=${encodeURIComponent(record.name)}`);
    await page.getByRole("button", { name: `Edit ${record.name}`, exact: true }).click();
    const panel = page.locator("section").filter({ has: page.getByRole("heading", { name: kind === "account" ? "Contacts" : "Employers", exact: true }) }).last();
    await panel.getByRole("button", { name: kind === "account" ? "Contacts" : "Employers", exact: true }).click();
    await page.getByRole("button", { name: "Related Records", exact: true }).click();
    await expect(panel.getByText("Page 1 of 1", { exact: true })).toBeVisible();
    await expect(panel.getByText(/Structured associations/)).toBeVisible();
    await expect(page.getByText(/Other connections/)).toBeVisible();
    const currentRow = panel.getByRole("listitem").filter({ hasText: `Related ${suffix} 02` });
    await currentRow.getByRole("button", { name: "Edit association", exact: true }).click();
    const editForm = currentRow.getByRole("form", { name: "Edit association details" });
    await expect(editForm).toContainText(`Edit association with Related ${suffix} 02`);
    await expect(editForm.getByLabel("Department", { exact: true })).toBeVisible();
    await editForm.getByRole("button", { name: "Cancel", exact: true }).click();
    await currentRow.getByRole("button", { name: "End association", exact: true }).click();
    const endForm = panel.getByRole("form", { name: "End association details" });
    await expect(endForm).toContainText(`End association with Related ${suffix} 02`);
    await expect(endForm.getByLabel("End date", { exact: true })).toBeVisible();
    await endForm.getByRole("button", { name: "Cancel", exact: true }).click();
    const archivedRow = panel.getByRole("listitem").filter({ hasText: "Linked record is archived." });
    await expect(archivedRow.getByRole("button", { name: "Edit association" })).toBeDisabled();
    await expect(archivedRow.getByRole("button", { name: "End association", exact: true })).toBeEnabled();
    await expect(panel.getByText("Reason: Term completed")).toHaveCount(0);
    await panel.getByLabel("Include ended associations").check();
    await expect(panel.getByText("Reason: Term completed")).toBeVisible();
    const endedRow = panel.getByRole("listitem").filter({ hasText: "Reason: Term completed" });
    await expect(endedRow.getByRole("button", { name: "Edit association" })).toHaveCount(0);
    await expect(endedRow.getByRole("button", { name: "End association", exact: true })).toHaveCount(0);
    await expect(panel.getByText("Started 2026-01-01 · Ended 2026-02-01", { exact: true })).toBeVisible();
    await expect(panel.getByText("Page 1 of 2", { exact: true })).toBeVisible();
    await panel.getByRole("button", { name: "Next associations" }).click();
    await expect(panel.getByText("Page 2 of 2", { exact: true })).toBeVisible();
    await panel.getByLabel("Include ended associations").uncheck();
    await expect(panel.getByText("Page 1 of 1", { exact: true })).toBeVisible();

    const endpoint = `**/api/v1/crm/records/${record.id}/account-contacts?*`;
    await page.route(endpoint, route => route.fulfill({ status: 503, json: { title: "Unavailable" } }));
    await panel.getByLabel("Include ended associations").check();
    await expect(panel.getByRole("alert")).toContainText("Associations could not be loaded.");
    await expect(panel.getByText(/No current/)).toHaveCount(0);
    await page.unroute(endpoint);
    await panel.getByRole("button", { name: "Retry associations" }).click();
    await expect(panel.getByText("Reason: Term completed")).toBeVisible();
    await page.screenshot({ path: `../../artifacts/association-${kind}-desktop.png`, fullPage: true });
    await page.setViewportSize({ width: 390, height: 844 });
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBeTruthy();
    await page.screenshot({ path: `../../artifacts/association-${kind}-mobile.png`, fullPage: true });
  });
}
