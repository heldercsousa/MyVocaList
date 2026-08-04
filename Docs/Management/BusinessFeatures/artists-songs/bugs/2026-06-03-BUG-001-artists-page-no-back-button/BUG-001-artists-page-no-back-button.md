# BUG-001 — Artists Page: No Back Button / Unclear Trailing Toggle

**Severity:** High — navigation is impossible without back button  
**Discovered:** 2026-06-02 — Phase 16C emulator smoke test  
**Reporter:** Helder  
**Status:** Fixed — 2026-06-03

---

## Issue 1 — Missing Back Button on Artists Page

### Symptom
The Artists page (`ArtistsPage`) has no visible back button in its app bar, making it impossible to navigate away from the page via UI. The only escape is the OS system back gesture.

### Screenshot
Image #2 shared during 2026-06-02 smoke test session.

### Expected
A back button (NavigationIcon / leading icon) in the `SmallAppBar` that pops the page from the navigation stack.

### Probable Cause
`SmallAppBar` `ShowBackButton` binding or `NavigationIcon` visibility is not configured for this page. May also be that the page is pushed as a detail page but the AppBar `BackCommand`/`BackVisible` property is missing.

### Fix Direction
Inspect `ArtistsPage.xaml` — confirm `SmallAppBar` has `ShowBackButton="True"` (or equivalent binding) and a `BackCommand` wired to `Shell.BackButtonBehavior` or `NavigationService.GoBackAsync`.

---

## Issue 2 — Trailing Toggle Button in Artist List Item Has No Label / Icon Hint

### Symptom
Each artist list item has a trailing pill/toggle button (Image #2) with no icon, label, or tooltip to indicate its purpose. Its function is opaque to the user.

### Screenshot
Image #2 — trailing purple oval button, no icon.

### Expected
The button should have a meaningful icon (e.g., `library_music` or `queue_music`) and/or a tooltip, or be replaced by an MD3-compliant interaction pattern (e.g., swipe action or contextual icon button with a visible affordance).

### Fix Direction
- Inspect `ArtistListItemTemplate` in `ArtistsPage.xaml` for the trailing button definition
- Add a descriptive icon inside the button **or** replace with a labeled `dx:SimpleButton` / `ImageButton` with accessible content description
- Verify against MD3 List Item trailing element specs: m3.material.io/components/lists

---

## Resolution (2026-06-03)

**Issue 1 fix:** Added `NavigationIcon="arrow_back_outlined"` and `NavigationCommand="{Binding GoBackCommand}"` to the `SmallAppBar` in `ArtistsPage.xaml`. Added `GoBackCommand` (calls `Shell.Current.GoToAsync("..")`) to `ArtistsViewModel`.

**Issue 2 fix:** Changed `Style="{StaticResource IconButton}"` → `Style="{StaticResource StandardIconButton}"` on the trailing `dx:DXButton` in both `ItemTemplate` and `SelectedItemTemplate`. The `IconButton` key does not exist in the app resources — `StandardIconButton` is the correct key defined in `MaterialStyles.xaml`. Also added `SemanticProperties.Description="View catalog"` to both buttons for accessibility.
