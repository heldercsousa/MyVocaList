# Work Ledger — branch/status tracker (lives on develop)

> **Purpose:** single source of truth, always on `develop`, for WHERE every in-flight task/bug lives (branch, worktree) and its current phase/status. This is what makes resuming development, debugging, and merges into develop possible after any interruption. Maintained via `/sln-ledger` and at every phase transition (dispatch, phase change, merge, close). Rules: `workflow.md § Rule 2` (docs land on develop).
>
> **Row lifecycle:** add the row when work is dispatched; update `Phase/Step`, `Status`, `Last commit` at each transition; on merge set Status `merged` and move the row to Completed. Closed rows older than the current month move to `LEDGER-archive.md` (verbatim).
>
> **Row-length rule `[HARD RULE]`:** a row is ONE short status line — what, where it stands, next action, pointer to the item folder — **max 450 characters** (enforced by `.claude/scripts/ledger-row-guard.py`). Detail (history, rulings, findings) goes in the item's `task-log.md` or `README.md`, never in the ledger.

## Active

| ID | Feature / Bug | Phase / Step | Branch | Worktree | Status | Last commit | Next action |
|----|---------------|--------------|--------|----------|--------|-------------|-------------|
| SFT | Song form title-first — one search over local + music APIs | 5.5/5.6/6.3 + 7.x pending; merged through 6.2, BUG-084/085 (develop 926/926) | develop (docs) | — | 🟡 in progress | 4d9faa54 | Wait for Helder's go to move forms onto `AutocompleteDXField` (ACF open). BUG-084/085 closed (device retest passed 2026-11-10). Detail: `BusinessFeatures/artists-songs/changes/2026-09-24-song-form-title-first-…/task-log.md` § Ledger detail |
| AC-FIELD | Shared autocomplete field: pick-lock via X, centralised behaviour (+ BUG-070 copy) | T2–T6 merged (Verifier PASS); only T7 left | develop | — | blocked: Helder T7 on device | 91f87884 | Helder runs the T7 on-device gate on the Person form; decide whether to fix `PersonFormPage` `SafeAreaEdges="All"`→`"Container"`. Detail: `DevCycleCraft/autocomplete-component/changes/2026-09-24-shared-…/task-log.md` § Ledger detail |
| ITF | Inline Trivial Fix lane — bounded orchestrator inline-edit exception | merged; 38/38 green; one gate left | develop | — | merged | a4c198a | Helder observes the FIRST live ITF fix end-to-end; decide on 4 misbased `agent-*` worktrees. Detail: `DevCycleCraft/inline-trivial-fix/handoff.md` + task-log § Ledger detail |
| SC-ENH | Session Continuity enhancements (Phase 8, Tasks 11–15) | complete, verifier PASS | develop (tooling + docs) | — | to review | 68e1852 | Helder runs the two-terminal live demo; optionally directs the 4 leftover agent worktrees. Detail: `DevCycleCraft/session-continuity-leasing/task-log.md` § Ledger detail |
| CATALOG-PROP | Song writes propagate to the artist catalog | 📋 spec; D1–D4 closed, `requirements.md`/`design.md` not yet written | — | — | deferred (spec after SFT) | e91947f6 | Write spec from D1–D4, then spec-reviewer + Helder gate. Do not dispatch while an `sft-*` branch owns `SongService`/Migrations. Detail: `BusinessFeatures/artists-songs/changes/2026-08-04-song-writes-…/README.md` § Ledger detail |

## Completed (this month)

Closed rows from earlier months: `LEDGER-archive.md`. SFT merge detail: the SFT `task-log.md`; verbose pre-2026-11-10 text of these rows is in `LEDGER-archive.md`.

| ID | Feature / Bug | Branch | Merged into develop | Commit |
|----|---------------|--------|---------------------|--------|
| BID | Build identity — MinVer version + About build details (app-versioning) | develop | 2026-11-10 closed; G3 PASS on S23 (`v0.2.0-alpha.0.4`); follow-ups: release-tag discipline, changelog retirement | 4a839cbe |
| SFT-CLEAN | cleanup: dead repository member, shared note text, stale comments | `task/sft-cleanup-dead-member-shared-notes` (deleted) | 2026-11-10 merge; Verifier PASS; develop 926/926; one unreproduced flake, see task-log | 357a6813 |
| BUG-085 | song save misses near-duplicate title when the typed title is longer than the… | `task/bug-085-song-fuzzy-trigram` | 2026-11-09 merge; Verifier CONDITIONAL PASS; develop 921/921; device retest passed 2026-11-10 (Helder), closed | 55ca9b88 |
| BUG-084 | Song form sheets: scrim/Back do not dismiss, Cancel unreachable with many rows | `task/bug-084-song-form-sheets` | 2026-11-09 merge; Verifier CONDITIONAL PASS; develop 911/911; device retest passed 2026-11-10 (Helder), closed | aa1af4da |
| SFT-6.2 | `ArtistFormViewModel`: Name option source, local pick opens the artist, Deezer… | `task/sft-6.2-artist-form-vm` | 2026-11-06 merge; Verifier CONDITIONAL PASS; develop 902/902 | e8eff07e |
| SFT-5.4 | VM: edit-mode title | `task/sft-5.4-vm-edit-mode` | 2026-11-06 merge; Verifier CONDITIONAL PASS; develop 884/884 | cb3ebd8d |
| SFT-5.3b | delete forwarding `CreateSongWithUrlsAsync(int, …)` overload | `task/sft-5.3b-delete-forwarding-overload` | 2026-11-06 merge; Verifier PASS; develop 874/874 | 74cf55cc |
| SFT-5.3 | VM: Save maps service outcomes to sheets | `task/sft-5.3-save-outcomes` | 2026-11-05 merge; Verifier CONDITIONAL PASS; develop 874/874 | e1721279 |
| SFT-3.10 | `SongService.SaveFromFormAsync`: every Save decision in the Service | `task/sft-3.10-save-from-form` | 2026-11-05 merge; Verifier CONDITIONAL PASS; develop 848/848 | e6922641 |
| SFT-5.2 | VM: title search, picks, online-search notes | `task/sft-5.2-vm-title-search` | 2026-11-05 merge; Verifier CONDITIONAL PASS; develop 809/809 | ffba3449 |
| SFT-6.1 | `ArtistSuggestionService` local + remote name search | `task/sft-6.1-artist-suggestions` | 2026-11-05 merge; Verifier CONDITIONAL PASS; develop 773/773 | 75fa4dda |
| SFT-4.3 | register `SongSuggestionService` | `task/sft-4.3-register-song-suggestions` (deleted) | 2026-11-04 merge; Verifier PASS; develop 769/769 | 4d9faa54 |
| SFT-4.2 | `SongSuggestionService` local + remote title search | `task/sft-4.2-song-suggestion-search` (deleted) | 2026-11-04 merge; Verifier CONDITIONAL PASS; develop 768/768 | b100ceb7 |
| SFT-3.9c | artist fuzzy tier reads the FTS pool | `task/sft-3.9c-artist-resolution-fts` (deleted) | 2026-11-04 merge; Verifier CONDITIONAL PASS; develop 748/748 | 8163505b |
| SFT-3.9b | FTS5 `ArtistSearch` name candidate query | `task/sft-3.9b-artist-search` (deleted) | 2026-11-04 merge; Verifier PASS; develop 751/751 | 02fc76b7 |
| SFT-4.1b | delete `MusicBrainzProvider` | `task/sft-4.1b-drop-musicbrainz` (deleted) | 2026-11-04 merge; Verifier PASS; develop 737/737 | 1aee99f6 |
| SFT-4.1a | Deezer plain query, failures throw, `title_short` + normalised `title_version` | `task/sft-4.1a-deezer-plain-query` (deleted) | 2026-11-04 merge; Verifier CONDITIONAL PASS; develop 737/737 | bc81f9e4 |
| SFT-3.9 | FTS5 trigram title search + dedup lookups | `task/sft-3.9-title-search` (deleted) | 2026-11-04 merge; Verifier CONDITIONAL PASS; develop 719/719 | f3770a83 |
| SFT-4.1c | `INetworkStatus` + `MauiNetworkStatus` + `RemoteSearchStatus` | `task/sft-4.1c-network-status` (deleted) | 2026-11-04 merge; Verifier PASS; develop 693/693 | a190b2a0 |
| SFT-3.8 | M3: FTS5 `SongSearch` + `ArtistSearch`, 7 triggers, RETURNING off | `task/sft-3.8-m3-fts5` (deleted) | 2026-11-04 merge; Verifier CONDITIONAL PASS; develop 688/688 | eca9f708 |
| SFT-3.7 | M2: unique `(ExternalProvider, ExternalId)` with lowest-Id dedupe | `task/sft-3.7-m2-external-identity-unique` (deleted) | 2026-11-04 merge; Verifier PASS; develop 678/678 | 4e81916b |
| SFT-3.6b | M1: drop `HasManualEdits` columns | `task/sft-3.6b-m1-drop-hasmanualedits` (deleted) | 2026-11-04 merge; Verifier PASS; develop 674/674 | a6a34647 |
| SFT-3.6a | `ArtistService` stops writing `HasManualEdits` | `task/sft-3.6a-artist-writer` (deleted) | 2026-11-04 merge; Verifier PASS; develop 671/671 | 29de1054 |
| SFT-3.5a | drop `HasManualEdits` from the song read model | `task/sft-3.5a-song-readmodel` (deleted) | 2026-11-04 merge; Verifier CONDITIONAL PASS; develop 671/671 | 39dbc58d |
| SFT-3.5b | drop `HasManualEdits` from the artist read models | `task/sft-3.5b-artist-readmodel` (deleted) | 2026-11-04 merge; Verifier CONDITIONAL PASS; develop 671/671 | 408929e7 |
| SFT-3.2c | delete obsolete `UpdateSongAsync(hasManualEdits)` overload | `task/sft-3.2c-delete-obsolete-update` (deleted) | 2026-11-04 merge; Verifier CONDITIONAL PASS; develop 671/671 | a9094bf5 |
| SFT-3.2b | `UpdateSongAsync` without `hasManualEdits` | `task/sft-3.2b-remove-hasmanualedits` (deleted) | 2026-11-04 merge; Verifier PASS | 712affbd |
| SFT-3.3b | shrink `SongResolution` | `task/sft-3.3b-shrink-resolution` (deleted) | 2026-11-04 merge; Verifier CONDITIONAL PASS; develop 671/671 | 5191b255 |
| SFT-3.4 | `ArtistResolutionService`: remove `CommitAsync` + `ResolutionChoice` | `task/sft-3.4-artist-resolution` (deleted) | 2026-11-04 merge; Verifier PASS | 23428828 |
| SFT-3.2a | `UpdateSongAsync`: version-aware uniqueness + `newArtistName` | `task/sft-3.2a-update-version-uniqueness` (deleted) | 2026-11-04 merge; Verifier PASS | eab8b6b2 |
| SFT-3.3 | simplify `SongResolutionService` | `task/sft-3.3-song-resolution` (deleted) | 2026-11-04 merge; Verifier PASS | c59c61d1 |
| BUG-080 | venuesSeedForTest ships ~504 fake venues | `fix/bug-080-venue-seed-debug-only` (deleted) | 2026-11-04 merge; Verifier PASS | 8f5d716c |
| SFT-3.1 | `SongService` creates a typed artist inside the song's unit of work | `task/sft-3.1-new-artist-uow` (deleted) | 2026-11-04 merge; Verifier CONDITIONAL PASS; develop 677/677 | 4748326b |
| SFT-5.1 | VM: resolution sheet → Open existing | `task/sft-5.1-open-existing` (deleted) | 2026-11-04 merge; Verifier PASS; develop 668/668 | f687e72d |
| SFT-2.2 | `ISimilarityScorer.RankScore` + `RemoteSearchTimeoutMilliseconds` | `task/sft-2.2-rankscore` (deleted) | 2026-11-04 merge; Verifier CONDITIONAL PASS; develop 668/668 | a6660eed |
| ACF-T6 | Person form Full Name onto `AutocompleteDXField` | `task/acf-t6-person-form` (deleted) | 2026-11-04 merge; Verifier PASS | 91f87884 |
| ACF-T1 | lock-mechanism spike | `spike/acf-t1-lock-mechanism` (deleted 2026-11-04) | Not merged by design — verdict **B** recorded 2026-11-03 in  | 73184803 |
| SFT-G5 | FTS5 trigram probe spike | `spike/sft-g5-fts5` (deleted 2026-11-04) | Not merged by design — **G5: PASS** recorded 2026-11-04 in t | 81e838a8 |
| ACF-T4 | `AutocompleteDXField` component | `task/acf-t4-component` | 2026-11-03 merge; Verifier PASS | 0d74b4a6 |
| SFT-2.1 | song lookup queries | `task/sft-2.1-song-lookups` | 2026-11-03 merge | e5311d5c |
| SFT-1.1 | remove Song-picker entry from Song form | `task/sft-1.1-song-picker-entry` | 2026-11-03 merge | 0d34c328 |
| SFT-1.2 | remove Artist-picker entry from Artist form | `task/sft-1.2-artist-picker-entry` | 2026-11-03 merge | 04b8fd4d |
| ACF-T2 | `SearchConstants.AutocompleteDebounceMilliseconds` | `task/acf-t2-debounce-constant` (deleted) | merge `78b2b735`; Verifier PASS.; Verifier PASS | 78b2b735 |
| ACF-T3 | `IMenuOptionSource + AutocompleteDXFieldLogic` | `task/acf-t3-field-logic` (deleted) | merge `83065e07`; Verifier PASS.; Verifier PASS | 83065e07 |
| ACF-T5 | `PersonFormViewModel.NameOptionSource + trimmed SearchPersonsCoreAsync` | `task/acf-t5-person-option-source` (deleted) | merge `7d228c41`; Verifier PASS.; Verifier PASS | 7d228c41 |

---

**Status vocabulary:** `dispatched` · `in progress` · `build failure` · `test failure` · `to review` · `blocked: <reason>` · `ready to merge` · `merged` · `abandoned: <reason>`

**Column notes:**
- **Branch** — the branch holding the changes (empty = work happened directly on develop, docs-only).
- **Worktree** — `.worktrees/<name>` path, or `—` if the worktree was already removed (branch still holds the commits).
- **Last commit** — short hash on the task branch, so a resuming session can `git log <hash>..<branch>` instantly.
- **Next action** — one line: what the next session should do (e.g. "fix failing test X", "await Helder review", "merge after wave 2").

**Resume chain (no globbing):** this ledger row → the feature's `task-log.md` `### Checkpoint` block (live step state, pinged on a ~10-min heartbeat) → read ONLY its Context manifest files. See `session-ops.md § Checkpoint Ping & Context Manifest`.
