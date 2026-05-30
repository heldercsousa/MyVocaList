# YouTube Karaoke — Tasks

> **Status:** Implementation complete — all Phase 1–4 UI tasks done; Phase 5 partial (SongKaraokeUrlService tests done; NextSingerAlertService + Repository tests deferred)
> **Plan:** `Docs/Management/BusinessFeatures/artists-songs/youtube-karaoke/plan.md`

---

## Phase 1 — Domain + Infra ✅

- [x] **Define SongKaraokeUrl entity and ISongKaraokeUrlRepository** [P]
  - Produces: `MyVocaList.Domain/Entities/SongKaraokeUrl.cs`, `MyVocaList.Domain/Interfaces/ISongKaraokeUrlRepository.cs`
  - Consumes: `Song.cs` (existing)
  - Risk: Low
  - Files owned: both above
  - Demo: Entity compiles; interface defines all methods

- [x] **EF Core migration + SongKaraokeUrlConfiguration + Repository** [SEQUENTIAL — after entity]
  - Produces: `*_AddSongKaraokeUrls.cs`, `SongKaraokeUrlConfiguration.cs`, `SongKaraokeUrlRepository.cs`
  - Consumes: `SongKaraokeUrl.cs`, `ISongKaraokeUrlRepository.cs`
  - Risk: Medium — composite PK, cascade delete
  - Files owned: migration, configuration, repository, `AppDbContext.cs`
  - Demo: `dotnet ef migrations add` succeeds; table created with correct schema

---

## Phase 2 — Contracts + Services ✅

- [x] **Define DTOs** [P]
  - Produces: `SongKaraokeUrlDto.cs`, `YouTubeSearchResultDto.cs`
  - Consumes: nothing
  - Risk: Low
  - Files owned: both DTOs

- [x] **Implement ISongKaraokeUrlService + URL normalisation** [SEQUENTIAL — after repo]
  - Produces: `ISongKaraokeUrlService.cs`, `SongKaraokeUrlService.cs`
  - Consumes: `ISongKaraokeUrlRepository.cs`, DTOs
  - Risk: Medium — regex normalisation, duplicate detection
  - Files owned: both service files
  - Demo: `ExtractVideoId` handles all 4 URL formats; AddUrlAsync rejects duplicates

- [x] **Implement IYouTubeSearchService** [P — independent of URL service]
  - Produces: `IYouTubeSearchService.cs`, `YouTubeSearchService.cs`
  - Consumes: `YouTubeSearchResultDto.cs`
  - Risk: Medium — external HTTP, API key validation
  - Files owned: both files
  - Demo: Returns empty list when no key; returns results when valid key present

- [x] **Implement INextSingerAlertService** [P — independent]
  - Produces: `INextSingerAlertService.cs`, `NextSingerAlertService.cs`
  - Consumes: nothing external
  - Risk: Medium — local notification scheduling, cancellation
  - Files owned: both files
  - Demo: Schedules two notifications at correct offsets; CancelAlertsAsync cancels both

---

## Phase 3 — Android Overlay ✅

- [x] **Implement OverlayService (Android platform project)** [SEQUENTIAL — after NextSingerAlertService]
  - Produces: `Platforms/Android/Services/OverlayService.cs`, `IOverlayService.cs`, `NoOpOverlayService.cs`
  - Consumes: `INextSingerAlertService.cs`
  - Risk: High — WindowManager, ForegroundService, ObjectAnimator, permission flow
  - Files owned: above + `AndroidManifest.xml`
  - Demo: Blinking label appears over another app when permission granted; NoOp compiles on iOS

---

## Phase 4 — UI (remaining)

- [x] **Extend SongFormViewModel with URL management** [SEQUENTIAL — after services]
  - Produces: updated `SongFormViewModel.cs`
  - Consumes: `ISongKaraokeUrlService`, `IYouTubeSearchService`
  - Risk: Medium
  - Files owned: `SongFormViewModel.cs`
  - Demo: Commands for add/remove/search update observable collections correctly
  - Committed: 9748de3

- [x] **Create SettingsViewModel + SettingsPage** [SEQUENTIAL — fixes broken build]
  - Produces: `MyVocaList/UI/ViewModels/SettingsViewModel.cs`, `MyVocaList/UI/Pages/Settings/SettingsPage.xaml`, `MyVocaList/UI/Pages/Settings/SettingsPage.xaml.cs`
  - Consumes: `IYouTubeSearchService`, `ISecureStorageWrapper`, `ISnackbarComponent`
  - Risk: Low — new files only; MauiProgram.cs already registers SettingsViewModel + SettingsPage
  - Files owned: three new files above
  - Demo: Settings page renders; API key field saves to SecureStorage; Test and Clear work

- [x] **Create 3 value converters** [P — prerequisite for SongFormPage XAML]
  - Produces: `MyVocaList/UI/Converters/IsNotZeroConverter.cs`, `IsNotNullConverter.cs`, `SecondsToMinutesConverter.cs`
  - Consumes: nothing
  - Risk: Low — new files + 3-line addition in App.xaml ResourceDictionary
  - Files owned: three new converter files, `App.xaml`
  - Demo: Converters registered in App.xaml; SongFormPage XAML builds without StaticResource errors

- [x] **Fix HasYouTubeApiKey gap in SongFormViewModel** [P — independent]
  - Produces: updated `MyVocaList/UI/ViewModels/SongFormViewModel.cs`
  - Consumes: `ISecureStorageWrapper` (already in DI)
  - Risk: Low — adds constructor parameter + 2 lines in `LoadKaraokeUrlsAsync`
  - Files owned: `SongFormViewModel.cs`
  - Demo: `HasYouTubeApiKey` is `true` when a key is stored; search strip shows

- [x] **Extend SongFormPage XAML with YouTube URLs section** [SEQUENTIAL — after converters + VM fix]
  - Produces: updated `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`
  - Consumes: value converters, updated SongFormViewModel
  - Risk: Medium — new DXCollectionView section, search strip, paste field
  - Files owned: `SongFormPage.xaml`
  - Demo: YouTube URLs section renders after lyrics; add/remove/search/paste functional

---

## Phase 5 — Tests

- [x] **Unit tests: SongKaraokeUrlService** [P]
  - Covers: ExtractVideoId (all 4 formats + invalid + FsCheck property tests), AddUrlAsync duplicate detection, RemoveUrlAsync, GetUrlsForSongAsync
  - Risk: Level A

- [ ] **Unit tests: NextSingerAlertService** [P]
  - Covers: scheduling at correct offsets, edge cases (duration ≤ 45s, ≤ 15s, null)
  - Risk: Level A

- [ ] **Integration tests: SongKaraokeUrlRepository** [P]
  - Covers: add, remove, composite PK uniqueness, cascade delete with Song
  - Risk: Level B
