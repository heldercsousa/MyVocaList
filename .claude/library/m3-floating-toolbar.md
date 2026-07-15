# M3 Components — M3 Floating Toolbar

> Section file split from `m3-components.md` on 2026-07-14 (token-scoped reads). Index + never-miss rules: `m3-components.md`.

## M3 Floating Toolbar

> **M3 Expressive component.** The floating toolbar (and "vibrant" color scheme) are M3 Expressive features — not in standard M3. "Vibrant" is the official spec term (confirmed via m3.material.io 2026-04-04).
>
> **Color design decision.** Official M3 Expressive vibrant uses `PrimaryContainer` bg + `OnPrimaryContainer` icons. This project uses `SecondaryContainer` bg + `OnSecondaryContainer` icons — a deliberate choice for better contrast in the indigo dark theme. The selected-state contrast (Primary bg + OnPrimary icon) applies against SecondaryContainer, not PrimaryContainer.
>
> **Height.** Confirmed from official MD3 measurements: floating toolbar = **64dp**. FAB = 56dp. Default padding = 8dp. Slot spacing = 4dp. FAB gap = 8dp. Screen margins: 16dp minimum left/right, 16dp above OS bottom bar (40dp total including OS bar). Confirmed via official spec images 2026-04-06.

### Spec

| Property | Value |
|---|---|
| Height | **64dp** (per official MD3 Expressive spec) |
| FAB height | 56dp |
| Width | Wrap content (auto-sizes to populated slots) |
| Padding (H, outside icon slots) | **8dp** |
| Slot spacing (between buttons) | **4dp** |
| FAB gap (toolbar ↔ FAB) | **8dp** (HorizontalStackLayout Spacing) |
| Background | **SecondaryContainer** (project vibrant — see note above) |
| Shape | CornerRadius = **32dp** (half of 64dp for full pill) |
| Screen margin left/right | **16dp minimum** |
| Screen margin bottom | **16dp** above OS bottom bar (40dp total including ~24dp OS nav bar) |
| Elevation | Omitted — tint conveys elevation in dark mode |
| Icon button tap zone | **48×48dp** (confirmed by spec: "minimum 48×48dp target area") |
| Icon size | 24dp (DXButton default) |
| Icon color (rest) | **OnSecondaryContainer** |
| Icon bg (selected) | **Primary** |
| Icon color (selected) | **OnPrimary** |
| Max slots | 5 |
| Scroll animation | NOT used — persistent by design (valid per M3 spec; animation adds threading risk) |
| Position | See FAB Coexistence below |

### Color tokens (dark mode — project vibrant)
| Token | Hex |
|---|---|
| SecondaryContainer | `#3F4566` |
| OnSecondaryContainer | `#AEB3DA` |
| Primary | `#BAC3FF` |
| OnPrimary | `#15267B` |

### When to use
- 2–5 page-specific actions (edit, delete, format, share, select-all)
- Use SmallAppBar trailing Action1–3 slots when ≤ 3 actions suffice
- Hide toolbar when contextual action bar (multi-select Shell.TitleView) is active

### When NOT to use
- Single action → use FAB instead
- Navigation actions → use Shell flyout
- ≤ 3 actions already covered by SmallAppBar trailing slots

**Full-screen CRUD forms (not list pages):** use a native Shell `ToolbarItem` for Save, not `SmallAppBar`. Full pattern + rationale: `crud-pages.md § Save/Cancel placement (full-screen forms)`.

### Anatomy
```
[DXBorder: pill, 64dp tall, CornerRadius=32, SecondaryContainer (vibrant), Padding="8,0"]
  └── HorizontalStackLayout (Spacing="4")
        ├── Slot 1: DXButton 48×48 (hidden if ActionNIcon empty)
        ├── Slot 2: DXButton 48×48
        ├── Slot 3: DXButton 48×48
        ├── Slot 4: DXButton 48×48
        └── Slot 5: DXButton 48×48
```

### Component
File: `MyVocaList/UI/Components/Toolbars/FloatingToolbar.xaml` + `.xaml.cs`
Namespace: `xmlns:toolbars="clr-namespace:MyVocaList.UI.Components.Toolbars"`

BindableProperties per slot (N = 1–5):
- `ActionNIcon` (string) — slot hidden when null/empty
- `ActionNCommand` (ICommand)
- `ActionNDescription` (string) — SemanticProperties.Description for TalkBack
- `ActionNIsSelected` (bool) — applies `Primary` bg + `OnPrimary` icon when selected (contrasts against vibrant bg)

### Page integration pattern

Toolbar and FAB are placed in a single `HorizontalStackLayout`, centered at the bottom.
FAB is to the RIGHT of the toolbar. `VerticalOptions=Center` on both aligns them vertically
(toolbar 64dp > FAB 56dp — center alignment keeps them visually level).

```xml
<!-- Root: single-cell Grid (toolbar+FAB row overlays content) -->
<Grid>
    <!-- Content list — bottom margin clears the combined bar -->
    <!-- Formula: max(toolbar 64dp, FAB 56dp) + 16dp margin + 8dp breathing = 88dp -->
    <dxcv:DXCollectionView Margin="0,0,0,88" ... />

    <!-- Combined toolbar + FAB row — centered, 16dp above safe area bottom -->
    <HorizontalStackLayout HorizontalOptions="Center"
                           VerticalOptions="End"
                           Margin="0,0,0,16"
                           Spacing="8">
        <toolbars:FloatingToolbar
            VerticalOptions="Center"
            Action1Icon="edit_outlined"
            Action1Command="{Binding EditCommand}"
            Action1Description="Edit"
            Action2Icon="delete_outlined"
            Action2Command="{Binding DeleteCommand}"
            Action2Description="Delete" />
        <dx:DXButton Style="{StaticResource Fab}"
                     Icon="add_outlined"
                     VerticalOptions="Center"
                     Command="{Binding AddCommand}" />
    </HorizontalStackLayout>
</Grid>
```

**Never** use independent overlays (FAB at bottom-right with Margin formula, toolbar separately centered). That pattern is retired.

### FAB coexistence
FAB is placed to the RIGHT of `FloatingToolbar` inside a shared `HorizontalStackLayout`.
The whole unit is `HorizontalOptions=Center`, not independently positioned.
`DXCollectionView.Margin="0,0,0,88"` — max(toolbar 64dp, FAB 56dp) + 16dp margin + 8dp breathing.

### Accessibility
- Every `ActionNDescription` is mandatory — TalkBack reads it as the button label
- Touch targets: 48×48dp (DXButton WidthRequest/HeightRequest)
- Focus order: left-to-right (HorizontalStackLayout natural order)

### Scroll animation — deliberately omitted
`DXCollectionView.CollectionViewScrolled` fires every frame. Multiple overlapping
`TranslateTo` calls create animation jitter. Debounce via `CancellationTokenSource` adds
race condition risk between scroll callbacks and animation state. For a queue management
admin app the UX benefit is marginal. Persistent toolbar is valid per MD3 spec.

### Global using
Add `MyVocaList.UI.Components.Toolbars` to `GlobalUsings.cs` **only when 2+ pages use it**.
Reference the namespace directly per-page until then.

### No DX/MAUI built-in
DevExpress MAUI has no Toolbar component. .NET MAUI `ToolbarItem` adds to Shell top bar only.
FloatingToolbar is always a custom `ContentView` — same approach as AppBars.

---
