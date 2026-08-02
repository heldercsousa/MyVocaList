# Session Operations Guide — MyVocaList

> Loaded on-demand. Reference when starting sessions, managing multi-wave state, or writing handoff artifacts.
> For the session start reading order (7 steps), see `.claude/rules/workflow.md § Rule 7`.

---

## Tiered memory governance

Memory in this workflow is tiered by durability and scope. Each tier has a different owner, a different lifecycle, and a different read obligation.

| Tier | File | Owner | Lifecycle | Read obligation |
|------|------|-------|-----------|-----------------|
| **Constitutional** | `CLAUDE.md`, `.claude/rules/*.md` | Helder | Permanent — amended only by Helder | Read on setup and after any amendment |
| **Architectural** | `Docs/Management/[BusinessFeatures|DevCycleCraft]/[feature]/design.md`, `Key Decisions` | Helder | Feature lifetime | Read at session start for features being worked on |
| **Operational** | `Docs/Management/[BusinessFeatures|DevCycleCraft]/[feature]/tasks.md`, task-log | Main agent | Feature lifetime | Read at every session start |
| **Session** | `ACTIVE-CONSIDERATIONS.md`, `session-handoff.md` | Main agent | Single session or single handoff | Read at session start; update continuously |
| **Ephemeral** | In-context notes, subagent briefing state | Subagent | Context window only | Not persisted — must be written to a durable tier before session ends |
| **Device auto-memory** (NOT a registration surface) | `~/.claude/projects/<project>/memory/*` | Harness / agent (per-device) | Per-device, not git-tracked, not team-visible | Optional aid only — never the sole home for any work item |

**Governance rules:**
1. **Constitutional tier** — never modify without explicit approval from Helder. Any agent that edits `CLAUDE.md` or a rules file without authorization has violated the governance model.
2. **Architectural tier** — subagents may add a `> **Spec updated:**` note but cannot rewrite or delete content.
3. **Operational tier** — main agent maintains. Subagents update task-log status and Changed files entries only.
4. **Session tier** — main agent creates and maintains. Written to disk before session ends (never left as ephemeral).
5. **Ephemeral tier** — no item should remain ephemeral if it needs to survive past the current session. If it matters, write it to the Operational or Session tier before stopping.
6. **Device auto-memory tier — NOT a registration surface.** This per-device tree is personal, not git-tracked, and not team-visible. Recording a work item here does NOT register it: **an item folder with frontmatter** (created by `backlog_gen.py register`) is the only registration surface — `BACKLOG.md` is a generated view of that surface, not the surface itself. A work item that lives only in device memory is an orphan, and the advisory Stop-hook will warn about it at session end. Use this tier as a private continuation aid only — never as the sole home for any work item.

**Promotion rule:** When a constraint, decision, or discovery is discovered during implementation and needs to be remembered:
- Temporary reminder → `ACTIVE-CONSIDERATIONS.md` (Session tier)
- Recurring constraint → `constraints-registry.md` (Constitutional tier)
- Design decision → `design.md` Key Decisions (Architectural tier)
- Task outcome → task-log (Operational tier)

---

## Session start constraint capture

**BACKLOG.md is not in the session-start read set.** Use `backlog_gen.py query --status "🟡,🟢"`
(workflow.md Rule 7 step 1). Reading the rendered file costs ~4.5k tokens for the same information
and is a Rule 7 violation, not a fallback.

After reading the session start steps (Rule 7), before dispatching the first wave, record any newly discovered constraints or decisions from the previous session that have NOT yet been committed to their permanent home:

- New constraint → add to `.claude/rules/constraints-registry.md`
- New design decision → add to `design.md` Key Decisions
- Open question → add to `ACTIVE-CONSIDERATIONS.md` Open items

**Why session start is not optional:** Context windows reset between sessions. An orchestrator that resumes from memory is operating on a lossy reconstruction of the previous session's state. The session start reading order replaces that lossy reconstruction with a direct read from the authoritative files.

---

## ACTIVE-CONSIDERATIONS.md — session priority stack

For long or complex sessions involving multiple waves and evolving context, maintain an `ACTIVE-CONSIDERATIONS.md` file as a **session priority stack** — a short, always-current list of the most important things to keep in mind.

**File location:** `Docs/DevEnv/ACTIVE-CONSIDERATIONS.md` (single file, overwritten each session)

**Format:**
```markdown
# Active Considerations — [YYYY-MM-DD]

## Current priority
[One sentence: what is the single most important thing right now?]

## Open items (ordered by urgency)
1. [Highest urgency: blocking something — must resolve before next wave]
2. [Spec gap to resolve — needs Helder input]
3. [Known risk to watch — may affect Wave N+1]

## Do not forget
- [Constraint discovered this session that is not yet in constraints-registry.md]
- [Decision made this session that is not yet in design.md]
- [Task that was deferred and must not be forgotten]

## Wave status
Current wave: N
Next wave: N+1 — [brief description of what it contains]
Checkpoint due: [after Wave N+1 | already done]
```

**When to update it:**
- At the start of a session: initialize with current state from task-log + MASTER_PLAN.md
- After each wave completes: update wave status and open items
- When a new constraint or decision arises: add to "Do not forget"
- At the end of a session: commit the final state (it is the handoff artifact for the next session)

**Rule:** This file replaces "held in context" items that are likely to be dropped during compaction. If something is important enough to remember across waves, it must be in `ACTIVE-CONSIDERATIONS.md` — not in the orchestrator's working memory.

**Relationship to handoff artifact:** `ACTIVE-CONSIDERATIONS.md` is the lightweight version for same-day use. The full `session-handoff.md` (see below) is written at session end for cross-session continuity. Both serve complementary roles.

---

## findings.md — session artifact for exploratory work

When a session involves significant exploration, debugging, or spike work, the findings must be captured in a `findings.md` file before the session ends. This prevents the work from being lost when the context window is discarded.

**When to create `findings.md`:**
- Any `[SPIKE]` task (mandatory — see workflow.md § Spike validation task pattern)
- Any session where the cause of a bug was non-obvious and required investigation
- Any session where an architectural option was explored and rejected
- Any session where a library or API was evaluated for the first time

**File location:** `Docs/Management/[BusinessFeatures|DevCycleCraft]/[feature]/findings.md` for feature spikes, or `Docs/DevEnv/findings/[YYYY-MM-DD]-[topic].md` for general technical findings not tied to a feature.

**findings.md format:**
```markdown
# Findings — [Topic] — [YYYY-MM-DD]

## Context
[One paragraph: why this investigation was needed]

## What was tried
- **Approach A:** [description] → [result: worked / failed / partial]
- **Approach B:** [description] → [result: worked / failed / partial]

## What was learned
[Key discoveries — platform behaviors, library quirks, performance data, etc.]

## Constraints discovered
[Any new constraints that should be added to constraints-registry.md]

## Recommendation
[One sentence: what to do next, with rationale]

## Open questions
[Questions that were not resolved — must be resolved before implementation proceeds]
```

**Rule:** A spike or investigation that does not produce a `findings.md` has produced nothing — its output exists only in the subagent's expiring context. The findings artifact is the only durable output of exploratory work.

**After `findings.md` is created:** The main agent reads it before writing the spec. Key constraints in `findings.md` must be propagated to `constraints-registry.md` before any implementation begins.

---

## Multi-session state handoff protocol

When a feature spans multiple sessions, the state at session end must be captured so the next session can resume without loss.

**Session-end handoff artifact (write to `Docs/Management/[BusinessFeatures|DevCycleCraft]/[feature]/handoff.md`):**
```markdown
# Session Handoff — [Feature Name] — [YYYY-MM-DD]

## Last completed wave
Wave N — [brief description] — all tasks committed

## Current state
- Build: PASS / FAIL
- Tests: N passing, N failing
- Last committed task: [task title]

## Next wave
Wave N+1 — [tasks to dispatch]
- Task A: [scope, files owned]
- Task B: [scope, files owned]

## Open items
- [spec gaps, pending decisions, deferred concerns]

## Contracts in play
- [current interface signatures that the next wave will consume]

## Files modified this session
- [list of all files touched since session start]
```

**Rule:** The session-end handoff artifact must be committed before the session ends. It is the only reliable state source for the next session — in-context memory does not persist.

---

## Checkpoint Ping & Context Manifest — interruption resilience

> Added 2026-07-14 (Helder directive). The artifacts above write at session/task *end* — they do not survive a hard interruption (connection lost, session limit, crash) mid-task. The Checkpoint Ping is the **write-ahead** counterpart: a small block updated continuously so any interruption loses at most one step, and the resuming agent reads a known short file list instead of globbing.

**Where:** a `### Checkpoint` block inside the task's `task-log.md` entry — **overwritten in place** at each ping (it is live state, not history; history is the rest of the entry).

**Ping cadence (write-ahead — the whole point):**
1. **Before starting each step**, write what is about to be attempted (if interrupted mid-step, the resumer knows exactly what was in flight and can verify/redo it).
2. **After every build/test run**, record the result.
3. **At every phase transition** (spec → plan → implement → review; or task step N → N+1).
4. **Time floor — the heartbeat loop:** regardless of events, ping at least every **~10 minutes of continuous work** (practical proxy for an agent: every **~15 tool operations** since the last ping, whichever comes first). Long single steps (big refactors, long investigations) are exactly where interruptions hurt most — the loop guarantees the checkpoint never goes stale even when no step boundary is reached.
5. **Cross-session pointer refresh:** each ping should also refresh the lease resume pointer via `python .claude/scripts/lease/resume.py --set <session_id> "<task-log path> § Checkpoint"` — or rely on the heartbeat hook's default (it fills an empty pointer from `.claude/active-task.json`; it never overwrites a non-empty one).

A ping is a 30-second edit — never batch pings "for later"; later may not come.

**Checkpoint block format:**
```markdown
### Checkpoint (live — overwrite on each ping)
**Pinged:** YYYY-MM-DD HH:MM
**Branch / worktree:** <branch> / <.worktrees/name or main tree>
**Step:** <N of M> — last completed: <one line> — now attempting: <one line>
**Build/test state:** <last known: build 0 errors | test failure X | not yet run>
**Next command:** <the literal next command or edit, e.g. "dotnet test after fixing VenueServiceTests.Save_...">

**Context manifest (read ONLY these to resume — no Glob):**
- `path/to/file` — <why: e.g. "the file being edited; method Foo half-done">
- `Docs/.../design.md § Section` — <why: contract being implemented>
- `Docs/.../tasks.md` — <why: step list, current step N>
```

**Context manifest rules:**
- Lists **every** file a fresh agent must read to diagnose and continue the task — exact paths, optionally `§` sections, each with a one-line why. Aim for ≤ 8 entries; if more are needed, the task is oversized (Rule 2 sizing).
- Kept current at each ping (files enter/leave as work moves).
- **Resume protocol:** LEDGER.md row → task-log `### Checkpoint` → read only the manifest files. A resuming agent MUST NOT `Glob` to reconstruct state — if the manifest is insufficient, that is a checkpoint-quality defect: fix the manifest for next time, note the gap in the task-log.
- On task completion, the Checkpoint block is replaced by the normal final entry fields (`Status: To Review`, `### Changed files`) — the manifest's job is done.

**Relationship to other artifacts:** the resume pointer (`resume.py --set`) stays the one-line cross-session locator; LEDGER.md locates the branch; the Checkpoint block carries the step-level state + read list. handoff.md remains the *planned* session-end artifact — the Checkpoint is what exists when the session never got to write one.

---

## Context exhaustion warning signs

Context window exhaustion degrades output quality before the window is fully used. Recognize the early signs and act before the damage compounds.

**Warning signs in subagent output:**
- Subagent contradicts a decision it made earlier in the same session
- Subagent asks about information it was given in the briefing
- Subagent produces code that duplicates something it already wrote
- Subagent forgets a constraint it acknowledged earlier (e.g., uses `DisplayAlert` after being told not to)
- Build errors reference types or namespaces the subagent invented rather than read from the spec
- Subagent output becomes shorter and less specific with each iteration
- Subagent claims work is done but Changed files list is sparse relative to the task scope

**Warning signs in orchestrator context:**
- You are writing a briefing from memory without re-reading the spec
- You cannot recall what the previous wave committed without checking the task-log
- You are reasoning about code structure from cached impressions rather than reading the current file

**Response protocol:**
1. If the subagent shows warning signs: kill it (see Kill criteria in `agents/orchestrator.md`), re-read the spec, produce a tighter briefing, dispatch a fresh subagent.
2. If the orchestrator shows warning signs: stop. Re-read MASTER_PLAN.md, the spec, and the task-log. Resume from verified ground truth.