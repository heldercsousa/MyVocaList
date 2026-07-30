# Constraint Registry — Routing Table

> **This file is a routing table.** Full detail for every constraint lives in `.claude/library/constraints-reference.md` (one cohesive file, `##` sections: DevExpress/UI · .NET MAUI · EF Core/SQLite · Visual Studio Solution · Design/Prototyping · How to add entries), loaded on demand via the `myvocalist-coding` skill map. Discovered during implementation; supersedes documented best practices where listed. The two `##` headings below are preserved because other files link to them by `§` anchor — the HARD RULE/HARD GATE lines stay inline (never-miss); everything else is in the library.

| Constraint area | Source |
|-----------------|--------|
| DevExpress / UI (BindableLayout vs DXCollectionView, native dialogs, selection-after-reload, no-Windows) | `library/constraints-reference.md § DevExpress / UI` |
| .NET MAUI (SafeAreaEdges, incremental XAML edits) | `library/constraints-reference.md § .NET MAUI` |
| Design / Prototyping tools (Stitch, Figma MCP evaluations) | `library/constraints-reference.md § Design / Prototyping Tools` |
| How to add a newly discovered constraint | `library/constraints-reference.md § How to add entries` |

## EF Core / SQLite

Full detail (MigrationsLock, CollationInterceptor, first-run table absence, Microsoft.Data.Sqlite sync-async freeze + SQLite-temporary REVERT marker): `library/constraints-reference.md § EF Core / SQLite`. Never-miss:

- **No C#-side string normalization for search/deduplication — HARD RULE (all relational DBs):** never `ToLowerInvariant()`/`ToUpperInvariant()`/`RemoveDiacritics()` or `*Normalized` shadow columns for search/uniqueness/dedup. Use DB collation (`.UseCollation(CollationConstants.Default)` + `EF.Functions.Collate(...)`); let the engine normalize at query time using its indexes. Rationale (correctness: "café" ≠ "cafe"; performance: C#-side normalization forces full table scans) — full text in the library.

## Visual Studio Solution (.sln)

- **Solution item registration — HARD GATE:** any file created, moved, or deleted in `Docs/` or `.claude/` **must** be reflected in `MyVocaList.sln` in the same commit (missing entries make files invisible in VS Solution Explorer; checked in the subagent exit checklist). In practice the gate applies to `Docs/` files — `.claude/library/*` and `.claude/rules/*` are NOT `.sln`-registered.
- Common case: add `RelativePath\file = RelativePath\file` to the matching Solution Folder's `ProjectSection(SolutionItems)`. **New-folder pattern, `NestedProjects` parent GUIDs, and the sequential GUID counter** are in `library/constraints-reference.md § Visual Studio Solution (.sln)` — the counter's **only** source of truth. Never restate its value here or anywhere else; derive it from the `.sln` with the one-liner given there.

> **Authorship note:** Human-reviewed and approved by Helder 2026-07-09 (CLAUDE.md § Continuous Enhancement — Authorship). Full content preserved in `library/constraints-reference.md`.
