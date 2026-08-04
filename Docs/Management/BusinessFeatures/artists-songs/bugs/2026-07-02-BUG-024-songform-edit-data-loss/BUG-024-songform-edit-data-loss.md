# BUG-024 — SongForm edit-mode Save wipes FeaturedArtists + Lyrics and discards Version

**Severity:** Critical (silent data loss)
**Found by:** Task 04 (Songs form validation) review, 2026-07-02
**Fixed:** 2026-07-02 (this worktree branch; merged by orchestrator after review)
**Approved approach:** Helder decision 2026-07-02 — add `GetSongByIdAsync` to `ISongService`/`SongService`, fully hydrate `LoadSongForEditAsync`, make `ExecuteEditSaveAsync` send complete form data including Version.

## Emulator smoke test — BLOCKED 2026-07-03
Helder's emulator session (`Docs/Management/EMULATOR_TEST_MASTER_LIST.md` TEST-003) could not exercise this fix: editing an existing song was blocked by **BUG-027** (SongFormPage Artist field has no working required-field validation/autocomplete — the same blocker that prevents new-song creation also prevents opening/saving edits in this test run). Re-run TEST-003 once BUG-027 is fixed.

---

## Symptom

Editing any field of an existing song (e.g. fixing a typo in the Title) and tapping Save
silently erased the song's stored `FeaturedArtists` and `Lyrics`, and any edit made to the
`Version` field was discarded. No error was shown — the data loss was invisible until the
song was next viewed.

## Root cause

Four compounding gaps across the edit flow:

1. **Edit navigation hydrated only `songId`/`artistId`/`artistName`/`songTitle`** (Shell
   query properties) — FeaturedArtists, Lyrics and Version were never loaded into the form.
2. **`SongFormViewModel.LoadSongForEditAsync` loaded YouTube URLs only.** It could not load
   more: `ISongService` had no `GetSongByIdAsync` (the method's own comment said so).
3. **`ExecuteEditSaveAsync` sent the form's empty `FeaturedArtists`/`Lyrics` and ignored its
   `version` parameter entirely** — `UpdateSongAsync` had no version parameter to receive it.
4. **`SongService.UpdateSongAsync` overwrote `FeaturedArtists`/`Lyrics` unconditionally**
   with whatever it received.

Net effect: empty form fields overwrote stored data on every edit save.

## Fix

- **Domain (`Domain/ServicesInterfaces/ISongService.cs`)**
  - Added `Task<Song?> GetSongByIdAsync(int id, CancellationToken ct = default)` (full XML docs;
    null = not found, matching `ISongRepository.GetByIdAsync` semantics).
  - Added `string? version = null` parameter to `UpdateSongAsync` — null = keep existing value
    (same semantics as the existing `externalId`/`externalProvider` parameters).
- **Services (`Services/SongService.cs`)**
  - Implemented `GetSongByIdAsync` delegating to `ISongRepository.GetByIdAsync` (the repository
    method already returned the full entity — `FeaturedArtists`, `Lyrics`, `Version` are scalar
    columns, no related-data loading required; no repository change was needed).
  - `UpdateSongAsync`: validates the provided version via `ValidateVersionInput` and persists
    `song.Version = version.Trim()` when `version != null`.
- **Services (`Services/SongResolutionService.cs`)**
  - Both `UpdateSongAsync` call sites (UpdateExisting, AttachExternalId) now pass `song.Version`
    explicitly, preserving their previous keep-existing behavior under the new signature.
- **UI (`MyVocaList/UI/ViewModels/SongFormViewModel.cs`)**
  - `LoadSongForEditAsync` now calls `_songService.GetSongByIdAsync(songId)` and hydrates every
    field `UpdateSongAsync` persists: `SongTitle`, `SongVersion`, `FeaturedArtists`, `Lyrics`,
    plus the `SelectedExternalId`/`SelectedProvider` stash (round-tripped unchanged on save).
    Hydration runs inside a re-opened `_isHydrating` window (save/restore around the property
    assignments) because the async entity load can complete after the page's `OnAppearing` has
    already called `CompleteHydration()` — this preserves the edit-mode dirty-guard (Form
    Validation Standard).
  - `IsArtistLocked` now applies the full BUG-008 rule from the loaded entity:
    `ExternalId` set AND `HasManualEdits == false`.
  - `ExecuteEditSaveAsync` passes the current (trimmed) `version` to `UpdateSongAsync`.

## Regression tests (Red → Green evidence)

| # | Test | Red evidence (before fix) | Green |
|---|------|---------------------------|-------|
| 1 | `SongServiceTests.GetSongByIdAsync_ExistingId_ReturnsSongWithAllFields` | Compile-Red (CS1061: no `GetSongByIdAsync`), then behavioral Red against the TDD stub: `Assert.NotNull() Failure: Value is null` | Pass |
| 2 | `SongFormViewModelTests.LoadSongForEdit_ExistingSong_HydratesFeaturedArtistsLyricsAndVersion` | `Assert.Equal() Failure: Strings differ` — `FeaturedArtists` empty, entity never loaded (the core data-loss proof) | Pass |
| 3 | `SongServiceTests.UpdateSongAsync_WithVersion_PersistsVersion` | Compile-Red (CS1503: no `version` param), then behavioral Red against skeleton param: `Expected: "Acoustic" / Actual: "Live"` | Pass |
| 4 | `SongServiceTests.UpdateSongAsync_NullVersion_KeepsExistingVersion` | Guard-rail for the null-keeps-existing branch written in test 3's Green step (not seen Red individually) | Pass |
| 5 | `SongServiceTests.UpdateSongAsync_VersionTooLong_ReturnsFalse` | Guard-rail for the validation branch written in test 3's Green step (not seen Red individually) | Pass |
| 6 | `SongFormViewModelTests.SaveAsync_EditMode_SendsHydratedFieldsAndEditedVersion` | Moq verify Red: `Performed invocations: UpdateSongAsync(42, "Stored Title", "Feat A", "Stored lyrics", True, null, null, null, ct)` — version still discarded after hydration fix | Pass |
| 7 | `SongFormViewModelTests.LoadSongForEdit_ApiImportedWithoutManualEdits_LocksArtistField` | Coverage for the IsArtistLocked rule enabled by hydration (written post-Green) | Pass |

Test counts: **428/428 before → 435/435 after** (+7).

## Out of scope / follow-up candidate

`UpdateSongAsync` still uses the **title-only** uniqueness check
(`ExistsByTitleForArtistAsync(artistId, title, excludeId)`), not the 3-column
`ExistsByTitleVersionForArtistAsync(..., excludeId)` overload the repository already exposes
"for edit/update validation". Consequence (pre-existing, NOT introduced by this fix): a song
that legitimately shares its title with a sibling version (created via "Save as new version")
cannot be edit-saved — the duplicate check trips on the sibling. Changing the check would have
altered behavior encoded in existing tests (`UpdateSongAsync_DuplicateTitleExcludingSelf_ReturnsFalse`),
which is outside this bug's approved scope and forbidden by `testing.md § Builder Must Not
Modify Tests`. Recommend registering as a separate bug.

## Regression risk

Low — additive service method + edit-path-only ViewModel changes; `SongResolutionService`
call sites preserve prior behavior by passing the entity's own `Version`.
