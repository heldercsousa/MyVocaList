# DevExpress MAUI Component Patterns — Pre-implementation audit checklist + namespace declarations

> Section file split from `devexpress-patterns.md` on 2026-07-14 (token-scoped reads). Index + never-miss rules: `devexpress-patterns.md`.

## ⚠️ PRE-IMPLEMENTATION AUDIT CHECKLIST

**Before implementing any custom UI component or using a generic component (DXButton, DXBorder, etc.) for a specific UI pattern, complete this checklist. Skipping this checklist is how MD3 non-compliance bugs slip into code review.**

### For every UI pattern you're about to code:

1. **Check this file first** (`devexpress-patterns.md`)
   - [ ] Search for the pattern name (e.g. "Filter Chip", "App Bar", "List Item")
   - [ ] If found: use the **documented component and patterns exactly** — no custom implementation

2. **Check DX documentation for a built-in component**
   - [ ] Use Context7 or the official DevExpress MAUI docs (v25.2+)
   - [ ] Query: "Does DevExpress have a [pattern] component?"
   - [ ] If yes: read the DevExpress API docs, then return to step 3
   - [ ] If no: proceed to step 4

3. **Verify MD3 spec compliance of the DX component**
   - [ ] Visit m3.material.io and find the component specification
   - [ ] Compare DX component properties against the MD3 spec
   - [ ] Document findings: "DX [component] implements MD3 [pattern]" in the spec/design doc
   - [ ] Add an entry to this file (`devexpress-patterns.md`) if this is a new confirmed DX component

4. **Only if DX has no equivalent: implement a custom component**
   - [ ] Document in `.claude/library/` the custom component and its MD3 alignment (example: `m3-components.md § AppBar`)
   - [ ] Store the component in `MyVocaList/UI/Components/[SubFolder]/`
   - [ ] Add examples and BindableProperty patterns to the rule file

### Quick-reference substitution table — check before reaching for stock MAUI

| Reach-for | Check DX first | DX component |
|-----------|---------------|--------------|
| `Button` for filter chips | ✅ DX has it | `dxe:FilterChipGroup` / `dxe:FilterChip` |
| `Frame` / `Border` | ✅ DX has it | `dx:DXBorder` |
| `ListView` / `CollectionView` | ✅ DX has it | `dxcv:DXCollectionView` |
| `Entry` / `Editor` | ✅ DX has it | `dxe:TextEdit` / `dxe:MultilineEdit` |
| Custom bottom sheet | ✅ DX has it | `dx:BottomSheet` |
| `Picker` / `DatePicker` | ✅ DX has it | `dxe:ComboBoxEdit` / `dxe:DateEdit` |
| Custom swipe actions | ✅ DX has it | `dxcv:SwipeContainer` |
| `ScrollView` | ✅ DX has it | `dx:DXScrollView` |
| Loading skeleton | ✅ DX has it | `dx:ShimmerView` |

**Never substitute `DXButton` for filter chips** — use `dxe:FilterChipGroup`.
**Never substitute `BoxView`/`Frame` for cards** — use `dx:DXBorder` (or `dxe:DXCard` if available).

### Example: Filter Chips

❌ **WRONG:** "I need filter buttons on SongsPage. I'll use three `DXButton` elements."
- You skipped step 1 (didn't check this file).
- You skipped step 2 (didn't ask if DX has a chip component).
- Result: `DXButton` is generic; you'll wire custom styling → MD3 non-compliance.

✅ **RIGHT:** "I need filter buttons. Let me check the file... Found it! `dxe:FilterChipGroup` is in step 1. I'll use that."
- You found the documented pattern.
- `FilterChipGroup` is confirmed DX MD3-compliant → automatically MD3 compliant.
- Result: correct component, correct MD3 alignment, confidence in review.

---

## Namespace Declarations

```xml
xmlns:dx="http://schemas.devexpress.com/maui"
xmlns:dxe="clr-namespace:DevExpress.Maui.Editors;assembly=DevExpress.Maui.Editors"
xmlns:dxcv="clr-namespace:DevExpress.Maui.CollectionView;assembly=DevExpress.Maui.CollectionView"
xmlns:dxg="clr-namespace:DevExpress.Maui.DataGrid;assembly=DevExpress.Maui.DataGrid"
xmlns:dxc="clr-namespace:DevExpress.Maui.Charts;assembly=DevExpress.Maui.Charts"
```
