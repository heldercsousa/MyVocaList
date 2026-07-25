# BACKLOG Archive — 2026-05

> Closed backlog rows completed in 2026-05, moved out of `Docs/Management/BACKLOG.md` (restructure 2026-07-15, `Docs/Management/DevCycleCraft/backlog-purpose-review/`). Rows use the slim PO template: Goal + one-sentence outcome + pointer. **Past BUG-NNN / feature lookups must grep all `backlog-archive/` files.**

## Business Features

<!-- BACKLOG:GENERATED:BEGIN archive-business -->
| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-05 | **Song Karaoke URLs (Artists & Songs Catalog)** | ✅ Done | Goal: YouTube URL management per song. Shipped (SongFormPage section, settings, converters, tests). Pointer: `BusinessFeatures/artists-songs/youtube-karaoke/`. |
| 2026-05 | Bug: GoToSettings navigation exception | ✅ Fixed | Goal: fix navigation crash from SongFormPage to Settings. Fixed with a single-line absolute-route change. Pointer: `BusinessFeatures/artists-songs/task-log.md`. |
<!-- BACKLOG:GENERATED:END archive-business -->

## Dev Cycle Craft

<!-- BACKLOG:GENERATED:BEGIN archive-craft -->
| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-05 | **SDD Master Plan (Phases 1-11, 162 steps)** | ✅ Done | Goal: adopt Spec-Driven Development across the project. Completed. Pointer: `DevCycleCraft/sdd/`. |
| 2026-05 | **workflow.md Reduction** | ✅ Done | Goal: compress workflow rules into agent role files. Completed (61 findings resolved). Pointer: `DevCycleCraft/workflow-compression/`. |
| 2026-05 | **CLAUDE.md Deep Restructure** | ✅ Done | Goal: move coding rules to `.claude/library/` and gate Docs/ context scope. Completed. Pointer: `DevCycleCraft/docs-context-scope-control/`. |
| 2026-05 | Architecture Tests Evaluation | ✅ Done | Goal: decide on NetArchTest/ArchUnitNET adoption. Evaluated — no changes produced. Pointer: `DevCycleCraft/architecture-tests-evaluation/`. |
| 2026-05 | Claude Managed Agents Evaluation | ✅ Done | Goal: evaluate Anthropic hosted agents for the dev cycle. Discarded — duplicates existing workflow. Pointer: `DevCycleCraft/claude-managed-agents-evaluation/`. |
| 2026-05 | Day-to-day task management workflow review | ✅ Done | Goal: make the backlog file the first-class SCRUM board. Completed (workflow rules updated). Pointer: `DevCycleCraft/backlog-workflow-integration/`. |
| 2026-05 | App Versioning Strategy | ✅ Done | Goal: git-tag-driven semver. Shipped (MinVer + release command). Pointer: `DevCycleCraft/app-versioning/`. |
<!-- BACKLOG:GENERATED:END archive-craft -->
