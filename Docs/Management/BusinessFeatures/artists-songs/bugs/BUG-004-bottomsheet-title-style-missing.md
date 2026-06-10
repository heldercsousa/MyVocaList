# BUG-004: BottomSheetTitle Style Missing from MaterialStyles.xaml

**Status:** 🟡 In Progress (specification phase)  
**Severity:** High (runtime style lookup failure)  
**Discovered:** 2026-06-09  

---

## Summary

BottomSheet components reference a `BottomSheetTitle` style that does not exist in `MaterialStyles.xaml`. This causes a runtime style lookup failure when any BottomSheet is displayed.

---

## Symptom

When a BottomSheet with a title appears (e.g., in SongFormPage, EditorPages), the title label has no style applied. Likely consequence: typographical inconsistency, wrong color, or layout collapse.

**Error indication:** Runtime warning/error in debug output about missing style `BottomSheetTitle`.

---

## Root Cause

A BottomSheet component in the app was implemented with `Style="{StaticResource BottomSheetTitle}"` but the corresponding style definition was never added to `MaterialStyles.xaml`.

---

## Solution

**Add the `BottomSheetTitle` style to `MaterialStyles.xaml`** with MD3 typography + colors:

| Property | Value | Rationale |
|----------|-------|-----------|
| **TargetType** | `Label` | BottomSheet titles are `Label` elements |
| **FontFamily** | `RobotoRegular` | MD3 title font |
| **FontSize** | `22` | MD3 titleLarge (22sp per spec) |
| **TextColor** | `{StaticResource OnSurface}` | MD3 semantic color for foreground text |
| **FontAttributes** | `None` (not Bold) | MD3 titleLarge is Regular weight, not Medium |
| **Padding** | `16,16,16,0` | M3 sheet content padding: 16dp start/end/top, 0 bottom (title is followed by body) |

### XAML Code

```xml
<Style x:Key="BottomSheetTitle" TargetType="Label">
    <Setter Property="FontFamily" Value="RobotoRegular" />
    <Setter Property="FontSize" Value="22" />
    <Setter Property="TextColor" Value="{StaticResource OnSurface}" />
    <Setter Property="FontAttributes" Value="None" />
    <Setter Property="Padding" Value="16,16,16,0" />
</Style>
```

---

## Reference

- **MD3 Bottom Sheet spec:** m3.material.io/components/bottom-sheets/specs
- **Typography:** Headline Large (22sp, RobotoRegular, OnSurface) per MD3 guidance
- **Existing styles:** See `MaterialStyles.xaml` for pattern (e.g. `EmptyStateHeadline`)
- **Usage location:** Any BottomSheet with a title label using `Style="{StaticResource BottomSheetTitle}"`

---

## Implementation

**File:** `MyVocaList/Resources/Styles/MaterialStyles.xaml`

**Action:** Add the style definition above to the file, then rebuild and verify BottomSheets display titles correctly.

**Verification:**
1. Run the app
2. Navigate to any page with a BottomSheet that has a title (e.g. SongFormPage URL removal/edit dialog, editor pages)
3. Open the BottomSheet
4. Verify the title is displayed with correct:
   - Font size (22sp, appears larger than body text)
   - Color (OnSurface — white/light gray on dark mode)
   - Padding (16dp margins visible)
   - No style lookup errors in debug output

---

## Note

This is a simple addition to the central style resource file. No code or architecture changes required — just a missing style definition.
