---
id: disabled-dotnet-skills-still-listed-at-every-session-start
title: Disabled dotnet-skills still listed at every session start
status: ✅ Done
target: 2026-11-03
section: DevCycleCraft
parent: per-agent-context-isolation
goal: About 30 dotnet-skills skills set off in settings.local.json still appear in the skill listing, so every session and subagent start pays for them.
gate: Find whether off only blocks invocation or is not applied, then drop them from the listing.
closed: 2026-11
kind: change
---

# Disabled dotnet-skills still listed at every session start

About 30 dotnet-skills skills set off in settings.local.json still appear in the skill listing, so every session and subagent start pays for them.


## Evidence (2026-11-03, `/context` in the main session)

- `.claude/settings.local.json` `skillOverrides` holds 55 entries; the `dotnet-skills:*` ones
  (Akka.NET ×5, Aspire ×3, mailpit, crap-analysis, api-design, concurrency, cscheck, mjml, OpenTelemetry,
  playwright ×2, testcontainers, …) are set to `"off"`.
- `/context` still lists all of them under Skills (≈ 30 entries, ~60–300 tokens each — roughly 3k of the
  9.9k skills total), plus the 6 `dotnet-skills` plugin agents (~660 tokens).
- Four `dotnet-skills` skills are deliberately ON (`CLAUDE.md § MCP & Skills`) — the fix must keep them.

## Questions

1. Does `"off"` remove a skill from the listing, or only block invocation? (Check the
   `claude-code-guide` docs / settings schema; test with one skill.)
2. If it is listing-visible by design: is there a setting that hides it, or should the plugin be
   disabled and the four needed skills vendored into `.claude/skills/`?

## Findings (2026-11-03, second session)

**Q1 answered — `"off"` works for project skills, is ignored for plugin skills.** Evidence from one
session's skill listing: the 19 `maui-*` project skills set `off` are absent; `dotnet-skills:crap-analysis`
and `superpowers:test-driven-development` (both `off`) are still listed.

**Skill listing by source (~9.9k total, `/context` estimates):**

| Source | ≈ tokens | Used by this project | Lever |
|---|---|---|---|
| `dotnet-skills` plugin (38 skills + 6 agents) | 3.7k + 0.7k | 4 skills | disable plugin, vendor the 4 into `.claude/skills/` (~4k saved) |
| Built-in (dataviz, code-review, claude-api, artifact-*, update-config, …) | 2.6k | few | test `skillOverrides` `off` on one (e.g. `claude-api`, `dataviz`) |
| claude.ai synced (docs, computer-use, browsers, deep-research, morning, office, …) | 2.0k | ~none | test `skillOverrides`; else claude.ai account settings |
| `superpowers` (16) | 1.1k | most | keep; per-skill `off` ignored, vendoring not worth ~0.5k |
| `claude-code-setup` + `cowork-plugin-management` | 0.4k | one-off | disable `claude-code-setup` after use; find who enables cowork |
| Project skills (11) | 0.6k | all | keep |

Memory files (13.9k) remain the largest always-loaded block — tracked by the dispatch post-ship review (#3).

## Protocol — one change at a time

1. Baseline: `/context` in a fresh session + GATE-A probes (`general-purpose`, `implementor`), method in
   `../../context-baseline.md`.
2. Apply ONE lever from the table, start a fresh session, re-measure both.
3. Record the delta here; keep it or revert it. Then the next lever.

Order: (a) `off` test on one built-in + one synced skill (zero risk, settings-only) → (b) dotnet-skills vendoring
(needs Helder approval: changes the CLAUDE.md `dotnet-skills:` references) → (c) plugin toggles.

## Results

| Run | general-purpose | implementor | Listing |
|---|---|---|---|
| Baseline | 39,417 | 31,449 | full (~100 entries) — subagents now get the full listing + memory files (July: ~2–2.5k, none) |
| A: `off` for `dataviz`, `anthropic-skills:morning`, `dotnet-skills:akka-aspire-configuration` (declared name) | 40,310 | — | `dataviz`, `morning` gone; akka still listed |
| C: `off` 12 unused built-in/synced; `user-invocable-only` 8 command-only built-ins | 39,221 | — | all 20 gone (~95 → 72 entries); `user-invocable-only` hides from the model, `/command` still works |
| B: `dotnet-skills` + `claude-code-setup` plugins disabled, 4 skills vendored | 38,346 | — | not applied live — plugins load at session start; confirm with `/context` after restart |

- Built-in and claude.ai-synced skills honour `off`, live, no restart. Plugin skills ignore it under either key form → only lever for dotnet-skills is disabling the plugin (b).
- Probe token totals vary ±1k with answer length — too noisy for sub-1k deltas; use the listing presence (binary) and `/context` in a fresh session for sizes.
- A is kept (local `settings.local.json`).

**Done (Helder-approved 2026-11-03):** A, B, C; all overrides now in `settings.json`. **Close when** a fresh-session `/context` shows no `dotnet-skills:*` skills/agents and Skills ≈ 5k (from 10k).

## Outcome (closed 2026-11-03)

Fresh-process check (`claude -p`): 0 `dotnet-skills:*` skills/agents, `claude-code-setup` gone, vendored skills present.
Main-session `/context` Skills **10k → 6.6k** measured after A + C (plugin still loaded there — `/clear` does not
reload plugins; restart the process). Expected after restart ≈ 3.0k Skills, Custom agents 1.1k → 0.5k.
Left: `cowork-plugin-management` (~0.24k) is synced from the claude.ai account — not repo-controllable.
