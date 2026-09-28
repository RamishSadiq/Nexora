// Run from a cleared debug environment. Only processes started here are stopped.
import fs from "node:fs";
import path from "node:path";
import net from "node:net";
import { spawn } from "node:child_process";
import { fileURLToPath } from "node:url";
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const launch = JSON.parse(fs.readFileSync(path.join(root, ".vscode/launch.json")));
const tasks = JSON.parse(fs.readFileSync(path.join(root, ".vscode/tasks.json")));
const expand = value => value.replaceAll("${workspaceFolder}", root);
const children = new Set();
let apiPid;
function start(command, args, cwd, env, name) {
  const fd = fs.openSync(path.join(root, "artifacts", `launch-order-${name}.log`), "w");
  const child = spawn(command, args, { cwd, env: { ...process.env, ...env }, windowsHide: true, stdio: ["ignore", fd, fd] });
  fs.closeSync(fd); children.add(child); return child;
}
async function run(command, args) {
  await new Promise((resolve, reject) => {
    const child = spawn(command, args, { cwd: root, windowsHide: true, stdio: "inherit" });
    child.on("error", reject); child.on("exit", code => code === 0 ? resolve() : reject(new Error(`${command} exited ${code}`)));
  });
}
async function stop(child) {
  if (child.exitCode === null) await run("taskkill", ["/PID", String(child.pid), "/T", "/F"]);
  children.delete(child);
}
async function ready(url, child) {
  const end = Date.now() + 180000;
  while (Date.now() < end) {
    if (child?.exitCode != null) throw new Error(`Process exited before ${url}`);
    try { const response = await fetch(url, { signal: AbortSignal.timeout(45000) }); if (response.ok) return; } catch {}
    await new Promise(r => setTimeout(r, 1000));
  }
  throw new Error(`Readiness failed: ${url}`);
}
async function free(port) {
  await new Promise((resolve, reject) => { const s = net.createServer(); s.on("error", reject); s.listen(port, () => s.close(resolve)); });
}
const dash = launch.configurations.find(c => c.name === "Company Dash");
const dashUrl = launch.configurations.find(c => c.name === "Company Dash Edge").url;
try {
  for (const port of [5097, 3017, 3018, 3019, Number(new URL(dashUrl).port)]) await free(port);
  fs.mkdirSync(path.join(root, "artifacts"), { recursive: true });
  const company = start(dash.runtimeExecutable, [dash.program], dash.cwd, dash.env, "company");
  await ready(dashUrl, company); await ready(`${dashUrl}/api/session`, company);
  console.log("PASS: Company Dash frontend and own API start without Nexora.");
  for (const order of [["CMS", "Portal", "CRM"], ["Portal", "CRM", "CMS"], ["CRM", "CMS", "Portal"]]) {
    const web = [];
    for (const name of order) {
      const config = launch.configurations.find(c => c.name === `Nexora internal ${name} web`);
      const task = tasks.tasks.find(t => t.label === config.preLaunchTask);
      await run(task.command, task.args.map(expand));
      const currentPid = Number(fs.readFileSync(path.join(root, "artifacts/local-shared-api.pid"), "utf8").trim());
      if (apiPid && apiPid !== currentPid) throw new Error("A frontend replaced the shared API.");
      apiPid = currentPid;
      const child = start(config.runtimeExecutable, [expand(config.program), ...config.args], expand(config.cwd), config.env, name);
      web.push(child);
      const url = launch.configurations.find(c => c.name === `Nexora ${name} Edge`).url;
      await ready(url, child);
    }
    await Promise.all([ready("http://localhost:5097/health/ready"), ready(dashUrl, company), ...[3017, 3018, 3019].map(port => ready(`http://localhost:${port}/api/v1/auth/csrf`))]);
    console.log(`PASS: ${order.join(" -> ")} from a cold API; all four apps and proxied API calls coexist.`);
    for (const child of web) await stop(child);
    await ready("http://localhost:5097/health/ready");
    await run("taskkill", ["/PID", String(apiPid), "/T", "/F"]); apiPid = undefined;
  }
  // Exercise the separate tasks that VS Code starts concurrently in All applications.
  await Promise.all(["CRM", "Portal", "CMS"].map(async name => {
    const config = launch.configurations.find(c => c.name === `Nexora internal ${name} web`);
    const task = tasks.tasks.find(t => t.label === config.preLaunchTask);
    await run(task.command, task.args.map(expand));
  }));
  apiPid = Number(fs.readFileSync(path.join(root, "artifacts/local-shared-api.pid"), "utf8").trim());
  await ready("http://localhost:5097/health/ready");
  await stop(company);
  await ready("http://localhost:5097/health/ready");
  console.log("PASS: simultaneous API readiness tasks and independent Company Dash shutdown.");
} finally {
  for (const child of children) await stop(child).catch(() => {});
  if (apiPid) await run("taskkill", ["/PID", String(apiPid), "/T", "/F"]).catch(() => {});
}
