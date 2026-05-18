# YouTube Karaoke — Tasks

> **Status:** Spec in progress — not yet ready for implementation
> **Plan:** TBD — to be written after spec approval

---

## Phase 1 — Domain + Infra

- [ ] **Define SongKaraokeUrl entity and ISongKaraokeUrlRepository** [P]
  - Produces: `MyVocaList.Domain/Entities/SongKaraokeUrl.cs`, `MyVocaList.Domain/Interfaces/ISongKaraokeUrlRepository.cs`
  - Consumes: `Song.cs` (existing)
  - Risk: Low
  - Files owned: both above
  - Demo: Entity compiles; interface defines all methods

- [ ] **EF Core migration + SongKaraokeUrlConfiguration + Repository** [SEQUENTIAL — after entity]
  - Produces: `*_AddSongKaraokeUrls.cs`, `SongKaraokeUrlConfiguration.cs`, `SongKaraokeUrlRepository.cs`
  - Consumes: `SongKaraokeUrl.cs`, `ISongKaraokeUrlRepository.cs`
  - Risk: Medium — composite PK, cascade delete
  - Files owned: migration, configuration, repository, `AppDbContext.cs`
  - Demo: `dotnet ef migrations add` succeeds; table created with correct schema

---

## Phase 2 — Contracts + Services

- [ ] **Define DTOs** [P]
  - Produces: `SongKaraokeUrlDto.cs`, `YouTubeSearchResultDto.cs`
  - Consumes: nothing
  - Risk: Low
  - Files owned: both DTOs

- [ ] **Implement ISongKaraokeUrlService + URL normalisation** [SEQUENTIAL — after repo]
  - Produces: `ISongKaraokeUrlService.cs`, `SongKaraokeUrlService.cs`
  - Consumes: `ISongKaraokeUrlRepository.cs`, DTOs
  - Risk: Medium — regex normalisation, duplicate detection
  - Files owned: both service files
  - Demo: `ExtractVideoId` handles all 4 URL formats; AddUrlAsync rejects duplicates

- [ ] **Implement IYouTubeSearchService** [P — independent of URL service]
  - Produces: `IYouTubeSearchService.cs`, `YouTubeSearchService.cs`
  - Consumes: `YouTubeSearchResultDto.cs`
  - Risk: Medium — external HTTP, API key validation
  - Files owned: both files
  - Demo: Returns empty list when no key; returns results when valid key present

- [ ] **Implement INextSingerAlertService** [P — independent]
  - Produces: `INextSingerAlertService.cs`, `NextSingerAlertService.cs`
  - Consumes: nothing external
  - Risk: Medium — local notification scheduling, cancellation
  - Files owned: both files
  - Demo: Schedules two notifications at correct offsets; CancelAlertsAsync cancels both

---

## Phase 3 — Android Overlay

- [ ] **Implement OverlayService (Android platform project)** [SEQUENTIAL — after NextSingerAlertService]
  - Produces: `Platforms/Android/Services/OverlayService.cs`, `IOverlayService.cs`, `NoOpOverlayService.cs`
  - Consumes: `INextSingerAlertService.cs`
  - Risk: High — WindowManager, ForegroundService, ObjectAnimator, permission flow
  - Files owned: above + `AndroidManifest.xml`
  - Demo: Blinking label appears over another app when permission granted; NoOp compiles on iOS

---

## Phase 4 — UI

- [ ] **Extend SongFormViewModel with URL management** [SEQUENTIAL — after services]
  - Produces: updated `SongFormViewModel.cs`
  - Consumes: `ISongKaraokeUrlService`, `IYouTubeSearchService`
  - Risk: Medium
  - Files owned: `SongFormViewModel.cs`
  - Demo: Commands for add/remove/search update observable collections correctly

- [ ] **Extend SongFormPage XAML with YouTube URLs section** [SEQUENTIAL — after ViewModel]
  - Produces: updated `SongFormPage.xaml`, `SongFormPage.xaml.cs`
  - Consumes: updated ViewModel
  - Risk: Medium — new DXCollectionView section, trailing buttons, search strip
  - Files owned: both page files
  - Demo: URL section renders; add/remove/search/paste all functional

- [ ] **Extend Settings page with YouTube API key field** [P — independent of form]
  - Produces: updated `SettingsPage.xaml`, `SettingsPage.xaml.cs` (or ViewModel)
  - Consumes: `IYouTubeSearchService.ValidateApiKeyAsync`, `SecureStorage`
  - Risk: Low
  - Files owned: Settings page files
  - Demo: Key saved to SecureStorage; Test button shows valid/invalid; clear removes key

---

## Phase 5 — Tests

- [ ] **Unit tests: SongKaraokeUrlService** [P]
  - Covers: ExtractVideoId (all 4 formats + invalid), AddUrlAsync duplicate detection, GetSuggestedUrlAsync ordering
  - Risk: Level A

- [ ] **Unit tests: NextSingerAlertService** [P]
  - Covers: scheduling at correct offsets, edge cases (duration ≤ 45s, ≤ 15s, null)
  - Risk: Level A

- [ ] **Integration tests: SongKaraokeUrlRepository** [P]
  - Covers: add, remove, composite PK uniqueness, cascade delete with Song
  - Risk: Level B
