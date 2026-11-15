---
id: hamburger-in-the-shell-native-navigation-slot-remove-the-duplicate-icon
title: Hamburger in the Shell-native navigation slot - remove the duplicate icon
status: ✅ Done
target: 2026-11-15
parent: hamburger-nav-pattern
goal: Show exactly one leading icon on the 4 CRUD list pages, in the Shell-native navigation slot (same slot as the form back arrow), and make it open the drawer; today the first navigation node draws two hamburgers and the native one is dead.
gate: Helder passes the emulator/device checklist.
closed: 2026-11
kind: change
---

# Hamburger in the Shell-native navigation slot - remove the duplicate icon

Show exactly one leading icon on the 4 CRUD list pages, in the Shell-native navigation slot (same slot as the form back arrow), and make it open the drawer; today the first navigation node draws two hamburgers and the native one is dead.

> Post-ship change to `DevCycleCraft/hamburger-nav-pattern` (shipped 2026-07, immutable). It amends
> REQ-HNAV-01/02 of that spec; REQ-HNAV-03/04 stay as written. Reported by Helder 2026-11-15 with an
> emulator screenshot; pre-authorised end-to-end in the same request.

## Symptom

- On the first navigation node (app start on Venues, and whenever back navigation returns to it)
  two hamburger icons render side by side in the top bar.
- The left one (bigger, aligned with the screen edge) does not respond to taps. The right one
  (smaller) opens the drawer.
- A list page reached through the menu shows only the smaller one.
- Form pages render their back arrow in the left slot, which is where the hamburger is wanted.

## Root cause

Two independent things draw a leading icon:

1. Shell's native toolbar navigation icon (left slot).
2. `SmallAppBar`'s navigation button inside `Shell.TitleView` (right, smaller), fed by
   `AppBarNavigationIcon` / `AppBarNavigationCommand` from `CrudListPageBase.OnNavigatedTo`.

The 4 CRUD list pages tried to suppress (1) with
`<BackButtonBehavior IsVisible="False" IsEnabled="False" />`. In MAUI's Android
`ShellToolbarTracker`:

- `IsVisible` is honored only on the back-arrow path (stack depth > 1, or a `Command` is set). At
  the stack root with `FlyoutBehavior="Flyout"` the drawer-icon path runs and ignores it, so the
  native hamburger is drawn anyway. A menu-reached page is a `PushAsync` (depth 2), so there the
  suppression works — hence "only on the first navigation node".
- The toolbar click handler is gated by `IsEnabled`, so `False` leaves that native icon dead.

The old XAML comment said `IsEnabled="False"` was needed so hardware back reaches
`OnBackButtonPressed`. On Android that is not so: `Shell.OnBackButtonPressed` does not read
`IsEnabled` and runs `BackButtonBehavior.Command` only on Windows; it calls the page's
`OnBackButtonPressed` first.

## Amended acceptance criteria

- **REQ-HNAV-01′** (replaces REQ-HNAV-01) — When any of the 4 CRUD list pages (Venues, Singers,
  Artists, Songs) is displayed as a top-level destination, exactly **one** leading icon is shown,
  the hamburger (`menu_outlined`), in the Shell-native navigation slot — at any navigation depth,
  including the first navigation node. `SmallAppBar` shows no navigation icon on these pages.
- **REQ-HNAV-02′** (replaces REQ-HNAV-02) — Tapping that icon opens the navigation drawer
  (`Shell.Current.FlyoutIsPresented == true`).
- **REQ-HNAV-03** (unchanged) — Android hardware back keeps its behavior: confirm-sheet dismiss →
  search-mode close → framework default pop; on the root Venues page, the exit confirmation.
- **REQ-HNAV-04** (unchanged) — no hand-written animation.
- **REQ-HNAV-05** (new) — A CRUD list shown as a pushed sub-page (Songs catalog mode) shows its
  back arrow in the same native slot and it runs the ViewModel's back command.

## Design

The native slot is driven by the same two ViewModel properties; they are no longer passed to
`SmallAppBar`. With a `Command` set, Shell shows `IconOverride` in the native slot at any stack
depth and a tap runs the command.

`CrudListPageBase` owns one `BackButtonBehavior` per page instance, attaches it in
`AttachViewModel()` with `Shell.SetBackButtonBehavior`, and copies the ViewModel's
`AppBarNavigationIcon` (as `ImageSource.FromFile`) and `AppBarNavigationCommand` into it — once
before the first frame and again whenever either property changes. The hamburger icon + command
are assigned in `AttachViewModel()` as well as in `OnNavigatedTo`, so a pushed page never flashes
Shell's default back arrow.

Per page (`VenuesPage.xaml`, `PeoplePage.xaml`, `ArtistsPage.xaml`, `SongsPage.xaml`) the
`<Shell.BackButtonBehavior>` block and the `NavigationIcon` / `NavigationCommand` attributes of
`<appbars:SmallAppBar>` are removed; nothing is added.

> **Rejected on the emulator (2026-11-15):** declaring the behavior in XAML with
> `IconOverride="{Binding AppBarNavigationIcon}"` / `Command="{Binding AppBarNavigationCommand}"`.
> The binding was evaluated once with the ViewModel's constructor defaults (`arrow_back_outlined`
> and a no-op command) and never refreshed, so the slot showed a dead back arrow. The behavior is
> therefore kept in code.

`SmallAppBar` (governed component) is **not** edited: with no `NavigationIcon` its button hides
itself through the existing `HasNavigationIcon`. The component four-gate therefore does not apply.

## Consumers

| Page | What could break | Verification |
|---|---|---|
| `VenuesPage` | exit confirmation on hardware back at the root | emulator steps 1, 2, 4 |
| `PeoplePage` | hamburger missing / back-arrow flash when pushed | emulator step 3 |
| `ArtistsPage` | same | emulator step 3 |
| `SongsPage` | same; catalog mode back arrow (REQ-HNAV-05) | emulator steps 3, 6 |

## Out of scope

- Settings, Backup, About, YouTubeSearch — not CRUD list pages; still deferred as in the original spec.
- Any restyling of `SmallAppBar`.

## Verification (emulator)

1. Cold start on Venues: one hamburger, left slot, tap opens the drawer.
2. Venues → form → back (on-screen and device): still one hamburger.
3. Menu → Singers / Artists / Songs / Venues: one hamburger in the left slot, no back-arrow flash, tap opens the drawer.
4. Device back: root Venues → exit sheet; pushed list → pops; delete-confirm sheet open → dismisses it.
5. Icon tint follows `Shell.ForegroundColor` in light and dark theme; the title sits right after the icon.
6. Songs catalog mode (Artists → catalog) shows the back arrow and pops.

Evidence is recorded in `../../task-log.md`.

## Known limitations (found during verification, not changed here)

- On a pushed list page Android labels the native button "Navigate up" for screen readers, although
  it shows the hamburger and opens the drawer (MAUI derives the label from the stack depth). At the
  root it is "Open navigation drawer".
- Device back on a Venues page reached through the menu (pushed) shows the exit confirmation instead
  of popping, because `VenuesPage.OnBackButtonPressed` always does. Behavior predates this change.

## Close-out (2026-11-15)

Merged to `develop` as `a3d743e9` (full suite 1660 passed, Android build 0 errors on the merged tree). Helder reported the checklist passed and asked to close the item.
