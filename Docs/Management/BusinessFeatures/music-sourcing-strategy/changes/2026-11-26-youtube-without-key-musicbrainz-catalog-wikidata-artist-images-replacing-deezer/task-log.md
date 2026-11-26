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
