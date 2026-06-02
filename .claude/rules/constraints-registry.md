# Constraint Registry — MyVocaList

Discovered during implementation. Supersedes documented best practices where listed.
Review before implementing features in the indicated area.

---

## DevExpress / UI

- **BindableLayout vs DXCollectionView in ScrollView forms:** For small inline lists embedded in a `ScrollView`-based form page, use `BindableLayout.ItemsSource` on a `VerticalStackLayout` — not `DXCollectionView`. `DXCollectionView` inside a `ScrollView` requires workarounds (`IsScrollable="False"` + fixed height). Established by `SongFormPage.xaml` (ApiResults section pre-existing, YouTube URLs section 2026-05-30).
- **ObservableRangeCollection / DXCollectionView reset events:** see `code-principles.md § UI Thread Performance — ObservableRangeCollection`.
- **Native dialogs:** Do NOT use `DisplayAlert`, `DisplayActionSheet`, or `DisplayPromptAsync`. Use `dx:BottomSheet` only. (CLAUDE.md)
- **Selection after reload:** After a list refresh or search, clear selection (`ClearRange` + `SelectedCount = 0`). Never restore prior selection via `ReplaceRange` — it fires a second Reset and crosses a data-reload boundary. (code-principles.md)

---

## .NET MAUI

- **SafeAreaEdges default:** `.NET MAUI 10` breaking change — `ContentPage` defaults to `SafeAreaEdges="None"`. Every `ContentPage` must declare `SafeAreaEdges="Container"` explicitly or content will render behind the status bar / notch. (CLAUDE.md)
- **Incremental XAML edits:** Edit one XAML file → build → fix before editing the next. Rationale: XAML parser errors cascade across files, making the error source ambiguous when batching changes. (CLAUDE.md Constitutional Constraints)

---

## EF Core / SQLite

- **MigrationsLock:** The `__EFMigrationsLock` row must be cleared before each `MigrateAsync()` call (SQLite single-user workaround). Omitting this causes a hang on second launch.
- **CollationInterceptor:** Must be applied globally for case-insensitive search (`LIKE`) to work correctly. Without it, `ExistsByNameAsync` and search queries are case-sensitive.
- **First-run table absence:** `DELETE FROM __EFMigrationsLock` will throw on first run (table does not exist). Wrap in a bare `catch { }` — see code-principles.md exception patterns.
- **No C#-side string normalization for search/deduplication — HARD RULE (all relational DBs):** Never call `ToLowerInvariant()`, `ToUpperInvariant()`, `RemoveDiacritics()`, or any equivalent C#-side normalization in services, repositories, or entity constructors for the purpose of search, duplicate detection, or uniqueness. Never add `*Normalized` shadow columns (properties or DB columns whose sole purpose is storing a lowercased/accent-stripped variant of another column). This rule applies to any relational database, not just SQLite. Two reasons: (1) correctness — `ToLowerInvariant()` handles case only; "café" ≠ "cafe", so accent-insensitive matching silently fails; (2) performance — normalizing strings in C# forces a full table scan on every query because the DB cannot use an index on a computed value it never sees. The correct pattern: configure the appropriate case- and accent-insensitive collation on searchable/unique columns in EF entity configurations; use `EF.Functions.Collate()` in LINQ queries; let the DB engine handle normalization at query time using its own indexes. For SQLite specifically: declare `.UseCollation(CollationConstants.Default)` in entity config and use `EF.Functions.Collate(column, CollationConstants.Default)` in queries, where `CollationConstants` lives in that Infra layer; there is no shared cross-Infra collation abstraction.

---

## Visual Studio Solution (.sln)

- **Solution item registration — HARD GATE:** Any file created, moved, or deleted in `Docs/` or `.claude/` **must** be reflected in `MyVocaList.sln` in the same commit. Missing entries make files invisible in VS Solution Explorer. This is not optional and is checked in the subagent exit checklist.

  **When to act:**
  - **Create** a file → add `RelativePath\file = RelativePath\file` to the matching Solution Folder's `ProjectSection(SolutionItems)`. If no folder exists for the feature yet, create one with a new GUID.
  - **Move** a file → remove the old path entry, add the new path entry.
  - **Delete** a file → remove its entry from the `.sln`.

  **New Solution Folder pattern** (copy when adding a folder for a new feature):
  \\\${nl}  Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "feature-name", "feature-name", "{NEW-GUID}"
  	ProjectSection(SolutionItems) = preProject
  		Docs\Management\[Path]\file.md = Docs\Management\[Path]\file.md
  	EndProjectSection
  EndProject
  \\\${nl}  Then add a `NestedProjects` entry in `GlobalSection(NestedProjects)`:
  - Under `BusinessFeatures` → parent `{8AB01C9F-E0FD-49D5-AE2C-E27AD8C8F05D}`
  - Under `DevCycleCraft` → parent `{0C4BA720-519E-4818-BD9B-34AC19E4FCD7}`
  - Under `Management` root → parent `{15F1DA03-2180-47BF-BC40-1BB457C97F9E}`

  **GUIDs:** Use sequential pattern `{FA1234BC-0001-4000-8000-00000000XXXX}` incrementing from the last used value (currently `0014`). Check the `.sln` before picking the next number.

---

## How to add entries

When a session discovers a new constraint — a DevExpress behavior, an EF Core migration limit, a MAUI platform quirk, a SQLite performance requirement — add an entry here before ending the session:

1. Place it under the appropriate section header (create one if needed).
2. One bullet per constraint. Include the symptom, the rule, and a reference file in parentheses if one exists.
3. Commit the update as part of the task commit (Rule 3).

Signs a constraint was discovered: "I found that…", "turns out…", "this fails because…", "we need to avoid…" appearing before a build fix.
