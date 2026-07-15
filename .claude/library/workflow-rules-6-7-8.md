# Development Workflow — Reference — Rules 6–8 — Research Gate, Session Start, Collision Check

> Section file split from `workflow-reference.md` on 2026-07-14 (token-scoped reads). Index + never-miss rules: `workflow-reference.md`.

## Rule 6 — Research Tool Gate (Context7 → WebSearch)

Before any web research query, follow this hierarchy:

1. **Library / framework / SDK / API docs** → Context7 (`mcp__context7__resolve-library-id` → `mcp__context7__query-docs`)
2. **General web research** (comparisons, news, tool evaluations, articles) → `WebSearch` / `WebFetch` — only when Context7 does not cover the topic

> Amended 2026-07-08: the former tier 2 (Exa MCP `exa_search`) was removed — the `exa` server has been disabled locally since before 2026-07-07 and was never in the Security Stance approved list; the rule routed research to a tool that could not respond (BACKLOG row 220c). Re-adding Exa requires the Security Stance review.

This applies to **both the main agent and all subagents.**

---

## Rule 7 — Session Start Protocol

Every session that involves implementation or planning must begin with this reading order before any code is written or any subagent is dispatched.

### Session start reading order

Read in this order — do not skip items, do not resume from memory alone:

0. **Hook health verification** — confirm hooks are operational (see Hook Enforcement Notes at the top of this file). Fix any misconfigured hooks before proceeding.
1. **Active session handoff file** (if one exists): `Docs/Management/[BusinessFeatures|DevCycleCraft]/[feature]/handoff.md` — use this as the exact continuation point
   - **If no handoff file exists:** read `Docs/Management/BACKLOG.md` to identify the current `🟡 In Progress` item or the highest-priority `🟢 Ready` item — that is the current work context
2. **`ACTIVE-CONSIDERATIONS.md`** (if it exists) — read the priority stack and open items
3. **`Docs/Management/[BusinessFeatures|DevCycleCraft]/[feature]/tasks.md`** — confirm which tasks are done, in-progress (`[~]`), and pending
4. **`Docs/Management/[BusinessFeatures|DevCycleCraft]/[feature]/requirements.md`** — refresh acceptance criteria (do not rely on previous-session memory)
5. **`Docs/Management/[BusinessFeatures|DevCycleCraft]/[feature]/design.md`** — refresh architecture and interface signatures
6. **`Docs/Management/[BusinessFeatures|DevCycleCraft]/[feature]/task-log.md`** — check for unresolved `blocked:` statuses or `Spec updated — re-planning required` entries
7. **Lease claim refresh + resume-pointer read (Session Continuity):**
   - For the picked work unit, classify any existing `[~]`/`🟡 In Progress` claim under
     `.claude/leases/` via `python .claude/scripts/lease/reclaim.py <my_session_id> <owner_session_id>`:
     `fresh` → pick a different unit; `reclaimed` → take over; `lost` → pick the next unit
     (see Rule 4 lease-aware reclaim). Reclaim any **stale** unit before starting new work.
   - Before resuming, read the resume pointer with
     `python .claude/scripts/lease/resume.py <session_id>` and continue from it.
   - The heartbeat hook (registered in `.claude/settings.json`, `PostToolUse`/`Stop`) writes
     and keeps this session's own claim fresh automatically on every tool call — no manual
     ping is required (AC-3.1/3.3). Record a resume pointer as material progress is made via
     `python .claude/scripts/lease/resume.py --set <session_id> "<one-line continue-from-here>"` (AC-4.3).

**Rule:** Steps 1–7 are mandatory. Steps 3–7 may be scoped to the specific feature being worked on if multiple features are in flight.

**Anti-glob rule:** Never call `Glob("Docs/**")` or equivalent open-ended scans during session start or briefing. Read only the 6 files listed above plus the active feature spec files.

> **Session operations detail** (ACTIVE-CONSIDERATIONS.md format, findings.md format, handoff artifact format, context exhaustion warning signs, tiered memory governance, session start constraint capture): see `.claude/library/session-ops.md`.

---

## Rule 8 — Pre-Task Collision Check

Before dispatching any wave that modifies files in the repository, confirm that no other agent or branch is currently modifying the same files.

> Amended 2026-07-09: GitHub MCP references removed (server disabled 2026-07-07 — unused). The check is git + lease based; PR checks use the `gh` CLI.

### Pre-task collision check protocol

- Run `git log --oneline -10` to check recent commits (confirm the current spec reflects those changes)
- Run `git status` to confirm no uncommitted changes from a previous interrupted session
- If a remote review flow is active, run `gh pr list` — if any open PR touches a file in the current wave's `Files owned` list, a collision risk exists
- Check `tasks.md` for any tasks marked `[~]` that should not be in-progress
- **Liveness check (Session Continuity):** for every `[~]` task with no known running
  agent, classify its claim under `.claude/leases/` via
  `python .claude/scripts/lease/reclaim.py <my_session_id> <owner_session_id>` (which calls
  `lease_lib.classify`) **before** assuming abandonment. A `fresh` result means another live
  session owns it — do NOT reset to `[ ]`.

**Collision types and responses:**

| Collision type | Response |
|----------------|----------|
| Another open PR modifies a file in `Files owned` | Do NOT start the wave. Resolve the PR first. |
| Recent commit from another agent changed an interface the current wave consumes | Re-read the changed interface before briefing. Update briefings if signatures changed. |
| `[~]` task exists but no agent is known to be running it | Classify the claim via `reclaim.py` / `lease_lib.classify`. `fresh` → another live session owns it, leave it and pick the next unit. `stale` → reclaim (`reclaimed`) and resume from the pointer, or reset to `[ ]` and re-dispatch if not resuming. Never reset a `fresh` claim. |
| No collision detected | Proceed with wave dispatch |
