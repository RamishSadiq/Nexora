// Run against the configured local development servers; never changes published content.
import fs from "node:fs";
import path from "node:path";
import { createRequire } from "node:module";
import { fileURLToPath } from "node:url";
import { associationPages } from "./lib/association-website.mjs";
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const require = createRequire(path.join(root, "apps/web/package.json"));
const { chromium, expect } = require("@playwright/test");
const { siteId } = JSON.parse(fs.readFileSync(path.join(root, "artifacts/local-test-data.json"), "utf8"));
const origin = "http://localhost:3017";
const definitions = associationPages(siteId);
const browser = await chromium.launch({ headless: true, channel: process.env.NEXORA_BROWSER_CHANNEL ?? "msedge" });
try {
  const context = await browser.newContext(); // Anonymous visitor, separate from staff/member sessions.
  const page = await context.newPage();
  const failures = [];
  page.on("pageerror", error => failures.push(error.message));
  const destinations = new Set();
  for (const width of [1440, 390]) {
    await page.setViewportSize({ width, height: 960 });
    for (const definition of definitions) {
      const response = await page.goto(`${origin}/site/${siteId}/${definition.slug}`, { waitUntil: "networkidle" });
      if (!response?.ok()) throw new Error(`Public page unavailable: ${definition.slug}`);
      await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
      await expect(page.locator('meta[name="description"]')).toHaveAttribute("content", /.+/);
      await expect(page.locator(".site-portal-link")).toHaveAttribute("href", "/portal");
      if (definition.visible) {
        if (width < 760) await page.getByLabel("Open website menu", { exact: true }).click();
        await expect(page.getByRole("navigation", { name: width < 760 ? "Mobile website navigation" : "Website navigation" }).getByRole("link", { name: definition.label, exact: true })).toHaveAttribute("href", `/site/${siteId}/${definition.slug === "home" ? "home" : definition.slug}`);
        if (width < 760) await page.getByLabel("Open website menu", { exact: true }).click();
      }
      if (await page.evaluate(() => document.documentElement.scrollWidth > innerWidth)) throw new Error(`Horizontal overflow: ${definition.slug} at ${width}`);
      for (const href of await page.locator(".association-site a[href]").evaluateAll(links => links.map(a => a.getAttribute("href")))) if (href?.startsWith("/") && !href.startsWith("//")) destinations.add(href);
      if (["home", "membership", "resources"].includes(definition.slug)) await page.screenshot({ path: path.join(root, `artifacts/public-${definition.slug}-${width}.png`), fullPage: true });
    }
  }
  for (const href of destinations) {
    const response = await context.request.get(origin + href);
    if (!response.ok()) throw new Error(`Broken public link: ${href} (${response.status()})`);
  }
  await page.setViewportSize({ width: 1440, height: 960 });
  await page.goto(`${origin}/site/${siteId}/home`);
  await page.getByRole("navigation", { name: "Website navigation" }).getByRole("link", { name: "Membership", exact: true }).click();
  await expect(page).toHaveURL(new RegExp(`/site/${siteId}/membership$`));
  await page.getByRole("link", { name: "Read the individual guide" }).click();
  await expect(page).toHaveURL(new RegExp(`/site/${siteId}/member-guide$`));
  await page.locator(".site-portal-link").click();
  await expect(page.getByLabel("Email", { exact: true })).toBeVisible();
  const sitemap = await context.request.get(`${origin}/site/${siteId}/sitemap.xml`);
  if (!sitemap.ok()) throw new Error("Sitemap unavailable");
  const xml = await sitemap.text();
  for (const definition of definitions) if (!xml.includes(`/${definition.slug}</loc>`)) throw new Error(`Sitemap missing ${definition.slug}`);
  if (failures.length) throw new Error(failures.join("\n"));
  console.log(`Public website verified: ${definitions.length} anonymous pages at desktop/mobile widths, ${destinations.size} internal links, navigation-to-portal journey and sitemap. Screenshots saved in artifacts/public-*.png.`);
} finally { await browser.close(); }
