# MyVocaList — Product Backlog

> **Product backlog.** Business features are ordered by target delivery date. Every feature begins as `💡 Pending` and is promoted through the lifecycle below before any code is written.
>
> **When to read:** At the start of any new feature cycle (workflow.md Rule 1 step 0) and when resuming a session with no active handoff file (Rule 7).
>
> **Who updates statuses:** The main agent updates this file at each workflow milestone. Subagents do not touch BACKLOG.md.
>

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
| `✅ Done` | Shipped — changelog updated |

---

## Business Features

| Target | Feature | Status | Notes |
|--------|---------|--------|-------|
| 2026-03 | **Venues CRUD** | ✅ Done | Full MD3 list, search, multi-select, swipe-delete |
| 2026-04 | **Person CRUD** | ✅ Done | Autocomplete field, duplicate detection. Plan: `Docs/Management/BusinessFeatures/persons/plan.md` |
| 2026-05 | **Artists & Songs Catalog** | 🟡 In Progress | Spec: `Docs/Management/BusinessFeatures/artists-songs/` · Plan: `Docs/Management/BusinessFeatures/artists-songs/plan.md` |
| 2026-05 | ↳ Song Karaoke URLs | ✅ Done | YouTube URL management per song; SongFormPage section, settings, converters, tests. Spec: `Docs/Management/BusinessFeatures/artists-songs/youtube-karaoke/` |
| 2026-05 | ↳ Bug: GoToSettings navigation exception | ✅ Fixed | `GoToAsync("settings")` called from pushed-page context (SongFormPage); FlyoutItem requires absolute route `//settings`. Single-line fix in `SongFormViewModel.cs`. |
| 2026-06 | **Crash & Error Reporting** | ✅ Done | 2 bugs found in review (fixed). Spec: `Docs/Management/BusinessFeatures/crash-reporting/` |
| 2026-06 | ↳ Pre-release checklist | 💡 Pending | App-wide: fill Sentry DSN, run smoke test, add multi-env DSN if needed. Details: `tasks.md`. |
| 2026-06 | **What's New / Release Notes** | 📋 Spec | Bundled `releases.json`; one-time modal on version upgrade; depends on App Versioning. Spec: `Docs/Management/BusinessFeatures/whats-new/` |
| 2026-06 | **User Suggestions** | 📋 Spec | In-app form → GitHub Issues API (MyVocaList repo); auto-captures version + OS + timestamp. Spec: `Docs/Management/BusinessFeatures/user-suggestions/` |
| 2026-06 | **App Update Check** | 📋 Spec | Remote version manifest (GitHub raw); soft nudge + hard block bottom sheets; fail-open on network error. Spec: `Docs/Management/BusinessFeatures/app-update-check/` |
| 2026-06 | **App Settings** | ✅ Done | YouTube API key management (PasswordEdit, save/test/clear); flyout "Preferences" now navigates to SettingsPage; stale `HasYouTubeApiKey` refreshed on `OnAppearing`. Spec: `Docs/Management/BusinessFeatures/app-settings/` |
| 2026-06 | **About Page** | ✅ Done | Version, logo, goal sentence, Since year, CC BY-NC-ND 4.0 license, What's New stub (hidden). Spec: `Docs/Management/BusinessFeatures/about-page/` |
| 2026-06 | **Queue Management** | 💡 Pending | Core product: active queue, round-based progression, singer registration, absence tracking, completion time estimate |
| 2026-06 | **Visual Theme Refresh** | 💡 Pending | App UI is too dark and monochromatic; spike for richer accent colors, gradient surfaces, warmer tones - usage of google's stilch, then create a MCP server from it, then install into Claude Code MCP server, then BANN, Claude Code has everything needed to make it just as planned, ease like a charm |
| 2026-06 | **Data Backup & Restore — Tier 1 + 3** | ✅ Done | Local auto-backup (SQLite snapshot + transaction log) + manual share sheet export/restore. Plan: `Docs/Management/BusinessFeatures/backup-restore/plan.md`. Spec: `Docs/Management/BusinessFeatures/backup-restore/design.md` |
| 2026-06 | **User Tutorial/Learning** | 💡 Pending | Local or/and online tutorials. Evaluation of best practices in the lower possible effort to produce 1st version and update it always app receives new features and updates existing ones |
| 2026-06 | **Website** | 💡 Pending | Evaluate the usage of the website - for marketing, documentation, and community engagement - myvocalist.com / myvocalist.app |
| 2026-06 | | 🏁 **MVP release** | |
| - | **Data Backup & Restore — Tier 2 (WiFi Mirror)** | 💡 Pending | mDNS auto-discovery + TCP sync + AES-256 pairing code encryption. Second device on same WiFi auto-receives transaction log in real time; fresh install auto-discovers mirror and restores in one tap. Spec: `Docs/Management/BusinessFeatures/backup-restore/design.md § Tier 2`. Depends on Tier 1 being shipped. |
| — | **Singer self-registration** | 💡 Pending | Singers register via public link / kiosk device / self device app connected to host device or able to self register into the host somehow |
| — | **Social features** | 💡 Pending | Post-event sharing, singer stats |
| — | **Windows version** | 🔴 Blocked | Blocked on DevExpress MAUI Windows support (no Windows renderer exists). Re-evaluate when DX announces Windows support. Spec: `Docs/Management/BusinessFeatures/windows-version/design.md` |

---

## Dev Cycle Craft

> Infrastructure, tooling, architecture, and process improvements that support business feature delivery.

| Completed | Activity | Status | Plan / Reference |
|-----------|----------|--------|-----------------|
| 2026-03 | Solution Structure Refactor | ✅ Done | Move service interfaces to Domain, delete IDatabaseInit, reorganize MAUI project. Plan: `Docs/Management/DevCycleCraft/solution-structure-refactor/plan.md` |
| 2026-03 | MD3 App Bar Components | ✅ Done | SmallAppBar + SearchAppBar ContentView components. Plan: `Docs/Management/DevCycleCraft/md3-appbar-components/plan.md` |
| 2026-03 | M3 Lists | ✅ Done | `Docs/Management/DevCycleCraft/m3-lists/plan.md` |
| 2026-03 | Venues MD3 rebuild | ✅ Done | `Docs/Management/BusinessFeatures/venues/plan.md` |
| 2026-03 | Styles & Structure | ✅ Done | `Docs/Management/DevCycleCraft/styles-structure/plan.md` |
| 2026-04 | Toolbar/FAB vibrant | ✅ Done | `Docs/Management/DevCycleCraft/toolbar-fab-vibrant/plan.md` |
| 2026-04 | Autocomplete field | ✅ Done | `Docs/Management/BusinessFeatures/persons/plan-autocomplete.md` |
| 2026-04 | Hooks redesign | ✅ Done | Stop/TaskCreated/TaskCompleted hooks; session-end auto-commit. Plan: `Docs/Management/DevCycleCraft/hooks-redesign/plan.md` |
| 2026-05-07 | SDD Master Plan (Phases 1–11, 162 steps) | ✅ Done | `Docs/Management/DevCycleCraft/sdd/impl-master-plan.md` |
| 2026-05-07 | workflow.md Reduction | ✅ Done | Agent role files (`implementor.md`, `orchestrator.md`) absorb scattered responsibilities; 61 findings resolved. Plan: `Docs/Management/DevCycleCraft/workflow-compression/plan.md` |
| 2026-05-08 | CLAUDE.md Deep Restructure | ✅ Done | 7 coding rule files → `.claude/library/`; `.claudeignore` Docs/ scope gates. Plan: `Docs/Management/DevCycleCraft/docs-context-scope-control/plan.md` |
| 2026-05-12 | Architecture Tests Evaluation | ✅ Done | Research: NetArchTest.Rules vs ArchUnitNET adoption decision — evaluated, no changes produced. Plan: `Docs/Management/DevCycleCraft/architecture-tests-evaluation/plan.md` |
| 2026-05-15 | Claude Managed Agents Evaluation | ✅ Done | Research: Anthropic hosted agents for dev cycle — discarded; duplicates existing workflow and lacks community maturity. Plan: `Docs/Management/DevCycleCraft/claude-managed-agents-evaluation/plan.md` |
| 2026-05-13 | Day-to-day task management workflow review | ✅ Done | BACKLOG.md first-class SCRUM board; workflow.md Rule 1/7 updated. Plan: `Docs/Management/DevCycleCraft/backlog-workflow-integration/plan.md` |
| 2026-05-31 | **App Versioning Strategy** | ✅ Done | MinVer NuGet; git-tag-driven semver; `/project:release` command. Spec: `Docs/Management/DevCycleCraft/app-versioning/design.md` · Plan: `Docs/Management/DevCycleCraft/app-versioning/plan.md` |
| 2026-05 | **Workflow & Folder Layout Alignment** | 🟡 In Progress | Resolve conflicts between SDD, superpowers skills, and custom rules; canonicalize Docs/ layout; spec evolution tracking; review enforcement. Findings: `Docs/Management/DevCycleCraft/workflow-folder-layout-alignment/findings.md` · Plan: `Docs/Management/DevCycleCraft/workflow-folder-layout-alignment/plan.md` |
| 2026-05 | **VS Solution File Registration Rule** | ✅ Done | Mandatory rule: any doc file visible in VS must be registered in .sln before commit. See `workflow.md` and `constraints-registry.md`. |
| 2026-05 | **Proactive BACKLOG Entry Rule** | ✅ Done | Agents must add brief BACKLOG entries for untracked work identified during sessions. See `workflow.md` Rule 1. |
| 2026-05 | **Inline Undo Pattern — UX Standard** | 💡 Pending | All inline destructive actions (sub-item removals within a form page, where the user never navigates away) must offer snackbar Undo using the commit-first pattern: delete immediately, undo re-inserts. Discovered fixing broken undo in SongFormPage URL removal. Applies to any future inline remove within a form context. Does NOT apply to list-page batch deletes (different interaction model). |
| 2026-06 | **Enforce Git Worktrees for Parallel Subagents** | ✅ Done | Mandatory rule encoded in `orchestrator.md § Git Worktrees as Isolation Primitive` — threshold lowered from 3+ to 2+ subagents, made hard gate, staging-collision rationale added. `.worktrees/` confirmed gitignored. workflow.md pointer needs manual `amend:` commit (rules dir is write-protected). |
| 2026-06 | **Bug Tracking Procedure** | 💡 Pending | `workflow.md` has a Bug Fix Pattern (commit message as spec) but no rules for: nesting bugs under parent features in BACKLOG, when bugs get task-log entries, severity classification, or regression test requirements per bug class. Discovered while handling the GoToSettings navigation exception. |
| 2026-06 | **Haiku Model Assignment for Low-Risk Subagent Tasks** | ✅ Done | Classification table + eligibility rules + gate condition encoded in `orchestrator.md § Model Selection for Subagent Tasks`. Haiku-eligible: docs-only, .sln registration, migration-only, boilerplate scaffolding, XAML cosmetic, single-file rename. Gate: first use in each type must be verified against Sonnet output before encoding as default. |
