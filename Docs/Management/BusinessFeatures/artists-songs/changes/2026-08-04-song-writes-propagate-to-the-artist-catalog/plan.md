# CATALOG-PROP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Task status lives in `tasks.md` (Rule 4); this file carries the steps.

**Goal:** Every song keeps its author's catalog row through create, author change and delete. The user can remove only covers from a catalog. Artist delete is blocked by owned songs, not by catalog rows.

**Architecture:** The propagation is staged inside the existing `SongService` `IUnitOfWork.ExecuteAsync` lambdas through `ICatalogRepository` resolved from `sp`. A data-only migration backfills old songs. `CatalogService` adds the derived-row guard and a bulk remove. `SongsViewModel` routes catalog-mode Delete to it.

**Tech Stack:** .NET MAUI, C#, EF Core + SQLite, CommunityToolkit.Mvvm, xUnit + Moq. Versions come from the `.csproj` files.

**Spec:** `requirements.md` (REQ-CATPROP-01…11, APPROVED 2026-11-20) and `design.md` in this folder. Executors read both. Every string below is copied from `requirements.md`; on any mismatch the spec wins.

## Global Constraints

- **Gate 0 (blocking):** do not create the worktree until branch `task/remove-song-version-featured` (worktree `.claude/worktrees/rmv-song-version`) is merged to `develop`. It owns `Domain/RepositoryInterface/ISongRepository.cs`, `Infra/Repository/SongRepository.cs`, `Infra/Repository/CatalogRepository.cs` and `Domain/Entity/Song.cs`. Check with `git worktree list` and `git branch -a`.
- **Branch and worktree:** `feature/catalog-prop`, worktree `.claude/worktrees/catalog-prop`, based on `develop` after Gate 0. All tasks run in order in that one worktree.
- **REQ-UOW-28/37:** every repository used inside `ExecuteAsync` / `ExecuteReadAsync` comes from `sp.GetRequiredService<…>()`, never from a constructor field. `Tests/Architecture/UnitOfWorkReadScopeTests.cs` must stay green.
- **REQ-UOW-41:** pure argument checks (empty id list) stay outside the lambda.
- **No `SaveChangesAsync`** in repositories or services. The unit of work owns the save.
- **Tests:** repository and migration tests use real SQLite (temp file), never the in-memory provider. Service tests use Moq. The first comment line of every AC test is `// [AC] REQ-CATPROP-NN: <criterion>`.
- **Builder must not modify tests.** Existing tests that this spec changes (`design.md § 9`, "Existing tests this spec changes") are rewritten in the Red step of the task that owns them.
- **Sequential-only:** `Infra/Migrations/*` (Task 2).
- **No XAML, no governed component, no `MauiProgram.cs` change.**
- **Signature style:** each new interface method follows its interface's existing style. `ICatalogRepository` methods take a required `CancellationToken ct` (no default). Check `ISongRepository` and `ICatalogService` and match them.
- **Test roles:** Red-step test work, including rewrites of existing tests that encode replaced behaviour, is done by the Tester role before Green. The Builder edits production code only.
- **Test command:** `dotnet test Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~<Class>"`. Use the full run (no filter) at each task end.

## Review Focus

1. **Duplicate ids in a bulk remove** (`[5, 5]`): count once, never throw. Test in Task 1.
2. **A cover of the same song by another artist** must survive a remove scoped to this artist. Test in Task 1.
3. **Author change when the old author has no derived row** (drift, e.g. a restored DB before startup migration): no throw, the new row is still written. Test in Task 3b.
4. **Author change to an artist created in the same save:** the row uses the real flushed id, not 0. Test in Task 3b.
5. **Catalog-mode Delete when the bulk remove reports r = 0:** error snackbar, the list still reloads, `DeleteSongsAsync` is never called. Test in Task 6.

---

### Task 1: Repository queries (Domain + Infra)

REQ-CATPROP-05, REQ-CATPROP-06 and 07 (repository part), REQ-CATPROP-10 (count). Review lane: Standard.
Size note: 6 files, because each interface change is paired with its implementation.

**Files:**
- Modify: `Domain/RepositoryInterface/ICatalogRepository.cs`, `Infra/Repository/CatalogRepository.cs`
- Modify: `Domain/RepositoryInterface/ISongRepository.cs`, `Infra/Repository/SongRepository.cs`
- Test: `Tests/Integration/Repositories/CatalogRepositoryTests.cs`, `Tests/Integration/Repositories/SongRepositoryTests.cs`

**Interfaces:**
- Consumes: nothing new.
- Produces:
```csharp
// ICatalogRepository (required ct, as the existing methods)
Task<bool> IsDerivedAsync(int artistId, int songId, CancellationToken ct);
Task<(int removed, int kept)> RemoveCoversAsync(int artistId, IReadOnlyCollection<int> songIds, CancellationToken ct);
// ISongRepository (ct style as that interface's existing methods)
Task<int> CountByArtistAsync(int artistId, CancellationToken ct);
```

- [ ] **Step 1: Write the failing tests** (seed with the file's existing builders/helpers):
  - `IsDerivedAsync_AuthorRow_ReturnsTrue` / `_CoverRow_ReturnsFalse` / `_MissingRow_ReturnsFalse`
  - `RemoveCoversAsync_MixedList_RemovesCoversKeepsDerived`: artist A authors S1 (derived row) and covers S2 (author B). Call `RemoveCoversAsync(A, [S1, S2])` → `(1, 1)`; `(A,S1)` exists, `(A,S2)` gone.
  - `RemoveCoversAsync_UnknownIds_CountsNeither`: `[999]` → `(0, 0)`.
  - `RemoveCoversAsync_DuplicateIds_CountedOnce`: `[S2, S2]` → `(1, 0)` (Review Focus 1).
  - `RemoveCoversAsync_OtherArtistsCoverOfSameSong_Untouched`: C also covers S2; after the A call `(C,S2)` exists (Review Focus 2).
  - `SongRepository.CountByArtistAsync_ReturnsOwnedSongsOnly`: A owns 2 songs and covers 1 → `2`; unknown artist → `0`.
  - `DeleteAsync_Song_RemovesDerivedAndCoverCatalogRows` (REQ-CATPROP-05; no such test exists today): song S by A, with `(A,S)` and a cover `(C,S)`. Delete S through the repository's delete method → zero `Catalog` rows with `SongId == S`, and `(A, otherSong)` untouched.
- [ ] **Step 2: Run, expect a compile failure** (methods missing).
- [ ] **Step 3: Implement.**
  - `IsDerivedAsync`: `AnyAsync` on `Catalog` with the pair and `c.Song.ArtistId == artistId`.
  - `RemoveCoversAsync`: `Distinct()` the ids. `kept` = `CountAsync` of the derived rows in the list. `removed` = `ExecuteDeleteAsync` where `c.ArtistId == artistId && ids.Contains(c.SongId) && c.Song.ArtistId != artistId`.
  - `CountByArtistAsync`: `CountAsync(s => s.ArtistId == artistId)`.
  - **Fallback** if EF cannot translate the navigation predicate inside `ExecuteDeleteAsync` (the test shows a translation error): first select the cover `SongId`s (`c.Song.ArtistId != artistId`) into a list, then `ExecuteDeleteAsync` on `c.ArtistId == artistId && coverIds.Contains(c.SongId)`. Both steps run inside the caller's unit-of-work transaction.
- [ ] **Step 4: Run the two classes, expect PASS.** Then do the full run.
- [ ] **Step 5: Commit** `feat: catalog repository derived-row queries and owned-song count` with trailer `Item: song-writes-propagate-to-the-artist-catalog`.

### Task 2: Backfill migration (REQ-CATPROP-09, 11)

**Files:**
- Create: `Infra/Migrations/<timestamp>_BackfillDerivedCatalogRows.cs` (+ `.Designer.cs`, generated). The model snapshot must stay unchanged; verify the diff.
- Create test: `Tests/Integration/Migrations/CatalogBackfillMigrationTests.cs`
- Test: `Tests/Integration/Repositories/ArtistRepositoryTests.cs`

Review lane: Elevated (migration).

**Interfaces:**
- Consumes: nothing.
- Produces: `public const string BackfillSql` on the migration class. `Up` and the tests use it.

- [ ] **Step 1: Generate the empty migration:** `dotnet ef migrations add BackfillDerivedCatalogRows -p Infra/MyVocaList.Infra.csproj`. This follows `.claude/library/database-setup.md` step 1, with `-p` given as the project file; respect MigrationsLock. Expect empty `Up`/`Down` and no snapshot change.
- [ ] **Step 2: Write the failing tests.** Follow `SongMigrationTests` (`MigratedToLatestMinusAsync` to the previous migration, raw-SQL seed, `IMigrator.MigrateAsync()`, `SqlQueryRaw` assert):
  - `Backfill_SongWithoutDerivedRow_GetsOne`
  - `Backfill_ExistingManualAuthorRow_NotDuplicated`: count for the pair = 1
  - `Backfill_CoverRowsUntouched`
  - `Backfill_SecondApply_InsertsNothing`: after migrating, run `ExecuteSqlRawAsync(BackfillDerivedCatalogRows.BackfillSql)` again; the `Catalog` row count is unchanged (`Up` is protected, so the test runs the constant)
  - In `ArtistRepositoryTests`: `PerformersOnly_AuthorWithOnlyDerivedRows_IsReturned` (REQ-CATPROP-11)
- [ ] **Step 3: Run, expect FAIL** (no rows inserted).
- [ ] **Step 4: Fill the migration body as in `design.md § 7`.** `BackfillSql` = `INSERT OR IGNORE INTO Catalog (ArtistId, SongId) SELECT ArtistId, Id FROM Songs;`. `Up` = `migrationBuilder.Sql(BackfillSql)`. `Down` = empty, with the D4 comment.
- [ ] **Step 5: Run, expect PASS.** Then do the full run, then `dotnet ef database update` per `database-setup.md`.
- [ ] **Step 6: Commit** `feat: backfill derived catalog rows for existing songs` + `Item:` trailer.

### Task 3a: Test setup for the new SongService dependency (Tester role)

Review lane: Standard. No AC; this is setup only.

Each SongService unit test builds its own fake `sp` with `PassthroughUnitOfWork.Over(...)`
(`Tests/Infrastructure/UnitOfWorkMocks.cs`). `StubServiceProvider.GetService` throws for an
unregistered type, so once Task 3b resolves `ICatalogRepository` in the create paths, every create-path
unit test would fail on setup, not on behaviour.

**Files (size note: 11, each a one-argument mechanical addition, no assertion changed):**
`Tests/Unit/Services/SongServiceTests.cs`, `SongServiceSaveFromFormTests.cs`,
`SongServiceSaveFromFormImageTests.cs`, `SongServiceSaveFromFormPictureTests.cs`,
`SongServiceArtistFreezeTests.cs`, `SongServiceTitleFreezeTests.cs`, `SongServiceLyricsOriginTests.cs`,
`SongServiceEnsureLyricsTests.cs`, `Tests/Integration/UnitOfWork/SongServiceReadScopeTests.cs`, and any
other `Over(` call that builds a `SongService`. Grep `PassthroughUnitOfWork.Over` plus `SongService`,
never from memory.

- [ ] **Step 1:** In each of those `Over(...)` calls, add `new Mock<ICatalogRepository>().Object` (a field
  per class where the class already keeps repository mocks as fields). Do not change `UnitOfWorkMocks.cs`.
- [ ] **Step 2:** Do the full run and expect PASS (nothing resolves it yet, so behaviour is unchanged).
- [ ] **Step 3:** Commit `test: register the catalog repository in SongService test scopes` + `Item:` trailer.

### Task 3b: SongService propagation (REQ-CATPROP-01…04)

Review lane: Elevated (transactional write path).

**Files:**
- Modify: `Services/SongService.cs` (`CreateSongAsync`, `CreateSongWithUrlsCoreAsync`, `UpdateSongCoreAsync`)
- Create test: `Tests/Integration/Services/SongServiceCatalogPropagationTests.cs` (real SQLite, real unit of work)

**Interfaces:**
- Consumes: `ICatalogRepository.ExistsAsync`, `AddAsync`, `RemoveAsync` (existing), and Task 3a's setup.
- Produces: nothing new.

- [ ] **Step 1: Write the failing tests** (integration, real stack):
  - `CreateSongAsync_WritesDerivedRow` (01)
  - `SaveFromForm_AddMode_WritesDerivedRow` (01)
  - `SaveFromForm_AddMode_NewInlineArtist_WritesDerivedRowWithRealId` (01, Review Focus 4)
  - `CreateSongWithUrls_WritesDerivedRow` (01)
  - `Update_SameAuthor_CatalogUnchanged` (02): a cover row of another artist is still there, and nothing was added
  - `Update_AuthorAtoB_MovesRow` (03): `(A,S)` gone, `(B,S)` once, A's other derived row untouched
  - `Update_AuthorAtoB_BAlreadyCovers_NoDuplicate` (03)
  - `Update_AuthorToNewInlineArtist_MovesRowWithRealId` (03, Review Focus 4)
  - `Update_AuthorChange_OldDerivedRowMissing_StillWritesNew` (03, Review Focus 3)
  - `Create_ValidationFails_NoCatalogRow` and `Update_ArtistChoiceNeeded_CatalogUnchanged` (04)
  - `Update_AuthorAtoB_SaveThrows_CatalogUnchanged` (04): use a throwing `ICatalogRepository` decorator on `AddAsync`, following `ThrowOnAddSongRepository` in `Tests/Integration/UnitOfWork/SongServiceNewArtistUnitOfWorkTests.cs`. Expect the exception; `(A,S)` is still present, `(B,S)` absent and `Song.ArtistId == A`.
- [ ] **Step 2: Run, expect FAIL.**
- [ ] **Step 3: Implement per `design.md § 3.1–3.2`.**
  - Create paths stage `new Catalog { Song = song, ArtistId = authorId }` through `ICatalogRepository.AddAsync`.
  - Update captures `oldArtistId` before the reassignment. After every refusal, when the author changed, it calls `RemoveAsync(old, id)` and then `AddAsync` only if `!ExistsAsync(new, id)`.
- [ ] **Step 4: Run, expect PASS.** Then do the full run, which includes `UnitOfWorkReadScopeTests`.
- [ ] **Step 5: Commit** `feat: song writes keep the author's catalog row` + `Item:` trailer.

### Task 4: CatalogService guard and bulk remove (REQ-CATPROP-06, 07)

Review lane: Standard.

**Files:**
- Modify: `Domain/ServicesInterfaces/ICatalogService.cs`, `Services/CatalogService.cs`
- Test: `Tests/Unit/Services/CatalogServiceTests.cs`. Rewrite the "remove always succeeds" test; it encodes the replaced behaviour.

**Interfaces (produces):**
```csharp
Task<(bool success, string message, int removed, int kept)> RemoveSongsFromCatalogAsync(
    int artistId, IReadOnlyCollection<int> songIds, CancellationToken ct = default);
```
(`CatalogService` uses `/// <inheritdoc />`.) **Consumes:** Task 1 `IsDerivedAsync`, `RemoveCoversAsync`.

- [ ] **Step 1: Write the failing tests.** There is one test per outcome, and each asserts the exact strings below. `<A>` is the artist name from the `IArtistRepository` mock.
  - Single remove:
    - derived → `(false, "<A> wrote this song, so it stays in their catalog.")`, `RemoveAsync` never called
    - cover → `(true, "Song removed from catalog.")`
    - missing → `(false, "Song is not in this catalog.")`
  - Bulk remove:
    - empty list → `(false, "No songs selected.", 0, 0)`, with no unit-of-work call
    - unknown artist → `(false, "Artist not found.", 0, 0)`
    - `(0,0)` → `"Nothing to remove from the catalog."`
    - `(0,1)` → `"<A> wrote this song, so it stays in their catalog."`
    - `(0,3)` → `"<A> wrote these songs, so they stay in their catalog."`
    - `(1,0)` → `(true, "Removed 1 song from the catalog.")`
    - `(2,0)` → `"Removed 2 songs from the catalog."`
    - `(2,1)` → `"Removed 2 songs from the catalog. 1 kept: <A> wrote it."`
    - `(2,3)` → `"Removed 2 songs from the catalog. 3 kept: <A> wrote them."`
- [ ] **Step 2: Run, expect FAIL.**
- [ ] **Step 3: Implement per `design.md § 4.2`.** The order is: exists, then derived, then remove. The bulk call is one `ExecuteAsync`, and `success = removed > 0`.
- [ ] **Step 4: Run, expect PASS.** Then do the full run.
- [ ] **Step 5: Commit** `feat: catalog removal keeps the author's own songs` + `Item:` trailer.

### Task 5: ArtistService delete check (REQ-CATPROP-10, service part)

Review lane: Standard.

**Files:**
- Modify: `Services/ArtistService.cs`: `DeleteArtistsAsync`, and remove the now-unused `ICatalogRepository` constructor parameter and field.
- Test: `Tests/Unit/Services/ArtistServiceTests.cs`. Rewrite the two delete-refusal tests that mock `ICatalogRepository.CountByArtistAsync`, and drop the catalog argument from its constructions.
- Modify (constructor call only): `Tests/Integration/UnitOfWork/PagedListConcurrencyTests.cs` (`new ArtistService(`). Grep for every other `new ArtistService(` and include it.
- Create test: `Tests/Integration/Services/ArtistServiceDeleteIntegrationTests.cs`

**Interfaces:**
- Consumes: Task 1 `ISongRepository.CountByArtistAsync`.
- Produces: `ArtistService` constructor without `ICatalogRepository`. DI resolves it automatically; there is no `MauiProgram.cs` edit.

- [ ] **Step 1: Write the failing tests.**
  - `Delete_OwnerOfSongs_Refused`: message `"Cannot delete — <Name> owns 2 song(s). Delete their songs first."`, `DeleteAsync` never called.
  - `Delete_TwoOwners_NamesFirstInSelectionOrder`.
  - `Delete_ArtistWithOnlyCatalogRows_Succeeds`.
  - Integration `Delete_CoverOnlyArtist_RowsGoneSongsKept`.
- [ ] **Step 2: Run, expect FAIL.**
- [ ] **Step 3: Implement.**
  - Swap the check to `sp.GetRequiredService<ISongRepository>().CountByArtistAsync(id, ct)` and use the AC-6.2 text.
  - Remove `_catalogRepository`. The architecture test's governed-parameter floor is ≥ 15; the current count is well above it. Record the count after the removal in `task-log.md`.
- [ ] **Step 4: Run, expect PASS.** Then do the full run, which includes `UnitOfWorkReadScopeTests`.
- [ ] **Step 5: Commit** `feat: only owned songs block an artist delete` + `Item:` trailer.

### Task 6: ViewModels (REQ-CATPROP-08, REQ-CATPROP-10 text)

**Files:**
- Modify: `MyVocaList/UI/ViewModels/SongsViewModel.cs` (`BuildDeleteConfirmMessage`, `ExecuteDeleteAsync`; delete `RemoveFromCatalogCommand` / `RemoveFromCatalogAsync`)
- Modify: `MyVocaList/UI/ViewModels/ArtistsViewModel.cs` (`BuildDeleteConfirmMessage` only)
- Create test: `Tests/Unit/ViewModels/SongsViewModelCatalogDeleteTests.cs`
- Create test: `Tests/Unit/ViewModels/ArtistsViewModelDeleteConfirmTests.cs`. Never touch `Shell.Current`.
  - Construction: copy it from `SongsViewModelBug086Tests.cs` / `ArtistsViewModelBug086Tests.cs` (Moq `ICatalogService`, `ISongService`, `ISnackbarComponent`, `INavigationService`, `ILogger`).
  - Route: `ExecuteDeleteAsync` and `BuildDeleteConfirmMessage` are protected. Drive them as `CrudListViewModelBaseTests` does: select the items, run `DeleteSelectedCommand`, read `ConfirmMessage`, then `await ConfirmActionCommand.ExecuteAsync(null)`. Handle the `Task.Run` / `RunOnUiThread` dispatch the same way those base tests do.

Review lane: Standard.

**Interfaces:**
- Consumes: Task 4 `RemoveSongsFromCatalogAsync`.
- Produces: nothing new.

- [ ] **Step 1: Write the failing tests.**
  - Catalog mode (`ArtistId = 7`, `ArtistName = "Ramones"`):
    - confirm text for 1 item is `"Remove 'Blitzkrieg Bop' from Ramones's catalog?"`, and for 3 items is `"Remove 3 songs from Ramones's catalog?"`;
    - `ExecuteDeleteAsync` calls `RemoveSongsFromCatalogAsync(7, ids)` and never calls `DeleteSongsAsync`;
    - `success` true → `ShowSuccessAsync(message)`;
    - `success` false → the error snackbar with the message, and the list still reloads (Review Focus 5).
  - Songs mode: confirm text and the `DeleteSongsAsync` call are unchanged.
  - Artists:
    - `"Delete 'Ramones'? Their catalog entries will also be removed."`
    - `"Delete 2 artists? Their catalog entries will also be removed."`
- [ ] **Step 2: Run, expect FAIL.**
- [ ] **Step 3: Implement per `design.md § 5–6`.** The catalog branch diverges from the song path on purpose: the song path refreshes only on success, while the catalog branch calls `RefreshAsync()` on both outcomes and then shows the snackbar (success or error by `success`). The songs-mode branch stays as it is.
- [ ] **Step 4: Run, expect PASS.** Then do the full run and `/sln-build` for Android (0 errors).
- [ ] **Step 5: Commit** `feat: catalog-mode delete removes catalog entries, not songs` + `Item:` trailer.

### Task 7: Close-out

- [ ] Merge `feature/catalog-prop` to `develop`, run the full tests, and build Android.
- [ ] Fill the traceability matrix (REQ-CATPROP-01…11 → implementation → test method) in `task-log.md`.
- [ ] Mark `requirements.md`/`design.md` as shipped and set status ✅ only after the device check.
- [ ] **Device check (Helder):**
  - an existing author's catalog lists their songs after the update;
  - add a song → the author's catalog lists it;
  - change a song's author → it moves;
  - in a catalog, delete a mix of own songs and covers → only covers go, and the snackbar shows both counts;
  - delete an artist with songs → refused (AC-6.2 text);
  - delete a cover-only artist → succeeds.
