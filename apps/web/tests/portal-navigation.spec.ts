import { test, expect } from "@playwright/test";

test("member dashboard and menus show separate sections with persistent URLs", async ({ page }) => {
  test.skip(!process.env.NEXORA_PORTAL_PASSWORD, "Requires a provisioned local portal member.");
  await page.goto("/portal");
  await page.getByLabel("Email", { exact: true }).fill(process.env.NEXORA_PORTAL_EMAIL ?? "local.member@nexora.local");
  await page.getByLabel("Password", { exact: true }).fill(process.env.NEXORA_PORTAL_PASSWORD!);
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await expect(page.getByRole("heading", { name: "Dashboard", exact: true })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Active memberships", exact: true })).toBeVisible();
  await expect(page.getByLabel("Display name")).toHaveCount(0);
  await expect(page.getByRole("button", { name: "New application", exact: true })).toHaveCount(0);
  const nav = page.getByRole("navigation", { name: "Member navigation" });
  for (const [label, path, content] of [
    ["My memberships", "memberships", "Membership details"],
    ["My applications", "applications", "Apply for membership"],
    ["My organisations", "organisations", "My organisations"],
    ["My profile", "profile", "My profile"],
    ["Notifications", "notifications", "Notifications"],
  ]) {
    await nav.getByRole("link", { name: label, exact: true }).click();
    await expect(page).toHaveURL(new RegExp(`/portal/${path}$`));
    await expect(nav.getByRole("link", { name: label, exact: true })).toHaveAttribute("aria-current", "page");
    await expect(page.getByRole("heading", { name: content, level: 2, exact: true })).toBeVisible();
    await expect(page.getByRole("heading", { name: "Quick links" })).toHaveCount(0);
    if (path !== "profile") await expect(page.getByLabel("Display name")).toHaveCount(0);
    if (path !== "applications") await expect(page.getByRole("heading", { name: "Apply for membership", exact: true })).toHaveCount(0);
    await page.reload();
    await expect(nav.getByRole("link", { name: label, exact: true })).toHaveAttribute("aria-current", "page");
  }
  await page.goBack();
  await expect(page).toHaveURL(/\/portal\/profile$/);
  await nav.getByRole("link", { name: "Dashboard", exact: true }).click();
  await page.setViewportSize({ width: 390, height: 844 });
  await expect(page.getByRole("heading", { name: "Quick links" })).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBeTruthy();
  await page.screenshot({ path: "../../artifacts/portal-dashboard-mobile.png", fullPage: true });
  await page.setViewportSize({ width: 1440, height: 1000 });
  await page.screenshot({ path: "../../artifacts/portal-dashboard-desktop.png", fullPage: true });
  await page.getByRole("link", { name: /Update your profile/ }).click();
  await expect(page.getByLabel("Display name")).toBeVisible();
  await page.getByRole("button", { name: "Sign out", exact: true }).click();
  await page.reload();
  await expect(page.getByRole("heading", { name: "Sign in", exact: true })).toBeVisible();
  await expect(page.getByLabel("Display name")).toHaveCount(0);
});
