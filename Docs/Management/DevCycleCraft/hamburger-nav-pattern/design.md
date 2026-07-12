# Design — Hamburger on CRUD list pages (B′)

## Root cause (verified against develop)

- `AppShellViewModel.NavigateAsync`: Queue/Exit → `PopToRootAsync(animated:false)`; **all other destinations → `Navigation.PushAsync(page)`** onto the Queue root. So every menu-pushed page sits at `NavigationStack.Count == 2`.
- `CrudListPageBase.OnNavigatedTo` selects the leading icon from `NavigationStack.Count <= 1`:
  - `true` → `AppBarNavigationIcon = "menu"` + `AppBarNavigationCommand = () => Shell.Current.FlyoutIsPresented = true`
  - `false` → `AppBarNavigationIcon = "arrow_back_outlined"` + `AppBarNavigationCommand = () => GoToAsync("..")`
- Because the 4 CRUD list pages are always pushed (Count 2), the `false` branch always wins → back arrow. The hamburger branch is **correctly wired but unreachable**.

## Decision — B′ (keep `PushAsync`, fix only the icon)

The spike (`findings.md`) proved the Shell-native `//route` alternative breaks the mandatory forward slide **and** hardware back, and the only rescue (a custom Shell transition) is forbidden by the **no-hand-written-animation** rule. Therefore the animated `PushAsync` model is retained and only the leading-icon determination changes.

### Classification principle

> A page shows the hamburger iff it is a **top-level menu destination**; otherwise it shows the back arrow.

For the **4 in-scope CRUD list pages**, every one of them *is* a top-level menu destination (routes `venues`, `people`, `artists`, `songs` — all members of the flyout menu set). So for `CrudListPageBase` the principle **collapses to: always show the hamburger.** The fragile `NavigationStack.Count` heuristic is removed.

This is the most maintainable option Helder's constraints allow:
- **Stateless & deterministic** — no runtime stack inspection, no origin flag threaded through `PushAsync`.
- **No new hardcoded list** — it reuses the fact (already encoded in `NavigationConfig`'s menu set) that these routes are top-level.
- **Animations untouched** — `PushAsync` forward slide and the OS-animated hardware back are not modified.

### Change (single file)

`UI\Pages\Base\CrudListPageBase.cs` → `OnNavigatedTo`: replace the `isRootPage`/`Count <= 1` conditional with an unconditional hamburger assignment:

```csharp
if (ListViewModel is ICrudListViewModel vm)
{
    // CRUD list pages are top-level menu destinations → always the hamburger.
    // (See hamburger-nav-pattern/design.md § Classification principle.)
    vm.AppBarNavigationIcon = "menu";
    vm.AppBarNavigationCommand = new Command(() => Shell.Current.FlyoutIsPresented = true);
}
```

The now-dead `arrow_back_outlined`/`GoToAsync("..")` branch is removed. No other file changes.

## What is deliberately NOT changed

- **`SmallAppBar`** (governed component) — untouched. Only the VM property *values* it binds to change; no edit to the component's XAML/code-behind, so the four-gate component-change process does not apply to `SmallAppBar` itself.
- **`OnBackButtonPressed`** (hardware back) — untouched (REQ-HNAV-03).
- **`Shell.BackButtonBehavior IsVisible/IsEnabled=False`** on the CRUD pages — untouched (still needed so hardware back reaches `OnBackButtonPressed`).
- **`NavigateAsync`** — untouched; `PushAsync` retained.

## Consumer map — `CrudListPageBase` (base class, 4 consumers)

Grep basis: pages deriving `CrudListPageBase` + binding `AppBarNavigationIcon`/`AppBarNavigationCommand`.

| Consumer page | Reached from menu? | Risk of the change | Verification |
|---|---|---|---|
| `VenuesPage` | Yes (route `venues`) | None — desired: hamburger instead of back | E2E: open from menu → hamburger shows, tap → drawer opens |
| `PeoplePage` (Singers) | Yes (route `people`) | None — same | E2E: same |
| `ArtistsPage` | Yes (route `artists`) | None — same | E2E: same |
| `SongsPage` | Yes (route `songs`) | None — same | E2E: same |

All four have identical behavior; there is no consumer for which "always hamburger" is wrong under the current navigation graph. `CrudListPageBase` is a shared base class rather than a listed governed *component*; the consumer map + per-consumer risk is provided to honor the spirit of component-change-governance.

**Decision (Helder 2026-07-11):** `CrudListPageBase` is **NOT** added to the governed-component list. Instead it is documented as the standard List-page *pattern* (with exceptions such as the Autocomplete component) in `.claude/library/crud-pages.md § CrudListPageBase`. Changes follow normal spec + review and must update every List-page consumer consistently, but do not require the four-gate component-change ceremony.

## Assumption & future-proofing

The fix assumes the 4 CRUD list pages are exclusively top-level destinations. If a future feature pushes a CRUD list as a sub-detail (e.g. Songs filtered from an Artist), "always hamburger" would be wrong for that instance. At that point, restore the classification as an explicit check ("is my route in the top-level menu set?" read from `NavigationConfig`) rather than the removed `NavigationStack.Count` heuristic. This is a one-method change and is noted inline in the code comment.

## Testing approach

`OnNavigatedTo` is page-lifecycle code that touches `Shell.Current` (null in unit tests) — Level C/UI. Verification is **manual E2E on the Android emulator** (Helder observes), documented in `task-log.md` per `bug-tracking.md`/`testing.md` for UI-only changes. Traceability matrix in the task-log maps REQ-HNAV-01..04 to the E2E steps.
