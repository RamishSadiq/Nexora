import { test, expect, type Page } from "@playwright/test";
import { submitLogin } from "./browser-login";

async function login(page: Page) {
  await page.goto("/login");
  await page.getByLabel("Work email").fill("admin@nexora.local");
  await page.getByLabel("Password", { exact: true }).fill(process.env.NEXORA_BROWSER_PASSWORD!);
  await submitLogin(page);
  await expect(page).not.toHaveURL(/\/login/);
}

test("shared entity headers sort the full filtered list before pagination and work on mobile", async ({ page }) => {
  await login(page);
  const products = Array.from({ length: 25 }, (_, index) => ({ id: `product-${index}`, name: `Product ${25 - index}`, category: index % 2 ? "Alpha" : "Zulu", termMonths: 25 - index, rate: index + 1, currency: "GBP", isActive: true }));
  await page.route("**/api/v1/membership/products", route => route.fulfill({ json: products }));
  await page.goto("/membership/products");
  const table = page.getByRole("table");
  const names = () => table.locator('tbody th[scope="row"]').allTextContents();
  await page.getByLabel("Rows per page", { exact: true }).selectOption("10");
  await page.getByRole("button", { name: "Next", exact: true }).click();
  await expect(page.getByText("Page 2 of 3", { exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Sort by Product ascending", exact: true }).click();
  await expect(table.getByRole("columnheader", { name: "Product", exact: true })).toHaveAttribute("aria-sort", "ascending");
  await expect.poll(names).toEqual(Array.from({ length: 10 }, (_, index) => `Product ${index + 1}`));
  await expect(page.getByText("Page 1 of 3", { exact: true })).toBeVisible();
  await page.getByLabel("Select Product 1", { exact: true }).check();
  await page.getByRole("button", { name: "Sort by Product descending", exact: true }).click();
  await expect.poll(names).toEqual(Array.from({ length: 10 }, (_, index) => `Product ${25 - index}`));
  await expect(page.getByText(/1 selected/)).toBeVisible();
  const term = page.getByRole("button", { name: "Sort by Term ascending", exact: true });
  await term.focus(); await page.keyboard.press("Enter");
  await expect.poll(names).toEqual(Array.from({ length: 10 }, (_, index) => `Product ${index + 1}`));
  await page.getByRole("searchbox", { name: "Search records", exact: true }).fill("Product 2");
  await page.getByRole("button", { name: "Search", exact: true }).click();
  await expect.poll(names).toEqual(["Product 2", "Product 20", "Product 21", "Product 22", "Product 23", "Product 24", "Product 25"]);
  await page.setViewportSize({ width: 390, height: 844 });
  await page.getByRole("button", { name: "Sort by Product ascending", exact: true }).click();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBeTruthy();
  await page.screenshot({ path: "../../artifacts/listing-sort-mobile.png", fullPage: true });
});

test("CRM headers use server sorting, retain URL state and keep filters when toggled", async ({ page }) => {
  await login(page);
  const seen: string[] = [];
  await page.route("**/api/v1/crm/records?**", route => {
    const url = new URL(route.request().url());
    const sort = url.searchParams.get("sort")!;
    seen.push(sort);
    const rows = [{ id: "a", name: "Zulu", category: "Alpha", status: "active", kind: "account" }, { id: "b", name: "Alpha", category: "Zulu", status: "active", kind: "account" }];
    const key = sort.replace(/^-/, "") as "name" | "category";
    rows.sort((a, b) => a[key].localeCompare(b[key]) * (sort.startsWith("-") ? -1 : 1));
    const size = Number(url.searchParams.get("pageSize"));
    const number = Number(url.searchParams.get("page"));
    return route.fulfill({ json: { items: rows.slice((number - 1) * size, number * size), total: 2, page: number, pageSize: size } });
  });
  await page.goto("/relationships/accounts?sort=name&search=Sort%20check&category=Review&page=2&pageSize=10");
  await expect(page.getByText("Loading relationships…", { exact: true })).toHaveCount(0);
  await page.getByRole("button", { name: "Sort by Category ascending", exact: true }).click();
  await expect(page).toHaveURL(/sort=category/);
  await expect.poll(() => page.getByRole("table").locator('tbody th[scope="row"]').allTextContents()).toEqual(["Zulu", "Alpha"]);
  expect(new URL(page.url()).searchParams.get("page")).toBe("1");
  expect(new URL(page.url()).searchParams.get("search")).toBe("Sort check");
  expect(new URL(page.url()).searchParams.get("category")).toBe("Review");
  await page.getByRole("button", { name: "Sort by Category descending", exact: true }).click();
  await expect(page).toHaveURL(/sort=-category/);
  await expect.poll(() => seen.at(-1)).toBe("-category");
  await page.reload();
  await expect(page.getByRole("columnheader", { name: "Category", exact: true })).toHaveAttribute("aria-sort", "descending");
  await expect.poll(() => page.getByRole("table").locator('tbody th[scope="row"]').allTextContents()).toEqual(["Alpha", "Zulu"]);
  await page.screenshot({ path: "../../artifacts/listing-sort-accounts.png", fullPage: true });
});

test("membership, setup and report headers submit or apply the selected sort", async ({ page }) => {
  await login(page);
  const sorts: string[] = [];
  await page.route("**/api/v1/membership/members?**", route => {
    const url = new URL(route.request().url()); sorts.push(url.searchParams.get("sort") ?? "name");
    return route.fulfill({ json: { items: [], totalCount: 0, page: 1, pageSize: 20 } });
  });
  await page.goto("/membership/members");
  await page.getByRole("button", { name: "Sort by Member descending", exact: true }).click();
  await expect.poll(() => sorts.at(-1)).toBe("-name");
  await page.getByRole("button", { name: "Sort by Product ascending", exact: true }).click();
  await expect.poll(() => sorts.at(-1)).toBe("product");
  await page.reload();
  await expect(page.getByRole("columnheader", { name: "Product", exact: true })).toHaveAttribute("aria-sort", "ascending");
  await page.route("**/api/v1/membership/setup/member-type", route => route.fulfill({ json: [{ id: "z", name: "Zulu", description: "Alpha description" }, { id: "a", name: "Alpha", description: "Zulu description" }] }));
  await page.goto("/membership/setup/member-type");
  await page.getByRole("button", { name: "Sort by Description ascending", exact: true }).click();
  await expect.poll(() => page.getByRole("table").locator("tbody tr td:first-child").allTextContents()).toEqual(["Zulu", "Alpha"]);
  const reportSorts: string[] = [];
  await page.route("**/api/v1/insights/query/*", route => {
    reportSorts.push(route.request().postDataJSON().sort);
    return route.fulfill({ json: { items: [], total: 0 } });
  });
  await page.goto("/insights");
  await page.getByRole("button", { name: "Sort by Name ascending", exact: true }).click();
  await expect.poll(() => reportSorts.at(-1)).toBe("label");
  await page.getByRole("button", { name: "Sort by Name descending", exact: true }).click();
  await expect.poll(() => reportSorts.at(-1)).toBe("-label");
});
