# Tasks — Song artist field: correctness fixes + inline "create new artist"

Consolidated effort (Helder, 2026-07-21). Single worktree on a task branch off `develop` (all tasks touch the same handlers — single-writer, strictly sequential). DRY Onion where types are added: Model → ViewModel → View. Each bug fix uses regression-test-first (Red→Green) per `bug-tracking.md`.

## Sequence (strictly ordered — same files throughout)

- [x] **T1 — BUG-050 lock-on-select (Critical).** Regression test first: assert `IsArtistLocked` is false→true across `SelectArtist`; confirm it fails. Then add `IsArtistLocked = true;` in `SongFormViewModel.SelectArtist`. Green. (REQ-ACREATE-12) Files: `SongFormViewModel.cs`, `SongFormViewModelTests.cs`.
- [x] **T2 — BUG-051 stale-search race (Major).** Regression test first: two out-of-order `SearchArtistsAsync` completions → assert the latest query's results win. Then add per-request generation/cancellation in `SearchArtistsAsync`; assign `ArtistSuggestions` only if latest. Green. (REQ-ACREATE-13) Files: `SongFormViewModel.cs`, `SongFormViewModelTests.cs` (+ possibly thread token from `SongFormPage.xaml.cs`).
- [ ] **T3 — Retain-text on blur (REQ-ACREATE-03).** Test first: blur with unmatched text → `ArtistSearchText` retained + `ArtistHasError` true. Then change the no-lock branch of `OnArtistBlurredWithoutSelection` to keep the text. Green. Files: `SongFormViewModel.cs`, `SongFormViewModelTests.cs`.
- [ ] **T4 — Re-verify BUG-052 (Major).** Pass condition: opening a saved song in edit mode shows the stored artist name with `IsArtistLocked = true`. After T1–T3, verify this holds; if the field still shows empty, add the `_isHydrating`-based origin guard so `InitializeArtistField` hydration fires no search, and add a guard regression test (asserts no search command executes during hydration). (REQ-ACREATE-14) Files: `SongFormViewModel.cs`, `SongFormPage.xaml.cs` (only if guard needed), tests.
- [ ] **T5 — DX capability spike (≤30 min, no production code).** Via Context7 (DevExpress 25.2.4), confirm `AutoCompleteEdit` renders + allows selecting a synthetic row not derived from typed text. One-line finding in task-log: Option A confirmed, or fall back to Option B. Gates T7's affordance.
- [ ] **T6 — `AutocompleteSuggestion` create-sentinel discriminator.** Add `IsCreateNew` (default false) + carry raw text. Level C (DTO). Files: `MyVocaList.Contracts/.../AutocompleteSuggestion.cs`. Build.
- [ ] **T7 — Inline create wiring.** `SongFormViewModel.CreateArtistInlineCommand` (AsyncRelayCommand<string> → `CreateArtistAsync`, success reuses the now-fixed lock path, failure maps error + retains text). `OnArtistItemsRequested` appends the sentinel from `e.Text`; `OnArtistSelectionChanged` routes `IsCreateNew` → create, else `SelectArtistCommand`. **Level A TDD** for the VM command (success-locks, failure-maps-retains). Files: `SongFormViewModel.cs`, `SongFormPage.xaml.cs`, `SongFormViewModelTests.cs`. (If T5 → Option B, wire the on-no-match button instead.)
- [ ] **T8 — `ItemTemplate` distinct render (`SongFormPage.xaml`).** DataTrigger on `IsCreateNew` → leading ➕ + top divider (REQ-ACREATE-02). Incremental single-file XAML edit → build → fix. (Option B: the on-no-match `DXButton` layout instead.)
- [ ] **T9 — Full suite + AC traceability matrix.** `dotnet test` green (baseline 501/501 + new tests); expand the matrix to one row per AC (REQ-ACREATE-01…14) in task-log.
- [ ] **T10 — On-device re-run [MANUAL, Helder].** Re-run the DX-AC T7 checklist items (a,b,c,e,i,j) — all must pass — plus inline-create: novel artist → ➕ → created + locked + song saves with new `ArtistId`; exact-existing name via ➕ → duplicate error, no orphan. On all-green, close BUG-027 / BUG-050 / BUG-051 / BUG-052 and unblock the Artists & Songs Catalog.

## Follow-ups to register separately (NOT in this feature)
- Tech-debt: extract dirty-tracking + character-counter → `ViewModelBase` (governed refactor).
- Gap: ArtistForm `DuplicateSuggestions` dead stub.
- Future: fuzzy near-duplicate artist detection (the deferred Form UX Redesign's "similar-match warning").
- Deferred UX items from the DX-AC task-log (ArtistFormPage picker→autocomplete; Song-title autocomplete vs lyric-versions).
