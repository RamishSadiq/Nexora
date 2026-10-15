import { submitLogin } from "./browser-login";
import { test, expect, type Page } from "@playwright/test";

async function login(page: Page) {
  await page.goto("/login");
  await page.getByLabel("Work email").fill("admin@nexora.local");
  await page.getByLabel("Password", { exact: true }).fill(process.env.NEXORA_BROWSER_PASSWORD!);
  await submitLogin(page);
  await expect(page).not.toHaveURL(/\/login/);
}
async function openFilters(page: Page) {
  const trigger = page.getByRole("button", { name: "Filter view", exact: true });
  if (await trigger.getAttribute("aria-expanded") !== "true") await trigger.click();
  const filters = page.getByRole("button", { name: "Filters", exact: true });
  if (await filters.getAttribute("aria-expanded") !== "true") await filters.click();
}
async function write(page: Page, path: string, data?: unknown, method = "POST") {
  const csrf = await (await page.request.get("/api/v1/auth/csrf")).json();
  const response = await page.request.fetch(`/api/v1/crm${path}`, { method, data, headers: { "X-CSRF-TOKEN": csrf.token } });
  expect(response.ok(), await response.text()).toBeTruthy();
  return response.status() === 204 ? null : response.json();
}

test("personal CRM views save typed columns and filters, restore, update and recover deleted fields", async ({ page }) => {
  await login(page);
  const suffix = Date.now().toString();
  const fieldIds: string[] = [];
  for (const dataType of ["text", "number", "boolean", "date"]) {
    const field = await write(page, "/fields", { name: `${dataType} ${suffix}`, kind: "contact", dataType });
    fieldIds.push(field.id);
  }
  const record = await write(page, "/records", { kind: "contact", name: `View person ${suffix}`, status: "active", category: "Member",
    attributes: { [fieldIds[0]]: { textValue: "Example value" }, [fieldIds[1]]: { numberValue: 0 }, [fieldIds[2]]: { booleanValue: false }, [fieldIds[3]]: { dateValue: "2026-09-18T00:00:00Z" } } });
  await page.goto("/relationships/contacts");
  await openFilters(page);
  await expect(page.getByRole("combobox", { name: "Saved view", exact: true })).toBeEnabled();
  await page.getByRole("searchbox", { name: "Search records", exact: true }).fill(record.name);
  await page.getByRole("button", { name: "Search", exact: true }).click();
  await openFilters(page);
  await page.getByRole("combobox", { name: "Status", exact: true }).selectOption("active");
  await page.getByLabel("Category", { exact: true }).fill("Member");
  await page.keyboard.press("Escape");
  await page.getByRole("button", { name: "Choose columns", exact: true }).click();
  const panel = page.getByRole("dialog", { name: "Columns", exact: true });
  await expect(panel.getByLabel("Name", { exact: true })).toBeDisabled();
  for (const dataType of ["text", "number", "boolean", "date"]) await panel.getByLabel(`${dataType} ${suffix} (${dataType})`, { exact: true }).check();
  await page.keyboard.press("Escape");
  const row = page.getByRole("row").filter({ has: page.getByRole("button", { name: record.name, exact: true }) });
  await expect(row.getByRole("cell", { name: "Example value", exact: true })).toBeVisible();
  await expect(row.getByRole("cell", { name: "0", exact: true })).toBeVisible();
  await expect(row.getByRole("cell", { name: "No", exact: true })).toBeVisible();
  await expect(row).toContainText("2026");
  await openFilters(page);
  await page.getByRole("button", { name: "Save as new view" }).click();
  await page.getByLabel("View name", { exact: true }).fill(`My view ${suffix}`);
  await page.getByRole("button", { name: "Save view", exact: true }).click();
  await expect(page.getByRole("combobox", { name: "Saved view", exact: true })).toHaveValue(/.+/);
  const viewId = await page.getByRole("combobox", { name: "Saved view", exact: true }).inputValue();
  const saved = await (await page.request.get(`/api/v1/crm/views/${viewId}`)).json();
  expect(saved.columns.length).toBeGreaterThan(100);
  await page.reload();
  await openFilters(page);
  await expect(page.getByRole("combobox", { name: "Saved view", exact: true })).toHaveValue(viewId);
  await expect(row).toContainText("Example value");
  // Explicit empty URL filters override saved values; a view-only URL restores its defaults.
  await page.goto(`/relationships/contacts?viewId=${viewId}&status=&category=&page=1`);
  await openFilters(page);
  await expect(page.getByRole("combobox", { name: "Status", exact: true })).toHaveValue("");
  await expect(page.getByLabel("Category", { exact: true })).toHaveValue("");
  await page.getByRole("combobox", { name: "Status", exact: true }).selectOption("inactive");
  await expect(page.getByText("No records match these filters.", { exact: false })).toBeVisible();
  await page.goBack();
  await openFilters(page);
  await expect(page.getByRole("combobox", { name: "Status", exact: true })).toHaveValue("");
  await expect(row).toContainText("Example value");
  await page.goto(`/relationships/contacts?viewId=${viewId}`);
  await openFilters(page);
  await expect(page.getByRole("combobox", { name: "Status", exact: true })).toHaveValue("active");
  await page.getByRole("combobox", { name: "Sort", exact: true }).selectOption("-name");
  await page.getByRole("button", { name: "Save or rename view" }).click();
  await page.getByLabel("View name", { exact: true }).fill(`Renamed ${suffix}`);
  await page.getByRole("button", { name: "Save view", exact: true }).click();
  await expect(page.getByRole("option", { name: `Renamed ${suffix}` })).toHaveCount(1);
  await page.reload();
  await openFilters(page);
  await expect(page.getByRole("combobox", { name: "Sort", exact: true })).toHaveValue("-name");
  await page.goto("/relationships/accounts");
  await openFilters(page);
  await expect(page.getByRole("combobox", { name: "Saved view", exact: true })).toBeEnabled();
  await expect(page.getByRole("option", { name: `Renamed ${suffix}` })).toHaveCount(0);
  await write(page, `/fields/${fieldIds[0]}`, undefined, "DELETE");
  await page.goto(`/relationships/contacts?viewId=${viewId}`);
  await openFilters(page);
  await expect(page.getByText("Unavailable columns were removed from this listing.")).toBeVisible();
  await expect(page.getByRole("columnheader", { name: `text ${suffix}` })).toHaveCount(0);
  await page.getByRole("button", { name: "Save or rename view" }).click();
  await page.getByRole("button", { name: "Save view", exact: true }).click();
  await expect(page.getByRole("form", { name: "Save saved view" })).toHaveCount(0);
  await page.screenshot({ path: "../../artifacts/crm-saved-views.png", fullPage: true });
  await page.getByRole("button", { name: "Delete view", exact: true }).click();
  await page.getByRole("button", { name: "Confirm delete" }).click();
  await expect(page.getByRole("combobox", { name: "Saved view", exact: true })).toHaveValue("");
  await page.goto(`/relationships/contacts?viewId=${viewId}`);
  await openFilters(page);
  await expect(page.getByText("Saved view is unavailable. Showing the URL filters instead.")).toBeVisible();
});

test("CRM record deep links survive reload and return to the filtered list", async ({ page }) => {
  await login(page);
  const record = await write(page, "/records", { kind: "contact", name: `Deep link ${Date.now()}`, status: "prospect" });
  await page.goto(`/relationships/contacts?record=${record.id}&status=prospect`);
  await expect(page.getByRole("button", { name: "Back to listing", exact: true })).toBeVisible();
  await page.reload();
  await page.getByRole("button", { name: "Back to listing", exact: true }).click();
  await openFilters(page);
  await expect(page.getByRole("combobox", { name: "Status", exact: true })).toHaveValue("prospect");
  await expect(page.getByRole("button", { name: record.name, exact: true })).toBeVisible();
});
