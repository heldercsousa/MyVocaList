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

---

## T6 — Full suite + on-device evaluation checklist (2026-07-20)

**Status:** To Review — automated evidence complete; checklist below is T7 (Helder, on device).

### Automated evidence (post-merge, on `develop`)
- Merge commit: `feat/dx-autocompleteedit-replacement` merged `--no-ff` into develop, verifier verdict CONDITIONAL PASS (no blockers).
- `dotnet test` on develop after merge: **Com falha: 0, Aprovado: 501, Total: 501** (11 s). Matches the post-T5 branch result — merge introduced no regression.
- Test-count delta vs pre-change baseline: 522 → 501 (−21 = the 6 excluded frozen-component test files, REQ-DXAC-11).
- Solution build: 0 errors (DX1000/DX1001 trial-license warnings only, pre-existing).

### Code-review findings carried into T7
- **W1 (fixed):** `requirements.md` REQ-DXAC-01 and `design.md` binding table said `HasError`/`ErrorText`; corrected to `ArtistHasError`/`ArtistErrorText` to match the actual VM members.
- **W3 (fixed):** stale comments referencing the frozen component removed (`GlobalUsings.cs`, `SongFormViewModel.cs`).
- **W2 (open — watch item for checklist item e):** in both `OnArtistItemsRequested`/`OnNameItemsRequested`, `token.ThrowIfCancellationRequested()` runs *after* `await …Command.ExecuteAsync(text)`, so a superseded request still mutates the VM's shared suggestions collection; cancellation only stops the provider from *displaying* it. Observable risk is a brief stale popup — exactly what item (e) exercises. If (e) fails, this is the root cause to fix.

### On-device checklist `[T7 — MANUAL, Helder]`

Run on a physical Android device. Mark each ✅/❌; any ❌ gets a BUG-NNN row per `bug-tracking.md` before the feature closes.

- [ ] **(a) REQ-DXAC-03 — typed text survives everything (BUG-027 core).** Song form: type a partial artist name, then in turn — tap outside (blur), dismiss the popup with the back gesture, rotate the device, switch apps and return. After each, the typed text must still be exactly what you typed. Repeat on the Person form's Full Name field.
- [ ] **(b) REQ-DXAC-04 — selection.** Song form: type until suggestions appear, tap one → artist is set and the field locks per existing behavior. Person form: tap a dedup suggestion → existing selection flow runs. Neither may clear the field.
- [ ] **(c) REQ-DXAC-05 — blur validation.** Song form: type text matching no artist, blur without selecting → the existing error appears via the editor's own error display (no separate error label, no native dialog). Person form: same via `ValidateNameCommand`.
- [ ] **(d) REQ-DXAC-06 — no client-side filtering.** Type a query whose match depends on DB collation, e.g. `cafe` when the stored artist is `Café` (and the reverse). The popup must show **exactly** what the Service returned — if the Service matches it, it appears. A result that the Service returned but the popup hides means a client filter is active (regression).
- [ ] **(e) REQ-DXAC-07 — debounce + stale results.** Type quickly (faster than 300 ms/char), then pause. Only the final query's results may be displayed; no flicker of an earlier query's results after the last one lands. See W2 above if this fails.
- [ ] **(f) REQ-DXAC-12 — visual match.** Both autocomplete fields must be visually indistinguishable from the adjacent Outlined `TextEdit` fields on the same form (border, focus color, label float, background) in both light and dark theme.
- [ ] **(g) BUG-044 / BUG-045 / BUG-047 residual check.** Re-run each bug's original reproduction steps on the new control. Record per bug: **resolved by the swap** / **still present** (→ new BUG row, since the old component is frozen and the fix must land in the DX wiring).
- [ ] **(h) Smoke 16C.1 (REQ-DXAC-10).** Full smoke run green.
- [ ] **(j) BUG-047 guard loss — programmatic text hydration `[HIGH PRIORITY]`.** Open an **existing** song for editing (artist pre-filled) and an **existing** person for editing (name pre-filled). On open, no suggestion popup may appear and no search may fire — the field is being hydrated programmatically, not typed. Then confirm the pre-filled text is intact and editable. See the analysis below for why this is the most likely regression of the whole swap.
- [ ] **(i) BUG-027 re-verification (REQ-DXAC-09).** Confirm the original BUG-027 symptom is gone; if so the Artists & Songs Catalog blocker is cleared.

### Where the prior BUG-044/045/047 fixes went after the swap (analysis, 2026-07-20)

Both fix branches (`fix/bug-044-045-autocomplete-regressions`, `fix/bug-047-autocomplete-trigger`) are fully merged ancestors of develop — no in-flight work competes with this change, and their worktrees were removed. But the two fixes fared very differently under the freeze:

| Bug | Fix commit | Fix lives in | Survives the swap? |
|---|---|---|---|
| BUG-044 / BUG-045 | `219af83` | `PersonFormViewModel.cs`, `NavigationService.cs`, `INavigationService.cs` | **Yes** — all still compiled; regression test `PersonFormViewModelBug044Tests.cs` is under `Unit/ViewModels/` and still runs (it is not in the csproj exclusion list). |
| BUG-047 | `5fba78d` | `UI/Components/AutocompleteField/AutocompleteField.xaml.cs` | **No** — that file is in the frozen family and is now excluded from compilation. Its regression test `AutocompleteFieldProgrammaticTextGuardTests.cs` is one of the 6 excluded test files, so nothing fails to warn us. |

**Consequence:** the BUG-047 guard is gone. It stopped a *programmatic* `Text` hydration (opening a form for editing) from being treated as user typing and firing a stale suggestions search. Nothing in the new DX wiring reproduces that guard — `AsyncItemsSourceProvider` sees a text change without knowing whether a human or the ViewModel caused it. `CharacterCountThreshold` does not help, because a hydrated value is well past the threshold.

This is the highest-probability regression of the whole change, and it compounds with W2: a spurious hydration search plus the late cancellation check could surface a suggestion popup over a freshly opened edit form. Checklist item **(j)** exists to catch exactly this. If (j) fails, the fix belongs in the DX wiring (suppress the request when the text change originates from the ViewModel), and it needs a new regression test — the old one is no longer compiled and cannot be revived as-is.
