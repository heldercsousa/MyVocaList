---
id: system-tools-tokens-grew-18-3k-to-25-4k-across-fresh-sessions
title: System tools tokens grew 18.3k to 25.4k across fresh sessions
status: ✅ Done
target: 2026-11-03
section: DevCycleCraft
parent: per-agent-context-isolation
goal: The always-loaded System tools block grew about 7k between sessions on 2026-11-03 and cancelled the skill-listing savings. Find what loads and cut what is unused.
gate: Root cause named with /context evidence; each lever applied one at a time and measured in a fresh process.
closed: 2026-11
kind: change
---

# System tools tokens grew 18.3k to 25.4k across fresh sessions

The always-loaded System tools block grew about 7k between sessions on 2026-11-03 and cancelled the skill-listing savings. Find what loads and cut what is unused.


## Evidence (2026-11-03, `/context all`, Claude Code v2.1.318, Opus 5.5)

| Session | System tools | Skills | Custom agents | Total |
|---|---|---|---|---|
| Start of day | 18.3k (+27.4k deferred) | 9.9k | 1.1k | 49.0k |
| Fresh #2 (after overrides) | 21.6k | 6.6k | 1.1k | 49.7k |
| Fresh #3 (after plugin disable) | 25.4k | 2.8k | 0.5k | 49.0k |
| Fresh #4 (baseline, no change; sqlite down) | 26.1k (+28.2k deferred) | 2.8k | 0.5k | 62.6k* |
| Fresh #5 (2026-11-04, sqlite up after BUG-083 pin) | 25.4k (+28.2k deferred) | 2.8k | 0.5k | 49k |
| Fresh #6 — lever 1: `deny: ["Artifact"]` (`settings.local.json`; Helder OK 2026-11-04) | **14.0k** (−11.4k) | 2.8k | 0.5k | **37.4k** (−11.6k) |
| Fresh #7 — deny moved to tracked `settings.json` | 14.0k | 2.8k | 0.5k | 37.4k |

## Result (2026-11-04)

| Item | Finding |
|---|---|
| Root cause | `Artifact` built-in tool schema (~11k), always loaded; not used in this project |
| Lever kept | `"Artifact"` in `permissions.deny` of tracked `.claude/settings.json` — applies to every clone (moved from gitignored `settings.local.json` 2026-11-04, Helder) |
| Revert | delete the `"Artifact"` deny entry |
| Not applied | Bash/PowerShell shell lever — 14k is already below the 18.3k start-of-day; a shell deny risks the rtk Bash hook |
| Residual | built-in `artifact-*` skills still listed (~390 tokens); left as-is |

\* Total includes 14.9k Messages (measured mid-session). Deferred line is back in #4 → hypothesis "deferred vanished" not confirmed;
#3's output may just have omitted it. Always-on extras in #4: Claude Docs MCP (3 tools loaded + 1.5k MCP instructions incl. Docs).

The skill cuts (sibling item `2026-11-03-disabled-dotnet-skills-…`) saved ~7.8k; System tools ate it. Same version,
same machine, same day. Not a skill lever. Hypothesis to test first: fewer listed skills/agents → harness loads
more tool schemas up front instead of deferring them (the deferred line vanished from #3's output).

## Hypotheses (2026-11-04, from #5's loaded-tool list)

| Candidate | Est. tokens | Lever (needs Helder OK — disables a tool) |
|---|---|---|
| `Artifact` tool (long schema; matches the ~7k jump) | ~6–7k | `permissions.deny: ["Artifact"]` in settings |
| `Bash` + `PowerShell` both loaded | ~2k each | deny one shell |
| `ScheduleWakeup`, `ReportFindings`, `SendFeedback`, `ListAgents` | <1k each | low value; skip |

## How to investigate

1. Fresh process only (`/exit` → `claude`; `/clear` does not reload). Record `/context all` + `/context` totals.
2. List which tools are loaded vs deferred (`/context all`, ToolSearch listing); compare with the start-of-day line.
3. Check `claude-code-guide` / docs for the deferral threshold or a setting that controls it.
4. One lever at a time, fresh process each time, record the row here. Probe subagents too (method:
   `../../context-baseline.md`; probe token totals are ±1k noise — use presence, not totals, below 1k).
