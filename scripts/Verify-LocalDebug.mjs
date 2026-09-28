// Verify the actual VS Code source launch commands without taking over a user's debug session.
import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import net from "node:net";
import { fileURLToPath } from "node:url";
import { createRequire } from "node:module";
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const launch = JSON.parse(fs.readFileSync(path.join(root, ".vscode/launch.json"), "utf8"));
if (launch.compounds.map(x => x.name).join(",") !== "CRM,Portal,CMS") throw new Error("Expected CRM, Portal and CMS compounds.");
const require = createRequire(path.join(root, "apps/web/package.json")); const { chromium } = require("@playwright/test");
const config = launch.configurations.find(x => x.type === "coreclr");
const env = { ...process.env, ...config.env, NEXORA_API_URL: "http://localhost:5097", NEXORA_NEXT_DIST_DIR: ".next-local-debug" };
if (!config.args.includes("http://localhost:5097") || !config.env.ConnectionStrings__Nexora.includes("Database=NexoraDev;")) throw new Error("Unexpected local debug target.");
const children = []; let browser;
async function free(port) { await new Promise((resolve, reject) => { const server = net.createServer(); server.once("error", () => reject(new Error(`Port ${port} is occupied; no process was stopped.`))); server.listen(port, "127.0.0.1", () => server.close(resolve)); }); }
function start(command, args, cwd, name) { const fd = fs.openSync(path.join(root, `artifacts/local-${name}.log`), "w"); const p = spawn(command, args, { cwd, env, windowsHide: true, stdio: ["ignore", fd, fd] }); fs.closeSync(fd); children.push(p); return p; }
async function ready(url, child) { const end = Date.now() + 180000; while (Date.now() < end) { if (child.exitCode !== null) throw new Error(`Local process exited: ${url}`); try { if ((await fetch(url, { signal: AbortSignal.timeout(45000) })).ok) return; } catch {} await new Promise(r => setTimeout(r, 1000)); } throw new Error(`Not ready: ${url}`); }
async function run(command, args) { await new Promise((resolve, reject) => { const p = spawn(command, args, { cwd: root, env, windowsHide: true, stdio: "inherit" }); p.on("error", reject); p.on("exit", code => code === 0 ? resolve() : reject(new Error(`Verification helper exited ${code}`))); }); }
try {
  await free(5097); await free(3017);
  const api = start("dotnet", ["bin/LocalDebug/net10.0/Nexora.Api.dll", "--urls", "http://localhost:5097"], path.join(root,"backend/src/Nexora.Api"), "api"); await ready("http://localhost:5097/health/ready", api);
  await run(process.execPath, ["scripts/Prepare-LocalTestData.mjs"]);
  await run(process.execPath, ["scripts/Prepare-LocalTestData.mjs"]);
  const web = start(process.execPath, ["node_modules/next/dist/bin/next", "dev", "--webpack", "--port", "3017"], path.join(root,"apps/web"), "web"); await ready("http://localhost:3017/login", web);
  browser = await chromium.launch({ headless: true, channel: process.env.NEXORA_BROWSER_CHANNEL ?? "msedge" }); const page = await browser.newPage();
  await page.goto("http://localhost:3017/login"); await page.getByLabel("Work email").fill("admin@nexora.local"); await page.getByLabel("Password", { exact: true }).fill(config.env.Identity__SeedAdminPassword); await page.getByRole("button", { name: "Sign in", exact: true }).click(); await page.waitForURL(url => url.pathname !== "/login");
  for (const route of ["/relationships/contacts", "/website"]) { const response = await page.goto("http://localhost:3017" + route); if (!response.ok() || page.url().includes("/login")) throw new Error(`Staff route unavailable: ${route}`); }
  const member = await browser.newPage(); await member.goto("http://localhost:3017/portal"); await member.getByLabel("Email", { exact: true }).fill("local.member@nexora.local"); await member.getByLabel("Password", { exact: true }).fill(config.env.Identity__SeedAdminPassword); await member.getByRole("button", { name: "Sign in", exact: true }).click();
  await member.getByRole("heading", { name: "Dashboard", exact: true }).waitFor({ timeout: 60000 });
  const profile = await member.request.get("http://localhost:3017/api/v1/portal/profile"); if (!profile.ok()) throw new Error("Local member profile unavailable.");
  await run(process.execPath, ["scripts/Verify-PublicWebsite.mjs"]);
  console.log("Verified shared local CRM, Portal and CMS source routes, member sign-in, and idempotent demo setup. Debug ports will be released for F5.");
} finally {
  await browser?.close();
  for (const child of children.reverse()) if (child.exitCode === null) await run("taskkill", ["/PID", String(child.pid), "/T", "/F"]).catch(() => {});
}
