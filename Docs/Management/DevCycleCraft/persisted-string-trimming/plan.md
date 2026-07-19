# Whitespace Normalization (BUG-046 + Persisted Trimming) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
> Spec: `requirements.md` (REQ-TRIM-01..10) + `design.md` (same folder). D1/D2 recorded 2026-07-15.

**Goal:** One static Services-layer helper (`StringNormalization`) wired into every search and Create/Update method of Person/Artist/Venue/Song (+ Event names), fixing BUG-046 and persisted-string trimming with zero UI/component changes.

**Architecture:** Static pure class `MyVocaList.Services.Text.StringNormalization` (no DI registration). Call sites exclusively inside Service methods (constitutional constraint). Search methods normalize the query before the min-length gate; Create/Update methods replace ad-hoc `Trim()` with `TrimForStorage`/`TrimForStorageOrNull`. D1 approved: storage collapses internal whitespace runs too.

**Tech Stack:** .NET MAUI 10 / C# 13, xUnit + Moq (`MyVocaList.Tests\Unit\Services\`), EF Core 10 repos mocked in service tests.

## Global Constraints

- Business logic in Services only — no ViewModel/page/component edits (REQ-TRIM-09; AutocompleteField is governed and UNTOUCHED).
- No case folding / diacritic removal in C# (REQ-TRIM-10; constraints-registry HARD RULE — collation owns that).
- BUG-046 is Major → regression test mandatory, Red before Green (`bug-tracking.md` HARD RULE).
- Every code task runs in a git worktree branched from `develop` (workflow Rule 2 HARD RULE); docs land on develop.
- AC traceability comment on every test: `// [AC] REQ-TRIM-NN: <criterion>`.
- English only; no regex in the helper (design.md implementation note).

---

### Task 1: StringNormalization helper + unit tests (TDD Level A)

**Files:**
- Create: `Services/Text/StringNormalization.cs`
- Test (create): `MyVocaList.Tests/Unit/Services/Text/StringNormalizationTests.cs`

**Interfaces:**
- Consumes: nothing (pure new code).
- Produces (Tasks 2–5 rely on these exact signatures, namespace `MyVocaList.Services.Text`):
  - `public static string NormalizeSearchQuery(string query)` — null/whitespace-only → `string.Empty`; else edge-trim + collapse internal whitespace runs to one space.
  - `public static string TrimForStorage(string value)` — null → null; else edge-trim + collapse internal runs (D1).
  - `public static string TrimForStorageOrNull(string value)` — as TrimForStorage, but empty/whitespace-only → null.

- [ ] **Step 1: Write the failing tests** (all branches — Level A):

```csharp
namespace MyVocaList.Tests.Unit.Services.Text;

using MyVocaList.Services.Text;

public class StringNormalizationTests
{
    // [AC] REQ-TRIM-08: NormalizeSearchQuery — null/whitespace-only → string.Empty
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("\t \n", "")]
    public void NormalizeSearchQuery_NullOrWhitespace_ReturnsEmpty(string input, string expected)
        => Assert.Equal(expected, StringNormalization.NormalizeSearchQuery(input));

    // [AC] REQ-TRIM-01: edge + internal whitespace normalize to single-spaced trimmed query
    [Theory]
    [InlineData("  jo", "jo")]
    [InlineData("jo ", "jo")]
    [InlineData("jo  hn", "jo hn")]
    [InlineData("  jo \t hn  ", "jo hn")]
    [InlineData("jo hn", "jo hn")]
    public void NormalizeSearchQuery_ExtraWhitespace_CollapsesAndTrims(string input, string expected)
        => Assert.Equal(expected, StringNormalization.NormalizeSearchQuery(input));

    // [AC] REQ-TRIM-08: TrimForStorage — null passes through as null
    [Fact]
    public void TrimForStorage_Null_ReturnsNull()
        => Assert.Null(StringNormalization.TrimForStorage(null));

    // [AC] REQ-TRIM-06: internal whitespace runs collapsed on storage (D1 approved)
    [Theory]
    [InlineData(" John  Doe ", "John Doe")]
    [InlineData("John Doe", "John Doe")]
    [InlineData("  ", "")]
    public void TrimForStorage_Whitespace_EdgeTrimsAndCollapses(string input, string expected)
        => Assert.Equal(expected, StringNormalization.TrimForStorage(input));

    // [AC] REQ-TRIM-07: optional fields — empty/whitespace-only persists as null
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TrimForStorageOrNull_NullOrWhitespace_ReturnsNull(string input)
        => Assert.Null(StringNormalization.TrimForStorageOrNull(input));

    // [AC] REQ-TRIM-07: optional fields with content are normalized like required ones
    [Theory]
    [InlineData(" a@b.c ", "a@b.c")]
    [InlineData("x  y", "x y")]
    public void TrimForStorageOrNull_WithContent_Normalizes(string input, string expected)
        => Assert.Equal(expected, StringNormalization.TrimForStorageOrNull(input));

    // [AC] REQ-TRIM-10: no case folding / diacritic changes — content preserved verbatim
    [Fact]
    public void Normalization_NeverAltersCaseOrDiacritics()
        => Assert.Equal("Ça VA", StringNormalization.NormalizeSearchQuery("  Ça  VA "));
}
```

- [ ] **Step 2: Run tests, verify they FAIL** — `dotnet test MyVocaList.Tests --filter StringNormalizationTests` → expected: compile error "StringNormalization does not exist" (that is the valid Red for a new type).

- [ ] **Step 3: Minimal implementation** — `Services/Text/StringNormalization.cs`:

```csharp
namespace MyVocaList.Services.Text;

/// <summary>
/// Whitespace-only normalization. Deliberately does NOT case-fold or strip diacritics —
/// that is owned by DB collation (constraints-registry § EF Core/SQLite HARD RULE) and must
/// never be reimplemented in C#. Do not conflate the two when extending this class.
/// </summary>
public static class StringNormalization
{
    /// <summary>Edge-trim + collapse internal whitespace runs to one space. Null/whitespace → "".</summary>
    public static string NormalizeSearchQuery(string query)
        => string.IsNullOrWhiteSpace(query) ? string.Empty : Collapse(query);

    /// <summary>Storage form of a required field. Null → null; else edge-trim + internal collapse (D1).</summary>
    public static string TrimForStorage(string value)
        => value is null ? null : Collapse(value);

    /// <summary>Storage form of an optional field. Empty/whitespace-only result → null.</summary>
    public static string TrimForStorageOrNull(string value)
    {
        var result = TrimForStorage(value);
        return string.IsNullOrEmpty(result) ? null : result;
    }

    private static string Collapse(string value)
        => string.Join(' ', value.Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
}
```

- [ ] **Step 4: Run tests, verify PASS** — same command, expected: all StringNormalizationTests green, 0 failures.
- [ ] **Step 5: Build full solution** (`/sln-build` — 0 errors) and commit on the task branch: `feat: add StringNormalization helper (REQ-TRIM-08) — Services/Text, Level-A tested`.

---

### Task 2: PersonService — BUG-046 regression (search) + storage trimming

**Files:**
- Modify: `Services/PersonService.cs` (SearchPersonsAsync, SearchPersonsStartsWithAsync, GetPagedPersonsForListAsync, CreatePersonAsync ~L143-152, UpdatePersonAsync ~L233-241)
- Test (modify): `MyVocaList.Tests/Unit/Services/PersonServiceTests.cs`

**Interfaces:**
- Consumes: `MyVocaList.Services.Text.StringNormalization` (Task 1 signatures above).
- Produces: no new public surface — existing `IPersonService` signatures unchanged.

- [ ] **Step 1: Write failing BUG-046 regression tests** (Major → mandatory Red first). Add to `PersonServiceTests.cs`, using the existing Moq repo setup pattern in that file:

```csharp
// [AC] REQ-TRIM-01: whitespace-polluted query produces the same repo call as the clean query
[Theory]
[InlineData("  jo")]
[InlineData("jo ")]
[InlineData(" jo  hn ")]
public async Task SearchPersonsAsync_ExtraWhitespace_ForwardsNormalizedTermToRepository(string dirty)
{
    var expected = StringNormalization.NormalizeSearchQuery(dirty);
    _personRepositoryMock
        .Setup(r => r.SearchByNameStartsWithAsync(expected, It.IsAny<int>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new List<Person>())
        .Verifiable();

    await _sut.SearchPersonsAsync(dirty);

    _personRepositoryMock.Verify(); // fails before fix: repo receives the raw dirty term
}

// [AC] REQ-TRIM-04: query that normalizes below min length returns empty WITHOUT hitting the repo
[Theory]
[InlineData(" a ")]
[InlineData("  ")]
[InlineData(null)]
public async Task SearchPersonsAsync_NormalizesBelowMinLength_ReturnsEmptyWithoutRepositoryCall(string dirty)
{
    var result = await _sut.SearchPersonsAsync(dirty);

    Assert.Empty(result);
    _personRepositoryMock.Verify(
        r => r.SearchByNameStartsWithAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
        Times.Never);
}
```

Mirror both tests for `SearchPersonsStartsWithAsync`. (Adapt mock method name/params to the actual `IPersonRepository` member the service calls — verify against the interface, do not guess.)

- [ ] **Step 2: Run, verify FAIL** — `dotnet test MyVocaList.Tests --filter PersonServiceTests` → the new tests fail (repo mock verified with normalized term never matches; raw term is forwarded today).
- [ ] **Step 3: Fix search methods** — at the top of `SearchPersonsAsync` and `SearchPersonsStartsWithAsync`:

```csharp
searchTerm = StringNormalization.NormalizeSearchQuery(searchTerm);
if (searchTerm.Length < MinSearchLength) // reuse the method's existing min-length constant/guard, now evaluated post-normalization
    return [];
```

  And in `GetPagedPersonsForListAsync`: `query = StringNormalization.NormalizeSearchQuery(query);` (empty → existing unfiltered behavior; REQ-TRIM-03).
- [ ] **Step 4: Write failing storage tests** (REQ-TRIM-05/06/07): assert the `Person` handed to the repo on Create/Update carries `"John Doe"` when given `" John  Doe "`, and that whitespace-only email/birthday persist as null.
- [ ] **Step 5: Fix storage sites** — replace `fullName.Trim()`/`email.Trim()` and the `string.IsNullOrWhiteSpace(x) ? null : x.Trim()` sites (Create ~L143-152, Update ~L233-241) with `StringNormalization.TrimForStorage(fullName)` / `StringNormalization.TrimForStorageOrNull(email)` / `TrimForStorageOrNull(birthday)`.
- [ ] **Step 6: Run full test suite** — `dotnet test` → all green (regression proved Red→Green). Build 0 errors.
- [ ] **Step 7: Commit** with the bug-fix template:

```
fix: PersonService search — extra whitespace in autocomplete query returned zero suggestions (BUG-046)

Root cause: SearchPersonsAsync/SearchPersonsStartsWithAsync forwarded the raw term to the repository with no normalization.
Fix: StringNormalization.NormalizeSearchQuery at method top (min-length gate now post-normalization); Create/Update use TrimForStorage/OrNull.
Regression risk: Low — normalization is additive; existing clean-query behavior unchanged (covered by suite).
```

---

### Task 3 [P]: ArtistService + suggestion services — search + storage normalization

**Files:**
- Modify: `Services/ArtistService.cs` (SearchArtistsByNameAsync ~L140, GetPagedArtistsForListAsync ~L130, CreateArtistAsync ~L52, UpdateArtistAsync ~L79)
- Modify: `Services/ArtistSuggestionService.cs` (GetLocalAsync/GetRemoteAsync term handling)
- Test (modify): `MyVocaList.Tests/Unit/Services/ArtistServiceTests.cs`

**Interfaces:**
- Consumes: `StringNormalization` (Task 1). Produces: none (signatures unchanged).

- [ ] **Step 1: Write failing tests** — same shape as Task 2 Step 1, against `SearchArtistsByNameAsync` and `GetPagedArtistsForListAsync`: `" ac  dc "` must reach the repo as `"ac dc"` (today `Trim()` yields `"ac  dc"` — internal run survives, test fails). Storage test: Create/Update with `" The  Band "` persists `"The Band"`; `externalId`/`externalProvider` via `TrimForStorageOrNull`.
- [ ] **Step 2: Run, verify FAIL** — `dotnet test MyVocaList.Tests --filter ArtistServiceTests`.
- [ ] **Step 3: Implement** — replace every `query.Trim()` / `query?.Trim()` in the four ArtistService methods with `StringNormalization.NormalizeSearchQuery(query)` (search/paged) and `TrimForStorage(name)` / `TrimForStorageOrNull(externalId)` (storage); in ArtistSuggestionService, normalize `term` once at the top of GetLocalAsync/GetRemoteAsync.
- [ ] **Step 4: Run tests → PASS; build 0 errors.**
- [ ] **Step 5: Commit** — `fix: ArtistService/ArtistSuggestionService — normalize search queries and stored names via StringNormalization (REQ-TRIM-01/03/05/06)`.

---

### Task 4 [P]: VenueService + EventService — search + storage normalization

**Files:**
- Modify: `Services/VenueService.cs` (CreateVenueAsync ~L60, UpdateVenueAsync ~L81, GetPagedVenuesForListAsync ~L166 — query currently passed RAW)
- Modify: `Services/EventService.cs` (CreateEventAsync / ValidateEventNameAsync name sites)
- Test (modify): `MyVocaList.Tests/Unit/Services/VenueServiceTests.cs`, `MyVocaList.Tests/Unit/Services/EventServiceTests.cs` (create either file if missing, following `ArtistServiceTests.cs` conventions)

**Interfaces:**
- Consumes: `StringNormalization` (Task 1). Produces: none (signatures unchanged).

- [ ] **Step 1: Write failing tests** — `GetPagedVenuesForListAsync(" bar  x ")` must forward `"bar x"` to `GetPagedWithEventInfoAsync` (fails today: raw pass-through); Create/Update venue and event names: `" My  Venue "` → `"My Venue"` on the entity handed to the repo.
- [ ] **Step 2: Run, verify FAIL** — `dotnet test MyVocaList.Tests --filter "VenueServiceTests|EventServiceTests"`.
- [ ] **Step 3: Implement** — `query = StringNormalization.NormalizeSearchQuery(query)` in GetPagedVenuesForListAsync; `TrimForStorage(name)` in Create/Update venue and event-name sites (replacing the existing `Trim()` calls).
- [ ] **Step 4: Run tests → PASS; build 0 errors.**
- [ ] **Step 5: Commit** — `fix: VenueService/EventService — normalize list search query and stored names via StringNormalization (REQ-TRIM-03/05/06)`.

---

### Task 5 [P]: SongService + SongSuggestionService + CatalogService — search + storage normalization

**Files:**
- Modify: `Services/SongService.cs` (GetPagedSongsForListAsync ~L246, ExistsByTitleForArtistAsync ~L219, CreateSongAsync ~L71/L84, UpdateSongAsync ~L114-132, CreateSongWithUrlsAsync ~L156-172)
- Modify: `Services/SongSuggestionService.cs` (GetLocalAsync/GetRemoteAsync term handling)
- Modify: `Services/CatalogService.cs` (GetPagedCatalogForArtistAsync query handling)
- Test (modify): `MyVocaList.Tests/Unit/Services/SongServiceTests.cs` (create if missing, following `ArtistServiceTests.cs` conventions)

**Interfaces:**
- Consumes: `StringNormalization` (Task 1). Produces: none (signatures unchanged).

- [ ] **Step 1: Write failing tests** — paged/suggestion/catalog queries: `" my  song "` reaches repo as `"my song"`; storage: title/featuredArtists/version via `TrimForStorage`/`TrimForStorageOrNull` (`" A  Title "` → `"A Title"`; whitespace-only featuredArtists → null); `ExistsByTitleForArtistAsync` compares the normalized title so dedup agrees with storage (REQ-TRIM-06).
- [ ] **Step 2: Run, verify FAIL** — `dotnet test MyVocaList.Tests --filter SongServiceTests`.
- [ ] **Step 3: Implement** — substitute the existing `Trim()`/`?.Trim()` sites per the mapping in Step 1; normalize query at top of CatalogService.GetPagedCatalogForArtistAsync and SongSuggestionService term handling.
- [ ] **Step 4: Run tests → PASS; build 0 errors.**
- [ ] **Step 5: Commit** — `fix: SongService/SongSuggestionService/CatalogService — normalize search queries and stored strings via StringNormalization (REQ-TRIM-01/03/05/06/07)`.

---

### Task 6: Integration verification + docs close-out (main agent, shell + docs only)

- [ ] **Step 1:** Merge task branches to develop in order (Task 1 first; 2–5 any order — disjoint files); after each merge run `dotnet test` (full suite green, expected count ≥ current 485).
- [ ] **Step 2:** Task-log entries (Rule 5): Changed-files sections + AC traceability matrix (REQ-TRIM-01..10 → test methods) in `task-log.md`; BUG-046 entry marks regression Red→Green evidence (Major class requirement).
- [ ] **Step 3:** BACKLOG rows (BUG-046 + String trimming) → 🟡 In Progress → ✅ per milestone; Helder on-device E2E of REQ-TRIM-01/02 (visible text untouched) noted as the human gate.
- [ ] **Step 4:** Session-End Spec Update Ritual — verify requirements.md/design.md still describe what was built; commit docs on develop.

## Self-review notes

- Spec coverage: REQ-TRIM-01/02 (T2 + Helder E2E; 02 holds by construction — services never write the bound Text), 03 (T2–T5 paged methods), 04 (T2 min-length tests), 05/06 (T2–T5 storage tests), 07 (T2 optional fields, T5 featuredArtists), 08 (T1), 09 (no UI files in any task's Files list), 10 (T1 diacritic-preservation test + no case-fold code).
- Line numbers are current as of the 2026-07-15 Explore sweep — implementors must re-verify against the live file, and verify repository-interface member names before writing mocks.
- MusicMetadataService/YouTubeSearchService (design.md call-site table) are remote-API query shapers — deferred: normalization there changes outbound API calls, not BUG-046/persistence; add a follow-up BACKLOG row if Helder wants them included.
