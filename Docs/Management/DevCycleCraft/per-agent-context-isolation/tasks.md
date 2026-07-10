# Per-Agent Context Isolation — Tasks (MVP)

> Sequencing: 1 → 2 → 3 → 4. No `[P]` waves — single-writer on `.claude/agents/*` and each step consumes the previous one's output.

- [x] **Task 1 — Frontmatter pass over the 5 agent briefs** *(done 2026-07-09, `eb9edd4`, verifier PASS)*
  - Produces: edited frontmatter in `implementor.md`, `orchestrator.md`, `spec-reviewer.md`, `plan-reviewer.md`, `verifier.md` per `design.md § Changes`
  - Consumes: design.md change table
  - Files owned: `.claude/agents/*.md` (frontmatter only — bodies untouched)
  - Risk: unsupported frontmatter key on installed CC version → 1-line revert per design.md rollback
  - Demo: `git diff` shows only frontmatter lines changed; ACs REQ-CTXISO-01..04 satisfied on paper
  - Note: `.claude/agents/*` files are NOT `.sln`-registered (constraints-registry.md — gate applies to `Docs/` only)

- [x] **Task 2 — Post-change implementor probe + baseline update** *(done 2026-07-09, `7263670` — probe 37,370: formal line FAILED, ~4–5k like-for-like saving confirmed; ⏳ Helder disposition)*
  - Produces: `context-baseline.md § Post-change` section with measured implementor cold-start
  - Consumes: Task 1 committed
  - Risk: probe costs ~30k throwaway tokens (accepted, single probe per design.md § Verification)
  - Demo: recorded number ≤35,127 (≥3k under the 38,127 baseline) → REQ-CTXISO-01 evidence

- [x] **Task 3 — Live reviewer validation (REQ-CTXISO-06)** *(done 2026-07-10, `2fb64f3` — verifier dispatch PASS under reduced frontmatter)*
  - Produces: successful `verifier` dispatch (verifying Task 1's commit) under the reduced frontmatter, noted in task-log.md
  - Consumes: Task 1 committed
  - Demo: verifier verdict returned without tool/context failures

- [x] **Task 4 — Close-out: BACKLOG + task-log** *(done 2026-07-10)*
  - Produces: BACKLOG row 174 updated (research (a)–(d) answered, worktree-overlay candidate marked obsolete, status → ✅ or ⏳ Helder gate); task-log.md entry with Changed files + verification evidence; `.sln` registration for ALL new Docs files in this folder (requirements, design, tasks, context-baseline, plan registered at spec/plan phase; task-log registered by Task 3 — verify all 6 present)
  - Consumes: Tasks 1–3
  - Demo: BACKLOG row reflects measured outcome; commit clean
  - Note: BACKLOG has two related rows — close row 174 (line 174); confirm the rules-file-refactoring row (line 195) only cross-references this feature and needs no close-out of its own
