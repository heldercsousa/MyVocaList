# Handoff — Song artist field: correctness fixes + inline create-new-artist

**For:** the next session (PLANNING phase). Read this first (Rule 7 session start).

## State (as of 2026-07-21)
- **Spec APPROVED by Helder.** spec-reviewer PASS. Files in this folder: `requirements.md` (REQ-ACREATE-01…14), `design.md`, `tasks.md` (T1–T10), `task-log.md`.
- No code written; no worktree created. All docs on `develop` (latest relevant commit `5f5d7b9`; LEDGER row `INLINE-AC`).
- BUG-050/051/052 registered in BACKLOG; BUG-027 will close when T10 passes on device.

## Next phase = PLANNING (do this, do NOT code)
1. Session-start reads: this handoff → `tasks.md` → `requirements.md` → `design.md` → `task-log.md`. Do not glob `Docs/**`.
2. Invoke `writing-plans` → produce `plan.md` in THIS folder (execution plan for T1–T10; strictly sequential, single worktree, single-writer on the shared handlers).
3. Dispatch a fresh **plan-reviewer** subagent; fold in its findings.
4. Present the plan to Helder for approval. **Stop there** — implementation is a separate session.
5. On plan approval: update BACKLOG `INLINE-AC` row 🗺️ Plan → 🟢 Ready, update LEDGER, commit docs to develop.

## Hard constraints (carry into both planning and coding)
- Orchestrator never reads `.cs`/`.xaml` — delegate code inspection to Explore/Plan subagents.
- All code edits happen in a **git worktree** on a task branch off `develop` (verify `develop` is ancestor). Docs commit to develop.
- Bug fixes are **regression-test-first** (Red→Green); BUG-050 Critical = mandatory. Business logic in Services only; DevExpress-first; no native dialogs; English-only; incremental single-file XAML edits.
- `git push` blocks in Claude shells (wincred) — subagents commit only; Helder pushes.

## Key facts the planner needs (from code traces — no need to re-explore)
- **BUG-050 fix:** add `IsArtistLocked = true;` in `SongFormViewModel.SelectArtist` (~L283–292); mirrors `ResolveAndLockArtistAsync` (~L411).
- **BUG-051 fix:** per-request generation/cancellation in `SongFormViewModel.SearchArtistsAsync` (~L274–281); only assign `ArtistSuggestions` if latest. (`e.Text` is already correct — not the bug.)
- **Retain-text:** `OnArtistBlurredWithoutSelection` no-lock branch keeps `ArtistSearchText` instead of clearing.
- **Inline create:** reuses existing `IArtistService.CreateArtistAsync` (exact-dup guard built in) + `ValidateNameInput`; `SongFormViewModel` already injects `_artistService`. Sentinel `IsCreateNew` flag on `AutocompleteSuggestion`.
- **DX spike (T5):** confirm via Context7 (DevExpress 25.2.4) that `AutoCompleteEdit` allows selecting a synthetic non-matched row; else Option-B button fallback.
