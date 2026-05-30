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

---

## Visual Studio Solution (.sln)

- **Solution item registration:** Any file that should be visible in VS Solution Explorer must be listed in `MyVocaList.sln` under the appropriate Solution Folder (`ProjectSection(SolutionItems) = preProject`). Pattern: `RelativePath\file.md = RelativePath\file.md`. Missing entries do not cause build failures but make files invisible in VS. Add as part of the task that creates the file — not as a follow-up.

---

## How to add entries

When a session discovers a new constraint — a DevExpress behavior, an EF Core migration limit, a MAUI platform quirk, a SQLite performance requirement — add an entry here before ending the session:

1. Place it under the appropriate section header (create one if needed).
2. One bullet per constraint. Include the symptom, the rule, and a reference file in parentheses if one exists.
3. Commit the update as part of the task commit (Rule 3).

Signs a constraint was discovered: "I found that…", "turns out…", "this fails because…", "we need to avoid…" appearing before a build fix.
