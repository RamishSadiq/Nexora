import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { createRequire } from "node:module";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const require = createRequire(path.join(root, "apps/web/package.json"));
const ts = require("typescript");
const catalog = JSON.parse(fs.readFileSync(path.join(root, "shared/automation-catalog.json"), "utf8"));
const definitions = Object.values(catalog.sections).flat();
const registered = new Set(definitions.map(section => section.id));
for (const [kind, sections] of Object.entries(catalog.sections)) {
  if (new Set(sections.map(section => section.id)).size !== sections.length) throw new Error(`${kind}: duplicate section IDs.`);
  if (sections.some(section => !section.id || !section.title || !section.region)) throw new Error(`${kind}: incomplete section metadata.`);
}

const rendered = new Set();
const failures = [];
function strings(node) {
  if (!node) return [];
  if (ts.isStringLiteral(node)) return [node.text];
  if (ts.isJsxExpression(node)) return strings(node.expression);
  if (ts.isConditionalExpression(node)) return [...strings(node.whenTrue), ...strings(node.whenFalse)];
  return [];
}
function scan(file) {
  const source = ts.createSourceFile(file, fs.readFileSync(file, "utf8"), ts.ScriptTarget.Latest, true, ts.ScriptKind.TSX);
  function visit(node) {
    if (ts.isJsxOpeningElement(node) || ts.isJsxSelfClosingElement(node)) {
      const attributes = node.attributes.properties.filter(ts.isJsxAttribute);
      const identity = attributes.find(attribute => attribute.name.getText(source) === "data-section")
        ?? (node.tagName.getText(source) === "RecordSection" ? attributes.find(attribute => attribute.name.getText(source) === "title") : undefined);
      if (identity) for (const id of strings(identity.initializer)) {
        rendered.add(id);
        if (!registered.has(id)) failures.push(`${path.relative(root, file)}: section '${id}' is not in the shared catalogue.`);
      }
    }
    ts.forEachChild(node, visit);
  }
  visit(source);
}
function scanDirectory(directory) {
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const file = path.join(directory, entry.name);
    if (entry.isDirectory()) scanDirectory(file);
    else if (file.endsWith(".tsx")) scan(file);
  }
}
scanDirectory(path.join(root, "apps/web/src"));
for (const id of registered) if (!rendered.has(id)) failures.push(`Section '${id}' is registered but has no rendered section identity.`);
if (failures.length) throw new Error(failures.join("\n"));
console.log(`Automation catalogue verified: ${registered.size} rendered section identities and ${Object.keys(catalog.modules).length} module groups.`);
