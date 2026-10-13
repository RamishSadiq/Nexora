import { randomBytes } from "node:crypto";
import { test, expect } from "@playwright/test";
import { retryLogin } from "./browser-login";

test("member signup, personal membership application and reviewed company access", async ({ page, browser }) => {
  const memberContext = await browser.newContext(); const member = await memberContext.newPage();
  const email = `onboarding-${randomBytes(5).toString("hex")}@nexora.test`, password = `NewMember!${randomBytes(12).toString("hex")}`;
  async function send(path: string, data: unknown, method = "POST") {
    const csrf = await (await page.request.get("/api/v1/auth/csrf")).json();
    const response = await retryLogin(() => page.request.fetch(path, { method, data, headers: { "X-CSRF-TOKEN": csrf.token } }), path.endsWith("/login"));
    expect(response.ok(), await response.text()).toBeTruthy(); return response.status() === 204 ? null : response.json();
  }
  await send("/api/v1/auth/login", { email: "admin@nexora.local", password: process.env.NEXORA_BROWSER_PASSWORD, rememberMe: false });
  if (!(await (await page.request.get("/api/v1/content/site")).json()).site) await send("/api/v1/content/site", { name: "Onboarding association", tagline: "Join your community" });
  const original = await (await page.request.get("/api/v1/content/portal")).json();
  try {
    const account = await send("/api/v1/crm/records", { kind: "account", name: "Onboarding Company", status: "active" });
    const product = await send("/api/v1/membership/products", { name: "New Member Membership", category: "individual", termMonths: 12, rate: 0, currency: "GBP", isActive: true });
    await send(`/api/v1/membership/products/${product.id}/portal`, { version: product.version, enabled: true, terms: "New member terms." }, "PUT");
    await page.goto("/portal-management"); await page.getByRole("button", { name: "Portal settings", exact: true }).click();
    await page.getByLabel("Allow member self-registration", { exact: true }).check(); await page.getByLabel("Allow company access requests", { exact: true }).check();
    await page.getByLabel("Portal theme", { exact: true }).selectOption("neon");
    await page.getByRole("button", { name: "Save portal draft", exact: true }).click(); await expect(page.getByRole("status")).toContainText("Portal draft saved");
    await member.goto("/portal"); await expect(member.getByRole("link", { name: "Create an account", exact: true })).toHaveCount(0);
    await page.getByRole("button", { name: "Publish portal", exact: true }).click(); await expect(page.getByRole("status")).toContainText("Portal published");
    await member.reload(); await member.getByRole("link", { name: "Create an account", exact: true }).click();
    await expect(member.getByRole("heading", { name: "Create your account", exact: true })).toBeVisible(); await expect(member.locator("[data-theme]")).toHaveAttribute("data-theme", "neon");
    await member.setViewportSize({ width: 390, height: 844 }); expect(await member.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBeTruthy();
    await member.screenshot({ path: "../../portal-signup-mobile.png", fullPage: true });
    await member.getByLabel("First name", { exact: true }).fill("New"); await member.getByLabel("Last name", { exact: true }).fill("Member"); await member.getByLabel("Email", { exact: true }).fill(email);
    await member.getByLabel("Password", { exact: true }).fill(password); await member.getByLabel("Confirm password", { exact: true }).fill("Mismatch-password!");
    await member.getByRole("button", { name: "Create account", exact: true }).click(); await expect(member.getByRole("alert").filter({ hasText: "Passwords must match" })).toBeVisible();
    await member.getByLabel("Confirm password", { exact: true }).fill(password); await member.getByRole("button", { name: "Create account", exact: true }).click();
    await expect(member.getByRole("heading", { name: "Dashboard", exact: true })).toBeVisible();
    expect((await member.request.get("/api/v1/crm/records")).status()).toBe(401);
    await member.getByRole("navigation", { name: "Member navigation" }).getByRole("link", { name: "My applications", exact: true }).click();
    await member.getByRole("button", { name: "New application", exact: true }).click(); await member.getByLabel("Membership product").selectOption(product.id);
    await member.getByLabel("Application notes").fill("New member application."); await member.getByRole("button", { name: "Save and review draft" }).click();
    await member.getByLabel("I accept these membership terms.").check(); await member.getByRole("button", { name: "Submit application", exact: true }).click();
    await expect(member.getByText("Application submitted for review. You can track its status above.")).toBeVisible();
    await member.getByRole("navigation", { name: "Member navigation" }).getByRole("link", { name: "My organisations", exact: true }).click();
    await member.getByLabel("Company name", { exact: true }).fill("Onboarding Company"); await member.getByLabel("Your job title", { exact: true }).fill("Manager");
    await member.getByLabel("How are you connected to this company?", { exact: true }).fill("I manage our membership applications.\nPlease verify my role."); await member.getByRole("button", { name: "Request company access", exact: true }).click();
    await expect(member.getByText("Your request has been sent to the membership team for review.")).toBeVisible(); await expect(member.getByText("No organisation access has been granted.")).toBeVisible();
    expect((await member.request.get(`/api/v1/portal/organisations/${account.id}/profile`)).status()).toBe(404);
    await page.goto("/settings"); const reviews = page.getByRole("region", { name: "Review company access requests", exact: true });
    const request = reviews.getByRole("article").filter({ hasText: email }); await request.getByRole("button", { name: /Review request/ }).click();
    const decision = page.getByRole("group", { name: "Company access decision", exact: true }); await decision.getByRole("button", { name: "Search matching companies", exact: true }).click();
    await decision.getByLabel("Approve access to", { exact: true }).selectOption(account.id); await decision.getByLabel("Allow company membership applications", { exact: true }).check();
    await decision.getByLabel("Message to member", { exact: true }).fill("Your company relationship has been verified."); await decision.getByRole("button", { name: "Approve company access", exact: true }).click();
    await expect(reviews.getByRole("status")).toContainText("request approved"); await member.getByRole("button", { name: "Refresh requests", exact: true }).click();
    await expect(member.getByText("Membership team: Your company relationship has been verified.")).toBeVisible(); await member.getByRole("button", { name: "Refresh organisations", exact: true }).click();
    await member.getByLabel("Organisation", { exact: true }).selectOption(account.id); await expect(member.getByRole("heading", { name: "Onboarding Company", exact: true, level: 3 })).toBeVisible();
    await expect(member.getByRole("region", { name: "Organisation membership application", exact: true })).toBeVisible();
    const contacts = await (await page.request.get("/api/v1/crm/records?kind=contact&search=New%20Member")).json(); expect(contacts.items.some((c: { name: string }) => c.name === "New Member")).toBeTruthy();
    await member.reload(); await expect(member.getByText("Membership team: Your company relationship has been verified.")).toBeVisible();
  } finally {
    const current = await (await page.request.get("/api/v1/content/portal")).json(); const restored = await send("/api/v1/content/portal", { version: current.version, configuration: original.published }, "PUT");
    await send("/api/v1/content/portal/publish", { version: restored.version, configuration: original.published }); await memberContext.close();
  }
});
