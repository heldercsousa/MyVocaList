# BACKLOG Archive — 2026-07

> Closed backlog rows completed in 2026-07, moved out of `Docs/Management/BACKLOG.md` (restructure 2026-07-15, `Docs/Management/DevCycleCraft/backlog-purpose-review/`). Rows use the slim PO template: Goal + one-sentence outcome + pointer. Full technical narratives were relocated verbatim into the feature docs named in each pointer. **Past BUG-NNN / feature lookups must grep all `backlog-archive/` files.**

## Business Features

<!-- BACKLOG:GENERATED:BEGIN archive-business -->
| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-01 | BUG-020: SongsPage FAB crash — unguarded SecureStorage in async void OnAppearing (Critical) (under: **Artists & Songs Catalog**) | ✅ Fixed | Goal: Fixed with try-catch fallback + regression test; emulator-verified 2026-07-03. Pointer: `BusinessFeatures/artists-songs/bugs/2026-07-01-BUG-020-songspage-fab-crash-secure-storage/`. |
| 2026-07-03 | BUG-036: PersonFormPage birthday validation rejects masked input (Major) (under: **Person CRUD**) | ✅ Fixed | Goal: Fixed 2026-07-12 (validator accepts 4-digit masked form); Helder on-device re-verify pending. Pointer: `BusinessFeatures/persons/bugs/2026-07-03-BUG-036-personformpage-birthday-mask/`. |
| 2026-06-30 | **03 - Update Singer form (validation)** (under: **Person CRUD**) | ✅ Done | Goal: apply the form-validation guide to the Singer form. Shipped; emulator E2E done 2026-07-01 through 2026-07-03 (found BUG-035-038). Pointer: `BusinessFeatures/persons/changes/2026-06-30-form-validation-update/`. |
| 2026-07-01 | BUG-021: SongsPage FAB crash — `ISimilarityScorer` not registered in DI (Critical) (under: **Artists & Songs Catalog**) | ✅ Fixed | Goal: Fixed via `AddAppServices()` extension + DI regression tests; emulator-verified 2026-07-03. Pointer: `BusinessFeatures/artists-songs/bugs/BUG-021-songspage-fab-crash/`. |
| 2026-07-02 | BUG-023: SongForm resolution/merge BottomSheets can never open (Critical) (under: **Artists & Songs Catalog**) | ✅ Fixed | Goal: Fixed via the BottomSheet code-behind pattern; emulator re-run pending on BUG-027. Pointer: `BusinessFeatures/artists-songs/bugs/BUG-023-songform-bottomsheet-broken/`. |
| 2026-07-02 | BUG-024: SongForm edit-mode Save silently wipes fields (Critical) (under: **Artists & Songs Catalog**) | ✅ Fixed | Goal: Fixed with full edit hydration + 7 regression tests; emulator re-run pending on BUG-027. Pointer: `BusinessFeatures/artists-songs/bugs/BUG-024-songform-edit-data-loss/`. |
| 2026-07-11 | **Song form — stays full-screen page + AppBar-save pattern** | ✅ Done | Goal: keep Song form as a full-screen page and move Save to the AppBar trailing slot, implemented 2026-07-12 (Helder-authorized sequencing override). Pointer: `DevCycleCraft/crud-form-action-pattern/`. |
| 2026-06-30 | **02 - Update Venues form (validation)** (under: **Venues CRUD**) | ✅ Done | Goal: reference implementation of the form-validation guide on the Venues form. Shipped; emulator E2E done 2026-07-03 (found BUG-034). Pointer: `BusinessFeatures/venues/changes/2026-06-30-form-validation-update/`. |
| 2026-07-11 | **Hamburger menu on all hamburger-loaded pages (CRUD-only scope)** | ✅ Done | Goal: hamburger menu on every hamburger-loaded page, shipped for the 4 CRUD list pages; Shell-native pages deferred to the AppBar/SearchAppBar redesign. Pointer: `DevCycleCraft/hamburger-nav-pattern/`. |
<!-- BACKLOG:GENERATED:END archive-business -->

## Dev Cycle Craft

<!-- BACKLOG:GENERATED:BEGIN archive-craft -->
| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-10 | **CRUD Form Action Pattern — MD3 Save/Cancel placement** (under: **Song form — stays full-screen page + AppBar-save pattern**) | ✅ Done | Goal: resolve MD3 Save/Cancel placement for CRUD forms, implemented 2026-07-12 (SongFormPage ToolbarItem-Save; library rules updated). Pointer: `DevCycleCraft/crud-form-action-pattern/changes/2026-07-10-md3-save-cancel-placement/`. |
| 2026-07-11 | **AutocompleteField Component Evaluation — Adjust or Replace** (under: **Autocomplete Component — Evaluation, Rebuild & Rollout**) | ✅ Done | Goal: decide whether to adjust/rebuild the hand-rolled AutocompleteField or replace it. Evaluation complete 2026-07-11: adjust/rebuild, not blind replace. Pointer: `DevCycleCraft/autocomplete-component/changes/2026-07-11-component-evaluation/`. |
| 2026-07-10 | **Branch-lock avoidance — orchestrator must also work in worktrees** | ✅ Done | Goal: orchestrator must work in worktrees too, not just implementors. Resolved 2026-07-14: worktrees mandatory for all code edits, branch guard hooks, a develop-branch ledger + commands added. Pointer: `cross-cutting/branch-lock-avoidance/`. |
| 2026-07-11 | **Apply new component to the simplest candidate** (under: **Autocomplete Component — Evaluation, Rebuild & Rollout**) | ✅ Done | Goal: validate the adjusted component on the simplest candidate form. Not needed as a separate task — Person/Song forms already consumed the field; Person is the test candidate. Pointer: `DevCycleCraft/autocomplete-component/changes/2026-07-11-apply-to-simplest-candidate/`. |
| 2026-07-12 | BUG-040: mobile autocomplete input loses focus (Major) (under: **Autocomplete Component — Evaluation, Rebuild & Rollout**) | ✅ Fixed | Goal: Fixed (deferred focus after modal animation); manual E2E documented. Pointer: `DevCycleCraft/autocomplete-component/bugs/2026-07-12-BUG-040-mobile-input-loses-focus/`. |
| 2026-07-12 | BUG-041: mobile Search View cannot be dismissed; duplicates on back (Critical) (under: **Autocomplete Component — Evaluation, Rebuild & Rollout**) | ✅ Fixed | Goal: Fixed via MobileFieldReopenGuard (TDD, Red first). Pointer: `DevCycleCraft/autocomplete-component/bugs/2026-07-12-BUG-041-search-view-duplicate-on-back/`. |
| 2026-07-12 | BUG-042: every back tap repeats the reappear/duplicate cycle (Critical) (under: **Autocomplete Component — Evaluation, Rebuild & Rollout**) | ✅ Fixed | Goal: Fixed together with BUG-041 (same root cause) with regression tests. Pointer: `DevCycleCraft/autocomplete-component/bugs/2026-07-12-BUG-042-back-tap-repeat-cycle/`. |
| 2026-07-12 | BUG-043: release build returns zero autocomplete suggestions (Critical) (under: **Autocomplete Component — Evaluation, Rebuild & Rollout**) | ✅ Fixed | Goal: Root cause: manual SetValue severed the OneWay `Suggestions` binding; fixed via `ClearSuggestionsPresentation()`, on-device verified. Follow-up defects registered separately (BUG-044–047). Pointer: `DevCycleCraft/autocomplete-component/bugs/bug-043/`. |
| 2026-07-01 | BUG-022: SingerForm birthday field mask missing (Minor) (under: **Form validation**) | ✅ Fixed | Goal: Fixed with a XAML-only date input mask on the birthday field. Pointer: `BusinessFeatures/persons/bugs/BUG-022-singerform-birthday-mask/`. |
<!-- BACKLOG:GENERATED:END archive-craft -->
