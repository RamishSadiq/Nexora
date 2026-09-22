import { spawn } from "node:child_process";
import { randomBytes, randomUUID } from "node:crypto";
import { fileURLToPath } from "node:url";
import path from "node:path";
import fs from "node:fs";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const database = `NexoraAutomationTest_${randomUUID().replaceAll("-", "")}`;
const connection = `Server=(localdb)\\MSSQLLocalDB;Database=${database};Integrated Security=true;TrustServerCertificate=true`;
const env = { ...process.env, ASPNETCORE_ENVIRONMENT: "Development", DOTNET_ENVIRONMENT: "Development",
  ConnectionStrings__Nexora: connection, NEXORA_CONNECTION_STRING: connection,
  Identity__SeedAdminPassword: `Nexora!${randomBytes(24).toString("hex")}`, Identity__Provider__LocalDevelopmentEnabled: "true",
  NEXORA_API_URL: "http://localhost:5091", NEXORA_BROWSER_URL: "http://localhost:3011", Web__AllowedOrigins__0: "http://localhost:3011" };
env.NEXORA_BROWSER_PASSWORD = env.Identity__SeedAdminPassword;
function run(command, args, cwd = root) {
  return new Promise((resolve, reject) => {
    const process = spawn(command, args, { cwd, env, stdio: "inherit", windowsHide: true });
    process.on("error", reject); process.on("exit", code => code === 0 ? resolve() : reject(new Error(`${command} exited with ${code}`)));
  });
}
fs.mkdirSync(path.join(root, "artifacts/automation-browser"), { recursive: true });
function start(command, args, cwd, log) {
  const output = fs.openSync(path.join(root, "artifacts/automation-browser", log), "w");
  const child = spawn(command, args, { cwd, env, stdio: ["ignore", output, output], windowsHide: true });
  fs.closeSync(output); return child;
}
async function ready(url, child) {
  for (let i = 0; i < 120; i++) {
    if (child.exitCode !== null) throw new Error(`Server exited before ${url} was ready. See artifacts/automation-browser.`);
    try { if ((await fetch(url, { signal: AbortSignal.timeout(2000) })).ok) return; } catch {}
    await new Promise(resolve => setTimeout(resolve, 1000));
  }
  throw new Error(`Server did not become ready: ${url}`);
}
let api, web;
try {
  await run("dotnet", ["tool", "restore"]);
  await run("dotnet", ["build", "backend/Nexora.slnx", "--configuration", "Release"]);
  api = start("dotnet", ["bin/Release/net10.0/Nexora.Api.dll", "--urls", env.NEXORA_API_URL], path.join(root, "backend/src/Nexora.Api"), "api.log");
  await ready(`${env.NEXORA_API_URL}/health/ready`, api);
  web = start(process.execPath, ["node_modules/next/dist/bin/next", "dev", "--webpack", "--port", "3011"], path.join(root, "apps/web"), "web.log");
  await ready(`${env.NEXORA_BROWSER_URL}/login`, web);
  await run(process.execPath, ["node_modules/@playwright/test/cli.js", "test", ...process.argv.slice(2)], path.join(root, "apps/web"));
} catch (error) { console.error(error.message); process.exitCode = 1; }
finally {
  for (const child of [web, api]) if (child && child.exitCode === null) {
    if (process.platform === "win32") await run("taskkill", ["/PID", String(child.pid), "/T", "/F"]).catch(() => {});
    else child.kill();
  }
  // Only the uniquely named database created by this invocation can be dropped.
  if (!/^NexoraAutomationTest_[a-f0-9]{32}$/.test(database)) throw new Error("Unexpected test database name.");
  await run("dotnet", ["ef", "database", "drop", "--force", "--project", "backend/src/Modules/Nexora.Modules.Crm", "--startup-project", "backend/src/Nexora.Api", "--context", "CrmDbContext", "--configuration", "Release", "--no-build"]).catch(error => { console.error("Test database cleanup failed:", error.message); process.exitCode = 1; });
}
