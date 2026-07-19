# Whitespace Normalization — Tasks

> Source spec: `requirements.md` (REQ-TRIM-01..10) · `design.md` · execution detail: `plan.md` (same folder).
> DRY Onion: all work is Services-layer (+ tests) — Domain/Infra untouched, UI untouched (REQ-TRIM-09).
> Every code task: git worktree branched from `develop` (Rule 2 HARD RULE). Tasks 2–5 are `[P]` — disjoint `Files owned`, may run as one wave of ≤4 after Task 1 merges.

- [ ] **Task 1 — `StringNormalization` helper + Level-A unit tests**
  - Produces: `MyVocaList.Services.Text.StringNormalization` (`NormalizeSearchQuery`, `TrimForStorage`, `TrimForStorageOrNull` — exact signatures in plan.md Task 1)
  - Consumes: nothing
  - Risk: Low — new pure static class, no existing behavior touched
  - Files owned: `Services/Text/StringNormalization.cs`, `MyVocaList.Tests/Unit/Services/Text/StringNormalizationTests.cs`
  - Demo: `dotnet test --filter StringNormalizationTests` green; `"  jo  hn "` → `"jo hn"` in all three methods per contract
  - Review lane: verifier subagent (spec REQ-TRIM-08/10 vs diff)

- [ ] **Task 2 [P] — PersonService: BUG-046 regression fix (search) + storage trimming** *(depends: Task 1)*
  - Produces: normalized queries in `SearchPersonsAsync`/`SearchPersonsStartsWithAsync`/`GetPagedPersonsForListAsync`; `TrimForStorage`/`OrNull` in `CreatePersonAsync`/`UpdatePersonAsync`; BUG-046 regression tests (Red→Green evidence — Major, mandatory)
  - Consumes: Task 1 helper
  - Risk: Medium — hot search path for autocomplete; min-length gate moves post-normalization (REQ-TRIM-04)
  - Files owned: `Services/PersonService.cs`, `MyVocaList.Tests/Unit/Services/PersonServiceTests.cs`
  - Demo: regression tests fail on pre-fix code, pass after; `SearchPersonsAsync("  jo ")` ≡ `("jo")`
  - Review lane: verifier subagent + Helder on-device E2E (REQ-TRIM-01/02, autocomplete singer field)

- [ ] **Task 3 [P] — ArtistService + ArtistSuggestionService normalization** *(depends: Task 1)*
  - Produces: `NormalizeSearchQuery` in `SearchArtistsByNameAsync`/`GetPagedArtistsForListAsync`/suggestion term handling; `TrimForStorage`/`OrNull` in `CreateArtistAsync`/`UpdateArtistAsync`
  - Consumes: Task 1 helper
  - Risk: Low — replaces existing `Trim()` sites; internal-collapse is the only behavior change
  - Files owned: `Services/ArtistService.cs`, `Services/ArtistSuggestionService.cs`, `MyVocaList.Tests/Unit/Services/ArtistServiceTests.cs`
  - Demo: `" ac  dc "` reaches repo as `"ac dc"`; ArtistServiceTests green
  - Review lane: verifier subagent

- [ ] **Task 4 [P] — VenueService + EventService normalization** *(depends: Task 1)*
  - Produces: `NormalizeSearchQuery` in `GetPagedVenuesForListAsync` (currently raw pass-through); `TrimForStorage` in venue/event Create/Update/Validate name sites
  - Consumes: Task 1 helper
  - Risk: Low — small surface; Venue list search gains normalization it never had
  - Files owned: `Services/VenueService.cs`, `Services/EventService.cs`, `MyVocaList.Tests/Unit/Services/VenueServiceTests.cs`, `MyVocaList.Tests/Unit/Services/EventServiceTests.cs`
  - Demo: Venues list search `" bar  x "` matches `"bar x"`; tests green
  - Review lane: verifier subagent

- [ ] **Task 5 [P] — SongService + SongSuggestionService + CatalogService normalization** *(depends: Task 1)*
  - Produces: `NormalizeSearchQuery` in `GetPagedSongsForListAsync`/`GetPagedCatalogForArtistAsync`/suggestion terms; `TrimForStorage`/`OrNull` in `CreateSongAsync`/`UpdateSongAsync`/`CreateSongWithUrlsAsync`/`ExistsByTitleForArtistAsync` (dedup agrees with storage — REQ-TRIM-06)
  - Consumes: Task 1 helper
  - Risk: Medium — most call sites (title/featuredArtists/version/externalId); dedup check semantics change with internal collapse
  - Files owned: `Services/SongService.cs`, `Services/SongSuggestionService.cs`, `Services/CatalogService.cs`, `MyVocaList.Tests/Unit/Services/SongServiceTests.cs`
  - Demo: `" A  Title "` persists as `"A Title"`; duplicate check flags `"A  Title"` vs `"A Title"`; tests green
  - Review lane: verifier subagent

- [ ] **Task 6 — Integration merge + docs close-out** *(depends: Tasks 2–5; main agent, no worktree)*
  - Produces: task branches merged to develop, full suite green; task-log entries with AC traceability matrix (REQ-TRIM-01..10) + BUG-046 Red→Green evidence; BACKLOG status updates; Session-End Spec Update Ritual
  - Consumes: all merged task output
  - Risk: Low — shell + docs only
  - Files owned: `Docs/Management/DevCycleCraft/persisted-string-trimming/task-log.md`, `Docs/Management/BACKLOG.md`, `MyVocaList.sln` (if new Docs files), `Docs/Management/LEDGER.md`
  - Demo: `dotnet test` full suite green on develop; BACKLOG rows advanced
  - Review lane: Helder final review + on-device E2E gate
