# BACKLOG Archive — 2026-05

> Closed backlog rows completed in 2026-05, moved out of `Docs/Management/BACKLOG.md` (restructure 2026-07-15, `Docs/Management/DevCycleCraft/backlog-purpose-review/`). Rows use the slim PO template: Goal + one-sentence outcome + pointer. **Past BUG-NNN / feature lookups must grep all `backlog-archive/` files.**

## Business Features

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-05 | ↳ Song Karaoke URLs (Artists & Songs Catalog) | ✅ Done | Goal: YouTube URL management per song. Shipped (SongFormPage section, settings, converters, tests). Pointer: `Docs/Management/BusinessFeatures/artists-songs/youtube-karaoke/`. |
| 2026-05 | ↳ Bug: GoToSettings navigation exception | ✅ Fixed | Goal: fix navigation crash from SongFormPage to Settings. Fixed with a single-line absolute-route change. Pointer: `Docs/Management/BusinessFeatures/artists-songs/task-log.md`. |

## Dev Cycle Craft

| Target | Activity | Status | Notes |
|--------|----------|--------|-------|
| 2026-05-07 | SDD Master Plan (Phases 1–11, 162 steps) | ✅ Done | Goal: adopt Spec-Driven Development across the project. Completed. Pointer: `Docs/Management/DevCycleCraft/sdd/impl-master-plan.md`. |
| 2026-05-07 | workflow.md Reduction | ✅ Done | Goal: compress workflow rules into agent role files. Completed (61 findings resolved). Pointer: `Docs/Management/DevCycleCraft/workflow-compression/`. |
| 2026-05-08 | CLAUDE.md Deep Restructure | ✅ Done | Goal: move coding rules to `.claude/library/` and gate Docs/ context scope. Completed. Pointer: `Docs/Management/DevCycleCraft/docs-context-scope-control/`. |
| 2026-05-12 | Architecture Tests Evaluation | ✅ Done | Goal: decide on NetArchTest/ArchUnitNET adoption. Evaluated — no changes produced. Pointer: `Docs/Management/DevCycleCraft/architecture-tests-evaluation/`. |
| 2026-05-15 | Claude Managed Agents Evaluation | ✅ Done | Goal: evaluate Anthropic hosted agents for the dev cycle. Discarded — duplicates existing workflow. Pointer: `Docs/Management/DevCycleCraft/claude-managed-agents-evaluation/`. |
| 2026-05-13 | Day-to-day task management workflow review | ✅ Done | Goal: make BACKLOG.md the first-class SCRUM board. Completed (workflow.md Rules 1/7 updated). Pointer: `Docs/Management/DevCycleCraft/backlog-workflow-integration/`. |
| 2026-05-31 | **App Versioning Strategy** | ✅ Done | Goal: git-tag-driven semver. Shipped (MinVer + release command). Pointer: `Docs/Management/DevCycleCraft/app-versioning/`. |
| 2026-05 | **VS Solution File Registration Rule** | ✅ Done | Goal: every Docs file visible in VS via `.sln` registration. Rule encoded in `workflow.md`/`constraints-registry.md`. Pointer: `.claude/rules/constraints-registry.md`. |
| 2026-05 | **Proactive BACKLOG Entry Rule** | ✅ Done | Goal: agents register untracked work in BACKLOG.md before proceeding. Rule encoded in `workflow.md` Rule 1. Pointer: `.claude/rules/workflow.md`. |
