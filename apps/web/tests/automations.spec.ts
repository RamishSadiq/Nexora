import { test, expect, type Page } from "@playwright/test";

async function login(page: Page) {
  await page.goto("/login");
  await page.getByLabel("Work email").fill("admin@nexora.local");
  await page.getByLabel("Password", { exact: true }).fill(process.env.NEXORA_BROWSER_PASSWORD!);
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await expect(page).not.toHaveURL(/\/login/, { timeout: 60_000 });
}
async function addField(page: Page, name: string, type: string) {
  const form = page.locator("form").filter({ has: page.getByRole("button", { name: "Add attribute", exact: true }) });
  await form.getByLabel("Name", { exact: true }).fill(name);
  await form.getByLabel("Data type").selectOption(type);
  await form.getByRole("button", { name: "Add attribute", exact: true }).click();
  await expect(page.getByRole("button", { name: `Move ${name} up`, exact: true })).toBeVisible();
}

test("attributes, calculated rules, and shared CRM layouts work through the browser", async ({ page, browser }) => {
  await login(page);
  await page.goto("/automations/contacts/attributes-controller");
  await addField(page, "Date of birth", "date");
  await addField(page, "Age", "number");
  await addField(page, "Temporary field", "text");
  const temporary = page.locator("li").filter({ has: page.getByRole("button", { name: "Move Temporary field up", exact: true }) });
  await temporary.getByLabel("Name", { exact: true }).fill("Renamed field");
  await temporary.getByRole("button", { name: "Save", exact: true }).click();
  await expect(page.getByRole("button", { name: "Move Renamed field up", exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Move Age up", exact: true }).click();
  await expect(page.locator("li").first().getByLabel("Name", { exact: true })).toHaveValue("Age");
  page.once("dialog", dialog => dialog.accept());
  await page.locator("li").filter({ has: page.getByRole("button", { name: "Move Renamed field up", exact: true }) }).getByRole("button", { name: "Delete", exact: true }).click();
  await expect(page.getByRole("button", { name: "Move Renamed field up", exact: true })).toHaveCount(0);

  await page.goto("/automations/contacts/rules-controller");
  await page.getByLabel("Source field").selectOption({ label: "Date of birth (date)" });
  await page.getByRole("combobox", { name: "Function", exact: true }).selectOption("age");
  await page.getByLabel("Target attribute").selectOption({ label: "Age (number)" });
  await page.getByRole("button", { name: "Add rule", exact: true }).click();
  await page.getByRole("button", { name: "Save rules", exact: true }).click();
  await expect(page.getByRole("status")).toContainText("Changes saved");
  await page.reload();
  await expect(page.getByRole("list", { name: "Configured rules" })).toContainText("Calculate age");

  await page.goto("/relationships/contacts");
  await page.getByRole("button", { name: "Create contact", exact: true }).click();
  await page.getByLabel("Full name", { exact: true }).fill("Browser automation contact");
  await page.getByLabel("Date of birth", { exact: true }).fill("2000-01-01");
  await expect(page.getByLabel("Age", { exact: true })).toHaveAttribute("readonly", "");
  await page.getByRole("button", { name: "Save contact", exact: true }).click();
  await expect(page.getByLabel("Age (number)", { exact: true })).toHaveValue(String(new Date().getUTCFullYear() - 2000));

  await page.goto("/automations/contacts/design-controller");
  // Keep both drag endpoints visible so scrolling cannot move the source under the pointer.
  await page.setViewportSize({ width: 1440, height: 1200 });
  const list = page.locator("main ol");
  await expect(list.locator("li").first()).toContainText("Addresses");
  const source = list.locator("li").filter({ hasText: "Custom fields" });
  const target = list.locator("li").first();
  await source.dragTo(target, {
    sourcePosition: { x: 10, y: 10 },
    targetPosition: { x: 10, y: 10 },
  });
  await expect(list.locator("li").first()).toContainText("Custom fields");
  await page.getByRole("button", { name: "Save layout", exact: true }).click();
  await expect(page.getByRole("status")).toContainText("Changes saved");
  const context = await browser.newContext({ baseURL: process.env.NEXORA_BROWSER_URL ?? "http://localhost:3011" });
  const second = await context.newPage(); await login(second);
  await second.goto("/relationships/contacts");
  await second.getByRole("button", { name: "View Browser automation contact", exact: true }).click();
  await expect(second.getByRole("region", { name: "Selected record" }).locator("h2").nth(1)).toHaveText("Custom fields");
  await context.close();
});

test("work layouts and rules apply to the record attributes workspace", async ({ page }) => {
  await login(page);
  await page.goto("/automations/work/attributes-controller");
  await addField(page, "Source amount", "number"); await addField(page, "Total", "number");
  await page.goto("/automations/work/rules-controller");
  await page.getByLabel("Source field").selectOption({ label: "Source amount (number)" });
  await page.getByRole("combobox", { name: "Function", exact: true }).selectOption("add");
  await page.getByLabel("Target attribute").selectOption({ label: "Total (number)" });
  await page.getByLabel("Amount", { exact: true }).fill("5");
  await page.getByRole("button", { name: "Add rule", exact: true }).click();
  await page.getByRole("button", { name: "Save rules", exact: true }).click();
  await expect(page.getByRole("status")).toContainText("Changes saved");
  await page.goto("/work");
  const form = page.locator("form").filter({ has: page.getByLabel("Source amount", { exact: true }) });
  await form.getByLabel("title", { exact: true }).fill("Browser automation task");
  await form.getByLabel("description", { exact: true }).fill("Verify calculated fields");
  await form.getByLabel("Source amount", { exact: true }).fill("10");
  await form.getByRole("button").click();
  await expect(page.getByText("Completed successfully.", { exact: true })).toBeVisible();
  const attributes = page.locator("section").filter({ has: page.getByRole("heading", { name: "Record attributes", exact: true }) });
  await attributes.getByRole("button", { name: "Refresh records" }).click();
  await attributes.getByRole("combobox", { name: "Record", exact: true }).selectOption({ label: "Browser automation task" });
  await expect(attributes.getByLabel("Total", { exact: true })).toHaveValue("15");
  await attributes.getByLabel("Source amount", { exact: true }).fill("20");
  await attributes.getByRole("button", { name: "Save attributes", exact: true }).click();
  await expect(attributes.getByLabel("Total", { exact: true })).toHaveValue("25");
  await page.goto("/automations/work/design-controller");
  await page.getByRole("button", { name: "Move Custom fields up", exact: true }).click();
  await page.getByRole("button", { name: "Save layout", exact: true }).click();
  await expect(page.getByRole("status")).toContainText("Changes saved");
  await page.goto("/work");
  await expect(page.locator('[data-section]').first()).toHaveAttribute("data-section", "Custom fields");
});
