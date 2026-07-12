# Hamburger on CRUD List Pages — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the AppBar leading icon on all 4 CRUD list pages (Venues, Singers/People, Artists, Songs) unconditionally the hamburger, replacing the broken `NavigationStack.Count <= 1` heuristic that always evaluates false for these pages (since `AppShellViewModel.NavigateAsync` always pushes them onto the Queue root, stack depth 2).

**Architecture:** Single-file change to `CrudListPageBase.OnNavigatedTo`. Removes the `isRootPage` conditional and its dead `arrow_back_outlined`/`GoToAsync("..")` branch; keeps the existing `"menu"` + `FlyoutIsPresented = true` assignment, now applied unconditionally whenever `ListViewModel is ICrudListViewModel vm`. No other file changes. No animation code touched (`PushAsync` and hardware-back handling are untouched — see `findings.md` for why `//route` migration was rejected).

**Tech Stack:** .NET MAUI 10, C# 13, DevExpress MAUI v25.2.4 (`SmallAppBar` — not edited, only the VM values it binds to change).

## Global Constraints

- Scope is CRUD-only: exactly 4 pages — `VenuesPage`, `PeoplePage` (Singers), `ArtistsPage`, `SongsPage` — all confirmed (Explore grep) as the only classes deriving `CrudListPageBase`. No other consumer exists; do not widen scope.
- No animation may be hand-written or introduced (`TranslateTo`, custom Shell transitions, etc.). `PushAsync` forward slide and OS-driven hardware back stay exactly as they are today.
- No migration to Shell `//route` sections — spike (`findings.md`) proved this breaks the forward slide and hardware back.
- `SmallAppBar` (governed component) is not edited — only the `ICrudListViewModel.AppBarNavigationIcon` / `AppBarNavigationCommand` values bound to it change.
- `OnBackButtonPressed` and `Shell.BackButtonBehavior IsVisible/IsEnabled=False` on these pages are untouched (REQ-HNAV-03 depends on this).
- Verification is manual E2E on the Android emulator (`Shell.Current` is null in unit tests) — no automated test for this change; Level C/UI per `testing.md`.
- Preserve the existing `PHASE2-INSTRUMENTATION` logging block in `OnNavigatedTo` verbatim — unrelated instrumentation, out of scope to remove.

---

## Task 1: Unconditional hamburger in `CrudListPageBase.OnNavigatedTo`

**Files:**
- Modify: `MyVocaList/UI/Pages/Base/CrudListPageBase.cs:56-80` (the `OnNavigatedTo` override)

**Interfaces:**
- Consumes: `ICrudListViewModel.AppBarNavigationIcon` (`string`, get/set) and `ICrudListViewModel.AppBarNavigationCommand` (`ICommand`, get/set) — defined at `MyVocaList/UI/ViewModels/ICrudListViewModel.cs:6-7`. Unchanged, already in place.
- Produces: nothing consumed by later tasks — this is the only code task in this plan.

### Current code (verbatim, for reference — do not copy into the file, only the replacement below goes in)

```csharp
protected override void OnNavigatedTo(NavigatedToEventArgs args)
{
    base.OnNavigatedTo(args);
    // PHASE2-INSTRUMENTATION: remove after page-load-frozen is closed.
    var navigatedMs = ElapsedMs(_ctorTimestamp);
    Serilog.Log.ForContext("SourceContext", GetType().Name)
        .Information("[PageLoad] {Page} navigatedTo={Ms}ms (ctor→OnNavigatedTo)", GetType().Name, navigatedMs);

    var isRootPage = Shell.Current?.Navigation?.NavigationStack?.Count <= 1;
    if (ListViewModel is ICrudListViewModel vm)
    {
        if (isRootPage)
        {
            vm.AppBarNavigationIcon = "menu";
            vm.AppBarNavigationCommand = new Command(
                () => Shell.Current.FlyoutIsPresented = true);
        }
        else
        {
            vm.AppBarNavigationIcon = "arrow_back_outlined";
            vm.AppBarNavigationCommand = new Command(
                async () => await Shell.Current.GoToAsync(".."));
        }
    }
}
```

- [ ] **Step 1: Pre-edit grep gate — re-confirm no 5th consumer exists**

Run:
```bash
grep -rn ": CrudListPageBase" --include=*.cs MyVocaList/
```
Expected: exactly 4 matches — `VenuesPage.xaml.cs`, `SongsPage.xaml.cs`, `ArtistsPage.xaml.cs`, `PeoplePage.xaml.cs`. (Already confirmed by the Explore pass at plan-writing time; re-run immediately before editing in case anything changed on disk since.) If a 5th match appears, STOP and escalate — do not proceed with an unconditional change until Helder confirms the new page's classification.

- [ ] **Step 2: Replace the conditional with the unconditional assignment**

Replace lines 56-80 of `MyVocaList/UI/Pages/Base/CrudListPageBase.cs` with:

```csharp
protected override void OnNavigatedTo(NavigatedToEventArgs args)
{
    base.OnNavigatedTo(args);
    // PHASE2-INSTRUMENTATION: remove after page-load-frozen is closed.
    var navigatedMs = ElapsedMs(_ctorTimestamp);
    Serilog.Log.ForContext("SourceContext", GetType().Name)
        .Information("[PageLoad] {Page} navigatedTo={Ms}ms (ctor→OnNavigatedTo)", GetType().Name, navigatedMs);

    if (ListViewModel is ICrudListViewModel vm)
    {
        // CRUD list pages are exclusively top-level menu destinations (see
        // hamburger-nav-pattern/design.md § Classification principle) → always the hamburger.
        // If a future feature ever pushes a CRUD list as a non-top-level sub-page, replace this
        // with an explicit "is my route in the top-level menu set?" check (design.md § Assumption).
        vm.AppBarNavigationIcon = "menu";
        vm.AppBarNavigationCommand = new Command(
            () => Shell.Current.FlyoutIsPresented = true);
    }
}
```

This removes the `isRootPage` local, the `else` branch (`arrow_back_outlined` / `GoToAsync("..")`), and the `Shell.Current?.Navigation?.NavigationStack?.Count <= 1` heuristic entirely.

- [ ] **Step 3: Build**

Run: `/sln-build` (or `dotnet build` targeting `net10.0-android`)
Expected: 0 errors, 0 new warnings.

- [ ] **Step 4: Post-edit re-read**

Read back `MyVocaList/UI/Pages/Base/CrudListPageBase.cs` in full and confirm:
- No leftover reference to `isRootPage` or `arrow_back_outlined` anywhere in the file.
- The `PHASE2-INSTRUMENTATION` block above the change is untouched.
- No stray `using` needed (none was required per the Explore pass — `Shell`/`Command` already resolve via global usings).

- [ ] **Step 5: Manual E2E verification (Helder, Android emulator) — do not check this step off yourself**

Deploy to the Android emulator and, for each of Venues, Singers (People), Artists, Songs:
1. Open the page from the flyout menu → leading icon must be the hamburger (`"menu"`), never the back arrow. (REQ-HNAV-01)
2. Tap the hamburger → the navigation drawer opens (`Shell.Current.FlyoutIsPresented == true`). (REQ-HNAV-02)
3. Press the Android hardware back button → behavior is unchanged from before this change: confirm-sheet dismiss if open → search-mode close if active → otherwise framework-default pop with its existing OS animation; the app must not exit unexpectedly from a list page. (REQ-HNAV-03)
4. Confirm forward navigation (menu tap → page open) still shows the framework-default slide animation, with no visual regression and no hand-written animation anywhere in the diff. (REQ-HNAV-04)

Record the outcome of all 4 checks × 4 pages in `task-log.md` (Step 7 below can be drafted before this, but the log entry is not final/committable until this verification evidence is filled in).

- [ ] **Step 6: AC traceability matrix + task-log entry**

Add to `Docs/Management/DevCycleCraft/hamburger-nav-pattern/task-log.md`:

```markdown
## Task 1 — Always-hamburger in CrudListPageBase.OnNavigatedTo

### Changed files
- `MyVocaList/UI/Pages/Base/CrudListPageBase.cs` — OnNavigatedTo: removed NavigationStack.Count<=1 conditional and dead back-arrow branch; unconditional hamburger assignment.

### AC traceability matrix

| AC ID | Criterion | Implementation location | Test method |
|---|---|---|---|
| REQ-HNAV-01 | Leading icon is hamburger, never back arrow, on all 4 CRUD list pages | `CrudListPageBase.cs` OnNavigatedTo (unconditional `vm.AppBarNavigationIcon = "menu"`) | Manual E2E — Android emulator, Helder observation |
| REQ-HNAV-02 | Tapping hamburger opens the drawer | `CrudListPageBase.cs` OnNavigatedTo (`AppBarNavigationCommand` → `Shell.Current.FlyoutIsPresented = true`) | Manual E2E — Android emulator, Helder observation |
| REQ-HNAV-03 | Hardware back behavior unchanged | Untouched: `OnBackButtonPressed` + `Shell.BackButtonBehavior` on each page | Manual E2E — Android emulator, Helder observation |
| REQ-HNAV-04 | No hand-written animation; forward slide unchanged | Untouched: `AppShellViewModel.NavigateAsync` PushAsync path | Manual E2E — Android emulator, Helder observation |

### Verification evidence
[Fill in after Step 5: pass/fail per page per check, any screenshots/notes from Helder's emulator observation.]
```

- [ ] **Step 7: Commit**

```bash
git add MyVocaList/UI/Pages/Base/CrudListPageBase.cs Docs/Management/DevCycleCraft/hamburger-nav-pattern/task-log.md
git commit -m "fix: CrudListPageBase — always show hamburger on CRUD list pages

Root cause: OnNavigatedTo picked the leading icon from NavigationStack.Count<=1,
which is only true for Queue; every menu-pushed CRUD list page (stack depth 2)
fell into the dead back-arrow branch.
Fix: CRUD list pages are exclusively top-level menu destinations, so the
classification collapses to unconditional hamburger; NavigationStack heuristic removed.
Regression risk: None — PushAsync, hardware back, and SmallAppBar untouched.

Closes REQ-HNAV-01..04 (hamburger-nav-pattern)."
```

---

## Task 2: Session-end BACKLOG + spec close-out

**Files:**
- Modify: `Docs/Management/BACKLOG.md` (the "Hamburger menu on all hamburger-loaded pages" row, 2026-07-11)
- Modify: `Docs/Management/DevCycleCraft/hamburger-nav-pattern/requirements.md` (flip the approval-gate line if not already `✅ Approved`)
- Modify: `Docs/Management/DevCycleCraft/hamburger-nav-pattern/tasks.md` (check off Task 1 and Task 2)

**Interfaces:** none — documentation only, no code interfaces.

**Produces:** closed-out BACKLOG/spec state (row marked done, tasks checked off, approval line correct).
**Consumes:** Task 1 completion (its commit + verification evidence in `task-log.md`).
**Demo:** BACKLOG row for "Hamburger menu on all hamburger-loaded pages" shows done/✅ with the CRUD-only note; `tasks.md` Task 1 and Task 2 both `[x]`.
**Review lane:** Standard (docs-only, no code review needed).

- [ ] **Step 1: Update BACKLOG status**

In `Docs/Management/BACKLOG.md`, update the "Hamburger menu on all hamburger-loaded pages" (2026-07-11) row to `✅` / done, with a one-line note: "CRUD-only fix shipped (B′ — see hamburger-nav-pattern/design.md); Shell-native menu pages (Events, Settings, Backup, About) deferred to AppBar/SearchAppBar Interaction Redesign (BACKLOG 2026-07-10)."

- [ ] **Step 2: Confirm out-of-scope capture**

Verify the 4 out-of-scope items in `requirements.md § Out of scope` are each traceable to an existing BACKLOG row (AppBar/SearchAppBar redesign, CRUD Form Action Pattern, navigation model). If any is missing a BACKLOG row, add a one-line 💡 row — do not expand scope of this feature to cover them.

- [ ] **Step 3: Check off tasks.md**

Mark Task 1 and Task 2 as `[x]` in `Docs/Management/DevCycleCraft/hamburger-nav-pattern/tasks.md`.

- [ ] **Step 4: Commit**

```bash
git add Docs/Management/BACKLOG.md Docs/Management/DevCycleCraft/hamburger-nav-pattern/requirements.md Docs/Management/DevCycleCraft/hamburger-nav-pattern/tasks.md
git commit -m "docs: hamburger-nav-pattern — close out BACKLOG + tasks after B' fix shipped"
```

---

## Notes for the implementor

- This is deliberately a **light-ceremony, single-production-file** change (workflow.md Rule 1 threshold: single file, < 1 hour). Task 1 is the only production-code task; Task 2 is docs-only.
- Do not touch `AppShellViewModel.NavigateAsync`, `SmallAppBar`, or any XAML — none of these are in scope and the design doc explicitly calls them out as untouched.
- Docs (this plan, task-log, BACKLOG, requirements/tasks updates) go to `develop`; the code change (Task 1) goes on `feat/hamburger-nav-pattern` per session instructions.
