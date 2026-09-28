import { randomBytes } from "node:crypto";
import { test, expect } from "@playwright/test";

test("CRM portal draft publication configures the member experience and custom pages", async ({ page, browser }) => {
  const memberContext = await browser.newContext(); const member = await memberContext.newPage();
  const password = `Portal!${randomBytes(12).toString("hex")}`;
  async function send(path: string, data: unknown, method = "POST") {
    const csrf = await (await page.request.get("/api/v1/auth/csrf")).json();
    const response = await page.request.fetch(path, { method, data, headers: { "X-CSRF-TOKEN": csrf.token } });
    expect(response.ok(), await response.text()).toBeTruthy(); return response.json();
  }
  await send("/api/v1/auth/login", { email: "admin@nexora.local", password: process.env.NEXORA_BROWSER_PASSWORD, rememberMe: false });
  const original = await (await page.request.get("/api/v1/content/portal")).json();
  try {
    const contact = await send("/api/v1/crm/records", { kind: "contact", name: "Portal Configuration Member", status: "active" });
    await send("/api/v1/admin/portal-access/local-member", { contactId: contact.id, email: "configuration@nexora.test", password });
    await member.goto("/portal");
    await member.getByLabel("Email", { exact: true }).fill("configuration@nexora.test");
    await member.getByLabel("Password", { exact: true }).fill(password);
    await member.getByRole("button", { name: "Sign in", exact: true }).click();
    await expect(member.getByRole("heading", { name: "Dashboard", exact: true })).toBeVisible();
    await page.goto("/portal-management");
    await page.getByLabel("Portal name", { exact: true }).fill("Association member hub");
    await page.getByLabel("Dashboard welcome text", { exact: true }).fill("Your association updates, in one place.");
    await page.getByLabel("Menu label: dashboard", { exact: true }).fill("My dashboard");
    await page.getByLabel("Show notifications", { exact: true }).uncheck();
    await page.getByLabel("Quick links", { exact: true }).uncheck();
    await page.getByRole("button", { name: "Add member page" }).click();
    await page.getByLabel("Page address", { exact: true }).fill("member-guide");
    await page.getByLabel("Page title", { exact: true }).fill("Your member guide");
    await page.getByLabel("Page content", { exact: true }).fill("Member information maintained by your association.\n<script>window.portalInjection = true</script>");
    await page.getByLabel("Menu label: member-guide", { exact: true }).fill("Member guide");
    await page.getByRole("button", { name: "Preview draft" }).click();
    await expect(page.getByRole("region", { name: "Portal draft preview" })).toContainText("Your member guide");
    await page.getByRole("button", { name: "Save portal draft", exact: true }).click();
    await expect(page.getByRole("status")).toContainText("Portal draft saved");
    await member.reload();
    await expect(member.getByRole("heading", { name: "Dashboard", exact: true })).toBeVisible();
    await expect(member.getByRole("link", { name: "Member guide", exact: true })).toHaveCount(0);
    await page.reload();
    await expect(page.getByLabel("Portal name", { exact: true })).toHaveValue("Association member hub");
    await page.getByRole("button", { name: "Publish portal", exact: true }).click();
    await expect(page.getByRole("status")).toContainText("Portal published");
    await member.reload();
    await expect(member.getByRole("heading", { name: "My dashboard", exact: true })).toBeVisible();
    await expect(member.getByText("Welcome back, Portal Configuration Member. Your association updates, in one place.")).toBeVisible();
    await expect(member.getByRole("heading", { name: "Quick links" })).toHaveCount(0);
    await expect(member.getByRole("navigation").getByRole("link", { name: "Notifications", exact: true })).toHaveCount(0);
    await member.getByRole("link", { name: "Member guide", exact: true }).click();
    await expect(member).toHaveURL(/\/portal\/member-guide$/);
    await expect(member.getByRole("heading", { name: "Your member guide" })).toBeVisible();
    await expect(member.getByText(/Member information maintained/)).toBeVisible();
    expect(await member.evaluate(() => "portalInjection" in window)).toBeFalsy();
    await member.reload();
    await expect(member.getByRole("heading", { name: "Your member guide" })).toBeVisible();
    await member.setViewportSize({ width: 390, height: 844 });
    expect(await member.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBeTruthy();
    await member.screenshot({ path: "../../portal-custom-page-mobile.png", fullPage: true });
    page.on("dialog", dialog => dialog.accept());
    await page.getByRole("button", { name: "Remove page", exact: true }).click();
    await page.getByRole("button", { name: "Save portal draft", exact: true }).click();
    await expect(page.getByRole("status")).toContainText("Portal draft saved");
    await page.getByRole("button", { name: "Publish portal", exact: true }).click();
    await expect(page.getByRole("status")).toContainText("Portal published");
    await member.reload();
    await expect(member.getByText("This page is not currently available. Choose a section from the menu.")).toBeVisible();
    await member.getByRole("button", { name: "Sign out", exact: true }).click();
    await expect(member.getByRole("heading", { name: "Sign in", exact: true })).toBeVisible();
    await expect(member.getByText(/Member information maintained/)).toHaveCount(0);

  } finally {
    const current = await (await page.request.get("/api/v1/content/portal")).json();
    const saved = await send("/api/v1/content/portal", { version: current.version, configuration: original.published }, "PUT");
    await send("/api/v1/content/portal/publish", { version: saved.version, configuration: original.published });
    await memberContext.close();
  }
});
