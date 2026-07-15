# M3 Components — M3 Lists — list item component

> Section file split from `m3-components.md` on 2026-07-14 (token-scoped reads). Index + never-miss rules: `m3-components.md`.

## M3 Lists — list item component

### Official M3 terminology
- Component: **Lists** (container = `DXCollectionView`, no wrapper needed)
- Row: **list item** → `ListItem` component
- Anatomy slots: **Container**, **Headline** (required), **Overline text** (optional),
  **Supporting text** (optional), **Leading element** (optional), **Trailing element** (optional)

### Set variants (determined by slot population)

| Set | Content | Height | Leading/Trailing alignment |
|---|---|---|---|
| 1-line | Headline only | ≈56dp | Center |
| 2-line | Headline + Supporting text (1 line), or Overline + Headline | ≈72dp | Center |
| 3-line | Headline + Supporting text (2 lines) | ≈88dp | **Top (8dp from top edge)** |

3-line rule: set `SupportingMaxLines="2"` on `ListItem` — leading/trailing/text column all top-align.

### Typography tokens

| Slot | M3 style | sp | Family | Color |
|---|---|---|---|---|
| Overline text | labelSmall | 11 | RobotoRegular | OnSurfaceVariant |
| Headline | bodyLarge | 16 | RobotoRegular | OnSurface |
| Supporting text | bodyMedium | 14 | RobotoRegular | OnSurfaceVariant |

### Interactive vs Non-interactive

- `IsInteractive="True"` (default): participates in DXCollectionView tap/ripple, keyboard focus
- `IsInteractive="False"`: `InputTransparent=True`, no state layer, display-only
  → Use for: section headers embedded in list, info-only rows, category labels

### Single-action vs Multi-action lists

- **Single-action**: entire row is one tap target → `DXCollectionView.Tap` event handles it
- **Multi-action**: row + independently tappable trailing element
  → Place `DXButton` (with its own `Command`) in `TrailingContent`
  → In MAUI, `InputTransparent=False` on a child element intercepts touch before DXCollectionView
  → No special component change — same `ListItem`

### Text-only selection — checkbox placement rule (M3)

> "Primary actions go LEFT. Secondary actions go RIGHT."

| Item type | Selection control slot | Reason |
|---|---|---|
| Text-only (no leading/trailing) | `TrailingContent` (RIGHT) | MD3 baseline spec: "With text and trailing checkbox" — trailing is the default selection control slot |
| With leading element (icon/avatar/image), no trailing action | `TrailingContent` (RIGHT) | Leading slot occupied by element; checkbox stays trailing — MD3 spec: "With leading icon and trailing checkbox" |
| With trailing action button (multi-action row) | `LeadingContent` (LEFT) | Trailing slot is taken by the independent action button; checkbox moves left — MD3 multi-action pattern. The leading icon is dropped to avoid crowding. |

**Multi-action row layout (e.g. Artist row with Catalog button):**
```xml
<lists:ListItem.LeadingContent>
    <!-- Checkbox is the ONLY leading element — person icon removed to avoid crowding -->
    <dx:CheckEdit IsChecked="False" InputTransparent="True" VerticalOptions="Center" />
</lists:ListItem.LeadingContent>
<lists:ListItem.TrailingContent>
    <!-- Catalog navigation button gets its own independent touch target -->
    <dx:DXButton Style="{StaticResource IconButton}"
                 Icon="queue_music_outlined"
                 InputTransparent="False" />
</lists:ListItem.TrailingContent>
```

`IsSelected=true` → container `BackgroundColor=SecondaryContainer` (applies regardless).

### Leading element presets

| Component | Size | Shape |
|---|---|---|
| `ListItemLeadingIcon` | 24dp icon / 40dp area | None |
| `ListItemLeadingAvatar` | 40dp circle | CornerRadius=20 |
| `ListItemLeadingImage` | 56dp square | CornerRadius=4 |

Namespace: `xmlns:lists="clr-namespace:MyVocaList.UI.Components.Lists"`

### Container padding and spacing

- Container: `Padding="16,0,24,0"` (16dp start, 24dp end)
- Leading slot: `Margin="0,0,16,0"` right-gap to text (invisible = zero space)
- Trailing slot: `Margin="8,0,0,0"` left-gap from text
- `MinimumHeightRequest="56"` on container Grid
- `ColumnSpacing="0"` (spacing managed via Margin on slots)

### Known gotchas
- `ColumnSpacing` applies even to invisible Auto columns → use `Margin` on slots instead
- 3-line `VerticalOptions=Start` must also have `Margin.Top=8` to match M3 8dp offset
- `LeadingContent` / `TrailingContent` are `View` BPs — set via XAML child element syntax:
  ```xml
  <lists:ListItem.LeadingContent>
      <lists:ListItemLeadingIcon Icon="person_outlined" />
  </lists:ListItem.LeadingContent>
  ```
- `IsSelected` drives row bg only — consumer must also update `CheckEdit.IsChecked` separately
- For multi-action trailing: bind via `Source={x:Reference page}` (compiled binding issue in DataTemplate)

---
