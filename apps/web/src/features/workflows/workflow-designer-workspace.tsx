"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import {
  ArrowLeft,
  ChevronDown,
  Pencil,
  Play,
  Plus,
  Save,
  Trash2,
  X,
} from "lucide-react";
import { moduleApi } from "@/features/shared/module-api";
import {
  buttonClass,
  inputClass,
  primaryClass,
} from "@/features/shared/module-ui";
import { cn } from "@/lib/cn";

import type { Graph, Node, Metadata } from "./workflow-types";
import {
  WorkflowCanvas,
  WorkflowTriggerScopeSettings,
  WorkflowTriggerSettings,
  AdvancedNodeSettings,
} from "./workflow-editor";
import { WorkflowActionInputs } from "./workflow-action-inputs";
import { WorkflowSettings } from "./workflow-settings";
import { WorkflowOperations } from "./workflow-operations";
import { WorkflowGateSettings } from "./workflow-gate-settings";
import styles from "./workflow-designer.module.css";
type Definition = {
  id: string;
  name: string;
  description: string;
  draftJson: string;
  version: string;
  publishedNumber: number;
  activeVersionId: string | null;
};
type Version = {
  id: string;
  number: number;
  graphJson: string;
  publishedBy: string;
  createdAtUtc: string;
};
type Run = {
  id: string;
  workflowVersionId: string;
  recordId: string;
  recordLabel: string;
  status: string;
  currentNodeId: string;
  error: string;
  version: string;
  dueAtUtc: string | null;
  createdAtUtc: string;
  finishedAtUtc: string | null;
};
type RunDetail = {
  run: Run;
  version: Version;
  steps: {
    id: string;
    nodeId: string;
    kind: string;
    outcome: string;
    sequence: number;
    createdAtUtc: string;
    inputsJson: string;
    outputsJson: string;
    scope: string;
  }[];
};
type Detail = { definition: Definition; versions: Version[] };
const api = <T,>(path: string, method = "GET", body?: unknown) =>
  moduleApi<T>("workflows", path, method, body);
const emptyGraph = (entityKey: string): Graph => ({
  schemaVersion: 2,
  entityKey,
  trigger: "manual",
  entryNodeId: "end",
  nodes: [{ id: "end", kind: "end", label: "Complete workflow" }],
});
const time = (value: string | null) =>
  value ? new Date(value).toLocaleString() : "—";
const nodeNames: Record<string, string> = {
  condition: "Condition",
  action: "Action",
  wait: "Wait",
  end: "End",
  query: "Query records",
  variable: "Set variable",
  loop: "Loop",
  return: "Return",
  parallel: "Parallel",
  join: "Join",
  subworkflow: "Subworkflow",
  approval: "Human approval",
  "await-condition": "Wait for business state",
};

export function WorkflowDesignerWorkspace() {
  const [preview, setPreview] = useState<unknown>(null);
  const [previewMode, setPreviewMode] = useState(false);
  const [bulkTargets, setBulkTargets] = useState<string[]>([]);
  const [runFilter, setRunFilter] = useState("");
  const [metadata, setMetadata] = useState<Metadata | null>(null);
  const [definitions, setDefinitions] = useState<Definition[]>([]);
  const [selected, setSelected] = useState<Detail | null>(null);
  const [graph, setGraph] = useState<Graph>(emptyGraph(""));
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [nodeId, setNodeId] = useState("end");
  const [dirty, setDirty] = useState(false);
  const [view, setView] = useState<"designer" | "history" | "settings" | "operations">(
    "designer",
  );
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [loading, setLoading] = useState(true);
  const [runs, setRuns] = useState<Run[]>([]);
  const [runDetail, setRunDetail] = useState<RunDetail | null>(null);
  const [executeOpen, setExecuteOpen] = useState(false);
  const [records, setRecords] = useState<
    { id: string; label: string; values: Record<string, string | null> }[]
  >([]);
  const [search, setSearch] = useState("");
  const [target, setTarget] = useState("");
  const [requestKey, setRequestKey] = useState("");
  const [directory, setDirectory] = useState<{
    users: { id: string; name: string }[];
    teams: { id: string; name: string }[];
  }>({ users: [], teams: [] });
  const [workflowDialog, setWorkflowDialog] = useState<"new" | "edit" | null>(
    null,
  );
  const [workflowSettingsOpen, setWorkflowSettingsOpen] = useState(false);
  const [workflowActionOpen, setWorkflowActionOpen] = useState(false);
  const [debugActionOpen, setDebugActionOpen] = useState(false);
  const [publishActionOpen, setPublishActionOpen] = useState(false);
  const [dialogName, setDialogName] = useState("");
  const [dialogDescription, setDialogDescription] = useState("");
  const can = (permission: string) =>
    metadata?.permissions.includes(`workflows.${permission}`) ?? false;
  const canEdit = selected ? can("edit") : can("create");
  const reload = useCallback(async () => {
    setDefinitions(await api<Definition[]>("/definitions"));
  }, []);
  useEffect(() => {
    let active = true;
    Promise.all([api<Metadata>("/metadata"), api<Definition[]>("/definitions")])
      .then(([meta, rows]) => {
        if (active) {
          setMetadata(meta);
          setDefinitions(rows);
          setGraph(emptyGraph(meta.entities[0]?.key ?? ""));
          if (meta.actions.some((a) => a.key === "work.task.create"))
            api<typeof directory>("/directory")
              .then(setDirectory)
              .catch(() => {});
        }
      })
      .catch((e) => {
        if (active) setError(e.message);
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, []);
  useEffect(() => {
    if (
      view !== "history" ||
      !metadata?.permissions.includes("workflows.history")
    )
      return;
    let active = true;
    const poll = async () => {
      try {
        const rows = await api<Run[]>("/runs");
        if (active) setRuns(rows);
      } catch (e) {
        if (active)
          setError(e instanceof Error ? e.message : "Unable to load runs.");
      }
    };
    void poll();
    const timer = setInterval(poll, 5000);
    return () => {
      active = false;
      clearInterval(timer);
    };
  }, [view, metadata]);
  useEffect(() => {
    if (
      !runDetail ||
      ["completed", "failed", "cancelled"].includes(runDetail.run.status)
    )
      return;
    let active = true;
    const id = runDetail.run.id;
    const timer = setInterval(() => {
      api<RunDetail>(`/runs/${id}`)
        .then((detail) => {
          if (active) setRunDetail(detail);
        })
        .catch((e) => {
          if (active) setError(e.message);
        });
    }, 3000);
    return () => {
      active = false;
      clearInterval(timer);
    };
  }, [runDetail]);
  useEffect(() => {
    if (!executeOpen) return;
    let active = true;
    const entity =
      !previewMode && selected?.definition.activeVersionId
        ? selected.versions.find(
            (v) => v.id === selected.definition.activeVersionId,
          )
        : undefined;
    const entityKey = entity
      ? (JSON.parse(entity.graphJson) as Graph).entityKey
      : graph.entityKey;
    const timer = setTimeout(() => {
      api<{ items: typeof records }>(
        `/records/${encodeURIComponent(entityKey)}?search=${encodeURIComponent(search)}`,
      )
        .then((page) => {
          if (active) setRecords(page.items);
        })
        .catch((e) => {
          if (active) setError(e.message);
        });
    }, 250);
    return () => {
      active = false;
      clearTimeout(timer);
    };
  }, [executeOpen, search, graph.entityKey, selected, previewMode]);
  async function action(work: () => Promise<void>, success: string) {
    setBusy(true);
    setError("");
    setNotice("");
    try {
      await work();
      setNotice(success);
    } catch (e) {
      setError(
        e instanceof Error ? e.message : "Unable to complete this action.",
      );
    } finally {
      setBusy(false);
    }
  }
  const verticalLayout = (value: Graph): Graph => ({
    ...value,
    nodes: value.nodes.map((node, index) => ({
      ...node,
      x: 60,
      y: 32 + index * 132,
    })),
  });
  async function openDefinition(id: string) {
    const detail = await api<Detail>(`/definitions/${id}`);
    setSelected(detail);
    setName(detail.definition.name);
    setDescription(detail.definition.description);
    const next = verticalLayout(
      JSON.parse(detail.definition.draftJson) as Graph,
    );
    setGraph(next);
    setNodeId(next.entryNodeId);
    setDirty(false);
    setView("designer");
  }
  function loadDraft(
    draftName: string,
    draftDescription: string,
    draftGraph: Graph,
  ) {
    const next = verticalLayout(draftGraph);
    setSelected(null);
    setName(draftName);
    setDescription(draftDescription);
    setGraph(next);
    setNodeId(next.entryNodeId);
    setDirty(true);
    setView("designer");
  }
  function newWorkflow() {
    setSelected(null);
    setName("");
    setDescription("");
    setGraph(emptyGraph(metadata?.entities[0]?.key ?? ""));
    setNodeId("end");
    setDirty(false);
    setNotice("");
    setError("");
  }
  function openWorkflowDialog(mode: "new" | "edit") {
    if (mode === "new" && dirty && !window.confirm("Discard unsaved changes?"))
      return;
    setWorkflowActionOpen(false);
    setWorkflowDialog(mode);
    setDialogName(mode === "edit" ? name : "");
    setDialogDescription(mode === "edit" ? description : "");
  }
  function submitWorkflowDialog() {
    if (workflowDialog === "new") {
      newWorkflow();
      setName(dialogName.trim());
      setDescription(dialogDescription.trim());
      setDirty(true);
    } else {
      setName(dialogName.trim());
      setDescription(dialogDescription.trim());
      setDirty(true);
    }
    setWorkflowDialog(null);
  }
  function updateNode(changes: Partial<Node>) {
    setGraph((g) => ({
      ...g,
      nodes: g.nodes.map((n) => (n.id === nodeId ? { ...n, ...changes } : n)),
    }));
    setDirty(true);
  }
  function addNode(kind: string) {
    const id = crypto.randomUUID();
    const end = graph.nodes.find((n) => n.kind === "end")?.id ?? "end";
    const node: Node = {
      id,
      kind,
      label: nodeNames[kind],
      x: 60,
      y: 32 + graph.nodes.length * 132,
      next: kind === "end" ? undefined : end,
    };
    if (kind === "condition")
      Object.assign(node, {
        field: "status",
        operator: "equals",
        value: "",
        ifTrue: end,
        ifFalse: end,
      });
    if (kind === "action")
      Object.assign(node, {
        actionKey: metadata?.actions[0]?.key,
        inputs: Object.fromEntries(
          (metadata?.actions[0]?.inputSchema ?? [])
            .filter((i) => i.required)
            .map((i) => [
              i.key,
              i.choices?.[0] ?? (i.key === "name" ? "{{record.label}}" : ""),
            ]),
        ),
      });
    if (kind === "wait") node.delayMinutes = 60;
    if (kind === "approval") Object.assign(node, {ifTrue:end,ifFalse:end,timeoutNext:end,timeoutMinutes:1440,pollMinutes:5,next:undefined});
    if (kind === "await-condition") Object.assign(node, {predicate:{field:"status",operator:"equals",value:"done"},timeoutNext:end,timeoutMinutes:1440,pollMinutes:5});
    if (kind === "query")
      Object.assign(node, {
        queryEntity: graph.entityKey,
        outputVariable: "results",
        limit: 100,
      });
    if (kind === "variable")
      Object.assign(node, { outputVariable: "value", value: "" });
    if (kind === "loop") node.itemsVariable = "variables.results";
    if (kind === "return") node.next = undefined;
    setGraph((g) => {
      const last = g.nodes.filter((n) => n.kind !== "end").at(-1);
      return {
        ...g,
        entryNodeId: last ? g.entryNodeId : id,
        nodes: [
          ...g.nodes
            .map((n) =>
              n.id === last?.id
                ? ["condition", "approval"].includes(n.kind)
                  ? { ...n, ifTrue: id }
                  : { ...n, next: id }
                : n,
            )
            .filter((n) => n.kind !== "end"),
          node,
          ...g.nodes.filter((n) => n.kind === "end"),
        ],
      };
    });
    setNodeId(id);
    setDirty(true);
  }
  function removeNode() {
    const node = graph.nodes.find((n) => n.id === nodeId);
    if (!node || node.kind === "end") return;
    const next =
      node.next ?? node.ifTrue ?? graph.nodes.find((n) => n.kind === "end")?.id ?? graph.nodes.find((n) => n.id !== nodeId)?.id;
    if (!next) return;
    setGraph((g) => ({
      ...g,
      entryNodeId: g.entryNodeId === nodeId ? next : g.entryNodeId,
      nodes: g.nodes
        .filter((n) => n.id !== nodeId)
        .map((n) => ({
          ...n,
          next: n.next === nodeId ? next : n.next,
          ifTrue: n.ifTrue === nodeId ? next : n.ifTrue,
          ifFalse: n.ifFalse === nodeId ? next : n.ifFalse,
          body: n.body === nodeId ? next : n.body,
          errorNext: n.errorNext === nodeId ? next : n.errorNext,
          timeoutNext: n.timeoutNext === nodeId ? next : n.timeoutNext,
          branches: n.branches?.filter((id) => id !== nodeId),
        })),
    }));
    setNodeId(next);
    setDirty(true);
  }
  async function save() {
    const row = await api<Definition>(
      selected ? `/definitions/${selected.definition.id}` : "/definitions",
      selected ? "PUT" : "POST",
      { name, description, graph, version: selected?.definition.version },
    );
    await reload();
    await openDefinition(row.id);
  }
  async function lifecycle(kind: "publish" | "activate" | "deactivate") {
    if (!selected) return;
    await api(`/definitions/${selected.definition.id}/${kind}`, "POST", {
      version: selected.definition.version,
    });
    await reload();
    await openDefinition(selected.definition.id);
  }
  async function archive() {
    if (!selected) return;
    await api(`/definitions/${selected.definition.id}`, "DELETE", {
      version: selected.definition.version,
    });
    await reload();
    newWorkflow();
  }
  const currentNode = graph.nodes.find((n) => n.id === nodeId);
  const destination = (
    label: string,
    value: string | undefined,
    key: "next" | "ifTrue" | "ifFalse",
  ) => (
    <label className="block space-y-1 text-sm">
      <span>{label}</span>
      <select
        className={inputClass}
        value={value ?? ""}
        disabled={!canEdit || busy}
        onChange={(e) => updateNode({ [key]: e.target.value })}
      >
        <option value="">Choose destination</option>
        {graph.nodes
          .filter((n) => n.id !== nodeId)
          .map((n) => (
            <option key={n.id} value={n.id}>
              {n.label}
            </option>
          ))}
      </select>
    </label>
  );
  const exportWorkflow = () => {
    const blob = new Blob(
      [
        JSON.stringify(
          { schema: "nexora.workflow/2", name, description, graph },
          null,
          2,
        ),
      ],
      { type: "application/json" },
    );
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = (name || "workflow") + ".json";
    a.click();
    URL.revokeObjectURL(url);
    setDebugActionOpen(false);
  };
  const importWorkflow = (file: File) =>
    void action(async () => {
      if (file.size > 262144) throw new Error("Import exceeds 256 KB.");
      const r = JSON.parse(await file.text()) as {
        name: string;
        description: string;
        graph: Graph;
      };
      if (!r.graph?.nodes || !r.graph.entityKey)
        throw new Error("Invalid workflow export.");
      loadDraft(r.name, r.description ?? "", r.graph);
      setDebugActionOpen(false);
    }, "Imported as an unsaved draft.");
  const loadTemplate = () =>
    void action(async () => {
      const templates =
        await api<{ name: string; description: string; graph: Graph }[]>(
          "/templates",
        );
      const choice = window.prompt(
        templates.map((t, i) => `${i + 1}: ${t.name}`).join("\n"),
        "1",
      );
      if (choice === null) return;
      const template = templates[Number(choice) - 1];
      if (!template) throw new Error("Choose a listed template.");
      loadDraft(template.name, template.description, template.graph);
      setDebugActionOpen(false);
    }, "Template loaded for review.");
  if (loading)
    return (
      <main className="p-8" role="status">
        Loading WFL Designer…
      </main>
    );
  return (
    <div className={cn(styles.workspace, "min-h-screen bg-slate-50 text-slate-800")}>
      <header className="border-b border-slate-200 bg-white p-4 sm:px-8">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <Link
            href="/dashboard"
            className="inline-flex items-center gap-2 text-sm font-medium text-blue-700"
          >
            <ArrowLeft className="size-4" />
            Return to Nexora
          </Link>
          <span className="text-xs text-slate-500">
            Workflow administration
          </span>
        </div>
      </header>
      <main className="mx-auto max-w-[1800px] space-y-4 p-3 sm:p-5">
        <div className="rounded-xl border border-slate-200 bg-white px-4 py-3">
          <div className="flex flex-wrap items-center gap-3">
            <div className="min-w-0 flex-1">
              <div className="min-w-0">
                <p className="text-[10px] font-semibold uppercase tracking-[0.14em] text-slate-500">
                  WFL Designer
                </p>
                <h1
                  className="break-words text-lg font-semibold leading-tight text-slate-900"
                  title={description || "No description added yet."}
                >
                  {name || "New workflow"}
                </h1>
              </div>
            </div>
            <div className="relative">
              <button
                type="button"
                className={buttonClass}
                disabled={busy}
                aria-haspopup="menu"
                aria-expanded={workflowActionOpen}
                onClick={() => setWorkflowActionOpen((open) => !open)}
              >
                Actions
                <ChevronDown className="ml-2 inline size-4" />
              </button>
              {workflowActionOpen && (
                <div
                  role="menu"
                  className="absolute right-0 z-30 mt-2 w-56 rounded-xl border border-slate-200 bg-white p-1 shadow-xl"
                >
                  <button
                    type="button"
                    role="menuitem"
                    className="flex w-full items-center gap-2 rounded-lg px-3 py-2 text-left text-sm hover:bg-slate-50"
                    disabled={!can("create")}
                    onClick={() => openWorkflowDialog("new")}
                  >
                    <Plus className="size-4" />
                    New WFL
                  </button>
                  <button
                    type="button"
                    role="menuitem"
                    className="flex w-full items-center gap-2 rounded-lg px-3 py-2 text-left text-sm hover:bg-slate-50 disabled:opacity-50"
                    disabled={!selected || !can("edit")}
                    onClick={() => openWorkflowDialog("edit")}
                  >
                    <Pencil className="size-4" />
                    Edit WFL
                  </button>
                  <button
                    type="button"
                    role="menuitem"
                    className="flex w-full items-center gap-2 rounded-lg px-3 py-2 text-left text-sm text-red-700 hover:bg-red-50 disabled:opacity-50"
                    disabled={!selected || !can("delete") || dirty}
                    data-destructive
                    onClick={() => {
                      setWorkflowActionOpen(false);
                      if (
                        window.confirm(
                          "Delete this saved workflow? Existing runs and history will remain available.",
                        )
                      )
                        void action(archive, "Workflow deleted.");
                    }}
                  >
                    <Trash2 className="size-4" />
                    Delete WFL
                  </button>
                  <button
                    type="button"
                    role="menuitem"
                    className="flex w-full items-center gap-2 rounded-lg px-3 py-2 text-left text-sm hover:bg-slate-50"
                    onClick={() => {
                      setWorkflowActionOpen(false);
                      setWorkflowSettingsOpen(true);
                    }}
                  >
                    Trigger &amp; variables
                  </button>
                </div>
              )}
            </div>
            <div className="flex shrink-0 gap-1 border-l border-slate-200 pl-3">
              <button className={buttonClass} aria-pressed={view === "operations"} onClick={() => setView("operations")}>Operations</button>
              <button
                className={buttonClass}
                aria-pressed={view === "designer"}
                onClick={() => setView("designer")}
              >
                Designer
              </button>
              {can("history") && (
                <button
                  className={buttonClass}
                  aria-pressed={view === "history"}
                  onClick={() => setView("history")}
                >
                  History
                </button>
              )}
              {can("integrations") && (
                <button
                  className={buttonClass}
                  aria-pressed={view === "settings"}
                  onClick={() => setView("settings")}
                >
                  Connections
                </button>
              )}
            </div>
          </div>
        </div>
        {error && (
          <p
            role="alert"
            className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-800"
          >
            {error}
          </p>
        )}
        {notice && (
          <p
            role="status"
            className="rounded-xl bg-green-50 p-3 text-sm text-green-800"
          >
            {notice}
          </p>
        )}
        {view === "operations" && metadata ? (
          <WorkflowOperations metadata={metadata} onDraft={loadDraft} onRun={(id) => void action(async () => {setRunDetail(await api<RunDetail>(`/runs/${id}`));setView("history");}, "Run loaded.")} />
        ) : view === "settings" ? (
          <WorkflowSettings canAi={can("ai")} onDraft={loadDraft} />
        ) : view === "designer" ? (
          <div className="grid min-h-[calc(100vh-11rem)] items-stretch gap-4 xl:grid-cols-[190px_minmax(0,1fr)_340px]">
            <aside className="min-w-0 rounded-xl border border-slate-200 bg-white p-3">
              <h2 className="font-semibold">Designer tools</h2>
              <p className="mt-1 text-xs leading-5 text-slate-500">
                Add steps to the WFL canvas.
              </p>
              <div className="mt-3 space-y-1.5">
                {canEdit &&
                  metadata?.nodeKinds
                    .filter((k) => k !== "end")
                    .map((kind) => (
                      <button
                        key={kind}
                        className="flex w-full items-center gap-2 rounded-lg border border-slate-200 px-3 py-2 text-left text-sm transition hover:border-blue-300 hover:bg-blue-50"
                        disabled={busy || graph.nodes.length >= 100}
                        onClick={() => addNode(kind)}
                      >
                        <Plus className="size-3.5 shrink-0" />
                        {nodeNames[kind]}
                      </button>
                    ))}
              </div>
            </aside>
            <section className="min-w-0 space-y-3">
              <div className="flex h-full min-h-[calc(100vh-11rem)] flex-col rounded-xl border border-slate-200 bg-white p-4">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <label className="text-sm font-medium">
                    Saved WFL
                    <select
                      aria-label="Saved WFL"
                      className={cn(inputClass, "ml-2 min-w-48")}
                      value={selected?.definition.id ?? ""}
                      disabled={busy}
                      onChange={(e) => {
                        const id = e.target.value;
                        if (!id) return;
                        if (
                          !dirty ||
                          window.confirm("Discard unsaved changes?")
                        )
                          void action(() => openDefinition(id), "");
                      }}
                    >
                      <option value="">New workflow</option>
                      {definitions.map((row) => (
                        <option key={row.id} value={row.id}>
                          {row.name}
                        </option>
                      ))}
                    </select>
                  </label>
                  <div className="flex flex-wrap items-center gap-2">
                    <div className="relative">
                      <button
                        type="button"
                        className={buttonClass}
                        disabled={busy}
                        aria-haspopup="menu"
                        aria-expanded={debugActionOpen}
                        onClick={() => {
                          setDebugActionOpen((open) => !open);
                          setPublishActionOpen(false);
                        }}
                      >
                        Debug Options
                        <ChevronDown className="ml-2 inline size-4" />
                      </button>
                      {debugActionOpen && (
                        <div
                          role="menu"
                          className="absolute right-0 z-30 mt-2 w-56 rounded-xl border border-slate-200 bg-white p-1 shadow-xl"
                        >
                          <button
                            type="button"
                            role="menuitem"
                            className="flex w-full items-center gap-2 rounded-lg px-3 py-2 text-left text-sm hover:bg-slate-50"
                            onClick={() => {
                              setDebugActionOpen(false);
                              setPreviewMode(true);
                              setBulkTargets([]);
                              setTarget("");
                              setSearch("");
                              setExecuteOpen(true);
                            }}
                          >
                            Test preview
                          </button>
                          <button
                            type="button"
                            role="menuitem"
                            className="flex w-full items-center gap-2 rounded-lg px-3 py-2 text-left text-sm hover:bg-slate-50"
                            onClick={exportWorkflow}
                          >
                            Export JSON
                          </button>
                          {canEdit && (
                            <label
                              role="menuitem"
                              className="flex w-full cursor-pointer items-center gap-2 rounded-lg px-3 py-2 text-left text-sm hover:bg-slate-50"
                            >
                              Import JSON
                              <input
                                type="file"
                                accept="application/json,.json"
                                className="sr-only"
                                onChange={(e) => {
                                  const file = e.target.files?.[0];
                                  if (file) importWorkflow(file);
                                  e.target.value = "";
                                }}
                              />
                            </label>
                          )}
                          {canEdit && (
                            <button
                              type="button"
                              role="menuitem"
                              className="flex w-full items-center gap-2 rounded-lg px-3 py-2 text-left text-sm hover:bg-slate-50"
                              onClick={loadTemplate}
                            >
                              Use template
                            </button>
                          )}
                          {selected && can("integrations") && (
                            <button
                              type="button"
                              role="menuitem"
                              className="flex w-full items-center gap-2 rounded-lg px-3 py-2 text-left text-sm hover:bg-slate-50"
                              onClick={() =>
                                void action(async () => {
                                  const hook = await api<{
                                    url: string;
                                    secret: string;
                                    signing: string;
                                  }>(
                                    `/definitions/${selected.definition.id}/webhook`,
                                    "POST",
                                    {},
                                  );
                                  window.prompt(
                                    `Webhook: ${hook.url}\n${hook.signing}\nCopy the secret now; rotating revokes the previous secret.`,
                                    hook.secret,
                                  );
                                  setDebugActionOpen(false);
                                }, "Webhook secret rotated.")
                              }
                            >
                              Configure webhook
                            </button>
                          )}
                        </div>
                      )}
                    </div>
                    <div className="relative">
                      <button
                        type="button"
                        className={buttonClass}
                        disabled={busy}
                        aria-haspopup="menu"
                        aria-expanded={publishActionOpen}
                        onClick={() => {
                          setPublishActionOpen((open) => !open);
                          setDebugActionOpen(false);
                        }}
                      >
                        Publish Options
                        <ChevronDown className="ml-2 inline size-4" />
                      </button>
                      {publishActionOpen && (
                        <div
                          role="menu"
                          className="absolute right-0 z-30 mt-2 w-56 rounded-xl border border-slate-200 bg-white p-1 shadow-xl"
                        >
                          <button
                            type="button"
                            role="menuitem"
                            className="flex w-full items-center gap-2 rounded-lg px-3 py-2 text-left text-sm hover:bg-slate-50"
                            disabled={busy}
                            onClick={() => {
                              setPublishActionOpen(false);
                              void action(async () => {
                                const result = await api<{ errors: string[] }>(
                                  "/validate",
                                  "POST",
                                  graph,
                                );
                                if (result.errors.length)
                                  throw new Error(result.errors.join(" "));
                              }, "Workflow is valid.");
                            }}
                          >
                            Validate
                          </button>
                          {canEdit && (
                            <button
                              type="button"
                              role="menuitem"
                              className="flex w-full items-center gap-2 rounded-lg px-3 py-2 text-left text-sm hover:bg-slate-50"
                              disabled={busy || !name || !graph.entityKey}
                              onClick={() => {
                                setPublishActionOpen(false);
                                void action(save, "Draft saved.");
                              }}
                            >
                              <Save className="size-4" />
                              Save draft
                            </button>
                          )}
                          {selected && can("publish") && (
                            <button
                              type="button"
                              role="menuitem"
                              className="flex w-full items-center gap-2 rounded-lg px-3 py-2 text-left text-sm hover:bg-slate-50 disabled:opacity-50"
                              disabled={busy || dirty}
                              title={dirty ? "Save the draft first" : undefined}
                              onClick={() => {
                                setPublishActionOpen(false);
                                void action(
                                  () => lifecycle("publish"),
                                  "Version published. Activate it to allow runs.",
                                );
                              }}
                            >
                              Publish version
                            </button>
                          )}
                          {selected && can("activate") && (
                            <button
                              type="button"
                              role="menuitem"
                              className="flex w-full items-center gap-2 rounded-lg px-3 py-2 text-left text-sm hover:bg-slate-50 disabled:opacity-50"
                              disabled={
                                busy ||
                                dirty ||
                                selected.definition.publishedNumber === 0
                              }
                              onClick={() => {
                                setPublishActionOpen(false);
                                void action(
                                  () =>
                                    lifecycle(
                                      selected.definition.activeVersionId
                                        ? "deactivate"
                                        : "activate",
                                    ),
                                  selected.definition.activeVersionId
                                    ? "Workflow deactivated."
                                    : "Published version activated.",
                                );
                              }}
                            >
                              {selected.definition.activeVersionId
                                ? "Deactivate"
                                : "Activate latest"}
                            </button>
                          )}
                          {selected?.definition.activeVersionId &&
                            can("execute") && (
                              <button
                                type="button"
                                role="menuitem"
                                className="flex w-full items-center gap-2 rounded-lg px-3 py-2 text-left text-sm hover:bg-slate-50"
                                disabled={busy}
                                onClick={() => {
                                  setPublishActionOpen(false);
                                  setPreviewMode(false);
                                  setBulkTargets([]);
                                  setExecuteOpen(true);
                                  setTarget("");
                                  setSearch("");
                                  setRequestKey(crypto.randomUUID());
                                }}
                              >
                                <Play className="size-4" />
                                Run workflow
                              </button>
                            )}
                        </div>
                      )}
                    </div>
                  </div>
                </div>
                {dirty && (
                  <p className="text-xs text-amber-800">
                    Unsaved changes. Publishing uses the saved draft; running
                    uses the active version.
                  </p>
                )}
                {selected && selected.versions.length > 0 && (
                  <details className="text-sm">
                    <summary className="cursor-pointer text-blue-700">
                      Published versions ({selected.versions.length})
                    </summary>
                    <ul className="mt-2 space-y-1 text-xs text-slate-600">
                      {selected.versions.map((v) => (
                        <li key={v.id}>
                          Version {v.number} · {time(v.createdAtUtc)}
                          {v.id === selected.definition.activeVersionId
                            ? " · Active"
                            : ""}
                        </li>
                      ))}
                    </ul>
                  </details>
                )}
                <div className="min-h-0 flex-1 pt-3">
                  <WorkflowCanvas
                    graph={graph}
                    selected={nodeId}
                    onSelect={setNodeId}
                    vertical
                    editable={canEdit && !busy}
                    onMove={(id, x, y) => {
                      setGraph((g) => ({
                        ...g,
                        nodes: g.nodes.map((n) =>
                          n.id === id ? { ...n, x, y } : n,
                        ),
                      }));
                      setDirty(true);
                    }}
                  />
                </div>
              </div>
            </section>
            <section className="space-y-4 rounded-xl border border-slate-200 bg-white p-5">
              <h2 className="font-semibold">Node settings</h2>
              {currentNode && (
                <>
                  {["approval", "await-condition"].includes(currentNode.kind) && metadata && <WorkflowGateSettings node={currentNode} graph={graph} metadata={metadata} disabled={!canEdit || busy} onChange={updateNode} />}
                  <label className="block space-y-1 text-sm">
                    <span>Label</span>
                    <input
                      className={inputClass}
                      value={currentNode.label}
                      maxLength={160}
                      disabled={!canEdit || busy}
                      onChange={(e) => updateNode({ label: e.target.value })}
                    />
                  </label>
                  {metadata && (
                    <AdvancedNodeSettings
                      node={currentNode}
                      graph={graph}
                      metadata={metadata}
                      disabled={!canEdit || busy}
                      onChange={updateNode}
                    />
                  )}
                  {currentNode.kind === "condition" &&
                    !currentNode.predicate && (
                      <>
                        <label className="block space-y-1 text-sm">
                          <span>Field</span>
                          <select
                            className={inputClass}
                            value={currentNode.field}
                            disabled={!canEdit || busy}
                            onChange={(e) =>
                              updateNode({ field: e.target.value })
                            }
                          >
                            {metadata?.entities
                              .find((e) => e.key === graph.entityKey)
                              ?.fields.map((f) => (
                                <option value={f.key} key={f.key}>
                                  {f.title}
                                </option>
                              ))}
                          </select>
                        </label>
                        <label className="block space-y-1 text-sm">
                          <span>Operator</span>
                          <select
                            className={inputClass}
                            value={currentNode.operator}
                            disabled={!canEdit || busy}
                            onChange={(e) =>
                              updateNode({ operator: e.target.value })
                            }
                          >
                            {metadata?.operators.map((op) => (
                              <option key={op}>{op}</option>
                            ))}
                          </select>
                        </label>
                        {!["is-empty", "is-not-empty"].includes(
                          currentNode.operator ?? "",
                        ) && (
                          <label className="block space-y-1 text-sm">
                            <span>Value</span>
                            <input
                              className={inputClass}
                              value={currentNode.value ?? ""}
                              disabled={!canEdit || busy}
                              maxLength={500}
                              onChange={(e) =>
                                updateNode({ value: e.target.value })
                              }
                            />
                          </label>
                        )}
                        {destination("When true", currentNode.ifTrue, "ifTrue")}
                        {destination(
                          "When false",
                          currentNode.ifFalse,
                          "ifFalse",
                        )}
                      </>
                    )}
                  {currentNode.kind === "condition" &&
                    currentNode.predicate && (
                      <>
                        {destination("When true", currentNode.ifTrue, "ifTrue")}
                        {destination(
                          "When false",
                          currentNode.ifFalse,
                          "ifFalse",
                        )}
                      </>
                    )}
                  {currentNode.kind === "action" && metadata && (
                    <WorkflowActionInputs
                      node={currentNode}
                      graph={graph}
                      metadata={metadata}
                      disabled={!canEdit || busy}
                      onChange={updateNode}
                      directory={directory}
                    />
                  )}
                  {currentNode.kind === "wait" && (
                    <label className="block space-y-1 text-sm">
                      <span>Delay (minutes)</span>
                      <input
                        type="number"
                        min={1}
                        max={525600}
                        className={inputClass}
                        value={currentNode.delayMinutes ?? 1}
                        disabled={!canEdit || busy}
                        onChange={(e) =>
                          updateNode({ delayMinutes: Number(e.target.value) })
                        }
                      />
                      <span className="block text-xs text-slate-500">
                        The run is saved and resumes in the background.
                      </span>
                    </label>
                  )}
                  {!["condition", "approval", "end", "return"].includes(currentNode.kind) &&
                    destination("Next node", currentNode.next, "next")}
                  {canEdit && currentNode.kind !== "end" && (
                    <button
                      className={buttonClass}
                      disabled={busy}
                      onClick={removeNode}
                    >
                      Remove node
                    </button>
                  )}
                </>
              )}
            </section>
          </div>
        ) : (
          <section className="space-y-4">
            <div className="overflow-x-auto rounded-xl border border-slate-200 bg-white p-4">
              <h2 className="mb-3 font-semibold">Workflow runs</h2>
              <select
                className={inputClass}
                aria-label="Filter run status"
                value={runFilter}
                onChange={(e) => setRunFilter(e.target.value)}
              >
                <option value="">All statuses</option>
                {["queued", "waiting", "completed", "failed", "cancelled"].map(
                  (s) => (
                    <option key={s}>{s}</option>
                  ),
                )}
              </select>
              <p className="my-3 text-sm">
                {runs.filter((r) => r.status === "completed").length} completed
                · {runs.filter((r) => r.status === "failed").length} failed ·{" "}
                {
                  runs.filter((r) => ["queued", "waiting"].includes(r.status))
                    .length
                }{" "}
                pending (latest {runs.length})
              </p>
              {runs.length === 0 ? (
                <p className="text-sm text-slate-500">No executions yet.</p>
              ) : (
                <table className="w-full text-left text-sm">
                  <thead>
                    <tr>
                      {["Record", "Status", "Started", "Resume", ""].map(
                        (h, i) => (
                          <th key={i} className="p-3">
                            {h}
                          </th>
                        ),
                      )}
                    </tr>
                  </thead>
                  <tbody>
                    {runs
                      .filter((r) => !runFilter || r.status === runFilter)
                      .map((run) => (
                        <tr key={run.id} className="border-t border-slate-100">
                          <td className="p-3">{run.recordLabel}</td>
                          <td className="p-3 capitalize">{run.status}</td>
                          <td className="p-3">{time(run.createdAtUtc)}</td>
                          <td className="p-3">
                            {run.status === "waiting"
                              ? time(run.dueAtUtc)
                              : "—"}
                          </td>
                          <td className="p-3">
                            <button
                              className={buttonClass}
                              onClick={() =>
                                void action(
                                  async () =>
                                    setRunDetail(
                                      await api<RunDetail>(`/runs/${run.id}`),
                                    ),
                                  "",
                                )
                              }
                            >
                              Inspect run
                            </button>
                          </td>
                        </tr>
                      ))}
                  </tbody>
                </table>
              )}
            </div>
            {runDetail && (
              <article className="space-y-4 rounded-xl border border-slate-200 bg-white p-5">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <h2 className="font-semibold">
                    {runDetail.run.recordLabel} · Version{" "}
                    {runDetail.version.number} · {runDetail.run.status}
                  </h2>
                  <div className="flex gap-2">
                    {can("retry") && runDetail.run.status === "failed" && (
                      <button
                        className={buttonClass}
                        disabled={busy}
                        onClick={() =>
                          void action(async () => {
                            await api(
                              `/runs/${runDetail.run.id}/retry`,
                              "POST",
                              { version: runDetail.run.version },
                            );
                            setRunDetail(
                              await api<RunDetail>(`/runs/${runDetail.run.id}`),
                            );
                          }, "Failed step queued for retry.")
                        }
                      >
                        Retry failed step
                      </button>
                    )}
                    {can("cancel") &&
                      ["queued", "waiting", "running"].includes(
                        runDetail.run.status,
                      ) && (
                        <button
                          className={buttonClass}
                          disabled={busy}
                          onClick={() =>
                            void action(async () => {
                              await api(
                                `/runs/${runDetail.run.id}/cancel`,
                                "POST",
                                { version: runDetail.run.version },
                              );
                              setRunDetail(
                                await api<RunDetail>(
                                  `/runs/${runDetail.run.id}`,
                                ),
                              );
                            }, "Run cancelled.")
                          }
                        >
                          Cancel run
                        </button>
                      )}
                  </div>
                </div>
                <p className="break-all text-xs text-slate-500">
                  Run {runDetail.run.id} · Target {runDetail.run.recordId}
                </p>
                {runDetail.run.error && (
                  <p className="text-sm text-red-700">{runDetail.run.error}</p>
                )}
                <WorkflowCanvas
                  graph={JSON.parse(runDetail.version.graphJson) as Graph}
                  selected={runDetail.run.currentNodeId}
                  onSelect={() => {}}
                  onMove={() => {}}
                  editable={false}
                />
                <ol className="space-y-3">
                  {runDetail.steps.map((step) => (
                    <li
                      key={step.id}
                      className="rounded-lg border border-slate-100 p-3 text-sm"
                    >
                      <span className="text-xs text-slate-500">
                        {time(step.createdAtUtc)} · {step.kind}
                      </span>
                      <p className="mt-1 break-words">{step.outcome}</p>
                      {step.scope && (
                        <p className="break-all text-xs text-slate-500">
                          {step.scope}
                        </p>
                      )}
                      {(step.inputsJson !== "{}" ||
                        step.outputsJson !== "{}") && (
                        <details className="mt-2 text-xs">
                          <summary>Resolved inputs and outputs</summary>
                          <pre className="max-h-48 overflow-auto whitespace-pre-wrap">
                            {JSON.stringify(
                              {
                                inputs: JSON.parse(step.inputsJson || "{}"),
                                outputs: JSON.parse(step.outputsJson || "{}"),
                              },
                              null,
                              2,
                            )}
                          </pre>
                        </details>
                      )}
                    </li>
                  ))}
                </ol>
              </article>
            )}
          </section>
        )}
        {workflowSettingsOpen && metadata && (
          <div className="fixed inset-0 z-50 grid place-items-center overflow-y-auto bg-slate-950/40 p-4">
            <section
              role="dialog"
              aria-modal="true"
              aria-labelledby="workflow-settings-title"
              className="w-full max-w-4xl space-y-4 rounded-xl bg-white p-6 shadow-xl"
            >
              <div className="flex items-center justify-between">
                <div>
                  <h2 id="workflow-settings-title" className="font-semibold">
                    Trigger and record scope
                  </h2>
                  <p className="mt-1 text-sm text-slate-500">
                    Configure when this WFL runs, eligible records, and initial
                    variables.
                  </p>
                </div>
                <button
                  type="button"
                  aria-label="Close workflow settings"
                  className={buttonClass}
                  onClick={() => setWorkflowSettingsOpen(false)}
                >
                  <X className="size-4" />
                </button>
              </div>
              <div className="grid gap-4 lg:grid-cols-[minmax(180px,0.7fr)_minmax(0,1.3fr)]">
                <div className="space-y-3">
                  <label className="block text-sm">
                    Workflow entity
                    <select
                      aria-label="Workflow entity"
                      className={cn(inputClass, "mt-1")}
                      value={graph.entityKey}
                      disabled={!canEdit || busy}
                      onChange={(e) => {
                        setGraph((g) => ({ ...g, entityKey: e.target.value }));
                        setDirty(true);
                      }}
                    >
                      {metadata.entities.map((e) => (
                        <option key={e.key} value={e.key}>
                          {e.title}
                        </option>
                      ))}
                    </select>
                  </label>
                  <div className="rounded-lg border border-slate-100 bg-slate-50 p-3">
                    <div className="[&_details]:hidden">
                      <WorkflowTriggerSettings
                        graph={graph}
                        metadata={metadata}
                        disabled={!canEdit || busy}
                        onChange={(g) => {
                          setGraph(g);
                          setDirty(true);
                        }}
                      />
                    </div>
                  </div>
                </div>
                <div className="rounded-lg border border-slate-100 bg-slate-50 p-3">
                  <WorkflowTriggerScopeSettings
                    graph={graph}
                    metadata={metadata}
                    disabled={!canEdit || busy}
                    onChange={(g) => {
                      setGraph(g);
                      setDirty(true);
                    }}
                  />
                </div>
              </div>
            </section>
          </div>
        )}
        {workflowDialog && (
          <div className="fixed inset-0 z-50 grid place-items-center overflow-y-auto bg-slate-950/40 p-4">
            <form
              role="dialog"
              aria-modal="true"
              aria-labelledby="workflow-details-title"
              className="w-full max-w-lg space-y-4 rounded-xl bg-white p-6 shadow-xl"
              onSubmit={(e) => {
                e.preventDefault();
                if (dialogName.trim()) submitWorkflowDialog();
              }}
            >
              <div className="flex items-center justify-between">
                <div>
                  <h2 id="workflow-details-title" className="font-semibold">
                    {workflowDialog === "new" ? "New WFL" : "Edit WFL"}
                  </h2>
                  <p className="mt-1 text-sm text-slate-500">
                    {workflowDialog === "new"
                      ? "Start a new workflow draft."
                      : "Update the saved workflow details."}
                  </p>
                </div>
                <button
                  type="button"
                  aria-label="Close workflow details dialog"
                  className={buttonClass}
                  onClick={() => setWorkflowDialog(null)}
                >
                  <X className="size-4" />
                </button>
              </div>
              <label className="block text-sm font-medium">
                WFL name
                <input
                  autoFocus
                  required
                  className={cn(inputClass, "mt-1")}
                  value={dialogName}
                  maxLength={160}
                  onChange={(e) => setDialogName(e.target.value)}
                />
              </label>
              <label className="block text-sm">
                Description
                <textarea
                  className={cn(inputClass, "mt-1")}
                  value={dialogDescription}
                  maxLength={500}
                  onChange={(e) => setDialogDescription(e.target.value)}
                />
              </label>
              <div className="flex justify-end gap-2">
                <button
                  type="button"
                  className={buttonClass}
                  onClick={() => setWorkflowDialog(null)}
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className={primaryClass}
                  disabled={!dialogName.trim()}
                >
                  {workflowDialog === "new" ? "Create draft" : "Save details"}
                </button>
              </div>
            </form>
          </div>
        )}
        {executeOpen && (
          <div className="fixed inset-0 z-50 grid place-items-center overflow-y-auto bg-slate-950/40 p-4">
            <section
              role="dialog"
              aria-modal="true"
              aria-labelledby="workflow-run-title"
              className="w-full max-w-xl space-y-4 rounded-xl bg-white p-6 shadow-xl"
            >
              <div className="flex items-center justify-between">
                <h2 id="workflow-run-title" className="font-semibold">
                  {previewMode
                    ? "Test workflow without effects"
                    : "Run active workflow"}
                </h2>
                <button
                  aria-label="Close run dialog"
                  className={buttonClass}
                  onClick={() => setExecuteOpen(false)}
                >
                  <X className="size-4" />
                </button>
              </div>
              <p className="text-sm text-slate-600">
                {previewMode
                  ? "Tests this draft with real record reads and planned actions only."
                  : "Executes the active published version. Select one target or multiple targets for a bulk run."}
              </p>
              <label className="block text-sm">
                Search records
                <input
                  className={cn(inputClass, "mt-1")}
                  value={search}
                  maxLength={160}
                  onChange={(e) => {
                    setSearch(e.target.value);
                    setTarget("");
                  }}
                />
              </label>
              <label className="block text-sm">
                Target record
                <select
                  className={cn(inputClass, "mt-1")}
                  value={target}
                  onChange={(e) => setTarget(e.target.value)}
                >
                  <option value="">Choose a record</option>
                  {records.map((row) => (
                    <option key={row.id} value={row.id}>
                      {row.label}{row.values.status ? ` · ${row.values.status}` : ""}
                    </option>
                  ))}
                </select>
              </label>
              {!previewMode && (
                <label className="block text-sm">
                  Bulk targets
                  <select
                    multiple
                    className={inputClass}
                    value={bulkTargets}
                    onChange={(e) =>
                      setBulkTargets(
                        Array.from(e.target.selectedOptions).map(
                          (o) => o.value,
                        ),
                      )
                    }
                  >
                    {records.map((r) => (
                      <option key={r.id} value={r.id}>
                        {r.label}
                      </option>
                    ))}
                  </select>
                </label>
              )}
              <button
                className={primaryClass}
                disabled={busy || (!target && !bulkTargets.length)}
                onClick={() =>
                  void action(async () => {
                    if (previewMode) {
                      setPreview(
                        await api("/test", "POST", { graph, recordId: target }),
                      );
                      setExecuteOpen(false);
                      return;
                    }
                    if (bulkTargets.length) {
                      await api(
                        `/definitions/${selected!.definition.id}/bulk-execute`,
                        "POST",
                        { recordIds: bulkTargets, requestKey },
                      );
                      setExecuteOpen(false);
                      setView("history");
                      return;
                    }
                    const run = await api<Run>(
                      `/definitions/${selected!.definition.id}/execute`,
                      "POST",
                      { recordId: target, requestKey },
                    );
                    setExecuteOpen(false);
                    if (can("history")) {
                      setView("history");
                      setRunDetail(await api<RunDetail>(`/runs/${run.id}`));
                    }
                  }, "Workflow queued for execution.")
                }
              >
                {previewMode
                  ? "Test preview"
                  : bulkTargets.length
                    ? "Run selected records"
                    : "Run workflow"}
              </button>
              <p className="text-xs text-slate-500">
                Up to 100 matching records shown. Narrow the search if needed.
              </p>
            </section>
          </div>
        )}
        {preview !== null && (
          <section className="rounded-xl border bg-white p-5">
            <div className="flex justify-between">
              <h2 className="font-semibold">
                Test preview — no mutations or deliveries
              </h2>
              <button className={buttonClass} onClick={() => setPreview(null)}>
                Close
              </button>
            </div>
            <pre className="mt-3 max-h-96 overflow-auto whitespace-pre-wrap text-xs">
              {JSON.stringify(preview, null, 2)}
            </pre>
          </section>
        )}
        <p className="text-xs text-slate-500">
          Drafts remain separate from active versions. Automatic runs use the
          publishing user&apos;s current permissions. External delivery outcomes
          can be inspected under Connections & AI.
        </p>
      </main>
    </div>
  );
}
