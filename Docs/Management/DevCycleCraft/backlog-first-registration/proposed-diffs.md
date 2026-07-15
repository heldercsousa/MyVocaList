# Proposed Diffs — BACKLOG-first Registration Enforcement (Phase 2)

> **These are PROPOSED diffs. Helder must read and EDIT (not rubber-stamp — Authorship
> gate per CLAUDE.md § Continuous Enhancement → Authorship) before applying.**
> `workflow.md`, `CLAUDE.md`, and `Docs/Changelog/changelog.md` were **NOT touched by the
> agent** — they are deny-listed. This file contains the exact, hand-applyable diffs plus the
> `amend:`/changelog triple for Helder to apply himself.
>
> Language rule: English only (CLAUDE.md § Constitutional Constraints).
> Posture: **A (advisory / non-blocking)** — ratified by Helder 2026-06-23.

---

## Scope of this file

| # | Target (deny-listed) | Change |
|---|----------------------|--------|
| Diff 1 | `.claude/rules/workflow.md` Rule 1 — Proactive BACKLOG triage | Strengthen obligation: memory is NOT a registration surface; memory-only = UNREGISTERED |
| Diff 2 | `.claude/rules/workflow.md` Rule 2 — Subagent exit checklist | Add one line: confirm new work item is in BACKLOG.md (not only memory) |
| Diff 3 | `.claude/rules/workflow.md` Hook Enforcement Notes table | Add row for the new `orphan_check.py` Stop hook (advisory / non-blocking) |
| — | Exempt categories | The 4 categories the rule text must agree with the classifier on |
| — | `amend:`/changelog triple | Exact commit subject+body + changelog line for Helder |

---

## Diff 1 — `workflow.md` Rule 1 (Proactive BACKLOG triage — Untracked work)

**Current text (lines ~116–138, for context):**

```markdown
### Proactive BACKLOG triage — Untracked work

**Any work identified during a session that is not already in BACKLOG.md must get a brief entry before proceeding.**

This applies to:
- A new DevCycleCraft activity (tooling change, process rule, infrastructure work)
- A business feature idea mentioned in conversation (even informally)
- A significant constraint, investigation, or one-off fix that took material effort

**Format — add a row to the appropriate BACKLOG.md table:**

| Date | Activity/Feature | `💡 Pending` | One-line description |

- Use `💡 Pending` for ideas that arrived but aren't being acted on immediately
- Use `🟡 In Progress` if work is starting now
- Keep descriptions to one sentence — BACKLOG is a dashboard, not a spec
```

**Proposed change** — insert a new bolded paragraph immediately AFTER the existing opening
sentence (`…must get a brief entry before proceeding.`) and BEFORE `This applies to:`:

```markdown
### Proactive BACKLOG triage — Untracked work

**Any work identified during a session that is not already in BACKLOG.md must get a brief entry before proceeding.**

**`BACKLOG.md` is the ONLY registration surface. Device auto-memory is NOT a registration**
**surface.** A work item recorded only in device auto-memory
(`~/.claude/projects/<project>/memory/`) — and not also given a `BACKLOG.md` row in the same
session — is an **UNREGISTERED** (orphan) item: it is per-device, not git-tracked, and invisible
to the team. Memory is never the sole home for a work item. A **work item** is a new business
feature, a new Dev Cycle Craft activity, a bug, a deferred follow-up, or a material one-off
investigation — anything that must get a BACKLOG row (nested per `bug-tracking.md` when it has a
parent feature). The 4 memory writes that are NOT work items (and must never be registered) are
listed under **Exempt memory writes** below.

This applies to:
- A new DevCycleCraft activity (tooling change, process rule, infrastructure work)
- A business feature idea mentioned in conversation (even informally)
- A significant constraint, investigation, or one-off fix that took material effort
```

**And** append the following new subsection immediately AFTER the existing
`If the answer is "no" to the first, or "yes" to the others → add the entry, then proceed.`
line (line ~138), before `### Spec quality gate`:

```markdown
**Exempt memory writes (NOT work items — never need a BACKLOG row):**
1. `feedback_*` learnings — guidance on how the agent should work.
2. `project_*` continuation pointers — "NEXT:" / resume notes for an **already-tracked** item.
3. Reference-fact caches — email, current date, architecture snapshots.
4. Harness-**automatic** captures the agent did not author.

Exemption is line/content-level, never a blanket file exemption: a new-work line inside an
otherwise auto-captured `MEMORY.md` is still a work item. When an exempt marker and a new-work
verb co-occur on one line (e.g. "NEXT: implement <new thing>"), the presence of a *new*
work-item noun wins → it is a work item, not exempt.
```

> Helder note: edit the prose to taste — the load-bearing content is (a) "memory is NOT a
> registration surface", (b) "memory-only = UNREGISTERED orphan", (c) the work-item definition,
> (d) the 4 exempt categories matching the classifier.

---

## Diff 2 — `workflow.md` Rule 2 (Subagent exit checklist)

**Current text (lines ~323–339, for context):**

```markdown
### Subagent exit checklist (mandatory before returning)

Every subagent must complete ALL of these steps in order before stopping:

1. **Invoke `superpowers:verification-before-completion`** — …
2. **Build:** Run `dotnet build` …
3. **Test:** If any `.cs` implementation file was changed, run `dotnet test` …
4. **Post-edit re-read:** Re-read the affected section of every edited file …
5. **.sln registration — BLOCKING:** For every file created, moved, or deleted in `Docs/` or `.claude/`: update `MyVocaList.sln` now (same commit). …
6. **Living spec check:** Review decisions made during implementation …
7. **Task-log:** Complete the task-log entry …
8. **Commit:** Commit all changed files including any spec updates.
9. **Push:** `git push origin HEAD`

**The `Stop` hook warns if uncommitted changes remain. Treat it as a hard gate.**

A subagent that stops without completing all 8 steps has not finished the task.
```

**Proposed change** — insert a new step between current step 6 (Living spec check) and current
step 7 (Task-log), and renumber accordingly. The minimal edit (single inserted line) reads:

```markdown
6. **Living spec check:** Review decisions made during implementation — write back any undocumented decisions to the spec.
7. **BACKLOG registration check:** Confirm any new work item discovered or started this session has a `BACKLOG.md` row (NOT only a device-memory note). A work item living only in auto-memory is unregistered — add the BACKLOG row now, in this same commit.
8. **Task-log:** Complete the task-log entry including Changed files, Verification evidence, and AC traceability matrix (if applicable).
9. **Commit:** Commit all changed files including any spec updates.
10. **Push:** `git push origin HEAD`
```

> Helder note: this shifts Task-log/Commit/Push from 7/8/9 to 8/9/10. Update the trailing
> sentence `A subagent that stops without completing all 8 steps has not finished the task.`
> to `…all 9 steps…` (the original count of 8 already trailed the 9-item list — pick the
> correct final count when you apply).

---

## Diff 3 — `workflow.md` Hook Enforcement Notes table

**Current text (lines ~13–18, for context):**

```markdown
| Hook | Trigger | Rule enforced |
|------|---------|---------------|
| `Stop` hook | Session ends with uncommitted changes | Rule 3 — Commit After Every Task; also triggers Verifier dispatch reminder |
| `PostCompact` hook | Context compaction event | Session resume — re-read spec reminder |
| `PostToolUse` hook (Services files) | Edit to a Services/*.cs file | testing.md — TDD reminder for service changes |
| `SessionStart` hook | New session begins | Hook health verification |
```

**Proposed change** — add one row for the new advisory hook:

```markdown
| Hook | Trigger | Rule enforced |
|------|---------|---------------|
| `Stop` hook | Session ends with uncommitted changes | Rule 3 — Commit After Every Task; also triggers Verifier dispatch reminder |
| `Stop` hook (`orphan_check.py`) | Session ends with a probable memory-only work item AND BACKLOG.md not changed this session | Rule 1 — Proactive BACKLOG triage. **Advisory / non-blocking** — prints a reminder only; fails open (any error → silent exit 0); never blocks session end |
| `PostCompact` hook | Context compaction event | Session resume — re-read spec reminder |
| `PostToolUse` hook (Services files) | Edit to a Services/*.cs file | testing.md — TDD reminder for service changes |
| `SessionStart` hook | New session begins | Hook health verification |
```

---

## The 4 exempt categories (locked working definition — rule text and classifier MUST agree)

From `requirements.md § 4`:

1. `feedback_*` learnings — guidance on how the agent should work.
2. `project_*` continuation pointers — "NEXT:" / resume notes for an **already-tracked** item.
3. Reference-fact caches — email, current date, architecture snapshots.
4. Harness-**automatic** captures the agent did not author.

**Classification is line/content-level, never a blanket file exemption.** Category 4 is applied at
the line level: a new-work line inside `MEMORY.md` is a candidate even though `MEMORY.md` is
otherwise auto-captured. **Precedence rule:** when an exempt marker and a new-work verb co-occur on
the same line (e.g. a `project_*` "NEXT: implement <new thing>"), the presence of a *new* work-item
noun wins → **candidate** (work item); a pure continuation of an already-tracked item → **exempt**.

---

## `amend:` + changelog triple (Helder applies)

### Commit (use after applying Diffs 1–3 to `workflow.md`)

Subject:
```
amend: workflow.md — memory is not a registration surface; orphan-check advisory
```

Body:
```
What is wrong with the current rule:
Rule 1 ("Proactive BACKLOG triage") states the obligation to register untracked work in
BACKLOG.md, but never says that device auto-memory is NOT a registration surface. Agents
recorded new work items only in per-device auto-memory — team-invisible, not git-tracked —
and nothing in the rule text or the exit checklist flagged it as unregistered.

Backward compatibility:
No existing code changes. Rule-text and hook-table strengthening only. New advisory Stop
hook (orphan_check.py) is fail-open and non-blocking — it cannot break any existing session.

Changes:
- Rule 1: device auto-memory declared NOT a registration surface; memory-only work item =
  UNREGISTERED orphan; work-item definition + 4 exempt categories made explicit.
- Rule 2 subagent exit checklist: added "BACKLOG registration check" step.
- Hook Enforcement Notes table: added the orphan_check.py advisory Stop-hook row.

Authorship: human-reviewed and edited by Helder (CLAUDE.md § Continuous Enhancement → Authorship).

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
```

### Changelog entry (add under `## Entries for june 2026` in `Docs/Changelog/changelog.md`)

```
- **06/24/2026** - amend - workflow.md — memory is not a registration surface; orphan-check advisory. **Old:** Rule 1 ("Proactive BACKLOG triage") stated the obligation to register untracked work in BACKLOG.md but never declared device auto-memory a non-registration surface, and the Rule 2 subagent exit checklist had no BACKLOG-registration step — so agents recorded new work items only in per-device auto-memory (team-invisible, not git-tracked) and nothing flagged the orphan. **New:** Rule 1 declares device auto-memory NOT a registration surface and a memory-only work item an UNREGISTERED orphan, with the explicit work-item definition + 4 exempt categories (feedback_*, project_* continuation, reference caches, harness-automatic); Rule 2 exit checklist gains a "BACKLOG registration check" step; the Hook Enforcement Notes table gains an advisory/non-blocking `orphan_check.py` Stop-hook row (fail-open, never blocks). Effective 2026-06-24. Rationale: memory-only registration was a silent, team-invisible loss path with no written rule or mechanical nudge against it. Authorship: requires Helder human review.
```

> Helder note: confirm the changelog line format matches the surrounding entries (it mirrors
> the `06/15/2026 - amend` lines: `**Old:**…/**New:**…/Effective…/Rationale:…/Authorship:`).

---

## ⚠️ RETARGETING NOTE — 2026-07-14 (post rules-split; read BEFORE applying)

This file was written 2026-06-24 against the **monolithic** `workflow.md`. The 2026-07-14
token-scoped split (`baa6557`) moved the target sections. Apply the SAME content, but to:

| Diff | Old target | Apply now to |
|------|-----------|--------------|
| Diff 1 (Rule 1 Proactive BACKLOG triage) | `workflow.md` Rule 1 body | `.claude/library/workflow-rule-1.md § Proactive BACKLOG triage — Untracked work` (line ~68). Optionally add a one-line "memory is NOT a registration surface" note to the inline Rule 1 summary in `workflow.md`. |
| Diff 2 (Rule 2 exit checklist) | `workflow.md` Rule 2 body | `.claude/library/workflow-rule-2.md` exit-checklist detail; the inline `workflow.md` Rule 2 exit-checklist one-liner may gain "→ BACKLOG registration check". |
| Diff 3 (Hook Enforcement table row) | `workflow.md` Hook Enforcement Notes | `workflow.md § Hook Enforcement (never-miss)` inline table (still exists) — add the `orphan_check.py` Stop-hook row there; mirror in `.claude/library/workflow-hooks-invariant.md` if it lists Stop hooks. |

Everything else (rule text, exempt categories, `amend:` commit + changelog triple) is unchanged
and still valid. The hook wiring itself (Diff 3's subject) is ALREADY LIVE — `orphan_check.py` +
`session_marker.py` were merged into `settings.json` on 2026-07-14 (`824885d`); only the rule-text
half awaits this gate.
