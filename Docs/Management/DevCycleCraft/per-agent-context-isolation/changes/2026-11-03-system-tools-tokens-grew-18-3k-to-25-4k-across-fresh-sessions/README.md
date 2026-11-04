---
id: system-tools-tokens-grew-18-3k-to-25-4k-across-fresh-sessions
title: System tools tokens grew 18.3k to 25.4k across fresh sessions
status: 💡 Pending
target: 2026-11-03
section: DevCycleCraft
parent: per-agent-context-isolation
goal: The always-loaded System tools block grew about 7k between sessions on 2026-11-03 and cancelled the skill-listing savings. Find what loads and cut what is unused.
gate: Root cause named with /context evidence; each lever applied one at a time and measured in a fresh process.
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

The skill cuts (sibling item `2026-11-03-disabled-dotnet-skills-…`) saved ~7.8k; System tools ate it. Same version,
same machine, same day. Not a skill lever. Hypothesis to test first: fewer listed skills/agents → harness loads
more tool schemas up front instead of deferring them (the deferred line vanished from #3's output).

## How to investigate

1. Fresh process only (`/exit` → `claude`; `/clear` does not reload). Record `/context all` + `/context` totals.
2. List which tools are loaded vs deferred (`/context all`, ToolSearch listing); compare with the start-of-day line.
3. Check `claude-code-guide` / docs for the deferral threshold or a setting that controls it.
4. One lever at a time, fresh process each time, record the row here. Probe subagents too (method:
   `../../context-baseline.md`; probe token totals are ±1k noise — use presence, not totals, below 1k).
