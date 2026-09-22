import { spawn } from "node:child_process";
import { randomBytes, randomUUID } from "node:crypto";
import { fileURLToPath } from "node:url";
import net from "node:net";
import path from "node:path";
import fs from "node:fs";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const id = randomUUID().replaceAll("-", "");
const database = `NexoraWebsiteTest_${id}`;
const artifacts = path.join(root, "artifacts", `website-${id}`);
const webRoot = path.join(artifacts, "web");
const apiPort = 5095, webPort = 3015;
const production = process.argv.includes("--production");
const portal = process.argv.includes("--portal");
const configuration = portal ? "PortalTest" : "Website";
// Override the server only; the runner always owns a uniquely named disposable catalog.
const sqlServer = process.env.NEXORA_TEST_SQLSERVER_INSTANCE ?? "(localdb)\\MSSQLLocalDB";
if (!sqlServer || /[;\r\n]/.test(sqlServer)) throw new Error("Invalid test SQL Server instance.");
const connection = `Server=${sqlServer};Database=${database};Integrated Security=true;TrustServerCertificate=true`;
const env = { ...process.env, ASPNETCORE_ENVIRONMENT: "Development", DOTNET_ENVIRONMENT: "Development",
  ConnectionStrings__Nexora: connection, NEXORA_CONNECTION_STRING: connection,
  Identity__SeedAdminPassword: `Nexora!${randomBytes(24).toString("hex")}`, Identity__Provider__LocalDevelopmentEnabled: "true",
  NEXORA_API_URL: `http://localhost:${apiPort}`, NEXORA_BROWSER_URL: `http://localhost:${webPort}`, Web__AllowedOrigins__0: `http://localhost:${webPort}`,
  NEXORA_WEBSITE_SCREENSHOT: path.join(root, "artifacts", "association-website.png") };
env.NEXORA_BROWSER_PASSWORD = env.Identity__SeedAdminPassword;
function run(command, args, cwd = root) {
  return new Promise((resolve, reject) => {
    const child = spawn(command, args, { cwd, env, stdio: "inherit", windowsHide: true });
    child.on("error", reject); child.on("exit", code => code === 0 ? resolve() : reject(new Error(`${command} exited with ${code}`)));
  });
}
function free(port) {
  return new Promise((resolve, reject) => {
    const server = net.createServer(); server.once("error", () => reject(new Error(`Port ${port} is occupied. Existing processes were left alone.`)));
    server.listen(port, "127.0.0.1", () => server.close(resolve));
  });
}
function start(command, args, cwd, log) {
  const fd = fs.openSync(path.join(artifacts, log), "w");
  const child = spawn(command, args, { cwd, env, stdio: ["ignore", fd, fd], windowsHide: true });
  fs.closeSync(fd); return child;
}
async function ready(url, child) {
  const deadline = Date.now() + 180000;
  while (Date.now() < deadline) {
    if (child.exitCode !== null) throw new Error(`Server exited. See ${artifacts}`);
    try { if ((await fetch(url, { signal: AbortSignal.timeout(45000) })).ok) return; } catch { /* still starting */ }
    await new Promise(resolve => setTimeout(resolve, 1000));
  }
  throw new Error(`Server did not become ready: ${url}`);
}
let api, web;
try {
  await free(apiPort); await free(webPort);
  fs.mkdirSync(webRoot, { recursive: true });
  for (const name of ["src", "public", "tests", "package.json", "package-lock.json", "tsconfig.json", "next-env.d.ts", "next.config.ts", "postcss.config.mjs", "playwright.config.ts"]) {
    const source = path.join(root, "apps/web", name);
    if (fs.existsSync(source)) fs.cpSync(source, path.join(webRoot, name), { recursive: true });
  }
  fs.symlinkSync(path.join(root, "apps/web/node_modules"), path.join(webRoot, "node_modules"), process.platform === "win32" ? "junction" : "dir");
  await run("dotnet", ["tool", "restore"]);
  await run("dotnet", ["build", "backend/src/Nexora.Api", "--configuration", configuration]);
  api = start("dotnet", [`bin/${configuration}/net10.0/Nexora.Api.dll`, "--urls", env.NEXORA_API_URL], path.join(root, "backend/src/Nexora.Api"), "api.log");
  await ready(`${env.NEXORA_API_URL}/health/ready`, api);
  if (production) await run(process.execPath, ["node_modules/next/dist/bin/next", "build", "--webpack"], webRoot);
  web = start(process.execPath, ["node_modules/next/dist/bin/next", ...(production ? ["start"] : ["dev", "--webpack"]), "--port", String(webPort)], webRoot, "web.log");
  await ready(`${env.NEXORA_BROWSER_URL}/login`, web);
  await run(process.execPath, ["node_modules/@playwright/test/cli.js", "test", portal ? "portal.spec.ts" : "website.spec.ts", "--output", path.join(artifacts, "results")], webRoot);
} catch (error) { console.error(error.message); process.exitCode = 1; }
finally {
  for (const child of [web, api]) if (child && child.exitCode === null) {
    if (process.platform === "win32") await run("taskkill", ["/PID", String(child.pid), "/T", "/F"]).catch(() => {});
    else child.kill();
  }
  if (api) {
    if (!/^NexoraWebsiteTest_[a-f0-9]{32}$/.test(database)) throw new Error("Unexpected test database name.");
    await run("dotnet", ["ef", "database", "drop", "--force", "--project", "backend/src/Modules/Nexora.Modules.Content", "--context", "ContentDbContext", "--configuration", configuration, "--no-build"]).catch(error => { console.error(error.message); process.exitCode = 1; });
  }
}
