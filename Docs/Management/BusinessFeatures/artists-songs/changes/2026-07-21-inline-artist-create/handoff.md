# Handoff — Song artist field (INLINE-AC): T10 re-run #2 outcome + remaining fixes

**For:** the next (fresh) session. Read this first (Rule 7 session start). Supersedes the 2026-07-21 planning handoff.

## Current state (as of 2026-07-23)
- Worktree `C:\Users\helde\source\repos\MyVocaList-inline-ac`, branch `feat/inline-artist-create`, HEAD after the BUG-054…059 fix wave. **Committed, not pushed, not merged.** Unit suite **520/520 green**. Code review = **CONDITIONAL PASS, no blockers** (scope-extension `SongRepository.GetByIdAsync .Include(OriginalArtist)` judged SAFE).
- Fix-wave commits on the branch: `f34cadc` (BUG-056+055), `cb78f3e` (BUG-054b/057/058 XAML), `fd83d90` (BUG-054a+059 note), `b696fd4` (checkpoint doc).
- **Helder ran T10 on device twice.** Re-run #2 results below are authoritative.

## T10 re-run #2 (Helder, on device, 2026-07-23)
| Item | Result | Disposition |
|------|--------|-------------|
| a (retain text) | ✅ | REQ-ACREATE-03 holds |
| b (lock/select) | ⚠️ PARTIAL | lock + clear-icon work, but **BUG-060** (see below) |
| c (error text) | ❌ | **BUG-057 REOPENED** — Label reserves layout space (title shifts down) but message text stays invisible |
| e (stale/first-empty) | ✅ | BUG-056 fixed |
| j (edit hydration) | ✅ | BUG-055 fixed — but exhibits **BUG-061** on page load |
| i (catalog) | ❌ | **BUG-059 REOPENED** — catalog still empty; the BUG-055 "cascade" assumption was WRONG. Needs own trace |
| C1 (novel create) | ✅ | — |
| C2 (duplicate) | ✅ | BUG-058 fixed |

## New / reopened defects (provisional IDs — CONFIRM against the current BACKLOG highest before registering; SPEC-EVO migration may have renumbered)

### INLINE-AC scope (same worktree `../MyVocaList-inline-ac`, same `AutoCompleteEdit`)
- **BUG-060 (Major, NEW) — artist field locks permanently; user cannot change the artist.** After selecting an artist the field is read-only; tapping the clear (X) icon empties `ArtistSearchText` but the field **stays read-only** (`IsArtistLocked` never cleared) and on **blur it reloads the just-cleared artist** (restore-prior-selection branch of `OnArtistBlurredWithoutSelection`). Net effect: the field is stuck on the first artist ever selected. **Needs a design decision + likely a new AC** (e.g. REQ-ACREATE-15: "clearing/editing a locked artist unlocks the field for a new search; blur with an intentionally cleared field does not silently restore the prior artist"). This is the functional half the code-review flagged as a "gap" — it is now a confirmed blocker, not cosmetic. SDD: add the AC to `requirements.md` before coding.
- **BUG-057 (Major, REOPENED) — inline error message text invisible.** The dedicated `Label` (added `cb78f3e`, `SongFormPage.xaml:~80`, `Text={Binding ArtistErrorText}` / `IsVisible={Binding ArtistHasError}` / `StyleClass=Body.Small` / `TextColor={StaticResource Error}`) now takes vertical space (proof it is `IsVisible`) but shows no text. Investigate: is `ArtistErrorText` actually populated when `ArtistHasError` flips true? Is the `Error` color resolving to something invisible on this surface? Is the binding path/DataContext on that Label correct (it may sit in a template/section with a different BindingContext)? Trace both VM (does it set `ArtistErrorText`?) and XAML (binding context + color).
- **BUG-059 (Major, REOPENED) — artist catalog empty for a linked artist.** Nav chain (`ViewCatalogCommand`→`NavigateToCatalog`→`Songs?artistId=…`→`GetPagedCatalogForArtistAsync`) was declared correct in re-run #1, but the catalog is still empty even though edit-hydration (j) now shows the artist. Trace: (1) does saving/updating a song actually PERSIST the ArtistId/OriginalArtistId FK to the DB (spot-check `.claude/MyVocaList.db` via the sqlite MCP)? (2) does `GetPagedCatalogForArtistAsync` filter on the SAME FK column the song is written to? (3) are the test songs pre-existing rows saved before the fix (no FK) vs newly saved? This is NOT a guaranteed cascade — treat as an independent bug.
- **BUG-061 (Minor/Major UI, NEW) — selected suggestion row lingers in the dropdown.** After tapping an autocomplete item the entry fills correctly, but the dropdown row for the selected artist stays visible and only disappears when tapped again; also visible on the edit page's initial load. The suggestion list / dropdown is not dismissed-or-cleared after a selection. Same `AutoCompleteEdit` in `SongFormPage.xaml(.cs)` / `SongFormViewModel`.

### SEPARATE feature — Songs LIST page (NOT this worktree; governed component → `component-change-governance.md`)
- **BUG-062 (Minor, NEW) — Songs list line-selector checkbox in the wrong MD3 slot.** The line-selector (checkbox) sits in the **trailing** slot, but MD3 requires it in the **leading** slot when a trailing action button is present (Artists list already does this — parity target). Touches the shared list/`ListItem`/`CrudListView` component → four-gate governance (dedicated task, consumer map, per-consumer risk, Helder approval) before any edit. Do NOT bundle into INLINE-AC.
- **BUG-063 (Major, NEW) — Songs list trailing action button has no action wired.** The per-row trailing button on the Songs list does nothing (mirrors the BUG-015/019/028 class on Artists list). Register under Artists & Songs Catalog / Songs list.

## Remaining plan for INLINE-AC (fresh session, SAME worktree, strictly sequential — same files)
1. **BUG-060 first — needs Helder's design decision** on the unlock behavior, then add the AC to `requirements.md`, then implement (VM: clear `IsArtistLocked`/`SelectedArtistId`/`SelectedArtistName` on clear-icon/edit; fix the blur restore branch so an intentional clear is not overwritten). Regression test at the VM seam (Red→Green).
2. **BUG-057** — fix error-text visibility. Regression: hard to unit-test the render; verify `ArtistErrorText` is set at the VM (that half IS testable) + on-device.
3. **BUG-059** — trace persistence + catalog query; DB spot-check. Fix where the FK/query actually breaks. Regression at the repo/service seam if the bug is there.
4. **BUG-061** — dismiss/clear suggestions after selection. On-device E2E.
5. Re-run T10 on device (Helder). On all-green → closeout (below).

## Closeout (only after a fully-green T10) — see `pending-backlog-closeout.md`
- ⚠️ **Do NOT hand-edit `BACKLOG.md`** — SPEC-EVO owns it via `BACKLOG:GENERATED` fences since 2026-07-22 (hand-edits silently overwritten by `regen`). Register/close bugs via `backlog_gen.py status <ID>` / item `README.md` frontmatter. Coordinate with the SPEC-EVO session (LEDGER row) if a row is fenced. Ownership ends when `feature/backlog-migration` merges.
- Merge `feat/inline-artist-create` → develop (take develop's versions of stale feature-doc files on conflict), push (creds cached), remove worktree, unblock the Artists & Songs Catalog.

## Hard constraints (carry over)
- Orchestrator never reads/edits `.cs`/`.xaml` — delegate to Explore/Plan/implementor subagents. All code edits in the worktree on the task branch. Docs land on develop.
- DevExpress-first; official MD3 style keys only; no native dialogs; English only; incremental single-file XAML edits (edit → build → fix). Context7 version-pinned (DevExpress MAUI 25.2.4) for any DX API.
- Bug fixes regression-test-first where a seam exists; UI/DX-wiring defects → on-device manual E2E documented in the task-log. `git push` may need Helder (creds cached 2026-07-22 — verify, don't assume blocked).

## Open decisions still with Helder (do not block the fresh session's investigation)
1. **BUG-060 unlock behavior** — what exactly should tapping X (and re-editing) do? (proposed AC above)
2. **AC-label typo** — the BUG-056 test is tagged `[AC] REQ-ACREATE-13` but proves direct-return, not the latest-wins race; correct the comment or leave.
3. **Docs commit** — the LEDGER + `pending-backlog-closeout.md` + `.sln` changes are currently UNCOMMITTED on develop (Helder rejected the earlier commit). This handoff + the pending file are also uncommitted until then.
