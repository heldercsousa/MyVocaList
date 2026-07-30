# MyVocaList — Product Backlog

> **Product backlog — a PO-level business artifact.** Business features are ordered by target delivery date. Every feature begins as `💡 Pending` and is promoted through the lifecycle below.
>
> **When to read:** At the start of any new feature cycle (workflow.md Rule 1 step 0) and when resuming a session with no active handoff file (Rule 7).
>
> **Who updates statuses:** The main agent updates this file at each workflow milestone. Subagents do not touch BACKLOG.md.

> ## ⚠️ THE ROWS IN THIS FILE ARE GENERATED — read before editing any row
>
> *(Single-branch ownership retired 2026-07-26: `feature/backlog-migration` merged into `develop`.
> The generated-artifact write-ownership protocol is still under definition — Spec Evolution T13d.)*
>
> The rows in this file are **generated** from each item's `README.md` frontmatter. Between the
> `<!-- BACKLOG:GENERATED:BEGIN … -->` / `END` markers, **a hand-edit is not a merge conflict — it is
> silently overwritten** on the next `regen`. An edit inside a fence can be lost without warning.
> A pre-commit gate (`regen --check`) blocks a commit that leaves this file stale.
>
> **To change a row:** edit that item's `README.md` frontmatter (or use
> `python .claude/scripts/backlog/backlog_gen.py status <ID> "<status>"`), never the row itself.
> **To add an item:** `backlog_gen.py register …` — it creates the folder, README and `.sln` entry
> together.
> **If you must touch a fenced row directly:** coordinate via `LEDGER.md` first (until T13d lands).
>
> Everything **outside** the fences — this header, the row rules, the status reference — is
> hand-written and preserved byte-for-byte by the generator. Editing here is always safe.

## Row rules (agents: do NOT re-fatten this file)

- **Template:** `| Target | Feature/Item | Status | Notes |`. **Target** = registration date (or originally targeted month) — carried over unchanged, never reinterpreted.
- **Notes column: max 3 sentences / ~50 words**, containing only: **Goal** (what the item delivers and why, in business terms), **Gate/blocker** (the single thing holding it — owner + what), and **one Pointer** (`Docs/Management/.../[feature]/` path where all technical detail lives).
- **Banned from rows:** commit hashes, file paths beyond the single pointer, root-cause narrative, review verdicts, test counts, per-step status trails, token measurements, AC numbers. Branch/phase tracking belongs to `LEDGER.md`; execution history to the feature's `task-log.md`; displaced narratives of folder-less items to `Docs/Management/cross-cutting-log.md`.
- **Archive rotation:** rows reaching `✅ Done` / `✅ Fixed` / superseded-closed move (slimmed to the same template) to `Docs/Management/backlog-archive/BACKLOG-ARCHIVE-YYYY-MM.md`, keyed by completion month; a Done sub-row archives independently of a still-active parent. Only active statuses (💡 📋 🗺️ 🟢 🟡 🔵 🔴) remain here.
- **Lookups:** past BUG-NNN / feature history lives in `backlog-archive/` — grep those files too.
- Rule pointer: `workflow.md` Rule 1 references this header as the authoritative row template.

---

## Status reference

| Status | Meaning |
|--------|---------|
| `💡 Pending` | Captured, not yet evaluated |
| `📋 Spec` | Approved — spec being written |
| `🗺️ Plan` | Spec approved — plan being written |
| `🟢 Ready` | Plan approved — dispatchable |
| `🟡 In Progress` | Implementation underway |
| `🔵 Deferred` | Paused — reason documented |
| `🔴 Blocked` | Cannot proceed — dependency named |
| `✅ Done` | Shipped — row moves to `backlog-archive/` |

---

## Business Features

<!-- BACKLOG:GENERATED:BEGIN business-features -->
| Target | Feature | Status | Notes |
|--------|---------|--------|-------|
| 2026-03 | ↳ Bug: Venues list fetch slow — 2.2s paged query (BUG-012) | 💡 Pending | Goal: restore fast venue list loading (N+1 query suspected). Pointer: `BusinessFeatures/venues/bugs/2026-03-01-BUG-012-venuesviewmodel-fetch-slow/`. |
| 2026-05 | **Artists & Songs Catalog** | 🔴 Blocked | Goal: full artist/song catalog management. Gate: BUG-027 (Critical) makes song registration impossible — smoke test 16C.1 must re-run green before phases 16C.2–16C.5 resume. Pointer: `BusinessFeatures/artists-songs/`. |
| 2026-07-03 | ↳ BUG-027: SongFormPage Artist field — no validation, no autocomplete, blur clears typed text (Critical) | 💡 Pending | Goal: make song creation possible again. Gate: fix direction now owned by the DX `AutoCompleteEdit` replacement task (decision 2026-07-19), superseding foundations ① + ②. Pointer: `BusinessFeatures/artists-songs/bugs/2026-07-03-BUG-027-songformpage-artist-field-broken/`. |
| 2026-07-21 | ↳ BUG-050: Song form — selecting an artist suggestion does not lock the field (Critical) | 💡 Pending | Goal: picking a suggestion must lock the Artist field. Root cause: `SelectArtist` never sets `IsArtistLocked=true` (one-line omission). Found in DX-AC T7. Pointer: `BusinessFeatures/artists-songs/bugs/2026-07-21-BUG-050-suggestion-not-locked/`. |
| 2026-07-21 | ↳ BUG-051: Song form — artist autocomplete returns stale results (searches prior keystroke) (Major) | 💡 Pending | Goal: dropdown must reflect the current query. Root cause: shared `ArtistSuggestions` race, no per-request cancellation in `SearchArtistsAsync`. Found in DX-AC T7 (W2 realized). Pointer: `BusinessFeatures/artists-songs/bugs/2026-07-21-BUG-051-autocomplete-stale-results/`. |
| 2026-07-21 | ↳ BUG-052: Song form — editing a saved song shows an empty Artist field (Major) | 💡 Pending | Goal: edit mode must hydrate the saved artist. Likely compound with BUG-050 (song saved without ArtistId); reconfirm after BUG-050 and BUG-051. Pointer: `BusinessFeatures/artists-songs/bugs/2026-07-21-BUG-052-edit-shows-empty-artist-field/`. |
| 2026-07-21 | ↳ **Song artist field — correctness fixes + inline "create new artist"** | 🟡 In Progress | Goal: make the Song Artist autocomplete correct (folding in BUG-050, BUG-051, BUG-052 and retain-text) and add inline create-new-artist (➕ row), closing BUG-027. Gate: on-device re-run #4 failed 2026-07-30 — editing a song's artist is not saved, inline create-new-artist is unreachable, and a spurious no-match row appears; a fix wave plus a green re-run gate closeout. Pointer: `BusinessFeatures/artists-songs/changes/2026-07-21-inline-artist-create/task-log.md`. |
| 2026-07-03 | ↳ BUG-028: ArtistsPage trailing catalog button no-op — regression of BUG-015/019 (Major) | 💡 Pending | Goal: trailing button must navigate to the artist's catalog. Pointer: `BusinessFeatures/artists-songs/bugs/2026-07-03-BUG-028-artistspage-trailing-catalog-button-noop/`. |
| 2026-07-03 | ↳ BUG-029: ArtistFormPage search-strip icon crashes the app (Critical) | 🔵 Deferred | Goal: the search-strip icon must not crash the app. Gate: the search-strip element is slated for deletion by the Form UX Redesign; re-triage only if any part of the strip survives. Pointer: `BusinessFeatures/artists-songs/bugs/2026-07-03-BUG-029-artistformpage-search-strip-icon-crash/`. |
| 2026-07-03 | ↳ BUG-030: ArtistFormPage search strip UX unclear (spec gap) | 🔵 Deferred | Goal: resolve the search-strip spec gap on the Artist form. Gate: Answered by Helder 2026-07-10: the element must disappear from both forms — folded into the Form UX Redesign. Pointer: `BusinessFeatures/artists-songs/bugs/2026-07-03-BUG-030-artistformpage-search-strip-ux-unclear/`. |
| 2026-07-03 | ↳ BUG-031/032: no API autocomplete while typing Artist Name / Song Title (spec gap) | 🔵 Deferred | Goal: settle whether API-backed autocomplete is required on the two name entries. Gate: Answered by Helder 2026-07-10: autocomplete (local + API) IS required on both entries — folded into the Form UX Redesign. Pointer: `BusinessFeatures/artists-songs/bugs/2026-07-03-BUG-031-no-api-autocomplete-artist-name-song-title/`. |
| 2026-07-10 | ↳ **Artist & Song Form UX Redesign — autocomplete, similar-match warning, search-strip removal** | 🔵 Deferred | Goal: friction-free artist/song name entry (local + API autocomplete, never clear typed text, similar-match warning before create). Gate: parked by Helder; gated on autocomplete foundations ① + ②; partial work sits on branch `feature/form-ux-redesign`. Pointer: `BusinessFeatures/artists-songs/changes/2026-07-10-form-ux-redesign/`. |
| 2026-07-11 | **Form & Autocomplete UX Overhaul** | 💡 Pending | Goal: umbrella sequencing all form-presentation + AppBar-save + adaptive-autocomplete changes. Gate: foundation order ② → component build → first application → ①; forms convert in order Venue → Artist → Singer. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-06 | Dead-code cleanup: superseded `QueueService`/`IQueueService` | 💡 Pending | Goal: verify-then-delete the dead service pair. Gate: verified dead 2026-07-15 (no DI registration, no runtime consumer) — ready for the delete step. Pointer: `BusinessFeatures/queue-management/queue-deadcode-cleanup.md`. |
| 2026-06 | **Queue Entry Point Redesign — QueuePage as CRUD event list** | 💡 Pending | Goal: QueuePage becomes the CRUD list of events (FAB creates a queue; tap opens QueueManagementPage); EventsPage deleted. Gate: audit 2026-07-15 found NO implementation ever landed (registration only) — QueueManagementPage is unreachable in the app; Helder to re-prioritize. Pointer: `BusinessFeatures/queue-management/task-log.md`. |
| 2026-06 | **User Tutorial/Learning** | 💡 Pending | Goal: lowest-effort first tutorial version, updated as features ship. Pointer: `cross-cutting/user-tutorial-learning/`. |
| 2026-06 | **Website** | 💡 Pending | Goal: evaluate a site for marketing/documentation/community — myvocalist.com / myvocalist.app. Pointer: `cross-cutting/website/`. |
| 2026-06 | | 🏁 **MVP release** | |
| — | **Data Backup & Restore — Tier 2 (WiFi Mirror)** | 💡 Pending | Goal: second device on the same WiFi mirrors data in real time; fresh installs restore in one tap. Gate: Tier 1 shipped (done). Pointer: `BusinessFeatures/backup-restore/design.md`. |
| — | **Singer self-registration** | 💡 Pending | Goal: singers register via public link / kiosk / own device connected to the host. Pointer: `cross-cutting/singer-self-registration/`. |
| — | **Social features** | 💡 Pending | Goal: post-event sharing, singer stats. Pointer: `cross-cutting/social-features/`. |
| 2026-07-03 | **Cross-cutting** | — | Bugs with no single parent business feature |
| 2026-07-03 | ↳ BUG-026: HWUI native crash (SIGABRT) on render teardown (Major) | 💡 Pending | Goal: confirm whether the crash is a real defect or debugger-teardown noise (Release logcat investigation first). Pointer: `BusinessFeatures/cross-cutting/bugs/BUG-026-hwui-sigabrt-render-teardown/`. |
<!-- BACKLOG:GENERATED:END business-features -->

---

## Dev Cycle Craft

> Infrastructure, tooling, architecture, and process improvements that support business feature delivery.

<!-- BACKLOG:GENERATED:BEGIN dev-cycle-craft -->
| Target | Activity | Status | Notes |
|--------|----------|--------|-------|
| 2026-07-11 | **Documentation & spec-tracking governance — where docs live** | 💡 Pending | Goal: standing rule/mechanism so docs never strand on feature branches (interim rule: docs commit to `develop`). Pointer: `cross-cutting/documentation-spec-tracking-governance/`. |
| 2026-07-12 | **Inline Trivial Fix (ITF) lane — bounded orchestrator inline-edit exception** | 🟡 In Progress | Goal: let the orchestrator apply a fully-diagnosed 1-file/≤5-line fix inline instead of paying a ~25–35k-token subagent round-trip. Gate: Helder observes the first live ITF fix end-to-end before ✅ — opportunistic, waits for a qualifying fix. Pointer: `DevCycleCraft/inline-trivial-fix/`. |
| 2026-05 | **Workflow & Folder Layout Alignment** | 🟡 In Progress | Goal: resolve SDD/skills/custom-rules conflicts and canonicalize the Docs/ layout. Pointer: `DevCycleCraft/workflow-folder-layout-alignment/`. |
| 2026-05 | **Inline Undo Pattern — UX Standard** | 💡 Pending | Goal: snackbar Undo (commit-first) standard for all inline destructive actions inside forms. Pointer: `cross-cutting/inline-undo-pattern/`. |
| 2026-06-27 | **Mandatory Worktree Rule Enforcement — ALL Subagent Work** | 💡 Pending | Goal: worktrees mandatory for every dispatch. Gate: largely delivered 2026-07-14 via the branch-lock-avoidance work — confirm remaining rule-doc updates then close. Pointer: `cross-cutting/mandatory-worktree-rule-enforcement/`. |
| 2026-06 | **Search Pattern Standardization + Navigation Result Service** | 💡 Pending | Goal: reconcile the app's two search patterns into one canonical choice + migration plan. Gate: blocks any new search surface until at least 📋 Spec. Pointer: `cross-cutting/search-pattern-standardization/`. |
| 2026-06 | **IAsyncRelayCommand Standardization** | 💡 Pending | Goal: one async-command pattern across all ViewModels (picker VMs are the reference). Pointer: `cross-cutting/iasyncrelaycommand-standardization/`. |
| 2026-06 | **Search Error State UX Standardization** | 💡 Pending | Goal: retrofit the picker pages' search-failure state to all CRUD search pages. Pointer: `cross-cutting/search-error-state-ux-standardization/`. |
| 2026-06 | **Filter Pattern Standardization** | 💡 Pending | Goal: standardized filter pattern (UI, binding contract, DB-side filtering) before any second CRUD page adds filters. Gate: blocks new filter additions. Pointer: `cross-cutting/filter-pattern-standardization/`. |
| 2026-07-15 | **String trimming on persistence — centralized normalization analysis** | 🗺️ Plan | Goal: strings persisted to the DB should be trimmed (extension of BUG-046's query-side trimming) via one centralized Services-layer helper (search) + EF Core `ValueConverter`s (persistence). Gate: D1/D2 recorded 2026-07-15; D3 (EF Core `ValueConverter`) + D4 (helper relocated to leaf `MyVocaList.Extensions` project, extension-method API) recorded 2026-07-19. Pointer: `DevCycleCraft/persisted-string-trimming/`. |
| 2026-07-19 | **`MyVocaList.Extensions` layer guidelines — placement criteria + rules-file promotion** | 💡 Pending | Goal: formalize when a helper belongs in the new dependency-free `MyVocaList.Extensions` project (created by D4 above) vs. Services/Domain, beyond the one worked example. Gate: `MyVocaList.Extensions` must exist first (Task 6a). Pointer: `DevCycleCraft/extensions-layer-guidelines/`. |
| 2026-06 | **Bug: Shell navigation swallows button tap animations** | 💡 Pending | Goal: let tap animations complete before Shell navigation begins (affects all flyout items). Pointer: `cross-cutting/shell-navigation-tap-animations/`. |
| 2026-06 | **Bug/Verify: FloatingToolbar always visible — should appear only on multi-select** | 💡 Pending | Goal: confirm intended visibility behavior against the original design, then fix or close as won't-fix. Pointer: `cross-cutting/floatingtoolbar-visibility-verify/`. |
| 2026-07-10 | **AppBar / SearchAppBar Interaction Redesign — page-nav pattern + persistent search bar** | 🟡 In Progress | Goal: kill the bar-swap search toggle — persistent MD3 `SearchBar` hosted in `CrudListView`. Gate: Helder — confirm D-1 (SearchAppBar survives for 4 picker pages) + emulator smoke test before ✅. Pointer: `DevCycleCraft/appbar-searchbar-redesign/`. |
| 2026-07-11 | **Autocomplete Component — Evaluation, Rebuild & Rollout** | 🟡 In Progress | Goal: make the app autocomplete MD3-compliant — evaluation, component build and rollout. Pointer: `DevCycleCraft/autocomplete-component/`. |
| 2026-07-19 | ↳ **Replace `AutocompleteMobileField` consumers with DX `AutoCompleteEdit`** | 🟡 In Progress | Goal: mature built-in autocomplete on all form consumers; unblocks BUG-027 → Artists & Songs Catalog. Gate: T2–T6 complete and merged to develop 2026-07-20; awaiting Helder's T7 on-device checklist (items a–i, incl. smoke 16C.1) before ✅. Pointer: `DevCycleCraft/autocomplete-component/changes/2026-07-19-dx-autocompleteedit-replacement/`. |
| 2026-07-11 | **① Autocomplete Mobile UX Pattern — Full-Screen Expansion Guideline** | 🔵 Deferred | Goal: guideline retained as documented future UX only — no longer a gate for any task (DX adoption decision 2026-07-19). Gate: Helder authorship review still pending on the written guideline. Pointer: `DevCycleCraft/autocomplete-component/2026-07-19-dx-autocomplete-adoption-decision.md`. |
| 2026-06-12 | CRUD page structural reduction — lazy BottomSheet + lazy SearchAppBar | 💡 Pending | Goal: faster Debug page loads; deferred — Release already instantaneous. Pointer: `cross-cutting/crud-page-structural-reduction/`. |
| — | **UI-2nd-refactor** | 📋 Spec | Goal: centralized UI codebase for mobile/windows/web — Blazor Hybrid + MudBlazor + shared RCL long-term direction. Gate: explicitly post-MVP (Helder decision 2026-07-19); first web need ships as a plain Blazor web app sharing Services. Pointer: `DevCycleCraft/UI-2nd-refactor/2026-07-19-post-mvp-sequencing-decision.md`. |
| 2026-06-12 | Large-volume data stress test (1–2 year seed) | 💡 Pending | Goal: verify list/search/queue performance at 1–2 years of realistic data; mandatory before MVP ship. Pointer: `cross-cutting/large-volume-data-stress-test/`. |
| 2026-06-12 | Cross-device / OS version compatibility test | 💡 Pending | Goal: min-API through Android 16, multiple sizes and OEM skins, via device farms; mandatory before MVP ship. Pointer: `cross-cutting/cross-device-os-compatibility-test/`. |
| 2026-06-12 | Play Store + Samsung Galaxy Store pre-submission compliance | 💡 Pending | Goal: pass store automated pre-review (target API, permissions, data safety, assets). Pointer: `cross-cutting/store-presubmission-compliance/`. |
| 2026-06-12 | Full pre-release mobile testing checklist (all categories) | 💡 Pending | Goal: tick off every pre-release test category before MVP public release. Pointer: `cross-cutting/pre-release-mobile-testing-checklist/`. |
| 2026-06-13 | **Session Continuity — Task Leasing & Auto-Resume** | 🟡 In Progress | Goal: lease-based collision safety + auto-resume across sessions; merged to develop. Gate: Helder live two-terminal demo (row below) before ✅. Pointer: `DevCycleCraft/session-continuity-leasing/`. |
| 2026-06-27 | **Infra Repository Folder Consolidation** | 💡 Pending | Goal: merge `Infra/Repository/` and `Infra/Repositories/` into one folder (moves + namespaces only). Pointer: `cross-cutting/infra-repository-folder-consolidation/`. |
| 2026-06-27 | **Read Model + Global NoTracking Pattern — Guidelines Update** | 💡 Pending | Goal: encode the BUG-018 canonical patterns into the library rules. Gate: BUG-018 on-device smoke test. Pointer: `cross-cutting/read-model-notracking-guidelines/`. |
| 2026-06-27 | **CRUD Read Model Refactoring — Persons, Songs, Venues** | 💡 Pending | Goal: apply the read-model pattern to the remaining CRUD entities and retire the list DTOs. Gate: Guidelines Update done first. Pointer: `cross-cutting/crud-read-model-refactoring/`. |
| 2026-06-30 | **Form validation** | 🟡 In Progress | Goal: establish and apply validation patterns to all form entries (guide + 5 form updates shipped; open bugs below). Pointer: `DevCycleCraft/ui-form-validation-guide/`. |
| 2026-07-09 | **Spec Evolution, Versioning & Feature-Folder Organization** | 🟡 In Progress | Goal: shipped specs become immutable history; bugs/changes get dated nested folders (`bugs/`, `changes/`); BACKLOG becomes generated from folder frontmatter instead of hand-maintained. Gate: migration merged to develop 2026-07-26; Helder must authorship-review the T13 rules bundle before it commits. Pointer: `DevCycleCraft/spec-evolution-versioning/`. |
<!-- BACKLOG:GENERATED:END dev-cycle-craft -->
