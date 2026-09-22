import { test, expect, type Page } from "@playwright/test";

async function login(page: Page) {
  await page.goto("/login");
  await page.getByLabel("Work email").fill("admin@nexora.local");
  await page.getByLabel("Password", { exact: true }).fill(process.env.NEXORA_BROWSER_PASSWORD!);
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await expect(page).not.toHaveURL(/\/login/, { timeout: 60_000 });
}

async function setup(page: Page, suffix: string) {
  await login(page);
  const { token } = await (await page.request.get("/api/v1/auth/csrf")).json();
  const post = async (path: string, data: object) => {
    const response = await page.request.post(`/api/v1/${path}`, { data, headers: { "X-CSRF-TOKEN": token } });
    expect(response.ok(), await response.text()).toBeTruthy();
    return response.json();
  };
  const product = await post("membership/products", { name: `Journey annual ${suffix}`, category: "Corporate", termMonths: 12, rate: 240, currency: "GBP", isActive: true });
  const account = await post("crm/records", { kind: "account", name: `Journey organisation ${suffix}`, status: "active" });
  return { product, account, post };
}

for (const createContact of [false, true]) {
  test(`organisation application links ${createContact ? "a new" : "an existing"} contact and activates the reviewed membership`, async ({ page }) => {
    const suffix = createContact ? "new contact" : "existing contact";
    const { product, account, post } = await setup(page, suffix);
    const contactName = `Journey applicant ${suffix}`;
    const existingContact = createContact ? null : await post("crm/records", { kind: "contact", name: contactName, status: "active" });
    await page.goto("/membership");
    await page.getByRole("button", { name: "Applications", exact: true }).click();
    const form = page.locator("section").filter({ has: page.getByRole("heading", { name: "Submit membership application", exact: true }) });
    await form.getByLabel("Find organisation", { exact: true }).fill(account.name);
    await form.getByRole("button", { name: "Search organisations", exact: true }).click();
    await form.getByLabel("Organisation", { exact: true }).selectOption(account.id);
    if (createContact) {
      await form.getByLabel("New contact name", { exact: true }).fill(contactName);
      await form.getByRole("button", { name: "Create and select contact", exact: true }).click();
      await expect(form.getByLabel("Main contact", { exact: true }).locator("option:checked")).toHaveText(contactName);
    } else {
      await form.getByLabel("Find main contact", { exact: true }).fill(contactName);
      await form.getByRole("button", { name: "Search contacts", exact: true }).click();
      await form.getByLabel("Main contact", { exact: true }).selectOption(existingContact.id);
    }
    await form.getByRole("button", { name: "Link contact to organisation", exact: true }).click();
    await form.getByLabel("Membership product", { exact: true }).selectOption(product.id);
    await form.getByLabel("Requested start date", { exact: true }).fill(new Date().toISOString().slice(0, 10));
    await form.getByLabel("Applicant role", { exact: true }).fill("Membership manager");
    await form.getByLabel("Applicant notes", { exact: true }).fill("Insurance and organisation details checked by the applicant.");
    await form.getByRole("checkbox", { name: "I confirm the applicant has accepted the membership terms." }).check();
    if (!createContact) {
      await page.route("**/api/v1/membership/applications", async route => {
        if (route.request().method() === "POST") {
          await route.fulfill({ status: 503, json: { title: "Application service temporarily unavailable" } });
        } else await route.continue();
      }, { times: 1 });
      await form.getByRole("button", { name: "Submit application", exact: true }).click();
      await expect(page.locator("main").getByRole("alert")).toContainText("Application service temporarily unavailable");
      await expect(form.getByLabel("Organisation", { exact: true })).toHaveValue(account.id);
      await expect(form.getByLabel("Applicant notes", { exact: true })).toHaveValue("Insurance and organisation details checked by the applicant.");
    }
    await form.getByRole("button", { name: "Submit application", exact: true }).click();
    await expect(page.getByText("Application submitted for review.", { exact: true })).toBeVisible();
    await page.getByLabel("Search applications", { exact: true }).fill(contactName);
    const application = page.locator("li").filter({ hasText: contactName }).filter({ has: page.getByRole("button", { name: "Record decision", exact: true }) });
    await expect(application).toContainText(account.name);
    await expect(application).toContainText("240.00");
    await application.getByLabel("Decision reason", { exact: true }).fill("Organisation and contact reviewed; approved for membership.");
    await application.getByRole("button", { name: "Record decision", exact: true }).click();
    await expect(page.getByRole("button", { name: "Record decision", exact: true })).toHaveCount(0);
    await page.getByRole("button", { name: "Members", exact: true }).click();
    await page.getByLabel("Search members", { exact: true }).fill(account.name);
    await page.getByRole("button", { name: account.name, exact: true }).click();
    await expect(page.getByText("Organisation and contact reviewed; approved for membership.", { exact: true })).toBeVisible();
    const links = await (await page.request.get(`/api/v1/crm/records/${account.id}/account-contacts`)).json();
    expect(links.items).toHaveLength(1);
    expect(links.items[0].contactName).toBe(contactName);
    const members = await (await page.request.get(`/api/v1/membership/members?search=${encodeURIComponent(account.name)}`)).json();
    expect(members.items).toHaveLength(1);
    expect(members.items[0]).toMatchObject({ accountId: account.id, contactName, status: "active", rate: 240 });
  });
}
