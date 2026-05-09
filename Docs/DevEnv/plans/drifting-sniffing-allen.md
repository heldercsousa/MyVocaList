# Plan: Docs/ Context Scope Control — Token Savings

## Context

The `Docs/` tree has grown to **125 files** (77 in DevEnv/SDD alone). Claude Code performs
directory-wide reads by default during session start and subagent briefing. Agents that scan
`Docs/` indiscriminately waste context on completed SDD theory, old changelogs, finished plans,
and setup guides — content that is never relevant once the project is past initial setup.

The SDD spec (S4.2, S5.3) calls for *just-in-time selective loading* and briefing subagents with
file *paths*, not content. The workflow rules (Rule 7) list which files to read at session start,
but nothing *prevents* broader scans if an agent improvises. The gap is mechanical enforcement.

---

## What Already Exists

| Mechanism | Location | What it does |
|-----------|----------|-------------|
| `.claudeignore` | repo root | Excludes build artifacts, binaries, IDE files, `Docs/Guides/**`. Already has a comment noting DevEnv setup docs should be ignored after setup. |
| `CLAUDE.md § MCP Context Budget` | CLAUDE.md | Limits which MCP servers to activate per task type. |
| `CLAUDE.md § Context size governance` | CLAUDE.md | Keeps CLAUDE.md under 600 lines; routes detail to `.claude/library/` and `.claude/rules/`. |
| `workflow.md Rule 7` | `.claude/rules/workflow.md` | Session start reading order: 7 specific files to read, scoped to the active feature. |
| `workflow.md § Briefing protocol` | `.claude/rules/workflow.md` | Subagents receive *file paths only* — no inline rule content pasted into briefings. |
| `workflow.md § Subagent MCP isolation` | `.claude/rules/workflow.md` | Permitted MCPs listed per task type in role scope block. |
| `workflow.md § Pre-task context gate` | `.claude/rules/workflow.md` | Subagent must verify spec + test files exist *before* reading broadly. |
| `implementor.md` | `.claude/agents/implementor.md` | Mandates reading only 6 specific files; defines files owned / off-limits. |
| `orchestrator.md` | `.claude/agents/orchestrator.md` | Re-reads spec fresh each wave; scoped to feature spec path. |

**Gap:** None of the above *excludes* the large, static, theoretical parts of `Docs/` from agent
reads. An agent that calls `Glob("Docs/**/*.md")` or `Read("Docs/")` during planning will pull in
all 125 files. `.claudeignore` currently does not cover `Docs/DevEnv/SDD/`, `Docs/Changelog/`,
completed plan logs in `Docs/superpowers/plans/`, or the `Docs/Plans/` folder.

---

## Recommended Approach

### 1. Extend `.claudeignore` with Docs/ scope rules

Add targeted exclusions at the bottom of `.claudeignore`. Rules are additive — existing entries
are untouched.

```
# Docs/ — selectively exclude high-volume, low-relevance subtrees
# SDD theory files are reference material; never needed during coding sessions
Docs/DevEnv/SDD/**

# Completed session plans and task-logs — read via explicit path if needed
# (active plan path is always injected into briefings directly)
Docs/superpowers/plans/**

# Changelog — historical record; never needed during feature work
Docs/Changelog/**

# Old Plans folder (superseded by superpowers/plans/)
Docs/Plans/**

# DevEnv setup guides — relevant only during environment onboarding
Docs/DevEnv/DevEnv/**
```

**Escape hatch:** Any agent that legitimately needs one of these files can still `Read()` it by
absolute path — `.claudeignore` only prevents *directory-glob scans*, not direct reads.

### 2. Add a `Docs/CLAUDE.md` routing stub (subdirectory-level context gate)

Claude Code merges CLAUDE.md files found while walking up the directory tree. A minimal
`Docs/CLAUDE.md` can declare scope rules that apply whenever an agent is working inside `Docs/`:

```markdown
# Docs/ — Context Scope Gate

Agents working inside this directory must read **only** the files explicitly listed
in their briefing's `Spec source:` field or `Files owned:` list.

Do NOT perform open-ended glob scans of this directory tree.
Do NOT read files outside the active feature's spec path
(`Docs/specs/[feature]/`) unless the briefing explicitly authorises it.

Quick routing:
| Need | Read |
|------|------|
| Active feature spec | `Docs/specs/[feature]/requirements.md`, `design.md`, `tasks.md` |
| Active plan/task-log | Path provided in briefing |
| SDD methodology reference | `Docs/DevEnv/SDD/0_0_0_0_0_SDD_Spec_Driven_Development.md` only |
| Changelog | Not needed during feature work — skip |
```

### 3. Tighten workflow.md Rule 7 — explicit anti-glob instruction

In `workflow.md § Session start reading order`, add one enforcement line after the 7-step list:

> **Anti-glob rule:** Never call `Glob("Docs/**")` or equivalent open-ended scans during
> session start or briefing. Read only the 7 files listed above plus the active feature's
> spec files. All other `Docs/` content is excluded by `.claudeignore` glob scans and must
> be accessed by explicit absolute path only.

### 4. Add `Docs/` scope note to implementor.md and orchestrator.md

In each agent definition's **Context reading protocol** section, add:

> Docs/ tree reads are restricted to `Docs/specs/[feature]/` and the explicit plan path in
> the briefing. No open-ended glob scans of Docs/. SDD files and completed plan logs are
> `.claudeignore`-excluded for glob scans.

---

## Files to Change

| File | Change |
|------|--------|
| `.claudeignore` | Add 5 Docs/ exclusion rules (new section at bottom) |
| `Docs/CLAUDE.md` | Create — subdirectory-level context gate stub (~20 lines) |
| `.claude/rules/workflow.md` | Add anti-glob line after Rule 7 step list |
| `.claude/agents/implementor.md` | Add one-line Docs/ scope note |
| `.claude/agents/orchestrator.md` | Add one-line Docs/ scope note |

---

## What This Does NOT Do

- Does not prevent agents from reading any file by explicit path — only glob scans are gated
- Does not modify the SDD spec files themselves
- Does not change CLAUDE.md (already under 600-line limit)
- Does not affect skills (third-party, excluded from scope per user instruction)

---

## Verification

After implementing:
1. Run `! cat .claudeignore` — confirm new Docs/ sections appear
2. In a fresh session, ask Claude: "List all files in Docs/" — it should return only the
   non-excluded subtrees (specs/, Design/, task-log.md, BACKLOG.md, CLAUDE.md)
3. Confirm an explicit `Read("Docs/DevEnv/SDD/S4_Context_and_Memory.md")` still works — 
   `.claudeignore` must not block direct reads, only glob scans
