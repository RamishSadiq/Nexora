import { submitLogin, retryLogin } from "./browser-login";
import { test, expect, type APIRequestContext } from "@playwright/test";
import { randomBytes } from "node:crypto";

// Test-Website owns the disposable database; never run against shared demo data.
test("restricted identity keeps CRM and Portal sessions independent", async ({ page, browser }) => {
  const operator = await browser.newContext({ baseURL: test.info().project.use.baseURL });
  async function send(request: APIRequestContext, path: string, data?: unknown, method = "POST") {
    const scheme = path.startsWith("/api/v1/portal/") ? "portal" : "auth";
    const csrfResponse = await request.get(`/api/v1/${scheme}/csrf`);
    expect(csrfResponse.ok()).toBeTruthy();
    const csrf = await csrfResponse.json();
    return request.fetch(path, { method, data, headers: { "X-CSRF-TOKEN": csrf.token } });
  }
  async function setup(path: string, data?: unknown, method = "POST") {
    const response = await retryLogin(() => send(operator.request, path, data, method), path.endsWith("/login"));
    expect(response.ok(), await response.text()).toBeTruthy();
    return response.status() === 204 ? null : response.json();
  }
  try {
    await setup("/api/v1/auth/login", { email: "admin@nexora.local", password: process.env.NEXORA_BROWSER_PASSWORD, rememberMe: false });
    const suffix = randomBytes(8).toString("hex");
    const email = `dual-${suffix}@nexora.test`;
    const password = `Reader!${randomBytes(16).toString("hex")}`;
    const group = await setup("/api/v1/admin/security-groups", { name: `CRM reader ${suffix}`, description: "Disposable dual access test", permissions: ["crm.read"] });
    const user = await setup("/api/v1/admin/users", { displayName: "Dual reader", email, password, isActive: true, groupIds: [group.id] });
    const contact = await setup("/api/v1/crm/records", { kind: "contact", name: `Dual contact ${suffix}`, status: "active" });
    const grant = await setup("/api/v1/admin/portal-access", { userId: user.id, contactId: contact.id });
    async function staffLogin() {
      await page.goto("/login");
      await page.getByLabel("Work email").fill(email);
      await page.getByLabel("Password", { exact: true }).fill(password);
      await submitLogin(page);
      await expect(page).not.toHaveURL(/\/login/);
    }
    async function portalLogin() {
      await page.goto("/portal");
      await page.getByLabel("Email", { exact: true }).fill(email);
      await page.getByLabel("Password", { exact: true }).fill(password);
      await submitLogin(page, "portal");
      await expect(page.getByRole("navigation", { name: "Member navigation" })).toBeVisible();
    }
    async function status(path: string, expected: number) {
      expect((await page.request.get(path)).status()).toBe(expected);
    }
    await staffLogin();
    await page.goto(`/relationships/contacts?record=${contact.id}`);
    await expect(page.getByText(contact.name, { exact: true }).first()).toBeVisible();
    await status("/api/v1/crm/records", 200);
    await status("/api/v1/portal/profile", 401);
    await portalLogin();
    await status("/api/v1/portal/profile", 200);
    await status("/api/v1/crm/records", 200);
    const cookies = await page.context().cookies();
    expect(cookies.some(cookie => cookie.name === "Nexora.Session")).toBeTruthy();
    expect(cookies.some(cookie => cookie.name === "Nexora.Portal")).toBeTruthy();
    expect((await send(page.request, "/api/v1/crm/records", { kind: "contact", name: "Denied", status: "active" })).status()).toBe(403);
    expect((await send(page.request, "/api/v1/portal/logout")).status()).toBe(204);
    await status("/api/v1/portal/profile", 401);
    await status("/api/v1/crm/records", 200);
    await portalLogin();
    expect((await send(page.request, "/api/v1/auth/logout")).status()).toBe(204);
    await status("/api/v1/crm/records", 401);
    await status("/api/v1/portal/profile", 200);
    await staffLogin();
    await setup(`/api/v1/admin/portal-access/${grant.id}`, undefined, "DELETE");
    await status("/api/v1/portal/profile", 401);
    await status("/api/v1/crm/records", 200);
    await setup("/api/v1/admin/portal-access", { userId: user.id, contactId: contact.id });
    await status("/api/v1/portal/profile", 401);
    expect((await send(page.request, "/api/v1/portal/logout")).status()).toBe(204);
    await portalLogin();
    await status("/api/v1/portal/profile", 200);
  } finally {
    await operator.close();
    // The runner disposes the entire database, including retained grant history.
  }
});
