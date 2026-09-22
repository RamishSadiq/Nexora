import { test, expect, type Page } from "@playwright/test";

async function login(page: Page) {
  await page.goto("/login");
  await page.getByLabel("Work email").fill("admin@nexora.local");
  await page.getByLabel("Password", { exact: true }).fill(process.env.NEXORA_BROWSER_PASSWORD!);
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await expect(page).not.toHaveURL(/\/login/, { timeout: 60000 });
}

test("staff draft, preview and publishing are separated from the anonymous association website", async ({ page, browser }) => {
  // Compile the anonymous route before editing: dev compilation can reload other tabs.
  await page.goto("/site/00000000-0000-0000-0000-000000000000/home");
  await login(page); await page.goto("/website");
  await page.getByLabel("Association name").fill("Northstar Association");
  await page.getByLabel("Short tagline").fill("People. Purpose. Progress.");
  await page.getByRole("button", { name: "Create website", exact: true }).click();
  await page.getByRole("button", { name: "New page", exact: true }).click();
  await page.getByLabel("Page title", { exact: true }).fill("A community that moves us forward");
  await page.getByLabel("Heading", { exact: true }).fill("Better connected. Greater possibilities.");
  await page.getByRole("textbox", { name: "Text", exact: true }).fill("Connect with people who share your purpose. Exchange knowledge, build relationships, and shape what comes next for our profession.");
  await page.getByRole("button", { name: "Preview draft" }).click();
  await expect(page.getByRole("heading", { name: "Better connected. Greater possibilities." })).toBeVisible();
  await page.getByRole("button", { name: "Back to editor" }).click();
  await page.getByRole("button", { name: "Save draft", exact: true }).click();
  await expect(page.getByRole("status")).toHaveText("Draft saved. The published page is unchanged.");
  const state = await (await page.request.get("/api/v1/content/site")).json();
  const publicUrl = `/site/${state.site.id}/home`;
  const context = await browser.newContext(); const visitor = await context.newPage();
  await visitor.goto(new URL(publicUrl, page.url()).href);
  await expect(visitor.getByRole("heading", { name: "A community that moves us forward" })).toHaveCount(0);
  await expect(visitor).not.toHaveURL(/\/login/);
  page.on("dialog", dialog => dialog.accept());
  await page.getByRole("button", { name: "Publish saved draft" }).click();
  await expect(page.getByRole("status")).toHaveText("Saved version published.");
  await visitor.reload();
  await expect(visitor.getByRole("heading", { level: 1 })).toHaveText("A community that moves us forward");
  await expect(visitor).toHaveTitle("A community that moves us forward | Northstar Association");
  await visitor.screenshot({ path: process.env.NEXORA_WEBSITE_SCREENSHOT ?? "../../artifacts/association-website.png", fullPage: true });
  await visitor.setViewportSize({ width: 390, height: 844 });
  expect(await visitor.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBeTruthy();
  await page.getByLabel("Page title", { exact: true }).fill("Unpublished update");
  await page.getByRole("button", { name: "Save draft", exact: true }).click();
  await expect(page.getByRole("status")).toHaveText("Draft saved. The published page is unchanged.");
  await visitor.reload(); await expect(visitor.getByRole("heading", { level: 1 })).toHaveText("A community that moves us forward");
  await page.getByRole("button", { name: "Publish saved draft" }).click();
  await expect(page.getByRole("status")).toHaveText("Saved version published.");
  await visitor.reload(); await expect(visitor.getByRole("heading", { level: 1 })).toHaveText("Unpublished update");
  await page.getByRole("button", { name: "Unpublish page" }).click();
  await expect(page.getByRole("status")).toHaveText("Page unpublished.");
  await visitor.reload(); await expect(visitor.getByRole("heading", { name: "Unpublished update" })).toHaveCount(0);
  await context.close();
});
