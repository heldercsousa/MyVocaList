# Decision Record — AppBar / SearchAppBar Interaction Redesign: persistent search bar replaces the bar swap

**Date:** 2026-07-19
**Decided by:** Helder (approved Claude Code's UX analysis verbatim — "I approve your words")
**Status:** Approved — supersedes the "hypothesis to be validated" note in the 2026-07-10 BACKLOG registration (preserved in `Docs/Management/cross-cutting-log.md § AppBar / SearchAppBar Interaction Redesign`).

## Context

All CRUD list pages (Venues reference: `MyVocaList/UI/Pages/Venues/VenuesPage.xaml`) stack `SmallAppBar` and `SearchAppBar` in one `Shell.TitleView` grid, toggled by `IsSearchMode` (search action icon → `OpenSearchCommand`; SearchAppBar's leading back arrow → `CloseSearchCommand`). Each page suppresses Shell's native back button and re-implements back/dismiss priority in `OnBackButtonPressed` (confirm sheet → search → navigation).

Helder registered (2026-07-10) that the bar swap is confusing — back buttons are for navigating back, not for switching bars — and hypothesized killing the swap in favor of an always-visible search bar below the AppBar. The hypothesis required validation against official MD3 docs before adoption.

## Evidence (validated 2026-07-19)

- **Material 3 (Material Components — Search docs):** canonical mobile search is **SearchBar** — "a persistent and prominent search field at the top of the screen" — optionally paired with a full-screen **SearchView** for suggestion-driven search. Persistent SearchBar in the `AppBarLayout` is the *recommended* placement (fixed / scroll-away / lift-on-scroll behaviors). The bar-swap-with-back-arrow is the Material 2 / AppCompat-era pattern; MD3 does not recommend it. MD3 even allows hiding the search view's back button "to reduce clutter".
- **Project's own `m3-appbars.md`** already specs "M3 Search (standalone/detached — NOT yet implemented)": 56dp pill, `SurfaceContainerLow`, 16dp horizontal margins, reuse `AppBarBase` logic; and notes the search→back leading-icon transition applies only to persistent inline bars.
- **NN/g:** hidden UI reduces usage (hidden menus ≈ −21% task completion); visible persistent search/filter improves findability. On CRUD list pages where filtering is a primary task, search behind an icon is the weaker choice.
- For this app's local list filtering (no suggestions/history), a full-screen SearchView is overkill — a persistent inline filter bar filtering as-you-type is the right MD3 shape.

Sources: material-components-android `docs/components/Search.md` · nngroup.com mobile-navigation-patterns · find-navigation-mobile-even-hamburger · designing-search video.

## Decision — the standard pattern

1. **`SmallAppBar` stays** the only `Shell.TitleView` occupant: title, leading icon per the shipped Navigation Icon Pattern (hamburger on root, back on pushed). The search action icon is removed.
2. **Persistent M3 standalone search bar** (56dp pill, `SurfaceContainerLow`, leading `search_outlined` — **no back arrow**, trailing auto-clear) docked at the top of the list content, filtering inline as the user types. Optional lift-on-scroll paired with `IsScrolled`.
3. **Delete the swap machinery:** `IsSearchMode`, `OpenSearchCommand`/`CloseSearchCommand`, the TitleView `Grid` + `InverseBoolConverter` toggle, `SearchAppBar` itself (governed component — retirement goes through component-change-governance four gates), and the search branch of `OnBackButtonPressed` (back gesture returns to pure navigation semantics).
4. **Centralization:** host the persistent search bar **inside `CrudListView`** (already exposes `SearchPlaceholder`; add a `SearchText` bindable property) so every CRUD page gets standard search with zero per-page wiring. `CrudListView`, `SmallAppBar`, `SearchAppBar` are governed components → dedicated four-gate task, never bundled.

## Consequences

- BACKLOG child task **"CRUD lists → AppBar + SearchBar logic enhancement" (2026-07-11) is KEPT** — it becomes the rollout vehicle for this pattern across all CRUD list pages.
- Guideline amendments required in the implementation pass (with `amend:` commits + changelog):
  - `.claude/library/crud-appbar-list-toolbar.md` — the law "Search belongs in Shell.TitleView via SearchAppBar; do not place a search bar inside the page content area" is superseded by this decision.
  - `.claude/library/m3-appbars.md` — promote "M3 Search (standalone/detached)" to the standard; retire "Pattern: Search replaces app bar".
- Related rows to sequence: *Search Pattern Standardization* (2026-06) should align with/consume this decision; *lazy SearchAppBar structural reduction* (2026-06-12) becomes moot when SearchAppBar is retired.
- Next step: spec (brainstorming → spec-reviewer → Helder) for the governed-component implementation task.
