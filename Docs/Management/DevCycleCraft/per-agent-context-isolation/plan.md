# Per-Agent Context Isolation (MVP) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reduce subagent cold-start context via agent-brief frontmatter only (`disallowedTools:`, `skills:` removal), verified by one throwaway probe.

**Architecture:** Config-only. Five `.claude/agents/*.md` frontmatter edits, zero body changes, zero source code. Verification = Agent-tool usage metadata from one 0-tool implementor probe + one live verifier dispatch. Spec: `requirements.md` (REQ-CTXISO-01..07), `design.md` (change table + risk assessment), baseline in `context-baseline.md`.

**Tech Stack:** Claude Code agent frontmatter (documented keys: `tools`, `disallowedTools`, `skills` — code.claude.com/docs/en/sub-agents.md), git.

## Global Constraints

- Frontmatter edits ONLY — agent-brief markdown bodies must not change (verify with `git diff`).
- English only; no source/`.cs`/`.xaml` files touched anywhere in this plan.
- Token thrift: exactly ONE post-change probe (Task 2); no re-probes, no pre-probes.
- Rollback rule (design.md § Error handling): any dispatch-time failure → revert that agent's frontmatter line(s), record in `task-log.md`; never work around with body edits.
- `.sln` HARD GATE: every new `Docs/` file lands in `MyVocaList.sln` solution folder `per-agent-context-isolation` (GUID `{FA1234BC-0001-4000-8000-000000000044}`) in the same commit. `.claude/agents/*` files are NOT `.sln`-registered.
- Commit after every task (`/sln-commit` discipline); pre-commit hook skips docs/config-only commits automatically.

---

### Task 1: Frontmatter pass over the 5 agent briefs

**Files:**
- Modify: `.claude/agents/implementor.md` (frontmatter lines 1–6 only)
- Modify: `.claude/agents/orchestrator.md` (frontmatter only)
- Modify: `.claude/agents/spec-reviewer.md` (frontmatter only)
- Modify: `.claude/agents/plan-reviewer.md` (frontmatter only)
- Modify: `.claude/agents/verifier.md` (frontmatter only)

**Interfaces:**
- Consumes: design.md § Changes table (authoritative change list).
- Produces: reduced-frontmatter agent briefs; Tasks 2–3 dispatch against these.

- [ ] **Step 1: Edit `implementor.md` frontmatter — add denylist, keep skills preload**

Current frontmatter begins:
```yaml
---
name: implementor
description: MyVocaList implementation subagent. Use to execute a scoped, briefed implementation task (specific files + tasks from the orchestrator's briefing) — codes, tests, and commits within scope; never makes architectural decisions or edits rules files.
skills:
  - myvocalist-coding
```
Insert one line immediately after the `description:` line (before `skills:`):
```yaml
disallowedTools: Agent, Artifact, NotebookEdit, PowerShell
```

- [ ] **Step 2: Edit `orchestrator.md` frontmatter — add denylist, remove skills preload**

Current frontmatter begins:
```yaml
---
name: orchestrator
description: MyVocaList multi-wave feature coordinator. Use when coordinating subagent waves for a feature — plans, dispatches, verifies wave output, manages session state. Never writes code and never reads source files (delegates all code inspection to Explore/Plan subagents).
skills:
  - myvocalist-coding
```
Replace the two `skills:` lines with the single line:
```yaml
disallowedTools: Artifact, NotebookEdit
```

- [ ] **Step 3: Edit the 3 reviewer briefs — remove skills preload, leave `tools:` untouched**

In each of `spec-reviewer.md`, `plan-reviewer.md`, `verifier.md`, delete exactly these two lines from the frontmatter (nothing else):
```yaml
skills:
  - myvocalist-coding
```
Resulting frontmatter keys: spec-reviewer/plan-reviewer = `name`, `description`, `tools: Read, Grep, Glob`; verifier = `name`, `description`, `tools: Read, Grep, Glob, Bash`.

- [ ] **Step 4: Verify frontmatter-only diff**

Run: `git diff --stat .claude/agents/` — expected: exactly 5 files, ~1–2 line delta each.
Run: `git diff .claude/agents/ | grep '^[+-]' | grep -v '^[+-][+-]' | grep -vE 'disallowedTools|skills:|myvocalist-coding'` — expected: empty output (no body lines touched). Non-empty output = a body edit slipped in → revert it.

- [ ] **Step 5: Commit**

```bash
git add .claude/agents/implementor.md .claude/agents/orchestrator.md .claude/agents/spec-reviewer.md .claude/agents/plan-reviewer.md .claude/agents/verifier.md
git commit -m "feat: per-agent context isolation — frontmatter pass (REQ-CTXISO-01..04)"
git push
```
Satisfies REQ-CTXISO-01 (edit half), 02, 03, 04 (risk table already in design.md).

---

### Task 2: Post-change implementor probe + baseline update

**Files:**
- Modify: `Docs/Management/DevCycleCraft/per-agent-context-isolation/context-baseline.md` (append `## Post-change` section)

**Interfaces:**
- Consumes: Task 1 committed; baseline comparator 38,127 (general-purpose, `context-baseline.md`).
- Produces: measured implementor cold-start number → REQ-CTXISO-01 evidence, quoted by Task 4's task-log entry.

- [ ] **Step 1: Dispatch ONE 0-tool probe of the `implementor` agent type**

Agent tool, `subagent_type: implementor`, prompt (verbatim — same as baseline probes):
```
You are a context-measurement probe. Make ZERO tool calls — do not Read, Grep, Glob, Bash, or use any other tool. Your only job is to introspect the context that was injected into you at startup and report on it. Respond with a single structured report:
1. Rules files: which of workflow.md, testing.md, code-principles.md, constraints-registry.md, bug-tracking.md, component-change-governance.md appear, routing-table vs full form, rough line estimates.
2. CLAUDE.md layers present (project / global / RTK.md) + line estimates.
3. Memory content injected? 4. Skills: is a skills-listing block present, and is the myvocalist-coding skill body preloaded? Estimate sizes.
5. MCP schemas/instructions visible (deferred names vs full)? 6. Tools available (names only) — explicitly state whether Agent, Artifact, NotebookEdit, PowerShell are present.
7. Rough percentage per category. Quote a short verbatim fragment from each rules file you claim is present. Do not perform any task — you are only measuring.
```

- [ ] **Step 2: Record the token total from the Agent-tool usage metadata**

Expected: `subagent_tokens` ≤ 35,127 (pass line), ~30–33k anticipated. Also confirm from the probe's report: Agent/Artifact/NotebookEdit/PowerShell absent from its tool list; myvocalist-coding body preloaded; skills-listing block presence noted (decides the design.md denylist-vs-listing-block uncertainty).

- [ ] **Step 3: Append results to `context-baseline.md`**

Add section:
```markdown
## Post-change (Task 2, YYYY-MM-DD)

| Probe | Cold-start tokens | vs 38,127 comparator | Pass (≤35,127)? |
|-------|-------------------|----------------------|-----------------|
| `implementor` (denylist + preload) | <measured> | −<delta> | YES/NO |

Denied tools confirmed absent: <list>. Skills-listing block: <present/absent — resolves design.md § Changes row-1 uncertainty>. myvocalist-coding preload: <confirmed>.
```
If > 35,127: do NOT iterate with more probes — record the number, mark REQ-CTXISO-01 FAILED, apply the rollback rule only if a tool-availability defect (not a token miss) caused it, and escalate the token miss to Helder in the task-log (`blocked: spec gap` is wrong here — use `To Review` with the failure stated).

- [ ] **Step 4: Commit**

```bash
git add Docs/Management/DevCycleCraft/per-agent-context-isolation/context-baseline.md
git commit -m "docs: per-agent context isolation — post-change probe result (REQ-CTXISO-01)"
git push
```

---

### Task 3: Live reviewer validation (REQ-CTXISO-06)

**Files:**
- Create: `Docs/Management/DevCycleCraft/per-agent-context-isolation/task-log.md` (first entry)

**Interfaces:**
- Consumes: Task 1's commit hash (the diff under verification).
- Produces: verifier verdict text → quoted in task-log; proves report-only agents work without the `skills:` preload.

- [ ] **Step 1: Dispatch the `verifier` agent on Task 1's commit**

Agent tool, `subagent_type: verifier`, prompt:
```
Verify the commit "feat: per-agent context isolation — frontmatter pass" (git show <TASK1-HASH>) against its spec at Docs/Management/DevCycleCraft/per-agent-context-isolation/ (requirements.md REQ-CTXISO-01..04, design.md § Changes). Check: (1) diff touches ONLY frontmatter of the 5 .claude/agents/*.md files; (2) each change matches the design table exactly (implementor denylist Agent/Artifact/NotebookEdit/PowerShell + skills kept; orchestrator denylist Artifact/NotebookEdit + skills removed; 3 reviewers skills removed, tools untouched); (3) REQ-CTXISO-04: read each brief's BODY line-by-line and confirm it references no tool its frontmatter now denies (implementor body must not require Agent/Artifact/NotebookEdit/PowerShell; orchestrator body must not require Artifact/NotebookEdit); (4) no non-English text. Produce a structured pass/fail verdict per requirement.
```

- [ ] **Step 2: Confirm the dispatch itself succeeded**

The validation is twofold: (a) the verifier RAN successfully under its reduced frontmatter (REQ-CTXISO-06 — a dispatch/tool failure here triggers the rollback rule for verifier.md), and (b) its verdict on Task 1 is PASS. Record both. If (a) fails: revert verifier.md's frontmatter, record REQ-CTXISO-06 as FAILED in the task-log, then RE-dispatch the verifier (now on original frontmatter) so the Task-1 verdict evidence is still obtained — the two outcomes are independent.

- [ ] **Step 3: Create `task-log.md` with entries for Tasks 1–3**

Follow workflow Rule 5 format. Task 1 entry MUST include `### Changed files` (the 5 agent briefs) and the Step-4 diff-check evidence; Task 2 entry cites the probe number; Task 3 entry quotes the verifier verdict + the REQ-CTXISO-06 dispatch-success observation. AC traceability matrix: REQ-CTXISO-01→Task 2 probe, 02/03/04→verifier verdict, 05→design.md citations, 06→this dispatch, 07→Task 4.

- [ ] **Step 4: Commit (includes `.sln` registration of task-log.md)**

Add to `MyVocaList.sln` inside the `per-agent-context-isolation` `ProjectSection(SolutionItems)` (folder GUID `{FA1234BC-0001-4000-8000-000000000044}`):
```
Docs\Management\DevCycleCraft\per-agent-context-isolation\task-log.md = Docs\Management\DevCycleCraft\per-agent-context-isolation\task-log.md
```
```bash
git add Docs/Management/DevCycleCraft/per-agent-context-isolation/task-log.md MyVocaList.sln
git commit -m "docs: per-agent context isolation — verifier validation + task log (REQ-CTXISO-06)"
git push
```

---

### Task 4: Close-out — BACKLOG + tasks.md + spec ritual

**Files:**
- Modify: `Docs/Management/BACKLOG.md` (row 174 status + close-out note)
- Modify: `Docs/Management/DevCycleCraft/per-agent-context-isolation/tasks.md` (check off Tasks 1–4)
- Modify: `Docs/Management/DevCycleCraft/per-agent-context-isolation/task-log.md` (Task 4 entry)
- Modify: `Docs/Changelog/changelog.md` (implementation-complete entry)

**Interfaces:**
- Consumes: Tasks 1–3 outcomes (probe number, verifier verdict).
- Produces: closed BACKLOG row; feature ready to unblock BUG-027.

- [ ] **Step 1: Update BACKLOG row 174**

Status `📋 Spec` → `✅ Done` (or `🟡 In Progress` + explicit ⏳ Helder gate if the probe FAILED the ≤35,127 line). Append to the row description: research (a)–(d) answered (cite `design.md § Research findings`), worktree-overlay candidate obsolete, measured outcome `<probe number>` vs 38,127 baseline, path-scoped-rules follow-up noted in requirements out-of-scope. Confirm line-195 rules-file-refactoring row needs no edit (it cross-references this feature as "the bigger lever" — historical statement, leave as-is).

- [ ] **Step 2: Spec ritual + tasks.md checkboxes**

Check off all 4 tasks in `tasks.md`. Session-End Spec Update Ritual: re-read requirements.md/design.md against what was actually done; if the probe produced a surprise (e.g. listing-block behavior), add a dated `> **Spec updated [YYYY-MM-DD]:**` note to design.md rather than rewriting.

- [ ] **Step 3: Changelog entry**

Append to `Docs/Changelog/changelog.md` under july 2026: `- **MM/dd/2026** - feat - **Per-Agent Context Isolation MVP shipped** — <one-line outcome with measured number>`.

- [ ] **Step 4: Commit and push**

```bash
git add Docs/Management/BACKLOG.md Docs/Management/DevCycleCraft/per-agent-context-isolation/tasks.md Docs/Management/DevCycleCraft/per-agent-context-isolation/task-log.md Docs/Changelog/changelog.md
git commit -m "docs: per-agent context isolation — close-out, BACKLOG row 174 (REQ-CTXISO-07)"
git push
```
Then report to Helder: measured saving, any ⏳ gates, and that BUG-027 is unblocked as the next item.
