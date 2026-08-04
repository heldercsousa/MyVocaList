---
id: persisted-string-trimming
title: **String trimming on persistence — centralized normalization analysis**
status: 🟡 In Progress
target: 2026-07-15
section: DevCycleCraft
goal: strings persisted to the DB should be trimmed (extension of BUG-046's query-side trimming) via one centralized Services-layer helper (search) + EF Core `ValueConverter`s (persistence).
gate: All code merged to develop 2026-08-04 — search normalization and the persistence ValueConverters are both live. Only Helder's on-device E2E sign-off remains before this goes terminal.
pointer: DevCycleCraft/persisted-string-trimming/
order: 100
kind: feature
---

# String trimming on persistence — centralized normalization analysis

Strings persisted to the DB should be trimmed (extension of BUG-046's query-side trimming) via one centralized Services-layer helper (search) + EF Core `ValueConverter`s (persistence). Specs: `requirements.md`, `design.md`, `tasks.md`, `plan.md`, `task-log.md`.

**Notes overflow (transcribed from the pre-migration BACKLOG row):** Task 6a in progress, Task 6 pending rebase onto it.

> **Task 7 (integration merge) executed [2026-08-04].** The persistence half of this item had
> been complete but **unmerged** on `feat/persisted-string-trimming-converters` since
> 2026-07-19; only Tasks 1–5 (search normalization) were on `develop`, which meant the item's
> own title deliverable was not actually shipped. That branch is now merged.
>
> - `develop` was merged into the branch first, so `develop` never held an unverified state.
> - The merge was conflict-free.
> - On the merge result: `dotnet build MyVocaList.sln` → 8 projects, 0 errors;
>   `dotnet test` → 519 passed, 0 failed; the 6 real-SQLite round-trip tests green.
>
> **Status is 🟡 In Progress, not ✅ Done, deliberately.** Every task in `tasks.md` is now
> checked, but Task 7's own review lane is *"Helder final review + on-device E2E gate"*, and
> Task 2's is *"Helder on-device E2E (REQ-TRIM-01/02, autocomplete singer field)"*. Neither has
> happened. Marking this ✅ would record a sign-off that nobody gave — same shape as the
> Session Continuity row, which holds at 🟡 pending Helder's live demo.
>
> **To resume:** the only remaining work is Helder exercising search + save on device and
> confirming REQ-TRIM-01/02 behaviour. Nothing is left to implement. Detail: `task-log.md`
> Task 7 entry.
>
> The verifier's one open warning — the `Infra → Services` ProjectReference — was already
> resolved by D4: `Infra` now references the leaf `MyVocaList.Extensions` project, so the
> DRY Onion direction is intact. No escalation is outstanding.
