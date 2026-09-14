# Nexora

Nexora is a modern modular SaaS operations platform covering CRM, membership, events, learning, examinations, sales, finance, engagement, tasks, querying, and reporting.

This standalone repository contains the complete Nexora platform source, tests, migrations, documentation and CI. It has no runtime or source dependency on the separate Sales API repository.

Start with the [complete product blueprint](docs/blueprint/NEXORA_PRODUCT_BLUEPRINT.md) and the decisions in [docs/architecture](docs/architecture).

## Delivery status

- [x] Stage 0 — Product blueprint and initial architecture decisions
- [x] Stage 1 — Platform shell
- [ ] Stage 2 — Identity and tenancy (first foundation slice implemented; Entra OIDC pending)
- [x] Stage 3 — CRM foundation (core workflows; production acceptance remains)
- [x] Stage 4 — Membership core
- [x] Stage 5 — Events, learning, and examinations core
- [x] Stage 6 — Sales and finance core
- [x] Stage 7 — Engagement and work core
- [ ] Stage 8 — Insights and operational foundation implemented; production acceptance pending

Core delivery does not imply production release approval. See the [release runbook](docs/operations/RELEASE-RUNBOOK.md) and stage notes for remaining scope and acceptance gates.

## Run the platform

Requirements: Node.js 24+, npm 11+, and .NET SDK 10.0.401 or a compatible later patch.

```powershell
npm --prefix apps/web install
npm run dev:web
```

In a separate terminal:

```powershell
dotnet run --project backend/src/Nexora.Api
```

To create the first local administrator, set a development-only password before running the API:

```powershell
$env:Identity__SeedAdminPassword = '<choose-a-strong-local-password>'
dotnet run --project backend/src/Nexora.Api
```

Local sign-in is enabled only in Development through `Identity:Provider:LocalDevelopmentEnabled`. Entra configuration is reserved for the next adapter and requires no credentials for this slice. The seeded email is `admin@nexora.local`. The web application runs at `http://localhost:3000`, the development API runs at `http://localhost:5080`, and the API health endpoint is `/health`.

Verification commands:

```powershell
npm run lint:web
npm run build:web
dotnet test backend/Nexora.slnx --configuration Release
```

The Relationships workspace now supports the Contact and Account lifecycle, related data, personal saved views and typed custom fields. See [Stage 3 implementation and verification](docs/stages/STAGE-03-CRM-FOUNDATION.md).

Stage-specific implementation notes are recorded in [docs/stages](docs/stages).
