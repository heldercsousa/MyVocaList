---
id: disabled-dotnet-skills-still-listed-at-every-session-start
title: Disabled dotnet-skills still listed at every session start
status: 💡 Pending
target: 2026-11-03
section: DevCycleCraft
parent: per-agent-context-isolation
goal: About 30 dotnet-skills skills set off in settings.local.json still appear in the skill listing, so every session and subagent start pays for them.
gate: Find whether off only blocks invocation or is not applied, then drop them from the listing.
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
