# YouTube without key + MusicBrainz catalog + Wikidata artist images — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace Deezer and the key-based YouTube search with a MusicBrainz catalog, keyless YouTube links + the official player, and Wikidata/Commons artist images.

**Architecture:** `MusicBrainzProvider` replaces `DeezerProvider` behind the existing `IMusicMetadataProvider`. The suggestion services, `SongService` and the Domain rules lose their Deezer-specific branches (T2a); the UI then drops Deezer wording (T2b). YouTube becomes search-out + paste/share-back + an IFrame WebView page; the Data API service, its page and the Settings key section are deleted. One data migration clears Deezer provenance in place. `IArtistProfileService` resolves photo, credit, description and discography at runtime.

**Tech Stack:** .NET MAUI (net10.0-android/ios), C#, CommunityToolkit.Mvvm, EF Core + SQLite, xUnit + Moq. Versions from the `.csproj`.

**Spec:** `README.md` (this folder) — REQ-MSRC-01…10; evidence `../../findings.md` §6–§7.

## Global Constraints
- **Provider string:** `"MusicBrainz"` exactly (same casing style as the existing `"Deezer"`), held in one constant `ProviderNames.MusicBrainz` (`Domain/Constants/ProviderNames.cs`, created in T1). Every comparison uses that constant; the migration compares `lower(ExternalProvider) = 'deezer'`.
- User-Agent for MusicBrainz and Wikimedia: `MyVocaList/<app version> (<contact>)`; `<contact>` from configuration key `UserAgent:Contact` (embedded `appsettings.json`, gitignored; placeholder in `appsettings.template.json`). Never commit the real value.
- MusicBrainz: ≤ 1 request/s per client through one shared limiter; `fmt=json`; base `https://musicbrainz.org/ws/2/`. Remote search keeps the existing suggestion debounce (`SearchConstants`), asserted in T2a.
- Stored from third parties: only MusicBrainz MBIDs + names (CC0) and the 11-character YouTube video id (`SongKaraokeUrl.VideoId`). Nothing from YouTube, Wikidata or Commons otherwise.
- Rows edited in place, never deleted or recreated. Deezer provenance cleared silently.
- Business logic in Services; worktree per task; one XAML file → build → next; `MauiProgram.cs`, `AppShell.xaml(.cs)`, `AppDbContext.cs`, `Migrations/*` sequential-only.
- Tests: `Method_Condition_Expected`; first line `// [AC] REQ-MSRC-NN: …`; migration tests on a real SQLite temp file.
- **Deferred (spec in-place note 2026-11-26):** MBID re-match of migrated rows, work grouping (REQ-03 second clause), iOS share extension.

## File ownership chain (serial)
`ServiceCollectionExtensions.cs`: T2b → T4 → T6 → T7. `SongFormViewModel.cs` / `SongFormPage.xaml`: T2b → T4 → T5 → T6. `ArtistFormViewModel.cs` / `ArtistFormPage.xaml`: T2b → T7. No task runs in parallel with another that owns the same file.

## Review Focus
1. MusicBrainz HTTP 503 (rate-limited) → empty remote result with the offline `RemoteSearchStatus`, local results still shown, non-blocking message (T1 + T2a tests).
2. Pasted text with spaces, `m.`/`music.` hosts, `&t=42s`/`&list=` → id extracted; playlist-only links rejected (T5).
3. No P18, or Commons file without `LicenseShortName` → placeholder, no credit line (T7).
4. Migration on a DB without Deezer rows, or run twice → no change (T3).
5. `UserAgent:Contact` missing → app starts; UA without contact; one warning (T1).

---

### Task 1: MusicBrainzProvider (REQ-MSRC-02, -10)
- **Review lane:** Standard · **Consumes:** `IMusicMetadataProvider`, `MusicSearchResultDto` (existing).
- **Files owned:** Create `Services/MusicBrainzProvider.cs`, `Services/MusicBrainzRateLimiter.cs`, `Services/MusicBrainzResponses.cs` (private JSON records), `Domain/Constants/ProviderNames.cs`; Test `Tests/Unit/Services/MusicBrainzProviderTests.cs`.
- **Produces:** `sealed class MusicBrainzProvider : IMusicMetadataProvider` (`ProviderName => ProviderNames.MusicBrainz`), ctor `(HttpClient, MusicBrainzRateLimiter, ILogger<MusicBrainzProvider>)`; `MusicBrainzRateLimiter.WaitAsync(CancellationToken)` (singleton, ≥ 1000 ms between calls); `static string MusicBrainzProvider.BuildUserAgent(string version, string? contact)`. `MusicSearchResultDto.ExternalId` = MBID. HTTP 503/timeout → throws the existing `MusicProviderException` (callers map it to offline status, as today).
- **Demo:** tests green.
- [ ] Failing tests: `SearchSongsAsync_WithArtistHint_SendsLuceneQuery` (path `recording/?query=recording:"x" AND artist:"y"&fmt=json`, URL-encoded), `SearchArtistsAsync_MapsMbidAndName`, `SearchSongsAsync_Http503_ThrowsMusicProviderException`, `WaitAsync_TwoCalls_SecondWaitsAtLeast1000ms`, `BuildUserAgent_MissingContact_ReturnsBareProductToken`.
- [ ] Run → FAIL. Implement. Run → PASS. Commit `feat:` + `Item: youtube-without-key-musicbrainz-catalog-wikidata-artist-images-replacing-deezer`.

### Task 2a: Services and Domain — drop Deezer branches (REQ-MSRC-01, -02, -03, -10)
- **Review lane:** Elevated (core save path) · **Consumes:** T1 `ProviderNames.MusicBrainz`, `MusicBrainzProvider`.
- **Files owned:** `Services/SongSuggestionService.cs`, `Services/ArtistSuggestionService.cs` (replace `DeezerProviderName` with `ProviderNames.MusicBrainz`; remove Deezer-only query qualifiers), `Services/SongService.cs`, `Domain/Rules/ImageReferenceRules.cs`, `Domain/Rules/LyricsOriginRules.cs`, `Domain/Rules/SongIdentityRules.cs`, `Domain/Resolution/SongFormSaveRequest.cs`, `Domain/RepositoryInterface/ISongRepository.cs`, `Contracts/DTOs/Suggestions/*.cs` (doc comments/field names only; public member names keep their meaning — rename `Deezer*` members to `Remote*`); Tests: matching files in `Tests/Unit/Services` and `Tests/Unit/Domain` (update Deezer-named fixtures only through new tests; old Deezer tests are deleted with the code they cover).
- **Produces:** unchanged service signatures except `Deezer*` → `Remote*` renames, listed in the commit body for T2b.
- **Demo:** tests green; save of a MusicBrainz pick stores `ExternalProvider = "MusicBrainz"` + MBID.
- [ ] Failing tests: `SaveAsync_MusicBrainzPick_StoresProviderAndMbid` (SongService), `SearchTitleRemoteAsync_ProviderThrows_ReturnsOfflineStatusAndLocalResults`, `SearchTitleRemoteAsync_RapidCalls_DebouncedToOneProviderCall`, `ImageReferenceRules_MusicBrainzSong_HasNoStoredImage`.
- [ ] Implement; build 0 errors; tests pass. Commit.

### Task 2b: DI + UI — remove Deezer (REQ-MSRC-01)
- **Review lane:** Standard · **Consumes:** T2a renames.
- **Files owned:** `MyVocaList/Extensions/ServiceCollectionExtensions.cs:36-45`, `MauiProgram.cs` (bind `UserAgent:Contact`), `MyVocaList/appsettings.template.json`, `Contracts/DTOs/List/ArtistListItemDto.cs:16` (`"MusicBrainz" => "MB"`), `MyVocaList/UI/ViewModels/SongFormViewModel.cs`, `MyVocaList/UI/ViewModels/ArtistFormViewModel.cs`, `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`, `MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml`, `Domain/Constants/SearchConstants.cs`; delete `Services/DeezerProvider.cs`, `Tests/Unit/Services/DeezerProviderTests.cs`; Tests `Tests/Unit/DependencyInjection/*`, `Tests/Unit/ViewModels/SongFormViewModelTests.cs`, `ArtistFormViewModelTests.cs`.
- **Demo:** Songs/Artists autocomplete on the emulator returns MusicBrainz results; no Deezer text.
- [ ] Failing tests: `AddAppServices_ResolvesMusicBrainzAsMetadataProvider`, `ArtistListItemDto_MusicBrainzProvider_ShowsMbBadge`.
- [ ] Implement (one XAML at a time). Exit check: `deezer` (case-insensitive) absent from `.cs`/`.xaml` outside Docs, Tests and old Migrations. Commit.

### Task 3: Migration — clear Deezer provenance in place
- **Review lane:** Elevated (data) · **Consumes:** none (raw SQL).
- **Files owned:** `MyVocaList/Infra/Migrations/<timestamp>_ClearDeezerProvenance.cs` + its `.Designer.cs` (generated with `dotnet ef migrations add`, so the snapshot stays consistent; no model change); Test `Tests/Integration/Migrations/ClearDeezerProvenanceTests.cs`.
- **Produces:** `Up`: `UPDATE Songs SET ExternalProvider = NULL, ExternalId = NULL, ImageUrl = NULL WHERE lower(ExternalProvider) = 'deezer'`; same for `Artists`. `Down`: no-op with a comment (spec note: not reversible by design).
- **Demo:** migrated copy of Helder's DB keeps every song id and history row.
- [ ] Failing tests: `Migrate_DeezerSong_KeepsIdTitleLyricsAndClearsProvenance`, `Migrate_KeepsKaraokeUrlsAndHistoryRows`, `Migrate_DbWithoutDeezerRows_NoChange`.
- [ ] Implement; PASS. Commit.

### Task 4: Remove the YouTube Data API (REQ-MSRC-04, -05)
- **Review lane:** Standard · **Consumes:** T2b `SongFormViewModel`.
- **Files owned:** delete `Domain/ServicesInterfaces/IYouTubeSearchService.cs`, `Services/YouTubeSearchService.cs`, `MyVocaList/UI/ViewModels/YouTubeSearchViewModel.cs`, `MyVocaList/UI/Pages/Songs/YouTubeSearchPage.xaml(.cs)`, `YouTubeSearchResultDto`, `Tests/Unit/Services/YouTubeSearchServiceTests.cs`, `Tests/Unit/ViewModels/YouTubeSearchViewModelTests.cs`; modify `ServiceCollectionExtensions.cs:104`, `AppShell.xaml.cs` (route), `SettingsViewModel.cs`, `SettingsPage.xaml`, `SongFormViewModel.cs`, `SongFormPage.xaml:355-400`; Tests `SongFormViewModelTests.cs`, `SettingsViewModelTests.cs`. (Deletions dominate; the edit surface is 6 files.)
- **Produces:** `LaunchYouTubeSearchCommand` opens `https://www.youtube.com/results?search_query=` + URL-encoded `"<title> <artist> karaoke"` via the existing launcher abstraction; Settings init removes secure-storage key `"youtube_api_key"`.
- **Demo:** song form shows "Search on YouTube" and the paste field with no key configured.
- [ ] Failing tests: `LaunchYouTubeSearch_BuildsResultsUrlWithKaraokeSuffix`, `InitializeAsync_RemovesStoredYouTubeKey`.
- [ ] Implement; no `googleapis.com/youtube` string remains. Commit.

### Task 5: YouTube link parsing + Android share target (REQ-MSRC-06)
- **Review lane:** Standard · **Consumes:** T4 paste field.
- **Files owned:** create `Domain/Rules/YouTubeLinkRules.cs`, `Domain/ServicesInterfaces/ISharedLinkInbox.cs`, `MyVocaList/UI/Services/SharedLinkInbox.cs`; modify `MyVocaList/Platforms/Android/MainActivity.cs`, `SongFormViewModel.cs`; Tests `Tests/Unit/Domain/YouTubeLinkRulesTests.cs`, `SongFormViewModelTests.cs`.
- **Produces:** `static bool YouTubeLinkRules.TryExtractVideoId(string? text, out string videoId)` — `youtu.be/{id}`, `(www.|m.|music.)youtube.com/watch?v={id}`, `/shorts/{id}`, `/embed/{id}`; id `[A-Za-z0-9_-]{11}`. `ISharedLinkInbox`: `string? TakePending()`, `event EventHandler? LinkReceived`. **Target song:** the song form currently open receives the link (the user searched from it); if no song form is open, the link stays pending and is applied when the next song form opens, with a snackbar "YouTube link ready to attach". MainActivity: `[IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataMimeType = "text/plain")]`, forwards `EXTRA_TEXT`.
- **Demo:** share from the YouTube app on the emulator → id attached to the open song.
- [ ] Failing tests: `TryExtractVideoId_YoutuBe_ReturnsId`, `_WatchWithTimeAndList_ReturnsId`, `_Shorts_ReturnsId`, `_MobileHost_ReturnsId`, `_SurroundingSpaces_ReturnsId`, `_PlaylistOnly_ReturnsFalse`, `_NonYouTube_ReturnsFalse`; VM `PasteInvalidLink_ShowsErrorAndStoresNothing`, `OnAppearing_PendingSharedLink_AttachesToSong`.
- [ ] Implement; PASS; manual share check. Commit.

### Task 6: In-app IFrame player page (REQ-MSRC-07)
- **Review lane:** Elevated (ToS surface) · **Consumes:** T5 `SongKaraokeUrl.VideoId`.
- **Files owned:** create `MyVocaList/UI/Pages/Songs/YouTubePlayerPage.xaml(.cs)`, `MyVocaList/UI/ViewModels/YouTubePlayerViewModel.cs`, `Services/YouTubePlayerHtmlBuilder.cs`; modify `ServiceCollectionExtensions.cs`, `AppShell.xaml.cs`, `SongFormViewModel.cs`, `SongsViewModel.cs`; Tests `Tests/Unit/Services/YouTubePlayerHtmlBuilderTests.cs`, `Tests/Unit/ViewModels/YouTubePlayerViewModelTests.cs`.
- **Produces:** `string YouTubePlayerHtmlBuilder.Build(string videoId)` — IFrame API page, `playsinline=1`, nothing over the player; video title shown by the player itself (no stored title/thumbnail). Page: `HtmlWebViewSource { Html, BaseUrl = "https://" + AppInfo.PackageName }`, `SafeAreaEdges="Container"`. `OnDisappearing` → VM `StopCommand` (WebView source cleared) so nothing plays in the background.
- **Demo:** video plays, no error 153, YouTube logo visible.
- [ ] Failing tests: `Build_ContainsVideoIdAndPlaysInline`, `Build_InvalidId_ThrowsArgumentException`, `Stop_ClearsSource`.
- [ ] Implement; manual emulator check. Commit. **Halt for Helder's on-device check.**

### Task 7: Artist profile — photo + credit, description, discography (REQ-MSRC-08, -09, -10)
- **Review lane:** Standard · **Consumes:** T1 limiter + `BuildUserAgent`; T2b `ArtistFormViewModel`; rows saved with `ProviderNames.MusicBrainz`.
- **Files owned:** create `Domain/ServicesInterfaces/IArtistProfileService.cs`, `Services/ArtistProfileService.cs`, `Contracts/DTOs/ArtistProfileDto.cs`; modify `ServiceCollectionExtensions.cs`, `ArtistFormViewModel.cs`, `ArtistFormPage.xaml`; Tests `Tests/Unit/Services/ArtistProfileServiceTests.cs`, `Tests/Unit/ViewModels/ArtistFormViewModelTests.cs`.
- **Produces:** `Task<ArtistProfileDto?> GetAsync(string mbid, CancellationToken ct)`; `record ArtistProfileDto(string? ImageUrl, string? ImageCredit, string? ImageLicenseUrl, string? Description, IReadOnlyList<ReleaseGroupDto> Discography)`; `record ReleaseGroupDto(string Mbid, string Title, string? FirstReleaseYear)`. Flow: MusicBrainz `artist/{mbid}?inc=url-rels+release-groups` → `wikidata` relation → `wbgetentities` P18 + description (pt, fallback en) → Commons `imageinfo&iiprop=url|extmetadata&iiurlwidth=500`; credit = `Artist` + `LicenseShortName`; image only when `LicenseShortName` present. Nothing persisted. Network failure → `null` and a non-blocking snackbar in the VM.
- **Demo:** an artist linked to MusicBrainz shows photo + credit + discography.
- [ ] Failing tests: `GetAsync_FullChain_ReturnsUrlCreditDescriptionAndDiscography`, `GetAsync_NoWikidataRelation_ReturnsNullImage`, `GetAsync_NoP18_ReturnsNullImage`, `GetAsync_MissingLicense_ReturnsNullImage`, `GetAsync_NetworkFailure_ReturnsNull`; VM `LoadAsync_NoProfile_ShowsInitialsPlaceholder`, `LoadAsync_ProfileFails_ShowsNonBlockingMessage`.
- [ ] Implement; manual check. Commit.

## Traceability
| REQ | Task |
|---|---|
| 01 | T2a, T2b |
| 02 | T1, T2a |
| 03 | T2a (work grouping deferred) |
| 04, 05 | T4 |
| 06 | T5 |
| 07 | T6 |
| 08, 09 | T7 |
| 10 | T1, T2a, T7 |

## Sizing note
T2a, T4 and T6 exceed the 5-file briefing guide only through test files and deletions; each is one reviewable unit (splitting would leave the build broken between halves). Flagged for Helder rather than split further.
