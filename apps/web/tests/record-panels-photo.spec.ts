import { test, expect, type Page } from "@playwright/test";
import { submitLogin } from "./browser-login";

const png = Buffer.from("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jS1sAAAAASUVORK5CYII=", "base64");

async function login(page: Page) {
  await page.goto("/login");
  await page.getByLabel("Work email").fill("admin@nexora.local");
  await page.getByLabel("Password", { exact: true }).fill(process.env.NEXORA_BROWSER_PASSWORD!);
  await submitLogin(page);
  await expect(page).not.toHaveURL(/\/login/);
}

async function fixture(page: Page, kind: "contact" | "account") {
  const id = "8a1b4c15-3cbb-43f6-a195-fb3db4df9230";
  const name = `Panel review ${kind}`;
  const record = { id, kind, name, status: "active", category: null, ownerUserId: null, ownerTeamId: null, isArchived: false, version: id, photoFileId: null as string | null, updatedAtUtc: "2026-10-13T09:00:00Z", createdAtUtc: "2026-10-12T09:00:00Z" };
  const detail = { record, contactDetails: null, accountDetails: null, addresses: [], communications: [], relationships: [], notes: [], tags: [], files: [], fields: [], profiles: [] };
  const state = { posts: 0, unavailable: false };
  await page.route("**/api/v1/crm/records?**", route => route.fulfill({ json: { items: [record], total: 1 } }));
  await page.route(`**/api/v1/crm/records/${id}**`, async route => {
    const request = route.request();
    const path = new URL(request.url()).pathname;
    if (request.method() === "POST" && path.endsWith("/photo")) {
      state.posts++;
      expect(request.headers()["content-type"]).toContain("multipart/form-data");
      expect(request.headers()["x-csrf-token"]).toBeTruthy();
      expect(request.postDataBuffer()!.toString()).toContain(record.version);
      if (state.unavailable) {
        await route.fulfill({ status: 503, json: { title: "Image scanning is temporarily unavailable. Please try again shortly." } });
      } else {
        record.photoFileId = `photo-${state.posts}`;
        record.version = "02591b6b-3a25-49d7-a93b-19b073f55948";
        await route.fulfill({ json: { id: record.photoFileId } });
      }
    } else if (request.method() !== "GET") {
      throw new Error(`Unexpected fixture mutation: ${request.method()} ${path}`);
    } else if (path.endsWith("/photo")) {
      await route.fulfill({ contentType: "image/png", body: png });
    } else if (path.endsWith(id)) {
      await route.fulfill({ json: detail });
    } else if (path.endsWith("/data-audit")) {
      await route.fulfill({ json: { total: 0, latestAtUtc: null, items: [] } });
    } else if (path.endsWith("/context")) {
      await route.fulfill({ json: [] });
    } else {
      await route.fulfill({ json: { items: [], total: 0 } });
    }
  });
  async function open() {
    await page.goto(`/relationships/${kind === "contact" ? "contacts" : "accounts"}`);
    await page.getByRole("button", { name: `Edit ${name}`, exact: true }).click();
    await expect(page.getByRole("region", { name: "Record audit", exact: true })).toBeVisible();
  }
  return { state, open, name };
}

for (const kind of ["contact", "account"] as const) {
  test(`${kind} panel arrows widen the form, preserve values, and remember the layout`, async ({ page }) => {
    await page.setViewportSize({ width: 1304, height: 884 });
    await login(page);
    const { open } = await fixture(page, kind);
    await open();
    const home = page.getByRole("navigation", { name: "Primary navigation", exact: true }).getByRole("link", { name: "Home", exact: true });
    await expect(home).toBeInViewport();
    const form = page.getByRole("region", { name: kind === "contact" ? "Personal Details" : "Organisation details", exact: true });
    const input = form.getByLabel(kind === "contact" ? "First name" : "Full organisation name", { exact: true });
    await input.fill("Keep this unsaved value");
    const originalWidth = (await form.boundingBox())!.width;
    const audit = page.getByRole("region", { name: "Record audit", exact: true });
    const rail = page.getByRole("complementary", { name: "Record tags", exact: true });
    await expect(audit).toHaveCSS("background-image", /radial-gradient/);
    await expect(audit.getByRole("button", { name: /Data Audit/ })).toHaveAttribute("aria-expanded", "false");
    await page.getByRole("button", { name: "Hide record side panel", exact: true }).click();
    await expect(rail).toBeHidden();
    await expect(audit).toBeHidden();
    await expect.poll(async () => (await form.boundingBox())!.width).toBeGreaterThan(originalWidth + 200);
    await expect(input).toHaveValue("Keep this unsaved value");
    const withRailHidden = (await form.boundingBox())!.width;
    await page.getByRole("button", { name: "Hide navigation", exact: true }).click();
    await expect(page.locator("#workspace-navigation")).toBeHidden();
    await expect.poll(async () => (await form.boundingBox())!.width).toBeGreaterThan(withRailHidden + 200);
    const showRail = page.getByRole("button", { name: "Show record side panel", exact: true });
    await page.keyboard.press("Tab");
    await showRail.focus();
    await page.keyboard.press("Enter");
    await expect(rail).toBeVisible();
    await expect(audit).toBeVisible();
    await expect(input).toHaveValue("Keep this unsaved value");
    expect(Math.abs((await audit.boundingBox())!.x - (await rail.boundingBox())!.x)).toBeLessThan(2);
    await page.getByRole("button", { name: "Hide record side panel", exact: true }).click();
    await page.reload();
    await expect(page.getByRole("button", { name: "Show record side panel", exact: true })).toBeVisible();
    await expect(page.getByRole("button", { name: "Show navigation", exact: true })).toBeVisible();
    await page.getByRole("button", { name: "Show navigation", exact: true }).click();
    await expect(home).toBeInViewport();
    await page.getByRole("button", { name: "Show record side panel", exact: true }).click();
    await expect(rail).toBeVisible();
    expect(Math.abs((await audit.boundingBox())!.x - (await rail.boundingBox())!.x)).toBeLessThan(2);
  });

  test(`${kind} photo uploads display immediately and scanner outages can be retried`, async ({ page }) => {
    await login(page);
    const { state, open, name } = await fixture(page, kind);
    await open();
    if (kind === "contact") await page.getByRole("button", { name: "General Information", exact: true }).click();
    const input = page.getByLabel(kind === "contact" ? "Contact image" : "Organisation image", { exact: true });
    const uploadError = page.getByRole("region", { name: "Selected record", exact: true }).getByRole("alert");
    await expect(input).toBeEnabled();
    await expect(page.getByText(/Any image dimensions/)).toBeVisible();
    state.unavailable = true;
    await input.setInputFiles({ name: "profile.png", mimeType: "image/png", buffer: png });
    await expect(uploadError).toHaveText("Image scanning is temporarily unavailable. Please try again shortly.");
    await expect(input).toBeEnabled();
    await expect(page.getByRole("img", { name: `${name} profile`, exact: true })).toHaveCount(0);
    state.unavailable = false;
    await input.setInputFiles({ name: "profile.png", mimeType: "", buffer: png });
    const avatar = page.getByRole("img", { name: `${name} profile`, exact: true });
    await expect(avatar).toBeVisible();
    await expect.poll(() => avatar.evaluate(image => (image as HTMLImageElement).naturalWidth)).toBe(1);
    await expect(input).toHaveValue("");
    expect(state.posts).toBe(2);
    if (kind === "contact") await page.getByRole("button", { name: "General Information", exact: true }).click();
    await input.setInputFiles({ name: "too-large.png", mimeType: "image/png", buffer: Buffer.alloc(5 * 1024 * 1024 + 1) });
    await expect(uploadError).toHaveText("Choose an image up to 5 MB.");
    expect(state.posts).toBe(2);
  });
}

test("audit expansion and panel controls fit mobile while desktop navigation stays hidden", async ({ page }) => {
  await login(page);
  await page.setViewportSize({ width: 390, height: 844 });
  const { open } = await fixture(page, "contact");
  await open();
  const audit = page.getByRole("region", { name: "Record audit", exact: true });
  await audit.getByRole("button", { name: /Data Audit/ }).click();
  await expect(audit.getByLabel("From", { exact: true })).toBeVisible();
  await expect(audit.getByLabel("Through", { exact: true })).toBeVisible();
  const box = (await audit.boundingBox())!;
  expect(box.x).toBeGreaterThanOrEqual(16);
  expect(box.x + box.width).toBeLessThanOrEqual(374);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBeTruthy();
  await page.getByRole("button", { name: "Hide record side panel", exact: true }).click();
  await expect(audit).toBeHidden();
  await page.getByRole("button", { name: "Show record side panel", exact: true }).click();
  await expect(audit.getByLabel("From", { exact: true })).toBeVisible();
  await expect(page.getByRole("button", { name: "Hide navigation", exact: true })).toBeHidden();
  await page.getByRole("button", { name: "Open navigation", exact: true }).click();
  const home = page.getByRole("navigation", { name: "Primary navigation", exact: true }).getByRole("link", { name: "Home", exact: true });
  await expect(home).toBeInViewport();
  await page.getByRole("button", { name: "Close navigation", exact: true }).click();
  await expect(home).not.toBeInViewport();
});

test("panel controls still work when browser storage rejects preference writes", async ({ page }) => {
  await login(page);
  await page.addInitScript(() => { Storage.prototype.setItem = () => { throw new DOMException("Storage unavailable", "QuotaExceededError"); }; });
  const { open } = await fixture(page, "contact");
  await open();
  await page.getByRole("button", { name: "Hide record side panel", exact: true }).click();
  await expect(page.getByRole("complementary", { name: "Record tags", exact: true })).toBeHidden();
  await page.getByRole("button", { name: "Show record side panel", exact: true }).click();
  await expect(page.getByRole("complementary", { name: "Record tags", exact: true })).toBeVisible();
});

test("designer side panels collapse independently without losing edited node settings", async ({ page }) => {
  await login(page);
  await page.setViewportSize({ width: 1440, height: 1000 });
  await page.route("**/api/v1/workflows/**", route => {
    if (route.request().method() !== "GET") throw new Error("Designer fixture must not save data.");
    const path = new URL(route.request().url()).pathname;
    return route.fulfill({ json: path.endsWith("/metadata") ? { entities: [{ key: "crm.contact", title: "Contacts", fields: [] }], actions: [], triggers: ["manual"], nodeKinds: ["end"], operators: [], permissions: ["workflows.read", "workflows.create"] } : [] });
  });
  await page.goto("/workflow-designer");
  const tools = page.getByRole("heading", { name: "Designer tools", exact: true });
  const settings = page.getByRole("heading", { name: "Node settings", exact: true });
  const label = page.getByLabel("Label", { exact: true });
  await expect(label).toBeEnabled();
  await label.fill("Keep edited node label");
  const canvas = page.getByLabel("Saved WFL", { exact: true }).locator("xpath=ancestor::section[1]");
  const before = (await canvas.boundingBox())!.width;
  await page.getByRole("button", { name: "Hide designer tools", exact: true }).click();
  await expect(tools).toBeHidden();
  await expect(settings).toBeVisible();
  await expect.poll(async () => (await canvas.boundingBox())!.width).toBeGreaterThan(before + 180);
  await page.getByRole("button", { name: "Hide node settings", exact: true }).click();
  await expect(settings).toBeHidden();
  await expect.poll(async () => (await canvas.boundingBox())!.width).toBeGreaterThan(before + 500);
  await page.getByRole("button", { name: "Show node settings", exact: true }).click();
  await expect(label).toHaveValue("Keep edited node label");
  await page.getByRole("button", { name: "Show designer tools", exact: true }).click();
  await expect(tools).toBeVisible();
  await expect.poll(async () => (await canvas.boundingBox())!.width).toBeCloseTo(before, 0);
});
