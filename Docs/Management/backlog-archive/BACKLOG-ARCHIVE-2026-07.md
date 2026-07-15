# BACKLOG Archive — 2026-07

> Closed backlog rows completed in 2026-07, moved out of `Docs/Management/BACKLOG.md` (restructure 2026-07-15, `Docs/Management/DevCycleCraft/backlog-purpose-review/`). Rows use the slim PO template: Goal + one-sentence outcome + pointer. Full technical narratives were relocated verbatim into the feature docs named in each pointer. **Past BUG-NNN / feature lookups must grep all `backlog-archive/` files.**

## Business Features

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-01 | ↳ BUG-020: SongsPage FAB crash — unguarded SecureStorage in async void OnAppearing (Critical) | ✅ Fixed | Fixed with try-catch fallback + regression test; emulator-verified 2026-07-03. Pointer: `BusinessFeatures/artists-songs/bugs/BUG-020-songspage-fab-crash-secure-storage.md`. |
| 2026-07-01 | ↳ BUG-021: SongsPage FAB crash — `ISimilarityScorer` not registered in DI (Critical) | ✅ Fixed | Fixed via `AddAppServices()` extension + DI regression tests; emulator-verified 2026-07-03. Pointer: `BusinessFeatures/artists-songs/bugs/BUG-021-songspage-fab-crash/`. |
| 2026-07-02 | ↳ BUG-023: SongForm resolution/merge BottomSheets can never open (Critical) | ✅ Fixed | Fixed via the BottomSheet code-behind pattern; emulator re-run pending on BUG-027. Pointer: `BusinessFeatures/artists-songs/bugs/BUG-023-songform-bottomsheet-broken/`. |
| 2026-07-02 | ↳ BUG-024: SongForm edit-mode Save silently wipes fields (Critical) | ✅ Fixed | Fixed with full edit hydration + 7 regression tests; emulator re-run pending on BUG-027. Pointer: `BusinessFeatures/artists-songs/bugs/BUG-024-songform-edit-data-loss/`. |
| 2026-06 | ↳ Bug/Gap: SongFormPage Artist field autocomplete with blur-clear (BUG-008) | 🔵 Superseded (closed 2026-07-10) | Originally fixed with blur-clear; the Artist & Song Form UX Redesign reverses that behavior and owns the field — no independent action. Pointer: `BusinessFeatures/artists-songs/bugs/BUG-008-songform-artist-autocomplete.md`. |
| 2026-07-11 | ↳ Song form → stays full-screen page + AppBar-save pattern | ✅ Done | Implemented 2026-07-12 (Save moved to AppBar trailing slot; Helder-authorized sequencing override). Pointer: `Docs/Management/DevCycleCraft/crud-form-action-pattern/`. |
| 2026-07-11 | ↳ Hamburger menu on all hamburger-loaded pages (CRUD-only scope) | ✅ Done | Shipped for the 4 CRUD list pages; Shell-native pages deferred to the AppBar/SearchAppBar redesign. Pointer: `Docs/Management/DevCycleCraft/hamburger-nav-pattern/`. |

## Dev Cycle Craft

| Target | Activity | Status | Notes |
|--------|----------|--------|-------|
| 2026-07-14 | **Token-scoped subagent reads — library file split + size-budget guard** | ✅ Done | Goal: cut subagent cold-start tokens. Shipped (28 section files, index stubs, size-guard hook). Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-07-10 | ↳ Branch-lock avoidance — orchestrator must also work in worktrees | ✅ Done | Resolved 2026-07-14: worktrees mandatory for all code edits, branch guard hooks, LEDGER.md + commands added. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-07-10 | **CRUD Form Action Pattern — MD3 Save/Cancel placement** | ✅ Done | Implemented 2026-07-12 (SongFormPage ToolbarItem-Save; library rules updated). Pointer: `Docs/Management/DevCycleCraft/crud-form-action-pattern/`. |
| 2026-07-11 | ↳ ② AutocompleteField Component Evaluation — Adjust or Replace | ✅ Done | Evaluation complete 2026-07-11: adjust/rebuild the hand-rolled field, not blind replace. Pointer: `Docs/Management/DevCycleCraft/autocomplete-component/findings.md`. |
| 2026-07-11 | ↳↳ Apply new component to the simplest candidate | ✅ Done | Not needed as a separate task — Person/Song forms already consumed the field; Person is the test candidate. Pointer: `Docs/Management/DevCycleCraft/autocomplete-component/`. |
| 2026-07-12 | ↳↳ BUG-040: mobile autocomplete input loses focus (Major) | ✅ Fixed | Fixed (deferred focus after modal animation); manual E2E documented. Pointer: `Docs/Management/DevCycleCraft/autocomplete-component/task-log.md`. |
| 2026-07-12 | ↳↳ BUG-041: mobile Search View cannot be dismissed; duplicates on back (Critical) | ✅ Fixed | Fixed via `MobileFieldReopenGuard` (TDD, Red first). Pointer: `Docs/Management/DevCycleCraft/autocomplete-component/task-log.md`. |
| 2026-07-12 | ↳↳ BUG-042: every back tap repeats the reappear/duplicate cycle (Critical) | ✅ Fixed | Fixed together with BUG-041 (same root cause) with regression tests. Pointer: `Docs/Management/DevCycleCraft/autocomplete-component/task-log.md`. |
| 2026-07-03 | ↳ BUG-036: PersonFormPage birthday validation rejects masked input (Major) | ✅ Fixed | Fixed 2026-07-12 (validator accepts 4-digit masked form); Helder on-device re-verify pending. Pointer: `Docs/Management/BusinessFeatures/persons/form-validation-task-log.md`. |
| 2026-07-01 | ↳ BUG-022: SingerForm birthday field mask missing (Minor) | ✅ Fixed | Fixed (XAML-only `Mask="00/00"`). Pointer: `BusinessFeatures/persons/bugs/BUG-022-singerform-birthday-mask/`. |
| 2026-06-30 | ↳ 02 - Update Venues form (validation) | ✅ Done | Reference implementation shipped; emulator E2E done 2026-07-03 (found BUG-034). Pointer: `Docs/Management/BusinessFeatures/venues/form-validation-task-log.md`. |
| 2026-06-30 | ↳ 03 - Update Singer form (validation) | ✅ Done | Shipped; emulator E2E done 2026-07-01/03 (found BUG-035–038). Pointer: `Docs/Management/BusinessFeatures/persons/form-validation-task-log.md`. |
| 2026-06-30 | ↳ 04 - Update Songs form (validation) | ✅ Done | Shipped; emulator E2E blocked by BUG-027, re-run once fixed. Pointer: `Docs/Management/BusinessFeatures/artists-songs/form-validation-task-log.md`. |
| 2026-06-30 | ↳ 05 - Update Artists form (validation) | ✅ Done | Shipped; emulator E2E done 2026-07-03 (found BUG-034/039). Pointer: `Docs/Management/BusinessFeatures/artists-songs/form-validation-task-log.md`. |
| 2026-07-02 | ↳ 06 - Character-counter threshold alignment | ✅ Done | Shipped across Song/Venue/Person services with TDD tests. Pointer: `Docs/Management/DevCycleCraft/ui-form-validation-guide/task-log.md`. |
| 2026-07-03 | **Local enforcement automations (solo, pre-prod)** | ✅ Done | Goal: mechanical gates while solo — constitutional guard hook + local pre-commit test gate. Both shipped and verified. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-07-05 | **Scope `myvocalist-coding` skill to project level** | ✅ Done | Skill moved to project scope 2026-07-07; user-level copy deleted. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-07-04 | **Rules File Refactoring — Reduce Unconditional Load** | ✅ Done | Goal: cut unconditional rules-load tokens. Shipped (routing tables + library files; measured ~8–11k/agent saved); all 18 tasks + audit closed 2026-07-09. Pointer: `Docs/Management/DevCycleCraft/rules-file-refactoring/`. |
| 2026-07-04 | ↳ Rules File Refactoring sub-tasks (SPIKE, 01–18, GATE-A/B, AUDIT) | ✅ Done | All sub-rows completed 2026-07-04..09; verbatim rows preserved in the task-log. Pointer: `Docs/Management/DevCycleCraft/rules-file-refactoring/task-log.md`. |
| 2026-07-07 | **SECURITY — rotate + de-commit secrets in `.mcp.json`** | ✅ Done | Secrets de-committed 2026-07-07; Helder rotated all three tokens 2026-07-09. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-07-07 | **MCP governance sync + Docs housekeeping** | ✅ Done | MCP server set trimmed to context7 + devexpress-maui; residual Docs-housekeeping items remain listed in the relocated narrative. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-07-09 | **HELDER MANUAL ACTIONS (reminder)** | ✅ Done | All three confirmed complete by Helder 2026-07-09 (token rotation, MCP re-enable, authorship review). Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-07-07 | **Tool-registry cleanup: context-budget plugin, duplicate `review`, exa MCP** | ✅ Done | All items done by 2026-07-09, incl. the `sln-` command-prefix HARD RULE. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-06-27 | **Per-Agent MCP/Skill Context Isolation** | ✅ Done | Helder accepted the measured outcome 2026-07-10 (~4–5k/agent saved; research questions all answered). Pointer: `Docs/Management/DevCycleCraft/per-agent-context-isolation/`. |
| 2026-06 | **Search AppBar Pattern — Root Page + Search Interaction** | 🔵 Superseded (closed 2026-07-10) | Superseded by the active *AppBar / SearchAppBar Interaction Redesign* row; MD3-research requirement carries over. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-07-14 | **BACKLOG.md purpose review — restore it as a PO-level business artifact** | ✅ Done | Executed 2026-07-15: row template + header rules, monthly archive rotation, verbatim narrative relocation, workflow.md pointer. Pointer: `Docs/Management/DevCycleCraft/backlog-purpose-review/`. |
