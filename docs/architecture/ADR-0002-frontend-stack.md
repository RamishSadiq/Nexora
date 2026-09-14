# ADR-0002: Use Next.js and TypeScript for the web application

- Status: Accepted
- Date: 2026-09-11

## Context

The product requires a polished, accessible interface, complex data grids, large forms, responsive record workspaces, saved client preferences, and a sustainable component system. The existing repository contains an ASP.NET Core API but no established frontend architecture.

## Decision

Use Next.js App Router, React, and TypeScript. Use token-based Tailwind CSS styling and accessible headless component primitives. Organise code by domain feature and generate the API client from OpenAPI.

## Consequences

- Backend and frontend use different languages but each uses the strongest ecosystem for its responsibility.
- The generated client and contract tests become important safeguards.
- Shared UI behaviour belongs in the design system; domain-specific behaviour remains in feature folders.
