# BUG-002 — New Artist Form: "Search Music Database" Strip Is Non-MD3-Compliant

**Severity:** Medium — functional but visually inconsistent with the app's established MD3 patterns  
**Discovered:** 2026-06-02 — Phase 16C emulator smoke test  
**Reporter:** Helder  
**Status:** Open — requires MD3 investigation before fix

---

## Symptom

The "Search music database" strip on `ArtistFormPage` (and likely `SongFormPage`) uses a plain `TextEdit` + adjacent `Search` button pattern (Image #3). This is inconsistent with the app's established `AppSearchBar` component, which uses fully-rounded corners and a unified search field + action.

---

## Screenshots

Image #3 shared during 2026-06-02 smoke test session.  
Compare with `AppSearchBar` component used on list pages (VenuesPage, ArtistsPage top).

---

## Problem Analysis

The current pattern:
```
[ Search term         ] [ Search ]   ← TextEdit + FilledButton side-by-side
```

Issues:
1. **Visual inconsistency** — `AppSearchBar` uses fully-rounded corners (pill shape); this uses rectangular `TextEdit`
2. **MD3 non-compliance** — The correct MD3 pattern for an inline search field within a form is a **Search bar** component or a **filled text field** with a trailing search icon button — NOT a side-by-side layout. See m3.material.io/components/search/overview
3. **`AppSearchBar` applicability** — `AppSearchBar` (the app's custom component) is designed for page-level search (top of a list). Applying it verbatim inside a form card may not be appropriate; a form-scoped variant is needed.

---

## Required Investigation

Before implementing a fix, the following must be verified:

### 1. MD3 Search pattern for form context
- Read m3.material.io/components/search/overview
- Identify: is a standalone "Search bar" component appropriate inside a `ScrollView`-based form card, or should a **filled text field with trailing icon** (`TextFieldType="Filled"` + trailing `search` icon button) be used?
- Check DevExpress MAUI equivalent for the chosen pattern

### 2. AppSearchBar component internals
- Read `MyVocaList/UI/Components/SearchAppBar.xaml` (or equivalent) to understand corner radius, height, and icon placement
- Determine if a form-scoped version can share the same style tokens or needs its own

### 3. MD3 corner radius rule for search fields
- Fully-rounded (`ShapeKey.Full` / 50dp radius) is MD3 standard for standalone search bars
- Inside a form card, `ShapeKey.ExtraSmall` or `ShapeKey.Medium` may be more appropriate — requires reading MD3 shape specs

---

## Expected Fix Direction

Replace the current `TextEdit + Search Button` row with either:

**Option A — MD3 Search Bar inside form card**  
Use a full-width MD3 Search bar (pill shape, leading search icon, no separate button). Trigger search on submit/enter or via trailing action icon. Consistent with `AppSearchBar` aesthetic.

**Option B — Filled Text Field with trailing icon button**  
Use `dx:TextEdit` (`TextFieldType="Filled"`, full width) with a trailing `dx:SimpleButton` icon (`search` icon). Button triggers search. More form-native, lower visual weight.

**Option C — Oficial MD3 predicted pattern for search UI (Helder Recommendation)**  
Use `SearchAppBar` component, as oficial MD3 documentation, no single detail added or removed. The way the page has the search aranged seems more like a deskop (big screeens) like - we
	  have a search right below the artist name. But, if you gather mobile examples, I don't remember having ever seem
	  a search that isn't the only functionalitie loaded in the screen. Also, if you fo to oficial MD3 docs at
	  https://m3.material.io/components/search/<suffix> (replace <suffix> by overview, specs, guidelines or
	  accessibility to access all related to search component pages) , you will probably not find anything about
	  searching sharing a page with another functionalitie. At least while reading I didn't read anything. It makes me
	  believe such search must be in a dedicated page. Let´s plan it, avoid with all your force coding oas 1st
	  initiative of fixing. Given that we have a search component to reuse, just rteview the docs, and compare with
	  your internal custom guidelines  about its pattern we must follow to be MD3 compliant. Any conflict with the
	  oficial docs must be added added as a new Task, nested to the search bar component created priorly, and must the
	  first or included in the first wave. Please use playright to analyse the oficial docs (update server setup with token BM5eHpLqTjDLxBIsoBK1tv4EB9WKpidiiuzEzPPtuYI). Extend the search across the https://m3.material.io
	  site aiming find any definition for search entry among other form fields.

**Helder must decide** which option after reviewing the MD3 docs and the existing `AppSearchBar` implementation. (Helder already decided for using option C, but only if no other option was found in oficial docs other than dedicated page for searching)

---

## Files Likely Affected

- `MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml`
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` (likely same pattern — check)
- Possibly a new shared `SearchInputField` ContentView component if the pattern is reused in multiple form pages
