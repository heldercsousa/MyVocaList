# Songs list search, one Deezer row per song, fixed Deezer artist, lyrics on first open — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task. Tasks are tracked in `tasks.md` in this folder.

> **Status: APPROVED 2026-11-16 under Helder's advance authorisation of the same day.** Section 8 lists the open points the planning pass raised and how each was decided (also in `design.md § 1.2`, C20 to C27); section 9 lists the fixes from the plan review. Where a task text differs from section 8 or 9, those sections win.

**Deezer API check (spec task 0):** the official reference is behind a login wall and could not be read. From a recorded real answer in `MyVocaList.Tests/Integration/Services/SongSuggestionTitleSearchRealStackTests.cs`: the default answer has 25 rows, paging uses `index`, and each row carries `artist.id`. A `limit` parameter is named only by unofficial sources, so it is **not sent** (section 8, item 1).

## 1. Header

- **Goal:** The Songs list finds songs by title or artist, Deezer shows one row per song with an artist that cannot be changed, and a Deezer song without lyrics gets and keeps them on first open.
- **Architecture:** Two pure rule classes in Domain decide "is a Deezer song" and the lyrics origin; one nullable `LyricsOrigin` column on `Songs`. `SongRepository.GetPagedAsync` becomes the form's full-text search in paged form when the query is non-blank. `SongSuggestionService` collapses Deezer rows before the library check. `SongService` refuses an artist change on a Deezer song and gains `EnsureLyricsAsync` (read, HTTP outside any scope, re-read and store). `SongFormViewModel` exposes `IsArtistFrozen` from the same condition that keeps the pick identity, and the page swaps the artist field for a plain `Label`.
- **Tech stack:** .NET MAUI (net10.0-android), C#, CommunityToolkit.Mvvm, EF Core + SQLite (FTS5 trigram `SongSearch`), DevExpress MAUI, xUnit + Moq.
- **Spec:** `Docs/Management/BusinessFeatures/artists-songs/changes/2026-11-16-songs-list-searches-title-and-artist-deezer-versions-collapsed-deezer-artist-frozen-lyrics-on-first-open/` (`requirements.md` REQ-SLD-01 to 31, `design.md`).

## 2. Global constraints

- Business logic lives in Services and Domain rules; the ViewModel decides nothing about origin or freeze.
- No C#-side normalisation for search or de-duplication; grouping uses ordinal equality only on Deezer's own `artist.id` and short title.
- No shared component is edited: `CrudListView`, `ListItem`, `TapToEditField`, `AutocompleteDXField`.
- English only. No native dialogs.
- XAML: edit one file, then build (`dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`).
- Every test's first comment line is `// [AC] REQ-SLD-NN: <criterion>`; one AC per test.
- Repository and migration tests run on a real SQLite temp file (`TestDbContextFactory.Create()`); never the in-memory provider, never a mocked DbContext.
- Service tests use Moq with `PassthroughUnitOfWork.Over(...)`. ViewModel tests never call `Shell.Current`.
- The Builder does not modify existing tests to make them pass. The only planned updates are the tests listed in section 7, each tied to a named task; updating them is part of that task because the spec's Supersedes table changes their AC. Section 8 decides the three cases the planning pass found outside the Supersedes table (items 1, 3, 4).
- Sequential-only files: `Infra/Migrations/*`, `Infra/AppDbContext.cs`, `MauiProgram.cs`, `GlobalUsings.cs`. This plan touches only `Infra/Migrations/*` (T3). No `GlobalUsings.cs` edit; use explicit `using`s.
- `dotnet ef` is a main-agent shell step (workflow Rule 2), not a Builder step.
- Work happens in a git worktree on a task branch from `develop` (suggested `feature/songs-list-deezer`, worktree `.claude/worktrees/songs-list-deezer`); tasks run in order in that one worktree.
- Commit after each task; `feat:`/`refactor:` commits carry the trailer `Item: songs-list-searches-title-and-artist-deezer-versions-collapsed-deezer-artist-frozen-lyrics-on-first-open`.
- Test commands: one class `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~<TestClassName>"`; full run `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj`. The repo docs give only the unfiltered form; the `--filter` form is standard xUnit usage.

## 3. Review focus

1. **Artist change slipping through on a Deezer song** (typed name that resolves to the same or another artist, or a catalog-mode query overwriting the artist): T8a, T10.
2. **Lyrics written over the user's text or lost at Save** (lookup on open racing Save, typed text, closed form): T9, T11.
3. **Stored lyrics not persisted**, because the context is `NoTracking` and a Moq test cannot see it: T9 real-stack test.
4. **List search pages overlapping or missing songs** (rank ties, count query differing from the page query, index table missing): T4.
5. **Deezer rows merged wrongly or hidden wrongly** (same title by two artists, missing artist id, only-live groups, a library song stored as "Live"): T7.

## 4. File map

| File | Action | Responsibility |
|---|---|---|
| `Contracts/Enums/LyricsOrigin.cs` | create | Enum `LyricsSource`, `User`, `NoneFound`; null means not recorded |
| `Contracts/DTOs/LyricsLookupResult.cs` | create | `LyricsLookupStatus` and `LyricsLookupResult` (Found with text, NoMatch, NoAnswer) |
| `Domain/Rules/SongIdentityRules.cs` | create | `IsDeezerSong` |
| `Domain/Rules/LyricsOriginRules.cs` | create | Origin on create, origin on update, "needs lookup on open" |
| `Domain/Resolution/SongFormSaveRequest.cs` | modify | New optional `PickLyricsLookup` |
| `Contracts/DTOs/MusicSearchResultDto.cs` | modify | New optional `ExternalArtistId` |
| `Domain/Constants/SearchConstants.cs` | modify | `RemoteSongFetchCount = 25` |
| `Domain/Entity/Song.cs` | modify | `LyricsOrigin? LyricsOrigin` |
| `Infra/EntityEFConfig/SongConfiguration.cs` | modify | Map the enum as a string |
| `Infra/Migrations/<timestamp>_AddSongLyricsOrigin.cs` (+ `.Designer.cs`, `AppDbContextModelSnapshot.cs`) | create / generated | Nullable column, no backfill |
| `Infra/Repository/SongRepository.cs` | modify | Paged full-text list search inside `GetPagedAsync` |
| `Services/ISongLyricsService.cs`, `Services/SongLyricsService.cs` | modify | `LookupLyricsAsync`; `FindLyricsAsync` wraps it |
| `Services/DeezerProvider.cs` | modify | Read `artist.id`; keep up to 25 track rows |
| `Services/SongSuggestionService.cs` | modify | Collapse song groups before the library check; blank version |
| `Contracts/DTOs/Suggestions/SongSuggestionDto.cs` | modify (doc comment only) | `Version` remark no longer says "normalised Deezer version" |
| `Domain/ServicesInterfaces/ISongService.cs` | modify | `EnsureLyricsAsync` |
| `Services/SongService.cs` | modify | Freeze check, origin on create and update, `EnsureLyricsAsync` |
| `MyVocaList/UI/ViewModels/SongFormViewModel.cs` | modify | `IsArtistFrozen`, no Version on pick, lookup on open, Save wait, pick outcome in request |
| `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` | modify | Plain artist `Label` when frozen |
| `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs` | modify | Cancel the lookup on open in `OnDisappearing` |
| Tests (new) | create | Named per task below |

Not changed: `SongsViewModel`, `ISongRepository`, `IMusicMetadataProvider`, `ILyricsProvider`, `LrclibLyricsProvider`, DI registration, `AppDbContext.cs`, `MauiProgram.cs`.

## 5. Tasks

Order: Domain/Contracts (T1, T2), Infra (T3, T4), Services (T5 to T9), ViewModel (T10, T11), Page (T12), close-out (T13). `[P]` means no shared file with the named task; with one worktree they still run in order.

---

### T1 — Lyrics origin types and rules (Domain/Contracts)
**Risk:** A for the rules, C for the records. **[P] with T2.**

**Files**
- Create: `Contracts/Enums/LyricsOrigin.cs`, `Contracts/DTOs/LyricsLookupResult.cs`, `Domain/Rules/SongIdentityRules.cs`, `Domain/Rules/LyricsOriginRules.cs`
- Modify: `Domain/Resolution/SongFormSaveRequest.cs`
- Test: `MyVocaList.Tests/Unit/Domain/LyricsOriginRulesTests.cs`, `MyVocaList.Tests/Unit/Domain/SongIdentityRulesTests.cs`

**Consumes:** nothing.

**Produces**
```csharp
namespace MyVocaList.Contracts.Enums;
public enum LyricsOrigin { LyricsSource, User, NoneFound }          // null = not recorded

namespace MyVocaList.Contracts.DTOs;
public enum LyricsLookupStatus { NoAnswer = 0, NoMatch = 1, Found = 2 }
public readonly record struct LyricsLookupResult(LyricsLookupStatus Status, string? Lyrics = null)
{
    public static LyricsLookupResult NoAnswer { get; }               // default(LyricsLookupResult)
    public static LyricsLookupResult NoMatch { get; }
    public static LyricsLookupResult Found(string lyrics);
}

namespace MyVocaList.Domain.Rules;
public static class SongIdentityRules
{
    public static bool IsDeezerSong(string? externalProvider, string? externalId);   // both non-blank
}
public static class LyricsOriginRules
{
    public static LyricsOrigin? OnCreate(string? savedLyrics, LyricsLookupResult? pickLookup);
    public static LyricsOrigin? OnUpdate(string? storedLyrics, LyricsOrigin? storedOrigin, string? savedLyrics);
    public static bool NeedsLookupOnOpen(string? externalProvider, string? externalId, string? lyrics, LyricsOrigin? origin);
}

// SongFormSaveRequest: new last positional parameter
LyricsLookupResult? PickLyricsLookup = null
```
- `LyricsLookupResult` is a struct with `NoAnswer = 0` on purpose: a loose Moq mock of `ISongLyricsService` returns `default`, which then reads as "no answer" (three existing ViewModel test files pass `new Mock<ISongLyricsService>().Object`).
- Lyrics comparisons in the rules are ordinal on trimmed values, with null, empty and white-space treated as blank (assumption; the ViewModel already sends `Lyrics?.Trim()`).

**Steps (TDD)**
1. `SongIdentityRulesTests.IsDeezerSong_ProviderAndIdSet_True` — `// [AC] REQ-SLD-12`: `("Deezer","123")` is true.
2. `SongIdentityRulesTests.IsDeezerSong_EitherBlank_False` (Theory: null, "", " " on each side) — `// [AC] REQ-SLD-13`: false.
3. `LyricsOriginRulesTests.OnCreate_LyricsEqualFoundPickLookup_LyricsSource` — REQ-SLD-26: saved "A", `Found("A")` gives `LyricsSource`.
4. `OnCreate_LyricsDifferFromPickLookup_User` — REQ-SLD-26: saved "B", `Found("A")` gives `User`.
5. `OnCreate_LyricsWithoutPickLookup_User` — REQ-SLD-26: saved "B", null gives `User`.
6. `OnCreate_BlankLyricsAndNoMatch_NoneFound` — REQ-SLD-26.
7. `OnCreate_BlankLyricsAndNoAnswerOrNoLookup_Null` (Theory: `NoAnswer`, null) — REQ-SLD-26.
8. `OnCreate_NonBlankLyricsAndNoMatch_User` — REQ-SLD-26: rule 2 wins over rule 3.
9. `OnUpdate_LyricsChanged_User` (Theory over stored origin null, LyricsSource, User, NoneFound) — REQ-SLD-27.
10. `OnUpdate_LyricsEmptied_User` — REQ-SLD-27: stored "A", saved "" gives `User`.
11. `OnUpdate_LyricsUnchanged_KeepsOrigin` (Theory over the four origins, including null and blank on both sides) — REQ-SLD-27.
12. `NeedsLookupOnOpen_DeezerBlankLyricsNoOrigin_True` — REQ-SLD-19.
13. `NeedsLookupOnOpen_LocalOnlyOrHasLyricsOrHasOrigin_False` (Theory) — REQ-SLD-24.
14. For each: run, see it fail, implement, see it pass.
15. Commit `feat(domain): lyrics origin types and rules`.

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~LyricsOriginRulesTests|FullyQualifiedName~SongIdentityRulesTests"`

**Covers:** REQ-SLD-26, 27 (rules); 12, 13, 19, 24 (predicates).

---

### T2 — Deezer artist id on the search DTO and the fetch-count constant (Contracts/Domain)
**Risk:** C (record member and constant; no test, note it in the task-log). **[P] with T1.**

**Files:** modify `Contracts/DTOs/MusicSearchResultDto.cs`, `Domain/Constants/SearchConstants.cs`.

**Produces**
```csharp
// MusicSearchResultDto: new last positional parameter
string? ExternalArtistId = null
// SearchConstants
/// <summary>Rows asked from and grouped out of one Deezer track answer (REQ-SLD-10).</summary>
public const int RemoteSongFetchCount = 25;
```
**Steps:** add both, build, run the full suite (existing positional constructions must still compile), commit `feat(contracts): Deezer artist id on search rows and track fetch count`.

**Covers:** REQ-SLD-06, 10 (types only).

---

### T3 — `Song.LyricsOrigin`, EF mapping, migration `AddSongLyricsOrigin` (Infra, sequential)
**Risk:** B. **Not parallel** (sequential-only files). After T1.

The entity property, the mapping and the migration land in one commit; otherwise `MigrateAsync`-based tests fail on pending model changes.

**Files**
- Modify: `Domain/Entity/Song.cs`, `Infra/EntityEFConfig/SongConfiguration.cs`
- Create (generated by the main agent): `Infra/Migrations/<timestamp>_AddSongLyricsOrigin.cs`, `.Designer.cs`; updated `Infra/Migrations/AppDbContextModelSnapshot.cs`
- Test: `MyVocaList.Tests/Integration/Migrations/LyricsOriginMigrationTests.cs` (model on `ImageUrlMigrationTests`)

**Consumes:** `LyricsOrigin` (T1).

**Produces**
```csharp
// Song
/// <summary>Where the stored lyrics came from; null = not recorded (REQ-SLD-26..28).</summary>
public LyricsOrigin? LyricsOrigin { get; set; }
// SongConfiguration (pattern: BackupHistoryConfiguration)
builder.Property(s => s.LyricsOrigin).IsRequired(false).HasConversion<string>().HasMaxLength(20);
```
Migration `AddSongLyricsOrigin`:
- `Up`: `AddColumn<string>("LyricsOrigin", "Songs", nullable: true, maxLength: 20)`; no default, no backfill. Accept the collation the generator emits.
- `Down`: `migrationBuilder.Sql("ALTER TABLE \"Songs\" DROP COLUMN \"LyricsOrigin\";")`. EF's `DropColumn` rebuilds the table and would drop the seven FTS triggers (constraints-reference § EF Core / SQLite; same pattern as `20261113032549_AddImageUrlToSongsAndArtists`).

Workflow verified in `.claude/library/database-setup.md`:
- Generate: `dotnet ef migrations add AddSongLyricsOrigin -p MyVocaList.Infra`. The project folder is `Infra/`, so the working form may be `-p Infra/MyVocaList.Infra.csproj`; check before running.
- Verify: `dotnet ef migrations has-pending-changes -p MyVocaList.Infra --exit-code`.
- A hook syncs `.claude/MyVocaList.db`.
- Latest migration today: `20261114133332_AddVenueProfile`.
- The `__EFMigrationsLock` handling is app start-up code and is not touched.

**Steps (TDD)**
1. `LyricsOrigin_ColumnExistsNullableText` — `// [AC] REQ-SLD-28`: `pragma_table_info('Songs')` has `LyricsOrigin`, `notnull = 0`.
2. `LyricsOrigin_SongStoredBeforeTheMigration_ReadsNull` — REQ-SLD-28: migrate to `AddVenueProfile`, insert an artist and a song by raw SQL, migrate to latest, read back `LyricsOrigin == null`.
3. `LyricsOrigin_EachValueRoundTrips` (Theory) — REQ-SLD-22: saved value reads back equal and the raw column text equals the enum name.
4. Run and see red; add property and mapping; main agent generates the migration; hand-edit `Down`; run green.
5. Also run `SongMigrationTests` (guard `FullMigrate_AllSevenFtsTriggersExist`) and `has-pending-changes`.
6. Commit `feat(infra): lyrics origin column on songs`.

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~LyricsOriginMigrationTests|FullyQualifiedName~SongMigrationTests"`

**Covers:** REQ-SLD-28; storage for 20, 22, 26, 27.

---

### T4 — Paged list search in `SongRepository.GetPagedAsync` (Infra)
**Risk:** B. **[P] with T3, T5, T6** (only `SongRepository.cs`).

**Design decision, deviating from the wording of design § 3.3/3.4 but not from any REQ (section 8, item 2):** no new `ISongRepository` member and no change in `SongService.GetPagedSongsForListAsync`. `GetPagedAsync(pageNumber, pageSize, query, ct)` keeps its signature and switches behaviour for a non-blank query.

**Files**
- Modify: `Infra/Repository/SongRepository.cs`
- Test: `MyVocaList.Tests/Integration/Repositories/SongRepositoryListSearchTests.cs` (schema by `MigrateAsync`, as `SongRepositorySearchTests`)

**Consumes:** `FtsQueryBuilder.BuildTrigramMatch(string)` (exists), `SongListItemDto` (exists).

**Produces (private to `SongRepository`)**
```csharp
private const string ListSearchSql;       // SELECT s.Id AS Id, s.Title AS Title, s.ArtistId AS OriginalArtistId, a.Name AS OriginalArtistName,
                                          //        s.FeaturedArtists, s.ExternalProvider, s.Version, COALESCE(s.ImageUrl, a.ImageUrl) AS ImageUrl
                                          // FROM SongSearch JOIN Songs s ON s.Id = SongSearch.rowid JOIN Artists a ON a.Id = s.ArtistId
                                          // WHERE SongSearch MATCH @m
                                          // ORDER BY bm25(SongSearch, 10.0, 1.0), length(s.Title), s.Id
                                          // LIMIT @take OFFSET @skip
private const string ListSearchCountSql;  // SELECT count(*) AS Value FROM SongSearch WHERE SongSearch MATCH @m
private Task<(IEnumerable<SongListItemDto> items, int totalCount)> SearchPagedAsync(
    int pageNumber, int pageSize, string query, CancellationToken ct);
```
Behaviour of `GetPagedAsync`:
- Blank query: today's code, unchanged.
- Non-blank and `BuildTrigramMatch` returns null: today's collated title-prefix query, ordered by title, with `ThenBy(s => s.Id)` added so pages are disjoint.
- Non-blank with a match string: the two SQL statements above.
- `SqliteException` "no such table": log and run the prefix query (spec failure mode).

If `SqlQueryRaw<SongListItemDto>` cannot bind the record because of its computed `SupportingText`, use a private row record with the same columns and map it.

**Steps (TDD)**
1. `GetPagedAsync_QueryMatchesArtistName_ReturnsThatArtistsSongs` — `// [AC] REQ-SLD-01`: query "guns" returns "Don't Cry" and "Sweet Child O' Mine".
2. `GetPagedAsync_ShortQuery_MatchesTitlePrefixOnly` — REQ-SLD-01: "ab" returns "Abba Song" and no song found only through its artist.
3. `GetPagedAsync_IndexTableMissing_FallsBackToTitlePrefix` — REQ-SLD-01: `EnsureCreatedAsync` database, query "abba" returns "Abba Song", no exception.
4. `GetPagedAsync_FullTextQuery_OrdersByRankThenTitleLengthThenId` — REQ-SLD-02: for "cry", the title hit "Cry" precedes artist-only hits; two songs with the same title and rank come in id order.
5. `GetPagedAsync_ShortQuery_OrdersByTitle` — REQ-SLD-02.
6. `GetPagedAsync_FullTextQuery_PagesAreDisjointAndCoverEveryMatch` — REQ-SLD-03: seed 45 matching songs, load pages 1 to 3 of size 20; ids are distinct, count 45, `totalCount == 45` on every page.
7. `GetPagedAsync_QueryMatchesNothing_ReturnsEmptyAndZeroCount` — REQ-SLD-04.
8. `GetPagedAsync_SearchRow_UsesArtistImageWhenSongHasNone` — REQ-SLD-01: `ImageUrl` equals the artist's when the song's is null.
9. Red, implement, green. Also run `SongRepositoryTests`, `SongListItemProjectionTests`, `SongRepositorySearchTests` and `SongServiceTests`; all must pass unchanged.
10. Commit `feat(infra): songs list search over title and artist`.

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~SongRepositoryListSearchTests|FullyQualifiedName~SongRepositoryTests|FullyQualifiedName~SongListItemProjectionTests"`

**Covers:** REQ-SLD-01, 02, 03, 04. REQ-SLD-05: nothing touched.

`HasMoreItems` (verified in `CrudListViewModelBase`): first page `totalCount > list.Count`, later pages `(list.Count + Items.Count) < _totalCount`. The count query therefore uses the same `MATCH` as the page query.

---

### T5 — `LookupLyricsAsync` on the lyrics service (Services)
**Risk:** A. **[P] with T3, T4, T6.** After T1.

**Files**
- Modify: `Services/ISongLyricsService.cs`, `Services/SongLyricsService.cs`
- Test: `MyVocaList.Tests/Unit/Services/SongLyricsServiceLookupTests.cs` (reuse the `SetupAnswers` pattern of `SongLyricsServiceTests`)
- Not touched: `SongLyricsServiceTests.cs`, `ILyricsProvider`, `LrclibLyricsProvider`.

**Produces**
```csharp
/// <summary>As FindLyricsAsync, but tells a definite "no match" from "no answer" (REQ-SLD-22, 23).</summary>
Task<LyricsLookupResult> LookupLyricsAsync(LyricsQuery query, CancellationToken ct = default);
// FindLyricsAsync becomes: (await LookupLyricsAsync(query, ct)).Lyrics
```
Mapping:

| Situation | Result |
|---|---|
| Provider returns null (404, instrumental, blank `plainLyrics`, null body) after the existing retry logic | `NoMatch` |
| Text blank after trim, or longer than `LyricsMaxLength` | `NoMatch` |
| Blank title or artist (no request), offline, 5,000 ms bound reached, provider exception | `NoAnswer` |
| Usable text | `Found(trimmed)` |
| Caller cancellation | rethrown |

**Steps (TDD)**
1. `LookupLyricsAsync_Match_ReturnsFoundWithTrimmedLyrics` — `// [AC] REQ-SLD-20`.
2. `LookupLyricsAsync_ProviderHasNoRecord_ReturnsNoMatch` — REQ-SLD-22.
3. `LookupLyricsAsync_BlankAnswer_ReturnsNoMatch` — REQ-SLD-22.
4. `LookupLyricsAsync_TextOverTheLimit_ReturnsNoMatch` — REQ-SLD-22.
5. `LookupLyricsAsync_Offline_ReturnsNoAnswerWithoutProviderCall` — REQ-SLD-23.
6. `LookupLyricsAsync_ProviderThrows_ReturnsNoAnswer` — REQ-SLD-23.
7. `LookupLyricsAsync_BoundReached_ReturnsNoAnswer` — REQ-SLD-23 (about 5 s, as the existing bound test).
8. `LookupLyricsAsync_TitleAndArtistOnly_SendsOneRequest` — REQ-SLD-29.
9. `LookupLyricsAsync_CallerCancelled_Rethrows` — REQ-SLD-23.
10. Red, implement, green; run `SongLyricsServiceTests` unchanged (16 tests).
11. Commit `feat(services): lyrics lookup result with definite no-match`.

Instrumental and 404 are already covered at provider level in `LrclibLyricsProviderTests` (both return null); no new provider test.

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~SongLyricsService"`

**Covers:** REQ-SLD-22, 23, 29 (service part); 20 (Found).

---

### T6 — `DeezerProvider`: artist id and up to 25 track rows (Services)
**Risk:** B. **[P] with T4, T5.** After T2.

**Files**
- Modify: `Services/DeezerProvider.cs`
- Test: `MyVocaList.Tests/Unit/Services/DeezerProviderArtistIdTests.cs` (stub-handler pattern of `DeezerProviderDurationTests`)

**Consumes:** `MusicSearchResultDto.ExternalArtistId`, `SearchConstants.RemoteSongFetchCount`.

**Produces**
- `SearchSongsAsync` keeps its request URL (`search/track?q={escaped}`, no `limit`; section 8, item 1) and keeps at most `SearchConstants.RemoteSongFetchCount` rows of the answer.
- Each row carries `ExternalArtistId = artist.id` as invariant text; null when the artist object or its id is absent.
- `SearchArtistsAsync` keeps `MaxResults = 5` and its URL.
- `IMusicMetadataProvider.SearchSongsAsync` signature is unchanged; a count parameter would break every existing Moq setup of it.
- Private record change: `DeezerTrackArtist([property: JsonPropertyName("id")] long? Id, string Name, string? PictureMedium)`.

**Steps (TDD)**
1. `SearchSongsAsync_RowWithArtistId_CarriesItAsText` — `// [AC] REQ-SLD-06`: `"artist":{"id":5288900,...}` gives `"5288900"`.
2. `SearchSongsAsync_RowWithoutArtistId_CarriesNull` (Theory: artist without id, no artist object) — REQ-SLD-11.
3. `SearchSongsAsync_UrlIsUnchanged` — REQ-SLD-10: `PathAndQuery == "/search/track?q=creep"`.
4. `SearchSongsAsync_AnswerOfThirtyRows_KeepsTwentyFive` — REQ-SLD-10.
5. `SearchArtistsAsync_StillAsksPlainQueryAndKeepsFive` — REQ-SLD-10.
6. Red, implement, green; no existing test is edited; run every `DeezerProvider*` class.
7. Commit `feat(services): Deezer track search reads the artist id and keeps up to 25 rows`.

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~DeezerProvider"`

**Covers:** REQ-SLD-10; 06 and 11 (data).

---

### T7 — One row per song group in `SongSuggestionService` (Services)
**Risk:** A. After T6.

**Files**
- Modify: `Services/SongSuggestionService.cs`, `Contracts/DTOs/Suggestions/SongSuggestionDto.cs` (doc comment)
- Test: `MyVocaList.Tests/Unit/Services/SongSuggestionServiceGroupingTests.cs` (fixture as `SongSuggestionServiceTests`: Moq provider, `PassthroughUnitOfWork.Over`)
- Existing tests to update in this task: three (section 7).

**Consumes:** `MusicSearchResultDto.ExternalArtistId`, `SearchConstants.RemoteSongFetchCount`.

**Produces (private)**
```csharp
private static List<MusicSearchResultDto> CollapseSongGroups(IReadOnlyList<MusicSearchResultDto> rows);
```
- Called in `SearchTitleRemoteCoreAsync` on `raw.Take(RemoteSongFetchCount)`, before `DedupAgainstLibraryAsync`.
- Group key: `(ExternalArtistId, SongTitle)` with `StringComparer.Ordinal`.
- A row with blank `ExternalArtistId`, or with a null title, is its own group.
- Kept row: first row of the group with a blank `Version`, else the first row.
- The kept row takes the position of the group's first row.
- Every output row is `row with { Version = string.Empty }`.
- The existing `Take(MaxSearchResults)` stays after the library check.

Verified in code: the option row text comes from `SongFormViewModel.ToTitleOption` (parts artist, version, album joined by " · ", `TrailingText = "Deezer"`). A blank version drops the middle part with no template change. The album part stays (section 8, item 8).

**Steps (TDD)**
1. `SearchTitleRemoteAsync_AlbumRepeatsOfOneSong_OneRow` — `// [AC] REQ-SLD-06`: three rows (artist "1", "Creep") give one remote row.
2. `SearchTitleRemoteAsync_SameTitleTwoArtists_TwoRows` — REQ-SLD-06.
3. `SearchTitleRemoteAsync_TitlesDifferingOnlyInCase_TwoRows` — REQ-SLD-06: "Creep" and "creep" stay apart.
4. `SearchTitleRemoteAsync_GroupsKeepDeezerOrder` — REQ-SLD-06: order of first appearance.
5. `SearchTitleRemoteAsync_LiveThenPlain_KeepsThePlainRow` — REQ-SLD-07: kept `ExternalId` is the plain row's.
6. `SearchTitleRemoteAsync_OnlyLabelledRows_KeepsTheFirst` — REQ-SLD-07.
7. `SearchTitleRemoteAsync_KeptRow_HasBlankVersion` — REQ-SLD-08: `Version == ""` for an only-live group.
8. `SearchTitleRemoteAsync_LibraryCheckUsesBlankVersion` — REQ-SLD-09: `FindByTitleAndArtistNamesAsync` is called with `Triple("Creep","Radiohead","")` and never with "Live".
9. `SearchTitleRemoteAsync_LibrarySongWithBlankVersion_HidesTheGroup` — REQ-SLD-09: promoted, no remote row.
10. `SearchTitleRemoteAsync_LibrarySongOnlyAsLive_GroupStaysRemote` — REQ-SLD-09.
11. `SearchTitleRemoteAsync_TwentyFiveRowsOfSixSongs_ShowsFive` — REQ-SLD-10.
12. `SearchTitleRemoteAsync_RowsWithoutArtistId_NeverMerged` — REQ-SLD-11: two rows with the same title and null id give two rows.
13. Red, implement, green; update the three existing tests (section 7); run all `SongSuggestion*` classes.
14. Commit `feat(services): one Deezer row per song`.

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~SongSuggestion"`

**Covers:** REQ-SLD-06, 07, 08 (service part), 09, 10, 11.

---

### T8a — Service refuses an artist change on a Deezer song (Services)
**Risk:** A. After T1. Same file as T8b and T9, so sequential.

**Files**
- Modify: `Services/SongService.cs`
- Test: `MyVocaList.Tests/Unit/Services/SongServiceArtistFreezeTests.cs` (fixture as `SongServiceSaveFromFormTests`)

**Consumes:** `SongIdentityRules.IsDeezerSong`.

**Produces**
```csharp
private const string DeezerArtistFrozenMessage = "The artist of a song from the music database cannot be changed.";
```
Verified today: in edit mode `SaveFromFormAsync` runs `ResolveFormArtistAsync` in a read scope (picked id, then `UseExisting`, then name validation, then `IArtistResolutionService`), then calls `UpdateSongCoreAsync`, which loads the song and resolves or creates the artist.

Two changes:
1. `SaveFromFormAsync`, edit mode, first thing inside the existing `ExecuteReadAsync` lambda, before `ResolveFormArtistAsync`:
   - Load the song with `sp.GetRequiredService<ISongRepository>().GetByIdAsync(songId, ct)`.
   - If it is non-null and `IsDeezerSong(song.ExternalProvider, song.ExternalId)`:
     - `request.ArtistId == song.ArtistId` gives `SaveFormResolution.Proceed(song.ArtistId, null, fillArtistImage: false)` with no resolution call (REQ-SLD-30).
     - Anything else gives `SaveFormResolution.Stop(SaveFailed(DeezerArtistFrozenMessage, SaveField.Artist))`.
   - `ArtistText` is ignored when the id matches, as today.
   - A null song falls through to today's path ("Song not found" from the write).
2. `UpdateSongCoreAsync`, right after the song is loaded and before the identity check and any artist creation: when it is a Deezer song and (`hasNewArtistName` or `artistId` is set and differs from `song.ArtistId`), return `(false, DeezerArtistFrozenMessage, SaveField.Artist)`. This covers `UpdateSongAsync` whatever the caller sends.

**Steps (TDD)**
1. `SaveFromForm_DeezerSongOtherArtistId_RefusedOnArtist` — `// [AC] REQ-SLD-15`: `Failed`, the exact message, `SaveField.Artist`, `UpdateAsync` never.
2. `SaveFromForm_DeezerSongTypedArtistName_RefusedBeforeAnyResolution` — REQ-SLD-15: `ArtistId` null plus text gives Failed; `IArtistResolutionService.ResolveAsync` never; `CreateArtistAsync` never.
3. `SaveFromForm_DeezerSongCreateTypedDecision_Refused` — REQ-SLD-15.
4. `SaveFromForm_DeezerSongTypedNameOfTheStoredArtist_StillRefused` — REQ-SLD-15: ids are compared, never names.
5. `UpdateSongAsync_DeezerSongOtherArtistIdOrNewName_Refused` (Theory) — REQ-SLD-15: `(false, message)`, song object unchanged.
6. `SaveFromForm_DeezerSongStoredArtistId_SavesWithoutArtistResolution` — REQ-SLD-30: Saved; `ResolveAsync` never; `ValidateNameInput` never.
7. `SaveFromForm_AddModeUseExistingArtist_StoresDeezerSongWithThatArtist` — REQ-SLD-16: `AddedSong.ExternalId` is the Deezer id and `ArtistId` is the chosen one.
8. `SaveFromForm_DeezerSongTitleChanged_Saved` — REQ-SLD-17.
9. `SaveFromForm_SaveAsNewVersionOrNewSong_StoresNoIdentity` (Theory) — REQ-SLD-18.
10. `SaveFromForm_LocalOnlySongOtherArtist_Saved` — REQ-SLD-13.
11. Red, implement, green; run every `SongService*` class, including `Integration/UnitOfWork/SongServiceReadScopeTests`.
12. Commit `feat(services): the artist of a Deezer song cannot be changed`.

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~SongService"`

**Covers:** REQ-SLD-15, 16, 17, 18, 30 (service part), 13 (service part).

---

### T8b — Lyrics origin on create and update (Services)
**Risk:** A. After T1, T3, T8a.

**Files**
- Modify: `Services/SongService.cs`
- Test: `MyVocaList.Tests/Unit/Services/SongServiceLyricsOriginTests.cs`

**Consumes:** `LyricsOriginRules.OnCreate/OnUpdate`, `Song.LyricsOrigin`, `SongFormSaveRequest.PickLyricsLookup`.

**Produces**
- `CreateSongWithUrlsCoreAsync(..., bool fillArtistImage = false, LyricsLookupResult? pickLyricsLookup = null)` sets `LyricsOrigin = LyricsOriginRules.OnCreate(lyrics, pickLyricsLookup)`.
- `SaveFromFormAsync` passes `request.PickLyricsLookup`, also when the identity is nulled by a "save as new" decision (REQ-SLD-18, 26).
- `CreateSongAsync` sets `OnCreate(lyrics, null)`.
- `UpdateSongCoreAsync`, before `song.Lyrics = lyrics`: `song.LyricsOrigin = LyricsOriginRules.OnUpdate(song.Lyrics, song.LyricsOrigin, lyrics)`. The comparison is with the row read inside the update, not with what the form loaded.

**Steps (TDD)**
1. `SaveFromForm_AddLyricsEqualThePickLookup_OriginLyricsSource` — `// [AC] REQ-SLD-26`.
2. `SaveFromForm_AddTypedLyrics_OriginUser` — REQ-SLD-26.
3. `SaveFromForm_AddBlankLyricsAndNoMatch_OriginNoneFound` — REQ-SLD-26.
4. `SaveFromForm_AddBlankLyricsAndNoLookup_OriginNotRecorded` — REQ-SLD-26.
5. `SaveFromForm_SaveAsNewSongWithPickLookup_OriginStillDerived` — REQ-SLD-18: identity null, origin `LyricsSource`.
6. `SaveFromForm_EditLyricsChanged_OriginUser` — REQ-SLD-27.
7. `SaveFromForm_EditLyricsEmptied_OriginUser` — REQ-SLD-27.
8. `SaveFromForm_EditLyricsUnchanged_OriginKept` (Theory: null, LyricsSource, NoneFound) — REQ-SLD-27.
9. `UpdateSongAsync_LyricsChanged_OriginUser` — REQ-SLD-27.
10. Red, implement, green; run every `SongService*` class.
11. Commit `feat(services): record where stored lyrics came from`.

**Covers:** REQ-SLD-26, 27, 18 (origin part); the stored outcomes of 31.

---

### T9 — `EnsureLyricsAsync` (Services)
**Risk:** A. After T5, T8b.

**Files**
- Modify: `Domain/ServicesInterfaces/ISongService.cs`, `Services/SongService.cs`
- Test: `MyVocaList.Tests/Unit/Services/SongServiceEnsureLyricsTests.cs` (Moq; pass `Mock<ISongLyricsService>.Object` as the new last constructor argument)
- Test: one added test in `MyVocaList.Tests/Unit/DependencyInjection/AppServicesRegistrationTests.cs`
- Test: `MyVocaList.Tests/Integration/Services/SongServiceEnsureLyricsRealStackTests.cs` (`UnitOfWorkTestHost.Create` with a stub `ILyricsProvider` and an online `INetworkStatus`, pattern of `SongSuggestionTitleSearchRealStackTests`)

**Consumes:** `ISongLyricsService.LookupLyricsAsync`, `LyricsOriginRules.NeedsLookupOnOpen`, `Song.LyricsOrigin`, `LyricsQuery(title, artistName)`.

**Produces**
```csharp
/// <summary>
/// Lookup on open (REQ-SLD-19..24): for a Deezer song with blank lyrics and no recorded origin, looks the lyrics up
/// with the stored title and artist name and stores a match (origin LyricsSource) or a definite no-match
/// (origin NoneFound). Returns the lyrics now stored on the song, or null.
/// </summary>
/// <remarks>Never throws except OperationCanceledException when the caller's token is cancelled before the answer arrives.</remarks>
Task<string?> EnsureLyricsAsync(int songId, CancellationToken ct = default);
```
Verified unit-of-work behaviour (`Infra/UnitOfWork/UnitOfWork.cs`):
- `ExecuteReadAsync` opens a DI scope and no transaction.
- `ExecuteAsync<T>` opens a scope and a transaction, and skips the save when the leading tuple `bool` is false.
- The context is `NoTracking` (`AppDbContext.cs:38,55`), so a write needs `ISongRepository.UpdateAsync` after `song.OriginalArtist = null` (the BUG-067 pattern in `UpdateSongCoreAsync`).

Three steps:
1. **Read scope** (`_uow.ExecuteReadAsync`):
   - Load the song with `GetByIdAsync`.
   - Check the preconditions in order: exists, `NeedsLookupOnOpen(...)`, title non-blank, `OriginalArtist.Name` non-blank. Any failure returns null with no request.
   - Return title and artist name.
   - `ISongLyricsService` comes from the optional constructor parameter (section 8, item 5); when it is null the method returns null before any read.
2. **Outside any scope:** `await _lyricsService.LookupLyricsAsync(new LyricsQuery(title, artistName), ct)`. No scope, context or transaction is open. Caller cancellation propagates. `NoAnswer` returns null.
3. **Write scope** (`_uow.ExecuteAsync<(bool success, string message, string? lyrics)>` with `CancellationToken.None`):
   - Re-read the song.
   - If its lyrics are no longer blank or its origin is no longer null, return `(false, "", song.Lyrics)`. Nothing is saved and the stored lyrics are still returned.
   - `Found`: set `Lyrics` and `LyricsOrigin = LyricsSource`, call `UpdateAsync`, return the lyrics.
   - `NoMatch`: set `LyricsOrigin = NoneFound`, call `UpdateAsync`, return null.
   - `UpdatedAt` is not touched.
   - A storage exception is logged and swallowed (returns null).

**Steps (TDD)** — unit, Moq
1. `EnsureLyricsAsync_DeezerSongBlankLyricsNoOrigin_LooksUpWithStoredTitleAndArtist` — `// [AC] REQ-SLD-19`: `LookupLyricsAsync` once with `LyricsQuery("Stored Title","Queen", null, null)`.
2. `EnsureLyricsAsync_Found_StoresLyricsWithOriginLyricsSource` — REQ-SLD-20: `UpdateAsync` once with both values; returns the lyrics.
3. `EnsureLyricsAsync_SongChangedMeanwhile_StoresNothing` (Theory: lyrics now non-blank; origin now `User`) — REQ-SLD-20: the second `GetByIdAsync` returns the changed song; `UpdateAsync` never.
4. `EnsureLyricsAsync_CancelledAfterTheAnswer_StillStores` — REQ-SLD-20: the token is cancelled inside the lookup callback after the answer is set; `UpdateAsync` once.
5. `EnsureLyricsAsync_NoMatch_StoresOriginNoneFoundAndReturnsNull` — REQ-SLD-22.
6. `EnsureLyricsAsync_NoAnswer_StoresNothing` — REQ-SLD-23.
7. `EnsureLyricsAsync_CancelledDuringTheLookup_RethrowsAndStoresNothing` — REQ-SLD-23.
8. `EnsureLyricsAsync_OutOfScopeSong_StartsNoLookup` (Theory: local-only, has lyrics, origin `User`, origin `NoneFound`, song not found, blank artist name) — REQ-SLD-24: `LookupLyricsAsync` never.
9. `EnsureLyricsAsync_LookupRunsOutsideAnyUnitOfWork` — REQ-SLD-20: a recording `IUnitOfWork` test double shows the lookup callback runs while no `ExecuteReadAsync`/`ExecuteAsync` body is active.

**Steps (TDD)** — real stack
10. `EnsureLyricsAsync_Match_IsPersistedAndReadBack` — REQ-SLD-20: a fresh context reads `Lyrics == "A"` and `LyricsOrigin == LyricsSource`; Title, ArtistId and `UpdatedAt` are unchanged.
11. `EnsureLyricsAsync_SecondCall_ReturnsStoredLyricsWithoutARequest` — REQ-SLD-24: the stub provider is called once across two calls; the second call returns null (preconditions fail). See section 8, item 9.
12. Red, implement, green; full `SongService*` run.
13. Commit `feat(services): lyrics of a Deezer song on first open`.

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~SongServiceEnsureLyrics"`

**Covers:** REQ-SLD-19 (service part), 20, 22, 23, 24, 29.

---

### T10 — `SongFormViewModel`: frozen artist, no Version on a pick (ViewModel)
**Risk:** A. After T1, T7, T8a. Same file as T11, so sequential.

**Files**
- Modify: `MyVocaList/UI/ViewModels/SongFormViewModel.cs`
- Test: `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelFrozenArtistTests.cs` (factory and helpers as `SongFormViewModelPictureTests`)
- Existing tests to update in this task: three (section 7).

**Consumes:** `SongIdentityRules.IsDeezerSong`.

Verified today:
- `BuildSaveRequest` computes `keepsIdentity = !IsEditMode && string.Equals(SongTitle, _pickedTitle, StringComparison.Ordinal)` and sends provider and id only when `keepsIdentity` and both are non-empty.
- `ClearTitle` sets `_pickedTitle = null` and empties the identity.
- A changed Title text makes `keepsIdentity` false while the identity fields stay set.
- Typing the picked title back makes it true again (section 8, item 6).

**Produces**
```csharp
/// <summary>Add mode: a Save now would store the pick's external identity (REQ-PIC-27, REQ-SLD-14).</summary>
private bool HoldsDeezerPick =>
    !IsEditMode && SongIdentityRules.IsDeezerSong(SelectedProvider, SelectedExternalId)
    && string.Equals(SongTitle, _pickedTitle, StringComparison.Ordinal);

/// <summary>REQ-SLD-12/14: the Artist is plain text. Edit mode: the loaded song is a Deezer song. Add mode: the pick is held.</summary>
public bool IsArtistFrozen => IsEditMode ? _storedIsDeezerSong : HoldsDeezerPick;
public bool IsArtistEditable => !IsArtistFrozen;      // for the page, no converter needed

private bool _storedIsDeezerSong;   // set in LoadSongForEditAsync from the loaded entity
private int _storedArtistId;        // set in LoadSongForEditAsync
```
- `BuildSaveRequest` uses `HoldsDeezerPick` in place of `keepsIdentity`, so the screen and the request share one condition.
- Edit mode and frozen: the request carries `ArtistId = _storedArtistId`, `ArtistText = null`, `ArtistDecision.None` (REQ-SLD-30).
- Edit-mode frozen comes from the loaded entity, not from `SelectedExternalId`, because X on Title in edit mode empties `SelectedExternalId`.
- `InitializeArtistField()` returns early when `IsEditMode && _storedIsDeezerSong`; otherwise the `artistId`/`artistName` query values of catalog mode could overwrite the stored artist after the load.
- Raise `PropertyChanged` for `IsArtistFrozen` and `IsArtistEditable` in `OnSongTitleChanged`, `ApplyRemotePick`, `ClearTitle`, `ClearArtist`, `OnSongIdChanged` and at the end of `LoadSongForEditAsync`.
- `ApplyRemotePick` no longer writes `SongVersion`. Remove `_pickFilledVersion` and its reset in `ClearTitle`.
- `IsArtistLocked` handling is unchanged (still set true on a pick and on load).

**Steps (TDD)**
1. `DeezerPick_ArtistIsFrozen` — `// [AC] REQ-SLD-12`: after a remote pick `IsArtistFrozen` is true and `IsArtistEditable` false.
2. `EditMode_DeezerSong_ArtistIsFrozen` — REQ-SLD-12.
3. `AddMode_NoPick_ArtistIsEditable` — REQ-SLD-13.
4. `EditMode_LocalOnlySong_ArtistIsEditable` — REQ-SLD-13.
5. `ClearTitle_AfterDeezerPick_ReleasesTheArtistAndKeepsItsText` — REQ-SLD-14.
6. `TitleTextChanged_AfterDeezerPick_ReleasesTheArtistAndKeepsItsText` — REQ-SLD-14: `SongTitle = "Creep (Acoustic)"`; frozen false; `ArtistSearchText == "Radiohead"`.
7. `NewDeezerPick_AfterRelease_FreezesAgain` — REQ-SLD-14.
8. `IsArtistFrozen_RaisesPropertyChangedOnPickAndRelease` — REQ-SLD-14.
9. `FrozenState_MatchesWhetherSaveSendsTheIdentity` (Theory: pick; pick then title change; pick then X) — REQ-SLD-14: `IsArtistFrozen == (request.ExternalId is not null)`.
10. `DeezerPick_LeavesTheVersionAsTheUserLeftIt` (Theory: typed "Demo" with a DTO version "Live" gives "Demo"; blank stays blank) — REQ-SLD-08.
11. `EditMode_DeezerSong_SaveSendsTheStoredArtistIdAndNoText` — REQ-SLD-30: `request.ArtistId == 7`, `ArtistText` null, `ArtistDecision.None`.
12. `EditMode_DeezerSong_QueryArtistDoesNotReplaceTheStoredArtist` — REQ-SLD-30: `ArtistId = 99`, `ArtistName = "Other"`, then `InitializeArtistField()` after the load; the request still carries 7 and `ArtistSearchText == "Queen"`.
13. `EditMode_DeezerSong_TitleEditKeepsTheArtistFrozen` — REQ-SLD-17.
14. Red, implement, green; update the three existing tests (section 7); run all four `SongFormViewModel*` classes.
15. Commit `feat(ui): the artist of a Deezer song is fixed in the song form`.

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~SongFormViewModel"`

**Covers:** REQ-SLD-08 (form part), 12 and 13 (state), 14, 17, 30 (form part).

---

### T11 — `SongFormViewModel`: lyrics on open, Save waits, pick outcome in the request (ViewModel)
**Risk:** A. After T9, T10.

**Files**
- Modify: `MyVocaList/UI/ViewModels/SongFormViewModel.cs`
- Test: `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelLyricsOnOpenTests.cs`
- Existing test file needing a mock re-point: `SongFormViewModelLyricsTests.cs` (sections 7 and 8).

**Consumes:** `ISongService.EnsureLyricsAsync`, `ISongLyricsService.LookupLyricsAsync`, `LyricsOriginRules.NeedsLookupOnOpen`, `SongFormSaveRequest.PickLyricsLookup`, `Song.LyricsOrigin`.

**Produces**
```csharp
private CancellationTokenSource? _openLyricsLookupCts;
private Task<string?> _openLyricsLookupTask = Task.FromResult<string?>(null);
private LyricsLookupResult? _pickLyricsLookup;        // outcome of the pick lookup of the held pick
/// <summary>Called by the page when the form is left: asks the lookup on open to stop (REQ-SLD-23).</summary>
public void CancelLyricsLookupOnOpen();
```
- **Trigger.** At the end of `LoadSongForEditAsync`, when `LyricsOriginRules.NeedsLookupOnOpen(song.ExternalProvider, song.ExternalId, song.Lyrics, song.LyricsOrigin)`:
  - set `IsLyricsLoading = true`;
  - start `_openLyricsLookupTask` (and expose it as `LyricsLookupTask`) without awaiting;
  - run the service call inside `Task.Run`, because Microsoft.Data.Sqlite runs "async" calls synchronously (constraints-reference);
  - at most once per form instance.
- **Answer.** On the UI thread: `IsLyricsLoading = false`; if a lyrics text came back, the Lyrics field is still blank and `SongId` is still that song, set `Lyrics` (REQ-SLD-21). A cancelled lookup is swallowed and gives null.
- **Save (REQ-SLD-31).** `SaveAsync` becomes: cancel the pick lookup as today (REQ-LYR-10); if a lookup on open runs, cancel its token, `await` its task, apply a returned match to a blank field, clear the hint; then `ClearSaveDecisions()` and `RunSaveAsync()`.
- **Pick lookup.** `RunLyricsLookupAsync` calls `LookupLyricsAsync`.
  - `ApplyLyricsAnswer` stores the result in `_pickLyricsLookup` only when that lookup is still the current one.
  - `_pickLyricsLookup` is set to null in `StartLyricsLookup`, `ClearTitle` and `ClearArtist`.
  - `CancelLyricsLookup()` does not clear it, so an answer that arrived before Save survives.
- **Request.** `BuildSaveRequest` passes `PickLyricsLookup: IsEditMode ? null : _pickLyricsLookup` (section 8, item 7).

**Steps (TDD)**
1. `EditLoad_DeezerSongBlankLyricsNoOrigin_StartsOneLookup` — `// [AC] REQ-SLD-19`: `EnsureLyricsAsync(42, any)` once.
2. `EditLoad_OutOfScopeSong_StartsNoLookupAndShowsNoHint` (Theory: local-only, has lyrics, origin `User`, origin `NoneFound`) — REQ-SLD-24.
3. `LookupOnOpen_Match_FillsTheBlankLyricsField` — REQ-SLD-21.
4. `LookupOnOpen_MatchAfterTheUserTyped_KeepsTheTypedText` — REQ-SLD-21.
5. `LookupOnOpen_Running_PlaceholderReadsLookingForLyrics` — REQ-SLD-25: the hint while the `TaskCompletionSource` is pending, idle text after.
6. `LookupOnOpen_NoAnswer_ShowsNoMessage` — REQ-SLD-25: the snackbar mock is never called.
7. `CancelLyricsLookupOnOpen_CancelsTheToken` — REQ-SLD-23.
8. `Save_WhileLookupOnOpenRuns_CancelsItAndWaitsBeforeTheWrite` — REQ-SLD-31: `SaveFromFormAsync` is not called until the lookup task completes; the token is cancelled.
9. `Save_TypedLyrics_SendsTheTypedTextWhateverTheLookupReturned` — REQ-SLD-31: the request has "B".
10. `Save_BlankFieldAndStoredMatch_SendsTheMatch` — REQ-SLD-31: the request has "A".
11. `Save_BlankFieldAndNoAnswer_SendsBlankLyrics` — REQ-SLD-31.
12. `Save_AfterPickLookupFound_RequestCarriesFound` — REQ-SLD-26: `PickLyricsLookup == Found("A")`.
13. `Save_AfterPickLookupNoMatch_RequestCarriesNoMatch` — REQ-SLD-26.
14. `Save_AfterClearTitleOrNewerPick_RequestCarriesNoPickLookup` (Theory) — REQ-SLD-26.
15. `Save_WhilePickLookupRuns_RequestCarriesNoPickLookup` — REQ-SLD-26.
16. Red, implement, green; apply the mock re-point (section 7); run all `SongFormViewModel*` classes.
17. Commit `feat(ui): song form fetches missing lyrics of a Deezer song on open`.

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~SongFormViewModel"`

**Covers:** REQ-SLD-19 (trigger), 21, 23 (form part), 24 (form part), 25, 26 (request), 31.

---

### T12 — Page: plain artist text when frozen (Page)
**Risk:** C (no automated test; device check). After T10, T11.

**Files:** modify `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`, then build; modify `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs`.

**Consumes:** `IsArtistFrozen`, `IsArtistEditable`, `ArtistSearchText`, `CancelLyricsLookupOnOpen()`.

**Changes**
- Directly above `artistTapField`: `<Label Text="{Binding ArtistSearchText}" StyleClass="Title.Large" IsVisible="{Binding IsArtistFrozen}" />`. No gesture, no error state.
- `artistTapField`: add `IsVisible="{Binding IsArtistEditable}"`. Nothing else in the element changes; no shared component is edited.
- Update the XAML comment above the Artist block to name REQ-SLD-12/13.
- `OnDisappearing`: call `ViewModel.CancelLyricsLookupOnOpen();`.

**Steps:** edit the XAML; `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` (0 errors); edit the code-behind; build again; full test run; commit `feat(ui): plain artist text for Deezer songs in the song form`.

**Covers:** REQ-SLD-12, 13 (display), 08 (option row, by observation), 23 (form closed).

---

### T13 — Close-out
1. Full `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj`.
2. Android build.
3. `dotnet ef migrations has-pending-changes -p MyVocaList.Infra --exit-code`.
4. Traceability matrix in `task-log.md`, including the level-C notes for REQ-SLD-05 and T2.
5. `/sln-review`, merge to `develop`, docs sync and `.sln` registration of new `Docs/` files, status update.
6. Device check (Helder), from design § 7:
   - search the Songs list by an artist name;
   - search a well-known title in the form and see each song once;
   - pick one: the artist is plain text;
   - save and reopen: the artist cannot be edited, the title can;
   - open an older Deezer song without lyrics online and see lyrics arrive; switch to airplane mode, reopen, lyrics still there.

## 6. Coverage table

| REQ | Task | Test method |
|---|---|---|
| SLD-01 | T4 | `GetPagedAsync_QueryMatchesArtistName_ReturnsThatArtistsSongs`, `GetPagedAsync_ShortQuery_MatchesTitlePrefixOnly`, `GetPagedAsync_IndexTableMissing_FallsBackToTitlePrefix`, `GetPagedAsync_SearchRow_UsesArtistImageWhenSongHasNone` |
| SLD-02 | T4 | `GetPagedAsync_FullTextQuery_OrdersByRankThenTitleLengthThenId`, `GetPagedAsync_ShortQuery_OrdersByTitle` |
| SLD-03 | T4 | `GetPagedAsync_FullTextQuery_PagesAreDisjointAndCoverEveryMatch` |
| SLD-04 | T4 | `GetPagedAsync_QueryMatchesNothing_ReturnsEmptyAndZeroCount`; the empty state itself is existing `SongsViewModel.IsEmptyNoResults` (device check) |
| SLD-05 | none | No test, level C: `ICatalogService` and its query are not edited |
| SLD-06 | T6, T7 | `SearchSongsAsync_RowWithArtistId_CarriesItAsText`; `SearchTitleRemoteAsync_AlbumRepeatsOfOneSong_OneRow`, `_SameTitleTwoArtists_TwoRows`, `_TitlesDifferingOnlyInCase_TwoRows`, `_GroupsKeepDeezerOrder` |
| SLD-07 | T7 | `SearchTitleRemoteAsync_LiveThenPlain_KeepsThePlainRow`, `_OnlyLabelledRows_KeepsTheFirst` |
| SLD-08 | T7, T10, T12 | `SearchTitleRemoteAsync_KeptRow_HasBlankVersion`; `DeezerPick_LeavesTheVersionAsTheUserLeftIt`; option row text by device check |
| SLD-09 | T7 | `SearchTitleRemoteAsync_LibraryCheckUsesBlankVersion`, `_LibrarySongWithBlankVersion_HidesTheGroup`, `_LibrarySongOnlyAsLive_GroupStaysRemote` |
| SLD-10 | T6, T7 | `SearchSongsAsync_UrlIsUnchanged`, `_AnswerOfThirtyRows_KeepsTwentyFive`, `SearchArtistsAsync_StillAsksPlainQueryAndKeepsFive`; `SearchTitleRemoteAsync_TwentyFiveRowsOfSixSongs_ShowsFive` |
| SLD-11 | T6, T7 | `SearchSongsAsync_RowWithoutArtistId_CarriesNull`; `SearchTitleRemoteAsync_RowsWithoutArtistId_NeverMerged` |
| SLD-12 | T1, T10, T12 | `IsDeezerSong_ProviderAndIdSet_True`; `DeezerPick_ArtistIsFrozen`, `EditMode_DeezerSong_ArtistIsFrozen`; display by device check |
| SLD-13 | T1, T8a, T10, T12 | `IsDeezerSong_EitherBlank_False`; `SaveFromForm_LocalOnlySongOtherArtist_Saved`; `AddMode_NoPick_ArtistIsEditable`, `EditMode_LocalOnlySong_ArtistIsEditable`; device check |
| SLD-14 | T10 | `ClearTitle_AfterDeezerPick_ReleasesTheArtistAndKeepsItsText`, `TitleTextChanged_AfterDeezerPick_ReleasesTheArtistAndKeepsItsText`, `NewDeezerPick_AfterRelease_FreezesAgain`, `IsArtistFrozen_RaisesPropertyChangedOnPickAndRelease`, `FrozenState_MatchesWhetherSaveSendsTheIdentity` |
| SLD-15 | T8a | `SaveFromForm_DeezerSongOtherArtistId_RefusedOnArtist`, `_DeezerSongTypedArtistName_RefusedBeforeAnyResolution`, `_DeezerSongCreateTypedDecision_Refused`, `_DeezerSongTypedNameOfTheStoredArtist_StillRefused`, `UpdateSongAsync_DeezerSongOtherArtistIdOrNewName_Refused` |
| SLD-16 | T8a | `SaveFromForm_AddModeUseExistingArtist_StoresDeezerSongWithThatArtist` |
| SLD-17 | T8a, T10 | `SaveFromForm_DeezerSongTitleChanged_Saved`; `EditMode_DeezerSong_TitleEditKeepsTheArtistFrozen` |
| SLD-18 | T8a, T8b | `SaveFromForm_SaveAsNewVersionOrNewSong_StoresNoIdentity`; `SaveFromForm_SaveAsNewSongWithPickLookup_OriginStillDerived` |
| SLD-19 | T1, T9, T11 | `NeedsLookupOnOpen_DeezerBlankLyricsNoOrigin_True`; `EnsureLyricsAsync_DeezerSongBlankLyricsNoOrigin_LooksUpWithStoredTitleAndArtist`; `EditLoad_DeezerSongBlankLyricsNoOrigin_StartsOneLookup` |
| SLD-20 | T5, T9 | `LookupLyricsAsync_Match_ReturnsFoundWithTrimmedLyrics`; `EnsureLyricsAsync_Found_StoresLyricsWithOriginLyricsSource`, `_SongChangedMeanwhile_StoresNothing`, `_CancelledAfterTheAnswer_StillStores`, `_LookupRunsOutsideAnyUnitOfWork`, `_Match_IsPersistedAndReadBack` |
| SLD-21 | T11 | `LookupOnOpen_Match_FillsTheBlankLyricsField`, `LookupOnOpen_MatchAfterTheUserTyped_KeepsTheTypedText` |
| SLD-22 | T3, T5, T9 | `LyricsOrigin_EachValueRoundTrips`; `LookupLyricsAsync_ProviderHasNoRecord_ReturnsNoMatch`, `_BlankAnswer_ReturnsNoMatch`, `_TextOverTheLimit_ReturnsNoMatch`; `EnsureLyricsAsync_NoMatch_StoresOriginNoneFoundAndReturnsNull` |
| SLD-23 | T5, T9, T11 | `LookupLyricsAsync_Offline_...`, `_ProviderThrows_...`, `_BoundReached_...`, `_CallerCancelled_Rethrows`; `EnsureLyricsAsync_NoAnswer_StoresNothing`, `_CancelledDuringTheLookup_RethrowsAndStoresNothing`; `CancelLyricsLookupOnOpen_CancelsTheToken` |
| SLD-24 | T1, T9, T11 | `NeedsLookupOnOpen_LocalOnlyOrHasLyricsOrHasOrigin_False`; `EnsureLyricsAsync_OutOfScopeSong_StartsNoLookup`, `_SecondCall_ReturnsStoredLyricsWithoutARequest`; `EditLoad_OutOfScopeSong_StartsNoLookupAndShowsNoHint` |
| SLD-25 | T11 | `LookupOnOpen_Running_PlaceholderReadsLookingForLyrics`, `LookupOnOpen_NoAnswer_ShowsNoMessage` |
| SLD-26 | T1, T8b, T11 | `OnCreate_*` (six); `SaveFromForm_Add*` (four); `Save_AfterPickLookupFound_...`, `_AfterPickLookupNoMatch_...`, `_AfterClearTitleOrNewerPick_...`, `_WhilePickLookupRuns_...` |
| SLD-27 | T1, T8b | `OnUpdate_*` (three); `SaveFromForm_EditLyricsChanged_OriginUser`, `_EditLyricsEmptied_OriginUser`, `_EditLyricsUnchanged_OriginKept`, `UpdateSongAsync_LyricsChanged_OriginUser` |
| SLD-28 | T3 | `LyricsOrigin_ColumnExistsNullableText`, `LyricsOrigin_SongStoredBeforeTheMigration_ReadsNull` |
| SLD-29 | T5 | `LookupLyricsAsync_TitleAndArtistOnly_SendsOneRequest`; the bound, offline and limit rules are the reused `SongLyricsService` code (existing REQ-LYR tests unchanged) |
| SLD-30 | T8a, T10 | `SaveFromForm_DeezerSongStoredArtistId_SavesWithoutArtistResolution`; `EditMode_DeezerSong_SaveSendsTheStoredArtistIdAndNoText`, `EditMode_DeezerSong_QueryArtistDoesNotReplaceTheStoredArtist` |
| SLD-31 | T11 | `Save_WhileLookupOnOpenRuns_CancelsItAndWaitsBeforeTheWrite`, `Save_TypedLyrics_...`, `Save_BlankFieldAndStoredMatch_SendsTheMatch`, `Save_BlankFieldAndNoAnswer_SendsBlankLyrics`; stored origins by the T8b tests |

## 7. Existing behaviour that changes, and the existing tests affected

**Covered by the Supersedes table; the update is part of the named task.**

| Task | File and method | Why it changes | Planned update |
|---|---|---|---|
| T7 | `Unit/Services/SongSuggestionServiceTests.cs` → `SearchTitleRemoteAsync_RemoteRowCarriesVersion` | REQ-SFT-12 (Version) replaced by REQ-SLD-08; asserts `creep.Version == "Live"` | Remove; replaced by `SearchTitleRemoteAsync_KeptRow_HasBlankVersion` |
| T7 | same file → `SearchTitleRemoteAsync_CollatedTitleArtistVersionMatch_Promoted` | REQ-SFT-05 is now applied with a blank version (REQ-SLD-09); the test expects the triple `("Creep","Radiohead","Live")` | Remove; replaced by `_LibraryCheckUsesBlankVersion` and `_LibrarySongOnlyAsLive_GroupStaysRemote` |
| T7 | `Integration/Services/SongSuggestionTitleSearchRealStackTests.cs` → `SearchTitleRemoteAsync_EmptyLibrary_ReturnsDeezerOptionsWithOkStatus` | Line 67 asserts `r.Version == "Ao Vivo"` (REQ-SFT-04 note, REQ-SFT-12) | Change that one assertion to `r.Version == ""`; the count of 3 holds (three different artist ids). The sibling `..._PopulatedLibrary_...` asserts no version and needs no change (§ 9 item 1) |
| T10 | `Unit/ViewModels/SongFormViewModelTests.cs` → `SelectTitle_RemoteOption_SetsVersionFromDeezer_OverwritingTypedVersion` | REQ-SFT-12 (Version) replaced by REQ-SLD-08 | Remove; replaced by `DeezerPick_LeavesTheVersionAsTheUserLeftIt` |
| T10 | same file → `ClearArtist_AfterRemotePick_ClearsPendingIdentity_KeepsTitle` | Asserts `SongVersion == "Live"` after a pick (REQ-SFT-12). The scenario, X on Artist after a Deezer pick, is also removed by the REQ-PIC row of the table | Remove the Version assertion only; the rest still passes |

**Found outside the Supersedes table by the planning pass; decided in section 8 (items 1, 3, 4).** The first row is not touched after all (no `limit` is sent).

| Task | File and method | Why it changes |
|---|---|---|
| T10 | `Unit/ViewModels/SongFormViewModelTests.cs` → `ClearTitle_AfterRemotePick_ResetsUnchangedPickFilledVersion` (REQ-SFT-14, Q21) | Its precondition `SongVersion == "Live"` after a pick no longer exists. The table lists REQ-SFT-14 as "Extended", not as replaced for this point |
| T11 | `Unit/ViewModels/SongFormViewModelLyricsTests.cs` → helpers `AnswerLater`, `AnswerNow`, the `Lookup` record, and the `Verify` calls in `PickWithoutLocalArtist_StartsOneLookupWithThePicksFields`, `PickWithLocalArtist_StartsOneLookupToo`, `NonBlankLyricsAtPickTime_StartsNoLookup`, `SecondPick_AfterFillAndEdit_StartsNoLookupAndKeepsTheText`, `LocalPick_NavigatesAndStartsNoLookup` | They set up and verify `FindLyricsAsync`; the ViewModel must call `LookupLyricsAsync` to know "no match" from "no answer" (REQ-SLD-26 rule 3). No assertion on form behaviour and no `[AC]` text changes |

**Checked and expected to pass unchanged.**
- `SongLyricsServiceTests` (16 tests) and `LrclibLyricsProviderTests`.
- `SongRepositoryTests.GetPagedAsync_*`: they use `EnsureCreatedAsync`, so there is no FTS table and the prefix fallback runs.
- `SongServiceTests.GetPagedSongsForListAsync_*`: the service still calls `GetPagedAsync`.
- `SongServiceSaveFromFormTests.SaveFromForm_EditMode_RunsNoSongGuard_CallsUpdateWithNullIdentity`: Deezer song, request artist id 3 equals stored 3.
- `SongFormViewModelPictureTests` REQ-PIC-31 edit tests: their fixture song has no external identity.
- `SongFormViewModelPictureTests.ArtistReleased_TextUnchanged_...` and `_TextChanged_...` (REQ-PIC-28 after a Deezer pick), and `SongFormViewModelLyricsTests.ClearArtist_*`: the add-mode request logic is not changed. They describe an input the page can no longer produce; record this in the task-log and leave them.
- `SongFormViewModelTests.LoadSongForEdit_ApiImportedSong_LocksArtistField`: `IsArtistLocked` is still set.
- `SongFormViewModelImageTests.TitleOption_RemoteRow_SupportingTextIsArtistVersionAlbum`: `ToTitleOption` is untouched.
- `TapToEditFieldLogicTests` (REQ-PIC-26): component untouched.
- **REQ-LYR-14:** the only test carrying this id is `LocalPick_NavigatesAndStartsNoLookup` (add mode, local pick). It stays true; only its `Verify` target moves. No existing test asserts "edit mode starts no lookup".
- Edit-mode ViewModel tests with a Deezer fixture and blank lyrics (`StoredSong(apiSourced: true)`) will now call `EnsureLyricsAsync` on a loose `Mock<ISongService>`, which returns null. Expected to pass; not run here.

## 8. Open points raised by the planning pass, and their decisions

Decided by Claude under Helder's advance authorisation (2026-11-16); recorded in `design.md § 1.2` as C20 to C27. Where a task text above differs, this section wins.

| # | Point | Decision |
|---|-------|----------|
| 1 | Sending `limit=25` would break the exact-URL test of REQ-SFT-35, and the parameter is confirmed only by unofficial sources | **No `limit` is sent** (REQ-SLD-10 allows it). The provider keeps up to `RemoteSongFetchCount` rows of the default answer. T6: drop test 3 (`SearchSongsAsync_AsksForTheFetchCount`); add `SearchSongsAsync_UrlIsUnchanged` (`PathAndQuery == "/search/track?q=creep"`); the existing `SearchSongsAsync_SendsPlainQueryWithoutQualifiers` is **not** touched. (C20) |
| 2 | Design says "new repository search"; the plan switches behaviour inside `SongRepository.GetPagedAsync` | **Accepted.** No interface change, no Service change, two existing tests stay valid. (C21) |
| 3 | The ViewModel must call `LookupLyricsAsync`, which re-points mocks in `SongFormViewModelLyricsTests` | **Accepted as a planned mechanical exception** in T11: only the mocked method name and its return wrapping change in the helpers and `Verify` calls listed in section 7; no assertion on form behaviour and no `[AC]` text changes. Listed in the task-log for Helder. (C22) |
| 4 | `ClearTitle_AfterRemotePick_ResetsUnchangedPickFilledVersion` cannot be set up once a pick writes no Version | **Removed in T10** with `_pickFilledVersion`; the Supersedes table now says the Version reset of REQ-SFT-14 no longer applies. (C23) |
| 5 | Where `SongService` gets `ISongLyricsService` | **Optional trailing constructor parameter** `ISongLyricsService? lyricsService = null`; no scoped service is used outside its scope and no existing construction site changes. When it is null, `EnsureLyricsAsync` returns null. T9 adds one DI test (`AppServicesRegistrationTests`) proving the resolved `ISongService` has it. (C24) |
| 6 | Add mode: change the Title, edit the artist, type the picked title back: the form holds the pick again with the edited artist | **Left as it is** (REQ-SLD-14 followed literally). Closing it means changing the identity rule of REQ-PIC-27/28 and its tests. Known gap, listed for Helder in the task-log. (C25) |
| 7 | Pick outcome after a Title text change | **Kept.** The outcome is dropped only by X (Title or Artist) and by a newer pick; unchanged looked-up lyrics keep the origin *lyrics source*. REQ-SLD-26 wording aligned. (C26) |
| 8 | Option row shows "artist · album" with "Deezer" trailing (REQ-ART-07) | **Album stays.** REQ-SLD-08 now says only that no version label is shown. (C27) |
| 9 | Return value of `EnsureLyricsAsync` on a later call | As planned: lyrics stored during this call (by it or by a concurrent call) are returned; a song that already had lyrics returns null at the precondition. Design wording aligned. |
| 10 | Missing index table | The list falls back to the title prefix; the form search keeps its current behaviour. Spec wording aligned. |
| 11 | "Writes two columns only" | The write goes through `UpdateAsync` with the row just read in the same transaction; no other column changes value. Design wording aligned. |
| 12 | Blank artist on a Deezer song update returns "Artist is required" | Accepted; nothing is stored and the frozen form cannot send it. |
| 13 | Lyrics comparison trims both sides | Accepted. |
| 14 | Catalog-mode query values could overwrite the loaded artist | Guarded for Deezer songs (T10 test 12); local-only behaviour unchanged. |
| 15 | Lookup on open after leaving and returning to the same form instance | Not restarted on that instance; the next open starts it. Accepted. |
| 16 | Up to 25 rows reach the library check | Accepted; checked on the device. |

**To verify while implementing (not known at plan time):** the exact `dotnet ef -p` path; whether `SqlQueryRaw<SongListItemDto>` binds the record directly (else a private row record); whether `SongServiceReadScopeTests` counts repository calls in the edit read scope (if it does and T8a's extra read changes the count, stop and report — do not edit that test).

## 9. Fixes from the plan review (2026-11-16)

A fresh plan reviewer returned "pass with fixes", no blocking finding. This section, like § 8, wins over any earlier task text.

1. **T7, sibling real-stack test.** `SongSuggestionTitleSearchRealStackTests` → the `..._PopulatedLibrary_...` test asserts only `Ok` and `NotEmpty` (read by the reviewer). It needs **no change**. T7 changes exactly one assertion in that file: `r.Version == "Ao Vivo"` becomes `r.Version == ""`.
2. **T9 is split.**
   - **T9a** — `EnsureLyricsAsync` and the optional constructor parameter, with the unit tests (steps 1 to 9). Files: `Domain/ServicesInterfaces/ISongService.cs`, `Services/SongService.cs`, `Unit/Services/SongServiceEnsureLyricsTests.cs`.
   - **T9b** — real-stack tests (steps 10, 11) and the DI test. Files: `Integration/Services/SongServiceEnsureLyricsRealStackTests.cs`, `Unit/DependencyInjection/AppServicesRegistrationTests.cs`. No production file unless a test exposes a defect in T9a's code.
   - **DI test:** `AppServices_SongService_ReceivesTheLyricsService` — `// [AC] REQ-SLD-19`: build the provider the way the other tests of that file do, resolve `ISongService`, read the private field `_lyricsService` of `SongService` by reflection, assert it is not null.
3. **T11 is split.**
   - **T11a** — lookup on open: trigger, hint, answer applied or kept, `CancelLyricsLookupOnOpen` (steps 1 to 7; REQ-SLD-19, 21, 23, 24, 25).
   - **T11b** — Save waits for the lookup on open, `_pickLyricsLookup`, the pick outcome in the request, and the mock re-point in `SongFormViewModelLyricsTests` (steps 8 to 15; REQ-SLD-26, 31).
4. **T11a, task property.** The existing `LyricsLookupTask` (pick lookup) is **not** reused. The lookup on open is exposed as a new `public Task<string?> LyricsOnOpenTask { get; private set; }` (initially a completed task with null). New tests await `LyricsOnOpenTask`; existing tests keep awaiting `LyricsLookupTask`.
5. **T11b, mock re-point rule.** In `SongFormViewModelLyricsTests` the helpers set up `LookupLyricsAsync` in place of `FindLyricsAsync`: a null answer maps to `LyricsLookupResult.NoAnswer`, a non-null text maps to `LyricsLookupResult.Found(text)`. `Verify` calls change the method name only. No assertion on form behaviour and no `[AC]` line changes.
6. **T3, hand-off.** The Builder adds the property, the mapping and the three tests, sees them fail, commits (`feat(infra): lyrics origin property and mapping` with the Item trailer) and reports `blocked: needs migration`. The main agent then, in the worktree: runs `dotnet ef migrations add AddSongLyricsOrigin` (first try `-p Infra/MyVocaList.Infra.csproj`; the working form is recorded in the task-log), replaces the generated `Down` body with the raw `ALTER TABLE ... DROP COLUMN` statement given in T3, runs the T3 test filter and `has-pending-changes`, and commits `feat(infra): lyrics origin column on songs`.
7. **T4, literal SQL and parameters.** Parameters are `@m`, `@take`, `@skip`. The count joins the same tables as the page query, so a stale index row cannot make the count exceed the rows:

```sql
-- ListSearchSql
SELECT s.Id AS Id, s.Title AS Title, s.ArtistId AS OriginalArtistId, a.Name AS OriginalArtistName,
       s.FeaturedArtists AS FeaturedArtists, s.ExternalProvider AS ExternalProvider, s.Version AS Version,
       COALESCE(s.ImageUrl, a.ImageUrl) AS ImageUrl
FROM SongSearch
JOIN Songs s ON s.Id = SongSearch.rowid
JOIN Artists a ON a.Id = s.ArtistId
WHERE SongSearch MATCH @m
ORDER BY bm25(SongSearch, 10.0, 1.0), length(s.Title), s.Id
LIMIT @take OFFSET @skip

-- ListSearchCountSql
SELECT count(*) AS Value
FROM SongSearch
JOIN Songs s ON s.Id = SongSearch.rowid
JOIN Artists a ON a.Id = s.ArtistId
WHERE SongSearch MATCH @m
```

   Column aliases follow the property names of the row type actually bound (verify against `SongListItemDto`). The blank test stays the existing `string.IsNullOrEmpty(query)`; the Service already turns white space into null.
8. **T11a, the ViewModel and the rule.** The ViewModel only calls the Domain predicate `LyricsOriginRules.NeedsLookupOnOpen` to decide whether to show the hint and start the call; `EnsureLyricsAsync` checks the same preconditions itself and is the authority.
9. **Device checklist, one more line.** Leaving the form for a child page (picture picker, YouTube search) while the lookup runs cancels it; lyrics then arrive on the next open of the song.
10. **`tasks.md` T14** points at plan T13 item 6 until the task-log exists; T13 copies the checklist into `task-log.md § Device check`.
