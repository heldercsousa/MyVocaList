# Whitespace Normalization — Search Queries (BUG-046) + Persisted Strings — Requirements

> Status: 📋 Spec — awaiting Helder review. Spec-only; no implementation yet.
> Scope: BUG-046 (autocomplete query whitespace → zero suggestions, Major) + the
> "String trimming on persistence — centralized normalization analysis" BACKLOG item (2026-07-15).
> Companion design: `design.md` (same folder).

## Problem statements

1. **BUG-046 (search side):** any extra whitespace in an autocomplete query — leading (`"  jo"`),
   trailing (`"jo "`), or doubled between words (`"jo  hn"`) — returns zero suggestions.
   Confirmed root cause: `PersonService.SearchPersonsAsync` / `SearchPersonsStartsWithAsync`
   forward the raw term to `IPersonRepository.SearchByNameStartsWithAsync` with no normalization.
   Other search services trim edges ad hoc (`query?.Trim()`) but none collapse internal runs,
   so `"foo  bar"` never matches `"foo bar"` anywhere.
2. **Persistence side:** name-like strings are trimmed before save in most Service `Create/Update`
   methods via scattered ad-hoc `name.Trim()` calls (15+ sites across PersonService, ArtistService,
   VenueService, EventService, SongService…). The pattern is convention, not contract — new fields
   can (and do) miss it, and internal double spaces are never collapsed, which defeats
   dedup/uniqueness checks (`"John  Doe"` and `"John Doe"` persist as distinct people).

## User stories

- As an admin typing in any autocomplete field (Person form singer lookup, Song form artist lookup)
  or any CRUD list search box (Venues, People, Artists, Songs, pickers), I get the same suggestions
  regardless of accidental extra spaces in what I typed.
- As an admin saving a Venue/Person/Artist/Song, the stored name never carries leading/trailing
  whitespace, so lists sort correctly and duplicate detection works.

## Acceptance criteria

### Search-query normalization (BUG-046)

- **REQ-TRIM-01** — Given an autocomplete-backed form field (PersonFormPage singer field,
  SongFormPage artist field), When the user types a query containing leading, trailing, or
  repeated internal whitespace (e.g. `"  jo"`, `"jo "`, `"jo  hn"`), Then the suggestion results
  are identical to those for the single-spaced, edge-trimmed query (`"jo"` / `"jo hn"`).
- **REQ-TRIM-02** — Given the user has typed a query with extra whitespace, When suggestions are
  returned, Then the visible entry text is exactly what the user typed — the normalization is
  applied only to the query string sent to the repository, never written back to the bound `Text`.
- **REQ-TRIM-03** — Given any CRUD list page or picker search box (Venues, People, Artists, Songs,
  ArtistPicker, SongPicker, PersonPicker, QueueSongPicker, Catalog), When a search term with extra
  whitespace is submitted, Then results match the normalized term (same rule as REQ-TRIM-01).
- **REQ-TRIM-04** — WHEN a query normalizes to a string shorter than the service's minimum search
  length (e.g. `" a "` → `"a"` with minimum 2), the service SHALL return an empty result set
  without querying the repository (existing short-query behavior, now evaluated post-normalization).

### Persisted-value trimming

- **REQ-TRIM-05** — Given a Venue/Person/Artist/Song/Event form, When the user saves a name-like
  field with leading/trailing whitespace, Then the persisted value has no leading/trailing
  whitespace (visible entry text may still show it until save completes — no live mutation).
- **REQ-TRIM-06** — WHEN a name-like value is persisted, internal whitespace runs SHALL be
  collapsed to a single space (`"John  Doe"` → `"John Doe"`), so uniqueness/dedup checks agree
  with search normalization. *(Helder decision point D1 — see design.md; if declined, this AC is
  reduced to edge-trimming only.)*
- **REQ-TRIM-07** — WHEN an optional string field (e.g. Person.Email, birthday) normalizes to
  empty/whitespace-only, the service SHALL persist `null` (existing convention, now via the helper).

### Helper contract

- **REQ-TRIM-08** — The system SHALL expose exactly one reusable normalization helper:
  `static class StringNormalization` in the Services project, namespace `MyVocaList.Services.Text`, with:
  - `string NormalizeSearchQuery(string query)` — null/whitespace-only → `string.Empty`; otherwise edge-trim + collapse internal whitespace runs to a single space.
  - `string TrimForStorage(string value)` — null → null; otherwise edge-trim (+ internal collapse per D1).
  - `string TrimForStorageOrNull(string value)` — as above, but empty/whitespace-only result → `null`.
- **REQ-TRIM-09** — All normalization call sites SHALL be inside Service-layer methods (business
  logic in Services — constitutional constraint). No ViewModel, page, or governed-component change
  is required for the core fix.

### Non-goals (explicit)

- **REQ-TRIM-10** — The helper SHALL NOT perform case folding, diacritic removal, or any
  linguistic normalization. Whitespace collapsing is a *different operation* from the
  case/diacritic normalization banned by `constraints-registry.md § EF Core / SQLite` (HARD RULE:
  no C#-side `ToLowerInvariant()`/`RemoveDiacritics()`/`*Normalized` columns) — that concern
  remains owned by DB collation (`CollationConstants.Default` / `EF.Functions.Collate`). This spec
  does not amend or weaken that rule.
- No change to `AutocompleteField` / `AutocompleteMobileField` source (governed components) is in
  scope for the core fix. An optional follow-up (min-length gate on normalized text) is stubbed in
  design.md behind the full four-gate governance process.
- No EF Core interceptor / SaveChanges-wide automatic trimming (rejected — see design.md analysis).

## Test expectations

- BUG-046 is Major → regression tests mandatory (`bug-tracking.md` HARD RULE): unit tests on
  `StringNormalization` (Level A: all branches) + service-level regression tests proving
  `PersonService.SearchPersonsAsync("  jo ")` ≡ `("jo")` fail-before/pass-after.
- Persistence: service tests (Moq repos) asserting the entity handed to the repository carries the
  normalized name (Level A for Create/Update paths touched).
