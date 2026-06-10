# BUG-003: SongsPage Filter Chips — Use FilterChipGroup Instead of DXButton

**Status:** 🟡 In Progress (specification phase)  
**Severity:** Medium (MD3 non-compliance)  
**Discovered:** 2026-06-09  

---

## Summary

If/when filter chips are added to SongsPage (either as a feature or in a future branch), they must use `dxe:FilterChipGroup` per MD3 spec, not plain `DXButton` elements styled as chips.

Root cause: Developers may code filters without consulting DX documentation or the `.claude/library/devexpress-patterns.md` pre-implementation checklist, defaulting to the generic `DXButton` component.

---

## Requirement

| Aspect | Specification |
|--------|---|
| **Component** | `dxe:FilterChipGroup` (DevExpress built-in MD3 Filter Chip component) |
| **Not:** | Plain `DXButton` elements with manual styling |
| **Reference** | `.claude/library/devexpress-patterns.md § FilterChipGroup` |
| **MD3 Spec** | m3.material.io/components/chips/overview — Filter variant |

---

## Context

SongsPage currently operates in two modes (Global or Catalog) controlled by an optional `ArtistId` query parameter. Currently, no filters exist on the page. This bug documents the **preventative guidance** that must apply IF filters are ever added to SongsPage:

1. If a catalog or role filter is added to SongsPage (e.g., "All Roles / Composers / Performers" like ArtistsPage has)
2. The filter UI must use `dxe:FilterChipGroup`, not `DXButton`

---

## Prevention

**Why this bug was created:** To encode the lesson from the pre-implementation checklist (`.claude/library/devexpress-patterns.md`) directly into the codebase as a tracked bug. This ensures future developers know the expected pattern for SongsPage filters.

**Prevention mechanism:** Developers must follow the pre-implementation audit checklist before coding any filter UI:
1. Check `.claude/library/devexpress-patterns.md` for existing patterns ✅ (FilterChipGroup is documented)
2. Check DevExpress docs for built-in component ✅ (dxe:FilterChipGroup exists)
3. Verify MD3 compliance ✅ (FilterChipGroup is MD3-compliant)
4. Use the documented pattern → Result: correct component, MD3 compliance

---

## Reference Implementation

See `ArtistsPage.xaml` and `ArtistsViewModel.cs` (Phase 16A Wave 2 — `4453bd7`):
- **XAML:** `dxe:FilterChipGroup` with `SelectedItems` binding (TwoWay)
- **ViewModel:** `SelectedRoleFilters` IList property with `OnSelectedRoleFiltersChanged` partial method mapping chip selection to domain enum

---

## Not a Task

This is **not** an active implementation task. It is a **recorded specification** of expected behavior IF filters are added to SongsPage. The bug documents "what we learned" so the lesson persists even if the current SongsPage implementation doesn't have filters yet.

**Closure condition:** When SongsPage gains a filter feature, verify that filters use `dxe:FilterChipGroup` as specified here. If the feature is never implemented, the bug remains as preventative documentation.
