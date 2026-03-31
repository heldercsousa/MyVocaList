# Styles & Structure Design
**Date:** 2026-03-30
**Status:** Approved — ready for implementation planning
**Scope:** Cleanup of existing inline styles (Part A) + style foundation for future CRUDs (Part B)

---

## Context

This spec covers:
- Centralizing inline styles scattered across VenuesPage, VenueFormPage, AppShell, and all 7 custom components
- Establishing a style foundation that makes future CRUD pages faster and consistent to build
- Ensuring every naming decision aligns with official MD3 terminology for cross-reference compliance
- Building two new reusable components that every CRUD list page will share

**Out of scope:** The 5 placeholder pages (Artists, Events, People, Preferences, BackupRestore, Queue) — these will be built from scratch as real CRUD pages later.

---

## Key Findings from Audit

### Cross-file inline pattern inventory

| Pattern | Files | Instances |
|---|---|---|
| 48×48 transparent `DXButton` (action icon) | SmallAppBar, SearchAppBar, FloatingToolbar | 11 |
| 48×48 transparent `DXButton` (nav/leading icon) | SmallAppBar, SearchAppBar | 2 |
| BoxView divider | AppShell, VenuesPage | 3 — **inconsistent property** |
| RobotoRegular 14sp OnSurfaceVariant (Body Medium) | SmallAppBar, ListItem, AppShell | 3 |
| Empty state block (icon + label) | VenuesPage | 2 — will repeat every CRUD |
| Confirm BottomSheet content | VenuesPage | 1 — will repeat every CRUD |
| Shimmer bones | VenuesPage | 6 identical DXBorder elements |

### Confirmed: DX MAUI provides colors only — not type scale

DevExpress ThemeManager generates color themes (seed → tonal palette → color scheme).
It does **not** define any MD3 typography styles for MAUI `Label`.
All `Title.Large`, `Body.Large`, etc. additions to `MaterialStyles.xaml` are non-redundant and safe.

### `VenueFormPage` implicit style redundancy confirmed

The implicit (keyless) `Style TargetType="dx:TextEdit"` in `MaterialStyles.xaml` resolves by CLR type — same as `dxe:TextEdit` in the page. The 5 explicit props in `VenueFormPage` (`BoxMode`, `FocusedBorderColor`, `BorderColor`, `BackgroundColor`, `TextColor`) are pure redundancy and should be removed.

### BoxView inconsistency

`AppShell` uses `BoxView.Color` (correct — BoxView-specific fill property).
`VenuesPage` uses `BoxView.BackgroundColor` (incorrect — `VisualElement` background, not BoxView fill).
The `Divider` named style must canonicalize `Color`, not `BackgroundColor`.

### NavDrawer section header: wrong MD3 type scale

Current: `RobotoMedium 14sp` = Label Large
MD3 spec for Navigation Drawer section header: **Label Medium (12sp, Medium weight)**
Fix as part of this work.

---

## MD3 Terminology Decisions

This app must be cross-referenceable against official MD3 docs at all times.
Every naming decision below follows MD3 official terminology over any informal name.

### "Body" terminology warning

In MD3 component anatomy, **"body"** denotes a **structural slot or container** in most contexts:
- Bottom sheet: Container → Header → **Body** (scrollable content area)
- Dialog: Container → Header → **Body** (supporting text + actions area)

Using `Body` as a BindableProperty name on content-holding components creates ambiguity.
**Rule:** For secondary descriptive text in components, use `SupportingText` — consistent with MD3 usage in Lists, Cards, Chips, and our existing `ListItem.SupportingText`.

### Component naming

| Current name | MD3 basis | Decision |
|---|---|---|
| `ListItemLeadingAvatar` | MD3 distinguishes **Monogram** (text initials in circle) from **Avatar** (photo in circle) | **Rename → `ListItemLeadingMonogram`** — component shows initials only |
| `EmptyState` (new) | MD3: "Empty state" component | Use `EmptyState` (no "View" suffix) |
| `ConfirmSheet` (new) | MD3: Modal bottom sheet with action list | Keep `ConfirmSheet` — app-level pattern name above the MD3 primitive |

### BindableProperty naming on `EmptyState`

| Slot (MD3 anatomy) | BindableProperty name |
|---|---|
| Illustration (icon/image) | `Illustration` |
| Headline (primary text) | `Headline` |
| Supporting text (secondary, optional) | `SupportingText` |

---

## Implementation Plan

### Part 1 — `MaterialStyles.xaml` additions

#### Complete MD3 type scale (5 new entries)

```xml
<Style TargetType="Label" Class="Title.Large">
    <Setter Property="FontFamily" Value="RobotoRegular" />
    <Setter Property="FontSize" Value="22" />
</Style>

<Style TargetType="Label" Class="Body.Large">
    <Setter Property="FontFamily" Value="RobotoRegular" />
    <Setter Property="FontSize" Value="16" />
</Style>

<Style TargetType="Label" Class="Body.Small">
    <Setter Property="FontFamily" Value="RobotoRegular" />
    <Setter Property="FontSize" Value="12" />
</Style>

<Style TargetType="Label" Class="Label.Medium">
    <Setter Property="FontFamily" Value="RobotoMedium" />
    <Setter Property="FontSize" Value="12" />
</Style>

<Style TargetType="Label" Class="Label.Small">
    <Setter Property="FontFamily" Value="RobotoMedium" />
    <Setter Property="FontSize" Value="11" />
</Style>
```

> Note: `Label.Small` weight is **Medium** per MD3 spec. Current `ListItem` overline uses `RobotoRegular` (deviation) — this must be corrected when Part 2 applies the style class.

#### Named styles (9 new keys)

```xml
<!-- MD3: Standard icon button — action/trailing role (OnSurfaceVariant) -->
<Style x:Key="StandardIconButton" TargetType="dx:DXButton">
    <Setter Property="BackgroundColor" Value="Transparent" />
    <Setter Property="IconColor" Value="{StaticResource OnSurfaceVariant}" />
    <Setter Property="WidthRequest" Value="48" />
    <Setter Property="HeightRequest" Value="48" />
    <Setter Property="CornerRadius" Value="24" />
    <Setter Property="HorizontalContentAlignment" Value="Center" />
    <Setter Property="VerticalOptions" Value="Center" />
</Style>

<!-- MD3: Standard icon button — navigation/leading role (OnSurface, higher prominence) -->
<Style x:Key="NavigationIconButton" TargetType="dx:DXButton">
    <Setter Property="BackgroundColor" Value="Transparent" />
    <Setter Property="IconColor" Value="{StaticResource OnSurface}" />
    <Setter Property="WidthRequest" Value="48" />
    <Setter Property="HeightRequest" Value="48" />
    <Setter Property="CornerRadius" Value="24" />
    <Setter Property="HorizontalContentAlignment" Value="Center" />
    <Setter Property="VerticalOptions" Value="Center" />
</Style>

<!-- MD3: FAB (medium, 56×56, CornerRadius=16 = ShapeKeyTokens.CornerLarge) -->
<Style x:Key="Fab" TargetType="dx:DXButton">
    <Setter Property="BackgroundColor" Value="{StaticResource Primary}" />
    <Setter Property="IconColor" Value="{StaticResource OnPrimary}" />
    <Setter Property="PressedBackgroundColor" Value="{StaticResource PrimaryContainer}" />
    <Setter Property="WidthRequest" Value="56" />
    <Setter Property="HeightRequest" Value="56" />
    <Setter Property="CornerRadius" Value="16" />
    <Setter Property="HorizontalOptions" Value="End" />
    <Setter Property="VerticalOptions" Value="End" />
</Style>

<!-- MD3: Divider — use Color (BoxView-specific fill), NOT BackgroundColor -->
<Style x:Key="Divider" TargetType="BoxView">
    <Setter Property="HeightRequest" Value="1" />
    <Setter Property="Color" Value="{StaticResource OutlineVariant}" />
</Style>

<!-- Loading skeleton bone — matches ListItem 56dp height -->
<Style x:Key="SkeletonBone" TargetType="dx:DXBorder">
    <Setter Property="BackgroundColor" Value="{dx:ThemeColor SurfaceContainerHighest}" />
    <Setter Property="CornerRadius" Value="0" />
    <Setter Property="HeightRequest" Value="56" />
    <Setter Property="Margin" Value="0,1" />
</Style>

<!-- MD3: Bottom sheet — destructive action button -->
<Style x:Key="BottomSheetDestructiveAction" TargetType="dx:DXButton">
    <Setter Property="BackgroundColor" Value="Transparent" />
    <Setter Property="TextColor" Value="{StaticResource Error}" />
    <Setter Property="HorizontalOptions" Value="Fill" />
    <Setter Property="HeightRequest" Value="56" />
</Style>

<!-- MD3: Bottom sheet — cancel/dismiss action button -->
<Style x:Key="BottomSheetCancelAction" TargetType="dx:DXButton">
    <Setter Property="BackgroundColor" Value="Transparent" />
    <Setter Property="TextColor" Value="{StaticResource Primary}" />
    <Setter Property="HorizontalOptions" Value="Fill" />
    <Setter Property="HeightRequest" Value="56" />
</Style>

<!-- MD3: Empty state — Headline slot -->
<Style x:Key="EmptyStateHeadline" TargetType="Label">
    <Setter Property="FontFamily" Value="RobotoMedium" />
    <Setter Property="FontSize" Value="16" />
    <Setter Property="TextColor" Value="{dx:ThemeColor OnSurfaceVariant}" />
    <Setter Property="HorizontalTextAlignment" Value="Center" />
</Style>

<!-- MD3: Empty state — Illustration slot (icon-only display button) -->
<Style x:Key="EmptyStateIllustration" TargetType="dx:DXButton">
    <Setter Property="IconColor" Value="{dx:ThemeColor OnSurfaceVariant}" />
    <Setter Property="IconWidth" Value="64" />
    <Setter Property="IconHeight" Value="64" />
    <Setter Property="BackgroundColor" Value="Transparent" />
    <Setter Property="InputTransparent" Value="True" />
    <Setter Property="WidthRequest" Value="80" />
    <Setter Property="HeightRequest" Value="80" />
    <Setter Property="HorizontalOptions" Value="Center" />
</Style>

<!-- MD3: Navigation drawer — Section header label (Label Medium: 12sp Medium) -->
<Style x:Key="NavDrawerSectionHeader" TargetType="Label">
    <Setter Property="FontFamily" Value="RobotoMedium" />
    <Setter Property="FontSize" Value="12" />
    <Setter Property="TextColor" Value="{StaticResource OnSurfaceVariant}" />
    <Setter Property="Padding" Value="16,8,16,4" />
</Style>
```

---

### Part 2 — Apply styles inside existing components and pages

| File | Changes |
|---|---|
| `SmallAppBar.xaml` | Nav button → `Style="{StaticResource NavigationIconButton}"`; action1–3 → `StandardIconButton`; title label → `StyleClass="Title.Large"`; subtitle → `StyleClass="Body.Medium"` |
| `SearchAppBar.xaml` | Leading button → `NavigationIconButton`; action1–3 → `StandardIconButton` |
| `FloatingToolbar.xaml` | All 5 slots → `StandardIconButton` |
| `ListItem.xaml` | Headline → `StyleClass="Body.Large"`; supporting → `StyleClass="Body.Medium"`; overline → `StyleClass="Label.Small"` *(fixes weight from Regular → Medium)* |
| `ListItemLeadingAvatar.xaml` → `ListItemLeadingMonogram.xaml` | File + class rename; initials label → `StyleClass="Label.Large"` |
| `VenuesPage.xaml` | 6 shimmer bones → `Style="{StaticResource SkeletonBone}"`; 2 empty state blocks → `<states:EmptyState ...>`; FAB → `Style="{StaticResource Fab}"` + `Margin`/`Icon`/`Command` inline; BottomSheet buttons/dividers → named styles |
| `VenueFormPage.xaml` | Remove 5 redundant TextEdit props; character counter → `StyleClass="Body.Small"` |
| `AppShell.xaml` | Group title → `Style="{StaticResource NavDrawerSectionHeader}"`; divider → `Style="{StaticResource Divider}"`; app name label → `StyleClass="Title.Large"` + `TextColor=Primary`; subtitle → `StyleClass="Body.Medium"` + `TextColor=OnSurfaceVariant` |

---

### Part 3 — New component: `EmptyState`

**Location:** `MyVocaList/UI/Components/States/EmptyState.xaml`
**Namespace:** `MyVocaList.UI.Components.States`

**MD3 Empty state anatomy:**
- Container (`VerticalStackLayout`, centered)
- Illustration slot (`dx:DXButton`, display-only, uses `EmptyStateIllustration` style)
- Headline slot (`Label`, uses `EmptyStateHeadline` style)
- Supporting text slot (`Label`, optional, `Body.Medium` + `OnSurfaceVariant`)

**BindableProperties:**

| Property | Type | Default | MD3 slot |
|---|---|---|---|
| `Illustration` | `string` | `""` | Illustration |
| `Headline` | `string` | `""` | Headline |
| `SupportingText` | `string` | `""` | Supporting text (hidden when empty) |

**Usage in VenuesPage:**
```xml
<states:EmptyState
    Illustration="nightlife_outlined"
    Headline="No venue registered"
    IsVisible="{Binding IsEmptyNoVenues}"
    Margin="32,32,32,80" />

<states:EmptyState
    Illustration="search_outlined"
    Headline="No venue found"
    IsVisible="{Binding IsEmptyNoResults}"
    Margin="32,32,32,80" />
```

---

### Part 4 — New component: `ConfirmSheet`

**Location:** `MyVocaList/UI/Components/Sheets/ConfirmSheet.xaml`
**Namespace:** `MyVocaList.UI.Components.Sheets`

**MD3 anatomy:** Modal bottom sheet → drag handle → message → Divider → destructive action → Divider → cancel action

**BindableProperties:**

| Property | Type | Notes |
|---|---|---|
| `SheetState` | `BottomSheetState` | TwoWay — driven by ViewModel |
| `Message` | `string` | The confirmation question text |
| `ActionText` | `string` | Label for the destructive action button |
| `ActionCommand` | `ICommand` | Bound to destructive action |
| `DismissCommand` | `ICommand` | Bound to cancel button |

**Internal structure:**
```xml
<dx:BottomSheet HalfExpandedRatio="0.28" AllowedState="HalfExpanded"
                IsModal="True" ShowGrabber="True" AllowDismiss="True"
                BackgroundColor="{StaticResource Surface}" CornerRadius="28"
                StateChanged="OnStateChanged">
    <VerticalStackLayout>
        <Label Text="{Binding Message, Source={x:Reference self}}"
               StyleClass="Title.Medium"
               TextColor="{StaticResource OnSurface}"
               HorizontalTextAlignment="Center"
               Margin="24,20" />
        <BoxView Style="{StaticResource Divider}" />
        <dx:DXButton Content="{Binding ActionText, Source={x:Reference self}}"
                     Style="{StaticResource BottomSheetDestructiveAction}"
                     Command="{Binding ActionCommand, Source={x:Reference self}}" />
        <BoxView Style="{StaticResource Divider}" />
        <dx:DXButton Content="Cancel"
                     Style="{StaticResource BottomSheetCancelAction}"
                     Command="{Binding DismissCommand, Source={x:Reference self}}" />
    </VerticalStackLayout>
</dx:BottomSheet>
```

**StateChanged sync (code-behind):**
```csharp
private void OnStateChanged(object sender, ValueChangedEventArgs<BottomSheetState> e)
{
    // Sync dismissal back to ViewModel when user swipes down
    SheetState = e.NewValue;
}
```

**⚠️ Implementation risk:** Verify that `dx:BottomSheet` wrapped inside a `ContentView` that lives inside a page `Grid` correctly positions itself as a modal overlay. DX BottomSheet is modal-overlay positioned relative to its visual tree parent. If wrapping causes z-order issues, the fallback is to keep ConfirmSheet as a documented inline template (not a component) and generate it consistently from `crud-pages.md`.

**Usage in VenuesPage:**
```xml
<sheets:ConfirmSheet
    SheetState="{Binding ConfirmSheetState, Mode=TwoWay}"
    Message="{Binding ConfirmMessage}"
    ActionText="{Binding ConfirmActionText}"
    ActionCommand="{Binding ConfirmActionCommand}"
    DismissCommand="{Binding DismissConfirmCommand}" />
```

---

### Part 5 — `ListItemLeadingAvatar` → `ListItemLeadingMonogram` rename

**MD3 basis:** MD3 distinguishes:
- **Monogram**: initials in a circle (no photo) — what our component renders
- **Avatar**: circular photo/image of a person or entity

**Action:** Rename file, class, and all references.
**Prerequisite:** Verify no usages in non-placeholder pages (placeholder page usages are safe to rename since those pages will be rebuilt anyway).

---

### Part 6 — Rules files updates

| File | Updates needed |
|---|---|
| `m3-components.md` | Add: complete MD3 type scale table (all 15 entries with sp + weight); `EmptyState` component anatomy; NavDrawer section header correct typography (Label Medium 12sp); `Label.Small` weight correction note (Medium, not Regular) |
| `devexpress-patterns.md` | Add: all 9 new named styles with usage examples; `SkeletonBone` usage; `ListItemLeadingMonogram` rename note; canonicalize `BoxView.Color` vs `BackgroundColor` in Divider section |
| `crud-pages.md` | Update CRUD list page template to use `EmptyState` and `ConfirmSheet` components; update shimmer section to use `SkeletonBone` style; update ViewModel checklist note for `ConfirmSheet` BindableProperties |

---

## Implementation order (for the writing-plans phase)

1. `MaterialStyles.xaml` — type scale additions + named styles (no risk, pure additions)
2. Apply styles inside existing components (SmallAppBar, SearchAppBar, FloatingToolbar, ListItem)
3. `ListItemLeadingMonogram` rename
4. `VenueFormPage` cleanup (remove redundant TextEdit props)
5. `AppShell` inline style cleanup
6. `EmptyState` component (new)
7. `ConfirmSheet` component (new — verify DX BottomSheet overlay behavior)
8. Apply `EmptyState` and `ConfirmSheet` in `VenuesPage`
9. Rules files updates (m3-components.md, devexpress-patterns.md, crud-pages.md)

Build after every step. Fix errors before proceeding.
