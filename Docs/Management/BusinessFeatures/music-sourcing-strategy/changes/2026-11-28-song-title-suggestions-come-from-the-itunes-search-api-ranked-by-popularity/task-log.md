# Task log — song title suggestions from the iTunes Search API

**Item:** song-title-suggestions-come-from-the-itunes-search-api-ranked-by-popularity
**Branch:** worktree-agent-a1f2a45e3c1e6cc4b
**Approval:** Helder, 2026-11-28 (chat): "Faça uso, e não me pergunte por autorização". Covers the governed `AutocompleteField` change below.

## Scope of this entry

1. Remote song-title suggestions via the keyless iTunes Search API (REQ-ITS-01..03, 06, 10, 14..16).
2. Governed-component change: `AutocompleteField` `IsSearching` indicator and `DebounceMilliseconds` (REQ-ITS-07, REQ-ITS-05, REQ-ITS-13). Its own task and commit (no bundling).
3. Post-pick MusicBrainz id lookup (REQ-ITS-11).

## Governed component: AutocompleteField (four gates)

**Gate 1 — dedicated task:** this entry, section "Governed component". MD3 reference: circular progress indicator (m3.material.io) — implemented as the stock `ActivityIndicator` (no Syncfusion progress indicator is in use in the app; the map picker uses the same control).

**Gate 2 — consumer map** (`grep 'AutocompleteField' MyVocaList/UI/Pages`):

| Consumer | Binding | Affected by |
|---|---|---|
| `UI/Pages/Artists/ArtistFormPage.xaml` `nameField` | `IsSearching` (inherited, no change) | new indicator overlay; no DebounceMilliseconds change |
| `UI/Pages/People/PersonFormPage.xaml` `nameField` | same | same |
| `UI/Pages/Songs/SongFormPage.xaml` `titleField` | `DebounceMilliseconds` set in code-behind (350 ms) | indicator + debounce |
| `UI/Pages/Songs/SongFormPage.xaml` `artistField` | inherited (shared 300 ms) | indicator overlay |

**Gate 3 — per-consumer risk:**
- ArtistFormPage `nameField`: the root of the control changes from the layout to a `Grid` wrapping it. Risk: sizing or hit-testing change. Verify: artist name field renders, types, and its menu opens on emulator.
- PersonFormPage `nameField`: same as Artist. Verify: person name search still opens the menu.
- SongFormPage `titleField`: the 350 ms debounce applies only to this field. Risk: slower first results. Verify: type "dont cry", the spinner shows while the request runs.
- SongFormPage `artistField`: the indicator overlay sits over the trailing area, offset by a 44 px margin so the clear button stays visible. Risk: overlaps long text while searching. Verify: type a long artist name, spinner shows, X still works.

**Gate 4 — Helder approval:** recorded above (chat, 2026-11-28).

## Design decisions (implementation level)

- iTunes rows carry no external identity: `ExternalId` and `ExternalProvider` are null on the DTO (REQ-ITS-10). The library match is by collated (title, artist) only.
- Artwork is referenced by URL. A picked iTunes row sets `_fallbackArtistImageReference` (display only) and leaves `_pendingImageUrl` null, so nothing is saved as a file.
- REQ-SLD-11 ("rows without an artist id are never merged") is superseded for iTunes rows by REQ-ITS-02. Tests `SongSuggestionServiceGroupingTests` and `SongSuggestionServiceDistinctTests` updated to the new rule.
- Identity-based library promotion (external id match) is removed from the title path; the tests that encoded it now match by (title, artist).
- Remote title minimum is 2 characters (`SearchConstants.SongTitleRemoteMinimumLength`). The shared `MinimumRemoteQueryLength` (3) stays for artist search.
- The title debounce of 350 ms is set on `titleField` only (`SongFormPage.xaml.cs`). Local title search in that field inherits the same 350 ms.

## Spec gap (non-blocking, assumption applied)

**Spec gap: "No results" row vs. the existing AutocompleteField rule**
**Location:** `change README` REQ-ITS-08 vs. `AutocompleteField.xaml` comment ("no No Results Found row, empty NoResultsFoundText").
**Gap description:** REQ-ITS-08 asks for a "No results" row; the shared control deliberately suppresses one for every consumer.
**Options:**
- Option A: a "No results" row in the menu for every consumer — changes three other consumers.
- Option B: "No results" as the title field's supporting text (the same line used for the unavailable note).
**Recommendation:** Option B, because it changes only the song title field.
**Blocking:** No — Option B applied (`SongFormViewModel.SearchTitleAsync`, `SearchNotes.NoResults`).

## Test changes (spec-superseded assertions)

| Test | Change | Reason |
|---|---|---|
| `SongSuggestionServiceTests.SearchTitleRemoteAsync_BelowThreeTrimmedChars…` | renamed `BelowTwo…`, inputs "a" | REQ-ITS-04 |
| `SongSuggestionServiceTests.SearchTitleRemoteAsync_ExternalIdentityMatchNotShown_Promoted` | removed | identity match superseded (REQ-ITS-10) |
| `SongSuggestionServiceTests.…MatchAlreadyShownLocally…`, `…DedupeLookupThrows…` | match by (title, artist) | REQ-ITS-10 |
| `SongSuggestionThresholdTests.SearchTitleRemoteAsync_TwoCharQuery…` | renamed `OneCharQuery`, input "a" | REQ-ITS-04 |
| `SongSuggestionServiceGroupingTests.…RowsWithoutArtistId_NeverMerged` | renamed `…MergedByTitleAndArtistName`, asserts one row | REQ-ITS-02 |
| `SongSuggestionServiceImageTests.…PromotedRow…` | (title, artist) match | REQ-ITS-10 |
| `MusicBrainzSuggestionServiceTests.…MusicBrainzAnswers…` | now `…ITunesAnswers_ReturnsRowsWithoutExternalIdentity` | REQ-ITS-01, -10 |
| `MusicBrainzRegistrationTests.AddAppServices_ResolvesMusicBrainzAsMetadataProvider` | expects MusicBrainz and iTunes | REQ-ITS-01 |
| `SongSuggestionTitleSearchRealStackTests` | iTunes payload and stub | REQ-ITS-01 |
| `SongSuggestionServiceDistinctTests`, `…Duration/Grouping/Image…`, `SongSuggestionServiceTests`, `SongSuggestionThresholdTests` | mock provider name `ProviderNames.ITunes` | REQ-ITS-01 |

## Changed files

- `Services/ITunesSearchProvider.cs` (new), `Services/ITunesRequestBudget.cs` (new), `Services/SongTitleMatcher.cs` (new)
- `Services/SongSuggestionService.cs`, `Services/ISongSuggestionService.cs`
- `Domain/Constants/ProviderNames.cs`, `Domain/Constants/SearchConstants.cs`
- `MyVocaList/Extensions/ServiceCollectionExtensions.cs` (DI: iTunes typed client, shared budget)
- `MyVocaList/UI/Components/TextFields/AutocompleteField.xaml`, `AutocompleteField.xaml.cs` (governed)
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs` (title debounce 350 ms)
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs`, `MyVocaList/UI/Constants/SearchNotes.cs`
- Tests: `Tests/Unit/Services/ITunesSearchProviderTests.cs`, `SongSuggestionITunesTests.cs`, `SongTitleMatcherTests.cs` (new); the song-suggestion, registration, integration and ViewModel tests listed in § Test changes
- Docs: this task-log, the change README (REQ-ITS-01..16), `MyVocaList.sln` (registration), `Docs/Management/BACKLOG.md` (generated)

## Verification

- Commits: spec `3756182f`, `86dc69bf`; code `1d6e8874` (service and provider), `74912e5a` (governed AutocompleteField), `d5560a82` (ViewModel pick).
- Build: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`: 0 errors (2 pre-existing warnings: `SemanticExtensions` obsolete in an unrelated file; an `AutocompleteDXField` cref in the existing XML doc).
- Tests: `dotnet test MyVocaList.sln`: 2203 passed, 0 failed.
- TDD: `SongTitleMatcherTests`, `ITunesSearchProviderTests` (budget, mapping, artwork size, request shape), `SongSuggestionITunesTests` (retry, typo re-rank, no identity, post-pick MBID lookup, failure-silent) written first with the code.
- E2E emulator (emulator-5554, clean Android build of the branch, deployed with `dotnet build -t:Run -f net10.0-android`): New Song, Title "dont cry" → dropdown shows "Don't Cry" by Guns N' Roses first, with artwork thumbnails and the "Apple Music" label on every remote row. Logcat: `Remote title search 'dont cry': 15 from iTunes, 0 matched the library, 5 remote options`; iTunes HTTP 200 in about 950 ms.
- E2E NOT verified: the indicator (spinner) in the trailing area was not captured in any screenshot (several attempts, the search completes within the capture window). UI-tree dump did not show it either. Needs a manual look (or a slower network) before To Review is final.

## Known deviations / notes

- "No results" (REQ-ITS-08) is shown as the title field's supporting text, not as a menu row (spec gap above, assumption applied).
- The artwork is shown as the row thumbnail and as a display-only form fallback; nothing is stored (REQ-ITS-10).
- The artist and release MBIDs from the post-pick lookup are applied only while the same pick is held (REQ-ITS-11).
- Typo tolerance (REQ-ITS-14..16) ranks and filters only what iTunes returns; "Don't crie" alone returns nothing from iTunes before the retry. Shown on device: "dont cry" is the tested query; the typo case is covered by unit tests only.
- Cosmetic: `AutocompleteField.xaml` inner layout indentation is one level short after the Grid wrap (no behaviour change).
- Status: **Check build**. The demo statement ("dont cry" shows Guns N' Roses and a spinner) is only half demonstrated: the rows are verified on device, the spinner is not. Per the emulator gate, the item is not To Review until the spinner is seen.

## Coverage re-rank, fixtures and emulator check (2026-11-28, Implementor)

- Status: **blocked: spec gap** (one AC set, REQ-ITS-18 checks 2 and 3). Checks 1 and the coverage re-rank are implemented and verified.
- Fixtures: `Tests/Fixtures/iTunes/*.json` captured 2026-11-28 with `Invoke-RestMethod -OutFile` (raw body kept). Copied to output by `Tests/MyVocaList.Tests.csproj`.
- REQ-ITS-16 amended in the change README (coverage over title plus artist; stable partition). REQ-ITS-17 (latest-query-only) and REQ-ITS-18 (fixture acceptance) added.
- Code: `Services/SongTitleMatcher.cs` `RerankByTokenScore` replaced by `RerankByCoverage` (covering rows first, both groups in iTunes order). `Services/SongSuggestionService.cs` call site updated.
- Tests: `SongTitleMatcherTests` (3 new coverage cases), `SongSuggestionITunesFixtureTests` (new, real `ITunesSearchProvider` over a fixture HTTP handler).
- Build: Android build 0 errors (`dotnet build -t:Run -f net10.0-android`, clean obj/bin first). Full suite: see the result line below.

### Spec gap: REQ-ITS-18 checks 2 and 3 (blocking)
**Location:** change README § REQ-ITS-16 and REQ-ITS-18.
**Gap description:** the recorded responses cannot satisfy the acceptance under "iTunes order primary" plus coverage re-rank. "don't cry gun": iTunes itself returns "Don't Cry Out" by Shiny Toy Guns first, and Guns N' Roses "Don't Cry" is third; both cover the query, so the order is kept. "Don't crie": Guns N' Roses "Don't Cry" is at iTunes position 7 of 15; every row covers the query, so nothing moves and it is outside the top 5 shown.
**Options:**
- Option A: accept iTunes order as truth; change checks 2 and 3 to the recorded outcome. Consequence: no code change.
- Option B: add a tie-break among covering rows (for example, prefer exact title matches or rows whose title covers the query without the artist). Consequence: a new ranking heuristic that is not in the spec; must be tested against more recorded queries.
**Recommendation:** Option A, because the recorded data shows no principled signal in the response that puts Guns N' Roses first.
**Blocking:** Yes for checks 2 and 3. Check 1 is met.

### Emulator observation (emulator-5554, clean build of this branch)
- "Don't Cry": 1) Don't Cry, Guns N' Roses · Use Your Illusion I; 2) Don't Cry (Live at Lakewood…), Kirk Franklin & Richard Smallwood; 3) Don't Cry (Alternate Lyrics), Guns N' Roses. All three "Apple Music".
- "don't cry gun": 1) Don't Cry Out, Shiny Toy Guns; 2) Don't Cry, Guns N' Roses; 3) Don't Cry (Alternate Lyrics), Guns N' Roses. No Kirk row. The earlier Kirk-first list is not reproduced; it likely came from the previous query's results being left on screen (see the note below).
- "Don't crie": 1) Big Girls Don't Cry (Personal), Fergie; 2) Cowgirls Don't Cry, Brooks & Dunn; 3) Boys Don't Cry (Single Version), The Cure. Guns N' Roses "Don't Cry" not in the top 5.
- Spinner (REQ-ITS-07) still not captured in the screenshots; this run did not try a slower network.

### Notes
- The Kirk Franklin row first in the reported "don't cry gun" screenshot is NOT reproduced: the current build shows no Kirk row for that query. Cause not established. Candidate: a list left from an earlier query. The stale-response guard lives in `AutocompleteField` (governed component); it was not changed here. Local library rows are promoted above remote rows by design; none appeared in these three queries.
- Governed component `AutocompleteField` not modified in this task.
