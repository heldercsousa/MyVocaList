# Session Continuity — Task Leasing & Auto-Resume — Spike Findings (AC-5)

> **Date:** 2026-06-14
> **Status:** COMPLETE — VIABLE
> **Executed by:** Opus 4.8 via `claude-code-guide` research against the official Claude Code hook docs.

## Summary

| AC | Verdict | Evidence |
|----|---------|----------|
| AC-5.1 — `session_id` exposure | PASS | `session_id` present on stdin in ALL hook event payloads; stable per session; `/clear` -> new id, `--continue`/`--resume` -> same id. |
| AC-5.2 — hook writes claim on tool use | PASS | `PostToolUse` fires after every successful tool call; can run arbitrary shell -> atomic heartbeat write. `Stop` also fires every turn end. |
| AC-5.3 — git-commit fallback | PASS (fallback only) | `cwd` present in every payload -> branch + `git log -1 --format=%ct` recency check viable; weaker than heartbeat. |

## AC-5.1 — session_id exposure: **PASS**

`session_id` is present in **all** hook event payloads on stdin (PreToolUse, PostToolUse, Stop, UserPromptSubmit, SessionStart, etc.) and is stable for the lifetime of one session.

- **`/clear` starts a FRESH session with a NEW `session_id`** — so an abandoned claim naturally ages into staleness (consistent with AC-3.2).
- **`--continue` / `--resume` RESTORE the same `session_id`** — enables same-session auto-resume (AC-4.1).

**Common input fields all hooks receive:** `session_id`, `transcript_path`, `cwd`, `hook_event_name`, `permission_mode`, `effort.level`.
**Conditional fields:** `agent_id` / `agent_type` (subagent context only); `model` + `source` (SessionStart only; `source` in startup | resume | clear | compact).

## AC-5.2 — hook can write claim file on tool use: **PASS**

`PostToolUse` command hooks fire after **every successful tool call** (Bash, Edit, Write, Read, MCP tools). They are non-blocking (the tool already ran) and can execute arbitrary shell — therefore they can write/update the claim file.

- Default timeout: **600s**.
- Exit codes: `0` = normal (stdout JSON parsed for optional `decision` / `additionalContext`); `2` = blocking error; other = stderr shown, execution continues.
- PostToolUse tool-specific fields: `tool_name`, `tool_input`, `tool_output`.
- The `Stop` hook also fires at every turn end and can write files (it is blocking — it can prevent stop).

**Recommended mechanism:** a `PostToolUse` command hook keyed by `session_id` that writes an **atomic heartbeat** (write tmp file + `mv`).

## AC-5.3 — git fallback viable: **PASS (fallback only)**

`cwd` is in every payload, so a fallback can derive the branch and check `git log -1 --format=%ct` recency.

Weaker than the heartbeat: commit-granularity window, requires a git repo, and only registers as "fresh" if work actually produces commits. Retained **only** as a defensive fallback; the hook heartbeat is the primary mechanism.

## Bonus — self-scheduled re-entry

`/loop` (CronCreate / ScheduleWakeup) exists but is **session-bound**: it fires only while the terminal/session is open and idle, is lost if the session exits, and is restored on `--resume` if <= 7 days old. True durable cross-session re-entry needs cloud routines (`/schedule`).

**Implication for AC-4:** in-session auto-resume after a usage-window reset is achievable with a scheduled wakeup loop; resilience to a fully-closed terminal would require a cloud routine or an external monitor process.

## Decision impact

The design's locked decisions (2026-06-14) hold. Heartbeat-via-hook is confirmed as the **PRIMARY** freshness mechanism; git-commit-on-branch is the documented **fallback** (no longer "if the spike fails" — the spike passed, so it is just a defensive fallback). **Design is now LOCKED; proceed to `writing-plans`.**

---

**Doc reference:** official Claude Code hooks docs (https://code.claude.com/docs/en/hooks.md) and scheduled-tasks docs (https://code.claude.com/docs/en/scheduled-tasks.md).
