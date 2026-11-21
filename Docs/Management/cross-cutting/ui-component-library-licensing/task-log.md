# Task log — UI component library licensing

## 2026-11-21 — Syncfusion POC (spike, throwaway)

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
