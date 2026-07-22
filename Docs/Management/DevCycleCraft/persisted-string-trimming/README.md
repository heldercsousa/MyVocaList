---
id: persisted-string-trimming
title: "**String trimming on persistence — centralized normalization analysis**"
status: "🗺️ Plan"
target: 2026-07-15
section: DevCycleCraft
kind: feature
order: 100
goal: "strings persisted to the DB should be trimmed (extension of BUG-046's query-side trimming) via one centralized Services-layer helper (search) + EF Core `ValueConverter`s (persistence)."
gate: "D1/D2 recorded 2026-07-15; D3 (EF Core `ValueConverter`) + D4 (helper relocated to leaf `MyVocaList.Extensions` project, extension-method API) recorded 2026-07-19."
pointer: DevCycleCraft/persisted-string-trimming/
---

# String trimming on persistence — centralized normalization analysis

Strings persisted to the DB should be trimmed (extension of BUG-046's query-side trimming) via one centralized Services-layer helper (search) + EF Core `ValueConverter`s (persistence). Specs: `requirements.md`, `design.md`, `tasks.md`, `plan.md`, `task-log.md`.

**Notes overflow (transcribed from the pre-migration BACKLOG row):** Task 6a in progress, Task 6 pending rebase onto it.
