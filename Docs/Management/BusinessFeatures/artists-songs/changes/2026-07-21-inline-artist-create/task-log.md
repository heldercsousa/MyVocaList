# Task Log — Song artist field: correctness fixes + inline "create new artist"

**Spec folder:** this folder (`requirements.md` · `design.md` · `tasks.md`).
**Parent:** Artists & Songs Catalog → closes BUG-027; folds in BUG-050 / BUG-051 / BUG-052 (found in DX-AC T7, 2026-07-21).

## Milestones

- **2026-07-21 — Design approved** (Helder, plan mode): inline create-new-artist, affordance = synthetic ➕ dropdown row (Option A) with Option-B fallback; scope minimal.
- **2026-07-21 — DX-AC T7 device run** (Helder): 3 pass / 6 fail. Root-caused BUG-050 (SelectArtist omits `IsArtistLocked=true`), BUG-051 (`ArtistSuggestions` race, no per-request cancellation), BUG-052 (empty artist on edit, compound). Evidence in `DevCycleCraft/autocomplete-component/changes/2026-07-19-dx-autocompleteedit-replacement/task-log.md § T7 outcome`.
- **2026-07-21 — Consolidation decision** (Helder): fold the fixes + inline-create into one sequenced worktree (same handlers, single-writer).
- **2026-07-21 — spec-reviewer PASS** on the consolidated spec (doc-hygiene polish folded in).
- **2026-07-21 — SPEC APPROVED by Helder.** Cleared for the planning phase (`writing-plans` → plan-reviewer → Helder plan approval → implementation in a worktree, T1 first, regression-test-first).

## Status
**Phase:** IMPLEMENTING. Worktree `../MyVocaList-inline-ac` on `feat/inline-artist-create` off `develop` (develop confirmed ancestor). Baseline **511/511 green** (plan.md's 501 was an estimate; expected counts offset +10).

## Implementation log

- **T5 — DX capability spike (Option A CONFIRMED via Context7, DevExpress MAUI 25.2.4).** `AutoCompleteEdit` supports a full custom `ItemTemplate`/`DataTemplate` for distinct row rendering; the app supplies suggestions via a custom async provider (`OnArtistItemsRequested`) whose returned items are the authoritative drop-down content, and `AutoCompleteEditTextChangeReason.ItemSelected` fires on selecting any displayed row. Guardrail: the synthetic row's `Headline` (`Add "text" as a new artist`) contains the typed text as a substring, so it survives any residual built-in Contains-filtering. → T7/T8 proceed with the synthetic ➕ row (Option A); Option B not needed. REQ-ACREATE-11 satisfied via Option A.
