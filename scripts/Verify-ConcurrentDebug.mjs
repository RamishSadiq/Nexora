// Verify the running Portal and CMS together using separate Edge contexts.
import fs from "node:fs";
import { createRequire } from "node:module";
const require = createRequire(new URL("../apps/web/package.json", import.meta.url));
const { chromium, expect } = require("@playwright/test");
const launch = JSON.parse(fs.readFileSync(new URL("../.vscode/launch.json", import.meta.url)));
const password = launch.configurations.find(c => c.name === "Nexora shared API").env.Identity__SeedAdminPassword;
const { publicPath } = JSON.parse(fs.readFileSync(new URL("../artifacts/local-test-data.json", import.meta.url)));
const browser = await chromium.launch({ channel: "msedge", headless: true });
try {
  const member = await browser.newPage();
  const cms = await browser.newPage();
  member.setDefaultNavigationTimeout(180000);
  cms.setDefaultNavigationTimeout(180000);
  await Promise.all([member.goto("http://localhost:3018/portal"), cms.goto(`http://localhost:3019${publicPath}`)]);
  await expect(cms.getByRole("heading", { level: 1 })).toBeVisible();
  await member.getByLabel("Email", { exact: true }).fill("local.member@nexora.local");
  await member.getByLabel("Password", { exact: true }).fill(password);
  await member.getByRole("button", { name: "Sign in", exact: true }).click();
  await expect(member.getByRole("heading", { name: "Dashboard", exact: true })).toBeVisible({ timeout: 60000 });
  for (let i = 0; i < 3; i++) {
    const [profile, website] = await Promise.all([member.request.get("http://localhost:3018/api/v1/portal/profile"), cms.reload()]);
    if (!profile.ok() || !website.ok()) throw new Error(`Concurrent request failure: portal=${profile.status()}, cms=${website.status()}`);
  }
  await member.screenshot({ path: "artifacts/portal-login-verified.png", fullPage: true });
  console.log("PASS: Edge portal member login and profile proxy, alongside CMS reloads on ports 3018/3019.");
} finally { await browser.close(); }
