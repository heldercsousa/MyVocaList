# Song Artist Field — Correctness Fixes + Inline "Create New Artist" Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the Song form's Artist autocomplete correct (fix BUG-050/051/052 + retain-text) and let the admin create a new artist inline from an unmatched typed name — one sequenced effort in a single worktree.

**Architecture:** No new layers. Creation/validation already live in `ArtistService` (constitutional: business logic in Services). This is UI/ViewModel plumbing: `SongFormViewModel` gains one inline-create command and corrected lock/search/blur behavior; `SongFormPage.xaml(.cs)` appends a synthetic ➕ suggestion row and routes its selection; `AutocompleteSuggestion` gains a create-sentinel discriminator. All tasks touch the same handlers, so the sequence is **strictly serial, single-writer**.

**Tech Stack:** .NET MAUI 10 · C# 13 · CommunityToolkit.Mvvm (`AsyncRelayCommand<T>`) · DevExpress MAUI 25.2.4 (`AutoCompleteEdit`) · EF Core 10 / SQLite · xUnit + Moq (Service-layer mocking) · Serilog.

## Global Constraints

- **Single worktree, task branch off `develop`** — verify `git merge-base --is-ancestor develop HEAD` after creation; never base on `main`. All code edits happen in the worktree.
- **Strictly sequential T1→T10** — every task edits the same shared handlers (`SelectArtist`, `SearchArtistsAsync`, `OnArtistBlurredWithoutSelection`, `OnArtistItemsRequested`/`OnArtistSelectionChanged`). Single-writer; no parallel wave.
- **Regression-test-first for every bug fix (Red→Green):** BUG-050 Critical = mandatory Red then Green; BUG-051/052 Major = mandatory where testable (Service/VM). Test must be **seen to fail** before the fix.
- **Business logic in Services only** — no creation/validation rule re-implemented in `SongFormViewModel`. Reuse `IArtistService.CreateArtistAsync` / `ValidateNameInput`.
- **DevExpress-first · no native dialogs** (`DisplayAlert`/`DisplayActionSheet`/`DisplayPromptAsync` forbidden) · **English-only** · **incremental single-file XAML edits** (edit one file → build → fix → next) · `SafeAreaEdges="Container"` already on the page.
- **Baseline test count: 501/501 green.** New tests add to that; never weaken or delete a test to pass (Builder-must-not-modify-tests).
- **Commit after every task** via `/sln-commit`. `git push` blocks in Claude shells — **commit only; Helder pushes.**
- **Level-A TDD** for the inline-create VM command and all correctness fixes; **Level C** (no mandatory test) for the DTO field (T6) and XAML render (T8, covered by on-device E2E).

---

## File Structure

**Review lanes:** T1 & T7 = **Elevated** (BUG-050 Critical; new command; shared-handler blast radius). T2–T4, T6, T8, T9 = **Standard**. T5 = spike (findings review). T10 = manual (Helder).

| File | Responsibility | Tasks |
|------|----------------|-------|
| `MyVocaList/ViewModels/SongFormViewModel.cs` | Corrected lock/search/blur; new `CreateArtistInlineCommand` | T1–T4, T7 |
| `MyVocaList.Tests/ViewModels/SongFormViewModelTests.cs` (path per test project) | Regression + Level-A behavior tests | T1–T4, T7, T9 |
| `MyVocaList.Contracts/…/AutocompleteSuggestion.cs` | `IsCreateNew` sentinel + raw text | T6 |
| `MyVocaList/Views/SongFormPage.xaml.cs` | Append sentinel; route selection; hydration guard (if needed) | T4, T7 |
| `MyVocaList/Views/SongFormPage.xaml` | `ItemTemplate` distinct ➕ render | T8 |

> **Orchestrator note:** exact paths, line numbers, and existing method bodies are confirmed by the implementor subagent when it opens each file. The line references below come from verified code traces in the handoff (`SelectArtist` ~L283–292, `SearchArtistsAsync` ~L274–281, `ResolveAndLockArtistAsync` lock at ~L411) and are starting anchors, not literal edit coordinates.

---

## Task 0: Worktree setup (main agent, shell only)

- [ ] **Step 1: Create worktree on a task branch off develop**

```bash
git worktree add ../MyVocaList-inline-ac -b feat/inline-artist-create develop
```

- [ ] **Step 2: Verify develop is the base**

Run: `git -C ../MyVocaList-inline-ac merge-base --is-ancestor develop HEAD && echo OK`
Expected: `OK`

- [ ] **Step 3: Confirm baseline green**

Run: `dotnet test` (from the worktree)
Expected: PASS, 501/501.

---

## Task 1 (T1): BUG-050 — lock on select (Critical)

**Files:**
- Modify: `MyVocaList/ViewModels/SongFormViewModel.cs` — `SelectArtist` (~L283–292)
- Test: `SongFormViewModelTests.cs`

**Interfaces:**
- Consumes: existing `SongFormViewModel` ctor (Moq `IArtistService`), `SelectArtistCommand`, `SelectedArtistId`, `SelectedArtistName`, `IsArtistLocked`, `ArtistSuggestions`.
- Produces: `IsArtistLocked == true` after a suggestion is selected — the invariant every later lock-reuse (T7) depends on.

- [ ] **Step 1: Write the failing regression test**

```csharp
// [AC] REQ-ACREATE-12 (BUG-050): selecting a suggestion locks the field
[Fact]
public async Task SelectArtist_ExistingSuggestion_LocksField()
{
    var vm = CreateSongFormViewModel();               // existing test helper
    var suggestion = new AutocompleteSuggestion { Id = 7, Headline = "Queen" };
    Assert.False(vm.IsArtistLocked);                  // precondition

    vm.SelectArtistCommand.Execute(suggestion);       // (confirm exact command/param shape in file)

    Assert.True(vm.IsArtistLocked);
    Assert.Equal(7, vm.SelectedArtistId);
}
```

- [ ] **Step 2: Run — verify it FAILS**

Run: `dotnet test --filter SelectArtist_ExistingSuggestion_LocksField`
Expected: FAIL — `IsArtistLocked` is false after select (the bug).

- [ ] **Step 3: Minimal fix** — in `SelectArtist`, add `IsArtistLocked = true;` alongside the existing `SelectedArtistId`/`SelectedArtistName` assignments (mirrors `ResolveAndLockArtistAsync` ~L411).

- [ ] **Step 4: Run — verify PASS** (`dotnet test --filter SelectArtist_ExistingSuggestion_LocksField` → PASS)

- [ ] **Step 5: Full suite** (`dotnet test` → 502/502) then `/sln-commit`.

---

## Task 2 (T2): BUG-051 — stale-search race (Major)

**Files:**
- Modify: `SongFormViewModel.cs` — `SearchArtistsAsync` (~L274–281)
- Test: `SongFormViewModelTests.cs`

**Interfaces:**
- Consumes: `SearchArtistsCommand`/`SearchArtistsAsync(string query)`, `ArtistSuggestions`, mocked `IArtistService.SearchArtistsByNameAsync`.
- Produces: only the latest query's results ever populate `ArtistSuggestions`.

- [ ] **Step 1: Write the failing regression test** — drive two out-of-order completions via a controllable mock.

```csharp
// [AC] REQ-ACREATE-13 (BUG-051): latest query wins over a slower earlier one
[Fact]
public async Task SearchArtists_OutOfOrderCompletion_LatestWins()
{
    var older = new TaskCompletionSource<IReadOnlyList<Artist>>();
    var newer = new TaskCompletionSource<IReadOnlyList<Artist>>();
    _artistServiceMock
        .SetupSequence(s => s.SearchArtistsByNameAsync(It.IsAny<string>(), It.IsAny<int>()))
        .Returns(older.Task)
        .Returns(newer.Task);
    var vm = CreateSongFormViewModel();

    var t1 = vm.SearchArtistsAsync("que");   // older request (issued first)
    var t2 = vm.SearchArtistsAsync("queen"); // newer request (issued second)

    newer.SetResult(new[] { new Artist { Id = 2, Name = "Queen" } });   // newer completes first
    await t2;
    older.SetResult(new[] { new Artist { Id = 9, Name = "Querido" } }); // older completes late
    await t1;

    Assert.Single(vm.ArtistSuggestions);
    Assert.Equal("Queen", vm.ArtistSuggestions[0].Headline);            // older must NOT clobber
}
```

- [ ] **Step 2: Run — verify it FAILS** (older completion overwrites `ArtistSuggestions`).
Run: `dotnet test --filter SearchArtists_OutOfOrderCompletion_LatestWins` → FAIL.

- [ ] **Step 3: Minimal fix (generation counter — Helder-approved 2026-07-21)** — add `int _searchGeneration`; at the start of `SearchArtistsAsync` do `var gen = ++_searchGeneration;`, then after the await assign `ArtistSuggestions` **only if** `gen == _searchGeneration`. (Adjust member name to the file's conventions.) CancellationToken-threading was considered and rejected in favor of this self-contained approach.

- [ ] **Step 4: Run — verify PASS** (filter → PASS).

- [ ] **Step 5: Full suite** (`dotnet test` → 503/503) then `/sln-commit`.

---

## Task 3 (T3): Retain typed text on blur (REQ-ACREATE-03)

**Files:**
- Modify: `SongFormViewModel.cs` — `OnArtistBlurredWithoutSelection` (no-locked-artist branch only)
- Test: `SongFormViewModelTests.cs`

**Interfaces:**
- Consumes: `ArtistSearchText`, `ArtistHasError`, `IsArtistLocked`, the blur handler.
- Produces: on unmatched blur, `ArtistSearchText` is retained (not cleared) and `ArtistHasError` is true.

- [ ] **Step 1: Write the failing test**

```csharp
// [AC] REQ-ACREATE-03: blur with unmatched text retains it and surfaces error
[Fact]
public void BlurWithoutSelection_UnmatchedText_RetainsTextAndErrors()
{
    var vm = CreateSongFormViewModel();
    vm.ArtistSearchText = "Nonexistent Band";        // no selection made

    vm.OnArtistBlurredWithoutSelection();            // confirm exact invocation in file

    Assert.Equal("Nonexistent Band", vm.ArtistSearchText); // retained, not cleared
    Assert.True(vm.ArtistHasError);
    Assert.False(vm.IsArtistLocked);
}
```

- [ ] **Step 2: Run — verify it FAILS** (current BUG-008 path clears the text).
Run: `dotnet test --filter BlurWithoutSelection_UnmatchedText_RetainsTextAndErrors` → FAIL.

- [ ] **Step 3: Minimal fix** — in the no-locked-artist branch of `OnArtistBlurredWithoutSelection`, remove the `ArtistSearchText = string.Empty;` clear; keep surfacing the validation error. Leave the "restore prior selection" branch unchanged.

- [ ] **Step 4: Run — verify PASS** (filter → PASS).

- [ ] **Step 5: Full suite** (`dotnet test` → 504/504) then `/sln-commit`.

---

## Task 4 (T4): Re-verify / guard BUG-052 (Major)

**Files:**
- Modify (only if guard needed): `SongFormViewModel.cs` — `InitializeArtistField` hydration; possibly `SongFormPage.xaml.cs`
- Test: `SongFormViewModelTests.cs`

**Interfaces:**
- Consumes: `InitializeArtistField`, existing `_isHydrating` concept, `SelectedArtistName`, `IsArtistLocked`, `SearchArtistsCommand`.
- Produces: edit-mode hydration shows the stored artist name + `IsArtistLocked == true`, firing **no** suggestion search.

- [ ] **Step 1: Assess** — with T1–T3 committed, reason about whether edit-mode hydration now shows the locked artist (BUG-052 was compound with BUG-050). If a device recheck is still pending, this task adds the guard defensively.

- [ ] **Step 2: Write the guard regression test** (add regardless — it locks the invariant)

```csharp
// [AC] REQ-ACREATE-14 (BUG-052): hydration shows locked artist and fires no search
[Fact]
public void InitializeArtistField_EditMode_ShowsLockedArtist_NoSearch()
{
    var vm = CreateSongFormViewModel();

    vm.InitializeArtistField(artistId: 5, artistName: "Pink Floyd"); // confirm exact signature

    Assert.Equal("Pink Floyd", vm.SelectedArtistName);
    Assert.True(vm.IsArtistLocked);
    _artistServiceMock.Verify(
        s => s.SearchArtistsByNameAsync(It.IsAny<string>(), It.IsAny<int>()),
        Times.Never);                                  // hydration must not search
}
```

- [ ] **Step 3: Run** (`dotnet test --filter InitializeArtistField_EditMode_ShowsLockedArtist_NoSearch`). If it PASSES already, log "BUG-052 resolved by T1; guard test added" and skip Step 4. If it FAILS on the search-fired assertion, proceed.

- [ ] **Step 4: Minimal fix (only if Step 3 failed)** — set the existing `_isHydrating` flag around the hydration assignment so the search-triggering text setter/handler early-returns during programmatic hydration. Re-run → PASS.

- [ ] **Step 5: Full suite** (`dotnet test`) then `/sln-commit`. Record BUG-052 disposition in the task-log.

---

## Task 5 (T5): DX capability spike — synthetic row (≤30 min, NO production code)

**Files:** none (findings only, into `task-log.md`).

- [ ] **Step 1: Query Context7** — `resolve-library-id` → `query-docs` for **DevExpress MAUI 25.2.4** `AutoCompleteEdit`: does it render and allow **selection of a suggestion row not derived from the typed text** (a synthetic row appended to the provider results)? Does it filter provider results against the input?

- [ ] **Step 2: Decide** — Option A (synthetic ➕ row selectable) confirmed → T7/T8 proceed as the row path. Otherwise **Option B fallback** (on-no-match `DXButton` below the field). Do **not** invent a third pattern.

- [ ] **Step 3: Record** — one-line finding in `task-log.md`: "T5: Option A confirmed via Context7 [evidence]" or "T5: Option B — [reason/evidence]". This gates T7/T8's affordance. No commit needed unless task-log is on the worktree (docs land on develop via main agent).

---

## Task 6 (T6): `AutocompleteSuggestion` create-sentinel discriminator

**Files:**
- Modify: `MyVocaList.Contracts/…/AutocompleteSuggestion.cs`
- Test: none (Level C — DTO record; covered downstream by T7/T8).

**Interfaces:**
- Produces: `bool IsCreateNew { get; init; }` (default `false`) and a way to carry the raw typed text (reuse `Headline`/an existing text member, or add `RawText`) — consumed by T7 routing and T8 render.

- [ ] **Step 1: Add the sentinel** — `public bool IsCreateNew { get; init; }` (default false); confirm the raw typed text is carried (existing text property or add one). No behavior change to existing usages.

- [ ] **Step 2: Build** — Run: `dotnet build` → 0 errors.

- [ ] **Step 3: Full suite** (`dotnet test` — unchanged count, all green) then `/sln-commit`. Log the Level-C no-test decision.

---

## Task 7 (T7): Inline create wiring (Level-A TDD)

**Files:**
- Modify: `SongFormViewModel.cs` (new `CreateArtistInlineCommand`), `SongFormPage.xaml.cs` (`OnArtistItemsRequested` append, `OnArtistSelectionChanged` route)
- Test: `SongFormViewModelTests.cs`

**Interfaces:**
- Consumes: `IArtistService.CreateArtistAsync(string name) → (bool success, string message, Artist? artist)`; the corrected lock path from T1 (`SelectArtist`/private lock helper); `AutocompleteSuggestion.IsCreateNew` from T6.
- Produces: `public IAsyncRelayCommand<string> CreateArtistInlineCommand { get; }` — success locks the created artist and clears error; failure maps the message and retains text.

- [ ] **Step 1: Write failing test — success path**

```csharp
// [AC] REQ-ACREATE-04/08: inline create success locks the created artist, clears error
[Fact]
public async Task CreateArtistInline_Success_LocksCreatedArtistAndClearsError()
{
    var created = new Artist { Id = 42, Name = "New Band" };
    _artistServiceMock
        .Setup(s => s.CreateArtistAsync("New Band", It.IsAny<CancellationToken>()))
        .ReturnsAsync((true, string.Empty, created));      // confirm exact signature/overload
    var vm = CreateSongFormViewModel();
    vm.ArtistHasError = true; vm.ArtistErrorText = "old";  // prior error present

    await vm.CreateArtistInlineCommand.ExecuteAsync("New Band");

    Assert.Equal(42, vm.SelectedArtistId);
    Assert.Equal("New Band", vm.SelectedArtistName);
    Assert.True(vm.IsArtistLocked);
    Assert.False(vm.ArtistHasError);
}
```

- [ ] **Step 2: Run — verify FAIL** (`CreateArtistInlineCommand` does not exist).
Run: `dotnet test --filter CreateArtistInline_Success_LocksCreatedArtistAndClearsError` → FAIL (compile/undefined).

- [ ] **Step 3: Write failing test — failure path**

```csharp
// [AC] REQ-ACREATE-05: inline create failure maps error, retains text, no lock
[Fact]
public async Task CreateArtistInline_Failure_MapsErrorAndRetainsText()
{
    _artistServiceMock
        .Setup(s => s.CreateArtistAsync("Dup", It.IsAny<CancellationToken>()))
        .ReturnsAsync((false, "Artist already exists.", (Artist?)null));
    var vm = CreateSongFormViewModel();
    vm.ArtistSearchText = "Dup";

    await vm.CreateArtistInlineCommand.ExecuteAsync("Dup");

    Assert.True(vm.ArtistHasError);
    Assert.Equal("Artist already exists.", vm.ArtistErrorText);
    Assert.Equal("Dup", vm.ArtistSearchText);   // retained
    Assert.False(vm.IsArtistLocked);            // no lock
    Assert.Null(vm.SelectedArtistId);
}
```

- [ ] **Step 4: Run both — verify FAIL** (`dotnet test --filter CreateArtistInline_` → both FAIL).

- [ ] **Step 5: Implement `CreateArtistInlineAsync`** in `SongFormViewModel`:
  - `var (success, message, artist) = await _artistService.CreateArtistAsync(name);`
  - success → call the **same private lock helper `SelectArtist` uses** (set `SelectedArtistId`/`SelectedArtistName`, `IsArtistLocked = true`, clear `ArtistSuggestions`, clear `ArtistHasError`/`ArtistErrorText`) — do not duplicate the lock logic.
  - failure → `ArtistHasError = true; ArtistErrorText = message;` retain `ArtistSearchText`. No dialog.
  - Expose as `public IAsyncRelayCommand<string> CreateArtistInlineCommand => new AsyncRelayCommand<string>(CreateArtistInlineAsync);` (field-backed per file convention).

- [ ] **Step 6: Run — verify PASS** (`dotnet test --filter CreateArtistInline_` → both PASS).

- [ ] **Step 7: Wire the page code-behind** (glue only — no business logic):
  - `OnArtistItemsRequested`: after awaiting the existing search, if `e.Text` has ≥1 non-whitespace char, append one `new AutocompleteSuggestion { IsCreateNew = true, Headline = $"Add \"{e.Text}\" as a new artist", RawText = e.Text }` as the **last** item (REQ-ACREATE-02/10). Empty results → list holds only the create row (REQ-ACREATE-03).
  - `OnArtistSelectionChanged`: if selected `IsCreateNew` → `CreateArtistInlineCommand.Execute(rawText)`; else existing `SelectArtistCommand` path (unchanged).
  - **Option B branch (if T5 chose it):** instead wire an on-no-match `DXButton`; observable create/lock/error/save behavior identical (REQ-ACREATE-11).

- [ ] **Step 8: Build** (`dotnet build` → 0 errors), full suite (`dotnet test` → green), then `/sln-commit`.

---

## Task 8 (T8): `ItemTemplate` distinct ➕ render (XAML)

**Files:**
- Modify: `MyVocaList/Views/SongFormPage.xaml` (only — incremental single-file XAML edit)
- Test: none (Level C — visual; covered by on-device T10).

- [ ] **Step 1: Edit the `AutoCompleteEdit` `ItemTemplate`** — add a `DataTrigger` on `IsCreateNew == true` that renders the row with a leading ➕ glyph and a top divider, visually distinct from real matches (REQ-ACREATE-02). Use existing MD3 divider/typography style keys; DevExpress-first controls. **Option B branch:** style the on-no-match `DXButton` layout instead.

- [ ] **Step 2: Build** — Run: `dotnet build` → 0 errors. Fix XAML errors before proceeding (never batch).

- [ ] **Step 3: Commit** — `/sln-commit`.

---

## Task 9 (T9): Full suite + AC traceability matrix

**Files:** `task-log.md` (matrix; committed to develop by main agent).

- [ ] **Step 1: Run full suite** — Run: `dotnet test`. Expected: green, baseline 501 + new tests (T1–T4, T7 ⇒ ~506+). Record exact count.

- [ ] **Step 2: Build the AC traceability matrix** — one row per AC (REQ-ACREATE-01…14): AC ID | Criterion | Implementation location | Test method. Every user-facing AC maps to a test or to the T10 on-device item. Put it in `task-log.md`. Mark **REQ-ACREATE-11 as "conditionally satisfied — Option A or B branch, per T5 finding"** so the matrix does not silently assume Option A.

- [ ] **Step 3: Commit** the matrix (docs → develop via main agent) / `/sln-commit` for any code.

---

## Task 10 (T10): On-device re-run [MANUAL — Helder]

**Not an agent task.** Helder re-runs the DX-AC T7 checklist items (a, b, c, e, i, j) — all must pass — plus inline-create E2E:
- Novel artist → ➕ → created + locked + song saves with the new `ArtistId` (REQ-ACREATE-04/08).
- Exact-existing name via ➕ → duplicate error surfaced, **no orphan** created (REQ-ACREATE-05).

- [ ] On all-green: close BUG-027 / BUG-050 / BUG-051 / BUG-052; unblock the Artists & Songs Catalog. Update BACKLOG + LEDGER.

---

## Self-Review

**Spec coverage:** REQ-ACREATE-01 (T7 append preserves existing search) · -02 (T7 append + T8 render) · -03 (T3 + T7 empty-list) · -04 (T7 success) · -05 (T7 failure) · -06/-07 (T7 reuses `CreateArtistAsync`/`ValidateNameInput`, no prompt) · -08 (T7 + T10 save) · -09 (T9 suite) · -10 (T7 append on any non-ws text) · -11 (T5/T7/T8 Option-B branch) · -12 (T1) · -13 (T2) · -14 (T4). All 14 covered.

**Placeholder scan:** no TBD/"add error handling"/"write tests for the above" — every test step shows code; every fix step names the exact member and change.

**Type consistency:** `IsArtistLocked`, `ArtistSearchText`, `ArtistHasError`, `ArtistErrorText`, `ArtistSuggestions`, `SelectedArtistId`/`SelectedArtistName`, `SelectArtistCommand`, `CreateArtistInlineCommand`, `AutocompleteSuggestion.IsCreateNew`/`RawText` used consistently across T1–T8. Exact existing signatures (`SelectArtistCommand` param, `InitializeArtistField`, `CreateArtistAsync` overload/token, the private lock helper name) are confirmed by the implementor on file-open — flagged inline at each use.
