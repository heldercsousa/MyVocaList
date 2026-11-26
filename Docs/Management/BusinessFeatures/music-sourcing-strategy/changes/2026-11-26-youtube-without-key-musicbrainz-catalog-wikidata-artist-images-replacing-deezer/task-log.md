# Task log — YouTube without key + MusicBrainz + Wikidata artist images

---
## Task: T1 MusicBrainzProvider + rate limiter + ProviderNames (REQ-MSRC-02, -10)
**Plan:** `plan.md` (this folder)
**Status:** To Review
**Started:** 2026-11-26
**Completed:** 2026-11-26

### Changed files:
- `Domain/Constants/ProviderNames.cs`
- `Services/MusicBrainzProvider.cs`
- `Services/MusicBrainzRateLimiter.cs`
- `Services/MusicBrainzResponses.cs`
- `Tests/Unit/Services/MusicBrainzProviderTests.cs`
- `MyVocaList.sln` (task-log registration)

### AC traceability
| AC | Implementation | Test |
|----|----------------|------|
| REQ-MSRC-02 | `MusicBrainzProvider.SearchSongsAsync` / `SearchArtistsAsync` | `SearchSongsAsync_WithArtistHint_SendsLuceneQuery`, `SearchArtistsAsync_MapsMbidAndName` |
| REQ-MSRC-10 | `MusicBrainzRateLimiter`, `BuildUserAgent`, 503 -> `MusicProviderException` | `WaitAsync_TwoCalls_SecondWaitsAtLeast1000ms`, `SearchSongsAsync_Http503_ThrowsMusicProviderException`, `BuildUserAgent_MissingContact_ReturnsBareProductToken` |

### Build notes
Tests: 6 passed, 0 failed (MusicBrainzProviderTests). Android build: see report. Tests were written together with the implementation (no separate Red run). Not wired into DI (T2b). The one-warning-on-missing-contact behaviour is left to the DI/UA wiring in T2b; `BuildUserAgent` only formats.

---
## Task: T2a Services and Domain, drop Deezer branches (REQ-MSRC-01, -02, -03, -10)
**Plan:** `plan.md` (this folder)
**Status:** To Review
**Started:** 2026-11-26
**Completed:** 2026-11-26

### Changed files:
- `Services/SongSuggestionService.cs`, `Services/ArtistSuggestionService.cs` (provider = `ProviderNames.MusicBrainz`, `TryFetchDeezerAsync` -> `TryFetchRemoteAsync`)
- `Services/SongService.cs`, `Domain/Rules/SongIdentityRules.cs`, `Domain/Rules/LyricsOriginRules.cs` (`IsDeezerSong` -> `IsRemoteSong`)
- `Domain/Rules/ImageReferenceRules.cs` (Deezer size-segment rewrite removed)
- Doc-comment only: `Contracts/DTOs/LyricsQuery.cs`, `Contracts/DTOs/Suggestions/*`, `Domain/Constants/SearchConstants.cs`, `Domain/RepositoryInterface/ISongRepository.cs`, `Domain/Resolution/SongFormSaveRequest.cs`, `Domain/ServicesInterfaces/ISongService.cs`, `Services/I*Service.cs`, `Services/IMusicMetadataProvider.cs`
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` (3 call sites only: `IsDeezerSong` -> `IsRemoteSong`, needed to keep the build green; T2b owns the rest)
- Tests: new `Tests/Unit/Services/MusicBrainzSuggestionServiceTests.cs`; updated the Song/Artist suggestion tests, `SongServiceSaveFromFormTests`, `SongIdentityRulesTests`, `ImageReferenceRulesTests`, `SongSuggestionTitleSearchRealStackTests`

### AC traceability
| AC | Implementation | Test |
|----|----------------|------|
| REQ-MSRC-01 | suggestion services consult only `ProviderNames.MusicBrainz` | `SearchTitleRemoteAsync_OnlyDeezerRegistered_DoesNotCallItAndReturnsUnavailable`, `ImageReferenceRules_MusicBrainzSong_HasNoStoredImage` |
| REQ-MSRC-02 | `SongSuggestionService`/`ArtistSuggestionService` via MusicBrainz; save stores provider + MBID | `SearchTitleRemoteAsync_MusicBrainzAnswers_...`, `SearchNameRemoteAsync_MusicBrainzAnswers_...`, `SaveAsync_MusicBrainzPick_StoresProviderAndMbid` |
| REQ-MSRC-03 | rows saved in place by the existing `SongService` path (no recreation); work grouping deferred | `SaveAsync_MusicBrainzPick_StoresProviderAndMbid` |
| REQ-MSRC-10 | provider failure -> `Unavailable`, no exception; debounce constant unchanged | `*_ProviderThrows_ReturnsUnavailableStatusNoException`, `AutocompleteDebounce_KeepsExistingValue` |

### Build notes
Red run first: 3 of 6 new tests failed for the right reason (provider name still "Deezer"). After the change: 2079 tests, 2078 passed on the full run; the one failure (`CatalogRepositoryTests.RemoveCoversAsync_DuplicateIds_CountedOnce`, 1 ms) is unrelated and passed on rerun in isolation (14/14). Android build exit 0, 0 errors.

### Renames for T2b
`SongIdentityRules.IsDeezerSong` -> `IsRemoteSong`. `SongFormViewModel` private names (`HoldsDeezerPick`, `_storedIsDeezerSong`, `DeezerSourceText = "Deezer"`) and `ArtistFormViewModel` `"Deezer"` source label, `ArtistListItem`/`ArtistListItemDto` `"deezer" => "DZ"`, `DeezerProvider` + its DI registration are untouched (T2b). Until T2b wires `MusicBrainzProvider`, no provider named "MusicBrainz" is registered, so remote search reports Unavailable at runtime.

### Design concern
The plan's `SearchTitleRemoteAsync_ProviderThrows_ReturnsOfflineStatusAndLocalResults` and `..._RapidCalls_DebouncedToOneProviderCall` do not match the code: a provider failure maps to `Unavailable` (Offline is only for no network), local results come from a separate call, and the debounce lives in the autocomplete field, not the service. Tests assert the real behaviour (`Unavailable`; debounce constant = 300).

---
## Task: T2b DI + UI, remove Deezer (REQ-MSRC-01, -02, -03, -10)
**Plan:** `plan.md` (this folder)
**Status:** To Review
**Started:** 2026-11-26
**Completed:** 2026-11-26

### Changed files:
- `MyVocaList/Extensions/ServiceCollectionExtensions.cs` (MusicBrainzProvider typed client + singleton `MusicBrainzRateLimiter`; `AddAppServices(IConfiguration? = null)`; one warning when `UserAgent:Contact` is missing)
- `MyVocaList/MauiProgram.cs` (passes `config`), `MyVocaList/appsettings.template.json` (empty `UserAgent:Contact`; real value only in the gitignored `appsettings.json`)
- `Contracts/DTOs/List/ArtistListItemDto.cs`, `Domain/ReadModels/ArtistListItem.cs` ("DZ" removed; "MusicBrainz"/"musicbrainz" => "MB")
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs`, `ArtistFormViewModel.cs` (Deezer names/labels -> Remote/MusicBrainz), `SongFormPage.xaml`, `ArtistFormPage.xaml` (comments only)
- Deleted: `Services/DeezerProvider.cs`, `Tests/Unit/Services/DeezerProvider*Tests.cs` (4 files)
- Tests: new `Tests/Unit/DependencyInjection/MusicBrainzRegistrationTests.cs`; `SongSuggestionTitleSearchRealStackTests` now uses real `MusicBrainzProvider` over a MusicBrainz payload (rename wrapper removed); label expectations updated in `SongFormViewModelTests`, `SongFormViewModelImageTests`, `ArtistFormViewModelTests`

### AC traceability
| AC | Implementation | Test |
|----|----------------|------|
| REQ-MSRC-01/02 | DI registers only MusicBrainz; badge MB | `AddAppServices_ResolvesMusicBrainzAsMetadataProvider`, `ArtistListItemDto_ProviderBadge` |
| REQ-MSRC-10 | contact from config, one warning | `AddAppServices_MissingContact_LogsOneWarning`, `AddAppServices_ConfiguredContact_NoWarning` |

### Build notes
Tests: 2048 passed, 0 failed. Android build: see commit report. Not emulator-checked (deploy left to Helder).
### Design concern
Remaining "deezer" text in `.cs` outside Tests/Docs: `Services/ILyricsProvider.cs`, `ISongLyricsService.cs`, `MusicProviderException.cs` (comments), and stored-data compatibility; not in T2b file list, left for review.

---
## Task: T3 Migration, clear Deezer provenance in place (REQ-MSRC-01)
**Plan:** `plan.md` (this folder)
**Status:** To Review
**Started:** 2026-11-26
**Completed:** 2026-11-26

### Changed files:
- `Infra/Migrations/20261126212504_ClearDeezerProvenance.cs` + `.Designer.cs` (generated; snapshot unchanged, no model change). `Up`: UPDATE Songs/Artists SET ExternalProvider/ExternalId/ImageUrl = NULL WHERE lower(ExternalProvider) = 'deezer'; `Down` no-op (not reversible by design)
- `Tests/Integration/Migrations/ClearDeezerProvenanceTests.cs` (3 tests, real SQLite)
- `Services/ILyricsProvider.cs`, `Services/ISongLyricsService.cs`, `Services/MusicProviderException.cs` (comment-only Deezer mentions removed)

### AC traceability
| AC | Implementation | Test |
|----|----------------|------|
| REQ-MSRC-01 | in-place UPDATE, rows never recreated | `Migrate_DeezerSong_KeepsIdTitleLyricsAndClearsProvenance`, `Migrate_KeepsKaraokeUrlsAndHistoryRows`, `Migrate_DbWithoutDeezerRows_NoChange` |

### Build notes
Red run first: 3/3 failed (migration not found). After: 2051 passed, 0 failed. UPDATE only (no table rebuild), so the FTS5 triggers are untouched. MigrationsLock is a runtime clear before MigrateAsync, unaffected. Android build result in commit report.

---
## Task: T4 Remove the YouTube Data API, add search-out (REQ-MSRC-04, -05)
**Plan:** `plan.md` (this folder)
**Status:** To Review
**Started:** 2026-11-26
**Completed:** 2026-11-26

### Changed files:
- Deleted: `Domain/ServicesInterfaces/IYouTubeSearchService.cs`, `Services/YouTubeSearchService.cs`, `Contracts/DTOs/List/YouTubeSearchResultDto.cs`, `Contracts/Messages/YouTubeVideoPickedMessage.cs`, `MyVocaList/UI/ViewModels/YouTubeSearchViewModel.cs`, `MyVocaList/UI/Pages/Songs/YouTubeSearchPage.xaml(.cs)`, `Tests/Unit/Services/YouTubeSearchServiceTests.cs`, `Tests/Unit/ViewModels/YouTubeSearchViewModelTests.cs`
- New: `Domain/Rules/YouTubeSearchLinkRules.cs` (`BuildResultsUrl(title, artist)` -> `.../results?search_query=<title> <artist> karaoke`), `Tests/Unit/Domain/YouTubeSearchLinkRulesTests.cs`, `Tests/Unit/ViewModels/SettingsViewModelTests.cs`
- `MyVocaList/UI/ViewModels/SettingsViewModel.cs` (only `InitializeAsync` removing secure-storage key `youtube_api_key`, failure logged not thrown; feedback command kept), `MyVocaList/UI/Pages/Settings/SettingsPage.xaml` (key section removed)
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` (no secure storage / API-key flag / picker command; `LaunchYouTubeSearch` uses the rule), `MyVocaList/UI/Pages/Songs/SongFormPage.xaml(.cs)` ("Search on YouTube" + paste field kept; API-key list item and "go to settings" block removed; `OnAppearing` no longer async)
- `MyVocaList/Extensions/ServiceCollectionExtensions.cs`, `MyVocaList/MauiProgram.cs`, `MyVocaList/AppShell.xaml.cs`, `MyVocaList/Navigation/Routes.cs` (registration + route removed)
- Tests: removed `ISecureStorageWrapper` argument from every `SongFormViewModel` construction (`SongFormViewModel*Tests`, `LoadingHoldTests`, `SongSuggestionTitleSearchRealStackTests`); dropped the BUG-020 `RefreshApiKeyFlagAsync` test (method removed)

### AC traceability
| AC | Implementation | Test |
|----|----------------|------|
| REQ-MSRC-04 | service, key setting, search page and route deleted; stored key removed on Settings open | `InitializeAsync_RemovesStoredYouTubeKey`, `InitializeAsync_SecureStorageThrows_DoesNotThrow`, `BuildResultsUrl_NeverUsesDataApiHost` |
| REQ-MSRC-05 | `YouTubeSearchLinkRules.BuildResultsUrl` + `SongFormViewModel.LaunchYouTubeSearch` | `BuildResultsUrl_BuildsResultsUrlWithKaraokeSuffix`, `BuildResultsUrl_TrimsAndEncodesSpecialCharacters` |

### Build notes
Red run first: compile failure (rule type missing, old `SettingsViewModel` constructor) for the right reason. After: 2040 passed, 0 failed. Android build: 0 errors. Not emulator-checked.

### Design concern
- Plan named the test `LaunchYouTubeSearch_BuildsResultsUrlWithKaraokeSuffix` over a "launcher abstraction"; none exists (static `Launcher`), so the URL is built by a pure rule and tested there.
- Out of T4's file list, left as is: `SongsViewModel.LaunchYouTubeSearchAsync` still builds `karaoke <title> <artist>` itself (should use the rule); `SongFormViewModel` keeps an unused `IMessenger` dependency; `Resources/Raw/releases.json` mentions YouTube (release notes text).

---
## Task: T5 — YouTube link rules + Android share target (REQ-MSRC-06)
**Plan:** `plan.md` (this folder)
**Status:** To Review
**Started:** 2026-11-26
**Completed:** 2026-11-26

### Changed files:
- New: `Domain/Rules/YouTubeLinkRules.cs`, `Domain/ServicesInterfaces/ISharedLinkInbox.cs` (adds `Receive`/`HasListener` beyond the plan so the activity can hand text in), `MyVocaList/UI/Services/SharedLinkInbox.cs`, `Tests/Unit/Domain/YouTubeLinkRulesTests.cs`
- `Services/SongKaraokeUrlService.cs` (`ExtractVideoId` delegates to the rule; old regex removed), `MyVocaList/Platforms/Android/MainActivity.cs` (ACTION_SEND text/plain filter, `OnCreate`/`OnNewIntent` forward `EXTRA_TEXT`), `MyVocaList/MauiProgram.cs` (singleton registration), `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs` (start/stop listening, apply pending)
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs`: unused `IMessenger` replaced by `ISharedLinkInbox` (same ctor slot); `ApplyPendingSharedLinkAsync`, `Start/StopListeningForSharedLinks`
- T4 leftover: `MyVocaList/UI/ViewModels/SongsViewModel.cs` uses `YouTubeSearchLinkRules.BuildResultsUrl`
- Tests: `SongFormViewModelTests.cs` (+3), `AppServicesRegistrationTests.cs`, and the `IMessenger` mock swapped for `ISharedLinkInbox` in every `SongFormViewModel` construction

### AC traceability
| AC | Implementation | Test |
|----|----------------|------|
| REQ-MSRC-06 | `YouTubeLinkRules.TryExtractVideoId`, `SharedLinkInbox`, `SongFormViewModel.ApplyPendingSharedLinkAsync` | `TryExtractVideoId_*` (8), `PasteInvalidLink_ShowsErrorAndStoresNothing`, `OnAppearing_PendingSharedLink_AttachesToSong`, `ApplyPendingSharedLink_NothingPending_DoesNothing` |

### Build notes
Red run first: compile failure (`YouTubeLinkRules` missing). After: 2055 passed, 0 failed (one unrelated flaky `PersonIdentityIndexTests` failure seen once, passed on rerun). Android build result in the commit report. Not emulator-checked (share from YouTube app still to verify).

---
## Task: T6 — IFrame player page (REQ-MSRC-07)
**Plan:** `plan.md` (this folder)
**Status:** To Review (halt for Helder's on-device check)
**Started:** 2026-11-26
**Completed:** 2026-11-26

### Changed files:
- New: `Services/YouTubePlayerHtmlBuilder.cs`, `MyVocaList/UI/ViewModels/YouTubePlayerViewModel.cs`, `MyVocaList/UI/Pages/Songs/YouTubePlayerPage.xaml(.cs)`, `Tests/Unit/Services/YouTubePlayerHtmlBuilderTests.cs`, `Tests/Unit/ViewModels/YouTubePlayerViewModelTests.cs`
- `MyVocaList/Navigation/Routes.cs` (`YouTubePlayer`), `MyVocaList/AppShell.xaml.cs` (route), `MyVocaList/MauiProgram.cs` (page + VM transient; the plan named `ServiceCollectionExtensions`, but pages/VMs are registered here), `MyVocaList/UI/ViewModels/SongFormViewModel.cs` (`PlayUrlCommand`), `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` (play button per saved link)

### AC traceability
| AC | Implementation | Test |
|----|----------------|------|
| REQ-MSRC-07 | `YouTubePlayerHtmlBuilder.Build`, `YouTubePlayerPage` (BaseUrl = `https://` + package id), `StopCommand` on disappearing | `Build_ContainsVideoIdAndPlaysInline`, `Build_InvalidId_ThrowsArgumentException`, `Stop_ClearsSource`, `SettingVideoId_*` |

### Build notes
8 new tests pass. Android build: 0 errors. Play entry point is a button on each saved link in the song form (`SongsViewModel` untouched). Tests were written together with the types, so no separate red run.
Full suite: 2063 passed, 0 failed. E2E: emulator-5554 package service was dead (DEAD_OBJECT) so install/launch could not be verified; restart denied by the permission classifier. Requires manual verification (play button on a saved link in Song form).

---
## Task: T7 — Artist profile: photo + credit, description, discography (REQ-MSRC-08, -09, -10)
**Plan:** `plan.md` (this folder)
**Status:** To Review
**Started:** 2026-11-26
**Completed:** 2026-11-26

### Changed files:
- New: `Domain/ServicesInterfaces/IArtistProfileService.cs`, `Services/ArtistProfileService.cs`, `Contracts/DTOs/ArtistProfileDto.cs` (+ `ReleaseGroupDto`), `Tests/Unit/Services/ArtistProfileServiceTests.cs`
- `MyVocaList/Extensions/ServiceCollectionExtensions.cs` (typed client + interface), `MyVocaList/UI/ViewModels/ArtistFormViewModel.cs`, `MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml`, `Tests/Unit/ViewModels/ArtistFormViewModelTests.cs`, `Tests/Unit/ViewModels/ArtistFormViewModelImageTests.cs` (ctor arg only)

### AC traceability
| AC | Implementation | Test |
|----|----------------|------|
| REQ-MSRC-08 | `ArtistProfileService` (MB -> Wikidata P18 -> Commons, licence required), form profile block | `GetAsync_FullChain_*`, `_NoWikidataRelation_*`, `_NoP18_*`, `_MissingLicense_*`, `LoadProfileAsync_NoProfile_ShowsInitialsPlaceholder` |
| REQ-MSRC-09 | description pt->en, release groups | `GetAsync_FullChain_*`, `LoadProfileAsync_FullProfile_*` |
| REQ-MSRC-10 | `GetAsync` returns null on failure; VM snackbar | `GetAsync_NetworkFailure_ReturnsNull`, `LoadProfileAsync_ProfileFails_ShowsNonBlockingMessage` |

### Build notes
Red: compile failure first (types missing). Then 8 new tests pass; full suite 2071 passed, 0 failed. Android build: 0 errors. Profile loads in edit mode only, for rows with `ExternalProvider = MusicBrainz` (add-mode pick has no profile yet). Registered in `ServiceCollectionExtensions` as planned (not MauiProgram). Not emulator-checked (no emulator work requested).
