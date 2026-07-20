# Task Log — DX AutoCompleteEdit replacement

**Plan:** `plan.md` (this folder) · **Branch:** `feat/dx-autocompleteedit-replacement` (worktree `MyVocaList-wt-dx-autocomplete`)

---
## Task: T2 — MaterialStyles.xaml AutoCompleteEdit form style
**Status:** To Review · **Started/Completed:** 2026-07-20

### Changed files:
- `MyVocaList/Resources/Styles/MaterialStyles.xaml`

### Build notes
Build (net10.0-android): passed — `ok dotnet build: 6 projects, 0 errors, 2 warnings`. Commit `17c5b31`. File re-read after edit: style present at lines 73–80, mirrors the adjacent implicit `dx:TextEdit` style setter-for-setter (REQ-DXAC-12). No tests (Level C — style resource only).

---
## Task: T3 — SongFormPage Artist field swap
**Status:** To Review · **Started/Completed:** 2026-07-20

### Changed files:
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs`

### Build notes
Build (net10.0-android): `0 errors` (first attempt — `e.RequestAsync` accepted the async lambda returning `IEnumerable<AutocompleteSuggestion>`). Tests: `dotnet test --filter FullyQualifiedName~SongFormViewModel` → **36 passed, 0 failed** (suite unchanged — REQ-DXAC-08). Commit `aa28874`. Files re-read after edits (element at lines 28–53; three handlers present).

Wiring (Option A per findings.md): `AsyncItemsSourceProvider RequestDelay="300" CharacterCountThreshold="1"` (matches VM gate: whitespace-only check → any 1 non-space char); `ItemsRequested` fulfilled via `e.RequestAsync` → `await SearchArtistsCommand.ExecuteAsync(text)` then returns `ArtistSuggestions`, honoring `e.CancellationToken`; `SelectionChanged` code-behind → `SelectArtistCommand`; `Unfocused` with `SelectedItem == null` → `ArtistBlurredWithoutSelectionCommand`. Error bindings kept verbatim from the removed element (`ArtistHasError`/`ArtistErrorText`). Code-behind forwards events only — no business logic. Typed text is never written by any handler (REQ-DXAC-03).

### Deviation
findings.md preferred `SelectionChangedCommand`, with a fallback clause "if the command receives no/wrong parameter, fall back to the SelectionChanged code-behind handler". The command's parameter shape is not statically verifiable (XML docs silent; DX demo-app MCP index returned empty again), and a null parameter would NRE `SelectArtist`. Took the findings' own fallback: code-behind `SelectionChanged` reading `SelectedItem` (plan's canonical Option A code).

---
## Task: T4 — PersonFormPage Full Name field swap
**Status:** To Review · **Started/Completed:** 2026-07-20

### Changed files:
- `MyVocaList/UI/Pages/People/PersonFormPage.xaml`
- `MyVocaList/UI/Pages/People/PersonFormPage.xaml.cs`

### Build notes
Build (net10.0-android): `0 errors` (first attempt). Tests: `dotnet test --filter FullyQualifiedName~PersonFormViewModel` → **33 passed, 0 failed** (suite unchanged — REQ-DXAC-08). Commit `e2294c8`. Files re-read after edits.

Wiring mirrors T3: `CharacterCountThreshold="2"` (REQ-DXAC-02; VM's 2-char gate retained as defense in depth); `RequestAsync` → `SearchPersonsCommand`; selection → `SuggestionSelectedCommand`; blur-without-selection → `ValidateNameCommand`. `x:Name="nameField"` kept so the existing `OnAppearing` focus call is untouched. Same `SelectionChanged` fallback deviation as T3.

---
## Task: T5 — Exclude frozen component family from build
**Status:** To Review · **Started/Completed:** 2026-07-20

### Changed files:
- `MyVocaList/MyVocaList.csproj`
- `MyVocaList.Tests/MyVocaList.Tests.csproj`
- `MyVocaList/UI/Components/AutocompleteField/README-FROZEN.md` (new; under `MyVocaList/`, not `Docs/` — no `.sln` SolutionItems entry needed per plan)

### Build notes
Pre-check: `grep -r AutocompleteField MyVocaList/UI/Pages/` → no matches. Before test count: `dotnet test MyVocaList.Tests` → **522 passed, 0 failed**. After exclusions: `dotnet build MyVocaList.sln` → `8 projects, 0 errors`; `dotnet test MyVocaList.Tests` → **501 passed, 0 failed** (delta −21 = the 6 excluded component test files; REQ-DXAC-11). Commit `2c579de`.

---
### AC traceability (T2–T5 scope)
| AC | Implementation | Evidence |
|---|---|---|
| REQ-DXAC-01 | SongFormPage.xaml `dxe:AutoCompleteEdit` bound to existing VM members | build 0 errors; VM tests 36/36; manual E2E pending (T7) |
| REQ-DXAC-02 | PersonFormPage.xaml, `CharacterCountThreshold=2` + VM gate | build 0 errors; VM tests 33/33; manual E2E pending (T7) |
| REQ-DXAC-03 | no handler/binding writes `Text`; VM behavior unchanged | code review of glue; manual E2E pending |
| REQ-DXAC-04 | `SelectionChanged` → existing selection commands | manual E2E pending |
| REQ-DXAC-05 | `Unfocused` (no selection) → existing blur/validation commands; DX `HasError`/`ErrorText` | manual E2E pending |
| REQ-DXAC-06 | `AsyncItemsSourceProvider` shows request results as-is (no client filter, per findings) | manual E2E item in T6 checklist |
| REQ-DXAC-07 | `RequestDelay=300` + provider `CancellationToken` cancellation | manual E2E pending |
| REQ-DXAC-08 | VM suites unchanged | 36/36 + 33/33 + full 501/501 green |
| REQ-DXAC-11 | csproj `Compile`/`MauiXaml Remove` + README-FROZEN.md | sln build 0 errors; test delta 522→501 |
| REQ-DXAC-12 | implicit `dx:AutoCompleteEdit` style | build 0 errors; visual check pending (T7) |

E2E note: emulator not run in this session — pages are user-facing; on-device verification is the plan's T6/T7 (Helder checklist). Per-task status therefore `To Review` with manual E2E explicitly pending (equivalent to `Check build` for the UI-visible ACs).
