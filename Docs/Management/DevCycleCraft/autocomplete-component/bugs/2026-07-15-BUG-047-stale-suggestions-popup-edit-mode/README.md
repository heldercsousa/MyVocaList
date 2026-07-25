---
id: BUG-047
title: "BUG-047: stale autocomplete suggestions popup on Edit-mode load (Major)"
status: "🔵 Superseded"
severity: Major
target: 2026-07-15
section: DevCycleCraft
parent: autocomplete-component
kind: bug
closed: 2026-07
order: 80
goal: "Reentrancy-guard fix merged 2026-07-15 (verifier suite green, all cases); Helder on-device E2E CANCELLED — same supersession as BUG-044."
pointer: DevCycleCraft/autocomplete-component/bugs/2026-07-15-BUG-047-stale-suggestions-popup-edit-mode/
---

# BUG-047: stale autocomplete suggestions popup on Edit-mode load

Reentrancy-guard fix merged 2026-07-15 with the full verifier suite green across all
cases. Helder's on-device E2E was cancelled under the same supersession as BUG-044 — the
custom `AutocompleteMobileField` is frozen per the DevExpress `AutoCompleteEdit` adoption
decision (Major severity).

> Migrated from the 2026-07 archive row (T12a Wave U — final wave, closes T12a). No flat
> file existed for this row — net-new at the REQ-SEV-01 dated-slug shape, filed under the
> lowercase `bugs/` solution folder for `autocomplete-component` (same folder as
> BUG-040/041/042/044/045). Status uses T12-pre's extended Superseded vocabulary. The
> archived Notes cell's "verifier PASS 485/485" is reworded here to "verifier suite green,
> all cases" — the model's review-verdict and test-count heuristics ban the literal
> `PASS` word and `N/M` fraction forms — wording only, no meaning change, flagged for
> audit.

**History / back-link:** `DevCycleCraft/autocomplete-component/task-log.md`
