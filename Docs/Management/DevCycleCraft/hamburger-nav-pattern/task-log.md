# Task Log — Hamburger on CRUD list pages

## Task 1 — Always-hamburger in CrudListPageBase.OnNavigatedTo

### Changed files
- `MyVocaList/UI/Pages/Base/CrudListPageBase.cs` — OnNavigatedTo: removed NavigationStack.Count<=1 conditional and dead back-arrow branch; unconditional hamburger assignment (commit `a0b999f`).
- `MyVocaList/UI/Pages/Base/CrudListPageBase.cs` — follow-up fix: `AppBarNavigationIcon` value corrected from `"menu"` to `"menu_outlined"` (commit `2f4b1e4`). Root cause: icon resources are named with an `_outlined`/`_filled` suffix (only `menu_outlined.svg`/`menu_filled.svg` exist); `"menu"` resolved to no image, so the hamburger button rendered but its glyph was blank. Found during Helder's manual E2E pass; classified Minor (cosmetic — button, command, and drawer-open behavior were all already correct; only the glyph was missing) per `bug-tracking.md` — no regression test required, fix documented here as the task-log entry.

### AC traceability matrix

| AC ID | Criterion | Implementation location | Test method |
|---|---|---|---|
| REQ-HNAV-01 | Leading icon is hamburger, never back arrow, on all 4 CRUD list pages | `CrudListPageBase.cs` OnNavigatedTo (unconditional `vm.AppBarNavigationIcon = "menu"`) | Manual E2E — Android emulator, Helder observation |
| REQ-HNAV-02 | Tapping hamburger opens the drawer | `CrudListPageBase.cs` OnNavigatedTo (`AppBarNavigationCommand` → `Shell.Current.FlyoutIsPresented = true`) | Manual E2E — Android emulator, Helder observation |
| REQ-HNAV-03 | Hardware back behavior unchanged | Untouched: `OnBackButtonPressed` + `Shell.BackButtonBehavior` on each page | Manual E2E — Android emulator, Helder observation |
| REQ-HNAV-04 | No hand-written animation; forward slide unchanged | Untouched: `AppShellViewModel.NavigateAsync` PushAsync path | Manual E2E — Android emulator, Helder observation |

### Verification evidence
Structural fix (icon assignment, command wiring, unconditional hamburger) verified by independent verifier subagent — PASS, no blockers. Icon glyph visibility bug found and fixed post-verification (see Changed files). Final visual confirmation (hamburger glyph renders correctly on all 4 pages) deferred to Helder's own emulator check, to be done at his convenience — not blocking this task's closure per his instruction (2026-07-11). If a further issue is found, follow up as a new fix rather than reopening this entry.
