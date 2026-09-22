import { defineConfig } from "@playwright/test";

export default defineConfig({
  testDir: "./tests",
  workers: 1,
  timeout: 180_000,
  expect: { timeout: 15_000 },
  outputDir: "../../artifacts/automation-browser-results",
  reporter: "list",
  use: {
    baseURL: process.env.NEXORA_BROWSER_URL ?? "http://localhost:3011",
    channel: process.env.NEXORA_BROWSER_CHANNEL ?? "msedge",
    headless: true,
    actionTimeout: 15_000,
    navigationTimeout: 60_000,
    screenshot: "only-on-failure",
    trace: "retain-on-failure",
  },
});
