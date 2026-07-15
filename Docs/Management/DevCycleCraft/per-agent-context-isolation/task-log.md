# Per-Agent Context Isolation — Task Log

## Task 1 — Frontmatter pass over the 5 agent briefs (2026-07-09)

**Status:** Done (verifier PASS — see Task 3 entry)
**Executed by:** fresh implementor subagent (subagent-driven, per plan)
**Commit:** `eb9edd4` — `feat: per-agent context isolation — frontmatter pass (REQ-CTXISO-01..04)` (pushed to `develop`)

### Changed files
- `.claude/agents/implementor.md` — +`disallowedTools: Agent, Artifact, NotebookEdit, PowerShell`; `skills:` kept
- `.claude/agents/orchestrator.md` — `skills:` block → `disallowedTools: Artifact, NotebookEdit`
- `.claude/agents/spec-reviewer.md` — `skills:` block removed; `tools: Read, Grep, Glob` untouched
- `.claude/agents/plan-reviewer.md` — `skills:` block removed; `tools: Read, Grep, Glob` untouched
- `.claude/agents/verifier.md` — `skills:` block removed; `tools: Read, Grep, Glob, Bash` untouched

### Verification evidence
- `git diff --stat .claude/agents/` → exactly 5 files, 2 insertions / 8 deletions total
- Body-line filter (plan Task 1 Step 4) → empty output (frontmatter-only confirmed)
- BOM check: all five files begin `2d 2d 2d` (`---`) — UTF-8 without BOM preserved (guards the 07/08 frontmatter-parsing regression)

## Task 2 — Post-change implementor probe (2026-07-09)

**Status:** Done — **REQ-CTXISO-01 FAILED on the formal threshold; lever effect confirmed.** Escalated per plan Task 2 Step 3 failure path; **Helder approved the measured outcome 2026-07-10** (threshold miss = comparator mismatch, not a lever defect — REQ closed).
**Commit:** `7263670` — `docs: per-agent context isolation — post-change probe result (REQ-CTXISO-01)`

### Changed files
- `context-baseline.md` — `## Post-change` section appended

### Verification evidence
- Single 0-tool implementor probe: **37,370 tokens** vs pass line ≤35,127 (comparator 38,127 general-purpose) → formal FAIL by 2,243.
- Denied tools confirmed ABSENT (Agent, Artifact, NotebookEdit, PowerShell) → frontmatter applies to dispatches live, no session restart needed.
- Analysis (no re-probe, token thrift): comparator carries no agent role prompt; implementor's ~250-line body ≈ ~3k + 0.7k preload sit on top → like-for-like pre-change implementor ≈ 41–42k → **actual saving ≈ 4–5k/agent, within the design's 3–6k estimate**. The miss is the comparator mismatch the spec's own REQ-CTXISO-01 caveat flagged.
- Design uncertainty resolved: `disallowedTools:` denylist does NOT suppress the skills-listing block (~2–2.5k remains).
- No rollback: all tool denials behaved; no availability defect.

## Task 3 — Live reviewer validation (2026-07-09/10)

**Status:** Done — **REQ-CTXISO-06 PASS**
**Commit:** (this file's commit)

### Changed files
- `task-log.md` (this file, created)
- `MyVocaList.sln` (task-log.md registered, folder GUID `{FA1234BC-0001-4000-8000-000000000044}`)

### Verification evidence
- Verifier dispatched on `eb9edd4` under its REDUCED frontmatter (no `skills:` preload): dispatch succeeded, full checklist executed → **REQ-CTXISO-06 validated live**.
- Verifier verdict on Task 1: **PASS** on all four checks — (1) frontmatter-only diff (`--unified=10` inspected), (2) exact design-table match per file, (3) REQ-CTXISO-04 body scan: no brief references a tool its frontmatter denies (orchestrator "Artifact" matches are English-word usage, not the tool), (4) English-only.
- Verifier warnings (accepted): PowerShell watch item stands (future `.ps1` briefings → `pwsh -File` under Bash or revert); incidental pre-existing stale reference in implementor body line ~284 (`superpowers:test-driven-development` + `dotnet-skills`, both deliberately disabled) — registered in Task 4 close-out as a follow-up note, not fixed here (body edits out of scope).
- Earlier live data point: plan-reviewer also dispatched successfully post-spec with restricted `tools:` (plan-phase, 2026-07-09).

### AC traceability matrix

| AC ID | Criterion | Implementation location | Verification |
|-------|-----------|-------------------------|--------------|
| REQ-CTXISO-01 | implementor denylist + probe ≤35,127 | implementor.md frontmatter (`eb9edd4`) | Probe 37,370 — **FAILED formal line; ~4–5k like-for-like saving confirmed** (`context-baseline.md § Post-change`) — ⏳ Helder disposition |
| REQ-CTXISO-02 | orchestrator denylist + skills removed | orchestrator.md frontmatter (`eb9edd4`) | Verifier PASS check (2) |
| REQ-CTXISO-03 | 3 reviewers: skills removed, tools unchanged | 3 reviewer briefs (`eb9edd4`) | Verifier PASS check (2) |
| REQ-CTXISO-04 | no brief body references a denied tool | design.md risk table | Verifier PASS check (3), line-by-line body read |
| REQ-CTXISO-05 | non-levers documented with citations | design.md § Research findings | Spec-reviewer PASS (2026-07-09) |
| REQ-CTXISO-06 | post-change report-only dispatch succeeds | verifier dispatch this task | PASS — dispatch + full checklist under reduced frontmatter |
| REQ-CTXISO-07 | BACKLOG row 174 close-out | Task 4 | Task 4 entry below |

## Task 4 — Close-out (2026-07-10)

**Status:** Done
**Commit:** (close-out commit)

### Changed files
- `Docs/Management/BACKLOG.md` — row 174 status + outcome note
- `tasks.md` — Tasks 1–4 checked off
- `Docs/Changelog/changelog.md` — feat entry
- this file — Task 4 entry

### Verification evidence
- BACKLOG row 174 records: research (a)–(d) answered, worktree-overlay candidate obsolete, measured outcome 37,370 vs 38,127 comparator (~4–5k like-for-like), ⏳ Helder gate on REQ-CTXISO-01 formal-threshold disposition.
- Line-195 rules-file-refactoring row confirmed cross-reference only ("the bigger lever" historical statement) — no edit needed.
- All 6 folder files `.sln`-registered (requirements, design, tasks, plan, context-baseline, task-log) under GUID 0044.
- Follow-ups registered in BACKLOG row note: (a) path-scoped rules evaluation (requirements out-of-scope), (b) implementor role-body slimming (~3k candidate), (c) stale implementor-body skill references (verifier incidental).


## Moved from BACKLOG.md (2026-07-15) — Per-Agent MCP/Skill Context Isolation

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-06-27 | **Per-Agent MCP/Skill Context Isolation** | ✅ Done — Helder accepted measured outcome 2026-07-10 | **Escalated during workflow.md authorship review (Rule 2):** fresh subagents must load only the tools each one needs — the huge, fast, needless token consumption at every cold start is crucial to stop; tackle next. **Scope update (2026-07-09):** research item (d) is ANSWERED — MCP tool *schemas* are now deferred by Claude Code (loaded on demand; see `mcp-governance.md § MCP Token Budgeting`), so MCP overlays matter less; the main per-agent levers are memory-file/rules inheritance, skill preloads, and agent-brief `tools:`/`skills:` frontmatter scoping (Task 15 groundwork already restricts reviewer agents to Read/Grep/Glob). Original statement: each subagent should activate only the MCPs and Skills relevant to its task, and deactivate them on completion — preventing irrelevant tool definitions from consuming context budget. Challenge: Claude Code currently loads MCPs globally (session-wide), so one agent enabling a server pollutes the context of all parallel agents in the same session. **Research required:** (a) whether Claude Code supports per-agent MCP scoping today or via a planned API; (b) whether git worktrees give each agent a separate `settings.json` / MCP config — enabling isolation by running agents in separate worktrees with task-scoped `.mcp.json` overlays; (c) viability of a pre-task "load MCP X" + post-task "unload MCP X" hook pattern without tool-schema reload overhead; (d) cost model: does enabling an unused MCP server cost tokens only at session start (schema load) or on every tool call? **Candidate design:** worktree-per-agent with a task-scoped `.mcp.json` overlay checked into the worktree's `.claude/` folder, merged from a project-level whitelist. The orchestrator writes the overlay before dispatching; the agent runs with only its declared servers active. **MVP SHIPPED 2026-07-10** (spec/plan/impl in `DevCycleCraft/per-agent-context-isolation/`): research (a)–(d) all answered — no per-agent CLAUDE.md/rules scoping mechanism exists (platform floor ~13–16k/agent; only built-in Explore/Plan exempt), MCP schemas deferred platform-side, worktree-`.mcp.json` overlay candidate OBSOLETE, load/unload hooks meaningless. Delivered: frontmatter pass on all 5 agent briefs (`eb9edd4`, verifier PASS) — implementor/orchestrator denylists + `skills:` preload removed from orchestrator/reviewers. Measured: implementor probe 37,370 vs 38,127 general-purpose comparator; like-for-like saving ~4–5k/agent (~16–20k per 4-wave); **formal ≤35,127 line FAILED (comparator lacked the ~3k implementor role body) — Helder GATE APPROVED 2026-07-10: measured outcome accepted as done (REQ-CTXISO-01 closed on the like-for-like evidence; threshold miss attributed to comparator mismatch, not the lever).** Follow-up candidates recorded in task-log Task 4: path-scoped rules (`paths:` frontmatter) evaluation; implementor role-body slimming (~3k); stale implementor-body skill refs (verifier incidental). Baseline + numbers: `context-baseline.md`. |


## Moved from BACKLOG.md (2026-07-15) — Worktree-scoped context slimming — investigation

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-10 | ↳ **Worktree-scoped context slimming — investigation** | 💡 Pending | Registered by Helder 2026-07-10, motivated by the parent row not reaching the estimated savings with tools/context filtering. Idea: given worktree usage becomes mandatory for a defined set of subagent types (always branched from the most-updated `develop`), could each worktree carry **custom content** holding only what that subagent will really need — fewer (sometimes drastically fewer) guideline/rules files to be loaded — thereby avoiding loading unneeded context at cold start? Investigate: (a) does Claude Code resolve CLAUDE.md / `.claude/rules` / agents / skills from the worktree root, making per-worktree pruned copies effective?; (b) mechanism to prune without dirtying the branch (overlay, sparse-checkout, ignore tricks) so the pruned files are never committed; (c) orchestration cost — who writes the pruned overlay before dispatch; (d) interaction with the parent row's remaining follow-up candidates (path-scoped `paths:` rules, implementor role-body slimming ~3k). Append findings before any rule change. |
