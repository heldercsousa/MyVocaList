# M3 Lists Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build four M3-compliant List sub-components (`ListItemLeadingIcon`, `ListItemLeadingAvatar`, `ListItemLeadingImage`, `ListItem`) that compose into M3-spec list rows for use inside `DXCollectionView`.

**Architecture:** `ListItem` is a single `ContentView` (3-column Grid) handling all three M3 sets (1-line/2-line/3-line) via slot BindableProperties; code-behind switches vertical alignment dynamically for 3-line. Three leading-element presets (`Icon`, `Avatar`, `Image`) are small dedicated `ContentView` components. No intermediate base class — all inherit `ContentView` directly.

**Tech Stack:** .NET MAUI 10 · C# 13 · DevExpress MAUI v24.2+ (DXButton, DXBorder) · MAUI Border (for image clipping)

**Spec:** `docs/superpowers/specs/2026-03-11-m3-lists-design.md`

---

## Chunk 1: Rules Update + Leading Presets

### Task 1: Update m3-components.md with M3 Lists section

**Files:**
- Modify: `.claude/rules/m3-components.md`

- [ ] **Step 1: Append M3 Lists section to the rules file**

Add the following section at the end of `.claude/rules/m3-components.md`:

```markdown
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
| Text-only (no leading/trailing) | `LeadingContent` (LEFT) | Selection is primary action |
| With leading element (icon/avatar/image) | `TrailingContent` (RIGHT) | Leading slot occupied; don't stack |

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
```

- [ ] **Step 2: Build to confirm rules file change does not affect compilation**

```
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add .claude/rules/m3-components.md
git commit -m "docs(rules): add M3 Lists section — list item anatomy, sets, alignment, selection placement"
```

---

### Task 2: Create `ListItemLeadingIcon`

**Files:**
- Create: `MyVocaList/UI/Components/Lists/ListItemLeadingIcon.xaml`
- Create: `MyVocaList/UI/Components/Lists/ListItemLeadingIcon.xaml.cs`

- [ ] **Step 1: Create the XAML**

`MyVocaList/UI/Components/Lists/ListItemLeadingIcon.xaml`:
```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentView
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:dx="http://schemas.devexpress.com/maui"
    x:Class="MyVocaList.UI.Components.Lists.ListItemLeadingIcon"
    x:Name="self">

    <!-- M3: leading icon — 24dp icon in 40dp area, OnSurfaceVariant -->
    <dx:DXButton Icon="{Binding Icon, Source={x:Reference self}}"
                 IconColor="{Binding IconColor, Source={x:Reference self}}"
                 IconWidth="24"
                 IconHeight="24"
                 BackgroundColor="Transparent"
                 WidthRequest="40"
                 HeightRequest="40"
                 CornerRadius="20"
                 HorizontalContentAlignment="Center"
                 InputTransparent="True"
                 SemanticProperties.IsHeadingLevel="None" />
</ContentView>
```

- [ ] **Step 2: Create the code-behind**

`MyVocaList/UI/Components/Lists/ListItemLeadingIcon.xaml.cs`:
```csharp
namespace MyVocaList.UI.Components.Lists;

public partial class ListItemLeadingIcon : ContentView
{
    // ── Icon ──────────────────────────────────────────────────────────────

    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(ListItemLeadingIcon), string.Empty);

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    // ── IconColor ─────────────────────────────────────────────────────────

    public static readonly BindableProperty IconColorProperty =
        BindableProperty.Create(nameof(IconColor), typeof(Color), typeof(ListItemLeadingIcon), null,
            defaultValueCreator: _ => GetDefaultIconColor());

    public Color IconColor
    {
        get => (Color)GetValue(IconColorProperty);
        set => SetValue(IconColorProperty, value);
    }

    // ── Constructor ────────────────────────────────────────────────────────

    public ListItemLeadingIcon()
    {
        InitializeComponent();
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static Color GetDefaultIconColor()
    {
        if (Application.Current?.Resources.TryGetValue("OnSurfaceVariant", out var c) == true)
            return (Color)c;
        return Colors.Gray;
    }
}
```

- [ ] **Step 3: Build**

```
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add MyVocaList/UI/Components/Lists/
git commit -m "feat(lists): add ListItemLeadingIcon — M3 24dp icon in 40dp area"
```

---

### Task 3: Create `ListItemLeadingAvatar`

**Files:**
- Create: `MyVocaList/UI/Components/Lists/ListItemLeadingAvatar.xaml`
- Create: `MyVocaList/UI/Components/Lists/ListItemLeadingAvatar.xaml.cs`

- [ ] **Step 1: Create the XAML**

`MyVocaList/UI/Components/Lists/ListItemLeadingAvatar.xaml`:
```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentView
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:dx="http://schemas.devexpress.com/maui"
    x:Class="MyVocaList.UI.Components.Lists.ListItemLeadingAvatar"
    x:Name="self">

    <!-- M3: leading avatar — 40dp circle, SecondaryContainer bg, initials -->
    <dx:DXBorder BackgroundColor="{Binding AvatarColor, Source={x:Reference self}}"
                 CornerRadius="20"
                 WidthRequest="40"
                 HeightRequest="40"
                 HorizontalOptions="Center"
                 VerticalOptions="Center">
        <Label Text="{Binding Initials, Source={x:Reference self}}"
               FontFamily="RobotoMedium"
               FontSize="14"
               TextColor="{Binding InitialsColor, Source={x:Reference self}}"
               HorizontalOptions="Center"
               VerticalOptions="Center"
               HorizontalTextAlignment="Center"
               LineBreakMode="NoWrap"
               MaxLines="1" />
    </dx:DXBorder>
</ContentView>
```

- [ ] **Step 2: Create the code-behind**

`MyVocaList/UI/Components/Lists/ListItemLeadingAvatar.xaml.cs`:
```csharp
namespace MyVocaList.UI.Components.Lists;

public partial class ListItemLeadingAvatar : ContentView
{
    // ── Initials ──────────────────────────────────────────────────────────

    public static readonly BindableProperty InitialsProperty =
        BindableProperty.Create(nameof(Initials), typeof(string), typeof(ListItemLeadingAvatar), string.Empty);

    public string Initials
    {
        get => (string)GetValue(InitialsProperty);
        set => SetValue(InitialsProperty, value);
    }

    // ── AvatarColor ───────────────────────────────────────────────────────

    public static readonly BindableProperty AvatarColorProperty =
        BindableProperty.Create(nameof(AvatarColor), typeof(Color), typeof(ListItemLeadingAvatar), null,
            defaultValueCreator: _ => GetToken("SecondaryContainer"));

    public Color AvatarColor
    {
        get => (Color)GetValue(AvatarColorProperty);
        set => SetValue(AvatarColorProperty, value);
    }

    // ── InitialsColor ─────────────────────────────────────────────────────

    public static readonly BindableProperty InitialsColorProperty =
        BindableProperty.Create(nameof(InitialsColor), typeof(Color), typeof(ListItemLeadingAvatar), null,
            defaultValueCreator: _ => GetToken("OnSecondaryContainer"));

    public Color InitialsColor
    {
        get => (Color)GetValue(InitialsColorProperty);
        set => SetValue(InitialsColorProperty, value);
    }

    // ── Constructor ────────────────────────────────────────────────────────

    public ListItemLeadingAvatar()
    {
        InitializeComponent();
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static Color GetToken(string key)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var c) == true)
            return (Color)c;
        return Colors.Gray;
    }
}
```

- [ ] **Step 3: Build**

```
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add MyVocaList/UI/Components/Lists/
git commit -m "feat(lists): add ListItemLeadingAvatar — M3 40dp circle with initials"
```

---

### Task 4: Create `ListItemLeadingImage`

**Files:**
- Create: `MyVocaList/UI/Components/Lists/ListItemLeadingImage.xaml`
- Create: `MyVocaList/UI/Components/Lists/ListItemLeadingImage.xaml.cs`

- [ ] **Step 1: Create the XAML**

`MyVocaList/UI/Components/Lists/ListItemLeadingImage.xaml`:
```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentView
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    x:Class="MyVocaList.UI.Components.Lists.ListItemLeadingImage"
    x:Name="self">

    <!-- M3: leading image — 56dp square, CornerRadius=4, AspectFill -->
    <Border StrokeShape="RoundRectangle 4"
            Stroke="Transparent"
            WidthRequest="56"
            HeightRequest="56"
            IsClippedToBounds="True"
            HorizontalOptions="Center"
            VerticalOptions="Center">
        <Image Source="{Binding ImageSource, Source={x:Reference self}}"
               Aspect="{Binding Aspect, Source={x:Reference self}}"
               WidthRequest="56"
               HeightRequest="56" />
    </Border>
</ContentView>
```

- [ ] **Step 2: Create the code-behind**

`MyVocaList/UI/Components/Lists/ListItemLeadingImage.xaml.cs`:
```csharp
namespace MyVocaList.UI.Components.Lists;

public partial class ListItemLeadingImage : ContentView
{
    // ── ImageSource ───────────────────────────────────────────────────────

    public static readonly BindableProperty ImageSourceProperty =
        BindableProperty.Create(nameof(ImageSource), typeof(ImageSource), typeof(ListItemLeadingImage));

    public ImageSource ImageSource
    {
        get => (ImageSource)GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }

    // ── Aspect ────────────────────────────────────────────────────────────

    public static readonly BindableProperty AspectProperty =
        BindableProperty.Create(nameof(Aspect), typeof(Aspect), typeof(ListItemLeadingImage), Aspect.AspectFill);

    public Aspect Aspect
    {
        get => (Aspect)GetValue(AspectProperty);
        set => SetValue(AspectProperty, value);
    }

    // ── Constructor ────────────────────────────────────────────────────────

    public ListItemLeadingImage()
    {
        InitializeComponent();
    }
}
```

- [ ] **Step 3: Build**

```
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add MyVocaList/UI/Components/Lists/
git commit -m "feat(lists): add ListItemLeadingImage — M3 56dp square image, CornerRadius=4"
```

---

## Chunk 2: Main ListItem Component

### Task 5: Create `ListItem`

**Files:**
- Create: `MyVocaList/UI/Components/Lists/ListItem.xaml`
- Create: `MyVocaList/UI/Components/Lists/ListItem.xaml.cs`

This is the core component. It handles all 3 M3 sets (1-line, 2-line, 3-line), dynamic slot alignment, selected-state container color, and interactive/non-interactive mode.

- [ ] **Step 1: Create the XAML**

`MyVocaList/UI/Components/Lists/ListItem.xaml`:
```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentView
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    x:Class="MyVocaList.UI.Components.Lists.ListItem"
    x:Name="self">

    <!--
        M3 list item — 3-column grid:
          Col 0 (Auto): leading element slot (16dp right gap to text)
          Col 1 (*):    text column (overline + headline + supporting text)
          Col 2 (Auto): trailing element slot (8dp left gap from text)

        Container: Surface (→ SecondaryContainer when IsSelected)
        MinimumHeightRequest=56 (1-line: 56dp, 2-line: 72dp, 3-line: 88dp — driven by content)
        Padding: 16dp start, 24dp end

        Vertical alignment:
          1-line / 2-line: all slots Center
          3-line (SupportingMaxLines ≥ 2): all slots Start + 8dp top margin
          UpdateSlotAlignment() in code-behind controls this.
    -->
    <Grid x:Name="container"
          ColumnDefinitions="Auto,*,Auto"
          ColumnSpacing="0"
          MinimumHeightRequest="56"
          Padding="16,0,24,0"
          BackgroundColor="{StaticResource Surface}"
          VerticalOptions="Fill">

        <!-- Leading element slot -->
        <ContentView Grid.Column="0"
                     x:Name="leadingSlot"
                     Content="{Binding LeadingContent, Source={x:Reference self}}"
                     IsVisible="{Binding HasLeadingContent, Source={x:Reference self}}"
                     Margin="0,0,16,0"
                     VerticalOptions="Center" />

        <!-- Text column: Overline + Headline + Supporting text -->
        <VerticalStackLayout Grid.Column="1"
                             x:Name="textColumn"
                             Spacing="2"
                             VerticalOptions="Center">

            <!-- Overline text (optional) -->
            <Label x:Name="overlineLabel"
                   Text="{Binding Overline, Source={x:Reference self}}"
                   FontFamily="RobotoRegular"
                   FontSize="11"
                   TextColor="{StaticResource OnSurfaceVariant}"
                   LineBreakMode="TailTruncation"
                   MaxLines="1"
                   IsVisible="{Binding HasOverline, Source={x:Reference self}}" />

            <!-- Headline (required) -->
            <Label x:Name="headlineLabel"
                   Text="{Binding Headline, Source={x:Reference self}}"
                   FontFamily="RobotoRegular"
                   FontSize="16"
                   TextColor="{StaticResource OnSurface}"
                   LineBreakMode="TailTruncation"
                   MaxLines="1" />

            <!-- Supporting text (optional) -->
            <Label x:Name="supportingLabel"
                   Text="{Binding SupportingText, Source={x:Reference self}}"
                   FontFamily="RobotoRegular"
                   FontSize="14"
                   TextColor="{StaticResource OnSurfaceVariant}"
                   LineBreakMode="TailTruncation"
                   MaxLines="{Binding SupportingMaxLines, Source={x:Reference self}}"
                   IsVisible="{Binding HasSupportingText, Source={x:Reference self}}" />
        </VerticalStackLayout>

        <!-- Trailing element slot -->
        <ContentView Grid.Column="2"
                     x:Name="trailingSlot"
                     Content="{Binding TrailingContent, Source={x:Reference self}}"
                     IsVisible="{Binding HasTrailingContent, Source={x:Reference self}}"
                     Margin="8,0,0,0"
                     VerticalOptions="Center" />
    </Grid>
</ContentView>
```

- [ ] **Step 2: Create the code-behind**

`MyVocaList/UI/Components/Lists/ListItem.xaml.cs`:
```csharp
namespace MyVocaList.UI.Components.Lists;

public partial class ListItem : ContentView
{
    // ── Headline (required) ────────────────────────────────────────────────

    public static readonly BindableProperty HeadlineProperty =
        BindableProperty.Create(nameof(Headline), typeof(string), typeof(ListItem), string.Empty);

    public string Headline
    {
        get => (string)GetValue(HeadlineProperty);
        set => SetValue(HeadlineProperty, value);
    }

    // ── Overline text (optional) ───────────────────────────────────────────

    public static readonly BindableProperty OverlineProperty =
        BindableProperty.Create(nameof(Overline), typeof(string), typeof(ListItem), string.Empty,
            propertyChanged: (b, _, _) => ((ListItem)b).OnPropertyChanged(nameof(HasOverline)));

    public string Overline
    {
        get => (string)GetValue(OverlineProperty);
        set => SetValue(OverlineProperty, value);
    }

    public bool HasOverline => !string.IsNullOrEmpty(Overline);

    // ── Supporting text (optional) ─────────────────────────────────────────

    public static readonly BindableProperty SupportingTextProperty =
        BindableProperty.Create(nameof(SupportingText), typeof(string), typeof(ListItem), string.Empty,
            propertyChanged: (b, _, _) => ((ListItem)b).OnPropertyChanged(nameof(HasSupportingText)));

    public string SupportingText
    {
        get => (string)GetValue(SupportingTextProperty);
        set => SetValue(SupportingTextProperty, value);
    }

    public bool HasSupportingText => !string.IsNullOrEmpty(SupportingText);

    // ── SupportingMaxLines (1 = 2-line set, 2 = 3-line set) ───────────────

    public static readonly BindableProperty SupportingMaxLinesProperty =
        BindableProperty.Create(nameof(SupportingMaxLines), typeof(int), typeof(ListItem), 1,
            propertyChanged: (b, _, _) => ((ListItem)b).UpdateSlotAlignment());

    public int SupportingMaxLines
    {
        get => (int)GetValue(SupportingMaxLinesProperty);
        set => SetValue(SupportingMaxLinesProperty, value);
    }

    // ── Leading element (optional) ─────────────────────────────────────────

    public static readonly BindableProperty LeadingContentProperty =
        BindableProperty.Create(nameof(LeadingContent), typeof(View), typeof(ListItem), null,
            propertyChanged: (b, _, _) => ((ListItem)b).OnPropertyChanged(nameof(HasLeadingContent)));

    public View LeadingContent
    {
        get => (View)GetValue(LeadingContentProperty);
        set => SetValue(LeadingContentProperty, value);
    }

    public bool HasLeadingContent => LeadingContent != null;

    // ── Trailing element (optional) ────────────────────────────────────────

    public static readonly BindableProperty TrailingContentProperty =
        BindableProperty.Create(nameof(TrailingContent), typeof(View), typeof(ListItem), null,
            propertyChanged: (b, _, _) => ((ListItem)b).OnPropertyChanged(nameof(HasTrailingContent)));

    public View TrailingContent
    {
        get => (View)GetValue(TrailingContentProperty);
        set => SetValue(TrailingContentProperty, value);
    }

    public bool HasTrailingContent => TrailingContent != null;

    // ── IsSelected ─────────────────────────────────────────────────────────

    public static readonly BindableProperty IsSelectedProperty =
        BindableProperty.Create(nameof(IsSelected), typeof(bool), typeof(ListItem), false,
            propertyChanged: (b, _, _) => ((ListItem)b).UpdateContainerColor());

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    // ── IsInteractive ──────────────────────────────────────────────────────

    public static readonly BindableProperty IsInteractiveProperty =
        BindableProperty.Create(nameof(IsInteractive), typeof(bool), typeof(ListItem), true,
            propertyChanged: (b, _, _) => ((ListItem)b).UpdateInteractivity());

    public bool IsInteractive
    {
        get => (bool)GetValue(IsInteractiveProperty);
        set => SetValue(IsInteractiveProperty, value);
    }

    // ── Constructor ────────────────────────────────────────────────────────

    public ListItem()
    {
        InitializeComponent();
    }

    // ── Slot alignment (1-line/2-line = center, 3-line = top+8dp) ─────────

    private void UpdateSlotAlignment()
    {
        bool top = SupportingMaxLines >= 2;

        leadingSlot.VerticalOptions  = top ? LayoutOptions.Start : LayoutOptions.Center;
        leadingSlot.Margin           = top ? new Thickness(0, 8, 16, 0) : new Thickness(0, 0, 16, 0);

        textColumn.VerticalOptions   = top ? LayoutOptions.Start : LayoutOptions.Center;
        textColumn.Margin            = top ? new Thickness(0, 8, 0, 0) : Thickness.Zero;

        trailingSlot.VerticalOptions = top ? LayoutOptions.Start : LayoutOptions.Center;
        trailingSlot.Margin          = top ? new Thickness(8, 8, 0, 0) : new Thickness(8, 0, 0, 0);
    }

    // ── Container color (Surface ↔ SecondaryContainer) ────────────────────

    private void UpdateContainerColor()
    {
        var key = IsSelected ? "SecondaryContainer" : "Surface";
        if (Application.Current?.Resources.TryGetValue(key, out var c) == true)
            container.BackgroundColor = (Color)c;
    }

    // ── Interactivity ──────────────────────────────────────────────────────

    private void UpdateInteractivity() => InputTransparent = !IsInteractive;
}
```

- [ ] **Step 3: Build**

```
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
```
Expected: `Build succeeded. 0 Error(s)`. Fix any errors before proceeding.

- [ ] **Step 4: Commit**

```bash
git add MyVocaList/UI/Components/Lists/
git commit -m "feat(lists): add ListItem — M3 list item, all sets, interactive/non-interactive, selection state"
```

---

### Task 6: Final verification build

- [ ] **Step 1: Clean build to confirm full compilation**

```
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 2: Manual visual verification checklist**

Deploy to emulator/device and verify:

**1-line list item:**
```xml
<lists:ListItem Headline="Settings" />
```
✓ Height ≈56dp, text vertically centered, 16dp left padding

**2-line list item:**
```xml
<lists:ListItem Headline="John Doe" SupportingText="Singer · 3 events">
    <lists:ListItem.LeadingContent>
        <lists:ListItemLeadingIcon Icon="person_outlined" />
    </lists:ListItem.LeadingContent>
</lists:ListItem>
```
✓ Height ≈72dp, icon centered, 24dp icon visible in 40dp area

**3-line list item:**
```xml
<lists:ListItem Headline="Summer Nights"
                Overline="Bandokê"
                SupportingText="4 singers in queue · Est. 12 min remaining"
                SupportingMaxLines="2">
    <lists:ListItem.LeadingContent>
        <lists:ListItemLeadingAvatar Initials="SN" />
    </lists:ListItem.LeadingContent>
</lists:ListItem>
```
✓ Height ≈88dp, avatar top-aligns (8dp from top), text stack top-aligns

**IsSelected=true:**
✓ Container background switches to SecondaryContainer

**IsInteractive=false:**
✓ Tap does nothing, no ripple

**Text-only selection (checkbox LEFT):**
✓ CheckEdit in LeadingContent, row bg SecondaryContainer when IsSelected=true

**Non-text-only selection (checkbox RIGHT):**
✓ CheckEdit in TrailingContent alongside avatar leading

- [ ] **Step 3: Run /project:review**

```
/project:review
```

- [ ] **Step 4: Final commit if any review fixes applied**

```bash
git add -A
git commit -m "fix(lists): apply review feedback on M3 list components"
```
