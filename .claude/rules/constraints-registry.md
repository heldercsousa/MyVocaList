# Constraint Registry — Routing Table

> Full detail for every constraint: `.claude/library/constraints-reference.md` (`##` sections: DevExpress/UI · .NET MAUI · EF Core/SQLite · Visual Studio Solution · Design/Prototyping · How to add entries), loaded via the `myvocalist-coding` skill map. The `##` headings below are cross-referenced by `§` anchor; the HARD RULE/HARD GATE lines stay inline.

Other areas: DevExpress / UI (BindableLayout vs DXCollectionView, native dialogs, selection-after-reload, no-Windows) → `§ DevExpress / UI`; .NET MAUI (SafeAreaEdges, incremental XAML edits) → `§ .NET MAUI`; Stitch/Figma evaluations → `§ Design / Prototyping Tools`; adding a newly discovered constraint → `§ How to add entries`.

## EF Core / SQLite

Full detail (MigrationsLock, CollationInterceptor, first-run table absence, Microsoft.Data.Sqlite sync-async freeze + SQLite-temporary REVERT marker): `library/constraints-reference.md § EF Core / SQLite`. Never-miss:

- **No C#-side string normalization for search/deduplication — HARD RULE (all relational DBs):** never `ToLowerInvariant()`/`ToUpperInvariant()`/`RemoveDiacritics()` or `*Normalized` shadow columns for search/uniqueness/dedup. Use DB collation (`.UseCollation(CollationConstants.Default)` + `EF.Functions.Collate(...)`); let the engine normalize at query time using its indexes. Rationale in the library.

## Visual Studio Solution (.sln)

- **Solution item registration — HARD GATE:** any file created, moved, or deleted in `Docs/` or `.claude/` **must** be reflected in `MyVocaList.sln` in the same commit (checked in the subagent exit checklist; a PostToolUse hook auto-registers new `Docs/` files written via `Write`). In practice the gate applies to `Docs/` files — `.claude/library/*` and `.claude/rules/*` are NOT `.sln`-registered.
- Common case: add `RelativePath\file = RelativePath\file` to the matching Solution Folder's `ProjectSection(SolutionItems)`. **New-folder pattern, `NestedProjects` parent GUIDs, and the sequential GUID counter** are in `library/constraints-reference.md § Visual Studio Solution (.sln)` — the counter's **only** source of truth. Never restate its value; derive it from the `.sln` with the one-liner given there.
