# Whitespace Normalization — Tasks

> Source spec: `requirements.md` (REQ-TRIM-01..10) · `design.md` · execution detail: `plan.md` (same folder).
> D1/D2 recorded 2026-07-15; D3 recorded 2026-07-19 (persistence mechanism moved to EF Core `ValueConverter` — design.md § D3).
> DRY Onion: search-normalization work (Tasks 2–5) is Services-layer (+ tests) — UI untouched (REQ-TRIM-09). Persistence work (Task 6) is Infra-layer (`EntityTypeConfiguration`) per D3 — this is a deliberate, spec-recorded carve-out from the Services-only rule, not a violation.
> Every code task: git worktree branched from `develop` (Rule 2 HARD RULE). Tasks 2–5 are `[P]` — disjoint `Files owned`, may run as one wave of ≤4 after Task 1 merges. Task 6 touches `EntityTypeConfiguration`/possibly `AppDbContext.cs` (sequential-only file registry, `workflow.md`) — confirmed no overlap with Tasks 2–5's file lists, but verify at dispatch time; do not run Task 6 in the same wave as any other task touching `AppDbContext.cs`.

- [ ] **Task 1 — `StringNormalization` helper + Level-A unit tests**
  - Produces: `MyVocaList.Services.Text.StringNormalization` (`NormalizeSearchQuery`, `TrimForStorage`, `TrimForStorageOrNull` — exact signatures in plan.md Task 1)
  - Consumes: nothing
  - Risk: Low — new pure static class, no existing behavior touched
  - Files owned: `Services/Text/StringNormalization.cs`, `MyVocaList.Tests/Unit/Services/Text/StringNormalizationTests.cs`
  - Demo: `dotnet test --filter StringNormalizationTests` green; `"  jo  hn "` → `"jo hn"` in all three methods per contract
  - Review lane: verifier subagent (spec REQ-TRIM-08/10 vs diff)

- [ ] **Task 2 [P] — PersonService: BUG-046 regression fix (search normalization only)** *(depends: Task 1)*
  - Produces: normalized queries in `SearchPersonsAsync`/`SearchPersonsStartsWithAsync`/`GetPagedPersonsForListAsync`; BUG-046 regression tests (Red→Green evidence — Major, mandatory)
  - Consumes: Task 1 helper
  - Risk: Medium — hot search path for autocomplete; min-length gate moves post-normalization (REQ-TRIM-04)
  - Files owned: `Services/PersonService.cs`, `MyVocaList.Tests/Unit/Services/PersonServiceTests.cs`
  - Demo: regression tests fail on pre-fix code, pass after; `SearchPersonsAsync("  jo ")` ≡ `("jo")`
  - Review lane: verifier subagent + Helder on-device E2E (REQ-TRIM-01/02, autocomplete singer field)
  - *(Persisted-value trimming for `Person.Name`/`Email` → Task 6, D3; not in this task.)*

- [ ] **Task 3 [P] — ArtistService + ArtistSuggestionService normalization (search only)** *(depends: Task 1)*
  - Produces: `NormalizeSearchQuery` in `SearchArtistsByNameAsync`/`GetPagedArtistsForListAsync`/suggestion term handling
  - Consumes: Task 1 helper
  - Risk: Low — replaces existing `Trim()` sites; internal-collapse is the only behavior change
  - Files owned: `Services/ArtistService.cs`, `Services/ArtistSuggestionService.cs`, `MyVocaList.Tests/Unit/Services/ArtistServiceTests.cs`
  - Demo: `" ac  dc "` reaches repo as `"ac dc"`; ArtistServiceTests green
  - Review lane: verifier subagent
  - *(Persisted-value trimming for `Artist.Name`/`externalId` → Task 6, D3; not in this task.)*

- [ ] **Task 4 [P] — VenueService normalization (search only)** *(depends: Task 1)*
  - Produces: `NormalizeSearchQuery` in `GetPagedVenuesForListAsync` (currently raw pass-through)
  - Consumes: Task 1 helper
  - Risk: Low — small surface; Venue list search gains normalization it never had
  - Files owned: `Services/VenueService.cs`, `MyVocaList.Tests/Unit/Services/VenueServiceTests.cs`
  - Demo: Venues list search `" bar  x "` matches `"bar x"`; tests green
  - Review lane: verifier subagent
  - *(`EventService` has no search-query call site — its only mapping was storage-side, now Task 6.
    Persisted-value trimming for `Venue.Name`/`Event.Name` → Task 6, D3; not in this task.)*

- [ ] **Task 5 [P] — SongService + SongSuggestionService + CatalogService normalization (search only)** *(depends: Task 1)*
  - Produces: `NormalizeSearchQuery` in `GetPagedSongsForListAsync`/`GetPagedCatalogForArtistAsync`/suggestion terms; normalized comparison term in `ExistsByTitleForArtistAsync` (agrees with Task 6's converter-normalized stored title — REQ-TRIM-06)
  - Consumes: Task 1 helper
  - Risk: Low — query-side only; no entity-write sites in this task
  - Files owned: `Services/SongService.cs`, `Services/SongSuggestionService.cs`, `Services/CatalogService.cs`, `MyVocaList.Tests/Unit/Services/SongServiceTests.cs`
  - Demo: `" my  song "` reaches repo as `"my song"`; tests green
  - Review lane: verifier subagent
  - *(Persisted-value trimming for `Song.Title`/`featuredArtists`/`version`/`externalId` → Task 6,
    D3; `CreateSongAsync`/`UpdateSongAsync`/`CreateSongWithUrlsAsync` not touched by this task.)*

- [ ] **Task 6 — Persistence: EF Core `ValueConverter`s for name-like properties (D3, 2026-07-19)** *(depends: Task 1)*
  - Produces: `ValueConverter<string,string>`/`ValueConverter<string?,string?>` in `EntityTypeConfiguration` for `Person.Name`/`Email`, `Artist.Name`/`externalId`, `Venue.Name`, `Event.Name`, `Song.Title`/`featuredArtists`/`version`/`externalId`, delegating to `StringNormalization.TrimForStorage`/`TrimForStorageOrNull`; real-SQLite round-trip tests per property; removal of now-redundant ad-hoc `Trim()` sites in the corresponding Service Create/Update methods
  - Consumes: Task 1 helper
  - Risk: Medium — touches shared `EntityTypeConfiguration`/possibly `AppDbContext.cs` (sequential-only file registry, `workflow.md`); confirm no wave overlap before dispatch
  - Files owned: `Infrastructure/.../EntityTypeConfiguration` files for the five entities (verify exact paths at dispatch — do not guess), possibly `AppDbContext.cs`, new round-trip test file(s)
  - Demo: save `" John  Doe "` → reload → `"John Doe"`; save whitespace-only `Email` → reload → `null`; real-SQLite tests green
  - Review lane: verifier subagent (checks D3 rationale comment + REQ-TRIM-05/06/07 coverage, and that redundant Service-side `Trim()` calls were removed)

- [ ] **Task 7 — Integration merge + docs close-out** *(depends: Tasks 2–6; main agent, no worktree)*
  - Produces: task branches merged to develop, full suite green; task-log entries with AC traceability matrix (REQ-TRIM-01..10) + BUG-046 Red→Green evidence; BACKLOG status updates; Session-End Spec Update Ritual
  - Consumes: all merged task output
  - Risk: Low — shell + docs only
  - Files owned: `Docs/Management/DevCycleCraft/persisted-string-trimming/task-log.md`, `Docs/Management/BACKLOG.md`, `MyVocaList.sln` (if new Docs files), `Docs/Management/LEDGER.md`
  - Demo: `dotnet test` full suite green on develop; BACKLOG rows advanced
  - Review lane: Helder final review + on-device E2E gate
