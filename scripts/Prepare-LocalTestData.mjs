import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { createRequire } from "node:module";
import { prepareAssociationWebsite } from "./lib/association-website.mjs";
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const require = createRequire(path.join(root, "apps/web/package.json"));
const { request } = require("@playwright/test");
const launch = JSON.parse(fs.readFileSync(path.join(root, ".vscode/launch.json"), "utf8"));
const api = launch.configurations.find(c => c.type === "coreclr");
if (!api.env.ConnectionStrings__Nexora.includes("Database=NexoraDev;") || !api.args.includes("http://localhost:5097")) throw new Error("This seed is restricted to the configured local NexoraDev debugger.");
const password = api.env.Identity__SeedAdminPassword;
const client = await request.newContext({ baseURL: "http://localhost:5097" });
async function get(url) { const r = await client.get(url); if (!r.ok()) throw new Error(`GET ${url}: ${r.status()}`); return r.json(); }
async function send(url, data, method = "POST") { const { token } = await get("/api/v1/auth/csrf"); const r = await client.fetch(url, { method, data, headers: { "X-CSRF-TOKEN": token } }); if (!r.ok()) throw new Error(`${method} ${url}: ${r.status()}`); return r.status() === 204 ? null : r.json(); }
try {
  await send("/api/v1/auth/login", { email: "admin@nexora.local", password, rememberMe: false });
  async function record(name, kind) { const found = (await get(`/api/v1/crm/records?kind=${kind}&search=${encodeURIComponent(name)}`)).items.find(x => x.name === name); return found ?? send("/api/v1/crm/records", { kind, name, status: "active", category: "Local testing" }); }
  const contact = await record("Nexora Local Test Member", "contact");
  const account = await record("Nexora Local Test Organisation", "account");
  const links = await get(`/api/v1/crm/records/${account.id}/account-contacts?page=1&includeEnded=false`);
  if (!links.items.some(x => x.association.contactId === contact.id)) await send(`/api/v1/crm/accounts/${account.id}/contacts`, { contactId: contact.id, startDate: new Date().toISOString().slice(0,10), workClassification: "unknown", isMainContact: true, isAuthorisedContact: true, roles: ["membership"] });
  let grant;
  for (let page = 1; ; page++) { const data = await get(`/api/v1/admin/portal-access?page=${page}`); grant = data.items.find(x => x.email === "local.member@nexora.local"); if (grant || page * 20 >= data.totalCount) break; }
  grant ??= await send("/api/v1/admin/portal-access/local-member", { contactId: contact.id, email: "local.member@nexora.local", password });
  if (grant.isRevoked) throw new Error("The existing local test member grant was revoked. Restore it explicitly in Settings.");
  const groups = await get(`/api/v1/admin/portal-access/${grant.id}/organisations`);
  if (!groups.some(x => x.accountId === account.id && !x.isRevoked)) await send(`/api/v1/admin/portal-access/${grant.id}/organisations`, { accountId: account.id, canSubmitApplications: true });
  let product = (await get("/api/v1/membership/products")).find(x => x.name === "Nexora Local Test Membership");
  if (!product) {
    product = await send("/api/v1/membership/products", { name: "Nexora Local Test Membership", category: "Local testing", termMonths: 12, rate: 0, currency: "GBP", isActive: true });
    await send(`/api/v1/membership/products/${product.id}/portal`, { version: product.version, enabled: true, organisationEnabled: true, terms: "Local demonstration membership terms. No payment is collected." }, "PUT");
  }
  let site = (await get("/api/v1/content/site")).site;
  if (!site) site = await send("/api/v1/content/site", { name: "Nexora Local Association", tagline: "Explore CRM, Portal and CMS together." });
  const website = await prepareAssociationWebsite({ get, send, site });
  console.log(`Public website: ${website.created.length} pages published, ${website.preserved.length} existing pages preserved.`);
  fs.mkdirSync(path.join(root, "artifacts"), { recursive: true });
  fs.writeFileSync(path.join(root, "artifacts/local-test-data.json"), JSON.stringify({ contactId: contact.id, accountId: account.id, siteId: site.id, portalEmail: "local.member@nexora.local", publicPath: `/site/${site.id}/home` }, null, 2));
  console.log("Local CRM records, portal member and CMS site are ready. See docs/LOCAL-TESTING.md.");
} finally { await client.dispose(); }
