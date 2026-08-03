# Handoff — INLINE-AC (Song artist field: correctness fixes + inline create-new-artist)

> ## ⛔ CURRENT STOP POINT — T10 re-run #5 FAILED (2026-08-02). Branch must NOT be merged.
>
> **Build under test:** `e13a495` on `feat/inline-artist-create`, worktree
> `C:\Users\helde\source\repos\MyVocaList-inline-ac` (clean, pushed, 535/535 unit tests green).
> **Read `task-log.md § T10 re-run #5` first — it carries the verbatim exception, the per-item table
> and the evidence.** Do not re-derive what is already recorded there.
>
> ### What re-run #5 settled
>
> | Verdict | Items |
> |---------|-------|
> | ✅ **Genuinely fixed — do not re-open** | BUG-065(b) 1st/2nd keystroke · BUG-066 inline ➕ create (Add **and** Edit mode) · BUG-060 clear-unlock · edit-mode hydration · retain-typed-text |
> | ❌ **Open — three new IDs** | **BUG-068** (Critical) · **BUG-069** (Major) · **BUG-070** (Minor/UX) |
>
> The BUG-067 fix (missing `artistId` parameter) and **REQ-ACREATE-16** were necessary and are
> **correct — they stay**. They were simply not sufficient: the write now reaches the repository, and
> the repository throws.
>
> ### ⚠️ Helder's standing instruction (2026-08-02): SPLIT THE WORK ACROSS SESSIONS
>
> These defects have been re-fixed repeatedly without ever fully closing. One session doing all three
> is what produced the last three failed re-runs. **Each session below takes ONE session, does only
> its own scope, and ends.** Do not chain them. Do not "just also fix" an adjacent defect — the
> accuracy loss from a wide session is the documented failure mode here.
>
> ---
>
> ## SESSION A — BUG-068 (Critical) · EF Core identity conflict on edit-mode save
>
> **Scope: this defect only. Nothing in the autocomplete/dropdown layer.**
>
> Symptom 1: tap a suggestion once → Save → UI says **success**, nothing persists (silent data loss).
> Symptom 2: tap the re-shown suggestion (BUG-069) → Save → *"Failed to save song"* +
> `InvalidOperationException: The instance of entity type 'Song' cannot be tracked because another
> instance with the same key value for {'Id'} is already being tracked` at
> `SongRepository.UpdateAsync` (`Infra\Repository\SongRepository.cs:135`) ← `SongService.UpdateSongAsync`
> (`Services\SongService.cs:156`) ← `SongFormViewModel.ExecuteEditSaveAsync` (`:605`).
>
> **Do this in order:**
> 1. `superpowers:systematic-debugging`. The hypothesis in the task-log (service loads a tracked
>    `Song`, repository then calls `DbSet.Update` on a second instance with the same key) is a
>    **hypothesis, not a finding** — prove or refute it against the real code first.
> 2. **Write the Red test at the repository/integration seam against REAL SQLite** (temp file), per
>    `testing.md § Project anti-patterns`: *repository tests use real SQLite, never the in-memory
>    provider; never mock the DbContext.* A mocked `ISongRepository` test **cannot** reproduce this
>    and must not be accepted as the regression test — that mocking gap is exactly why 535/535 was
>    green while every save failed on device. See it FAIL, record the output, then fix.
> 3. Fix in the **Services/Infra** layer (business logic in Services — unamendable). Evaluate the
>    options recorded in the task-log; do not pre-commit to one.
> 4. Silent-success path 1 is its own defect: a failed save must never report success. Verify the
>    result-tuple handling in `ExecuteEditSaveAsync` surfaces the failure.
>
> **Session A ends when:** Red→Green recorded, full suite green, committed, pushed, task-log entry
> written. **No device pass is requested of Helder for Session A alone** — it is verified by the new
> integration test. Do NOT proceed to BUG-069.
>
> ---
>
> ## SESSION B — BUG-069 (Major) · dropdown re-opens after a selection
>
> **Scope: this defect only. Do not touch the save/persistence path (Session A owns it).**
>
> After tapping a suggestion the list hides, then **immediately re-opens** listing every prefix match
> (artists *Helder* / *Helder Sousa* / *Helder Carvalho de Sousa* → picking *Helder* re-shows all
> three). Also fires on **edit-mode page load**. The re-shown row is tappable and tapping it is the
> exact trigger for BUG-068 path 2.
>
> **This is NOT the old BUG-061.** BUG-061 was a *lingering* row (never dismissed). This is
> *dismiss-then-re-open*: `IsArtistDropDownOpen = false` **does** run, and something re-opens the
> popup afterwards. Establish **what re-opens it** from decompiled DevExpress IL before editing —
> `task-log.md § IL evidence (2026-07-30)` confirms `IsDropDownOpen` is the supported lever but does
> **not** cover the re-open path. Decompiling the shipped DLL is the route that has worked twice; the
> DevExpress demo-app MCP returns empty (treat as UNAVAILABLE per the MCP Availability Gate) and
> Context7 lacks these `AutoCompleteEdit` members. **Never guess an API name.**
>
> **Session B ends when:** fix committed + pushed, task-log entry written, and the limits of unit
> verification stated explicitly (popup behaviour is invisible to VM tests).
>
> ---
>
> ## SESSION C — BUG-070 (Minor/UX) · misleading validation copy — SPEC WORK, needs Helder first
>
> On a novel artist name the field shows **"Search and select an artist from the list"**, which reads
> as "you cannot create a new artist here" — the opposite of the shipped ➕ behaviour (B1–B3 pass).
> The Artist field also takes the error border while the ➕ path is available.
>
> **Blocked on Helder:** the replacement copy and the trigger condition. This likely amends a
> REQ-ACREATE acceptance criterion, so the **spec changes before the code** (SDD invariant). Do not
> invent the wording.
>
> ---
>
> ## Only after A, B and C are all green: T10 re-run #6 (Helder, on device)
>
> Re-verify in one pass: BUG-068 (edit-mode artist change persists, and a failure is never reported as
> success) · BUG-069 (no re-open after selection, none on edit-load) · BUG-070 (copy) · and regression
> re-checks of BUG-060 / 061 / 064 / 065 / 066. Then closeout per `pending-backlog-closeout.md`,
> merge, push, remove the worktree, unblock the Artists & Songs Catalog.
>
> ## Carry-over constraints (every session)
>
> - Orchestrator never reads/edits `.cs`/`.xaml` — delegate to Explore/Plan/implementor subagents.
>   All code edits in the worktree on the task branch; **docs land on develop**.
> - Android build is blocked locally by `XARLP7024` (AV/EDR corruption on AndroidX AAR extract — NOT
>   code). Build/test on the **net10.0** TFM; Helder builds Android locally.
> - **A green unit suite proves nothing about the device here.** Re-runs #1–#5 all passed unit tests
>   and failed on device. Say what is unverified rather than implying it is fixed.
> - BUG-068/069/070 are **task-log-tracked, NOT `backlog_gen.py register`-ed** — the allocator would
>   reissue BUG-053 (see `spec-evolution-versioning/POST-MIGRATION-FOLLOWUPS.md` FUP-4). Same caveat
>   as BUG-065/066/067.
> - DevExpress-first · official MD3 style keys · no native dialogs · English only · incremental
>   single-file XAML edits (edit → build → fix).


---

<details>
<summary>Earlier handoff history (2026-07-30 stop point, re-runs #2–#4, prior plans)</summary>

**For:** the next (fresh) session. Read this first (Rule 7 session start). Supersedes the 2026-07-21 planning handoff.

## Current state (as of 2026-07-25, refreshed after T10 re-run #3)
- Worktree `C:\Users\helde\source\repos\MyVocaList-inline-ac`, branch `feat/inline-artist-create`, **HEAD `b8f7d2c` — PUSHED to `origin/feat/inline-artist-create`.** Not merged. Unit suite **530/530 green**. Verifier on the re-fix set = **CONDITIONAL PASS, no blockers** (all residuals closed).
- **T10 re-run #3 (Helder, on device, 2026-07-25):** item 1 (change-artist/unlock, BUG-060) **✅ PASSED — closed**. Items 2 & 3 still failed → re-fixed this session (below).
- **Re-fix wave (BUG-061 completion + BUG-064), all pushed:**
  - **BUG-064** (`283866f`) — duplicate artist error message removed; kept the DX `AutoCompleteEdit` native `HasError`/`ErrorText`, dropped the redundant `Label`.
  - **BUG-061** — root cause per Helder: programmatic `ArtistSearchText` assignment re-triggered the autocomplete search, re-opening the dropdown. Fix = `_suppressNextArtistSearch` one-shot guard set before EVERY programmatic assignment, consumed in `OnArtistItemsRequested`. Landed across `7c594e2` (initial: LockArtist/init/hydration) → `3e38066` (2 missed paths: `ResolveAndLockArtistAsync` ×2, `OnArtistBlurredWithoutSelection`) → `b8f7d2c` (final residual: `ClearArtist`). **All 7 programmatic sites now guarded.** +7 regression tests total.
  - Earlier this session: `efe65c6` = manual compiler-warning fix in `StringExtensions.cs` (no behavior change, Helder-confirmed safe).
- **BUG-059 CANCELLED** (Helder 2026-07-23, works-as-designed): catalog join table is deliberately picker-only; empty catalog after Song-form save is by design. Reframed as a NEW enhancement (auto-link artist-OWNED songs to author's catalog) — seed `BusinessFeatures/artists-songs/ENHANCEMENT-artist-owned-song-catalog-autolink.md`; register in BACKLOG when SPEC-EVO migration settles. **T10 item i is DROPPED.**

> ## 🔴 SUPERSEDED 2026-07-30 — T10 re-run #4 FAILED; a fix wave is required
>
> Helder ran re-run #4 on device against `b8f7d2c` on **2026-07-30**. **BUG-061's core behavior passed** (select → fill → hide rows → lock → clear → re-select overrides). **Three new defects were found** — full verbatim report, per-item table, root-cause hypotheses and fix order in `task-log.md § T10 re-run #4 (2026-07-30)`:
> - **BUG-067 (Critical)** — editing a song's artist to a different existing artist **is not persisted**; the song keeps the original artist. Silent loss of a user edit. **Failing regression test MANDATORY first** (`bug-tracking.md`).
> - **BUG-066 (Major)** — **inline "create new artist" is unreachable** in both add and edit mode: a non-existent name is rejected with *"search and select an artist from the list"*. This is the headline capability of this change and C1 passed in re-run #2, so it is a **regression** from a later fix wave (suspects: BUG-054a sentinel suppression, `_suppressNextArtistSearch`).
> - **BUG-065 (Major)** — spurious **"Not found"** row: (a) after any programmatic text assignment (selection, edit-page load, re-selection), clearing only on blur; (b) at 1 typed character even when matches exist, resolving at 2 chars. Residual of BUG-061 — keep BUG-061 open until both re-verify.
>
> ### ✅ SUPERSEDED — stop point of 2026-07-30 (kept: its IL evidence is still authoritative)
>
> *Its BUG-065/066 work is DONE and verified on device in re-run #5. Read it only for the IL findings.*
>
> ### ⛔ (historical) STOP POINT — session ended 2026-07-30 by Helder: "record progress until here, I will resume another session"
>
> **Nothing was coded. No file in the worktree was touched.** Worktree `C:\Users\helde\source\repos\myvocalist-inline-ac` is clean at `b8f7d2c` on `feat/inline-artist-create`. All work this session was diagnosis + tracking, committed to `develop`.
>
> **A root cause was found and proven from IL — read `task-log.md § IL evidence (2026-07-30)` before doing anything else.** Summary:
> `AsyncItemsSourceProvider.OnEditorTextChanged` begins `if (e.Reason != AutoCompleteEditTextChangeReason.UserInput) return;` — so **`ItemsRequested` never fires for a programmatic text change**; DevExpress already suppresses those natively. Therefore `_suppressNextArtistSearch` (added as BUG-061's fix, set at 7 sites) is **never consumed by the assignment it was set for**: it stays set and is consumed by the user's **next real keystroke**, which is then early-returned with an empty array and skips the ➕-append block (`SongFormPage.xaml.cs:51-55` → `:57-80`).
> That one mechanism explains **BUG-065(b)** (1st char fails, 2nd works) and **BUG-066** (inline create unreachable) with no timing hypothesis.
>
> **Also settled by IL, do not re-investigate:** `CharacterCountThreshold` compares `>=` (default 1) and is correctly configured — NOT the cause. "Not found" is DevExpress's own localizer string (`EditorStringId.ComboBox_NotFound`) with **no** bindable override. **`IsDropDownOpen`** (`ItemsEditBase`, two-way `BindableProperty`) is the supported way to force the popup shut — the provider uses it itself. Leaving `RequestAsync`/`Request` unassigned **crashes** (null delegate invoked on a background task, awaited in `async void`) — never do that. `ItemsRequestEventArgs.CancellationToken` is get-only; a handler cannot self-cancel.
> **Unresolved and unprovable from IL:** BUG-065(a)'s exact native trigger lives in the Android-native widget, outside decompilable IL. `IsDropDownOpen = false` is the evidenced remedy but needs on-device confirmation.
>
> #### 🔷 OPEN DECISION FOR HELDER — blocks the fix wave (architectural; an agent must not take it)
> The coherent fix is to **delete `_suppressNextArtistSearch` at all 7 sites** and close the dropdown via `IsDropDownOpen = false` instead. But that mechanism *is* BUG-061's fix and its regression tests assert on it (`SongFormViewModelTests.cs:476-535`). Three options were put to Helder and **none was chosen** — the session ended first:
> 1. **(recommended)** Delete the guard, use `IsDropDownOpen`. One coherent change covering 065(a)/065(b)/066; BUG-061's tests get rewritten against the real mechanism; re-verify all 7 sites + BUG-064 in the same pass.
> 2. Keep the guard, patch narrowly so it cannot leak into the next keystroke. Smaller diff, BUG-061's tests survive — but keeps a mechanism the IL shows is inert.
> 3. Re-derive what actually caused BUG-061 first, since the flag cannot have been the cure — slower, but guards against removing something that masks a third, unidentified defect.
>
> The 7 guard sites (all must be handled together, this class of gap has already regressed twice): `LockArtist` `SongFormViewModel.cs:333` · `InitializeArtistField` `:422` · `LoadSongForEditAsync` `:490` · `OnArtistBlurredWithoutSelection` restore `:385` · `ClearArtist` `:404` · `ResolveAndLockArtistAsync` ×2 `:539`, `:551`.
>
> **BUG-067 (Critical — edit-mode artist change not saved) was NOT investigated this session.** Helder chose to start with BUG-065; the trace never reached 067. It remains unanalysed and is still the highest-severity open item.
>
> **Resume in this order:** (1) take the decision above; (2) fix per that decision in the worktree; (3) BUG-067 with a failing regression test first (`bug-tracking.md` — Critical); (4) T10 re-run #5 on device re-verifying BUG-061 + BUG-064 alongside 065/066.
>
> **Do not trust a green unit suite here.** The popup behavior, the threshold boundary and the debounce are all invisible to VM tests — that is exactly why re-runs #1–#4 passed unit tests and failed on device.
>
> **Tooling note:** the DevExpress demo-app MCP returned empty for every query including a bare `AutoCompleteEdit` — treated as **unavailable** per `CLAUDE.md § MCP Availability Gate`, not as an empty result. Context7 lacks these members for `DevExpress.Maui.Editors.AutoCompleteEdit` (it documents only the DataForm/DataGrid siblings). Decompiling the shipped DLL was the route that worked; use it again rather than guessing an API name.

<details>
<summary>Fix-wave plan as written before the IL evidence (superseded ordering — kept for context)</summary>

> **Next session:** fix wave in the SAME worktree `C:\Users\helde\source\repos\myvocalist-inline-ac` on `feat/inline-artist-create`, strictly sequential (all three converge on `SongFormPage.xaml(.cs)` + `SongFormViewModel.cs`). Order: **BUG-067 → BUG-066 → BUG-065(b) → BUG-065(a)**. Trace BUG-065 and BUG-066 together before editing — both likely live in `OnArtistItemsRequested`. Then **T10 re-run #5** on device, re-verifying BUG-061 and BUG-064 in the same pass. Closeout, merge and the catalog unblock all stay parked.
>
</details>

> These three are **not** registered via `backlog_gen.py` — `next_bug_id()` reports BUG-053 because BUG-053…064 have no folders, so it would reissue used ids. Tracked in `task-log.md`; follow-up logged in `spec-evolution-versioning/POST-MIGRATION-FOLLOWUPS.md`.

<details>
<summary>Previous gate (re-run #4 instructions) — kept for history</summary>

> ## ⚠️ HELDER'S DUTIES — on-device T10 re-run #4 (the ONLY gate left before closeout)
> No code work remains. Closeout is blocked solely on **your on-device T10 re-run #4** (Android built locally — local Android build blocked by `XARLP7024` AV/EDR corruption, NOT code). Item 1 already passed re-run #3; re-verify ONLY the two re-fixed items:
> 2. **Inline error text — no duplicate (BUG-064):** trigger the artist validation error → exactly ONE message shows (the redundant bottom one is gone).
> 3. **Lingering dropdown row (BUG-061, your diagnosis implemented):** after tapping a suggestion the dropdown does NOT re-open on the just-selected name; also confirm it stays closed on **edit-page load**, after returning from the **song picker** (exact-match and no-match), and after tapping the **clear (X)** icon — all programmatic-text paths are now guarded, so check each.
> 4. (Optional) sanity re-check a / e / j / C1 / C2. **Skip item i (catalog) — dropped.**
>
> **On all-green → tell the next session "T10 re-run #4 passed"** and it runs the closeout below (merge → develop, push, remove worktree, unblock catalog).
>
> Two minor decisions carried, non-blocking: (a) AC-label typo — you OK'd folding a one-line comment fix into the merge commit; (b) the auto-link enhancement gets a BACKLOG row once SPEC-EVO settles.

</details>

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
2. **AC-label typo (DEFERRED to closeout cleanup — Helder OK'd "best option" 2026-07-23):** the test `SearchArtistsCoreAsync_ReturnsCurrentQueryResultsDirectly` is tagged `[AC] REQ-ACREATE-13` but proves direct-return (BUG-056), not the latest-wins race (REQ-ACREATE-13 = BUG-051). Fold a one-line comment correction into the closeout/merge commit — not worth a subagent or an ITF edit on its own.
3. **Docs commit** — the LEDGER + `pending-backlog-closeout.md` + `.sln` changes are currently UNCOMMITTED on develop (Helder rejected the earlier commit). This handoff + the pending file are also uncommitted until then.

</details>
