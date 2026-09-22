# External coding and primary integration workflow

## Current working agreement

The user has since chosen direct implementation by the primary AI in small
15-minute chunks, with documentation and a pause for the user between chunks.
See ../design/MEMBERSHIP-BUILD-LOG.md. The external handoff procedure below is
retained as history and does not direct the current membership rebuild.

## Previous external handoff procedure

The user wants the external AI to write feature code and the primary AI to own
scope, architecture, review, integration and verification. Avoid duplicate coding.

- Dispatch the next independent, bounded task early while useful local review or
  integration continues. Give each task its own current baseline and file scope.
- Never dispatch a dependent task against changes that have not stabilised.
- Require a source-map checkpoint before the external AI implements unfamiliar
  modules. Verify actual symbols, paths and contracts first.
- Prefer complete changed files in a ZIP, preserving relative paths and including
  baseline file hashes. Git-generated patches are also acceptable.
- On return, perform a fast baseline/symbol/contract compatibility check first.
- Send substantial mismatches back with precise corrections; do not silently
  rewrite the feature locally. The primary AI may make small integration fixes.
- Run focused checks first. Broaden checks for shared contracts or unresolved
  concerns, and distinguish failures in unchanged areas from feature failures.
- Keep a concise record of accepted work and pending limitations.

Task 1 membership pagination is integrated. Task 2A is Entra account resolution;
it is deliberately smaller than the full production authentication workstream.
