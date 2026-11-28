# Task log - share-to-song-form change

---
## Task: S2 Share to Song form routing + S3 prefill from the video title (REQ-MSRC-11, -12, -13)
**Plan:** README.md of this change
**Status:** To Review
**Started:** 2026-11-27
**Completed:** 2026-11-27

### Changed files:
- `Domain/Rules/KaraokeTitleRules.cs` (new)
- `Services/ISharedVideoService.cs`, `Services/SharedVideoService.cs` (new)
- `Contracts/DTOs/SharedVideoDtos.cs` (new), `Contracts/DTOs/YouTubeVideoInfo.cs`, `Contracts/DTOs/MusicSearchResultDto.cs`, `Contracts/DTOs/Suggestions/SongSuggestionDto.cs`
- `Domain/RepositoryInterface/ISongKaraokeUrlRepository.cs`, `Infra/Repository/SongKaraokeUrlRepository.cs`
- `Domain/Resolution/SongFormSaveRequest.cs`, `Services/SongService.cs`, `Services/SongSuggestionService.cs`, `Services/YouTubeOEmbedClient.cs`
- `MyVocaList/UI/Services/SharedLinkInbox.cs`, `MyVocaList/UI/ViewModels/SongFormViewModel.cs`, `MyVocaList/Extensions/ServiceCollectionExtensions.cs`
- Tests: `KaraokeTitleRulesTests`, `SharedVideoServiceTests`, `SharedLinkInboxTests`, `SongFormViewModelSharedPrefillTests`, `SongServiceSaveFromFormArtistProfileTests` (new); `SongSuggestionServiceDurationTests`, `YouTubeOEmbedClientTests`, `Bug068RegressionTests` and the SongFormViewModel test constructors (one added mock)
- `README.md` (S2/S3 ticked, spec note), `task-log.md`

### AC traceability
| AC | Implementation | Tests |
|----|----------------|-------|
| REQ-MSRC-11 | `SharedLinkInbox.RouteAsync`, `SharedVideoService.ResolveTargetAsync`, `SongKaraokeUrlRepository.FindSongIdByVideoIdAsync` | `SharedLinkInboxTests`, `SharedVideoServiceTests.ResolveTarget*`, `SongFormViewModelSharedPrefillTests` (link in list) |
| REQ-MSRC-12 | `KaraokeTitleRules`, `SharedVideoService.PrefillAsync`, `SongFormViewModel.PrefillFromVideoAsync` (+ existing lyrics lookup on a pick) | `KaraokeTitleRulesTests`, `SharedVideoServiceTests.Prefill*`, `SongFormViewModelSharedPrefillTests` |
| REQ-MSRC-13 | `SongService.SaveFromFormAsync` (artist MBID, `AdoptNewArtistPhotoAsync`), VM `_pendingArtistExternalId` | `SongServiceSaveFromFormArtistProfileTests`, `SongFormViewModelSharedPrefillTests.Save_*`, `SongSuggestionServiceDurationTests` |

### Build notes
Unit tests: 2164 passed, 0 failed. Android build and emulator check: see commit message / final report.
