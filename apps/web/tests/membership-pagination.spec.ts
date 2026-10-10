import { test, expect, type Page } from "@playwright/test";
import { submitLogin } from "./browser-login";

async function login(page: Page) {
  await page.goto("/login");
  await page.getByLabel("Work email").fill("admin@nexora.local");
  await page.getByLabel("Password", { exact: true }).fill(process.env.NEXORA_BROWSER_PASSWORD!);
  await submitLogin(page);
  await expect(page).not.toHaveURL(/\/login/);
}

test("membership pages preserve filters and recover after decisions and cancellation", async ({ page }) => {
  await login(page);
  const token = await (await page.request.get("/api/v1/auth/csrf")).json();
  async function post(path: string, data: object) {
    const response = await page.request.post(`/api/v1/${path}`, { data, headers: { "X-CSRF-TOKEN": token.token } });
    expect(response.ok(), await response.text()).toBeTruthy();
    return response.json();
  }
  const product = await post("membership/products", { name: "Pagination annual", category: "Standard", termMonths: 12, rate: 100, currency: "GBP", isActive: true });
  for (let i = 0; i < 11; i++) {
    const contact = await post("crm/records", { kind: "contact", name: `Paging person ${String(i).padStart(2,"0")}`, status: "active" });
    await post("membership/applications", { contactId: contact.id, productId: product.id, applicationType: "individual", termsAccepted: true, startsAtUtc: new Date().toISOString().slice(0,10) + "T00:00:00Z" });
  }
  await page.goto("/membership/applications");
  await page.getByLabel("Search applications", { exact: true }).fill("Paging person");
  await page.getByRole("button", { name: "Filter view", exact: true }).click();
  await page.getByRole("button", { name: "Filters", exact: true }).click();
  await page.getByRole("combobox", { name: "applications status", exact: true }).selectOption("submitted");
  await page.getByRole("combobox", { name: "applications page size", exact: true }).selectOption("10");
  await page.getByRole("button", { name: "Filter view", exact: true }).click();
  const applicationPages = page.getByRole("navigation", { name: "applications pages" });
  await expect(page.getByText("Showing 1–10 of 11", { exact: true })).toBeVisible();
  await applicationPages.getByRole("button", { name: "Next" }).click();
  await expect(page.getByText("Showing 11–11 of 11", { exact: true })).toBeVisible();
  await page.getByRole("button", { name: /^Review Paging person / }).click();
  await page.getByLabel("Decision reason", { exact: true }).fill("Accepted for browser check");
  await page.getByRole("button", { name: "Record decision", exact: true }).click();
  await expect(page.getByRole("button", { name: "Record decision", exact: true })).toHaveCount(0);
  await page.getByRole("button", { name: "Back to listing", exact: true }).click();
  await expect(page.getByText("Showing 1–10 of 10", { exact: true })).toBeVisible();
  await expect(applicationPages).toContainText("Page 1 of 1");
  const remaining = await (await page.request.get("/api/v1/membership/applications?search=Paging%20person&status=submitted&pageSize=100")).json();
  for (const application of remaining.items) await post(`membership/applications/${application.id}/decision`, { version: application.version, decision: "approve", reason: "Accepted" });
  await page.goto("/membership/members");
  await page.getByLabel("Search members", { exact: true }).fill("Paging person");
  await page.getByRole("button", { name: "Filter view", exact: true }).click();
  await page.getByRole("button", { name: "Filters", exact: true }).click();
  await page.getByRole("combobox", { name: "members status", exact: true }).selectOption("active");
  await page.getByRole("combobox", { name: "members page size", exact: true }).selectOption("10");
  await page.getByRole("button", { name: "Filter view", exact: true }).click();
  const memberPages = page.getByRole("navigation", { name: "members pages" });
  await expect(page.getByText("Showing 1–10 of 11", { exact: true })).toBeVisible();
  await memberPages.getByRole("button", { name: "Next" }).click();
  await expect(page.getByText("Showing 11–11 of 11", { exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Paging person 10", exact: true }).click();
  await page.getByRole("combobox", { name: "Membership action", exact: true }).selectOption("cancel");
  await page.getByLabel("Reason", { exact: true }).fill("Member request");
  await page.getByRole("button", { name: "Apply change", exact: true }).click();
  await expect(page.getByRole("combobox", { name: "Membership action", exact: true })).toHaveValue("reinstate");
  await page.getByRole("button", { name: "Back to listing", exact: true }).click();
  await expect(page.getByText("Showing 1–10 of 10", { exact: true })).toBeVisible();
  await expect(memberPages).toContainText("Page 1 of 1");
  await page.getByRole("button", { name: "Filter view", exact: true }).click();
  await page.getByRole("button", { name: "Filters", exact: true }).click();
  await expect(page.getByRole("combobox", { name: "members status", exact: true })).toHaveValue("active");
  await page.getByRole("button", { name: "Filter view", exact: true }).click();
  await page.getByLabel("Search members", { exact: true }).fill("No such person");
  await expect(page.getByText("Showing 0–0 of 0", { exact: true })).toBeVisible();
  await expect(memberPages.getByRole("button", { name: "Next" })).toBeDisabled();
});

test("late search responses cannot overwrite newer results and list errors can be retried", async ({ page }) => {
  await login(page);
  let releaseOld!: () => void;
  const oldGate = new Promise<void>(resolve => { releaseOld = resolve; });
  let oldStarted!: () => void;
  const started = new Promise<void>(resolve => { oldStarted = resolve; });
  let fail = true;
  await page.route("**/api/v1/membership/members?*", async route => {
    const url = new URL(route.request().url());
    const search = url.searchParams.get("search");
    if (search === "Old") { oldStarted(); await oldGate; }
    if (search === "Failure" && fail) {
      await route.fulfill({ status: 500, json: { title: "Temporary list failure" } }); return;
    }
    const name = search === "Old" ? "Old result" : "New result";
    await route.fulfill({ json: { items: [{ id: name, contactName: name, productId: "none", status: "active", endsAtUtc: "2027-01-01", rate: 100, currency: "GBP" }], totalCount: 1, page: 1, pageSize: 25 } });
  });
  await page.goto("/membership");
  const search = page.getByLabel("Search members", { exact: true });
  await search.fill("Old");
  await started;
  await search.fill("New");
  await expect(page.getByRole("button", { name: "New result", exact: true })).toBeVisible();
  const oldResponse = page.waitForResponse(response => response.url().includes("search=Old"));
  releaseOld();
  await oldResponse;
  await expect(page.getByRole("button", { name: "Old result", exact: true })).toHaveCount(0);
  await search.fill("Failure");
  await expect(page.locator("main").getByRole("alert")).toContainText("Temporary list failure");
  fail = false;
  await page.getByRole("button", { name: "Retry members", exact: true }).click();
  await expect(page.getByRole("button", { name: "New result", exact: true })).toBeVisible();
});
