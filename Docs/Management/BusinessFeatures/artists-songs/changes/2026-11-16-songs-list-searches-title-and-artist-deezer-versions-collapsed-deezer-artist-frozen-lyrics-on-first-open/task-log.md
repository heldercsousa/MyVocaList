# Task log: Songs list search, one Deezer row per song, fixed Deezer artist, lyrics on first open

Spec: `requirements.md`, `design.md`. Plan: `plan.md`. Tasks: `tasks.md`.
Branch `feature/songs-list-deezer`, worktree `.claude/worktrees/songs-list-deezer`.

### Checkpoint

- **Updated:** 2026-11-16, after T5.
- **Phase:** 3 — Services. T7 and T8a dispatched to one implementor (sequential).
- **Done:** T0, T1, T2, T4, T5, T6. Task branch at 0c3abbb3, pushed. Full suite 1,729 passed, 0 failed.
- **Next:** T3 (Builder part, then the main agent generates the migration, plan § 9 item 6), T8b, T9a, T9b, T10, T11a, T11b, T12, T13.
- **Context manifest (≤ 8 files):** `plan.md`, `tasks.md`, `requirements.md`, `design.md`, this file.

## T0 — Spec and plan (2026-11-16) — Done

- Discussion with Helder on how Deezer data is kept. Id-only storage rejected; four certain pieces specified here; the Deezer section in the Songs list is a separate undecided trial; sending lyrics to LRCLIB is post-MVP.
- Helder's instruction of 2026-11-16: "You are authorized in advance to proceed with all next needed steps. Be autonomous." The spec and the plan are approved under it. The device check stays his.
- Spec review (fresh reviewer): pass with fixes; fixes applied (REQ-SLD-30, 31 added; decisions C14 to C19).
- Plan review (fresh reviewer): pass with fixes; fixes applied (`plan.md § 9`).
- Deezer API check: official reference behind a login wall, not read. A recorded real answer in the test suite shows 25 rows by default and `artist.id` on each row. No page-size parameter is sent (C20).

### For Helder to confirm or overrule

Decisions taken for him are C1 to C27 in `design.md § 1.2`. The ones most worth a look:

| # | Decision |
|---|----------|
| C14 | Add mode: changing the Title text after a Deezer pick makes the artist editable again (that song is saved as local-only by the existing rule). |
| C16 | Save waits for the first-open lyrics lookup to end before writing. |
| C17 | "None found" is permanent, also when the user had corrected the title or the lyrics were over the limit. |
| C22 | Mocks in `SongFormViewModelLyricsTests` are re-pointed to the new lookup method; no assertion changes. |
| C23 | The existing test of the Version reset on X is removed with the code it tests. |
| C25 | **Known gap:** add mode — change the Title, edit the artist, type the picked title back — saves a Deezer song with the edited artist. Not closed. |
| C27 | A Deezer option row keeps its album part; only the version label is gone. |

## T1, T2, T4, T5, T6 (2026-11-16) — To Review

### Changed files

| Task | Commit | Files |
|------|--------|-------|
| T1 | 9c87cbde | `Contracts/Enums/LyricsOrigin.cs`, `Contracts/DTOs/LyricsLookupResult.cs`, `Domain/Rules/SongIdentityRules.cs`, `Domain/Rules/LyricsOriginRules.cs`, `Domain/Resolution/SongFormSaveRequest.cs`, tests `Unit/Domain/LyricsOriginRulesTests.cs`, `Unit/Domain/SongIdentityRulesTests.cs` |
| T2 | 646efce9 | `Contracts/DTOs/MusicSearchResultDto.cs`, `Domain/Constants/SearchConstants.cs` (level C, no test) |
| T6 | 3d5aff4a | `Services/DeezerProvider.cs`, test `Unit/Services/DeezerProviderArtistIdTests.cs` |
| T4 | a0258799 | `Infra/Repository/SongRepository.cs`, test `Integration/Repositories/SongRepositoryListSearchTests.cs` |
| T5 | 0c3abbb3 | `Services/ISongLyricsService.cs`, `Services/SongLyricsService.cs`, test `Unit/Services/SongLyricsServiceLookupTests.cs` |

### Verification evidence

- T1: rule tests 46 cases passed; full suite 1,706 passed, 0 failed (run by the main agent).
- T2 + T6: `DeezerProvider` filter 41 passed; full suite 1,712 passed, 0 failed. No existing test edited.
- T4: list-search filter 118 passed; full suite 1,720 passed, 0 failed. `SqlQueryRaw<SongListItemDto>` binds the record directly.
- T5: `SongLyricsService` filter 31 passed (the 16 existing tests unchanged); full suite 1,729 passed, 0 failed.

### Deviations from strict TDD (honest record)

- T1: the implementor lost its shell before running anything. Tests were written as one batch and first failed at compile level. The main agent reviewed the diff, ran the tests and committed.
- T5: the nine tests were first red against a throwing stub, not on their assertions.
- T4 and T6: tests that guard existing behaviour passed before the change and are recorded as guards; the others were red on their assertions first.

### Notes

- T2/T6 commits are signed "Claude Fable 5.2" although that implementor reported running on Haiku (briefing error by the main agent). Later briefings let each agent sign with its own model.
- The pre-commit hook failed once on the T2 commit and passed on an unchanged retry; no cause found.
- T5: the "blank title or artist gives no answer" row has no dedicated new test (the plan lists nine); the existing `FindLyricsAsync_BlankTitleOrArtist` test covers the wrapper.

