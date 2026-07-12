# Requirements — Hamburger on CRUD list pages

> **Approval gate:**  `✅ Approved — Helder 2026-07-11.`  Track: **feature** (confirmed by Helder — no BUG-NNN). Spec-reviewer verdict: **PASS** (2026-07-11).

**Feature:** Hamburger Navigation Pattern (CRUD list pages)
**BACKLOG:** DevCycleCraft — "Hamburger menu on all hamburger-loaded pages" (2026-07-11), cross-ref *AppBar / SearchAppBar Interaction Redesign* point (1).
**Predecessor (shipped):** "Navigation Icon Pattern — Root Pages vs Pushed Pages" (2026-06). This is its corrective follow-up.
**Scope (confirmed by Helder 2026-07-11):** the **4 CRUD list pages only** — Venues, Singers (People), Artists, Songs.

## User story

> As a user, when I open a CRUD list page from the hamburger menu, I want the AppBar to keep showing the hamburger (so I can reach the same root options again), instead of a back arrow — because these pages are top-level destinations, not detail pages.

## Background (the bug)

`AppShellViewModel.NavigateAsync` reaches every menu destination except Queue via `PushAsync` onto the Queue root, so the navigation stack depth is always 2. `CrudListPageBase.OnNavigatedTo` picks the leading icon purely from `NavigationStack.Count <= 1`, which is only true for Queue. Every menu-pushed CRUD list page therefore shows the back arrow — indistinguishable from a form pushed on top of a list. The hamburger command is correctly wired but never assigned.

## Acceptance criteria

- **REQ-HNAV-01** — When any of the 4 CRUD list pages (Venues, Singers, Artists, Songs) is displayed, its `SmallAppBar` leading icon is the hamburger (`"menu"`), never the back arrow.
- **REQ-HNAV-02** — Tapping the hamburger on a CRUD list page opens the navigation drawer (`Shell.Current.FlyoutIsPresented == true`).
- **REQ-HNAV-03** — The Android **hardware** back button on a CRUD list page keeps its current behavior unchanged: confirm-sheet dismiss → search-mode close → otherwise the framework default pop, with its existing OS-driven animation.
- **REQ-HNAV-04** — No animation is hand-written or introduced. Forward navigation keeps the framework-default `PushAsync` slide; no `TranslateTo`/manual animation anywhere.

## Out of scope (explicit)

1. **Shell-native menu pages** — Events, Settings, Backup, About (plain `ContentPage`, Shell-native title). They also show a back arrow when reached from the menu, but fixing them needs per-page `Shell.BackButtonBehavior` overrides and overlaps the **AppBar / SearchAppBar Interaction Redesign** (BACKLOG 2026-07-10). Deferred there.
2. **AppBar back-button animation inconsistency** on form/detail pages (`GoToAsync("..")` does not animate like hardware back). Separate concern; candidate for the AppBar unification / *CRUD Form Action Pattern* task.
3. **SearchAppBar bar-swap** (redesign point 2). Separate task.
4. **Navigation model change** — `PushAsync` is retained deliberately (see `findings.md`). No move to `//route` sections.

## Assumption (documented)

The 4 CRUD list pages are **exclusively top-level menu destinations** — none is ever pushed as a sub-detail of another page today. The fix relies on this. If a future feature pushes a CRUD list as a non-top-level sub-page (e.g. Songs filtered from an Artist detail), the leading-icon rule must be revisited (see `design.md § Assumption & future-proofing`).
