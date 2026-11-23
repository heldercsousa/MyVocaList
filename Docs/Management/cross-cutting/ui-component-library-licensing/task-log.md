# Task log — UI component library licensing

Closed entries are in the `task-log-archive-YYYY-MM.md` files beside this log.

## 2026-11-21 — Syncfusion POC (spike, throwaway) — Closed

Helder's directive (2026-11-21): do the POC. Write new code only; existing code stays unchanged. Duplicate existing code under different names so the two can be compared side by side. Work in a worktree, and keep every step's status here so the job can resume after an abrupt stop.

- **Branch:** `task/spike-syncfusion` (from `develop`), never merged to `develop`.
- **Worktree:** `C:\Users\helde\source\repos\MyVocaList\.claude\worktrees\spike-syncfusion`
- **Naming rule:** each duplicate carries the `Sf` suffix (`CrudListViewSf`, `VenuesSfPage`, …). The originals are never edited.
- **Allowed additive edits to shared files** (on the spike branch only):
  - package entries in `Directory.Packages.props` / the `.csproj`;
  - one Syncfusion registration call in `MauiProgram.cs`;
  - one DI line per new page;
  - one Shell route / flyout entry, so the copy can be reached.

### Checkpoint

**Resume rule:** read this block, then `git -C <worktree> log --oneline -10`, then continue at the first step not marked `done`.

| # | Step | Status | Evidence / notes |
|---|------|--------|------------------|
| S0 | Checkpoint log on develop + worktree created | done | worktree at `efd4cdf3` (develop tip 2026-11-21) |
| S1 | Context7: Syncfusion MAUI packages (Toolkit vs commercial suite), net10 versions, license-key behaviour | done | Context7 `/syncfusion/maui-toolkit`. **Syncfusion Toolkit for .NET MAUI** (`Syncfusion.Maui.Toolkit`, latest 1.0.12 on nuget.org, LICENSE.txt in package — MIT per GitHub repo) has BottomSheet, Buttons, Chips, Shimmer, TextInputLayout, Popup, Expander, NavigationDrawer, TabView, PullToRefresh, SegmentedControl, Picker, Cards. Registered with `.ConfigureSyncfusionToolkit()`; **no license key**. It has **no ListView** → the POC uses stock MAUI `CollectionView` for the list. Commercial `SfListView` (needs a key) is not used. Context7 has no entry for the commercial suite. |
| S2 | Duplicate `CrudListView` → `CrudListViewSf` (Syncfusion controls) | done | `cb570587`. New `CrudListViewSf` + `SearchBarSf` copies. Verified 2026-11-21: `git diff --numstat` shows 0 deleted lines; originals untouched |
| S3 | Duplicate Venues page → `VenuesSfPage` using `CrudListViewSf`; route + DI | done | `1f696102`. `VenuesViewModel` reused unchanged. Hidden route `venues-sf`; DEBUG-only launch hook in `AppShell.xaml.cs` opens it first |
| S4 | Build net10.0-android, 0 errors | done | 0 errors, 10 warnings, none from Syncfusion. Tests pass (1 flaky repository test passed on retry) |
| S5 | Emulator run: screenshots of the DX vs Sf Venues pages (list, shimmer, selection, search, empty state, sheets) | done (partial) | `1a9e9974`, `spike-screenshots/`. List + delete sheet OK. **Failed on Sf:** (a) `SfTextInputLayout`+`Entry` search — no EditText in the native tree, no keyboard; (b) row tap / long-press never reach the VM (cause unconfirmed). Shimmer not captured; back-closes-sheet not checked |
| S5b | Diagnose + fix (a) and (b) in the Sf copies only (≤3 attempts each); re-capture search / empty / selection; check back button closing the sheet | done | `21aa8bba` (pushed). (a) `SfTextInputLayout` never sized its `Entry` → replaced by a plain MAUI `Entry` (works). (b) XAML command binding never executed → forwarded from code-behind (tap opens the form; long-press selects). Back button matches DX. Shimmer still not captured. 0 deleted lines vs base re-checked |
| S6 | Spike results written into `findings.md` § Spike results | done | `findings.md` § 7. Orchestrator screenshot review added a white strip behind the FAB, and noted the Toolkit has no Autocomplete/ComboBox |
| S7 | Commit spike branch + push; docs commit on develop | done | spike branch pushed at `21aa8bba`; this docs commit on develop |

**POC closed 2026-11-21.** Emulator left running. The worktree is kept, so Helder can deploy the branch and compare: the DEBUG build opens "Venues (Syncfusion POC)" first, and the original Venues is in the flyout.

**Context manifest** (≤ 8 files; spike branch files are listed in `findings.md` § 7):
- `Docs/Management/cross-cutting/ui-component-library-licensing/README.md`
- `Docs/Management/cross-cutting/ui-component-library-licensing/findings.md`
- this file

## 2026-11-22 — Migration implementation (plan.md, tasks.md)

Plan approved by Helder 2026-11-22 ("plan approved. Start implementation"); autonomy grant the same day (any step inside this project folder, push included). Orchestrator ledger: `.superpowers/sdd/plan/progress.md` (gitignored).

### Checkpoint

**Resume rule:** read this block, then the ledger, then `git log --oneline -10`; continue at the first step not marked `done`.

| # | Step | Status | Evidence / notes |
|---|------|--------|------------------|
| M0 | Item 🟡; wave 1 claimed `[~]` | done | `794c9f10` |
| M1 | Wave 1: T0.1a, T0.1b, T0.1c (scratch worktrees `spike-sf-controls-a/b/c`), T0.2 (`check-dx-script`) | done | all four reported; results below |
| M2 | Review T0.2, merge to develop; apply T0.1 results to design.md | done | T0.2 merged `0ae29cea`; T0.1 results applied to design.md (this commit) |
| M3 | T0.3 packages + license target | done | merged `db9b6d90` |
| M4 | Wave: T0.4a theme + T0.6a preview registry | pending | |

**Context manifest:** `plan.md`, `tasks.md`, `design.md`, `requirements.md`, this file, `.superpowers/sdd/plan/progress.md`.

## T3.2 — `TapToEditFieldSf` — Done (2026-11-23)

Commit `54f35e72`. Review: spec ✅, quality Approved, 3 Minor findings. Merged.
- The copy keeps every BindableProperty and every state transition (tap, focus, blur, commit, cancel). `TapToEditFieldLogic` is shared and unchanged.
- `DXImage` became an `Image` with `IconTint`.
- Emulator proof is in `artifacts/parity/t3.2/`: tap enters edit mode, the keyboard opens, text binds, and commit and cancel work. Consumers set `ContainerType="Outlined"` on their `SfTextInputLayout`.

**Deviation, carried to T3.3 by orchestrator ruling:** the original's `AutocompleteDXField` handling is removed. That handling covered three things: it released `IsLocked` on tap, it had a special case in `FocusEditor`, and a pick returned the field to text state. `ReleasesPickOnEdit` stays in the API but does nothing for now. T3.3 restores the hook for `AutocompleteFieldSf` at three points in `TapToEditFieldSf`: `OnTextTapped`, `FocusWrappedEditor`, and `Attach`/`Detach`. For that, T3.3 owns `TapToEditFieldSf`.
