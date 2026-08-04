---
id: BUG-040
title: "BUG-040: mobile autocomplete input loses focus (Major)"
status: "✅ Fixed"
severity: Major
target: 2026-07-12
section: DevCycleCraft
parent: autocomplete-component
kind: bug
closed: 2026-07
order: 30
goal: "Fixed (deferred focus after modal animation); manual E2E documented."
pointer: DevCycleCraft/autocomplete-component/bugs/2026-07-12-BUG-040-mobile-input-loses-focus/
---

# BUG-040: mobile autocomplete input loses focus

Fixed by deferring focus until after the modal open animation completes; manual E2E
documented (Major severity, UI-only — per `bug-tracking.md` no automated regression test is
mandatory).

> Migrated from the 2026-07 archive row (T12a Wave P). No flat file existed for this row —
> net-new at the REQ-SEV-01 dated-slug shape. This is the first item filed under a fresh
> lowercase `bugs/` solution folder for `autocomplete-component` — the pre-existing capitalized
> `Bugs/` folder (holding `bug-043`) predates the REQ-SEV-01 scheme and is a separate,
> untouched registration; flagged for audit.

**History / back-link:** `DevCycleCraft/autocomplete-component/task-log.md`
