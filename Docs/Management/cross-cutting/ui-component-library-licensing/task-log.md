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
| S1 | Context7: Syncfusion MAUI packages (Toolkit vs commercial suite), net10 versions, license-key behaviour | in progress | — |
| S2 | Duplicate `CrudListView` → `CrudListViewSf` (Syncfusion controls) | pending | — |
| S3 | Duplicate Venues page → `VenuesSfPage` using `CrudListViewSf`; route + DI | pending | — |
| S4 | Build net10.0-android, 0 errors | pending | — |
| S5 | Emulator run: screenshots of the DX vs Sf Venues pages (list, shimmer, selection, search, empty state, sheets) | pending | — |
| S6 | Spike results written into `findings.md` § Spike results | pending | — |
| S7 | Commit spike branch + push; docs commit on develop | pending | — |

**Context manifest** (≤ 8 files):
- `Docs/Management/cross-cutting/ui-component-library-licensing/README.md`
- `Docs/Management/cross-cutting/ui-component-library-licensing/findings.md`
- this file
