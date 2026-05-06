# Constraint Registry — MyVocaList

Discovered during implementation. Supersedes documented best practices where listed.
Review before implementing features in the indicated area.

---

## DevExpress / UI

- **DXCollectionView reset events:** `ReplaceRange` and `ClearRange` each fire `CollectionChanged(Reset)`, triggering a full re-render of all visible items. Never call both in the same `RunOnUiThread` block — two calls = two full render passes = ANR risk. (code-principles.md)
- **Native dialogs:** Do NOT use `DisplayAlert`, `DisplayActionSheet`, or `DisplayPromptAsync`. Use `dx:BottomSheet` only. (CLAUDE.md)
- **Selection after reload:** After a list refresh or search, clear selection (`ClearRange` + `SelectedCount = 0`). Never restore prior selection via `ReplaceRange` — it fires a second Reset and crosses a data-reload boundary. (code-principles.md)

---

## .NET MAUI

- **SafeAreaEdges default:** `.NET MAUI 10` breaking change — `ContentPage` defaults to `SafeAreaEdges="None"`. Every `ContentPage` must declare `SafeAreaEdges="Container"` explicitly or content will render behind the status bar / notch. (CLAUDE.md)

---

## EF Core / SQLite

- **MigrationsLock:** The `__EFMigrationsLock` row must be cleared before each `MigrateAsync()` call (SQLite single-user workaround). Omitting this causes a hang on second launch.
- **CollationInterceptor:** Must be applied globally for case-insensitive search (`LIKE`) to work correctly. Without it, `ExistsByNameAsync` and search queries are case-sensitive.
- **First-run table absence:** `DELETE FROM __EFMigrationsLock` will throw on first run (table does not exist). Wrap in a bare `catch { }` — see code-principles.md exception patterns.

---

## How to add entries

When a session discovers a new constraint — a DevExpress behavior, an EF Core migration limit, a MAUI platform quirk, a SQLite performance requirement — add an entry here before ending the session:

1. Place it under the appropriate section header (create one if needed).
2. One bullet per constraint. Include the symptom, the rule, and a reference file in parentheses if one exists.
3. Commit the update as part of the task commit (Rule 3).

Signs a constraint was discovered: "I found that…", "turns out…", "this fails because…", "we need to avoid…" appearing before a build fix.
