---
id: post-ship-review-did-the-agent-dispatch-recalibration-meet-its-expectations
title: Post-ship review — did the agent-dispatch recalibration meet its expectations
status: 💡 Pending
target: 2026-11-03
section: DevCycleCraft
parent: inline-trivial-fix
goal: "After a few work sessions, confirm the 2026-11-03 dispatch recalibration works: trivial fixes take the ITF lane and the trimmed context stays small."
gate: "Done when a clean Lane: ITF commit exists and no trivial-fix dispatch is found in recent sessions; otherwise open a follow-up."
kind: change
---

# Post-ship review — did the agent-dispatch recalibration meet its expectations

After a few work sessions, confirm the 2026-11-03 dispatch recalibration works: trivial fixes take the ITF lane and the trimmed context stays small.


Reviews the closed item [Recalibrate agent-dispatch settings](../2026-08-18-recalibrate-agent-dispatch-settings-orchestrator-read-ban-and-subagent-for-trivia/README.md)
(shipped 2026-11-03, commits `1ae8c017`, `a1b035d3`). Run it after a few real work sessions, not before.

## Expectations to check

| # | Expectation | Baseline (2026-11-03) | How to check |
|---|-------------|-----------------------|--------------|
| 1 | Diagnosed 1-file / ≤ 5-line fixes take the ITF lane instead of an implementor dispatch | No live `/sln-itf` run yet | `git log --grep "Lane: ITF" --oneline` (≥ 1 expected) · `grep -rn "^ITF" Docs/Management --include=task-log.md` (read every `ITF aborted:` reason) · session transcripts (`~/.claude/projects/<project>/*.jsonl`): any `implementor` dispatch whose briefing was a 1-file / ≤ 5-line fix means the lane is still being bypassed |
| 2 | The orchestrator confirms diagnoses with the read lane (grep + ≤ 2 files) instead of an Explore dispatch for single-fact lookups | Not measured | Transcripts: Explore/general-purpose dispatches whose prompt is a single signature/member/field question |
| 3 | Always-loaded context stays small after the trim | `/context` memory files 13.9k tokens (was 22.9k; CLAUDE.md 3.2k, workflow.md 3.1k) | `/context` in a fresh session; flag growth above ~16k |
| 4 | Subagents run on the cheaper default model | `model: sonnet` on implementor, verifier, spec-reviewer, plan-reviewer | Transcripts: model field of dispatched agents; Opus only where passed explicitly for design |

`/doctor` checks installation and settings health only; use it to confirm the hooks still load, not to answer #1, #2 or #4.

## Also review — settings changes of 2026-11-03

5. Settings commit `chore(settings): …` (this date) holds up:
   - TaskCreated/TaskCompleted agent hooks removed; `active-task.json` deleted, `changed-files.txt` untracked (orphan check still reads it).
   - `settings.local.json` untracked; its UserPromptSubmit gate moved to `settings.json`, stale Stop dup dropped, allow list 154 → 55.
   - Hook scripts on `$CLAUDE_PROJECT_DIR` (BUG-072); `Bash(...)` matchers → `hook-bash-filter.py`.
   - New: `protect-generated.py` guard, heartbeat 60 s throttle, force-push / `reset --hard` deny rules, Playwright pinned, `microsoft-learn` MCP.
   - Check: hooks fire, no false blocks, `/context` lower.

## Outcome

- All met → close ✅ with a one-line summary here.
- Any missed → record what was observed here and register a follow-up change under `inline-trivial-fix` before closing.
