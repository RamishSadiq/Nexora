import { randomBytes } from "node:crypto";
import { test, expect } from "@playwright/test";
import { submitLogin, retryLogin } from "./browser-login";

test("CRM portal draft publication configures the member experience and custom pages", async ({ page, browser }) => {
  const memberContext = await browser.newContext(); const member = await memberContext.newPage();
  const password = `Portal!${randomBytes(12).toString("hex")}`;
  async function send(path: string, data: unknown, method = "POST") {
    const csrf = await (await page.request.get("/api/v1/auth/csrf")).json();
    const response = await retryLogin(() => page.request.fetch(path, { method, data, headers: { "X-CSRF-TOKEN": csrf.token } }), path.endsWith("/login"));
    expect(response.ok(), await response.text()).toBeTruthy(); return response.json();
  }
  await send("/api/v1/auth/login", { email: "admin@nexora.local", password: process.env.NEXORA_BROWSER_PASSWORD, rememberMe: false });
  if (!(await (await page.request.get("/api/v1/content/site")).json()).site)
    await send("/api/v1/content/site", { name: "Configuration association", tagline: "Together" });
  const original = await (await page.request.get("/api/v1/content/portal")).json();
  try {
    const contact = await send("/api/v1/crm/records", { kind: "contact", name: "Portal Configuration Member", status: "active" });
    await send("/api/v1/admin/portal-access/local-member", { contactId: contact.id, email: "configuration@nexora.test", password });
    await member.goto("/portal");
    await member.getByLabel("Email", { exact: true }).fill("configuration@nexora.test");
    await member.getByLabel("Password", { exact: true }).fill(password);
    await submitLogin(member, "portal");
    await expect(member.getByRole("heading", { name: "Dashboard", exact: true })).toBeVisible();
    await page.goto("/portal-management");
    await page.getByRole("button", { name: "Portal settings", exact: true }).click();
    await page.getByLabel("Portal name", { exact: true }).fill("Association member hub");
    await page.getByLabel("Dashboard welcome text", { exact: true }).fill("Your association updates, in one place.");
    await page.getByLabel("Portal theme", { exact: true }).selectOption("neon");
    await page.getByLabel("Portal accent colour hex", { exact: true }).fill("#50f5d6");
    await page.getByLabel("Portal secondary accent colour hex", { exact: true }).fill("#9255ff");
    await page.getByLabel("Portal corner radius (px)", { exact: true }).fill("20");
    await page.getByLabel("Portal hover duration (ms)", { exact: true }).fill("350");
    await page.getByLabel("Menu label: dashboard", { exact: true }).fill("My dashboard");
    await page.getByLabel("Show notifications", { exact: true }).uncheck();
    await page.getByLabel("Quick links", { exact: true }).uncheck();
    await page.getByRole("button", { name: "Back to listing", exact: true }).click();
    await page.getByRole("button", { name: "Add member page" }).click();
    await page.getByLabel("Page address", { exact: true }).fill("member-guide");
    await page.getByLabel("Page title", { exact: true }).fill("Your member guide");
    await page.getByLabel("Page content", { exact: true }).fill("Member information maintained by your association.\n<script>window.portalInjection = true</script>");
    await page.getByRole("button", { name: "Back to listing", exact: true }).click();
    await page.getByRole("button", { name: "Portal settings", exact: true }).click();
    await page.getByLabel("Menu label: member-guide", { exact: true }).fill("Member guide");
    await page.getByRole("button", { name: "Preview draft" }).click();
    await expect(page.getByRole("region", { name: "Portal draft preview" })).toContainText("Your member guide");
    await expect(page.getByRole("region", { name: "Portal draft preview" })).toHaveAttribute("data-theme", "neon");
    await page.getByRole("button", { name: "Save portal draft", exact: true }).click();
    await expect(page.getByRole("status")).toContainText("Portal draft saved");
    await member.reload();
    await expect(member.getByRole("heading", { name: "Dashboard", exact: true })).toBeVisible();
    await expect(member.locator("[data-theme]")).toHaveAttribute("data-theme", "classic");
    await expect(member.getByRole("link", { name: "Member guide", exact: true })).toHaveCount(0);
    await page.reload();
    await page.getByRole("button", { name: "Portal settings", exact: true }).click();
    await expect(page.getByLabel("Portal name", { exact: true })).toHaveValue("Association member hub");
    await expect(page.getByLabel("Portal theme", { exact: true })).toHaveValue("neon");
    await expect(page.getByLabel("Portal accent colour hex", { exact: true })).toHaveValue("#50f5d6");
    await page.getByRole("button", { name: "Publish portal", exact: true }).click();
    await expect(page.getByRole("status")).toContainText("Portal published");
    await member.reload();
    await expect(member.getByRole("heading", { name: "My dashboard", exact: true })).toBeVisible();
    await expect(member.locator("[data-theme]")).toHaveAttribute("data-theme", "neon");
    await expect(member.locator("[data-theme]")).toHaveCSS("color", "rgb(250, 250, 250)");
    await expect(member.locator(".portal-summary-card").first()).toHaveCSS("border-radius", "20px");
    await member.setViewportSize({ width: 1440, height: 1000 });
    expect(await member.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBeTruthy();
    await member.screenshot({ path: "../../portal-neon-dashboard.png", fullPage: true });
    await member.emulateMedia({ reducedMotion: "reduce" });
    await expect(member.locator(".portal-summary-card").first()).toHaveCSS("transition-duration", "0s");
    await member.emulateMedia({ reducedMotion: "no-preference" });
    const appearance = await (await memberContext.request.get("/api/v1/public/portal-appearance")).json();
    expect(appearance.design.theme).toBe("neon");
    expect(Object.keys(appearance).sort()).toEqual(["brandColor", "design", "name", "tagline"]);
    await expect(member.getByText("Welcome back, Portal Configuration Member. Your association updates, in one place.")).toBeVisible();
    await expect(member.getByRole("heading", { name: "Quick links" })).toHaveCount(0);
    await expect(member.getByRole("navigation").getByRole("link", { name: "Notifications", exact: true })).toHaveCount(0);
    // Delayed branding/auth responses must not replace the established shell with
    // the classic loading screen when moving between long and short member pages.
    const brandingReads = /\/api\/v1\/(?:portal\/(?:profile|configuration)|public\/portal-appearance)$/;
    let releaseReads!: () => void;
    const pendingReads = new Promise<void>(resolve => { releaseReads = resolve; });
    const resumedReads: Promise<void>[] = [];
    await member.route(brandingReads, route => {
      const resumed = pendingReads.then(() => route.continue());
      resumedReads.push(resumed);
      return resumed;
    });
    const shell = await member.locator("[data-theme]").elementHandle();
    try {
      for (const [label, heading] of [["My profile", "My profile"], ["My memberships", "My memberships"], ["My applications", "My applications"], ["My organisations", "My organisations"], ["My dashboard", "My dashboard"], ["Member guide", "Your member guide"]]) {
        const frames = member.evaluate(async () => {
          const unexpected: string[] = [];
          for (let frame = 0; frame < 30; frame++) {
            await new Promise(requestAnimationFrame);
            const root = document.querySelector("[data-theme]");
            if (root?.getAttribute("data-theme") !== "neon" || getComputedStyle(root).backgroundColor !== "rgb(9, 9, 11)") unexpected.push(`Frame ${frame}: ${root?.getAttribute("data-theme") ?? "missing"}`);
          }
          return unexpected;
        });
        await member.getByRole("navigation", { name: "Member navigation" }).getByRole("link", { name: label, exact: true }).click();
        await expect(member.getByRole("heading", { name: heading, exact: true }).first()).toBeVisible();
        await expect(member.getByRole("navigation", { name: "Member navigation" })).toBeVisible();
        await expect(member.getByText("Loading your portal…", { exact: true })).toHaveCount(0);
        expect(await shell!.evaluate(node => node.isConnected)).toBeTruthy();
        expect(await frames).toEqual([]);
        if (label === "My profile") await member.getByRole("button", { name: "Save profile", exact: true }).scrollIntoViewIfNeeded();
      }
    } finally {
      releaseReads();
      await Promise.all(resumedReads);
      await member.unroute(brandingReads);
    }
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
    await page.getByRole("button", { name: "Back to listing", exact: true }).click();
    await page.getByRole("button", { name: "Edit Your member guide", exact: true }).click();
    await page.getByRole("button", { name: "Remove page", exact: true }).click();
    await page.getByRole("button", { name: "Portal settings", exact: true }).click();
    await page.getByRole("button", { name: "Save portal draft", exact: true }).click();
    await expect(page.getByRole("status")).toContainText("Portal draft saved");
    await page.getByRole("button", { name: "Publish portal", exact: true }).click();
    await expect(page.getByRole("status")).toContainText("Portal published");
    await member.reload();
    await expect(member.getByText("This page is not currently available. Choose a section from the menu.")).toBeVisible();
    // A response authorized before sign-out must not restore the member shell
    // when it finally arrives after sign-out.
    const authReads = /\/api\/v1\/portal\/(?:profile|configuration)$/;
    let releaseStale!: () => void;
    let readsCaptured!: () => void;
    let captured = 0;
    const holdStale = new Promise<void>(resolve => { releaseStale = resolve; });
    const capturedReads = new Promise<void>(resolve => { readsCaptured = resolve; });
    const deliveries: Promise<void>[] = [];
    await member.route(authReads, route => {
      const delivery = (async () => {
        const response = await route.fetch();
        expect(response.ok()).toBeTruthy();
        if (++captured === 2) readsCaptured();
        await holdStale;
        await route.fulfill({ response });
      })();
      deliveries.push(delivery);
      return delivery;
    });
    try {
      await member.getByRole("navigation", { name: "Member navigation" }).getByRole("link", { name: "My dashboard", exact: true }).click();
      await capturedReads;
      await member.getByRole("button", { name: "Sign out", exact: true }).click();
      await expect(member.getByRole("heading", { name: "Sign in", exact: true })).toBeVisible();
    } finally {
      releaseStale();
      await Promise.all(deliveries);
      await member.unroute(authReads);
    }
    expect(await member.evaluate(async () => {
      let memberShellReappeared = false;
      for (let frame = 0; frame < 30; frame++) {
        await new Promise(requestAnimationFrame);
        memberShellReappeared ||= !!document.querySelector("nav[aria-label='Member navigation']");
      }
      return memberShellReappeared;
    })).toBeFalsy();
    await expect(member.getByRole("heading", { name: "Sign in", exact: true })).toBeVisible();
    await expect(member.getByText(/Member information maintained/)).toHaveCount(0);

  } finally {
    const current = await (await page.request.get("/api/v1/content/portal")).json();
    const saved = await send("/api/v1/content/portal", { version: current.version, configuration: original.published }, "PUT");
    await send("/api/v1/content/portal/publish", { version: saved.version, configuration: original.published });
    await memberContext.close();
  }
});
