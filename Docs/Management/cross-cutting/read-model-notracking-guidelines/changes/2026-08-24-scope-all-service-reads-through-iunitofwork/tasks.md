# Tasks — Scope all service reads through `IUnitOfWork`

Spec: `./requirements.md` (REQ-UOW-36…52) · `./design.md` (§ 7 work ordering)
Plan: `./plan.md` · Log: `./task-log.md` · Item: `READ-SCOPE` (LEDGER)

> **Wave order is load-bearing.** Waves 1→2→3 are strictly sequential and deliberately invert
> DRY Onion (`design.md § 7` warning block, decision D6). Removing `.AsTracking()` before Task 1.1
> destroys the mandatory BUG-078 Red and invalidates the fix. **Do not "correct" this ordering.**

> **Worktree mandatory.** Every task below edits code — all run in a git worktree on a task branch
> based on `develop` (`workflow.md` Rule 2). Docs (`tasks.md`, `task-log.md`, spec) land on `develop`.

> **Evidence rules.** Exhaustiveness / "zero occurrences" / file-comparison claims are verified by a
> direct **Python file walk**, never `grep`/`rg` (R2; the wrapper returned false zeroes twice).

> **New tests go in NEW files.** REQ-UOW-49 caps edits to *pre-existing* files under
> `MyVocaList.Tests/` at a closed four-row carve-out (two `CreateSut` helpers + two comment rewrites).
> Every test this change adds is therefore a **new file**. Test-file paths below follow the existing
> `MyVocaList.Tests/Integration/UnitOfWork/` and `MyVocaList.Tests/Unit/Services/` convention — match
> the folder layout already in the repo if it differs.

---

## Wave 0 — Search-length constants

- [ ] **0.1** Introduce `SearchConstants` — `MinimumLocalQueryLength = 2`, `MinimumRemoteQueryLength = 3`
  - Produces: `SearchConstants` (Domain layer, beside `CollationConstants`)
  - Consumes: —
  - Risk: **C** (constants only, no call sites) · Est: **15 min**
  - Files owned: `MyVocaList/Domain/Constants/SearchConstants.cs` (new — place it beside the existing
    `CollationConstants` file, wherever that lives)
  - Demo: solution builds; both constants exist with XML doc citing REQ-UOW-51/52 and `design.md § 2c`
  - Review lane: build-only
  - AC: REQ-UOW-51, REQ-UOW-52 (constant form)

## Wave 1 — BUG-078 Red `[SEQUENTIAL — no production file edited]`

- [ ] **1.1** Write the BUG-078 regression test and **see it FAIL**
  - Produces: new integration test reproducing the stale delete-confirmation
  - Consumes: —
  - Risk: **A** · Est: **1 h**
  - Files owned: `MyVocaList.Tests/Integration/UnitOfWork/Bug078RegressionTests.cs` (new)
  - Demo: test-run output showing FAIL with actual `"Delete 'Old Name'?"`, pasted into `task-log.md`
  - Review lane: full
  - AC: REQ-UOW-45 (Red limb)
  - **Guard:** `ArtistRepository.cs:79-80` `.AsTracking()` MUST still be present and `ArtistService`
    unchanged. If the test passes on first run, STOP — the reproduction is wrong, not the bug.

## Wave 2 — BUG-078 Green `[SEQUENTIAL]`

- [ ] **2.1** Wrap `ArtistService.GetDeleteConfirmationAsync`'s `GetByIdAsync` in `ExecuteReadAsync`
  - Produces: the fix
  - Consumes: Task 1.1's failing test
  - Risk: **A** · Est: **30 min**
  - Files owned: `MyVocaList/Services/ArtistService.cs` (`GetDeleteConfirmationAsync` only)
  - Demo: the Wave 1 test now PASSES; both outputs (fail-before / pass-after) in `task-log.md`
  - Review lane: full
  - AC: REQ-UOW-45 (Green limb), **REQ-UOW-41** (the `idList.Count == 1` guard at `:175` stays
    **outside** the lambda — guard-table row 3)
  - **Guard:** `.AsTracking()` is still present at this point. That is correct — it is the honest pair.

## Wave 3 — Remove `.AsTracking()` `[SEQUENTIAL]`

- [ ] **3.1** Delete `.AsTracking()` and rewrite **both** stale comments in the same commit
  - Produces: tracking-free read path
  - Consumes: Waves 1–2 committed
  - Risk: **B** · Est: **45 min**
  - Files owned: `MyVocaList/Infra/Repository/ArtistRepository.cs`,
    `MyVocaList/Infra/AppDbContext.cs` (comment `:36` only — `ChangeTracker.QueryTrackingBehavior`
    at `:37` untouched),
    `MyVocaList.Tests/Integration/UnitOfWork/Bug068RegressionTests.cs` (comment `:41-44` only)
  - Demo: BUG-078 test still green; `Bug068RegressionTests.Artist_CreateThenReadThenUpdate_DoesNotThrowTrackingConflict`
    green **unmodified**; Python file walk over `Infra/Repository/` showing zero `.AsTracking()`
  - Review lane: full
  - AC: REQ-UOW-46
  - **Guards:**
    - **Two** comments, not three. The third `DbLoadGate` comment
      (`CrudListViewModelBaseTests.cs:215`) is REQ-UOW-49 carve-out row 4 and belongs to **Wave 8** —
      **do not touch it here.**
    - A red existing test ⇒ log `blocked: spec gap` (R6). Never restore `.AsTracking()`, never edit
      the test.

## Wave 4 — Service wraps `[P]`

Sub-wave 4a: **4.1–4.4** in parallel (cap 4). Sub-wave 4b: **4.5–4.6** in parallel.
4.6 additionally requires Wave 2 committed (same file).

- [ ] **4.1** `PersonService.cs` — wrap `GetPersonByIdAsync:177`, `GetPersonByNameAsync:181`,
      `SearchPersonsAsync:185`, `SearchPersonsStartsWithAsync:194`, `GetPagedPersonsForListAsync:207`
  - Files owned: `MyVocaList/Services/PersonService.cs`,
    `MyVocaList.Tests/Integration/UnitOfWork/PersonServiceReadScopeTests.cs` (new)
  - Est: **2 h** · Risk: **B** · AC: REQ-UOW-39, -40, -41
- [ ] **4.2** `SongService.cs` — wrap `GetSongByIdAsync:268`, `ExistsByTitleForArtistAsync:272`,
      `GetPagedSongsForListAsync:305`
  - Files owned: `MyVocaList/Services/SongService.cs`,
    `MyVocaList.Tests/Integration/UnitOfWork/SongServiceReadScopeTests.cs` (new)
  - Est: **1.5 h** · Risk: **B** · AC: REQ-UOW-39, -40, -41
- [ ] **4.3** `VenueService.cs` (`GetPagedVenuesForListAsync:189`) + `CatalogService.cs`
      (`GetPagedCatalogForArtistAsync:25`)
  - Files owned: `MyVocaList/Services/VenueService.cs`, `MyVocaList/Services/CatalogService.cs`,
    `MyVocaList.Tests/Integration/UnitOfWork/VenueCatalogServiceReadScopeTests.cs` (new)
  - Est: **1.5 h** · Risk: **B** · AC: REQ-UOW-40, -41
- [ ] **4.4** `SongKaraokeUrlService.cs` — wrap `GetUrlsForSongAsync:38`, `GetSuggestedUrlAsync:106`
  - Files owned: `MyVocaList/Services/SongKaraokeUrlService.cs`,
    `MyVocaList.Tests/Integration/UnitOfWork/SongKaraokeUrlServiceReadScopeTests.cs` (new)
  - Est: **1 h** · Risk: **B** · AC: REQ-UOW-40
- [ ] **4.5** `BackupService.cs` — wrap `GetHistoryAsync:169`, `HasRecentBackupAsync:175`, and
      `ExportBundleAsync:86`'s `_repo.GetLatestSnapshotAsync` call at `:90` **only**
  - Files owned: `MyVocaList/Services/BackupService.cs`,
    `MyVocaList.Tests/Unit/Services/BackupServiceReadScopeTests.cs` (new)
  - Est: **2 h** · Risk: **B** · AC: REQ-UOW-39, -40, **-44**
  - **Guard (REQ-UOW-44):** the wrap goes **INSIDE** the existing `try` at `:88-89`, never around it —
    hoisting it changes the observable failure tuple. `File.Exists:91`, `ZipFile.Open:97` and the
    entry copies stay **outside** the lambda.
- [ ] **4.6** Rest of `ArtistService.cs` — wrap `GetPagedArtistsForListAsync:149`,
      `SearchArtistsByNameAsync:161`, **+ REQ-UOW-51 threshold** in `SearchArtistsByNameAsync`
      (`IsNullOrWhiteSpace(normalized)` → `normalized.Length < SearchConstants.MinimumLocalQueryLength`)
  - Files owned: `MyVocaList/Services/ArtistService.cs`,
    `MyVocaList.Tests/Integration/UnitOfWork/ArtistServiceReadScopeTests.cs` (new)
  - Est: **2 h** · Risk: **A** (behaviour change) · AC: REQ-UOW-40, -41, **-51**
  - Consumes: Wave 0's constants; **Wave 2 committed** (same file)

  For every 4.x task —
  - Produces: scoped reads in the owned service file + the new test file
  - Demo: **(i)** the tests REQ-UOW-39/40/41/44 mandate — one integration test per wrapped method
    against a **real SQLite temp file** (never the in-memory provider), asserting unchanged return
    values for a seeded fixture, plus the two-distinct-`AppDbContext` assertion via REQ-UOW-39's
    observation mechanism (capture `sp.GetRequiredService<AppDbContext>()` from *inside* each lambda,
    `Assert.NotSame`); **(ii)** one unit test per guard-table row owned by this task asserting the
    guard short-circuits with **zero** `ExecuteReadAsync` invocations (counting double);
    **(iii)** existing tests for the file green **unmodified**; **(iv)** Python file walk over the
    owned file showing no `_`-field repository dereference inside any lambda
  - Review lane: full
  - **Guard:** validation/short-circuit logic stays **outside** the lambda (REQ-UOW-41). A `_field.`
    reference inside a lambda compiles and passes tests while leaving the defect in place (R1).
  - **Single-writer:** every file above is owned by exactly one task. Do not create a shared helper in
    a file another 4.x task owns; put shared test infrastructure in Wave 7.2's file or leave it
    duplicated.

## Wave 5 — Suggestion services `[P]` (both after Wave 4)

- [ ] **5.1** `ArtistSuggestionService.cs` — `IUnitOfWork` injection (**2nd of 5** ctor params, after
      `artistRepository`), wrap `GetLocalAsync:36` and the `GetByNamesCollatedAsync` call at `:79`
      inside `GetRemoteAsync`, **+ REQ-UOW-52** remote guard
  - Files owned: `MyVocaList/Services/ArtistSuggestionService.cs`,
    `MyVocaList.Tests/Unit/Services/ArtistSuggestionServiceTests.cs` (**`CreateSut` helper `:22` ONLY**
    — carve-out row 2), `MyVocaList.Tests/Unit/Services/ArtistSuggestionThresholdTests.cs` (new)
  - Est: **2 h** · Risk: **A** · AC: REQ-UOW-38, -41, -43, **-52**
  - Note: its local guard is already `trimmed.Length < 2` — REQ-UOW-51 requires **no change** here
    beyond swapping the literal for `SearchConstants.MinimumLocalQueryLength`.
- [ ] **5.2** `SongSuggestionService.cs` — `IUnitOfWork` injection (**3rd of 6** ctor params, after
      `artistRepository`), wrap `GetLocalAsync:40`, `DedupAsync:113`'s `GetByTitlesCollatedAsync` and
      `ResolveLocalArtistIdsAsync:153`'s `GetByNamesCollatedAsync`, **+ REQ-UOW-51/52 thresholds**
  - Files owned: `MyVocaList/Services/SongSuggestionService.cs`,
    `MyVocaList.Tests/Unit/Services/SongSuggestionServiceTests.cs` (**`CreateSut` helper `:20` ONLY**
    — carve-out row 1), `MyVocaList.Tests/Unit/Services/SongSuggestionThresholdTests.cs` (new)
  - Est: **2 h** · Risk: **A** · AC: REQ-UOW-38, -41, -43, **-51**, **-52**
  - Note: `GetLocalAsync` gains a normalize/`Trim` ahead of its guard (`IsNullOrWhiteSpace` →
    `Length < MinimumLocalQueryLength` measured on the trimmed term) — the **only** behavioural
    addition REQ-UOW-51 permits.

  For both 5.x tasks —
  - Consumes: Wave 0's constants; Wave 4 complete
  - Demo: existing suite green apart from the single permitted `CreateSut` edit; **new-file** threshold
    tests — 1-char ⇒ empty result **and** repository `Times.Never`; 2-char ⇒ reaches the repository;
    2-char ⇒ provider fake records **zero** invocations; 3-char ⇒ reaches the provider; plus
    REQ-UOW-43's blocking-fake test asserting zero open scopes during the provider fetch
  - Review lane: full
  - **Guards:**
    - `MauiProgram.cs` is **NOT** edited. DI registration belongs to the autocomplete feature (**D3**).
      These services are pre-built, not dead code — never "clean them up".
    - The new threshold tests go in **new files**. The two pre-existing suites stay
      `CreateSut`-edit-only: no `[Fact]` body, no `Assert`, no `Setup`/`Verify`, no field may change
      (REQ-UOW-49). Anything else there is a violation, not a judgement call.
    - The two services are disjoint files, so 5.1 and 5.2 parallelise.

## Wave 6 — Concurrency probe

- [ ] **6.1** Concurrency test — two paged-list loads across two services, overlap forced
  - Produces: the REQ-UOW-42 test
  - Consumes: Waves 4–5
  - Risk: **A** · Est: **2 h**
  - Files owned: `MyVocaList.Tests/Integration/UnitOfWork/PagedListConcurrencyTests.cs` (new)
  - Demo: written and green **with the gate still present** in this wave; overlap forced by a
    `TaskCompletionSource` awaited inside each lambda body (**not** `Task.WhenAll`); asserts (i) two
    distinct `AppDbContext` instances via REQ-UOW-39's observation mechanism and (ii) no
    `InvalidOperationException` mentioning "A second operation was started on this context"
  - Review lane: full
  - AC: REQ-UOW-42 (authoring limb)
  - **Note:** REQ-UOW-42's mandated condition is "**with `DbLoadGate` removed**" — that run happens in
    Wave 8, which co-owns this AC. This wave writes the test; Wave 8 re-runs it gate-free.

## Wave 7 — Enforcement `[after Wave 5]`

- [ ] **7.1** xUnit architecture test enforcing REQ-UOW-36/37/43 permanently
  - Produces: the permanent gate (highest-value review artifact — R1)
  - Consumes: Wave 5 (fails against unconverted suggestion services)
  - Risk: **A** · Est: **2 h**
  - Files owned: `MyVocaList.Tests/Architecture/UnitOfWorkReadScopeTests.cs` (new) + the shared
    test-infrastructure file **only if** `LocateSource` is extracted rather than called from
    `UnitOfWorkCompositionTests`
  - Demo: built per REQ-UOW-50 (i)–(iv) — enumerated governed-field set, `Assert.NotEmpty` + count
    floor, comment/literal stripping with `_field.` anchoring, `LocateSource` path resolution;
    **seen to FAIL** against a scratch `_field` reintroduction, reverted, landed green — **both**
    outputs pasted
  - Review lane: full
  - AC: REQ-UOW-50
  - **Guard:** the allow-list must be empty, or every entry explicitly commented. Never loosen the
    assertion to make it pass.

- [ ] **7.2** **Census-wide file walk — limb (a) evidence** (this is the artifact Wave 8 depends on)
  - Produces: the REQ-UOW-36/37 source-level assertion over **all** of `Services/*.cs`, pasted into
    `task-log.md`. Wave 4/5 walks are per-file; this is the tree-wide one, and without it Wave 8's
    "limb (a) evidence" precondition has no producer.
  - Consumes: Waves 4–5 committed
  - Risk: **A** · Est: **1 h**
  - Files owned: `.claude/scripts/` walk script (new, or a scratch script referenced in the log) —
    **no production or test file is edited by this task**
  - Demo: for every method named in `requirements.md § Scope`, the walk shows its repository call lies
    textually inside an `ExecuteReadAsync(` lambda body; and **zero** `ExecuteReadAsync` lambda bodies
    anywhere contain a `_`-prefixed repository/data-service field. Python walk only — a `grep` result
    is **not admissible** (R2). Full output pasted.
  - Review lane: full
  - AC: **REQ-UOW-36, REQ-UOW-37**
  - **Guard:** a non-empty result ⇒ Wave 8 does **not** start. Fix the offending service first.

## Wave 8 — `DbLoadGate` removal `[STRICTLY LAST]`

- [ ] **8.1** Remove `DbLoadGate`, both REQ-UOW-29 limbs evidenced
  - Produces: Phase 4.7 of the parent spec closed
  - Consumes: **all** prior waves, and specifically **7.2's clean census walk**
  - Risk: **A** · Est: **2 h**
  - Files owned: `MyVocaList/UI/ViewModels/CrudListViewModelBase.cs`,
    `MyVocaList.Tests/Unit/ViewModels/CrudListViewModelBaseTests.cs` (**comment only** — the `:215`
    `DbLoadGate` reference; REQ-UOW-49 carve-out row 4)
  - Demo: both limbs per `design.md § 6`; tree-wide Python walk showing zero `DbLoadGate`; all three
    stale-comment rewrites accounted for (§ 6 step 4 — two landed in Wave 3, the third here); the
    **two** `Assert.NotSame` off-context assertions (`CrudListViewModelBaseTests.cs:37`, `:75`) green;
    source-level check that `Task.Run` survives in `LoadFirstPageAsync` (`:141`) and `LoadMoreAsync`
    (`:216`); **re-run Wave 6's concurrency test with the gate deleted** (`design.md § 6` step 6) and
    paste that output; **`git diff --stat` over `MyVocaList.Tests/`** plus a reviewed diff of the four
    carve-out files showing no `Assert`/`Verify`/`Setup` change (REQ-UOW-49 evidence)
  - Review lane: full
  - AC: REQ-UOW-47, REQ-UOW-48, **REQ-UOW-42** (gate-removed run), **REQ-UOW-49** (final evidence)
  - **Guards:**
    - The `Task.Run` offloads share a comment block with the gate — deleting them regresses
      `page-load-frozen` (R3). They must survive.
    - The `:254` and `:306` `Task.Run` calls are **out of scope — do not touch**.
    - If any read is still unscoped, gate removal is unsafe (R4). 7.2's evidence gates this task.

---

## Out of scope — do not do these

- **`MauiProgram.cs`** — appears in no wave. Sequential-only file; suggestion-service DI registration
  belongs to the `ArtistFormPage`/`SongFormPage` autocomplete feature.
- **Input debounce** — a UI-timing concern of that same future feature (REQ-UOW-52 note).
- **Transactions or an ambient scope on the read path** (R8; `design.md § 2b`).
- **Event/Queue code** — deleted in `c7ad5bd4`; `QueueService` no longer exists.
- **Any edit to a pre-existing file under `MyVocaList.Tests/`** beyond the closed four-row REQ-UOW-49
  carve-out.
