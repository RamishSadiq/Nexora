import { test, expect, type Page } from "@playwright/test";
import { submitLogin } from "./browser-login";

async function login(page: Page) {
  await page.goto("/login");
  await page.getByLabel("Work email").fill("admin@nexora.local");
  await page.getByLabel("Password", { exact: true }).fill(process.env.NEXORA_BROWSER_PASSWORD!);
  await submitLogin(page);
  await expect(page).not.toHaveURL(/\/login/);
}

async function fixture(page: Page, kind: "contact" | "account", archived = false) {
  const id = "9e3b6200-0e1a-45d1-9ed3-77d6af5fe927";
  const name = kind === "contact" ? "Tag review contact" : "Tag review account";
  const record = { id, kind, name, status: "active", category: null, ownerUserId: null, ownerTeamId: null, isArchived: archived, version: id, updatedAtUtc: "2026-10-13T09:00:00Z", createdAtUtc: "2026-10-12T09:00:00Z" };
  const detail = { record, contactDetails: null, accountDetails: null, addresses: [], communications: [], relationships: [], notes: [], tags: [{ id: "existing-tag", name: "crm pilot" }], files: [], fields: [], profiles: [] };
  const state = { posts: 0, deletes: 0, failNext: false, hold: null as Promise<void> | null };
  await page.route("**/api/v1/crm/records?**", route => route.fulfill({ json: { items: [record], total: 1 } }));
  await page.route(`**/api/v1/crm/records/${id}**`, async route => {
    const request = route.request();
    const pathname = new URL(request.url()).pathname;
    if (request.method() === "POST" && pathname.endsWith("/tags")) {
      state.posts++;
      await state.hold;
      if (state.failNext) {
        state.failNext = false;
        await route.fulfill({ status: 500, json: { title: "Tag could not be saved. Please retry." } });
      } else {
        const tag = { id: `new-tag-${state.posts}`, name: request.postDataJSON().name.trim().toLowerCase() };
        detail.tags.push(tag);
        await route.fulfill({ json: { id: tag.id } });
      }
    } else if (request.method() === "DELETE" && pathname.includes("/tags/")) {
      state.deletes++;
      detail.tags = detail.tags.filter(tag => tag.id !== pathname.split("/").at(-1));
      await route.fulfill({ status: 204 });
    } else if (request.method() !== "GET") {
      throw new Error(`Unexpected fixture mutation: ${request.method()} ${pathname}`);
    } else if (pathname.endsWith(id)) {
      await route.fulfill({ json: detail });
    } else if (pathname.endsWith("/data-audit")) {
      await route.fulfill({ json: { total: 0, latestAtUtc: null, items: [] } });
    } else if (pathname.endsWith("/context")) {
      await route.fulfill({ json: [] });
    } else {
      await route.fulfill({ json: { items: [], total: 0 } });
    }
  });
  async function open(edit = true) {
    await page.goto(`/relationships/${kind === "contact" ? "contacts" : "accounts"}`);
    await page.getByRole("button", { name: `${edit ? "Edit" : "View"} ${name}`, exact: true }).click();
    await expect(page.getByRole("region", { name: "Tags", exact: true })).toBeVisible();
  }
  return { state, open };
}

for (const kind of ["contact", "account"] as const) {
  test(`${kind} tags use themed chips and a popup, with confirmed removal`, async ({ page }) => {
    await login(page);
    const { state, open } = await fixture(page, kind);
    await open();
    const tags = page.getByRole("region", { name: "Tags", exact: true });
    const newTag = tags.getByRole("button", { name: "New tag", exact: true });
    await expect(tags.getByRole("textbox")).toHaveCount(0);
    await expect(tags.getByRole("listitem")).toHaveCSS("background-color", "rgb(25, 41, 65)");
    await expect(tags.getByRole("listitem")).toHaveCSS("border-radius", "10px");
    await newTag.click();
    const dialog = page.getByRole("dialog", { name: "New tag", exact: true });
    const input = dialog.getByRole("textbox", { name: "Tag name", exact: true });
    await expect(input).toBeFocused();
    await expect(input).toHaveAttribute("maxlength", "80");
    await input.fill("   ");
    await dialog.getByRole("button", { name: "Add tag", exact: true }).click();
    await expect(dialog.getByRole("alert")).toHaveText("Enter a tag name.");
    expect(state.posts).toBe(0);
    await input.fill("  Partner  ");
    await dialog.getByRole("button", { name: "Add tag", exact: true }).click();
    await expect(dialog).toHaveCount(0);
    await expect(tags.getByText("partner", { exact: true })).toBeVisible();
    expect(state.posts).toBe(1);
    await expect(newTag).toBeFocused();
    const listBox = await tags.getByRole("list").boundingBox();
    const buttonBox = await newTag.boundingBox();
    expect(listBox!.y + listBox!.height).toBeLessThan(buttonBox!.y);
    await tags.getByRole("button", { name: "Remove partner", exact: true }).click();
    const confirmation = page.getByRole("dialog", { name: "Confirm change", exact: true });
    await confirmation.getByRole("button", { name: "Cancel", exact: true }).click();
    expect(state.deletes).toBe(0);
    await expect(tags.getByText("partner", { exact: true })).toBeVisible();
    await tags.getByRole("button", { name: "Remove partner", exact: true }).click();
    await confirmation.getByRole("button", { name: "Confirm", exact: true }).click();
    await expect(tags.getByText("partner", { exact: true })).toHaveCount(0);
    expect(state.deletes).toBe(1);
    await open(false);
    await expect(tags.getByText("crm pilot", { exact: true })).toBeVisible();
    await expect(tags.getByRole("button")).toHaveCount(0);
  });
}

test("tag popup retains failed values, prevents duplicate saves, and supports keyboard cancellation", async ({ page }) => {
  await login(page);
  const { state, open } = await fixture(page, "contact");
  await open();
  const newTag = page.getByRole("button", { name: "New tag", exact: true });
  await newTag.click();
  const dialog = page.getByRole("dialog", { name: "New tag", exact: true });
  const input = dialog.getByRole("textbox", { name: "Tag name", exact: true });
  await input.fill("Retry tag");
  state.failNext = true;
  await dialog.getByRole("button", { name: "Add tag", exact: true }).click();
  await expect(dialog.getByRole("alert")).toHaveText("Tag could not be saved. Please retry.");
  await expect(input).toHaveValue("Retry tag");
  await expect(input).toBeFocused();
  await expect(page.getByText("retry tag", { exact: true })).toHaveCount(0);
  let release!: () => void;
  state.hold = new Promise<void>(resolve => { release = resolve; });
  try {
    await dialog.getByRole("button", { name: "Add tag", exact: true }).click();
    await expect(dialog.getByRole("button", { name: "Adding…", exact: true })).toBeDisabled();
    await expect(dialog.getByRole("button", { name: "Cancel", exact: true })).toBeDisabled();
    await page.keyboard.press("Escape");
    await expect(dialog).toBeVisible();
    await dialog.locator("form").evaluate(form => (form as HTMLFormElement).requestSubmit());
    await expect.poll(() => state.posts).toBe(2);
  } finally { release(); }
  await expect(dialog).toHaveCount(0);
  await expect(page.getByText("retry tag", { exact: true })).toBeVisible();
  expect(state.posts).toBe(2);
  await newTag.click();
  await expect(input).toHaveValue("");
  await input.fill("Discard me");
  await page.keyboard.press("Escape");
  await expect(dialog).toHaveCount(0);
  await expect(newTag).toBeFocused();
  await newTag.click();
  await expect(input).toHaveValue("");
  await dialog.getByRole("button", { name: "Close new tag", exact: true }).click();
  await expect(dialog).toHaveCount(0);
  expect(state.posts).toBe(2);
});

test("tag popup fits mobile, isolates background focus, and archived records retain read-only tags", async ({ page }) => {
  await login(page);
  const { open } = await fixture(page, "account");
  await page.setViewportSize({ width: 390, height: 844 });
  await open();
  await page.getByRole("button", { name: "New tag", exact: true }).click();
  const dialog = page.getByRole("dialog", { name: "New tag", exact: true });
  const input = dialog.getByRole("textbox", { name: "Tag name", exact: true });
  await input.fill("Long tag ".repeat(8));
  await page.getByRole("button", { name: "Back to listing", exact: true }).evaluate(button => (button as HTMLButtonElement).focus());
  await expect(input).toBeFocused();
  for (let index = 0; index < 7; index++) {
    await page.keyboard.press("Tab");
    // Native dialogs may let Tab reach browser chrome, but the page behind remains inert.
    expect(await dialog.evaluate(element => element.contains(document.activeElement) || document.activeElement === document.body)).toBeTruthy();
  }
  const box = await dialog.boundingBox();
  expect(box!.x).toBeGreaterThanOrEqual(16);
  expect(box!.x + box!.width).toBeLessThanOrEqual(374);
  await dialog.getByRole("button", { name: "Add tag", exact: true }).click();
  await expect(dialog).toHaveCount(0);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBeTruthy();
  await fixture(page, "account", true).then(value => value.open());
  const tags = page.getByRole("region", { name: "Tags", exact: true });
  await expect(tags.getByText("crm pilot", { exact: true })).toBeVisible();
  await expect(tags.getByRole("button")).toHaveCount(0);
});
