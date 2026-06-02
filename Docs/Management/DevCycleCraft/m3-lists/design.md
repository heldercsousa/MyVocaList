# M3 Lists — Design Spec
Date: 2026-03-11
Updated: 2026-03-11 (final — 3-line alignment, text-only, interactive/non-interactive, single/multi-action, selection placement)

Sources: https://m3.material.io/components/lists/overview, /specs, /guidelines, /accessibility;
secondary: https://www.composables.com/material3/listitem, https://m1.material.io/components/lists-controls.html

---

## Official M3 Terminology

| M3 term | Description |
|---|---|
| **Lists** | Continuous, vertical index of list items. Container = `DXCollectionView`. |
| **list item** | A single row. Contains the container and one or more anatomy slots. |
| **Container** | Background surface of a list item. Default: `Surface`. |
| **Headline** | Primary text — **required**. |
| **Overline text** | Small text rendered *above* the headline — optional. |
| **Supporting text** | Secondary text rendered *below* the headline — optional. |
| **Leading element** | Start-side slot (left in LTR): icon, avatar, image — optional. |
| **Trailing element** | End-side slot (right in LTR): icon, metadata text, badge, checkbox, radio, switch — optional. |

---

## Set Variants

Three sets, determined implicitly by slot population and `SupportingMaxLines`:

| Set | Content | Height | Leading / Trailing alignment |
|---|---|---|---|
| **1-line** | Headline only | ≈56dp | **Center** |
| **2-line** | Headline + Supporting text (1 line), OR Overline + Headline | ≈72dp | **Center** |
| **3-line** | Headline + Supporting text (2 lines) | ≈88dp | **Top — 8dp from top edge** |

### 3-line Vertical Alignment (M3 spec rule)

For 3-line items all slots top-align. For 1-line and 2-line all slots center-align.

| Slot | 1-line / 2-line | 3-line |
|---|---|---|
| Leading element | `VerticalOptions=Center` | `VerticalOptions=Start`, `Margin.Top=8` |
| Text column | `VerticalOptions=Center` | `VerticalOptions=Start`, `Margin.Top=8` |
| Trailing element | `VerticalOptions=Center` | `VerticalOptions=Start`, `Margin.Top=8` |

Controlled in `ListItem` code-behind via `UpdateSlotAlignment()` triggered by `SupportingMaxLines` BP change.

---

## List Types — Action Model

### Single-action list
Each row has **one** primary tap action. The entire row is the tap target.
- In `DXCollectionView`: handled via `Tap` event or `CollectionViewGestureEventArgs`.
- Trailing element (if present) is **visual metadata**, not separately tappable.
- Most common pattern.

### Multi-action list
Each row has **two or more** independently tappable areas.
- Example: row tap → navigate to detail; trailing icon → contextual action.
- Implementation: place a `DXButton` (with its own `Command`) inside `TrailingContent`. In MAUI, a child element with `InputTransparent=False` intercepts touch before the parent collection view. No special component change needed.
- Use sparingly — multiple tap targets in a small space risk accidental taps (M3 guideline).

> **No new component** for single vs multi-action. Same `ListItem` + consumer wires tap targets.

---

## Interactive vs Non-Interactive ListLines (M3)

### Interactive (default — `IsInteractive=true`)
- Shows state layer: hover 8%, pressed 12%, focused 12% (`OnSurface` overlay).
- Receives keyboard focus; participates in accessibility focus order.
- In `DXCollectionView`: `UseRippleEffect=True` on the collection handles state layer.
- `InputTransparent=False` on `ListItem`.

### Non-interactive (`IsInteractive=false`)
- No state layer. No keyboard focus. No tap response.
- Used for: section headers embedded in list, info-only/display rows, category labels.
- `InputTransparent=True` on `ListItem`.

---

## Text-only ListLines and Selection Placement

### Text-only definition
No `LeadingContent` AND no `TrailingContent`. Only text slots (Headline, Overline, Supporting text).

### M3 selection placement rule (M3 guideline)

> "States and primary actions are placed on the LEFT side. Secondary actions and info are placed on the RIGHT side."

| List item type | Selection control placement | Rationale |
|---|---|---|
| **Text-only** | **Leading (LEFT)** | Selection is the primary action; no other leading content conflicts |
| **With leading element** (icon/avatar/image) | **Trailing (RIGHT)** | Leading slot already occupied; don't stack icon + checkbox on same side |

Selection visual for the row: `IsSelected=true` → `BackgroundColor=SecondaryContainer`.
The checkbox/radio itself is placed by the consumer in `LeadingContent` or `TrailingContent` (DevExpress `CheckEdit`).

> The `ListItem` does NOT auto-inject a selection control. The consumer decides placement per the above rule and wires `CheckEdit` into the appropriate slot.

### Excluded per project decision
- Collapse/expand behavior on list items — **excluded**.

---

## Component Decomposition

### DevExpress built-ins (no new wrapper)
| DX component | M3 role |
|---|---|
| `DXCollectionView` | Lists container (scroll, swipe, multi-select, load-more) |
| `SwipeContainer` / `SwipeContainerItem` | Leave-behind swipe actions on list items |
| `CheckEdit` | Checkbox control (used inside `LeadingContent` or `TrailingContent`) |

### New components — `UI/Components/Lists/`

```
ListItem.xaml / .cs              — M3 list item: all sets, all alignment rules
ListItemLeadingIcon.xaml / .cs   — 24dp icon in 40dp area (OnSurfaceVariant)
ListItemLeadingAvatar.xaml / .cs — 40dp circle: initials on SecondaryContainer
ListItemLeadingImage.xaml / .cs  — 56dp square image, CornerRadius=4
```

### Decomposition rationale

**One `ListItem` for all sets**: grid skeleton `Auto | * | Auto` is identical for all 3 sets. Alignment changes are handled in code-behind via `UpdateSlotAlignment()`. No subclassing needed.

**Leading presets**: Icon (24dp/40dp area), Avatar (40dp circle), Image (56dp square) differ structurally — separate components standardize them per M3.

**No trailing presets**: Trailing is a generic `View` slot. Common trailing patterns (trailing icon, metadata text, `CheckEdit`, `Switch`) are documented in rules but don't warrant separate components.

---

## `ListItem` — Full Spec

### Grid layout
```
Grid  ColumnDefinitions="Auto,*,Auto"
      ColumnSpacing="0"
      MinimumHeightRequest="56"
      Padding="16,0,24,0"
      x:Name="container"
      BackgroundColor=Surface (→ SecondaryContainer when IsSelected=true)

  Col 0 (Auto)  ContentView x:Name="leadingSlot"
                  Content = LeadingContent
                  IsVisible = HasLeadingContent
                  Margin = "0,0,16,0"      (right-gap to text; zero when invisible)
                  VerticalOptions = Center | Start+8dp  (set by UpdateSlotAlignment)

  Col 1 (*)     VerticalStackLayout x:Name="textColumn"
                  Spacing="2"
                  VerticalOptions = Center | Start+8dp
                  Children:
                    overlineLabel   IsVisible=HasOverline      labelSmall 11sp OnSurfaceVariant
                    headlineLabel   MaxLines=1                  bodyLarge  16sp OnSurface
                    supportingLabel IsVisible=HasSupportingText bodyMedium 14sp OnSurfaceVariant
                                    MaxLines=SupportingMaxLines

  Col 2 (Auto)  ContentView x:Name="trailingSlot"
                  Content = TrailingContent
                  IsVisible = HasTrailingContent
                  Margin = "8,0,0,0"       (left-gap from text; zero when invisible)
                  VerticalOptions = Center | Start+8dp
```

### BindableProperties

| Property | Type | Required | Default | Effect |
|---|---|---|---|---|
| `Headline` | string | **YES** | `""` | bodyLarge 16sp OnSurface |
| `Overline` | string | No | `""` | labelSmall 11sp OnSurfaceVariant; hidden when empty |
| `SupportingText` | string | No | `""` | bodyMedium 14sp OnSurfaceVariant; hidden when empty |
| `SupportingMaxLines` | int | No | `1` | 2 = 3-line set; triggers top-alignment |
| `LeadingContent` | View | No | `null` | Placed in Col 0 |
| `TrailingContent` | View | No | `null` | Placed in Col 2 |
| `IsSelected` | bool | No | `false` | Container bg: `SecondaryContainer` when true |
| `IsInteractive` | bool | No | `true` | `false` → `InputTransparent=True` |

### Computed properties
- `HasOverline` → `!string.IsNullOrEmpty(Overline)`
- `HasSupportingText` → `!string.IsNullOrEmpty(SupportingText)`
- `HasLeadingContent` → `LeadingContent != null`
- `HasTrailingContent` → `TrailingContent != null`
- `IsThreeLine` → `SupportingMaxLines >= 2`

### Code-behind helpers
```csharp
private void UpdateSlotAlignment()
{
    bool top = SupportingMaxLines >= 2;
    leadingSlot.VerticalOptions   = top ? LayoutOptions.Start : LayoutOptions.Center;
    leadingSlot.Margin            = top ? new Thickness(0, 8, 16, 0) : new Thickness(0, 0, 16, 0);
    textColumn.VerticalOptions    = top ? LayoutOptions.Start : LayoutOptions.Center;
    textColumn.Margin             = top ? new Thickness(0, 8, 0,  0) : Thickness.Zero;
    trailingSlot.VerticalOptions  = top ? LayoutOptions.Start : LayoutOptions.Center;
    trailingSlot.Margin           = top ? new Thickness(8, 8, 0,  0) : new Thickness(8, 0, 0, 0);
}

private void UpdateContainerColor()
{
    var key = IsSelected ? "SecondaryContainer" : "Surface";
    if (Application.Current?.Resources.TryGetValue(key, out var c) == true)
        container.BackgroundColor = (Color)c;
}

private void UpdateInteractivity() => InputTransparent = !IsInteractive;
```

### Typography tokens

| Slot | M3 style | sp | Family | Color |
|---|---|---|---|---|
| Overline text | labelSmall | 11 | RobotoRegular | OnSurfaceVariant |
| Headline | bodyLarge | 16 | RobotoRegular | OnSurface |
| Supporting text | bodyMedium | 14 | RobotoRegular | OnSurfaceVariant |

---

## Leading Preset Sub-components

### `ListItemLeadingIcon`
```
ContentView (40×40dp, no background)
  DXButton  Icon="{Binding Icon}"
            IconColor="{Binding IconColor}"  default OnSurfaceVariant
            IconWidth=24  IconHeight=24
            WidthRequest=40  HeightRequest=40
            BackgroundColor=Transparent
            InputTransparent=True
            HorizontalContentAlignment=Center
```
BPs: `Icon` (string), `IconColor` (Color, default `OnSurfaceVariant`)

### `ListItemLeadingAvatar`
```
DXBorder  WidthRequest=40  HeightRequest=40  CornerRadius=20
          BackgroundColor="{Binding AvatarColor}"  default SecondaryContainer
  Label   Text="{Binding Initials}"
          FontFamily=RobotoMedium  FontSize=14
          TextColor="{Binding InitialsColor}"  default OnSecondaryContainer
          HorizontalOptions=Center  VerticalOptions=Center
          HorizontalTextAlignment=Center
```
BPs: `Initials` (string), `AvatarColor` (Color, default SecondaryContainer), `InitialsColor` (Color, default OnSecondaryContainer)

### `ListItemLeadingImage`
```
Border  StrokeShape="RoundRectangle 4"  Stroke=Transparent
        WidthRequest=56  HeightRequest=56
        IsClippedToBounds=True
  Image Source="{Binding ImageSource}"
        Aspect="{Binding Aspect}"  default AspectFill
        WidthRequest=56  HeightRequest=56
```
BPs: `ImageSource` (ImageSource), `Aspect` (Aspect, default AspectFill)

---

## Usage Examples

### 1-line text-only, single-action (navigation row)
```xml
<lists:ListItem Headline="Preferences" />
```

### 1-line text-only with selection (checkbox on LEFT — text-only rule)
```xml
<lists:ListItem Headline="Rock"
                IsSelected="{Binding IsSelected}"
                IsInteractive="True">
    <lists:ListItem.LeadingContent>
        <dx:CheckEdit IsChecked="{Binding IsSelected, Mode=OneWay}"
                      CheckedCheckBoxColor="{dx:ThemeColor Primary}"
                      InputTransparent="True"
                      VerticalOptions="Center" />
    </lists:ListItem.LeadingContent>
</lists:ListItem>
```

### 2-line with icon leading, single-action
```xml
<lists:ListItem Headline="John Doe"
                SupportingText="Singer · 3 events">
    <lists:ListItem.LeadingContent>
        <lists:ListItemLeadingIcon Icon="person_outlined" />
    </lists:ListItem.LeadingContent>
</lists:ListItem>
```

### 2-line with avatar + selection checkbox on RIGHT (has leading — non-text-only rule)
```xml
<lists:ListItem Headline="John Doe"
                SupportingText="Singer · 3 events"
                IsSelected="{Binding IsSelected}">
    <lists:ListItem.LeadingContent>
        <lists:ListItemLeadingAvatar Initials="{Binding Initials}" />
    </lists:ListItem.LeadingContent>
    <lists:ListItem.TrailingContent>
        <dx:CheckEdit IsChecked="{Binding IsSelected, Mode=OneWay}"
                      CheckedCheckBoxColor="{dx:ThemeColor Primary}"
                      InputTransparent="True"
                      VerticalOptions="Center" />
    </lists:ListItem.TrailingContent>
</lists:ListItem>
```

### 3-line with overline, leading avatar — top-aligned (set SupportingMaxLines=2)
```xml
<lists:ListItem Headline="Summer Nights"
                Overline="Bandokê"
                SupportingText="4 singers in queue · Est. 12 min remaining"
                SupportingMaxLines="2">
    <lists:ListItem.LeadingContent>
        <lists:ListItemLeadingAvatar Initials="SN" />
    </lists:ListItem.LeadingContent>
    <lists:ListItem.TrailingContent>
        <dx:DXButton Icon="chevron_right_outlined"
                     BackgroundColor="Transparent"
                     IconColor="{StaticResource OnSurfaceVariant}"
                     WidthRequest="24" HeightRequest="24"
                     InputTransparent="True" />
    </lists:ListItem.TrailingContent>
</lists:ListItem>
```

### Multi-action: row tap navigates + trailing icon opens menu
```xml
<!-- In DXCollectionView.ItemTemplate — DXCollectionView.Tap handles row navigation -->
<lists:ListItem Headline="{Binding Name}" SupportingText="{Binding Role}">
    <lists:ListItem.LeadingContent>
        <lists:ListItemLeadingAvatar Initials="{Binding Initials}" />
    </lists:ListItem.LeadingContent>
    <lists:ListItem.TrailingContent>
        <!-- DXButton.InputTransparent=False intercepts touch before DXCollectionView -->
        <dx:DXButton Icon="more_vert_outlined"
                     BackgroundColor="Transparent"
                     IconColor="{StaticResource OnSurfaceVariant}"
                     WidthRequest="48" HeightRequest="48"
                     CornerRadius="24"
                     Command="{Binding BindingContext.OpenMenuCommand, Source={x:Reference page}}"
                     CommandParameter="{Binding}" />
    </lists:ListItem.TrailingContent>
</lists:ListItem>
```

### Non-interactive (section header in list)
```xml
<lists:ListItem Headline="Round 2 — Queue"
                IsInteractive="False" />
```

### In DXCollectionView
```xml
<dxcv:DXCollectionView UseRippleEffect="True"
                       ItemSeparatorThickness="0"
                       Tap="OnItemTapped">
    <dxcv:DXCollectionView.ItemTemplate>
        <DataTemplate x:DataType="dto:PersonDto">
            <lists:ListItem Headline="{Binding Name}"
                            SupportingText="{Binding Role}">
                <lists:ListItem.LeadingContent>
                    <lists:ListItemLeadingAvatar Initials="{Binding Initials}" />
                </lists:ListItem.LeadingContent>
            </lists:ListItem>
        </DataTemplate>
    </dxcv:DXCollectionView.ItemTemplate>
</dxcv:DXCollectionView>
```

---

## Excluded per Project Decision
- Collapse/expand behavior on list items
- Menu list items (contextual/nested menus) — separate task
- `List` wrapper around `DXCollectionView` — DX covers it
- Trailing preset sub-components — generic `View` slot is sufficient; patterns in rules
- Video thumbnail leading element — deferred

---

## Files to Create / Modify

| File | Action |
|---|---|
| `UI/Components/Lists/ListItem.xaml` | Create |
| `UI/Components/Lists/ListItem.xaml.cs` | Create |
| `UI/Components/Lists/ListItemLeadingIcon.xaml` | Create |
| `UI/Components/Lists/ListItemLeadingIcon.xaml.cs` | Create |
| `UI/Components/Lists/ListItemLeadingAvatar.xaml` | Create |
| `UI/Components/Lists/ListItemLeadingAvatar.xaml.cs` | Create |
| `UI/Components/Lists/ListItemLeadingImage.xaml` | Create |
| `UI/Components/Lists/ListItemLeadingImage.xaml.cs` | Create |
| `.claude/rules/m3-components.md` | Update — add Lists section |
