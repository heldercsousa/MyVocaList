# Search Page Component — Research Findings

**Date:** 2026-06-03  
**Session:** BUG-002 MD3 investigation + component scope expansion  
**Status:** Spike complete — ready for brainstorming → spec

---

## 1. MD3 Research (Playwright — m3.material.io)

### What MD3 documents for Search

Three and only three search entry points are documented:

| Entry point | When to use |
|---|---|
| **Search bar** | Top of a screen, below a title — searches content in that specific view |
| **Search app bar** | App bar variant — when search is the primary global function |
| **Search icon button** | Secondary action — leads into focused / full-screen search |

Key placement rule: *"A search bar is typically placed at the **top of a screen** to remain prominent and accessible."*

Key standalone rule: *"If search is the primary action, focused search can be a **standalone destination** reached from a navigation bar."*

**No inline form search pattern exists anywhere in MD3 docs.** Text fields guidelines also contain no search-in-form pattern.

### Conclusion for BUG-002

The current `TextEdit + Search button` strip inside `ArtistFormPage` and `SongFormPage` has no MD3 basis. The correct approach is a **dedicated search destination page** navigated to from the form (MD3 search icon button entry point → standalone search page → result returned to caller).

---

## 2. Current Search Instances in the Codebase

All three share the same non-MD3 pattern: `TextEdit + FilledButton` side-by-side inside a `Border` card.

| Location | Purpose | Result item shape |
|---|---|---|
| `ArtistFormPage.xaml` lines 63–107 | Search music database for artist | Single line: `ArtistName` |
| `SongFormPage.xaml` lines 57–107 | Search music database for song | Two lines: `SongTitle` + `ArtistName` |
| `SongFormPage.xaml` lines 172–275 | Search YouTube for karaoke video | Thumbnail + title + channel + duration + Add button; also has a "Paste URL" sub-section |

---

## 3. Shared Component Opportunity

Helder identified that the three search instances share structure and many could be backed by a **Search Page Component** — a reusable `ContentPage` shell that:

- Hosts the existing `SearchAppBar` (already built) at the top
- Renders a parameterised result list (caller supplies item template)
- Handles focused/full-screen layout (MD3 default for compact screens)
- Returns a selected result to the caller via Shell query parameters or a callback

Shared across all three instances:
- `SearchAppBar` (already exists in `UI/Components/AppBars/`)
- Loading indicator pattern
- Empty / no-results state (`EmptyState` component, already exists)
- Back navigation (`BackCommand` already on `SearchAppBar`)

Per-instance variation:
- Placeholder text
- Result item layout (single-line, two-line, thumbnail row)
- The service/API being called
- What gets returned to the caller

---

## 4. Open Questions for Brainstorming Session

1. **Navigation model:** Full push (form → search page → pop back with result) vs modal/bottom sheet vs replace?
2. **Result passing:** Shell `QueryProperty` on the form ViewModel, or a shared `ISearchResultCallback` interface?
3. **YouTube search specifics:** Does YouTube search move to its own page too, or stay in `SongFormPage` since it also has the Paste URL sub-section (which is NOT a search — it's a direct input)?
4. **Component boundary:** One generic `SearchPage<TResult>` (generic XAML isn't supported in MAUI) vs a base `SearchPageBase` with typed subclasses vs a `SearchPage` ContentView with injected item template?
5. **FloatingToolbar:** Are there cases where a floating toolbar is needed on the search results page?
