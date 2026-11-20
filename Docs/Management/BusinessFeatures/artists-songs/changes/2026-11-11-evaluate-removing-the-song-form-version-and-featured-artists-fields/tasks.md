# Remove the Song Version and Featured artists fields — Tasks

Branch: `task/remove-song-version-featured` (worktree off `develop`). Waves are sequential; each wave ends with a green build and green tests. Tests are written or rewritten with the production change they cover (Red → Green per REQ).

## Wave 1 — Domain + Infra (+ their tests)
- [ ] T1 Entity, `SongConfiguration`, repository interfaces/implementations, domain/read-model types; repository tests (REQ-RMV-02, 06, 10)
- [ ] T2 Migration `RemoveSongVersionAndFeaturedArtists` + snapshot; migration tests per rule a–e, `Down`, FK check (REQ-RMV-03, 04, 05; INV-RMV-1/2). Starts after T1 lands.

## Wave 2 — Contracts + Services (+ their tests)
- [ ] T3 DTOs, `SongService`/`ISongService`, `SongResolutionService`; service tests (REQ-RMV-06, 07, 09, 10, 11)
- [ ] T4 `SongSuggestionService`, `DeezerProvider`; tests (REQ-RMV-08)

## Wave 3 — UI (+ VM tests)
- [ ] T5 `SongFormViewModel` + VM tests (REQ-RMV-01, 07, 11)
- [ ] T6 `SongFormPage.xaml(.cs)`: one file → build → next (REQ-RMV-01, 07)

## Wave 4 — Verification and docs
- [ ] T7 Full test run; fresh Grep proves no leftover consumer
- [ ] T8 `spec-changelog.md` line, task-log with AC traceability matrix, `.sln` registration, item status
