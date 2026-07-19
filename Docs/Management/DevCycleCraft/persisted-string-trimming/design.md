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

Because every search and every save already funnels through a Service method, normalizing there
fixes all autocomplete consumers, all list/picker search boxes, and all future CRUD pairs at once —
with **zero changes to the governed AutocompleteField component** and zero ViewModel changes.
This also automatically satisfies "never mutate the user's visible entry text" (REQ-TRIM-02):
the bound `Text` is never written by the Service.

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
| Persist (required names) | `PersonService`, `ArtistService`, `VenueService`, `EventService`, `SongService` Create/Update — every current `name.Trim()` site | `TrimForStorage` |
| Persist (optional fields) | e.g. `PersonService` email/birthday (`string.IsNullOrWhiteSpace(x) ? null : x.Trim()` sites) | `TrimForStorageOrNull` |

UI layer: no changes. `CrudListViewModelBase._currentSearchQuery = SearchText.Trim()` (UI-side
edge-trim) is left as-is — harmless, and removing it is out of scope; the Service normalization
behind it is what guarantees correctness.

## Analysis: why NOT the alternatives

- **EF Core SaveChanges interceptor (trim everything automatically):** rejected — violates the
  "zero friction" gate. It is invisible magic (values change between service and DB), would trim
  fields where whitespace may be meaningful (future lyrics/notes fields), and puts business
  behavior in Infra. Explicit helper calls in Services keep intent auditable.
- **Fix inside AutocompleteField:** rejected for the core fix — it is a governed component
  (four-gate process), it only covers 2 of the ~10 search surfaces, and component-side trimming
  would still leave the persistence problem. Service-side normalization covers everything.
- **One shared method for both search and storage:** rejected — same primitive, different
  contracts (nullability, D1 divergence risk). Two thin named methods cost nothing and keep call
  sites self-documenting.

## Decision points (resolved)

> **Decision recorded [2026-07-15]:** Helder resolved both points — D1: **YES**; D2: **APPROVED**.

- **D1 (resolved YES):** `TrimForStorage` collapses *internal* whitespace runs in addition to
  edge-trimming (REQ-TRIM-06, now unconditional) — stored values stay congruent with search
  normalization and dedup works. Search-side collapsing (REQ-TRIM-01/03) stands regardless.
- **D2 (resolved APPROVED):** Folder/tracking call — BUG-046 was *tracked* under the autocomplete
  component in BACKLOG.md, but its fix is Service-layer with no component edit, so this spec folder
  (`persisted-string-trimming/`) owns both items and the BUG-046 BACKLOG pointer was moved here
  (cross-reference kept in this section for readers arriving from
  `DevCycleCraft/autocomplete-component/`). No AutocompleteField four-gate governance is needed
  for the core fix. Reasoning: one helper, one spec, one review.

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
3. Remaining search services; 4. persistence sites; each in a worktree task branch per workflow Rule 2.
