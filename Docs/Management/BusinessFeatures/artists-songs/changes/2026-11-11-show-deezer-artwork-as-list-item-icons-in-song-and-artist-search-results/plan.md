# Plan: Deezer artwork in the Song form options and the Songs list

Spec: `requirements.md` (REQ-ART-01…24), `design.md`. One brief per task of `tasks.md`.

## Common rules for every task

- Work only in the worktree `C:\Users\helde\source\repos\MyVocaList\.claude\worktrees\deezer-artwork`
  (branch `feature/deezer-artwork`). Never edit the main checkout's copies.
- Read `design.md` and the REQ-ART criteria named in the brief before coding. Spec gap → stop and report.
- TDD: write the test, run it, see it fail for the right reason, then the code. First line of each
  test body: `// [AC] REQ-ART-NN: <criterion>`. **Never edit an existing test** (single exception:
  the four tests named in T8); the new record parameters are optional and trailing so existing tests
  compile. If any other existing test fails, stop and report.
- **Expression trees cannot omit optional arguments (CS0854).** Every `new SongListItemDto(...)`,
  `new SongSearchHit(...)` or `new ArtistListItem(...)` inside an `IQueryable` `.Select(...)` must pass
  all arguments explicitly.
- Generated migration files (migration, Designer, snapshot) do not count toward a task's file size.
- English only. No `DisplayAlert`. No C#-side normalisation for search or de-duplication.
- Verification before reporting done:
  - `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` (report passed / failed / skipped counts);
  - tasks that touch the MAUI head (T6…T9): also `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` with 0 errors.
- XAML: edit one file → build → fix → next.
- Commit on the task branch (`feat:` commits carry the trailer
  `Item: show-deezer-artwork-as-list-item-icons-in-song-and-artist-search-results`, except T6 and T7,
  which carry their component item id). Add a `### T<n>` entry to `task-log.md` with `### Changed files`
  and verification evidence. Do not push; the orchestrator merges.
- Do not touch `tasks.md`, `BACKLOG.md`, README frontmatter or `.claude/` files; the orchestrator owns them.

## T1 — Domain + Infra: `ImageUrl`, rules, migration (REQ-ART-03, 05)

Files:
- `MyVocaList.Domain` → `Entity/Song.cs`, `Entity/Artist.cs`: add `public string? ImageUrl { get; set; }`.
- New `Domain/Rules/ImageUrlRules.cs`, namespace `MyVocaList.Domain.Rules` (add the `using` where
  consumed, or a global using in the consuming project's `GlobalUsings.cs` if that is the project's habit):
  `public const int MaxLength = 500;` and `public static string? Normalize(string? value)`:
  trim; null when empty; null when longer than `MaxLength`; null unless
  `Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps`; else the trimmed value.
- `Infra/EntityEFConfig/SongConfiguration.cs`, `ArtistConfiguration.cs`:
  `builder.Property(x => x.ImageUrl).HasMaxLength(ImageUrlRules.MaxLength);` (optional, no index, no collation).
- Migration: `dotnet ef migrations add AddImageUrlToSongsAndArtists -p MyVocaList.Infra` (see
  `.claude/library/database-setup.md`; use the project path that exists in the worktree). The
  migration must only add two nullable TEXT columns; check that `Up` contains nothing else.

Tests:
- Unit `ImageUrlRulesTests`: https URL kept; surrounding spaces trimmed; `http://`, relative path,
  empty, whitespace, null, 501 characters → null; exactly 500 characters kept. `// [AC] REQ-ART-03`.
- Integration, in the style of `Integration/Migrations/SongMigrationTests.cs` (real SQLite,
  `MigrateAsync`): after migration `Songs.ImageUrl` and `Artists.ImageUrl` exist; a song and an artist
  saved without a value read back null; a value round-trips. `// [AC] REQ-ART-05`.

## T2 — Read models and projections (REQ-ART-06, 10, 19, 20)

Files:
- `Contracts/DTOs/List/SongListItemDto.cs`: add trailing optional `string? Version = null, string? ImageUrl = null`;
  add computed `public string SupportingText` = non-empty parts of `OriginalArtistName`, `Version`,
  `"feat. " + FeaturedArtists`, joined by `" · "` (whitespace-only counts as empty).
- `Domain/ReadModels/SongSearchHit.cs`: trailing optional `string? ImageUrl = null, string? ArtistImageUrl = null`.
- `Domain/ReadModels/ArtistListItem.cs`: trailing optional `string? ImageUrl = null`.
- `Infra/Repository/SongRepository.cs`:
  - `GetPagedAsync` projection (about line 68) adds `s.Version, s.ImageUrl ?? s.OriginalArtist.ImageUrl`;
  - every builder of `SongSearchHit` fills `ImageUrl` and `ArtistImageUrl`: the raw-SQL title search
    (the shared select constant gains `s.ImageUrl AS ImageUrl, a.ImageUrl AS ArtistImageUrl`; the
    column aliases must match the record's parameter names, as `SqlQueryRaw<SongSearchHit>` binds by
    name), its EF fallback branch (about line 193), `FindByExternalIdsAsync` (about line 232) and
    `FindByTitleAndArtistNamesAsync` (about line 253). The last two feed the rows the suggestion
    service promotes to local options, so they must carry the stored images.
- `Infra/Repository/CatalogRepository.cs`: same projection change as `GetPagedAsync`.
- `Infra/Repository/ArtistRepository.cs`: `SearchByNameAsync` (about line 87) projects
  `a.CatalogEntries.Count()` (was the constant 0) and `a.ImageUrl`; `GetPagedAsync` (about line 61) adds `a.ImageUrl`.
- Any other `IQueryable` projection of these three records found by grep gets all arguments explicitly.

This brief names six production files; they are one mechanical change (three records and the
projections that fill them) and must stay together: once a record gains a parameter, every
expression-tree projection of it must change in the same commit or the build breaks. Estimated under 2 hours.

Tests:
- Unit `SongListItemDtoTests`: `SupportingText` for artist only; artist + version; artist + featured;
  all three; empty artist name. `// [AC] REQ-ART-20`.
- Integration (real SQLite, style of `Integration/Repositories/SongRepositorySearchTests.cs`):
  - list projection: song with image → its image; song without image and artist with image → artist
    image; neither → null; `Version` carried (`// [AC] REQ-ART-19`), for `SongRepository.GetPagedAsync`
    and for the catalog query;
  - `SearchTitleCandidatesAsync`, `FindByExternalIdsAsync` and `FindByTitleAndArtistNamesAsync` hits
    carry both URLs (`// [AC] REQ-ART-06`);
  - `ArtistRepository.SearchByNameAsync`: artist with 3 catalog entries returns `CatalogCount == 3`
    and its `ImageUrl` (`// [AC] REQ-ART-10`).

## T3 — Deezer provider (REQ-ART-01, 02, 03, 04)

Files:
- `Contracts/DTOs/MusicSearchResultDto.cs`: trailing optional
  `string? ImageUrl = null, string? ArtistImageUrl = null, string? AlbumTitle = null`.
- `Services/DeezerProvider.cs`: private records gain
  `DeezerTrack.Album` (`album`: `title`, `cover_medium`), `DeezerTrackArtist.PictureMedium` (`picture_medium`),
  `DeezerArtist.PictureMedium` (`picture_medium`). Song results: `ImageUrl = Normalize(album.cover_medium)`,
  `ArtistImageUrl = Normalize(artist.picture_medium)`, `AlbumTitle = album.title` trimmed, null when empty.
  Artist results: `ImageUrl = Normalize(picture_medium)`. No new HTTP request.

Tests (extend the pattern of `Unit/Services/DeezerProviderTests.cs` in a **new** file
`DeezerProviderImageTests.cs`, with its own JSON builder; do not edit the existing file):
- track with album and artist picture → all three values mapped (`REQ-ART-01`);
- track without `album`, without `artist.picture_medium`, with `cover_medium: ""` → nulls, search succeeds (`REQ-ART-01`, `REQ-ART-03`);
- `http://` cover → null (`REQ-ART-03`);
- artist search maps `picture_medium` (`REQ-ART-02`);
- the handler saw exactly one request per search (`REQ-ART-04`).

## T4 — Suggestion DTOs and services (REQ-ART-06, 24)

Files:
- `Contracts/DTOs/Suggestions/SongSuggestionDto.cs`: trailing optional
  `string? ImageUrl = null, string? ArtistImageUrl = null, string? AlbumTitle = null`; computed
  `public string? RowImageUrl => ImageUrl ?? ArtistImageUrl;`.
- `Contracts/DTOs/Suggestions/ArtistSuggestionDto.cs`: trailing optional `string? ImageUrl = null`.
- `Services/SongSuggestionService.cs`: `MapLocal` copies `hit.ImageUrl`, `hit.ArtistImageUrl`; the remote
  mapping copies `r.ImageUrl`, `r.ArtistImageUrl`, `r.AlbumTitle`. A promoted row is built from a
  `SongSearchHit` through `MapLocal`, so it carries the stored song's values (T2 made the lookups
  fill them); no extra work, and no album on it.
- `Services/ArtistSuggestionService.cs`: remote mapping copies `r.ImageUrl`.

Tests (new files, same mocks as the existing suggestion tests):
- local title option carries the hit's two URLs; `RowImageUrl` falls back to the artist image (`REQ-ART-06`);
- remote title option carries cover, artist image and album; `RowImageUrl` rule (`REQ-ART-06`);
- promoted row carries the stored images, not Deezer's: the `ISongRepository` mock's
  `FindByExternalIdsAsync` returns a hit with stored URLs while the Deezer result has different ones (`REQ-ART-06`);
- remote artist suggestion carries `ImageUrl`; local one is null (`REQ-ART-24`).

## T5 — Save rules (REQ-ART-15, 16, 17, 18)

Files:
- `Domain/Resolution/SongFormSaveRequest.cs`: trailing optional `string? ImageUrl = null, string? ArtistImageUrl = null`.
- `Services/SongService.cs` only, rules of `design.md § 3.4`:
  - normalise both request values with `ImageUrlRules.Normalize` at the top of `SaveFromFormAsync`;
  - the private record `SaveFormResolution` gains `bool FillArtistImage`; it is false only on the
    branch taken for `ArtistDecision.UseExisting`, true for the branches by `request.ArtistId`, by
    exact collated match and by create (`CreateTyped` included);
  - the private `CreateSongWithUrlsCoreAsync` gains optional
    `string? songImageUrl = null, string? artistImageUrl = null, bool fillArtistImage = false`: it sets
    `Song.ImageUrl = songImageUrl`; for an artist it creates, sets `ImageUrl = artistImageUrl` before
    the flush; for an existing artist, when `fillArtistImage` and `artistImageUrl` is not null, loads
    the tracked entity the method already uses (or `GetByIdAsync`) and sets `ImageUrl` only when it is null;
  - the edit path (`UpdateSongCoreAsync`) is not given the values;
  - public `ISongService` and `CreateSongWithUrlsAsync` signatures stay unchanged;
  - all inside the existing unit of work.

Tests (new file `SongServiceSaveFromFormImageTests.cs`, same setup as `SongServiceSaveFromFormTests.cs`):
- add + pending image → stored song has it; no pending → null (`REQ-ART-15`);
- add with `SaveAsNewVersion` and with `SaveAsNewSong` → identity null, image kept (`REQ-ART-15`);
- artist created by the Save gets the artist image, for the plain create path and for the `CreateTyped` decision (`REQ-ART-16`);
- existing artist by `ArtistId` or exact name with null image → filled; with an image → unchanged (`REQ-ART-16`);
- artist chosen in the near-duplicate decision → unchanged (`REQ-ART-16`);
- edit mode with a request that carries an image → stored `ImageUrl` unchanged (`REQ-ART-17`);
- a refused or failed Save (e.g. exact duplicate with no decision) changes no `ImageUrl` (`REQ-ART-18`);
- invalid URL in the request is stored as null (`REQ-ART-03`).

## T6 — Component: `ListItemLeadingImage.PlaceholderIcon` + `ListItemLeadingAvatar` (REQ-ART-22)

Authoritative description: the component record (README) named in `tasks.md` T6. Commit trailer
`Item: listitem-leading-media-placeholder-on-listitemleadingimage-and-new-listitemleadingavatar`.

Files: `MyVocaList/UI/Components/Lists/ListItemLeadingImage.xaml(.cs)`; new
`ListItemLeadingAvatar.xaml(.cs)` in the same folder. Check `dx:DXImage` and `dx:DXBorder` against
the DevExpress docs for the csproj version (Context7) before use. No consumer page is edited.
Level C (pure XAML, no logic): no unit test; Android build must be clean. Task-log entry includes the
consumer map and risk table by reference to the record, the Level-C no-test decision, and the note for
REQ-ART-23: a URL string bound to `ImageSource` becomes a `UriImageSource`, whose caching is enabled by
default (confirm against the MAUI docs for the csproj version and record the source).

## T7 — Component: `AutocompleteSuggestion` + option row (REQ-ART-06, 08, 09)

Authoritative description: the component record (README) named in `tasks.md` T7. Commit trailer
`Item: autocompletedxfield-option-rows-optional-leading-image-or-avatar-and-trailing-text`.

Files: `Contracts/Models/AutocompleteSuggestion.cs` (+ enum `AutocompleteLeadingKind` in its own file
beside it), `MyVocaList/UI/Components/TextFields/AutocompleteDXField.xaml` (template only).
Tests: unit `AutocompleteSuggestionTests`: defaults are `None` / null / both flags false; `Image` and
`Avatar` set the matching flag. Existing component tests stay green. Android build clean.

## T8 — `SongFormViewModel` (REQ-ART-06…14)

File: `MyVocaList/UI/ViewModels/SongFormViewModel.cs` only, per `design.md § 3.5`.
Tests (new file `SongFormViewModelImageTests.cs`, same `CreateSut` pattern as `SongFormViewModelTests.cs`):
- Title option: leading kind Image, `ImageUrl` = row image, placeholder `music_note_outlined` (`REQ-ART-06`);
- supporting text examples of `REQ-ART-07` (incl. album equal to title ignoring case → omitted; local row has no album);
- trailing text `Library` / `Deezer`, and no source word in the supporting text (`REQ-ART-08`);
- Artist option: leading kind Avatar, image, placeholder `account_circle_outlined` (`REQ-ART-09`); supporting text is `CatalogCountText` (`REQ-ART-10`);
- Deezer pick then Save → request carries song image (cover, not the fallback) and artist image (`REQ-ART-11`);
- Deezer pick leaves `FeaturedArtists`, `Lyrics`, karaoke URLs unchanged and calls no write (`REQ-ART-12`);
- X on Title → request has no song image, keeps the artist image; X on Artist → neither (`REQ-ART-13`);
- local Artist pick with no prior Deezer pick → request images null (`REQ-ART-14`);
- edit mode → request images null (`REQ-ART-17`).
**Planned test edits (the only ones allowed, `design.md § 6`):** four tests of
`MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` assert the REQ-SFT-04 supporting text that
REQ-ART-07/08 replace: `TitleOptionSource_LocalHit_SupportingTextHasArtistVersionAndLibraryMarker`,
`TitleOptionSource_LocalHitEmptyVersion_OmitsVersionPart`,
`TitleOptionSource_RemoteHit_SupportingTextIsArtistVersionAndDeezer`,
`TitleOptionSource_RemoteHitEmptyVersion_OmitsVersionPart`. First run them and confirm they fail only on
the supporting-text assertion; then change only the expected string (source word removed) and add an
assertion on `TrailingText`; rename the `[AC]` line to REQ-ART-07 / REQ-ART-08. Do not rename the test
methods. List the four edits in the task-log. Any other failing existing test: stop and report.
`ArtistFormViewModelTests` also asserts "In your library" / "Deezer"; `ArtistFormViewModel` is not
changed, so those must stay green untouched.

## T9 — `SongsPage.xaml` (REQ-ART-19, 20, 21)

File: `MyVocaList/UI/Pages/Songs/SongsPage.xaml` only, per `design.md § 3.6` (both templates).
`SongsViewModel` is not changed. Level C: no unit test; the task-log records the no-test decision for
REQ-ART-19 (on screen), 21 and 22; Android build clean. Device check is T11.

## T10 — Close-out (orchestrator)

Full test run and Android build; `verifier` on the branch; traceability matrix in `task-log.md`;
add `ListItemLeadingImage` and `ListItemLeadingAvatar` to the governed list
(`.claude/library/component-safety-gate.md`); register the follow-up items (image backfill, Artists
list avatars); mark the 2026-11-09 Songs-list supporting-text item delivered; statuses; merge to
`develop`; push.

## T11 — Device check (Helder)

Checklist in `task-log.md § Device check`. It includes an offline re-display step for REQ-ART-23
(open the Songs list online, go offline, reopen: images seen before still show).

## Task interface table

| Task | Consumes | Produces | Demo | Review lane |
|------|----------|----------|------|-------------|
| T1 | — | `Song.ImageUrl`, `Artist.ImageUrl`, `ImageUrlRules`, migration | tests green; migration adds two columns | Standard |
| T2 | T1 columns | `SongListItemDto.Version/ImageUrl/SupportingText`, `SongSearchHit` and `ArtistListItem` image fields, filled by every projection | integration tests green | Standard |
| T3 | `ImageUrlRules` | `MusicSearchResultDto.ImageUrl/ArtistImageUrl/AlbumTitle` | provider tests green | Standard |
| T4 | T2 hits, T3 results | `SongSuggestionDto.ImageUrl/ArtistImageUrl/AlbumTitle/RowImageUrl`, `ArtistSuggestionDto.ImageUrl` | suggestion tests green | Standard |
| T5 | T1 columns, `ImageUrlRules` | `SongFormSaveRequest.ImageUrl/ArtistImageUrl`; save rules | save tests green | Elevated (single write path) |
| T6 | — | `ListItemLeadingImage.PlaceholderIcon`, `ListItemLeadingAvatar` | Android build clean | Elevated (governed component) |
| T7 | T6 components | `AutocompleteSuggestion.LeadingKind/ImageUrl/PlaceholderIcon/TrailingText`; option row | Android build clean; tests green | Elevated (governed component) |
| T8 | T4, T5, T7 | Title and Artist options with images; pending images in the save request | ViewModel tests green | Standard |
| T9 | T2 DTO, T6 component | Songs list rows with image and artist text | Android build clean | Standard |
