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
| 2026-05 | **Artists & Songs Catalog** | 🔴 Blocked | Goal: full artist/song catalog management. Gate: BUG-068 (Critical) — an EF Core tracking conflict aborts every edit-mode song save; smoke test 16C.1 must re-run green before phases 16C.2–16C.5 resume. Pointer: `BusinessFeatures/artists-songs/`. |
| 2026-07-21 | ↳ **Song artist field — correctness fixes + inline "create new artist"** | 🟡 In Progress | Goal: make the Song Artist autocomplete correct (folding in BUG-050, BUG-051, BUG-052 and retain-text) and add inline create-new-artist (➕ row), closing BUG-027. Gate: on-device re-run #5 failed 2026-08-02 — an EF Core tracking conflict blocks every edit-mode save; three split fix sessions plus a green re-run gate closeout. Pointer: `BusinessFeatures/artists-songs/changes/2026-07-21-inline-artist-create/task-log.md`. |
| 2026-07-03 | ↳ BUG-030: ArtistFormPage search strip UX unclear (spec gap) | 🔵 Deferred | Goal: resolve the search-strip spec gap on the Artist form. Gate: Answered by Helder 2026-07-10: the element must disappear from both forms — folded into the Form UX Redesign. Pointer: `BusinessFeatures/artists-songs/bugs/2026-07-03-BUG-030-artistformpage-search-strip-ux-unclear/`. |
| 2026-07-10 | ↳ **Artist & Song Form UX Redesign — autocomplete, similar-match warning, search-strip removal** | 🔵 Deferred | Goal: friction-free artist/song name entry (local + API autocomplete, never clear typed text, similar-match warning before create). Gate: parked by Helder only — the autocomplete foundations that once gated it are both retired; partial work sits on branch `feature/form-ux-redesign`. Now also owns cancelled BUG-028, BUG-029, BUG-031 and BUG-032. Pointer: `BusinessFeatures/artists-songs/changes/2026-07-10-form-ux-redesign/`. |
| 2026-08-03 | ↳ BUG-071 (alias BUG-068): Edit-mode song save fails — EF Core identity conflict (Critical) | 🟡 In Progress | Goal: editing a saved song must persist; today one tap reports success but writes nothing (silent data loss) and a second tap throws an EF tracking conflict. Gate: Fixed for Songs and Artists by the unit-of-work pilot. Queue and Event still carry the defect until Phase 3.5, so this stays open. Pointer: `BusinessFeatures/artists-songs/bugs/2026-08-03-BUG-071-bug-071-alias-bug-068-edit-mode-song-save-fails-ef-core-identity-conflict-critical/`. |
| 2026-08-04 | ↳ Song writes propagate to the artist catalog | 💡 Pending | Goal: Creating a song must also create the matching record in that artist's catalog, and editing a song must be reflected there. Today the two can drift apart. Gate: Spec first: confirm what SongFormPage already anticipates, then trace to a new or existing acceptance criterion before any code. Pointer: `BusinessFeatures/artists-songs/changes/2026-08-04-song-writes-propagate-to-the-artist-catalog/`. |
| 2026-06 | Dead-code cleanup: superseded `QueueService`/`IQueueService` | 💡 Pending | Goal: verify-then-delete the dead service pair. Gate: verified dead 2026-07-15 (no DI registration, no runtime consumer) — ready for the delete step. Pointer: `BusinessFeatures/queue-management/queue-deadcode-cleanup.md`. |
| 2026-06 | **Queue Entry Point Redesign — QueuePage as CRUD event list** | 💡 Pending | Goal: QueuePage becomes the CRUD list of events (FAB creates a queue; tap opens QueueManagementPage); EventsPage deleted. Gate: audit 2026-07-15 found NO implementation ever landed (registration only) — QueueManagementPage is unreachable in the app; Helder to re-prioritize. Pointer: `BusinessFeatures/queue-management/task-log.md`. |
| 2026-06 | **User Tutorial/Learning** | 💡 Pending | Goal: lowest-effort first tutorial version, updated as features ship. Pointer: `cross-cutting/user-tutorial-learning/`. |
| 2026-06 | **Website** | 💡 Pending | Goal: evaluate a site for marketing/documentation/community — myvocalist.com / myvocalist.app. Pointer: `cross-cutting/website/`. |
| 2026-06 | | 🏁 **MVP release** | |
| — | **Data Backup & Restore — Tier 2 (WiFi Mirror)** | 💡 Pending | Goal: second device on the same WiFi mirrors data in real time; fresh installs restore in one tap. Gate: Tier 1 shipped (done). Pointer: `BusinessFeatures/backup-restore/design.md`. |
| 2026-08-04 | ↳ RestoreFromBundleAsync overwrites the live SQLite file while AppDbContext holds it open | 💡 Pending | Goal: BackupService.RestoreFromBundleAsync:137 does File.Copy(snapshotFile, _dbPath, overwrite: true) on the same path AppDbContext's connection string points at, with no context dispose and no connection close. Gate: Found during UOW plan verification 2026-08-04; unrelated to the unit-of-work rollout and deliberately excluded from it. Pointer: `BusinessFeatures/backup-restore/bugs/2026-08-04-BUG-073-restorefrombundleasync-overwrites-the-live-sqlite-file-while-appdbcontext-holds-it-open/`. |
| — | **Singer self-registration** | 💡 Pending | Goal: singers register via public link / kiosk / own device connected to the host. Pointer: `cross-cutting/singer-self-registration/`. |
| — | **Social features** | 💡 Pending | Goal: post-event sharing, singer stats. Pointer: `cross-cutting/social-features/`. |
| 2026-07-03 | **Cross-cutting** | — | Bugs with no single parent business feature |
| 2026-07-03 | ↳ BUG-026: HWUI native crash (SIGABRT) on render teardown (Major) | 💡 Pending | Goal: confirm whether the crash is a real defect or debugger-teardown noise (Release logcat investigation first). Pointer: `BusinessFeatures/cross-cutting/bugs/2026-07-03-BUG-026-hwui-sigabrt-render-teardown/`. |
<!-- BACKLOG:GENERATED:END business-features -->

---

## Dev Cycle Craft

> Infrastructure, tooling, architecture, and process improvements that support business feature delivery.

<!-- BACKLOG:GENERATED:BEGIN dev-cycle-craft -->
| Target | Activity | Status | Notes |
|--------|----------|--------|-------|
| 2026-07-12 | **Inline Trivial Fix (ITF) lane — bounded orchestrator inline-edit exception** | 🟡 In Progress | Goal: let the orchestrator apply a fully-diagnosed 1-file/≤5-line fix inline instead of paying a ~25–35k-token subagent round-trip. Gate: Helder observes the first live ITF fix end-to-end before ✅ — opportunistic, waits for a qualifying fix. Pointer: `DevCycleCraft/inline-trivial-fix/`. |
| 2026-08-18 | ↳ Recalibrate agent-dispatch settings — orchestrator read-ban and subagent-for-trivia | 💡 Pending | Goal: Agent-dispatch settings over-trigger: the orchestrator is barred from reading any source file, and tiny tasks spawn fresh subagents that reload full context they never use. Gate: Review alongside the ITF lane's first live fix so both share one calibration pass. Pointer: `DevCycleCraft/inline-trivial-fix/changes/2026-08-18-recalibrate-agent-dispatch-settings-orchestrator-read-ban-and-subagent-for-trivia/`. |
| 2026-05 | **Inline Undo Pattern — UX Standard** | 💡 Pending | Goal: snackbar Undo (commit-first) standard for all inline destructive actions inside forms. Pointer: `cross-cutting/inline-undo-pattern/`. |
| 2026-06-27 | **Mandatory Worktree Rule Enforcement — ALL Subagent Work** | 💡 Pending | Goal: worktrees mandatory for every dispatch. Gate: largely delivered 2026-07-14 via the branch-lock-avoidance work — confirm remaining rule-doc updates then close. Pointer: `cross-cutting/mandatory-worktree-rule-enforcement/`. |
| 2026-06 | **IAsyncRelayCommand Standardization** | 💡 Pending | Goal: one async-command pattern across all ViewModels (picker VMs are the reference). Pointer: `cross-cutting/iasyncrelaycommand-standardization/`. |
| 2026-07-15 | **String trimming on persistence — centralized normalization analysis** | 🟡 In Progress | Goal: strings persisted to the DB should be trimmed (extension of BUG-046's query-side trimming) via one centralized Services-layer helper (search) + EF Core `ValueConverter`s (persistence). Gate: All code merged to develop 2026-08-04 — search normalization and the persistence ValueConverters are both live. Only Helder's on-device E2E sign-off remains before this goes terminal. Pointer: `DevCycleCraft/persisted-string-trimming/`. |
| 2026-07-19 | **`MyVocaList.Extensions` layer guidelines — placement criteria + rules-file promotion** | 💡 Pending | Goal: formalize when a helper belongs in the new dependency-free `MyVocaList.Extensions` project (created by D4 above) vs. Services/Domain, beyond the one worked example. Gate: Unblocked 2026-08-04 — the project now exists and is in use on develop; what remains is promoting the criteria into a rules or library file. Pointer: `DevCycleCraft/extensions-layer-guidelines/`. |
| 2026-06 | **Bug: Shell navigation swallows button tap animations** | 💡 Pending | Goal: let tap animations complete before Shell navigation begins (affects all flyout items). Pointer: `cross-cutting/shell-navigation-tap-animations/`. |
| 2026-06 | **Bug/Verify: FloatingToolbar always visible — should appear only on multi-select** | 💡 Pending | Goal: confirm intended visibility behavior against the original design, then fix or close as won't-fix. Pointer: `cross-cutting/floatingtoolbar-visibility-verify/`. |
| 2026-06-12 | CRUD page structural reduction — lazy BottomSheet + lazy SearchAppBar | 💡 Pending | Goal: faster Debug page loads; deferred — Release already instantaneous. Pointer: `cross-cutting/crud-page-structural-reduction/`. |
| — | **UI-2nd-refactor** | 📋 Spec | Goal: centralized UI codebase for mobile/windows/web — Blazor Hybrid + MudBlazor + shared RCL long-term direction. Gate: explicitly post-MVP (Helder decision 2026-07-19); first web need ships as a plain Blazor web app sharing Services. Pointer: `DevCycleCraft/UI-2nd-refactor/2026-07-19-post-mvp-sequencing-decision.md`. |
| 2026-06-12 | Large-volume data stress test (1–2 year seed) | 💡 Pending | Goal: verify list/search/queue performance at 1–2 years of realistic data; mandatory before MVP ship. Pointer: `cross-cutting/large-volume-data-stress-test/`. |
| 2026-06-12 | Cross-device / OS version compatibility test | 💡 Pending | Goal: min-API through Android 16, multiple sizes and OEM skins, via device farms; mandatory before MVP ship. Pointer: `cross-cutting/cross-device-os-compatibility-test/`. |
| 2026-06-12 | Play Store + Samsung Galaxy Store pre-submission compliance | 💡 Pending | Goal: pass store automated pre-review (target API, permissions, data safety, assets). Pointer: `cross-cutting/store-presubmission-compliance/`. |
| 2026-06-12 | Full pre-release mobile testing checklist (all categories) | 💡 Pending | Goal: tick off every pre-release test category before MVP public release. Pointer: `cross-cutting/pre-release-mobile-testing-checklist/`. |
| 2026-06-13 | **Session Continuity — Task Leasing & Auto-Resume** | 🟡 In Progress | Goal: lease-based collision safety + auto-resume across sessions; merged to develop. Gate: Helder live two-terminal demo (row below) before ✅. Pointer: `DevCycleCraft/session-continuity-leasing/`. |
| 2026-06-27 | **Infra Repository Folder Consolidation** | 💡 Pending | Goal: merge `Infra/Repository/` and `Infra/Repositories/` into one folder (moves + namespaces only). Pointer: `cross-cutting/infra-repository-folder-consolidation/`. |
| 2026-06-27 | **Read Model + Global NoTracking Pattern — Guidelines Update** | 💡 Pending | Goal: encode the BUG-018 canonical patterns into the library rules. Gate: BUG-018 on-device smoke test. Pointer: `cross-cutting/read-model-notracking-guidelines/`. |
| 2026-08-03 | ↳ DbContext lifetime & unit-of-work pattern — MAUI has no per-page scope | 🟡 In Progress | Goal: AddDbContext registers Scoped but MAUI never creates a scope, so one AppDbContext lives for the whole app session and leaks tracked entities between operations (root cause of BUG-068). Establish one correct unit-of-work pattern with minimal repeated code. Gate: Phases 0-3 merged to develop; Phase 3.5 and Phase 4+ remain, each gated on a spec. Pointer: `cross-cutting/read-model-notracking-guidelines/changes/2026-08-03-dbcontext-lifetime-unit-of-work-pattern-maui-has-no-per-page-scope/`. |
| 2026-08-04 | ↳ Apply the unit-of-work pattern to Queue and Event entities (deferred) | 💡 Pending | Goal: Queue and Event code is excluded from the unit-of-work rollout pending their own full refactor, so they keep using the session-lifetime context and stay exposed to the tracking-conflict defect. Gate: Starts only once the pattern is established in the guides; the six embedded repository saves live here. Pointer: `cross-cutting/read-model-notracking-guidelines/changes/2026-08-04-apply-the-unit-of-work-pattern-to-queue-and-event-entities-deferred/`. |
| 2026-08-04 | ↳ Merge duplicate repository families into one (Infra/Repository + Infra/Repositories) | 💡 Pending | Goal: Two repository folders exist by accident from prior sessions; they must become one family so later refactors touch a single code path. Gate: Runs AFTER the pilot proves the pattern (Helder 2026-08-04, option a); merged with the deferred Queue/Event unit-of-work item. Pointer: `cross-cutting/read-model-notracking-guidelines/changes/2026-08-04-merge-duplicate-repository-families-into-one-infra-repository-infra-repositories/`. |
| 2026-08-18 | ↳ Flaky ObjectDisposedException in the UOW SQLite test harness | 💡 Pending | Goal: Integration tests intermittently fail with ObjectDisposedException on SQLitePCL.sqlite3 during EnsureCreated, landing on a different test each run; affected tests pass in isolation. Gate: Suspected pooling/disposal interaction in the temp-file SQLite harness. Threatens the Phase 3.1 suite-green gate, whose whole value is a trustworthy signal. Pointer: `cross-cutting/read-model-notracking-guidelines/bugs/2026-08-18-BUG-076-flaky-objectdisposedexception-in-the-uow-sqlite-test-harness/`. |
| 2026-06-27 | **CRUD Read Model Refactoring — Persons, Songs, Venues** | 💡 Pending | Goal: apply the read-model pattern to the remaining CRUD entities and retire the list DTOs. Gate: Guidelines Update done first. Pointer: `cross-cutting/crud-read-model-refactoring/`. |
| 2026-06-30 | **Form validation** | 🟡 In Progress | Goal: establish and apply validation patterns to all form entries (guide + 5 form updates shipped; open bugs below). Pointer: `DevCycleCraft/ui-form-validation-guide/`. |
| 2026-08-04 | ↳ Pre-commit test gate blocks the RED-first commit that bug-tracking mandates | 💡 Pending | Goal: The pre-commit hook refuses any commit containing a failing test, but a Critical bug's regression test must be committed before its fix. The two rules cannot both be obeyed. Gate: Found during UOW Phase 0; Helder authorised the no-verify bypass for RED commits as an interim workaround. Pointer: `DevCycleCraft/hooks-redesign/bugs/2026-08-04-BUG-074-pre-commit-test-gate-blocks-the-red-first-commit-that-bug-tracking-mandates/`. |
| 2026-08-04 | ↳ Hook scripts invoked by relative path — guards break or go silently inert outside repo root | 💡 Pending | Goal: The pre-tool constitutional guard is invoked by a relative path, so it fails whenever the working directory is not the repo root and takes Edit and Write down with it. Gate: Anchor hook commands to the project-dir variable, then verify both guards still run when started from a subdirectory. Pointer: `DevCycleCraft/spec-evolution-versioning/bugs/2026-08-04-BUG-072-hook-scripts-invoked-by-relative-path-guards-break-or-go-silently-inert-outside-repo-root/`. |
| 2026-08-18 | ↳ Pre-commit hook inoperative in worktrees: dotnet test without --no-restore aborts on NETSDK1147 | 💡 Pending | Goal: The pre-commit hook runs dotnet test without --no-restore; in a worktree the restore fails with NETSDK1147, so the hook exits before running a single test. Gate: Found in UOW Phase 2. The test gate silently does not run in any worktree, and every worktree commit needs --no-verify for an unrelated reason. Pointer: `DevCycleCraft/hooks-redesign/bugs/2026-08-18-BUG-075-pre-commit-hook-inoperative-in-worktrees-dotnet-test-without-no-restore-aborts-on-netsdk1147/`. |
<!-- BACKLOG:GENERATED:END dev-cycle-craft -->
