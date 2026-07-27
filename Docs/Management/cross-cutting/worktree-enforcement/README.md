---
id: worktree-enforcement
title: "**Enforce Git Worktrees for Parallel Subagents**"
status: "✅ Done"
target: 2026-06
section: DevCycleCraft
kind: feature
closed: 2026-06
order: 10
goal: "mandatory rule for git worktree isolation across parallel subagents."
pointer: cross-cutting/worktree-enforcement/
---

# Enforce Git Worktrees for Parallel Subagents

Mandatory rule encoded in `orchestrator.md § Git Worktrees as Isolation Primitive` — threshold
lowered from 3+ to 2+ subagents, made hard gate, staging-collision rationale added. `.worktrees/`
confirmed gitignored.

> Migrated from `Docs/Management/cross-cutting-log.md` (T12a Wave K, F-1a log-pointer batch 1).
> Slug and folder shape are agent-authored. This is a DISTINCT item from the existing, unrelated
> `cross-cutting/mandatory-worktree-rule-enforcement/` folder (a live Pending item scoping "ALL
> Subagent Work") — confirmed no collision, not merged, per the plan's explicit warning. Goal text
> is reworded from the archived Notes cell (verbatim text tripped the file-path-beyond-pointer
> banned pattern via `workflow.md`); meaning preserved.
