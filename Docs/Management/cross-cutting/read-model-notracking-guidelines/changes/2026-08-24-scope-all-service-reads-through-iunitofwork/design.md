# Design — Scope all service reads through `IUnitOfWork`

> Companion to `requirements.md` (REQ-UOW-36 … REQ-UOW-50).
> Parent: `../2026-08-03-dbcontext-lifetime-unit-of-work-pattern-maui-has-no-per-page-scope/design.md`.
> Defect: `../../bugs/2026-08-24-BUG-078-.../README.md`.

---

## 1. Decision summary (already made by Helder — not re-opened here)

| # | Decision |
|---|---|
| **D1** | Every service read is scoped through `IUnitOfWork.ExecuteReadAsync`. The whole app follows the pattern; there is no per-method opt-out. |
| **D2** | The rationale is **`DbContext` lifetime** — not tracking, not transactions. See § 2. |
| **D3** | HTTP stays **outside** the unit of work. The two suggestion services gain an `IUnitOfWork`, but only their repository segments go inside a lambda. They are **pre-built for the imminent autocomplete feature, not dead code**; they are converted now and **not** registered in DI by this change (`requirements.md § The two suggestion services are pre-built, not dead code`). |
| **D4** | Paged list reads are **mandatory** scope, not optional — they are exactly what `DbLoadGate` serialises. |
| **D5** | `DbLoadGate` removal requires **both** limbs of REQ-UOW-29, with limb (b) actually executed, and the `LoadFirstPageAsync` / `LoadMoreAsync` `Task.Run` offloads survive. |
| **D6** | The BUG-078 **Red is captured before `.AsTracking()` is removed**. This inverts the usual Infra-before-Services order deliberately; without it the mandatory Red is unobtainable. See § 7's warning. |
| **D7** | The REQ-UOW-36/37/43 rules are made permanent by an xUnit **architecture test** (REQ-UOW-50), not by task-log file-walk evidence alone. |

---

## 2. Why this is a lifetime fix — the correction to two intuitive-but-wrong readings

This section exists because both wrong readings are plausible enough that an implementor will reach for
them unprompted.

### 2a. It is **not** about tracking

`QueryTrackingBehavior.NoTracking` is set globally in three places — `Infra/AppDbContext.cs:37`,
`Infra/AppDbContext.cs:54`, and `MauiProgram.cs:72`. Query results are therefore already untracked
regardless of which context runs the query. **Wrapping a read in `ExecuteReadAsync` buys exactly zero
on tracking.** Any AC or code comment that justifies a wrap by "so the read does not track" is wrong
and should be rejected in review.

The one tracking-shaped item in this change (`ArtistRepository.cs:80`'s `.AsTracking()`, § 5) is a
*consequence* of the lifetime defect, not its cause: `.AsTracking()` is only harmful because the
context it attaches to lives forever.

### 2b. It is **not** about transactions

`IUnitOfWork.ExecuteReadAsync` **opens no transaction** and **never publishes an ambient scope** —
REQ-UOW-34, ratified in the parent's Revision 12. Only `ExecuteAsync` assigns `_ambientScope`. So
"transaction safety" cannot be the justification for any requirement in this change, and an implementor
must not add a transaction to the read path to "make it consistent".

### 2c. What the wrap actually buys

Two things, both lifetime-derived:

1. **A fresh scoped `AppDbContext` per read**, instead of the app-lifetime captive one. `DbContext` is
   not thread-safe for concurrent operations; giving each read its own instance is what makes
   concurrent CRUD list loads legal, and it is therefore **the precondition for removing `DbLoadGate`**
   (REQ-UOW-42, REQ-UOW-47).
2. **Read-your-own-writes.** Because a read nested inside a write **joins the write's ambient scope**
   (REQ-UOW-34, direction 1), it sees the write's uncommitted state and the same context. And because a
   top-level read gets a *fresh* context, it sees the latest committed state rather than a stale cached
   instance. That second half is the BUG-078 staleness class.

---

## 3. Canonical wrap shape

The reference implementation is `Services/ArtistResolutionService.cs:36` — copy it exactly. The
load-bearing rule is REQ-UOW-28/37: **every collaborator is resolved from the lambda's own `sp`, never
from a constructor `_field`.**

```csharp
/// <inheritdoc />
// [AC] REQ-UOW-39: read scoped through IUnitOfWork — fresh AppDbContext per call (lifetime, not tracking).
public Task<Person> GetPersonByIdAsync(int id, CancellationToken ct = default)
    => _uow.ExecuteReadAsync<Person>(async sp =>
    {
        // REQ-UOW-37: resolved from the lambda's own scope — never the constructor field.
        var personRepository = sp.GetRequiredService<IPersonRepository>();
        return await personRepository.GetByIdAsync(id, ct);
    }, ct);
```

Rules that fall out of the shape:

- **`_uow`, `_logger`, `_scorer` and other non-data collaborators may still be referenced from the
  enclosing instance** inside the lambda — the prohibition is on repository- and data-writing-service
  fields, whose identity determines which `AppDbContext` is used. `ArtistResolutionService.cs` does
  exactly this with `_logger`.
- **Validation and short-circuits stay outside the lambda** (REQ-UOW-41), so no scope is created for a
  call that never touches the database and exception types/messages are unchanged:

```csharp
public async Task<IEnumerable<ArtistListItem>> SearchArtistsByNameAsync(
    string query, int maxResults = 5, CancellationToken ct = default)
{
    var normalized = query.NormalizeSearchQuery();
    if (string.IsNullOrWhiteSpace(normalized))
        return [];                                   // outside — no scope created

    return await _uow.ExecuteReadAsync<IEnumerable<ArtistListItem>>(async sp =>
    {
        var artistRepository = sp.GetRequiredService<IArtistRepository>();
        return await artistRepository.SearchByNameAsync(normalized, maxResults, ct);
    }, ct);
}
```

- **A read lambda SHALL return a materialized result; no deferred query may escape it.** The lambda's
  `AppDbContext` is disposed when `ExecuteReadAsync` returns, so a lazy `IEnumerable` enumerated by the
  caller afterwards would enumerate against a disposed context. Several wrapped methods return
  `.Select(...)` projections that are lazy as written — `PersonService.cs:215`, `VenueService.cs:200`,
  `SongKaraokeUrlService.cs:42` (line numbers verified 2026-08-25). Those are safe **today** only
  because the repository materializes first (e.g. `PersonRepository.GetPagedAsync` ends in
  `.ToListAsync(...)` at `:86-90`), so the `.Select` runs over an in-memory list. That is an accident of
  the current repository implementations, not an invariant, and it stops being true the moment a
  repository returns an `IQueryable`. **Materialize inside the lambda** (`.ToList()` / `.ToListAsync()`
  before `return`) rather than relying on it. Declared return types stay unchanged (REQ-UOW-49) — an
  `IEnumerable<T>` return may hold a `List<T>`.

- **One lambda per contiguous repository segment.** A **contiguous repository segment** is a run of
  repository calls with **no intervening network, file, or other long-running I/O** — no
  `IMusicMetadataProvider` fetch, no `File`/`ZipFile` access, no `HttpClient` call. In-memory work
  (projection, filtering, scoring, mapping) does not break contiguity; long-running I/O does, and each
  side of it becomes its own segment with its own lambda. This definition is what decides lambda
  boundaries throughout Waves 4–5, and it is why `GetRemoteAsync` in the suggestion services cannot be
  one lambda (§ 4a) while `GetLocalAsync`'s two calls can be (§ 4b).
  `ArtistSuggestionService.GetLocalAsync`'s two calls (`SearchByNameAsync` + `GetByNameAsync`) share one
  lambda so both observe the same context — see § 4b.
- **Ceremony budget (REQ-UOW-10) is preserved**: one expression-bodied wrap per service method, zero
  lines per repository method. No repository signature gains an `AppDbContext` parameter.

---

## 4. The HTTP-outside-scope shape (D3)

`SongSuggestionService` and `ArtistSuggestionService` have no `IUnitOfWork` today. They gain one by
constructor injection (REQ-UOW-38). The structural rule: **a lambda never encloses a provider call.**

`ArtistSuggestionService.GetRemoteAsync` — HTTP first, then a *separate* short-lived read scope:

```csharp
public async Task<IReadOnlyList<ArtistSuggestionDto>> GetRemoteAsync(
    string term, IReadOnlyList<ArtistSuggestionDto> localResults, CancellationToken ct = default)
{
    var normalizedTerm = term.NormalizeSearchQuery();

    // OUTSIDE the unit of work — REQ-UOW-43. Never hold a DbContext scope across network I/O.
    var fetched = await FetchFromProvidersAsync(normalizedTerm, ct);
    if (fetched.Count == 0) return [];

    var afterExternalId = fetched.Where(r => !HasExternalIdMatch(r, localResults)).ToList();
    if (afterExternalId.Count == 0) return [];

    var candidateNames = /* … in-memory projection … */;

    // The ONLY database segment — its own scope, opened after the network call has completed.
    IReadOnlyList<Artist> collatedMatches = candidateNames.Count > 0
        ? await _uow.ExecuteReadAsync<IReadOnlyList<Artist>>(async sp =>
            await sp.GetRequiredService<IArtistRepository>()
                    .GetByNamesCollatedAsync(candidateNames, ct) ?? [], ct)
        : [];

    // … tiers (b) and (c), pure in-memory — outside any scope …
}
```

`SongSuggestionService` follows the same rule, but its database segments live in the two **private
helpers** reached after the fetch. Each helper wraps its own repository call:

- `DedupAsync:113` → `GetByTitlesCollatedAsync` inside its own `ExecuteReadAsync`;
- `ResolveLocalArtistIdsAsync:153` → `GetByNamesCollatedAsync` inside its own `ExecuteReadAsync`;
- `FetchFromProvidersAsync:78` → untouched, and no lambda may enclose a call to it.

### 4a. Two short-lived scopes per `SongSuggestionService.GetRemoteAsync` — a decision, not an open question

> **This subsection is about `SongSuggestionService`, not `ArtistSuggestionService`** — it follows the
> `ArtistSuggestionService` code block above only because that block illustrates the general
> HTTP-outside-scope shape. The two scopes described here are `SongSuggestionService`'s two private
> helpers, listed immediately above this subsection.

A single `SongSuggestionService.GetRemoteAsync` call therefore opens **two** short-lived read scopes
rather than one: `DedupAsync` wraps its own `GetByTitlesCollatedAsync`, and `ResolveLocalArtistIdsAsync`
wraps its own `GetByNamesCollatedAsync`. The alternative — one lambda around `GetRemoteAsync` as a
whole — is **prohibited**, because that lambda would enclose `FetchFromProvidersAsync` and pin a
`DbContext` across a network round-trip (REQ-UOW-43). The shape is therefore forced by D3, not chosen.

**`ArtistSuggestionService.GetRemoteAsync` opens exactly ONE scope**, not two: it makes a single
repository call — `GetByNamesCollatedAsync` at `ArtistSuggestionService.cs:79` (verified 2026-08-25) —
and that call is the sole database segment shown in the code block above. Do not carry this
subsection's "two scopes" over to it.

**Decision: accept the two scopes.** Two cheap SQLite context creations in exchange for never pinning
a context across network I/O is the correct trade, and it is not a performance concern at
suggestion-lookup call rates (R7). No AC depends on the *number* of scopes; an implementor must not
"optimise" it back to one.

### 4b. One shared lambda for `ArtistSuggestionService.GetLocalAsync` — also a decision

`GetLocalAsync` makes two repository calls (`SearchByNameAsync` + `GetByNameAsync`) with no
intervening network or file I/O. They are a **contiguous repository segment**, so per § 3 they go in
**one** lambda and both observe the same `AppDbContext`. Splitting them into two lambdas would create
a second scope for no benefit and would let the two calls observe different committed states.

No AC depends on this, but it is the shape the design specifies and reviewers should enforce it.

**Same principle, non-network case:** `BackupService.ExportBundleAsync:86` wraps only
`GetLatestSnapshotAsync`; the `File.Exists` check, `ZipFile.Open` and the entry copies stay outside
(REQ-UOW-44). Zip creation over a multi-megabyte snapshot is long-running I/O and must not pin a scope.

**Enforcement.** REQ-UOW-43's file-walk check (no `_providers` / `IMusicMetadataProvider` /
`FetchFromProviders` identifier inside any `ExecuteReadAsync` body) is mechanical. Run it with a Python
walk, not `grep` — the Bash grep proxy in this environment is lossy and has produced false zeroes, so a
clean `grep` result is not admissible evidence for an exhaustive claim.

---

## 5. The BUG-078 defect site and the `.AsTracking()` decision

### 5a. The fix

`ArtistService.GetDeleteConfirmationAsync:172` reads `_artistRepository.GetByIdAsync(idList[0], ct)`
through the captive field. Wrapped per § 3, it becomes a fresh-context read, and the staleness
disappears: the context that serves the confirmation is created *after* the rename committed, so it has
no cached copy to prefer over the database.

The `idList.Count == 1` branch guard stays outside the lambda (REQ-UOW-41) — the multi-artist path
makes no database call at all and must not create a scope.

### 5b. Decision on `.AsTracking()` (REQ-UOW-46): **remove it.**

`Infra/Repository/ArtistRepository.cs:80` is the only `.AsTracking()` in `Infra/Repository/`
(Python-walk verified, count = 1).

**The honest position on both options:**

- *Keeping it is genuinely harmless once the context is scoped.* The whole harm of `.AsTracking()` here
  was that the tracked entity attached to an app-lifetime context: unbounded tracker growth, and a
  cached instance preferred over the database on the next read. A scope that is disposed at the end of
  `ExecuteReadAsync` takes its change tracker with it. So "leave it, the lifetime fix already defuses
  it" is a defensible answer, and choosing it would not reintroduce BUG-078.

- *Removing it is still the better answer*, for three reasons that survive the lifetime fix:
  1. **It contradicts the project's stated policy.** `NoTracking` is the global default set in three
     places; a lone `.AsTracking()` is an unexplained exception that every future reader must
     re-adjudicate. The read-model guidelines this whole feature folder exists to establish say reads
     do not track.
  2. **It is load-bearing for nothing.** `GetByIdAsync` has write-path callers, but writes in this
     codebase mutate through `ExecuteAsync` lambdas that resolve the repository from `sp` and then call
     `UpdateAsync`/`DeleteAsync`, which set entity state explicitly. Tracking on the *read* is not what
     makes those writes work.
  3. **It re-arms silently under the one condition this design permits.** A read nested inside a write
     **joins the write's ambient scope** (REQ-UOW-34). In that case the tracked `Artist` attaches to
     the *write's* context — exactly the BUG-068 "another instance with the same key is already being
     tracked" shape if that write later calls `Update` on the same id. The lifetime fix does not cover
     this path, because in it the context is deliberately shared.

  Reason 3 is the decisive one: keeping `.AsTracking()` leaves a live failure mode, just a narrower one.

**Guard on the removal:** REQ-UOW-46 requires the existing artist update/delete tests **and**
`MyVocaList.Tests/Integration/UnitOfWork/Bug068RegressionTests.cs`'s
`Artist_CreateThenReadThenUpdate_DoesNotThrowTrackingConflict` (`[Fact]` at `:45`) to stay green
**unmodified**. If any turns red, that is evidence a write path *was* relying on the read's tracking —
stop and log `blocked: spec gap` rather than restoring `.AsTracking()` or editing the test
(`testing.md § Builder Must Not Modify Tests`).

That last test deserves naming because it is the one place in the suite that **documents** the
dependence being removed. Its comment at `:41-44` reads, in part: *"this family never reproduced
BUG-068 — `ArtistRepository.GetByIdAsync` (`ArtistRepository.cs:79-80`) explicitly calls
`.AsTracking()`, so EF identity resolution returns the already-tracked instance instead of a fresh
detached one."* Once `.AsTracking()` is gone that sentence describes code that no longer exists.
REQ-UOW-46 therefore requires the comment to be **rewritten in the same commit as the removal** — the
test should still pass (reason 2 above: the write path sets entity state explicitly, it does not rely
on the read's tracking), but its *explanation* of why must change from "identity resolution returns the
tracked instance" to "the write path sets entity state explicitly, so no tracking conflict arises". A
green test carrying a false comment is a trap for the next reader, not a pass.

**Ordering constraint:** this removal is **not** the first step of the work. See § 7 — the BUG-078 Red
must be captured while `.AsTracking()` is still in place.

### 5c. The regression test

Per `.claude/rules/bug-tracking.md`, Major severity ⇒ a regression test is mandatory and must be seen
to **fail before, pass after**. It is an integration test against a real SQLite temp-file database
(never the in-memory provider — `testing.md § Project anti-patterns`), asserting REQ-UOW-45's
Given/When/Then: prime the read, rename through `UpdateArtistAsync`, re-read, expect
`"Delete 'New Name'?"`.

The priming call is essential — without it the captive context has nothing cached and the test passes
against the buggy code, which would make it a non-test.

**The harness is `UnitOfWorkTestHost`, and priming only works there.** `UnitOfWorkTestHost` composes
the real DI graph over a SQLite temp file behind a **single long-lived scope**
(`UnitOfWorkTestHost.cs:22`: `public IServiceProvider Services => Scope.ServiceProvider;`) — that scope
*is* the captive context this change exists to eliminate. A test built on a per-call
`TestDbContextFactory` hands every operation a fresh context, so the priming call caches nothing the
re-read can see, and the Red is unobtainable no matter what `ArtistService` does. REQ-UOW-45 therefore
requires `IArtistService` to be resolved from **one** `UnitOfWorkTestHost` that is reused across
prime → rename → re-read; a second host or scope between the steps silently destroys the test.

**And `.AsTracking()` must still be present when the Red is captured.** The priming call only caches an
`Artist` because `ArtistRepository.cs:80` tracks it. Remove `.AsTracking()` first and the priming read
caches nothing, so the second read hits the database, returns `"Delete 'New Name'?"`, and the test
passes **against unfixed `ArtistService` code** — a green test that proves nothing. `.claude/rules/
bug-tracking.md` requires a Major-severity fix to have a test seen to FAIL before and PASS after; that
Red is obtainable only in the window before REQ-UOW-46 lands. § 7 encodes the ordering.

---

## 6. `DbLoadGate` removal sequence (Phase 4.7)

`DbLoadGate` lives at `MyVocaList/UI/ViewModels/CrudListViewModelBase.cs`: declaration `:16`, waits
`:128` and `:207`, releases `:179` and `:241`, plus a reference in the `:304` comment. Its comment block
(`:12–15`) carries **two independent rationales with two independent revert triggers** (REQ-UOW-29).

**Sequence — strictly ordered:**

1. **Convert every read** (REQ-UOW-36 … 45), commit, full test suite green. Nothing about the gate is
   touched yet.
2. **Evidence limb (a).** Run the REQ-UOW-36/37/43 Python file-walk checks; paste output into
   `task-log.md`. Limb (a) is "every in-scope consumer converted" — the census table in
   `requirements.md § Scope` is the closure definition.
3. **Baseline limb (b) *before* deletion.** Run `dotnet test --filter CrudListViewModelBaseTests` with
   the gate still present; record the pass output. This is the comparison baseline.
4. **Delete the gate**: the field at `:16`, the two `WaitAsync` calls, the two `Release` calls, and the
   `entered` bookkeeping they drive. Edit the comment block so the surviving `SQLITE-WORKAROUND`
   rationale (sentence 3–4) still reads as a complete justification for the `Task.Run` offloads once
   the MAUI-no-per-page-scope sentence is gone. Then clear **all three** surviving references to the
   deleted symbol — the tree-wide zero-`DbLoadGate` walk in step 5 checks for exactly these:
   - the comment block at `:12-15` (above);
   - `CrudListViewModelBase.cs:304`, "Do NOT hold `DbLoadGate` here" — a reference to a thing that no
     longer exists. The **comment** changes; the `await Task.Run(action)` at `:306` stays (§ 6a);
   - **`MyVocaList.Tests/Unit/ViewModels/CrudListViewModelBaseTests.cs:215`** — inside
     `PumpSynchronizationContext.Post`'s `catch (InvalidOperationException)`, the comment justifies the
     `ThreadPool.QueueUserWorkItem` fallback at `:217` with *"a swallowed post would strand the static
     `DbLoadGate` and hang every later test"*. Rewrite it to justify the fallback on its surviving
     ground — a swallowed post strands the SUT's `finally` blocks and hangs later tests. **Comment text
     only**: no assertion, no `[Fact]`, no setup or teardown change, and the fallback itself stays. This
     is the fourth row of REQ-UOW-49's carve-out and is committed in **this** (Wave 8) commit; without
     it REQ-UOW-46's "a review failure, not a nit" standard and REQ-UOW-49's closed carve-out would
     contradict each other.
   Finally, still in step 4, **run the tree-wide zero-`DbLoadGate` walk** (REQ-UOW-47, parent
   REQ-UOW-29's source-level limb): a Python file walk over the whole repository asserting the
   identifier `DbLoadGate` occurs **zero** times. Unlike the REQ-UOW-36/37/43 walks, this one
   deliberately does **not** strip comments or string literals — catching stale comments is its whole
   purpose, and it is what mechanically proves all three rewrites above were done. Paste the output
   into `task-log.md`.

5. **Evidence limb (b) *after* deletion.** Re-run `dotnet test --filter CrudListViewModelBaseTests`;
   paste the output. **This step must actually be run.** The file holds **five** `[Fact]` methods
   (`:10`, `:40`, `:78`, `:103`, `:120`), of which exactly **two** carry
   `Assert.NotSame(uiContext, contextDuringFetch)` — at `:37` and `:75`. Those two assert `Task.Run`
   off-context behaviour and not the semaphore. **Limb (b) requires all five green**, not just the two:
   the other three (shimmer-toggle and search-debounce) run through the same `LoadFirstPageAsync` /
   `LoadMoreAsync` paths the gate is cut out of. All five are *expected* to stay green — but REQ-UOW-47
   requires verification, not expectation.
6. **Run the REQ-UOW-42 concurrency test** with the gate absent: two paged-list loads across two
   services concurrently, asserting two distinct `AppDbContext` instances and no "A second operation
   was started on this context" exception. This is the test that would actually catch a missed read.
7. **Full `dotnet test`** green, then commit.

### 6a. What must NOT be deleted (REQ-UOW-48)

The `Task.Run(...)` offloads at **`:141` (in `LoadFirstPageAsync`)** and **`:216` (in `LoadMoreAsync`)**
are a **different** mitigation — for `Microsoft.Data.Sqlite` completing async methods synchronously on
the calling thread (the `page-load-frozen` bug class) — whose revert trigger is `INFRA_MSSQL`
(replacing SQLite), not "reads are now scoped". They **survive** this change.

**`CrudListViewModelBase.cs` has FOUR `Task.Run` calls, not two.** The other two are unrelated to both
the gate and this AC and must simply be left alone: the fire-and-forget `_ = Task.Run(...)` at `:254`,
and `await Task.Run(action)` at `:306` in `ExecuteConfirmActionAsync`. Note `:306`'s neighbouring
comment at `:304` says "Do NOT hold `DbLoadGate` here" — that **comment** must be updated by step 4
above because it names a deleted symbol, but the `Task.Run` itself stays.

The trap is that the gate's rationale and the SQLITE-WORKAROUND rationale live in one comment block, so
an implementor deleting "the comment" may take a `Task.Run` with it. The two
`Assert.NotSame(uiContext, contextDuringFetch)` assertions (`CrudListViewModelBaseTests.cs:37` and
`:75`) are the mechanical guard: they fail if the `:141` or `:216` offload is removed. REQ-UOW-48
additionally requires a source-level assertion that `Task.Run` still appears in both
`LoadFirstPageAsync` and `LoadMoreAsync` — named by method, not by count, since a raw count of four
would also be satisfied by deleting a protected one and adding another elsewhere.

---

## 7. Work ordering

> ## ⚠ Waves 1–3 are load-bearing and MUST NOT be reordered
>
> The obvious DRY-Onion instinct — *Infra before Services*, so remove `.AsTracking()` first — **destroys
> the mandatory BUG-078 Red** and must be resisted. This ordering is deliberate; do not "optimise" it.
>
> **Why.** BUG-078's staleness exists *only* because `Infra/Repository/ArtistRepository.cs:80` calls
> `.AsTracking()`: the priming read caches a tracked `Artist` on the app-lifetime captive context, and
> the post-rename read is served that stale copy. Remove `.AsTracking()` first and the priming read
> caches nothing — the regression test then passes **against unfixed `ArtistService` code**. A test that
> has never failed proves nothing, and `.claude/rules/bug-tracking.md` requires a **Major**-severity fix
> to have a regression test **seen to FAIL before and PASS after**. The Red window is exactly the period
> before Wave 3.
>
> Consequently: **Wave 1 (Red) → Wave 2 (fix, Green) → Wave 3 (remove `.AsTracking()`, re-run).**
> `.AsTracking()` removal is Infra work sequenced *after* a Services change, which is a **deliberate
> exception** to the DRY-Onion order in `workflow.md` Rule 4 — it is safe because nothing in Waves 1–2
> consumes a new Infra type; Wave 3 only deletes a call. Any reordering that puts `.AsTracking()`
> removal before Wave 1 invalidates REQ-UOW-45's evidence and the task-log entry is rejected.

Waves 4 onward follow the ordinary Domain → Infra → Services → UI discipline of `workflow.md` Rule 4.

| Wave | Work | Files owned |
|---|---|---|
| **1** | **REQ-UOW-45 Red.** Write the BUG-078 regression test **with `.AsTracking()` still present and `ArtistService` unchanged**. Run it; it MUST fail, returning `"Delete 'Old Name'?"`. Paste the failing output into `task-log.md`. **No production file is edited in this wave.** | new integration test file only |
| **2** | **REQ-UOW-45 Green.** Wrap `ArtistService.GetDeleteConfirmationAsync:172`'s `GetByIdAsync` call in `ExecuteReadAsync` per § 3/§ 5a. Re-run the Wave 1 test; it MUST now pass. Paste the passing output. `.AsTracking()` is **still present** at this point — this is the honest Red→Green pair. | `Services/ArtistService.cs` (`GetDeleteConfirmationAsync` only) |
| **3** | **REQ-UOW-46.** Remove `.AsTracking()` from `ArtistRepository.cs:80`. Re-run the BUG-078 test (still green) **and** the guard tests, including `Bug068RegressionTests.Artist_CreateThenReadThenUpdate_DoesNotThrowTrackingConflict`. Rewrite **both** stale comments **in this same commit** (§ 5b): that test's `:41-44` comment, **and** `Infra/AppDbContext.cs:36` ("Edit queries use explicit `.AsTracking()`…"), which after this wave describes code that no longer exists anywhere. `AppDbContext.cs` was previously owned by no wave, so this stale comment would have survived the change — REQ-UOW-46's file walk is scoped to `Infra/Repository/` and does not reach it. | `Infra/Repository/ArtistRepository.cs`, `Infra/AppDbContext.cs` (**comment only** — `:36`; `ChangeTracker.QueryTrackingBehavior` at `:37` is untouched), `MyVocaList.Tests/Integration/UnitOfWork/Bug068RegressionTests.cs` (comment only) |
| **4** (parallel) | REQ-UOW-39/40/41 wraps — one service per subagent | `PersonService.cs` · `SongService.cs` · `VenueService.cs` + `CatalogService.cs` · `SongKaraokeUrlService.cs` · `BackupService.cs` (incl. REQ-UOW-44) · rest of `ArtistService.cs` |
| **5** | REQ-UOW-38/43 — the two suggestion services. **No DI registration; `MauiProgram.cs` is NOT owned by this or any wave.** Includes the permitted `CreateSut` edits in both suggestion-service test suites (REQ-UOW-49 carve-out). | `Services/SongSuggestionService.cs`, `Services/ArtistSuggestionService.cs`, `MyVocaList.Tests/Unit/Services/SongSuggestionServiceTests.cs` (`CreateSut` only), `MyVocaList.Tests/Unit/Services/ArtistSuggestionServiceTests.cs` (`CreateSut` only) |
| **6** | REQ-UOW-42 concurrency test | new test file |
| **7** | REQ-UOW-50 architecture test — built per REQ-UOW-50 (i)–(iv): enumerated governed-field set, `Assert.NotEmpty` + count floor, comment/literal stripping with `_field.` anchoring, and `UnitOfWorkCompositionTests.LocateSource` for path resolution. Demonstrate it failing on a scratch `_field` reintroduction, revert, land green | new test file (plus, if `LocateSource` is extracted rather than called, the shared test-infrastructure file it moves to) |
| **8** | REQ-UOW-47/48 — gate removal, both limbs evidenced (§ 6), including the tree-wide zero-`DbLoadGate` walk and all three stale-comment rewrites (§ 6 step 4) | `MyVocaList/UI/ViewModels/CrudListViewModelBase.cs`, `MyVocaList.Tests/Unit/ViewModels/CrudListViewModelBaseTests.cs` (**comment only** — the `:215` `DbLoadGate` reference; REQ-UOW-49 carve-out row 4) |

**Serialisation notes.**
- Waves 1, 2 and 3 are **strictly sequential** — each depends on the previous wave's evidence, not just
  its code. Never collapse them into one task; the Red and the Green must be two separate recorded runs.
- Wave 4's services are disjoint files, so they parallelise (wave cap 4, `workflow.md` Rule 2).
- `Services/ArtistService.cs` is written in Wave 2 (`GetDeleteConfirmationAsync`) and again in Wave 4
  (`GetPagedArtistsForListAsync`, `SearchArtistsByNameAsync`). Those are sequential waves, so the
  single-writer rule holds automatically — but the Wave 4 subagent owning `ArtistService.cs` must not
  start before Wave 2 is committed.
- Wave 7's architecture test must land **after** Wave 5, or it will fail on the two
  not-yet-converted suggestion services.
- Wave 8 is strictly last — it is the only wave depending on *all* others.
- `MauiProgram.cs` appears in no wave (sequential-only file; DI registration is out of scope).

---

## 8. Risks

| # | Risk | Likelihood | Mitigation |
|---|---|---|---|
| R1 | An implementor references `_repository` inside a new lambda — the code compiles, tests pass, and the defect survives while *looking* fixed. | **High** — this is the pattern's classic failure and the parent spec's BL-G finding. | REQ-UOW-37's file-walk check + per-method review item at landing, **and REQ-UOW-50's xUnit architecture test as the permanent gate**. A file walk pasted into a task-log guards the landing commit only; the architecture test guards every service added afterwards. This is the single highest-value review gate in the change. |
| R2 | A `grep`-based exhaustiveness check returns a false zero and an incomplete conversion is declared complete — which would make `DbLoadGate` removal unsafe. | Medium | All census/enforcement claims use a Python file walk. A `grep`/`rg` result is not admissible evidence. |
| R3 | `Task.Run` offloads deleted together with `DbLoadGate` (shared comment block) → `page-load-frozen` regresses. | Medium | REQ-UOW-48 + the two `Assert.NotSame` assertions (`CrudListViewModelBaseTests.cs:37`, `:75`) + an explicit source-level check that `Task.Run` remains in `LoadFirstPageAsync` (`:141`) and `LoadMoreAsync` (`:216`). The file's other two `Task.Run` calls (`:254`, `:306`) are out of scope. Called out in § 6a. |
| R4 | The gate is removed while one read remains unscoped → concurrent operations on a shared `DbContext`, an intermittent crash that CI may not reproduce. | Medium | Ordering: gate removal is wave 8 (the last wave), gated on limb (a) evidence. REQ-UOW-42's concurrency test is the direct probe. |
| R5 | A network stall now pins a `DbContext` because a suggestion-service lambda swallowed the provider call. | Medium | REQ-UOW-43's two-part test — identifier walk *and* the blocking-fake behavioural test asserting zero open scopes during the fetch. |
| R6 | Removing `.AsTracking()` breaks a write path that was silently relying on it. | Low | REQ-UOW-46: existing artist update/delete tests must stay green **unmodified**; a red test ⇒ `blocked: spec gap`, not a restore and not a test edit. |
| R7 | Scope-creation overhead on the paged-list hot path (a scope per page load, two per remote suggestion). | Low | A DI scope + `AppDbContext` construction is cheap relative to a SQLite query; the parent spec already accepted this cost for every write. No mitigation beyond watching the existing page-load timings. |
| R8 | Someone "improves consistency" by adding a transaction or an ambient scope to the read path. | Low | § 2b states the prohibition; REQ-UOW-34 is unchanged and out of scope for this change. |
| R9 | Scope creep into Event/Queue code. | **Eliminated** | The feature was deleted in commit `c7ad5bd4`; `QueueService` no longer exists. The parent's D12 exclusion is recorded in `requirements.md § Explicitly out of scope` as history only. |

---

## 9. Conventions checked

- **Business logic stays in Services** (`CLAUDE.md § Constitutional Constraints`, unamendable) — this
  change moves no logic into ViewModels; the only UI-layer edit is deleting a synchronisation
  primitive from `CrudListViewModelBase.cs`.
- **Nullable reference types remain lenient/disabled** (`code-style-reference.md § Nullable Reference
  Types`). No `<Nullable>enable</Nullable>` is introduced and no null-forgiving ceremony is added.
- **Service return patterns unchanged** — tuple returns, no exceptions for business failures.
  `ExecuteReadAsync` never saves, so the parent's save-skip tuple inspection is not involved.
- **No native dialogs, no DevExpress or XAML surface touched.**
- **English only** in all code, comments and logs.
