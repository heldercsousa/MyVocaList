# Tasks — Inline "create new artist" on the Song form

DRY Onion order: Model → ViewModel → View. Single-writer; all edits in a git worktree on a task branch off `develop`.

- [ ] **T1 — DX capability check (SPIKE, ≤30 min).** Via Context7 (DevExpress 25.2.4), confirm `AutoCompleteEdit` renders + allows selecting a synthetic suggestion not derived from typed text. Produce a one-line finding in task-log: Option A confirmed, or fall back to Option B. Gates T3–T5's affordance mechanism. No production code.
- [ ] **T2 — `AutocompleteSuggestion` create-sentinel discriminator.** Add `IsCreateNew` (default false) + carry raw text. Level C (DTO), no mandatory test. Files: `MyVocaList.Contracts/.../AutocompleteSuggestion.cs`. Build.
- [ ] **T3 — `SongFormViewModel.CreateArtistInlineCommand` + blur retain.** New `AsyncRelayCommand<string>` → `CreateArtistAsync`, success locks via the existing lock path, failure maps error + retains text. Adjust `OnArtistBlurredWithoutSelection` no-lock branch to retain typed text (REQ-ACREATE-03). **Level A — TDD:** write failing `SongFormViewModelTests` first (success-locks, failure-maps-retains, blur-retains), then implement. Files: `SongFormViewModel.cs`, `SongFormViewModelTests.cs`.
- [ ] **T4 — Page wiring (`SongFormPage.xaml.cs`).** `OnArtistItemsRequested` appends the create sentinel from `e.Text`; `OnArtistSelectionChanged` routes `IsCreateNew` → `CreateArtistInlineCommand`, else existing `SelectArtistCommand`. Glue only. Build + VM tests.
- [ ] **T5 — `ItemTemplate` distinct render (`SongFormPage.xaml`).** DataTrigger on `IsCreateNew` → leading ➕ + top divider (REQ-ACREATE-02). Incremental single-file XAML edit → build → fix. (If T1 → Option B: replace with the on-no-match `DXButton` layout instead.)
- [ ] **T6 — Full suite + AC traceability matrix.** `dotnet test` green (baseline 501/501 + new tests); fill the matrix in task-log. Update requirements.md traceability seed.
- [ ] **T7 — On-device E2E [MANUAL, Helder].** Novel artist → ➕ → created + locked + song saves with new `ArtistId`; exact-existing name via ➕ → duplicate error, no orphan; blur with unmatched text → text retained.

## Follow-ups to register separately (NOT in this feature)
- Tech-debt: extract dirty-tracking + character-counter → `ViewModelBase` (governed refactor).
- Gap: ArtistForm `DuplicateSuggestions` dead stub.
- Future: fuzzy near-duplicate artist detection (shared service method) — the deferred Form UX Redesign's "similar-match warning".
