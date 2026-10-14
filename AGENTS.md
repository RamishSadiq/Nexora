# Project boundaries

Nexora is a standalone project. Keep all Nexora source and project-specific documentation in this repository. Do not edit the separate Sales API project as part of Nexora work. New unrelated applications must have their own directories and Git repositories. Shared code must be copied here or added as an explicit versioned dependency; never depend on a sibling checkout.

# Beginner documentation maintenance

The user requires the complete beginner documentation to stay current with future platform changes. For changes to Nexora implementation or selected runtime configuration, review and update the matching teaching catalogue, code map, exercise and stated limits in scripts/docs. Use scripts/docs/Sync-PlatformGuides.py --check and --refresh to update the maintained HTML, Word and Markdown manuals under docs/beginner-guides and preserve the previous complete copy in history.

Follow docs/beginner-guides/DOCUMENTATION-MAINTENANCE.md. Verify source copies and offline browser behaviour, then render and visually review the affected Word outputs before marking the collection reviewed. If documentation is pending or cannot be refreshed, report that explicitly instead of claiming it is current. Do not overwrite unrelated source changes or commit them with a documentation update.

The complete manual also includes a versioned documentation snapshot of the separate Company Dash application. Read its source only through the documentation capture workflow; keep its application code and runtime history in its own repository. This introduces no Nexora runtime dependency on Company Dash or any sibling checkout.
