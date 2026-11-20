# Design: song writes propagate to the artist catalog (CATALOG-PROP)

> **Status: APPROVED by Helder 2026-11-20**, written with `requirements.md` (REQ-CATPROP-01…11). Decisions: `README.md`
> D1–D4 (2026-08-24) and § Decisions — Helder, 2026-11-19.

## 1. Decisions this design rests on

| # | Decision | Source |
|---|----------|--------|
| 1 | The catalog is the performer relation. The author always performs their songs, so a derived row exists for every song. | D1 |
| 2 | An author change moves the derived row in the same transaction. Derived rows are read-only to the user, with a Services-layer guard. | D2 |
| 3 | "Derived" is computed (`Catalog.ArtistId == Song.ArtistId`). No provenance column. | D4 |
| 4 | Existing songs are backfilled once by an idempotent EF migration. | D4 |
| 5 | Catalog-mode Delete removes catalog rows (covers only), never songs. | D5 |
| 6 | Catalog rows never block an artist delete. Owned songs still do (parent AC-6.2 kept). | D6 |
| 7 | Propagation lives in `SongService` and uses `ICatalogRepository` from the lambda's `sp`, not `ICatalogService`. `ICatalogService` is not in the governed list of `Tests/Architecture/UnitOfWorkReadScopeTests.cs`, so going through it would escape the REQ-UOW-28/37 check, and a service calling a service inside a unit of work adds nothing here. | design choice |

## 2. Data model, invariants, postconditions

No schema change. `Catalog(ArtistId, SongId)` keeps its composite PK and its two cascading FKs
(`Infra/EntityEFConfig/CatalogConfiguration.cs`). The one migration is data-only (§ 7).

**Invariant (I1):** for every row of `Songs`, a row `Catalog(Songs.ArtistId, Songs.Id)` exists.
It holds after the migration (§ 7) and is kept by every song write (§ 3).

**Postconditions per operation**

| Operation | On success | On failure |
|-----------|------------|------------|
| Song create | I1 holds for the new song; no other catalog row changes | no song, no catalog change |
| Song update, same author | catalog rows of the song unchanged | no change |
| Song update, author A→B | `(A, song)` absent, `(B, song)` present once, others unchanged | no change |
| Song delete | no catalog row references the song | no change |
| Single / bulk catalog remove | only cover rows removed; I1 still holds | no change |
| Artist delete (song-less artists only) | the artists and their catalog rows gone; no song removed | nothing deleted |

**State machines:** none. A catalog row has no lifecycle beyond exists/absent. "Derived" is a computed
property of a row, not a state.

## 3. SongService (`Services/SongService.cs`)

All writes already run inside `_uow.ExecuteAsync(async sp => …)`, and every repository comes from
`sp.GetRequiredService<…>()` (REQ-UOW-28). The single `SaveChanges` and the explicit transaction belong
to `IUnitOfWork`, so anything staged in the lambda commits or rolls back with the song (REQ-CATPROP-04).

### 3.1 Create — REQ-CATPROP-01

- **`CreateSongWithUrlsCoreAsync`** (also used by `SaveFromFormAsync` in add mode): next to the URL
  staging (`Song = song`), stage `new Catalog { Song = song, ArtistId = <resolved author id> }`. The
  author id is known once the artist is resolved. The inline-created artist is already flushed
  (`_uow.FlushAsync`), so its id exists.
- **`CreateSongAsync`**: the same staging after the song is added. This method resolves only the song
  and artist repositories today, so it adds `sp.GetRequiredService<ICatalogRepository>()`.
- No existence check is needed: the song is new, so no row for it can exist.

### 3.2 Update — REQ-CATPROP-02, 03

In `UpdateSongCoreAsync`, capture `oldArtistId` before the author is reassigned (`song.ArtistId = effectiveArtistId`). Then:

```
if (effectiveArtistId != oldArtistId)
{
    var catalog = sp.GetRequiredService<ICatalogRepository>();
    await catalog.RemoveAsync(oldArtistId, song.Id, ct);          // ExecuteDeleteAsync, inside the UoW transaction
    if (!await catalog.ExistsAsync(effectiveArtistId, song.Id, ct)) // B's cover row becomes the derived row
        await catalog.AddAsync(new Catalog { ArtistId = effectiveArtistId, SongId = song.Id }, ct);
}
```

- If the new author is created in the same save, it is flushed before this point (existing code), so its
  id is real.
- Deezer songs cannot change author (existing guard), so this branch only runs for non-Deezer songs.
- `EnsureLyricsAsync` and every other update that does not touch `ArtistId` are unaffected (REQ-CATPROP-02).

The move goes after every refusal in `UpdateSongCoreAsync` (after the author is reassigned). Nothing
after it can fail except the final save, which rolls back with it.

### 3.3 Delete — REQ-CATPROP-05

No code change. `DeleteSongsAsync` → `ExecuteDeleteAsync` → the DB-level cascade removes the rows.
No test covers the catalog cascade on song delete today. A new `SongRepositoryTests` case pins it, for
both a derived row and a cover row.

## 4. CatalogService and ICatalogRepository

### 4.1 Repository additions (`Domain/RepositoryInterface/ICatalogRepository.cs`, `Infra/.../CatalogRepository.cs`)

```csharp
// ICatalogRepository — additions
/// <summary>True when Catalog(artistId, songId) exists and the song's author is artistId.</summary>
Task<bool> IsDerivedAsync(int artistId, int songId, CancellationToken ct);

/// <summary>Removes the artist's cover rows for the listed songs; derived rows are left and counted.</summary>
Task<(int removed, int kept)> RemoveCoversAsync(
    int artistId, IReadOnlyCollection<int> songIds, CancellationToken ct);
// Required ct, matching the existing ICatalogRepository methods.
```

- `IsDerivedAsync`: `Catalog` filtered on the pair, with `Song.ArtistId == artistId` (navigation join).
- `RemoveCoversAsync`:
  - `kept` = count of `Catalog` rows with `ArtistId == artistId`, `SongId` in the list and
    `Song.ArtistId == artistId`;
  - `removed` = `ExecuteDeleteAsync` over the same rows with `Song.ArtistId != artistId`. EF translates
    the navigation filter into a subquery. A real-SQLite repository test proves it.

The existing `RemoveAsync`, `ExistsAsync`, `AddAsync`, `CountByArtistAsync` and the paged read stay.

```csharp
// ISongRepository — addition (no owned-song count exists today)
/// <summary>Number of songs whose author (Song.ArtistId) is artistId.</summary>
Task<int> CountByArtistAsync(int artistId, CancellationToken ct);   // ct style as ISongRepository's existing methods
```

### 4.2 Service changes (`Domain/ServicesInterfaces/ICatalogService.cs`, `Services/CatalogService.cs`)

- **`RemoveSongFromCatalogAsync`** (REQ-CATPROP-06), inside `ExecuteAsync`, in this order:
  1. if `!ExistsAsync` → `(false, "Song is not in this catalog.")`;
  2. if `IsDerivedAsync` → `(false, "<Artist> wrote this song, so it stays in their catalog.")`. The
     artist name comes from `IArtistRepository` via `sp`;
  3. otherwise `RemoveAsync` → `(true, "Song removed from catalog.")`.
- **New bulk remove** (REQ-CATPROP-07):

  ```csharp
  // ICatalogService — addition; CatalogService implements it with /// <inheritdoc />
  /// <summary>Removes the artist's cover rows for the listed songs; the artist's own songs are kept.</summary>
  Task<(bool success, string message, int removed, int kept)> RemoveSongsFromCatalogAsync(
      int artistId, IReadOnlyCollection<int> songIds, CancellationToken ct = default);
  ```

  - The empty-list check stays outside the lambda (REQ-UOW-41).
  - One `ExecuteAsync`: resolve `IArtistRepository` (name, unknown-artist case) and
    `ICatalogRepository` from `sp`, then call `RemoveCoversAsync`.
  - `success` is `removed > 0`. The message follows the REQ-CATPROP-07 table exactly. It is built in
    the service, because the wording rules are business output; the VM only displays it.

## 5. UI — `MyVocaList/UI/ViewModels/SongsViewModel.cs` (REQ-CATPROP-08)

- **`BuildDeleteConfirmMessage`** (abstract hook of `CrudListViewModelBase`, overridden per VM): in
  catalog mode it returns `Remove '<title>' from <ArtistName>'s catalog?` or
  `Remove <n> songs from <ArtistName>'s catalog?`. Otherwise it is unchanged.
- **`ExecuteDeleteAsync`**: `if (IsCatalogMode)` → `_catalogService.RemoveSongsFromCatalogAsync(ArtistId, ids)`.
  - Call `RefreshAsync()` on both outcomes, then show the message: `ShowSuccessAsync` when `success`,
    else the error snackbar. This diverges on purpose from the song path, which refreshes only on
    success.
  - The selection is cleared by `CrudListViewModelBase.RequestDelete` after `ExecuteDeleteAsync`
    returns, on both paths. That is the guarantee; `RefreshAsync` also clears it after a completed load.

  Outside catalog mode it calls `DeleteSongsAsync` as today.

  Selection-mode Delete and the item-actions Delete both end in `ExecuteDeleteAsync`, so both paths are
  covered by this one branch.
- **Remove the dead `RemoveFromCatalogCommand` / `RemoveFromCatalogAsync`.** Nothing binds to it.
  It is a VM-only command, not a governed component.
- **Not touched:** `CrudListViewModelBase` sets `ConfirmActionText = "Delete"` for every list. Changing it
  per mode is a governed-component change and is out of scope (requirements § Out of scope). This
  design needs no change to `CrudListView`, `CrudListViewModelBase` or any XAML.

## 6. ArtistService — REQ-CATPROP-10

- **`ArtistService.DeleteArtistsAsync`:**
  - Replace the `catalogRepository.CountByArtistAsync(id)` check with
    `sp.GetRequiredService<ISongRepository>().CountByArtistAsync(id)` (§ 4.1). The loop keeps today's
    shape: the first owner in selection order is refused.
  - The refusal text becomes AC-6.2's: `Cannot delete — {Name} owns N song(s). Delete their songs first.`
  - `ICatalogRepository` is no longer used by `ArtistService`. Remove the `_catalogRepository`
    constructor parameter. `UnitOfWorkReadScopeTests`' floor of ≥ 15 governed parameters still holds
    with a wide margin; the count is recorded in the task-log. Test call sites of `new ArtistService(`
    change with it.
  - The catalog rows of a deletable (song-less) artist go through the existing Artist → Catalog
    cascade.
- **`ArtistsViewModel.BuildDeleteConfirmMessage`** (the VM override behind AC-6.3): returns
  `Delete '<Name>'? Their catalog entries will also be removed.` or
  `Delete <n> artists? Their catalog entries will also be removed.` This is a VM-only text change, not
  a governed component.

Recorded, not built: once the queue exists, an artist referenced by queue events can never be deleted
(Helder 2026-11-19, queue work).

## 7. Migration — REQ-CATPROP-09

New migration `BackfillDerivedCatalogRows` in `Infra/Migrations/` (generated with `dotnet ef migrations add`; the model snapshot is unchanged):

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
    => migrationBuilder.Sql("INSERT OR IGNORE INTO Catalog (ArtistId, SongId) SELECT ArtistId, Id FROM Songs;");

protected override void Down(MigrationBuilder migrationBuilder) { }
// Down is a deliberate no-op: derived rows cannot be told apart from rows a user added by hand (D4).
```

- `INSERT OR IGNORE` against the composite PK makes it idempotent and protects manual rows.
- It does not rebuild `Songs`, so the FTS triggers are not involved.
- A restored older backup gets the backfill from the startup `MigrateAsync` (`App.xaml.cs`).
- First-launch effect (D4, REQ-CATPROP-11): authors gain catalog entries and appear under `Performers`.

## 8. Unchanged on purpose

- `ArtistRepository` role filter (`PerformersOnly => a.CatalogEntries.Any()`) and `CatalogCountText`.
  REQ-CATPROP-11 is the expected result of the invariant.
- `AddSongToCatalogAsync`. It already refuses duplicates. Adding the author's own song is a no-op
  duplicate refusal, since the row exists.
- Catalog picker (Phase 15 stub), grouped grid and participation auto-fill are out of scope. A second
  row source fits this model: auto-filled rows would be ordinary cover rows (or derived, when the
  performer is the author), and nothing here assumes the song form is the only writer of cover rows.

## 9. Test map

| AC | Level | Test class (real SQLite unless noted) |
|----|-------|--------------------------------------|
| REQ-CATPROP-01 | A | `SongServiceUpdateIntegrationTests` (or a new `SongServiceCatalogPropagationTests`, integration): create via each path, including an inline new artist |
| REQ-CATPROP-02 | A | same class: title/lyrics update keeps the rows |
| REQ-CATPROP-03 | A | same class: A→B existing; A→new inline artist; B already has a cover row → one row; other rows untouched |
| REQ-CATPROP-04 | A | same class: forced failure (validation, `ArtistChoiceNeeded`, exception) leaves the catalog unchanged |
| REQ-CATPROP-05 | B | new `SongRepositoryTests.DeleteAsync_Song_RemovesDerivedAndCoverCatalogRows` |
| REQ-CATPROP-06 | A | `CatalogServiceTests` (Moq) for the three outcomes, plus `CatalogRepositoryTests.IsDerivedAsync` |
| REQ-CATPROP-07 | A | `CatalogRepositoryTests.RemoveCoversAsync` (mixed list, unknown ids, real SQLite proves the join-filtered `ExecuteDeleteAsync`) + `CatalogServiceTests`: one test per row of the outcome table |
| REQ-CATPROP-08 | A | new `SongsViewModelCatalogDeleteTests` (Moq services): catalog mode calls bulk remove and never `DeleteSongsAsync`; confirmation text (1 / n); success vs error snackbar by `success`; refresh on both paths. Songs mode unchanged |
| REQ-CATPROP-09 | B | new `Tests/Integration/Migrations/CatalogBackfillMigrationTests.cs`, following `SongMigrationTests` (`MigratedToLatestMinusAsync`, raw-SQL seed, migrate, `SqlQueryRaw`): missing row added, manual row not duplicated, re-run inserts nothing |
| REQ-CATPROP-10 | A | `ArtistServiceTests` (Moq): owner refused with AC-6.2 text, first owner named; artist with only catalog rows deleted. New `ArtistServiceDeleteIntegrationTests`: cover-only artist deleted with its rows, songs untouched. `ArtistsViewModelTests`: AC-6.3 confirmation texts. `SongRepositoryTests.CountByArtistAsync` |
| REQ-CATPROP-11 | B | `ArtistRepository` paged read with `PerformersOnly`: an author with no cover rows is returned |

The architecture test `UnitOfWorkReadScopeTests` must stay green. It guards the `sp` resolution of
`ICatalogRepository` in `SongService` and `CatalogService`, and of `ISongRepository` in `ArtistService`.

**Existing tests this spec changes.** These are spec-driven updates, written by the Tester role in the
task that owns the AC, never by the Builder during Green:
- `ArtistServiceTests` (the two tests mocking `ICatalogRepository.CountByArtistAsync` for the delete
  refusal) encode the behaviour REQ-CATPROP-10 replaces. They are rewritten against
  `ISongRepository.CountByArtistAsync` and the AC-6.2 text.
- SongService unit tests with a fake `sp` (`SongServiceTests`, `SongServiceSaveFromForm*Tests`,
  `SongServiceArtistFreezeTests`, …) each build their own `PassthroughUnitOfWork.Over(...)`, whose
  provider throws for an unregistered type. Each such call gets an `ICatalogRepository` mock added
  (plan Task 3a). This is test setup only; no assertion is changed and `UnitOfWorkMocks.cs` is not
  edited.
- `CatalogServiceTests`' "remove always succeeds" test encodes the behaviour REQ-CATPROP-06 replaces.
  It is rewritten.
- `SongsViewModel` tests that cover `RemoveFromCatalogCommand`, if any, are deleted with the command.
- `RemoveSongVersionMigrationTests.Up_RowCountAndForeignKeyRowsUnchanged_ForeignKeyCheckEmpty` migrated to
  latest and asserted the `Catalog` rows unchanged; the backfill (REQ-CATPROP-09) adds rows there. It now
  migrates to its own migration id only, with every assertion unchanged (added during C2, 2026-11-20).
- `VenueCatalogServiceReadScopeTests.GetPagedCatalogForArtistAsync_SeededFixture_ReturnsExpectedItemsAndTotalCount`
  seeded a song and then added it to its own author's catalog; with REQ-CATPROP-01 the row already exists, so the
  explicit add (now a refused duplicate) is dropped and every assertion is unchanged (added during C3b, 2026-11-20).

## 10. Files touched (for the plan; DRY Onion order)

1. **Domain:** `ICatalogRepository.cs`, `ICatalogService.cs`, `ISongRepository.cs`.
2. **Infra:** `CatalogRepository.cs`, `SongRepository.cs`, the new migration (sequential-only file).
3. **Services:** `SongService.cs`, `CatalogService.cs`, `ArtistService.cs`.
4. **UI:** `SongsViewModel.cs`, `ArtistsViewModel.cs` (text only).
5. **Tests** as in § 9.

No XAML, no governed component, no `MauiProgram.cs` change (no new types to register).
