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

- [❌] **(a) REQ-DXAC-03 — typed text survives everything (BUG-027 core).** Song form: type a partial artist name, then in turn — tap outside (blur), dismiss the popup with the back gesture, rotate the device, switch apps and return. After each, the typed text must still be exactly what you typed. Repeat on the Person form's Full Name field. (Helder: - type partialy a matching artist name, autocomplete showed found options, no item tapped in autocomplete options, and blur the entry clear the typed text. If I tap an item, then clear the selected one with X tap, and repeat the test, the last tapped option reappears like a magic) 
- [❌] **(b) REQ-DXAC-04 — selection.** Song form: type until suggestions appear, tap one → artist is set and the field locks per existing behavior. Person form: tap a dedup suggestion → existing selection flow runs. Neither may clear the field. (Helder: - after selected an item, entry isn't locked. 
- [❌] **(c) REQ-DXAC-05 — blur validation.** Song form: type text matching no artist, blur without selecting → the existing error appears via the editor's own error display (no separate error label, no native dialog). Person form: same via `ValidateNameCommand`.
- [✅] **(d) REQ-DXAC-06 — no client-side filtering.** Type a query whose match depends on DB collation, e.g. `cafe` when the stored artist is `Café` (and the reverse). The popup must show **exactly** what the Service returned — if the Service matches it, it appears. A result that the Service returned but the popup hides means a client filter is active (regression).
- [❌] **(e) REQ-DXAC-07 — debounce + stale results.** Type quickly (faster than 300 ms/char), then pause. Only the final query's results may be displayed; no flicker of an earlier query's results after the last one lands. See W2 above if this fails. (helder: - it's producing weird results, sometimes brings partialy the matching ones, like discarding some of the artists that must appear. Sometime it just return "not found" wrongly once the typed name actually matchs existing ones. I noticed a pattern: when type J, Jéssica is listed. Then, I removed J and typed B, when Jéssica again was listed. So, it didn't used the B to search but still used the prior J)
- [✅] **(f) REQ-DXAC-12 — visual match.** Both autocomplete fields must be visually indistinguishable from the adjacent Outlined `TextEdit` fields on the same form (border, focus color, label float, background) in both light and dark theme.
- [✅] **(g) BUG-044 / BUG-045 / BUG-047 residual check.** Re-run each bug's original reproduction steps on the new control. Record per bug: **resolved by the swap** / **still present** (→ new BUG row, since the old component is frozen and the fix must land in the DX wiring).
- [later] **(h) Smoke 16C.1 (REQ-DXAC-10).** Full smoke run green.
- [❌] **(j) BUG-047 guard loss — programmatic text hydration `[HIGH PRIORITY]`.** Open an **existing** song for editing (artist pre-filled) and an **existing** person for editing (name pre-filled). On open, no suggestion popup may appear and no search may fire — the field is being hydrated programmatically, not typed. Then confirm the pre-filled text is intact and editable. See the analysis below for why this is the most likely regression of the whole swap. (Helder:-when editing song, artist entry is empty when must fill the saved - if really was saved. Person edit worked as excpected)
- [❌] **(i) BUG-027 re-verification (REQ-DXAC-09).** Confirm the original BUG-027 symptom is gone; if so the Artists & Songs Catalog blocker is cleared. (Helder:-when tap catalog button in artistpage for an artist that has 1 song registered, or, that should have being registered in the test above, no action happens. It depends on prior test success once the true artist registration in the song form is really happening)

### T7 outcome (2026-07-21) — Helder ran the on-device checklist; root-cause triage

Result: **3 pass (d, f, g), 6 fail (a, b, c, e, i, j), h deferred.** The swap did **not** close BUG-027 and introduced new defects. Confirmed root causes (read-only code trace):

- **(b) → BUG-050 (Critical, NEW).** `SongFormViewModel.SelectArtist` (lines 283–292) sets `SelectedArtistId/Name`, `ArtistSearchText`, clears suggestions/errors — but **never sets `IsArtistLocked = true`**. The view wiring (`OnArtistSelectionChanged` → `SelectArtistCommand` with a real `AutocompleteSuggestion`) and the `IsEnabled`/`InverseBool` binding are all correct; the lock simply is never raised. The API-import path `ResolveAndLockArtistAsync` (line 411) sets it correctly — proof of intent. One-line omission.
- **(e) → BUG-051 (Major, NEW; = the W2 watch-item realized).** `e.Text` is **not** stale. `ArtistSuggestions` (VM field) is written by every in-flight `SearchArtistsAsync` with no cancellation/sequencing; out-of-order completion lets an older query ("J") overwrite a newer one ("B"). `SearchArtistsCommand` is a bare `AsyncRelayCommand<string>` allowing concurrent re-entrancy; `token.ThrowIfCancellationRequested()` in code-behind only guards DX's display, not the VM network call. Fix belongs in `SearchArtistsAsync` (per-request generation/token; only assign if still latest).
- **(j) → BUG-052 (Major, NEW; likely compound).** Editing a song shows an empty Artist field. Most likely downstream of BUG-050 (artist never locked ⇒ song saved without `ArtistId` ⇒ nothing to hydrate — Helder: "if really was saved"). Reconfirm after 050/051 fixed; if it persists, it is the item-(j) programmatic-hydration guard gap.
- **(a)/(c) — BUG-027 original symptom still live.** Blur clears typed text (`OnArtistBlurredWithoutSelection` empties `ArtistSearchText`); the "reappear like magic" is the restore-prior-selection branch. Resolved by the agreed **retain-text** decision (below) — delivered by the inline-artist-create feature's REQ-ACREATE-03.
- **(i) — blocked, not independent.** Catalog button no-op because no song was truly registered with an artist (cascades from BUG-050/052).

**Consequence for sequencing:** the inline-artist-create spec's REQ-ACREATE-04 ("reuse the existing lock path") is invalid until BUG-050 is fixed, and all these defects live in the same handlers inline-create would edit (single-writer). The field must be made correct before/with inline-create. New rows: BUG-050/051/052 in BACKLOG.

### Where the prior BUG-044/045/047 fixes went after the swap (analysis, 2026-07-20)

Both fix branches (`fix/bug-044-045-autocomplete-regressions`, `fix/bug-047-autocomplete-trigger`) are fully merged ancestors of develop — no in-flight work competes with this change, and their worktrees were removed. But the two fixes fared very differently under the freeze:

| Bug | Fix commit | Fix lives in | Survives the swap? |
|---|---|---|---|
| BUG-044 / BUG-045 | `219af83` | `PersonFormViewModel.cs`, `NavigationService.cs`, `INavigationService.cs` | **Yes** — all still compiled; regression test `PersonFormViewModelBug044Tests.cs` is under `Unit/ViewModels/` and still runs (it is not in the csproj exclusion list). |
| BUG-047 | `5fba78d` | `UI/Components/AutocompleteField/AutocompleteField.xaml.cs` | **No** — that file is in the frozen family and is now excluded from compilation. Its regression test `AutocompleteFieldProgrammaticTextGuardTests.cs` is one of the 6 excluded test files, so nothing fails to warn us. |

**Consequence:** the BUG-047 guard is gone. It stopped a *programmatic* `Text` hydration (opening a form for editing) from being treated as user typing and firing a stale suggestions search. Nothing in the new DX wiring reproduces that guard — `AsyncItemsSourceProvider` sees a text change without knowing whether a human or the ViewModel caused it. `CharacterCountThreshold` does not help, because a hydrated value is well past the threshold.

This is the highest-probability regression of the whole change, and it compounds with W2: a spurious hydration search plus the late cancellation check could surface a suggestion popup over a freshly opened edit form. Checklist item **(j)** exists to catch exactly this. If (j) fails, the fix belongs in the DX wiring (suppress the request when the text change originates from the ViewModel), and it needs a new regression test — the old one is no longer compiled and cannot be revived as-is.

### Code trace confirming the (j) risk (2026-07-20)

A read-only trace of both ViewModels and both `ItemsRequested` handlers found **no origin guard anywhere on the path**:

- `SongFormViewModel.InitializeArtistField()` assigns `ArtistSearchText = ArtistName` with no guard, and the page calls it from `OnAppearing` *after* `CompleteHydration()`. Opening an existing song therefore pushes text into the editor programmatically.
- Three further unguarded programmatic assignments exist: `OnArtistBlurredWithoutSelection()` (two paths) and `ResolveAndLockArtistAsync()` (two paths, inside `RunOnUiThread`).
- `PersonFormViewModel` never assigns `PersonName` in C#, but edit-mode pre-population still arrives programmatically via the Shell `[QueryProperty]`, so the same binding-driven path exists.
- Both ViewModels carry an `_isHydrating` flag with a `CompleteHydration()` method — but it guards only **dirty-flag marking** (`OnSongTitleChanged`, `OnSongVersionChanged`, `OnPersonNameChanged`). It is not consulted on any autocomplete text assignment.
- Neither `OnArtistItemsRequested` nor `OnNameItemsRequested` inspects the origin of the text change; both execute the search command unconditionally on `e.Text`.

Conclusion: item (j) is a **live** risk on the Song form and probably on the Person form. Any fix should likely reuse the existing `_isHydrating` concept rather than inventing a second flag.

### ⚠ ESCALATION — possible REQ-DXAC-03 conflict `[ Helder's decision was registered below]`

The same trace reports that `SongFormViewModel.OnArtistBlurredWithoutSelection()` clears the field:

```csharp
if (!SelectedArtistId.HasValue || SelectedArtistId.Value == 0)
{
    ArtistSearchText = string.Empty;   // typed text discarded on blur
    ArtistSuggestions = [];
}
```

REQ-DXAC-03 states typed text is *never* cleared on blur and that "under no circumstance does the user lose their entry (BUG-027 core criterion)" — yet the preserved ViewModel deliberately empties it when the user typed something that resolved to no artist. The new wiring binds `Unfocused` → `ArtistBlurredWithoutSelectionCommand`, so this behavior carries over unchanged.

If that reading is right, swapping the control does **not** by itself satisfy REQ-DXAC-03 or close BUG-027, because the clearing lives in the ViewModel, not the control. Approach A deliberately preserved ViewModel contracts, so this was never in the swap's scope.

Per `workflow.md` (spec is source of truth; a spec/code conflict stops for Helder) this is **not** being fixed unilaterally — the resolution changes intended behavior:
- If REQ-DXAC-03 is literal, the ViewModel must stop clearing on blur, which is a ViewModel behavior change needing its own task, spec update, and regression test.
- If clearing an unresolvable entry is intended product behavior, REQ-DXAC-03 needs rewording to carve out that case.

Checklist item **(a)** exercises this directly — type an artist name that matches nothing, then blur. Note: this finding comes from a code trace, not from reading the file directly; Helder should confirm against the source before acting.

#### What BUG-027's original wording adds

The bug was filed (2026-07-03, from the TEST-001 emulator run) as:

> "SongFormPage Artist field — no required-field validation, no autocomplete, **blur clears typed text with no create-new fallback** (Critical)"

The clearing and the missing create-new path were reported as *one* symptom, and that coupling explains the ViewModel's behavior: it discards an unresolvable entry precisely because there is nowhere for that entry to go. But "no match → add new" was deliberately scoped **out** of this change as a separate follow-up (Helder's decision, 2026-07-19). So the swap delivers two of BUG-027's three parts — validation and autocomplete — while the half that motivated the clearing stays deferred.

That leaves a coherence problem independent of REQ-DXAC-03: **REQ-DXAC-05 requires blur validation to surface an error on the field**, but if blur has just emptied the field, the error describes an input the user can no longer see. Retaining the text *and* showing the error is the self-consistent behavior, and it is what both -03 and -05 read as intending.

This narrows the decision to a genuine product question rather than a spec-wording cleanup:
- **Retain text + show error** (satisfies -03 and -05 together; smallest change; leaves the entry recoverable until add-new ships) — this is the reading the specs support, but it is still a ViewModel behavior change and needs its own task.
- **Keep clearing** — then -03 must be reworded, and BUG-027 cannot be closed by this change, because its headline symptom survives.

---

## Deferred UX investigation — autocomplete in form/edit contexts `[Helder 2026-07-20]`

Raised by Helder after the UX research turn. Recorded here as investigate-and-decide items; **none are in scope for the DX-AC swap** — they seed future tasks. Helder's decisions and corrections are captured verbatim below so the follow-up specs inherit them.

### Agreed (research recommendation accepted)
Helder: *"2. I agree."* — the research reframing stands:
- **No-match blur → retain typed text + show validation error** (satisfies REQ-DXAC-03 and -05 together). Still a ViewModel behavior change; needs its own task + regression test (the (j) guard fix rides alongside).
- **Picking an autocomplete item → the picked entity becomes the locked reference** (form switches to its selected/edit state).

### Correction — the primary autocomplete candidate is `ArtistFormPage`, not the Song artist field
Helder's original UX question was about **`ArtistFormPage.xaml`**, which today offers a separate *"Search music database"* picker (`NavigateToArtistPickerCommand`) plus a local-catalog duplicate-suggestion list. His view: that picker is **bad UX — it adds clicks** to pick from a predicted data source.

- **Investigate:** replace the picker-navigation with inline **autocomplete on the Artist Name field**, drawing suggestions from (a) the local catalog (the existing dedup source) and (b) the predicted/music-database source the picker currently reaches. The pick behavior should mirror what this feature's spec/plan/code already define for autocomplete selection.
- **Decide:** whether the "Search music database" picker is fully retired or kept as a secondary/advanced path once inline autocomplete exists.
- MD3 / component-governance note: `AutocompleteField`-class controls are governed components — any new consumer goes through the four-gate process (`component-change-governance.md`).

### On `SongFormPage`
- **Artist field** = pure **reference picker** (foreign key to Artist). Re-pointing it swaps only the `ArtistId` + display name; it does **not** load or overwrite the song's other data, and needs **no** confirmation dialog. Helder agrees ("not the primary data of song"). No further UX question here beyond the -03/-05 clearing decision above.
- **Song *title* field** is the genuine autocomplete candidate on this page (it *is* the song's primary data). Helder observes the current autocomplete-selection behavior already shows "medium-to-large alignment" with the agreed pattern — **lacks investigation**, flagged for it.
  - **Entanglement to investigate:** the title autocomplete intersects the existing **lyric-versions preservation** business logic — the resolution/merge flow (`resolutionSheet` / `mergeSheet`, `HasManualEdits`, per-field `FieldDiff` accept toggles) that tracks and preserves manual lyric edits and keeps version distinctions recognizable. Selecting a title suggestion must not silently clobber a manually-edited lyric version. Any title-autocomplete spec must be designed *around* this logic, not bolted on.

### Routing
These become their own BACKLOG-tracked tasks (spec → spec-review → plan → plan-review → implement → review), sequenced after the DX-AC swap closes. They are **not** to be bundled into BUG-027 closure.
