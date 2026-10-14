# Maintaining the complete Nexora guide

Keep the current complete guide at its existing filename. It covers CRM, Portal, CMS, the public website, the Nexora backend and Company Dash. The HTML is the single offline file with full code; Word is the editable teaching edition. Older complete copies are kept as dated ZIP files in history, and Git preserves the collection's committed versions.

## What triggers an update

Compare the captured SHA256 fingerprints with current source in both projects. The checker detects changed, added and removed files and changes to the teaching catalogue or documentation tooling. It reads working files, including allowed uncommitted implementation changes. It does not read business records from SQL or SQLite and does not include private configuration, credentials, runtime data, dependencies or build output.

The current Company Dash source path is C:/Users/ramish/Documents/ChatGPT/Company Dash. Override NEXORA_DOCS_COMPANY_DASH_ROOT when this workstation path changes. The Company Dash code is read and copied into the documentation's source library with a company-dash prefix and its original path. It remains a separate application; Nexora has no runtime dependency on that directory. Company Dash source and task history are never modified by these documentation scripts.

## Maintainer workflow

1. Run the check command below. Read the changed paths and affected guides.
2. Review the actual source difference. Update scripts/docs/platform_guide_catalog.py or company_dash_catalog.py when behaviour, navigation, configuration, endpoints or file roles changed. Add a guide for a newly implemented feature instead of merely copying its code into the library.
3. Run refresh. It validates paths and exercise anchors before authoring. It archives the previous complete manual, then rebuilds current HTML, Word, Markdown, diagrams and manifests. A failed authoring run restores the prior collection from an internal backup.
4. Run structural and offline browser verification. Render the Word master and individual guides, inspect all pages and correct layout defects. The documentation skill's artifact marker and render requirements apply to each authoring run.
5. Run --reviewed only after source/browser checks and visual page review pass. maintenance-state.json records the captured version and its review state. Refresh alone leaves pending_review.
6. Commit only your documentation changes and approved instruction updates. Preserve unrelated application edits. A documentation update does not automatically commit, push, deploy or apply proposals.

```powershell
& 'C:\Users\ramish\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' scripts/docs/Sync-PlatformGuides.py --check
& 'C:\Users\ramish\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' scripts/docs/Sync-PlatformGuides.py --refresh
& 'C:\Users\ramish\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' scripts/docs/Verify-GuideStructure.py
$env:NEXORA_DOCS_NODE_MODULES='C:\Users\ramish\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\node_modules'
& 'C:\Users\ramish\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe' scripts/docs/Verify-PlatformGuide.mjs
& scripts/docs/Render-PlatformGuides.ps1 -IncludeChapters
& 'C:\Users\ramish\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' scripts/docs/Review-PlatformGuides.py
& 'C:\Users\ramish\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' scripts/docs/Sync-PlatformGuides.py --reviewed
```

The runtime paths are workstation-specific. Resolve current bundled runtimes when they change. Review-PlatformGuides.py renders internal page images and records overflow checks; a person or reviewing agent must still inspect the images. QA artifacts are ignored and are not deliverables.

## Automatic check

An hourly follow-up in the current Codex chat checks both source repositories. It remains quiet when unchanged. When source changes, it reviews affected explanations, preserves the previous copy, regenerates the current manuals and verifies them. It reports meaningful completion or a blocking failure. It does not send customer messages, alter application data, run proposals or push to Git.

Local scheduled runs need this computer on, the Codex app running and access to both source paths. The schedule is not an instantaneous file watcher. The Nexora AGENTS.md maintenance rule also requires documentation updates as part of code changes, so planned development should not wait for the hourly check.

## History and recovery

Each history ZIP names the previous source-capture time and a digest of its HTML file. It contains that complete offline manual, the Word and Markdown reading copies, source/coverage manifests and diagrams. Open the extracted HTML directly to read the older complete version. The original approved login guide remains separately preserved. Do not overwrite history files or label a pending rebuild as reviewed.
