# Whitespace Normalization — Design

> Status: 📋 Spec — awaiting Helder review. Spec-only; no implementation yet.
> Requirements: `requirements.md` (REQ-TRIM-01..10). Covers BUG-046 + persisted-string trimming analysis.

## Key architectural finding

The entire fix lives in the **Services layer**. The autocomplete pipeline is:

```
AutocompleteField (raw text, len≥2 gate, debounce)
  → SearchRequestedCommand.Execute(rawText)                 [component — governed, UNTOUCHED]
  → ViewModel handler (PersonFormViewModel.SearchPersonsAsync,
                       SongFormViewModel.SearchArtistsAsync)  [pass-through, UNTOUCHED]
  → Service method (PersonService.SearchPersonsAsync — currently NO normalization ← BUG-046 root cause;
                    ArtistService.SearchArtistsByNameAsync — Trim() only, no internal collapse)
  → Repository (StartsWith/Contains query — whitespace-sensitive)
```

Because every search already funnels through a Service method, normalizing search there fixes all
autocomplete consumers, all list/picker search boxes, and all future search call sites at once —
with **zero changes to the governed AutocompleteField component** and zero ViewModel changes.
This also automatically satisfies "never mutate the user's visible entry text" (REQ-TRIM-02):
the bound `Text` is never written by the Service.

> **Amended by D3 [2026-07-19]:** the *persistence* half of this finding (trim-on-save) no longer
> lives in Service Create/Update methods — see "Decision points → D3" below. Search-query
> normalization (REQ-TRIM-01–04) is unaffected and remains exactly as described above.

## The helper

**Location:** `Services/Text/StringNormalization.cs` — `namespace MyVocaList.Services.Text`.
Static pure class; no DI registration (nothing to inject — consistent with
`code-style-reference.md § DI Registration Conventions`, which registers stateful services, not
pure functions). Placed in Services (not Domain) deliberately: normalization is business behavior
and the constitutional constraint says business logic lives in Services only; a Domain placement
would invite repository/UI callers and dilute the chokepoint.

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
    public static string NormalizeSearchQuery(string query);

    /// <summary>Storage form of a required field. Null → null; else edge-trim + internal collapse (D1 approved).</summary>
    public static string TrimForStorage(string value);

    /// <summary>Storage form of an optional field. Empty/whitespace-only result → null.</summary>
    public static string TrimForStorageOrNull(string value);
}
```

Implementation note: single char-scan or `string.Join(' ', value.Split((char[])null,
StringSplitOptions.RemoveEmptyEntries))` — no regex, no allocation surprises, Level-A unit tested.
Search and persistence stay **separate named entry points** even though they share the same
collapsing primitive internally — they are different concerns (query shaping vs stored-value
shaping) and may legitimately diverge later.

## Call sites (all Service-layer)

| Path | Method | Change |
|------|--------|--------|
| Autocomplete (BUG-046 core) | `PersonService.SearchPersonsAsync`, `SearchPersonsStartsWithAsync` | `searchTerm = StringNormalization.NormalizeSearchQuery(searchTerm)` at method top; min-length check runs on the normalized value (REQ-TRIM-04) |
| Autocomplete (artist) | `ArtistService.SearchArtistsByNameAsync`, `ArtistSuggestionService.GetLocal/RemoteAsync`, `SongSuggestionService` | replace ad-hoc `Trim()` with `NormalizeSearchQuery` |
| List/picker search | `ArtistService.GetPagedAsync`, `CatalogService.GetPagedByArtistAsync`, Venue/Person/Song/Event paged+search methods, `MusicMetadataService.Search*`, `YouTubeSearchService.SearchAsync` | same substitution |
| Persist (required names) | `EntityTypeConfiguration` for `Person.Name`, `Artist.Name`, `Venue.Name`, `Event.Name`, `Song.Title`, … (Infra) | `ValueConverter<string,string>` delegating to `TrimForStorage` (D3 — supersedes per-call-site `name.Trim()`/`TrimForStorage()` calls in Service Create/Update methods) |
| Persist (optional fields) | `EntityTypeConfiguration` for nullable name-like/optional fields, e.g. `Person.Email` | `ValueConverter<string?,string?>` delegating to `TrimForStorageOrNull` (D3) |

UI layer: no changes. `CrudListViewModelBase._currentSearchQuery = SearchText.Trim()` (UI-side
edge-trim) is left as-is — harmless, and removing it is out of scope; the Service normalization
behind it is what guarantees correctness.

## Analysis: why NOT the alternatives

- **EF Core SaveChanges interceptor (trim everything automatically):** rejected — violates the
  "zero friction" gate. It is invisible magic at the DbContext level (affects every string
  property with no per-field opt-in), would trim fields where whitespace may be meaningful (future
  lyrics/notes fields), and offers no place to name/scope the behavior per property. Superseded for
  the *persistence* half by the scoped `ValueConverter` decision below (D3) — the interceptor
  remains rejected; the converter is a narrower, per-property mechanism and was evaluated
  separately.
- **Fix inside AutocompleteField:** rejected for the core fix — it is a governed component
  (four-gate process), it only covers 2 of the ~10 search surfaces, and component-side trimming
  would still leave the persistence problem. Service-side normalization covers everything for the
  *search* half (D3 does not change this — converters do not see bare query-string parameters).
- **One shared method for both search and storage:** rejected — same primitive, different
  contracts (nullability, D1 divergence risk). Two thin named methods cost nothing and keep call
  sites self-documenting. `StringNormalization.TrimForStorage`/`TrimForStorageOrNull` remain the
  implementation the `ValueConverter` delegates to (D3) — the primitive isn't duplicated.

## Decision points (resolved)

> **Decision recorded [2026-07-15]:** Helder resolved D1/D2 — D1: **YES**; D2: **APPROVED**.
> **Decision recorded [2026-07-19]:** D3 resolved — **ValueConverter for persistence sites,
> approved**. See below.

- **D1 (resolved YES):** `TrimForStorage` collapses *internal* whitespace runs in addition to
  edge-trimming (REQ-TRIM-06, now unconditional) — stored values stay congruent with search
  normalization and dedup works. Search-side collapsing (REQ-TRIM-01/03) stands regardless.
- **D2 (resolved APPROVED):** Folder/tracking call — BUG-046 was *tracked* under the autocomplete
  component in BACKLOG.md, but its fix is Service-layer with no component edit, so this spec folder
  (`persisted-string-trimming/`) owns both items and the BUG-046 BACKLOG pointer was moved here
  (cross-reference kept in this section for readers arriving from
  `DevCycleCraft/autocomplete-component/`). No AutocompleteField four-gate governance is needed
  for the core fix. Reasoning: one helper, one spec, one review.
- **D3 (resolved 2026-07-19 — ValueConverter for persistence, approved):** persisted-string
  trimming (REQ-TRIM-05/06/07) moves from explicit per-Service-method calls to a shared EF Core
  `ValueConverter<string, string>` applied in `EntityTypeConfiguration` for each name-like property
  (`Person.Name`, `Artist.Name`, `Venue.Name`, `Event.Name`, `Song.Title`, …). Rationale:
  - **Not business logic:** unlike domain rules (pricing, workflow state), whitespace-in-storage
    is a universal, exception-free data-integrity invariant — no entity has a case where leading/
    trailing/doubled whitespace is meaningful. It sits closer to "column collation" or "NOT NULL"
    than to domain behavior, so the constitutional "business logic in Services only" constraint
    does not bind it. This is a deliberate re-scoping of that constraint's boundary for this one
    concern — recorded here per `CLAUDE.md § Amending These Rules` intent, not a silent exception.
  - **No SQL-side cost:** a `ValueConverter`'s `ToProviderExpression` runs client-side in .NET on
    the parameter value before EF binds it to the SQL command (`INSERT`/`UPDATE`/`WHERE` parameter)
    — EF never emits `TRIM()`/`REPLACE()` into generated SQL. `FromProviderExpression` is the
    identity function (`v => v`), so reads are zero-cost passthrough. Verified: this eliminates the
    original objection to "Infra-layer normalization" being a query-performance risk.
  - **Enforcement over convention:** a converter fires for every write through a mapped property —
    including ones a future developer adds and forgets to wrap in `TrimForStorage` — closing the
    gap the original ad-hoc `name.Trim()` pattern had (15+ scattered call sites, easy to miss on a
    new field).
  - **Scope limit (unchanged from D1/D2 reasoning):** a converter only sees mapped entity
    properties. It does **not** reach the autocomplete/search query string (a bare method
    parameter, never an entity property) — `NormalizeSearchQuery` therefore stays an explicit
    Service-layer call per REQ-TRIM-01/03/04. D3 only reassigns *where* REQ-TRIM-05/06/07 is
    enforced; it does not change REQ-TRIM-01–04.
  - **Delegation, not reimplementation:** the converter's lambda calls
    `StringNormalization.TrimForStorage`/`TrimForStorageOrNull` (Services project) — Infra
    configures *where* the rule applies, Services still owns *what* the rule does. This keeps the
    normalization algorithm itself in one place regardless of D3.
  - **Pre-existing-data caveat (same as the rejected interceptor and as the original Service-call
    design):** rows persisted before this converter existed are not retroactively trimmed;
    converters only affect rows written/rewritten going forward. No backfill migration is in scope
    unless Helder requests one separately.

## Governance note — AutocompleteField four-gate (stub)

A **known residual limitation** stays in the component: `AutocompleteSearchGate.MinSearchLength`
(2) and the debouncer evaluate the *raw* text, so `" a"` passes the component gate but normalizes
to a 1-char query and correctly returns empty (REQ-TRIM-04) — one wasted service call, no user-visible
bug. Making the component gate whitespace-aware is an OPTIONAL follow-up that touches
`AutocompleteField`/`AutocompleteSearchGate` and therefore requires the full
`component-change-governance.md` four-gate process — **required before any such implementation,
tracked as its own dedicated gate/task**: (1) dedicated task + MD3 review, (2) fresh consumer map
(grep `<local:AutocompleteField`, currently PersonFormPage + SongFormPage), (3) per-consumer risk
assessment, (4) Helder approval on record. It is NOT part of this spec's implementation scope and
must never be bundled into the BUG-046 fix task.

## Implementation sketch (for the future plan.md — not authorized yet)

1. `StringNormalization` + full unit tests (Red first — BUG-046 is Major, regression mandatory).
2. Person search path (BUG-046 regression tests fail-before/pass-after).
3. Remaining search services (Service-layer `NormalizeSearchQuery` calls, unchanged by D3).
4. Persistence: `ValueConverter<string,string>` / `ValueConverter<string?,string?>` per name-like
   property in each `EntityTypeConfiguration`, delegating to `TrimForStorage`/`TrimForStorageOrNull`
   (D3, 2026-07-19 — supersedes the earlier "per-Service-method call" sketch); real-SQLite
   round-trip tests per converter.
Each step in its own worktree task branch per workflow Rule 2.
