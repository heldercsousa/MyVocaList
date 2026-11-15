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


## Moved from BACKLOG.md (2026-07-15) — Hamburger menu on all hamburger-loaded pages

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-11 | ↳ Hamburger menu on all hamburger-loaded pages | ✅ Done (CRUD-only) | **Scope narrowed (Helder 2026-07-11): CRUD-only** (Venues/Singers/Artists/Songs list pages) — Shell-native pages (Events/Settings/Backup/About) deferred to *AppBar / SearchAppBar Interaction Redesign* (cross-ref point 1). Spike rejected Shell-native `//route` (broke forward slide + hardware back → app exit; findings.md). Shipped: always-hamburger in `CrudListPageBase.OnNavigatedTo` (`a0b999f`) + icon-glyph-suffix fix (`2f4b1e4`, `"menu"` → `"menu_outlined"`, found during Helder's E2E pass). `PushAsync`/animations untouched. spec-reviewer PASS 2026-07-11; verifier PASS. Final visual glyph confirmation on emulator deferred to Helder at his convenience (non-blocking). **Spec:** `Docs/Management/DevCycleCraft/hamburger-nav-pattern/` (findings + requirements + design + tasks + task-log). |


## 2026-11-15 — Change: hamburger in the Shell-native navigation slot (remove the duplicate icon)

**Status:** To Review — waiting for Helder's device pass.
**Spec:** `changes/2026-11-15-hamburger-in-the-shell-native-navigation-slot-remove-the-duplicate-icon/README.md`
**Branch / worktree:** `fix/hamburger-native-slot` (from `develop`). Lane: main agent, inline (pre-authorised by Helder 2026-11-15).

### Changed files
- `MyVocaList/UI/Pages/Base/CrudListPageBase.cs` — owns a `BackButtonBehavior`, attached in `AttachViewModel()`; `SyncNavigationBehavior()` copies the ViewModel icon/command into it on `PropertyChanged`; hamburger assignment extracted to `ApplyHamburgerNavigation()` and also run before the first frame.
- `MyVocaList/UI/Pages/Venues/VenuesPage.xaml`, `People/PeoplePage.xaml`, `Artists/ArtistsPage.xaml`, `Songs/SongsPage.xaml` — removed `<Shell.BackButtonBehavior IsVisible=False IsEnabled=False>` and the `NavigationIcon` / `NavigationCommand` attributes of `SmallAppBar`.
- `SmallAppBar` not edited (governed component untouched).

### AC traceability matrix

| AC ID | Criterion | Implementation location | Test method |
|---|---|---|---|
| REQ-HNAV-01′ | Exactly one leading icon (hamburger) in the native slot, at any depth | `CrudListPageBase.AttachViewModel` / `SyncNavigationBehavior` | Emulator: screenshots at cold start, Singers (pushed), back to root; UI dump shows a single toolbar `ImageButton` (x 0–154) on Venues root, Artists, Songs, Venues-via-menu |
| REQ-HNAV-02′ | Tap opens the drawer | `_openDrawerCommand` | Emulator: tap at root and on pushed Singers → drawer shown |
| REQ-HNAV-03 | Hardware back unchanged | untouched `OnBackButtonPressed` overrides | Emulator: device back on pushed Singers → pops to Venues; on root Venues → "Exit MyVocaList?" sheet; form → device back and on-screen back → Venues with one icon |
| REQ-HNAV-04 | No hand-written animation | none added | Code review of the diff |
| REQ-HNAV-05 | Songs catalog mode shows the back arrow in the native slot | `SyncNavigationBehavior` follows `SongsViewModel.ApplyAppBarNavigation` | Emulator, after merging `develop` (BUG-086 merged) into the branch: Artists → "View catalog" → back arrow in the native slot, single toolbar button; tap pops to Artists |

Level C (page lifecycle, `Shell.Current`) — no unit test, per `testing.md`.

### Verification evidence
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` → 0 errors (27 pre-existing NuGet/DX warnings).
- `dotnet test` → 1652 passed, 0 failed.
- Android emulator (dark theme), build installed from the worktree: all rows above (REQ-HNAV-05 on the build that includes `develop` at `dd42b143`).
- Not checked: light theme tint, delete-confirm sheet dismiss on device back, a physical device.
- First attempt (XAML binding on `BackButtonBehavior`) failed on the emulator — dead back arrow; see the spec's "Rejected" note.

### Manual device checklist (Helder)
- [ ] Cold start: one hamburger, left slot, opens the drawer
- [ ] Each list page from the menu: one hamburger, no back-arrow flash
- [ ] Device back: pushed list pops; root Venues asks to exit; delete-confirm sheet dismisses first
- [ ] Light theme: icon tint readable
- [ ] Artists → catalog shows a back arrow that pops (emulator-checked by Claude 2026-11-15)
