# MyVocaList — Product Backlog

> **Product backlog — a PO-level business artifact.** Business features are ordered by target delivery date. Every feature begins as `💡 Pending` and is promoted through the lifecycle below.
>
> **When to read:** At the start of any new feature cycle (workflow.md Rule 1 step 0) and when resuming a session with no active handoff file (Rule 7).
>
> **Who updates statuses:** The main agent updates this file at each workflow milestone. Subagents do not touch BACKLOG.md.

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

| Target | Feature | Status | Notes |
|--------|---------|--------|-------|
| 2026-03 | Bug: Venues list fetch slow — 2.2s paged query (BUG-012) | 💡 Pending | Goal: restore fast venue list loading (N+1 query suspected). Pointer: `BusinessFeatures/venues/bugs/BUG-012-venuesviewmodel-fetch-slow.md`. |
| 2026-05 | **Artists & Songs Catalog** | 🔴 Blocked | Goal: full artist/song catalog management. Gate: BUG-027 (Critical) makes song registration impossible — smoke test 16C.1 must re-run green before phases 16C.2–16C.5 resume. Pointer: `BusinessFeatures/artists-songs/`. |
| 2026-07-03 | ↳ BUG-027: SongFormPage Artist field — no validation, no autocomplete, blur clears typed text (Critical) | 💡 Pending | Goal: make song creation possible again. Gate: fix direction owned by the parked *Artist & Song Form UX Redesign*, which depends on autocomplete foundations ① + ②. Pointer: `BusinessFeatures/artists-songs/task-log.md`. |
| 2026-07-03 | ↳ BUG-028: ArtistsPage trailing catalog button no-op — regression of BUG-015/019 (Major) | 💡 Pending | Goal: trailing button must navigate to the artist's catalog. Pointer: `BusinessFeatures/artists-songs/bugs/BUG-019-artistspage-listitem-button-noop/`. |
| 2026-07-03 | ↳ BUG-029: ArtistFormPage search-strip icon crashes the app (Critical) | 🔵 Deferred | Deferred: the search-strip element is slated for deletion by the Form UX Redesign; re-triage only if any part of the strip survives. Pointer: `BusinessFeatures/artists-songs/task-log.md`. |
| 2026-07-03 | ↳ BUG-030: ArtistFormPage search strip UX unclear (spec gap) | 🔵 Deferred | Answered by Helder 2026-07-10: the element must disappear from both forms — folded into the Form UX Redesign. Pointer: `BusinessFeatures/artists-songs/task-log.md`. |
| 2026-07-03 | ↳ BUG-031/032: no API autocomplete while typing Artist Name / Song Title (spec gap) | 🔵 Deferred | Answered by Helder 2026-07-10: autocomplete (local + API) IS required on both entries — folded into the Form UX Redesign. Pointer: `BusinessFeatures/artists-songs/task-log.md`. |
| 2026-07-10 | ↳ **Artist & Song Form UX Redesign — autocomplete, similar-match warning, search-strip removal** | 🔵 Deferred | Goal: friction-free artist/song name entry (local + API autocomplete, never clear typed text, similar-match warning before create). Gate: parked by Helder; gated on autocomplete foundations ① + ② (~6/14 tasks done on branch `feature/form-ux-redesign`). Pointer: `BusinessFeatures/artists-songs/changes/2026-07-10-form-ux-redesign/`. |
| 2026-07-10 | ↳ Form presentation — bottom-sheet/modal conversion for simple forms | 💡 Pending | Goal: simple CRUD forms (Artist, Venue) open as bottom sheets instead of full pages. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-07-11 | **Form & Autocomplete UX Overhaul** | 💡 Pending | Goal: umbrella sequencing all form-presentation + AppBar-save + adaptive-autocomplete changes. Gate: foundation order ② → component build → first application → ①; forms convert in order Venue → Artist → Singer. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-07-11 | ↳ Venue form → bottom-sheet conversion | 💡 Pending | Goal: first (pilot) conversion — single entry, no autocomplete; predecessor of all other form-presentation tasks. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-07-11 | ↳ Artist form → bottom-sheet conversion | 💡 Pending | Gate: Venue pilot success + autocomplete foundations ① & ②. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-07-11 | ↳ Singer (Person) form → bottom-sheet conversion (candidate) | 💡 Pending | Goal: evaluate whether the Person form benefits from a sheet. Gate: Venue pilot success (+ ① & ② if converted). Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-07-11 | ↳ CRUD lists → AppBar + SearchBar logic enhancement | 💡 Pending | Goal: apply the AppBar/SearchAppBar redesign across all CRUD list pages; parallel-capable. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-06 | ↳ YouTube API Key — Automated Setup via AI Agent | 💡 Pending | Goal: research automating Google Cloud project + YouTube API key setup for users (manual flow kills adoption). Pointer: `BusinessFeatures/search-picker/task-log.md`. |
| 2026-06 | ↳ YouTube Share Intent | 💡 Pending | Goal: share-from-YouTube replaces the API-key requirement for adding karaoke URLs. Gate: pre-spec analysis (intent setup, metadata extraction, upsert strategy). Pointer: `BusinessFeatures/youtube-share/findings.md`. |
| 2026-06-13 | ↳ **Song Import & Entity Resolution** | 🟡 In Progress | Goal: senior-grade insert-vs-update engine for manual + API song data (version variants, fuzzy matching, safe merge). Gate: Helder emulator smoke test (Wave 5) before ✅. Pointer: `BusinessFeatures/artists-songs/song-import-resolution/`. |
| 2026-06 | ↳ Pre-release checklist (Crash & Error Reporting) | 💡 Pending | Goal: fill Sentry DSN, run smoke test, add multi-env DSN if needed. Pointer: `BusinessFeatures/crash-reporting/`. |
| 2026-06 | Dead-code cleanup: superseded `QueueService`/`IQueueService` | 💡 Pending | Goal: verify-then-delete the dead service pair. Pointer: `BusinessFeatures/queue-management/queue-deadcode-cleanup.md`. |
| 2026-06 | **Queue Entry Point Redesign — QueuePage as CRUD event list** | 🟡 In Progress | Goal: QueuePage becomes the CRUD list of events (FAB creates a queue; tap opens QueueManagementPage); EventsPage deleted. Pointer: `BusinessFeatures/queue-management/`. |
| 2026-06 | ↳ BUG-013: QueueListItem bypasses ListItem — MD3 non-compliance (Major) | 🟡 In Progress | Goal: migrate queue list items to the standard `ListItem` component. Pointer: `BusinessFeatures/queue-management/task-log.md`. |
| 2026-06 | ↳ BUG-014: `GetActiveEventAsync` fetches only 1 event — wrong business logic (Critical) | 🟡 In Progress | Goal: active-event lookup must consider all events, not only the most recent. Pointer: `BusinessFeatures/queue-management/task-log.md`. |
| 2026-06 | **User Tutorial/Learning** | 💡 Pending | Goal: lowest-effort first tutorial version, updated as features ship. |
| 2026-06 | **Website** | 💡 Pending | Goal: evaluate a site for marketing/documentation/community — myvocalist.com / myvocalist.app. |
| 2026-06 | | 🏁 **MVP release** | |
| - | **Data Backup & Restore — Tier 2 (WiFi Mirror)** | 💡 Pending | Goal: second device on the same WiFi mirrors data in real time; fresh installs restore in one tap. Gate: Tier 1 shipped (done). Pointer: `BusinessFeatures/backup-restore/design.md`. |
| — | **Singer self-registration** | 💡 Pending | Goal: singers register via public link / kiosk / own device connected to the host. |
| — | **Social features** | 💡 Pending | Goal: post-event sharing, singer stats. |
| — | **Windows version** | 🔴 Blocked | Gate: DevExpress MAUI has no Windows renderer — re-evaluate when DX announces support. Pointer: `BusinessFeatures/windows-version/design.md`. |
| 2026-07-03 | **Cross-cutting** | — | Bugs with no single parent business feature |
| 2026-07-03 | ↳ BUG-026: HWUI native crash (SIGABRT) on render teardown (Major) | 💡 Pending | Goal: confirm whether the crash is a real defect or debugger-teardown noise (Release logcat investigation first). Pointer: `Docs/Management/cross-cutting-log.md`. |

---

## Dev Cycle Craft

> Infrastructure, tooling, architecture, and process improvements that support business feature delivery.

| Target | Activity | Status | Notes |
|--------|----------|--------|-------|
| 2026-07-11 | **Documentation & spec-tracking governance — where docs live** | 💡 Pending | Goal: standing rule/mechanism so docs never strand on feature branches (interim rule: docs commit to `develop`). Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-07-12 | **Evaluate guideline update — allow inline trivial-task execution to save tokens** | 💡 Pending | Goal: token-cost evaluation of letting agents perform genuinely trivial edits inline instead of dispatching a subagent; merge-vs-delegate decisions stay with the orchestrator. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-05 | **Workflow & Folder Layout Alignment** | 🟡 In Progress | Goal: resolve SDD/skills/custom-rules conflicts and canonicalize the Docs/ layout. Pointer: `DevCycleCraft/workflow-folder-layout-alignment/`. |
| 2026-05 | **Inline Undo Pattern — UX Standard** | 💡 Pending | Goal: snackbar Undo (commit-first) standard for all inline destructive actions inside forms. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-06-27 | **Mandatory Worktree Rule Enforcement — ALL Subagent Work** | 💡 Pending | Goal: worktrees mandatory for every dispatch. Gate: largely delivered 2026-07-14 via the branch-lock-avoidance work — confirm remaining rule-doc updates then close. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-06 | **Search Pattern Standardization + Navigation Result Service** | 💡 Pending | Goal: reconcile the app's two search patterns into one canonical choice + migration plan. Gate: blocks any new search surface until at least 📋 Spec. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-06 | **IAsyncRelayCommand Standardization** | 💡 Pending | Goal: one async-command pattern across all ViewModels (picker VMs are the reference). Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-06 | **Search Error State UX Standardization** | 💡 Pending | Goal: retrofit the picker pages' search-failure state to all CRUD search pages. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-06 | **Filter Pattern Standardization** | 💡 Pending | Goal: standardized filter pattern (UI, binding contract, DB-side filtering) before any second CRUD page adds filters. Gate: blocks new filter additions. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-07-15 | **String trimming on persistence — centralized normalization analysis** | 💡 Pending | Goal: strings persisted to the DB should be trimmed (extension of BUG-046's query-side trimming); centralize the normalization only if zero friction is introduced — analysis first, Helder decides on the proposal. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-06 | **Bug: Shell navigation swallows button tap animations** | 💡 Pending | Goal: let tap animations complete before Shell navigation begins (affects all flyout items). Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-06 | **Bug/Verify: FloatingToolbar always visible — should appear only on multi-select** | 💡 Pending | Goal: confirm intended visibility behavior against the original design, then fix or close as won't-fix. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-07-10 | **AppBar / SearchAppBar Interaction Redesign — page-nav pattern + persistent search bar** | 💡 Pending | Goal: consistent hamburger/back behavior + kill the confusing bar-swap search toggle, validated against official MD3 docs. Gate: governed components — four-gate governance applies. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-07-11 | ↳↳ Build new MD3-compliant autocomplete component (②) | 🟡 In Progress | Goal: `AutocompleteMobileField` full-screen phone variant (desktop dropdown unchanged). Gate: BUG-043 closed ✅ 2026-07-15 (archived); remaining — BUG-044/045 Helder E2E + new BUG-046/047 and UX-analysis rows below. Pointer: `DevCycleCraft/autocomplete-component/`. |
| 2026-07-15 | ↳↳ BUG-044: duplicate PersonFormPage after Save via autocomplete — second save creates duplicate entity (Critical) | 🟡 In Progress | Goal: Save must return to the singers list, never reveal a stale second form. Gate: fix merged to develop 2026-07-15 (latent stacked-navigation defect, exposed by BUG-043 fix) — Helder on-device E2E pending, incl. back-gesture UX decision. Pointer: `DevCycleCraft/autocomplete-component/task-log.md`. |
| 2026-07-15 | ↳↳ BUG-045: name entry cursor stuck at leading position after autocomplete usage (Major) | 🟡 In Progress | Goal: cursor must be placeable at the end of the typed name. Gate: same root cause as BUG-044 (stale stacked form) — fix merged 2026-07-15; Helder manual E2E per task-log steps pending. Pointer: `DevCycleCraft/autocomplete-component/task-log.md`. |
| 2026-07-15 | ↳↳ BUG-046: autocomplete search — any extra space (leading/trailing/double between words) returns zero suggestions (Major) | 💡 Pending | Goal: normalize whitespace in the query string sent to the search service (never mutate the user's entry text), via a centralized reusable helper so the formatting isn't duplicated per caller. Registered by Helder 2026-07-15 (on-device). Pointer: `DevCycleCraft/autocomplete-component/task-log.md`. |
| 2026-07-15 | ↳↳ BUG-047: stale autocomplete suggestions popup floats over PersonFormPage/SongFormPage on Edit-mode load (Major) | 🟡 In Progress | Goal: Edit-mode load must show the loaded name cleanly, no stray popup; retyping to correct a name must still trigger dedup suggestions. Gate: root cause confirmed (programmatic `Text` hydration was firing the same search-trigger as user typing) — reentrancy-guard fix merged to develop 2026-07-15, verifier PASS 485/485; Helder on-device E2E pending (`task-log.md` Manual E2E steps). Pointer: `DevCycleCraft/autocomplete-component/task-log.md`. |
| 2026-07-15 | ↳↳ Autocomplete "no match → add new" UX analysis — back button is the only path to Add (all consumers) | 💡 Pending | Goal: when no suggestion is tapped, reaching "add person" requires tapping back — analyse for a better MD3-compliant affordance than back-button-only; solution is shared across every autocomplete consumer. Registered by Helder 2026-07-15. Pointer: `DevCycleCraft/autocomplete-component/task-log.md`. |
| 2026-07-12 | ↳↳ Evaluate shimmer/loading-state need for autocomplete (desktop + mobile) | 💡 Pending | Goal: decide whether shimmer/empty-state is needed for both autocomplete variants; if yes, append an implementation task following the existing `CrudListView` pattern. Pointer: `DevCycleCraft/autocomplete-component/task-log.md`. |
| 2026-07-11 | **① Autocomplete Mobile UX Pattern — Full-Screen Expansion Guideline** | 💡 Pending | Goal: encode the responsive autocomplete rule (desktop dropdown / phone full-screen) in the UX library rules. Gate: runs last — gated on ② + component build + first application. Pointer: `DevCycleCraft/autocomplete-component/`. |
| 2026-06-12 | CRUD page structural reduction — lazy BottomSheet + lazy SearchAppBar | 💡 Pending | Goal: faster Debug page loads; deferred — Release already instantaneous. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-06 | ↳ DbContext-per-operation architecture review | 💡 Pending | Goal: `IDbContextFactory` per-operation contexts, removing the static load gate; must align with the multi-provider direction. Gate: spec + Helder review required. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-06 | ↳ Flaky test: repository tests parallel SQLite race | 💡 Pending | Goal: stabilize intermittent repository-test failures under parallel execution. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-06 | ↳ Paged query optimization — Venue/Artist count subqueries | 💡 Pending | Goal: shorten shimmer time on data-heavy pages; evidence-driven (only if S23 still shows long shimmer). Pointer: `Docs/Management/cross-cutting-log.md`. |
| - | **UI-2nd-refactor** | 📋 Spec | Goal: centralized UI codebase for mobile/windows/web — Blazor Hybrid + MudBlazor + shared RCL chosen as long-term direction. Gate: parallel spike; Helder to verify MudMCP index. Pointer: `DevCycleCraft/UI-2nd-refactor/`. |
| 2026-06-12 | Large-volume data stress test (1–2 year seed) | 💡 Pending | Goal: verify list/search/queue performance at 1–2 years of realistic data; mandatory before MVP ship. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-06-12 | Cross-device / OS version compatibility test | 💡 Pending | Goal: min-API through Android 16, multiple sizes and OEM skins, via device farms; mandatory before MVP ship. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-06-12 | Play Store + Samsung Galaxy Store pre-submission compliance | 💡 Pending | Goal: pass store automated pre-review (target API, permissions, data safety, assets). Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-06-12 | Full pre-release mobile testing checklist (all categories) | 💡 Pending | Goal: tick off every pre-release test category before MVP public release. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-06-13 | **Session Continuity — Task Leasing & Auto-Resume** | 🟡 In Progress | Goal: lease-based collision safety + auto-resume across sessions; merged to develop. Gate: Helder live two-terminal demo (row below) before ✅. Pointer: `DevCycleCraft/session-continuity-leasing/`. |
| 2026-06-14 | ↳ ⏳ **Helder MANUAL TEST: live two-terminal lease demo** | 🟡 In Progress | Goal: verify fresh-claim avoidance, stale reclaim, and resume-pointer takeover live. Pointer: `DevCycleCraft/session-continuity-leasing/demo-and-traceability.md`. |
| 2026-06-14 | ↳ Feature-scope BACKLOG-row claiming (phased follow-up) | 💡 Pending | Goal: claim the BACKLOG feature row itself (deferred phase of the leasing design). Pointer: `DevCycleCraft/session-continuity-leasing/design.md`. |
| 2026-07-10 | ↳ Review: per-step progress tracking + dead-agent takeover — usefulness & definition audit | 💡 Pending | Goal: audit whether the leasing/checkpoint layers fully cover interruption takeover; report gaps to Helder before changes. Pointer: `DevCycleCraft/session-continuity-leasing/task-log.md`. |
| 2026-07-14 | ↳ Session Continuity enhancements — lease↔ledger↔checkpoint linking | 🟡 In Progress | Goal: claims carry branch/worktree/task and self-maintaining resume pointers; code complete, verifier PASS. Gate: Helder — two-terminal demo + worktree-triage decision. Pointer: `DevCycleCraft/session-continuity-leasing/task-log.md`. |
| 2026-06-20 | **BACKLOG-first Registration Enforcement** | 🟡 In Progress | Goal: work items must be registered in BACKLOG.md before memory writes (advisory Stop-hook posture). Gate: Helder — apply the workflow.md `amend:` from proposed-diffs, authorship review, AC-13 default confirmation. Pointer: `DevCycleCraft/backlog-first-registration/`. |
| 2026-06-13 | ↳ Context-Size Self-Monitoring & Auto-Clear Advisory | 💡 Pending | Goal: advise Helder when context is large enough to clear, with continuation prompt + handoff. Pointer: `DevCycleCraft/session-continuity-leasing/design.md`. |
| 2026-07-10 | ↳ Worktree-scoped context slimming — investigation | 💡 Pending | Goal: investigate per-worktree pruned context (fewer rules files loaded per subagent type). Pointer: `DevCycleCraft/per-agent-context-isolation/task-log.md`. |
| 2026-06-27 | **Infra Repository Folder Consolidation** | 💡 Pending | Goal: merge `Infra/Repository/` and `Infra/Repositories/` into one folder (moves + namespaces only). Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-06-27 | **Read Model + Global NoTracking Pattern — Guidelines Update** | 💡 Pending | Goal: encode the BUG-018 canonical patterns into the library rules. Gate: BUG-018 on-device smoke test. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-06-27 | **CRUD Read Model Refactoring — Persons, Songs, Venues** | 💡 Pending | Goal: apply the read-model pattern to the remaining CRUD entities and retire the list DTOs. Gate: Guidelines Update done first. Pointer: `Docs/Management/cross-cutting-log.md`. |
| 2026-06-30 | **Form validation** | 🟡 In Progress | Goal: establish and apply validation patterns to all form entries (guide + 5 form updates shipped; open bugs below). Pointer: `DevCycleCraft/ui-form-validation-guide/`. |
| 2026-07-03 | ↳ BUG-034: character counter renders duplicated past ~26 chars — Venue + Artist name fields (Minor) | 💡 Pending | Goal: fix the shared counter rendering defect. Pointer: `Docs/Management/BusinessFeatures/venues/form-validation-task-log.md`. |
| 2026-07-03 | ↳ BUG-035: PersonFormPage edit-load — full-name entry UI glitch (Minor) | 💡 Pending | Pointer: `Docs/Management/BusinessFeatures/persons/form-validation-task-log.md`. |
| 2026-07-03 | ↳ BUG-037: PersonFormPage edit-Save does not navigate back (Major) | 💡 Pending | Goal: consistent post-save navigation across CRUD forms; confirm the intended pattern before fixing. Pointer: `Docs/Management/BusinessFeatures/persons/form-validation-task-log.md`. |
| 2026-07-03 | ↳ BUG-038: PersonFormPage email-uniqueness inline error only appears after Save, not on blur (Major) | 💡 Pending | Same async-uniqueness-on-blur family as BUG-025/BUG-039 — candidate for one shared fix. Pointer: `Docs/Management/BusinessFeatures/persons/form-validation-task-log.md`. |
| 2026-07-02 | ↳ BUG-025: SingerForm async email-uniqueness error cleared by weaker re-validation (Major) | 💡 Pending | Goal: keystroke re-validation must not clear async-sourced errors; regression test mandatory. Pointer: `Docs/Management/BusinessFeatures/persons/form-validation-task-log.md`. |
| 2026-07-03 | ↳ BUG-039: ArtistFormPage duplicate-name inline error only appears after Save, not on blur (Major) | 💡 Pending | Same family as BUG-025/BUG-038. Pointer: `Docs/Management/BusinessFeatures/artists-songs/form-validation-task-log.md`. |
| 2026-07-09 | **Spec Evolution, Versioning & Feature-Folder Organization** | 💡 Pending | Goal: shipped specs become immutable history; changes get dated delta specs; feature folders get a nested-content pattern. Gate: holds back several rule amends until the design lands. Pointer: `DevCycleCraft/spec-evolution-versioning/findings.md`. |
| 2026-07-09 | ↳ Richer task-status vocabulary (beyond binary checkboxes) | 💡 Pending | Goal: evaluate a unified status vocabulary as part of the parent feature's design. Pointer: `DevCycleCraft/spec-evolution-versioning/findings.md`. |
