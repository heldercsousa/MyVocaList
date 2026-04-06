# Styles & Structure Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Centralize all inline styles, add the complete MD3 type scale, rename `ListItemLeadingAvatar` → `ListItemLeadingMonogram`, and build two new reusable components (`EmptyState`, `ConfirmSheet`) that every future CRUD page will share.

**Architecture:** Style-only changes go into `MaterialStyles.xaml` as new named styles and style classes. Components apply those styles. Two new ContentView components are created in `UI/Components/States/` and `UI/Components/Sheets/`. No behavior changes — purely structural and visual.

**Tech Stack:** .NET MAUI 10 · C# 13 · DevExpress MAUI v25.2.4 · XAML

---

## File Map

| Status | File | Purpose |
|--------|------|---------|
| Modify | `MyVocaList/Resources/Styles/MaterialStyles.xaml` | Add 5 type scale styles + 9 named styles |
| Modify | `MyVocaList/UI/Components/AppBars/SmallAppBar.xaml` | Apply `NavigationIconButton`, `StandardIconButton`, type scale classes |
| Modify | `MyVocaList/UI/Components/AppBars/SearchAppBar.xaml` | Apply `NavigationIconButton`, `StandardIconButton` |
| Modify | `MyVocaList/UI/Components/Toolbars/FloatingToolbar.xaml` | Apply `StandardIconButton` to all 5 slots |
| Modify | `MyVocaList/UI/Components/Lists/ListItem.xaml` | Apply type scale classes to 3 labels |
| Rename | `MyVocaList/UI/Components/Lists/ListItemLeadingAvatar.xaml` → `ListItemLeadingMonogram.xaml` | Rename file + class |
| Rename | `MyVocaList/UI/Components/Lists/ListItemLeadingAvatar.xaml.cs` → `ListItemLeadingMonogram.xaml.cs` | Rename file + class |
| Modify | `MyVocaList/UI/Pages/Venues/VenueFormPage.xaml` | Remove 5 redundant TextEdit props; apply `Body.Small` to counter |
| Modify | `MyVocaList/AppShell.xaml` | Apply `NavDrawerSectionHeader`, `Divider`, `Title.Large`, `Body.Medium` |
| Create | `MyVocaList/UI/Components/States/EmptyState.xaml` | New MD3 Empty state component |
| Create | `MyVocaList/UI/Components/States/EmptyState.xaml.cs` | Code-behind + BindableProperties |
| Create | `MyVocaList/UI/Components/Sheets/ConfirmSheet.xaml` | New confirm BottomSheet component |
| Create | `MyVocaList/UI/Components/Sheets/ConfirmSheet.xaml.cs` | Code-behind + BindableProperties + state management |
| Modify | `MyVocaList/UI/Pages/Venues/VenuesPage.xaml` | Apply `SkeletonBone`, `EmptyState`, `Fab`, `ConfirmSheet`, named button/divider styles |
| Modify | `MyVocaList/UI/Pages/Venues/VenuesPage.xaml.cs` | Update ConfirmSheet interaction |
| Modify | `.claude/rules/m3-components.md` | Add type scale table, EmptyState anatomy, NavDrawer typography fix |
| Modify | `.claude/rules/devexpress-patterns.md` | Add all 9 named styles; `ListItemLeadingMonogram` rename note; BoxView.Color note |
| Modify | `.claude/rules/crud-pages.md` | Update shimmer, EmptyState, ConfirmSheet sections |
| Modify | `.claude/rules/dialogs-validation.md` | Update ConfirmSheet pattern; mark inline BottomSheet as the current approach |

---

## Task 1: MaterialStyles.xaml — Add type scale and named styles

**Files:**
- Modify: `MyVocaList/Resources/Styles/MaterialStyles.xaml`

- [ ] **Step 1: Add 5 new MD3 type scale styles to the TYPOGRAPHY section**

  After the existing `Label.Large` style (line 131), insert:

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

  > Note: `Label.Small` weight is **Medium** (not Regular) per MD3 spec. This corrects the current `ListItem` overline which uses `RobotoRegular 11sp`.

- [ ] **Step 2: Add a new ICON BUTTONS section with `StandardIconButton` and `NavigationIconButton`**

  Before the closing `</ResourceDictionary>`, insert:

  ```xml
  <!-- ================================================================ -->
  <!-- ICON BUTTON STYLES (MD3 Standard Icon Button)                    -->
  <!-- ================================================================ -->

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
  ```

- [ ] **Step 3: Add FAB, Divider, SkeletonBone, BottomSheet action styles**

  In the same section (or a new UTILITY STYLES section), insert:

  ```xml
  <!-- ================================================================ -->
  <!-- UTILITY STYLES                                                    -->
  <!-- ================================================================ -->

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
  ```

- [ ] **Step 4: Add EmptyState and NavDrawerSectionHeader styles**

  Still before the closing `</ResourceDictionary>`:

  ```xml
  <!-- ================================================================ -->
  <!-- EMPTY STATE STYLES                                               -->
  <!-- ================================================================ -->

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

  <!-- ================================================================ -->
  <!-- NAVIGATION DRAWER STYLES                                         -->
  <!-- ================================================================ -->

  <!-- MD3: Navigation drawer — Section header label (Label Medium: 12sp Medium) -->
  <Style x:Key="NavDrawerSectionHeader" TargetType="Label">
      <Setter Property="FontFamily" Value="RobotoMedium" />
      <Setter Property="FontSize" Value="12" />
      <Setter Property="TextColor" Value="{StaticResource OnSurfaceVariant}" />
      <Setter Property="Padding" Value="16,8,16,4" />
  </Style>
  ```

- [ ] **Step 5: Build and verify no errors**

  ```
  dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
  ```

  Expected: Build succeeded, 0 errors.

- [ ] **Step 6: Commit**

  ```bash
  git add MyVocaList/Resources/Styles/MaterialStyles.xaml
  git commit -m "style: add MD3 type scale, icon button, utility, and component styles"
  ```

---

## Task 2: Apply styles to AppBar components

**Files:**
- Modify: `MyVocaList/UI/Components/AppBars/SmallAppBar.xaml`
- Modify: `MyVocaList/UI/Components/AppBars/SearchAppBar.xaml`

- [ ] **Step 1: Update `SmallAppBar.xaml` — nav button, action buttons, labels**

  Replace the nav button (Grid.Column="0") with:
  ```xml
  <dx:DXButton Grid.Column="0"
               x:Name="navButton"
               Icon="{Binding NavigationIcon, Source={x:Reference self}}"
               Style="{StaticResource NavigationIconButton}"
               IsVisible="{Binding HasNavigationIcon, Source={x:Reference self}}"
               SemanticProperties.Description="Navigate back"
               Command="{Binding NavigationCommand, Source={x:Reference self}}" />
  ```

  Replace the title label with:
  ```xml
  <Label x:Name="titleLabel"
         Text="{Binding Title, Source={x:Reference self}}"
         StyleClass="Title.Large"
         TextColor="{StaticResource OnSurface}"
         LineBreakMode="TailTruncation"
         MaxLines="1" />
  ```

  Replace the subtitle label with:
  ```xml
  <Label x:Name="subtitleLabel"
         Text="{Binding Subtitle, Source={x:Reference self}}"
         StyleClass="Body.Medium"
         TextColor="{StaticResource OnSurfaceVariant}"
         LineBreakMode="TailTruncation"
         MaxLines="1"
         IsVisible="{Binding HasSubtitle, Source={x:Reference self}}" />
  ```

  Replace all three action buttons (action1Button, action2Button, action3Button) — same pattern for each:
  ```xml
  <dx:DXButton Grid.Column="2"
               x:Name="action1Button"
               Icon="{Binding Action1Icon, Source={x:Reference self}}"
               Style="{StaticResource StandardIconButton}"
               IsVisible="{Binding HasAction1, Source={x:Reference self}}"
               SemanticProperties.Description="Action"
               Command="{Binding Action1Command, Source={x:Reference self}}" />

  <dx:DXButton Grid.Column="3"
               x:Name="action2Button"
               Icon="{Binding Action2Icon, Source={x:Reference self}}"
               Style="{StaticResource StandardIconButton}"
               IsVisible="{Binding HasAction2, Source={x:Reference self}}"
               SemanticProperties.Description="Action"
               Command="{Binding Action2Command, Source={x:Reference self}}" />

  <dx:DXButton Grid.Column="4"
               x:Name="action3Button"
               Icon="{Binding Action3Icon, Source={x:Reference self}}"
               Style="{StaticResource StandardIconButton}"
               IsVisible="{Binding HasAction3, Source={x:Reference self}}"
               SemanticProperties.Description="Action"
               Command="{Binding Action3Command, Source={x:Reference self}}" />
  ```

- [ ] **Step 2: Build `SmallAppBar` changes**

  ```
  dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
  ```

  Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Update `SearchAppBar.xaml` — leading button and action buttons**

  Replace the leading button with:
  ```xml
  <dx:DXButton Grid.Column="0"
               x:Name="leadingButton"
               Icon="search_outlined"
               Style="{StaticResource NavigationIconButton}"
               SemanticProperties.Description="Search"
               Clicked="OnLeadingButtonClicked" />
  ```

  Replace all three action buttons with `Style="{StaticResource StandardIconButton}"` (same pattern as Step 1 but without `IconColor` and `BackgroundColor` inline props).

- [ ] **Step 4: Build `SearchAppBar` changes**

  ```
  dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
  ```

  Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Commit**

  ```bash
  git add MyVocaList/UI/Components/AppBars/SmallAppBar.xaml
  git add MyVocaList/UI/Components/AppBars/SearchAppBar.xaml
  git commit -m "style(appbars): apply NavigationIconButton, StandardIconButton, and type scale classes"
  ```

---

## Task 3: Apply styles to FloatingToolbar and ListItem

**Files:**
- Modify: `MyVocaList/UI/Components/Toolbars/FloatingToolbar.xaml`
- Modify: `MyVocaList/UI/Components/Lists/ListItem.xaml`

- [ ] **Step 1: Update `FloatingToolbar.xaml` — all 5 action slots**

  For each of the 5 `dx:DXButton` elements, replace inline icon/background/size props with `Style="{StaticResource StandardIconButton}"`.

  Example (apply to all 5 slots):
  ```xml
  <dx:DXButton x:Name="action1Button"
               Icon="{Binding Action1Icon, Source={x:Reference self}}"
               Style="{StaticResource StandardIconButton}"
               IsVisible="{Binding HasAction1, Source={x:Reference self}}"
               SemanticProperties.Description="{Binding Action1Description, Source={x:Reference self}}"
               Command="{Binding Action1Command, Source={x:Reference self}}" />
  ```

  > Note: `FloatingToolbar.xaml.cs` has `ApplySelectedState(button, isSelected)` which directly sets `button.BackgroundColor` and `button.IconColor` at runtime for the selected state. This overrides the style defaults — no conflict.

- [ ] **Step 2: Update `ListItem.xaml` — apply type scale classes to 3 labels**

  Replace the overline label with:
  ```xml
  <Label x:Name="overlineLabel"
         StyleClass="Label.Small"
         TextColor="{StaticResource OnSurfaceVariant}"
         IsVisible="False"
         MaxLines="1"
         LineBreakMode="TailTruncation" />
  ```
  > `Label.Small` is RobotoMedium 11sp — corrects the previous deviation (was RobotoRegular).

  Replace the headline label with:
  ```xml
  <Label x:Name="headlineLabel"
         StyleClass="Body.Large"
         TextColor="{StaticResource OnSurface}"
         MaxLines="1"
         LineBreakMode="TailTruncation" />
  ```

  Replace the supporting label with:
  ```xml
  <Label x:Name="supportingLabel"
         StyleClass="Body.Medium"
         TextColor="{StaticResource OnSurfaceVariant}"
         IsVisible="False"
         MaxLines="1"
         LineBreakMode="TailTruncation" />
  ```

- [ ] **Step 3: Build**

  ```
  dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
  ```

  Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

  ```bash
  git add MyVocaList/UI/Components/Toolbars/FloatingToolbar.xaml
  git add MyVocaList/UI/Components/Lists/ListItem.xaml
  git commit -m "style(components): apply StandardIconButton and type scale classes to FloatingToolbar and ListItem"
  ```

---

## Task 4: Rename ListItemLeadingAvatar → ListItemLeadingMonogram

**Files:**
- Rename: `MyVocaList/UI/Components/Lists/ListItemLeadingAvatar.xaml` → `ListItemLeadingMonogram.xaml`
- Rename: `MyVocaList/UI/Components/Lists/ListItemLeadingAvatar.xaml.cs` → `ListItemLeadingMonogram.xaml.cs`

**Why:** MD3 distinguishes Monogram (initials in circle) from Avatar (photo in circle). This component renders initials only — it is a Monogram per MD3 spec.

- [ ] **Step 1: Create `ListItemLeadingMonogram.xaml`**

  ```xml
  <?xml version="1.0" encoding="utf-8" ?>
  <ContentView
      xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
      xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
      xmlns:dx="http://schemas.devexpress.com/maui"
      x:Class="MyVocaList.UI.Components.Lists.ListItemLeadingMonogram"
      x:Name="self">

      <dx:DXBorder
          WidthRequest="40"
          HeightRequest="40"
          CornerRadius="20"
          BackgroundColor="{Binding MonogramColor, Source={x:Reference self}}">

          <Label
              Text="{Binding Initials, Source={x:Reference self}}"
              StyleClass="Label.Large"
              TextColor="{Binding InitialsColor, Source={x:Reference self}}"
              HorizontalOptions="Center"
              VerticalOptions="Center"
              HorizontalTextAlignment="Center" />

      </dx:DXBorder>
  </ContentView>
  ```

  > Note: `StyleClass="Label.Large"` replaces the inline `FontFamily="RobotoMedium" FontSize="14"`.

- [ ] **Step 2: Create `ListItemLeadingMonogram.xaml.cs`**

  ```csharp
  namespace MyVocaList.UI.Components.Lists;

  public partial class ListItemLeadingMonogram : ContentView
  {
      public static readonly BindableProperty InitialsProperty = BindableProperty.Create(
          nameof(Initials),
          typeof(string),
          typeof(ListItemLeadingMonogram),
          defaultValue: string.Empty);

      public static readonly BindableProperty MonogramColorProperty = BindableProperty.Create(
          nameof(MonogramColor),
          typeof(Color),
          typeof(ListItemLeadingMonogram),
          defaultValue: Colors.Transparent);

      public static readonly BindableProperty InitialsColorProperty = BindableProperty.Create(
          nameof(InitialsColor),
          typeof(Color),
          typeof(ListItemLeadingMonogram),
          defaultValue: Colors.Transparent);

      public string Initials
      {
          get => (string)GetValue(InitialsProperty);
          set => SetValue(InitialsProperty, value);
      }

      public Color MonogramColor
      {
          get => (Color)GetValue(MonogramColorProperty);
          set => SetValue(MonogramColorProperty, value);
      }

      public Color InitialsColor
      {
          get => (Color)GetValue(InitialsColorProperty);
          set => SetValue(InitialsColorProperty, value);
      }

      public ListItemLeadingMonogram()
      {
          InitializeComponent();
      }
  }
  ```

  > Note: `AvatarColor` renamed to `MonogramColor` for MD3 accuracy.

- [ ] **Step 3: Delete the old files**

  ```bash
  git rm "MyVocaList/UI/Components/Lists/ListItemLeadingAvatar.xaml"
  git rm "MyVocaList/UI/Components/Lists/ListItemLeadingAvatar.xaml.cs"
  ```

- [ ] **Step 4: Build**

  ```
  dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
  ```

  Expected: Build succeeded, 0 errors. (No non-placeholder page uses `ListItemLeadingAvatar` in XAML — verified during audit.)

- [ ] **Step 5: Commit**

  ```bash
  git add MyVocaList/UI/Components/Lists/ListItemLeadingMonogram.xaml
  git add MyVocaList/UI/Components/Lists/ListItemLeadingMonogram.xaml.cs
  git commit -m "refactor(lists): rename ListItemLeadingAvatar to ListItemLeadingMonogram per MD3 terminology"
  ```

---

## Task 5: VenueFormPage cleanup

**Files:**
- Modify: `MyVocaList/UI/Pages/Venues/VenueFormPage.xaml`

- [ ] **Step 1: Remove 5 redundant TextEdit properties**

  The implicit `Style TargetType="dx:TextEdit"` in `MaterialStyles.xaml` already sets `BoxMode`, `FocusedBorderColor`, `BorderColor`, `BackgroundColor`, `TextColor`. Remove those 5 explicit setters from `nameEdit` in `VenueFormPage.xaml`.

  Replace the `dxe:TextEdit` element with:
  ```xml
  <dxe:TextEdit x:Name="nameEdit"
                Text="{Binding VenueName, Mode=TwoWay}"
                LabelText="Venue Name"
                PlaceholderText="Enter venue name"
                MaxCharacterCount="30"
                HasError="{Binding NameHasError}"
                ErrorText="{Binding NameErrorText}" />
  ```

- [ ] **Step 2: Apply `Body.Small` style class to character counter label**

  Replace the counter label with:
  ```xml
  <Label Text="{Binding CharacterCounterText}"
         IsVisible="{Binding ShowCharacterCounter}"
         StyleClass="Body.Small"
         HorizontalOptions="End">
      <Label.Triggers>
          <DataTrigger TargetType="Label"
                       Binding="{Binding IsCharacterCounterError}"
                       Value="True">
              <Setter Property="TextColor" Value="{StaticResource Error}" />
          </DataTrigger>
          <DataTrigger TargetType="Label"
                       Binding="{Binding IsCharacterCounterWarning}"
                       Value="True">
              <Setter Property="TextColor" Value="{StaticResource Warning}" />
          </DataTrigger>
      </Label.Triggers>
  </Label>
  ```

  > Note: No default `TextColor` is set on the label itself — the DataTriggers handle Error/Warning, and the normal state inherits the theme's label color (OnSurface). This matches MD3 supporting text behavior.

- [ ] **Step 3: Build**

  ```
  dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
  ```

  Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

  ```bash
  git add MyVocaList/UI/Pages/Venues/VenueFormPage.xaml
  git commit -m "style(venue-form): remove redundant TextEdit props; apply Body.Small to character counter"
  ```

---

## Task 6: AppShell inline style cleanup

**Files:**
- Modify: `MyVocaList/AppShell.xaml`

- [ ] **Step 1: Apply `Title.Large` + `TextColor=Primary` to app name label**

  Replace:
  ```xml
  <Label Text="MyVocaList"
         FontFamily="RobotoBold" FontSize="24"
         TextColor="{StaticResource Primary}" />
  ```
  With:
  ```xml
  <Label Text="MyVocaList"
         StyleClass="Title.Large"
         FontFamily="RobotoBold"
         TextColor="{StaticResource Primary}" />
  ```
  > Keep `FontFamily="RobotoBold"` explicitly — `Title.Large` sets `RobotoRegular`, but the brand name uses Bold. The explicit inline `FontFamily` takes precedence over the style class.

- [ ] **Step 2: Apply `Body.Medium` + `TextColor=OnSurfaceVariant` to subtitle label**

  Replace:
  ```xml
  <Label Text="Karaoke Queue Manager"
         FontFamily="RobotoRegular" FontSize="14"
         TextColor="{StaticResource OnSurfaceVariant}" />
  ```
  With:
  ```xml
  <Label Text="Karaoke Queue Manager"
         StyleClass="Body.Medium"
         TextColor="{StaticResource OnSurfaceVariant}" />
  ```

- [ ] **Step 3: Apply `Divider` style to the FlyoutHeader BoxView**

  Replace:
  ```xml
  <BoxView HeightRequest="1" Margin="0,12,0,0"
           Color="{StaticResource OutlineVariant}" />
  ```
  With:
  ```xml
  <BoxView Style="{StaticResource Divider}" Margin="0,12,0,0" />
  ```
  > `Color` is already the correct BoxView property (not `BackgroundColor`). The `Divider` style sets `HeightRequest=1` and `Color=OutlineVariant`. `Margin` stays inline since it is position-specific.

- [ ] **Step 4: Apply `NavDrawerSectionHeader` style to section group title label**

  Replace:
  ```xml
  <Label Text="{Binding GroupTitle}"
         FontFamily="RobotoMedium" FontSize="14"
         TextColor="{StaticResource OnSurfaceVariant}"
         Padding="16,8,16,4" />
  ```
  With:
  ```xml
  <Label Text="{Binding GroupTitle}"
         Style="{StaticResource NavDrawerSectionHeader}" />
  ```
  > `NavDrawerSectionHeader` sets `FontFamily=RobotoMedium`, `FontSize=12` (was 14 — MD3 fix), `TextColor=OnSurfaceVariant`, `Padding=16,8,16,4`. All inline props are now in the style.

- [ ] **Step 5: Build**

  ```
  dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
  ```

  Expected: Build succeeded, 0 errors.

- [ ] **Step 6: Commit**

  ```bash
  git add MyVocaList/AppShell.xaml
  git commit -m "style(appshell): apply Divider, NavDrawerSectionHeader, and type scale styles; fix section header to Label Medium 12sp"
  ```

---

## Task 7: EmptyState component

**Files:**
- Create: `MyVocaList/UI/Components/States/EmptyState.xaml`
- Create: `MyVocaList/UI/Components/States/EmptyState.xaml.cs`

- [ ] **Step 1: Create `EmptyState.xaml`**

  ```xml
  <?xml version="1.0" encoding="utf-8" ?>
  <ContentView
      xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
      xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
      xmlns:dx="http://schemas.devexpress.com/maui"
      x:Class="MyVocaList.UI.Components.States.EmptyState"
      x:Name="self">

      <!--
          MD3 Empty state anatomy:
          Container → Illustration slot (icon) → Headline → Supporting text (optional)
          Container is vertically and horizontally centered; consumer sets Margin/IsVisible.
      -->
      <VerticalStackLayout
          VerticalOptions="Center"
          HorizontalOptions="Center"
          Spacing="8">

          <!-- Illustration slot: display-only icon, 80×80dp, 64dp icon -->
          <dx:DXButton x:Name="illustrationButton"
                       Style="{StaticResource EmptyStateIllustration}"
                       Icon="{Binding Illustration, Source={x:Reference self}}" />

          <!-- Headline slot -->
          <Label x:Name="headlineLabel"
                 Style="{StaticResource EmptyStateHeadline}"
                 Text="{Binding Headline, Source={x:Reference self}}" />

          <!-- Supporting text slot (hidden when empty) -->
          <Label x:Name="supportingLabel"
                 StyleClass="Body.Medium"
                 TextColor="{StaticResource OnSurfaceVariant}"
                 HorizontalTextAlignment="Center"
                 Text="{Binding SupportingText, Source={x:Reference self}}"
                 IsVisible="False" />

      </VerticalStackLayout>
  </ContentView>
  ```

- [ ] **Step 2: Create `EmptyState.xaml.cs`**

  ```csharp
  namespace MyVocaList.UI.Components.States;

  /// <summary>
  /// MD3 Empty state component. Shows an illustration icon, a headline, and optional supporting text.
  /// Set <see cref="Illustration"/>, <see cref="Headline"/>, and optionally <see cref="SupportingText"/>.
  /// Control visibility via <c>IsVisible</c> on this component.
  /// </summary>
  public partial class EmptyState : ContentView
  {
      public static readonly BindableProperty IllustrationProperty =
          BindableProperty.Create(nameof(Illustration), typeof(string), typeof(EmptyState), string.Empty);

      public static readonly BindableProperty HeadlineProperty =
          BindableProperty.Create(nameof(Headline), typeof(string), typeof(EmptyState), string.Empty);

      public static readonly BindableProperty SupportingTextProperty =
          BindableProperty.Create(nameof(SupportingText), typeof(string), typeof(EmptyState), string.Empty,
              propertyChanged: (b, _, n) =>
              {
                  var c = (EmptyState)b;
                  c.supportingLabel.IsVisible = !string.IsNullOrEmpty((string)n);
              });

      /// <summary>Icon name for the illustration slot (e.g. "nightlife_outlined").</summary>
      public string Illustration
      {
          get => (string)GetValue(IllustrationProperty);
          set => SetValue(IllustrationProperty, value);
      }

      /// <summary>Primary text displayed below the illustration.</summary>
      public string Headline
      {
          get => (string)GetValue(HeadlineProperty);
          set => SetValue(HeadlineProperty, value);
      }

      /// <summary>Optional secondary text. Hidden when null or empty.</summary>
      public string SupportingText
      {
          get => (string)GetValue(SupportingTextProperty);
          set => SetValue(SupportingTextProperty, value);
      }

      public EmptyState()
      {
          InitializeComponent();
      }
  }
  ```

- [ ] **Step 3: Build**

  ```
  dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
  ```

  Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

  ```bash
  git add MyVocaList/UI/Components/States/EmptyState.xaml
  git add MyVocaList/UI/Components/States/EmptyState.xaml.cs
  git commit -m "feat(components): add EmptyState component (MD3 Empty state anatomy)"
  ```

---

## Task 8: ConfirmSheet component

**Files:**
- Create: `MyVocaList/UI/Components/Sheets/ConfirmSheet.xaml`
- Create: `MyVocaList/UI/Components/Sheets/ConfirmSheet.xaml.cs`

**⚠️ Risk note:** `dx:BottomSheet` wrapped inside a `ContentView` in a page `Grid` must still function as a modal overlay. If the overlay z-order or host resolution is broken, use the fallback path in Step 5 and document the inline template pattern in `dialogs-validation.md` instead of shipping a broken component.

- [ ] **Step 1: Create `ConfirmSheet.xaml`**

  ```xml
  <?xml version="1.0" encoding="utf-8" ?>
  <ContentView
      xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
      xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
      xmlns:dx="http://schemas.devexpress.com/maui"
      x:Class="MyVocaList.UI.Components.Sheets.ConfirmSheet"
      x:Name="self">

      <!--
          MD3 Modal bottom sheet — confirm/destructive action pattern.
          Drive via SheetState BindableProperty (TwoWay from ViewModel).
          HalfExpandedRatio=0.28 fits single-message confirmations.
      -->
      <dx:BottomSheet x:Name="bottomSheet"
                      HalfExpandedRatio="0.28"
                      AllowedState="HalfExpanded"
                      IsModal="True"
                      ShowGrabber="True"
                      AllowDismiss="True"
                      BackgroundColor="{StaticResource Surface}"
                      CornerRadius="28"
                      StateChanged="OnStateChanged">
          <VerticalStackLayout>
              <Label x:Name="messageLabel"
                     StyleClass="Title.Medium"
                     TextColor="{StaticResource OnSurface}"
                     HorizontalTextAlignment="Center"
                     Margin="24,20" />
              <BoxView Style="{StaticResource Divider}" />
              <dx:DXButton x:Name="actionButton"
                           Style="{StaticResource BottomSheetDestructiveAction}"
                           Command="{Binding ActionCommand, Source={x:Reference self}}" />
              <BoxView Style="{StaticResource Divider}" />
              <dx:DXButton Content="Cancel"
                           Style="{StaticResource BottomSheetCancelAction}"
                           Command="{Binding DismissCommand, Source={x:Reference self}}" />
          </VerticalStackLayout>
      </dx:BottomSheet>

  </ContentView>
  ```

- [ ] **Step 2: Create `ConfirmSheet.xaml.cs`**

  ```csharp
  namespace MyVocaList.UI.Components.Sheets;

  /// <summary>
  /// MD3 modal bottom sheet for confirming a destructive action.
  /// Bind <see cref="SheetState"/> TwoWay to a ViewModel property to open/close.
  /// </summary>
  public partial class ConfirmSheet : ContentView
  {
      private bool _isSyncing;

      public static readonly BindableProperty SheetStateProperty =
          BindableProperty.Create(nameof(SheetState), typeof(BottomSheetState), typeof(ConfirmSheet),
              BottomSheetState.Hidden,
              propertyChanged: (b, _, n) => ((ConfirmSheet)b).OnSheetStateChanged((BottomSheetState)n));

      public static readonly BindableProperty MessageProperty =
          BindableProperty.Create(nameof(Message), typeof(string), typeof(ConfirmSheet), string.Empty,
              propertyChanged: (b, _, n) => ((ConfirmSheet)b).messageLabel.Text = (string)n);

      public static readonly BindableProperty ActionTextProperty =
          BindableProperty.Create(nameof(ActionText), typeof(string), typeof(ConfirmSheet), string.Empty,
              propertyChanged: (b, _, n) => ((ConfirmSheet)b).actionButton.Content = (string)n);

      public static readonly BindableProperty ActionCommandProperty =
          BindableProperty.Create(nameof(ActionCommand), typeof(ICommand), typeof(ConfirmSheet));

      public static readonly BindableProperty DismissCommandProperty =
          BindableProperty.Create(nameof(DismissCommand), typeof(ICommand), typeof(ConfirmSheet));

      public BottomSheetState SheetState
      {
          get => (BottomSheetState)GetValue(SheetStateProperty);
          set => SetValue(SheetStateProperty, value);
      }

      public string Message
      {
          get => (string)GetValue(MessageProperty);
          set => SetValue(MessageProperty, value);
      }

      public string ActionText
      {
          get => (string)GetValue(ActionTextProperty);
          set => SetValue(ActionTextProperty, value);
      }

      public ICommand ActionCommand
      {
          get => (ICommand)GetValue(ActionCommandProperty);
          set => SetValue(ActionCommandProperty, value);
      }

      public ICommand DismissCommand
      {
          get => (ICommand)GetValue(DismissCommandProperty);
          set => SetValue(DismissCommandProperty, value);
      }

      public ConfirmSheet()
      {
          InitializeComponent();
      }

      private void OnSheetStateChanged(BottomSheetState newState)
      {
          if (_isSyncing) return;

          var host = this.GetParentPage();
          if (host == null) return;

          if (newState == BottomSheetState.Hidden)
              bottomSheet.Close();
          else
              bottomSheet.Show(newState, host);
      }

      private void OnStateChanged(object sender, ValueChangedEventArgs<BottomSheetState> e)
      {
          // Sync sheet dismissal (user swipe) back to the ViewModel via TwoWay binding
          if (e.NewValue != SheetState)
          {
              _isSyncing = true;
              SheetState = e.NewValue;
              _isSyncing = false;
          }
      }
  }

  // Extension to traverse the visual tree for the containing Page
  file static class VisualElementExtensions
  {
      public static Page GetParentPage(this VisualElement element)
      {
          var parent = element.Parent;
          while (parent != null)
          {
              if (parent is Page page)
                  return page;
              parent = parent.Parent;
          }
          return null;
      }
  }
  ```

- [ ] **Step 3: Build**

  ```
  dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
  ```

  Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

  ```bash
  git add MyVocaList/UI/Components/Sheets/ConfirmSheet.xaml
  git add MyVocaList/UI/Components/Sheets/ConfirmSheet.xaml.cs
  git commit -m "feat(components): add ConfirmSheet component (MD3 modal bottom sheet for destructive confirm)"
  ```

---

## Task 9: Apply EmptyState and ConfirmSheet in VenuesPage

**Files:**
- Modify: `MyVocaList/UI/Pages/Venues/VenuesPage.xaml`
- Modify: `MyVocaList/UI/Pages/Venues/VenuesPage.xaml.cs`

- [ ] **Step 1: Add namespace declarations to `VenuesPage.xaml`**

  Add these xmlns declarations to the `<ContentPage>` opening tag:
  ```xml
  xmlns:states="clr-namespace:MyVocaList.UI.Components.States"
  xmlns:sheets="clr-namespace:MyVocaList.UI.Components.Sheets"
  ```

- [ ] **Step 2: Replace 6 shimmer bones with `SkeletonBone` style**

  Replace the `<dx:ShimmerView.LoadingView>` content:
  ```xml
  <dx:ShimmerView.LoadingView>
      <VerticalStackLayout Spacing="0">
          <dx:DXBorder Style="{StaticResource SkeletonBone}" />
          <dx:DXBorder Style="{StaticResource SkeletonBone}" />
          <dx:DXBorder Style="{StaticResource SkeletonBone}" />
          <dx:DXBorder Style="{StaticResource SkeletonBone}" />
          <dx:DXBorder Style="{StaticResource SkeletonBone}" />
          <dx:DXBorder Style="{StaticResource SkeletonBone}" />
      </VerticalStackLayout>
  </dx:ShimmerView.LoadingView>
  ```

- [ ] **Step 3: Replace 2 empty state VerticalStackLayout blocks with `EmptyState` components**

  Replace the "No venue registered" block:
  ```xml
  <states:EmptyState
      Illustration="nightlife_outlined"
      Headline="No venue registered"
      IsVisible="{Binding IsEmptyNoVenues}"
      Margin="32,32,32,80" />
  ```

  Replace the "No venue found" block:
  ```xml
  <states:EmptyState
      Illustration="search_outlined"
      Headline="No venue found"
      IsVisible="{Binding IsEmptyNoResults}"
      Margin="32,32,32,80" />
  ```

- [ ] **Step 4: Replace FAB with `Fab` style**

  Replace:
  ```xml
  <dx:DXButton Icon="add_outlined"
               IconColor="{StaticResource OnPrimary}"
               BackgroundColor="{StaticResource Primary}"
               PressedBackgroundColor="{StaticResource PrimaryContainer}"
               WidthRequest="56" HeightRequest="56"
               CornerRadius="16"
               HorizontalOptions="End" VerticalOptions="End"
               Margin="0,0,16,88"
               SemanticProperties.Description="Add venue"
               Command="{Binding AddVenueCommand}" />
  ```
  With:
  ```xml
  <dx:DXButton Style="{StaticResource Fab}"
               Icon="add_outlined"
               Margin="0,0,16,88"
               SemanticProperties.Description="Add venue"
               Command="{Binding AddVenueCommand}" />
  ```

- [ ] **Step 5: Replace inline BottomSheet with `ConfirmSheet` component**

  Replace the `<dx:BottomSheet x:Name="confirmSheet" ...>` block with:
  ```xml
  <sheets:ConfirmSheet x:Name="confirmSheet"
                       SheetState="{Binding ConfirmSheetState, Mode=TwoWay}"
                       Message="{Binding ConfirmMessage}"
                       ActionText="{Binding ConfirmActionText}"
                       ActionCommand="{Binding ConfirmActionCommand}"
                       DismissCommand="{Binding DismissConfirmCommand}" />
  ```

- [ ] **Step 6: Update `VenuesPage.xaml.cs` to remove manual sheet management**

  The page code-behind currently manages the BottomSheet via `PropertyChanged` + `confirmSheet.Show()/Close()`. With `ConfirmSheet`, the component handles its own state internally. Remove the sheet management from code-behind:

  Remove the `_viewModel.PropertyChanged += OnViewModelPropertyChanged;` subscription from the constructor.

  Remove the `OnViewModelPropertyChanged` method entirely.

  Remove the `OnConfirmSheetStateChanged` method entirely (no longer wired in XAML).

  The `OnBackButtonPressed` check for `ConfirmSheetState != Hidden` remains — it still reads the ViewModel property.

  Final code-behind (relevant sections):
  ```csharp
  public VenuesPage(VenuesViewModel viewModel)
  {
      InitializeComponent();
      _viewModel = viewModel;
      BindingContext = _viewModel;
      // No PropertyChanged subscription needed — ConfirmSheet handles itself
  }
  ```

- [ ] **Step 7: Build**

  ```
  dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
  ```

  Expected: Build succeeded, 0 errors.

- [ ] **Step 8: Smoke test on emulator — verify ConfirmSheet works as a modal overlay**

  Deploy to Android emulator and verify:
  - Navigate to Venues page
  - Select one or more venues
  - Tap Delete → confirm sheet appears as a modal overlay
  - The sheet dims the background correctly
  - Tapping Cancel dismisses the sheet
  - Tapping the destructive action triggers deletion

  **If the BottomSheet does not appear or does not overlay correctly:**
  - Revert `VenuesPage.xaml` Step 5 — restore the inline `<dx:BottomSheet>` block
  - Revert `VenuesPage.xaml.cs` Step 6 — restore `OnViewModelPropertyChanged` and `OnConfirmSheetStateChanged`
  - Delete `ConfirmSheet.xaml` and `ConfirmSheet.xaml.cs`
  - Document the inline BottomSheet as the canonical pattern in `dialogs-validation.md`

- [ ] **Step 9: Commit**

  ```bash
  git add MyVocaList/UI/Pages/Venues/VenuesPage.xaml
  git add MyVocaList/UI/Pages/Venues/VenuesPage.xaml.cs
  git commit -m "feat(venues): apply SkeletonBone, EmptyState, Fab style, and ConfirmSheet component"
  ```

---

## Task 10: Update rules files

**Files:**
- Modify: `.claude/rules/m3-components.md`
- Modify: `.claude/rules/devexpress-patterns.md`
- Modify: `.claude/rules/crud-pages.md`
- Modify: `.claude/rules/dialogs-validation.md`

- [ ] **Step 1: Update `m3-components.md`**

  In the "MD3 Terminology Conventions" section, add:

  ```markdown
  ### Complete MD3 type scale — MAUI StyleClass keys

  | MD3 role | Style class | Family | sp | Weight |
  |---|---|---|---|---|
  | Display Large | `Display.Large` | RobotoRegular | 57 | Regular |
  | Headline Large | `Headline.Large` | RobotoRegular | 32 | Regular |
  | Title Large | `Title.Large` | RobotoRegular | 22 | Regular |
  | Title Medium | `Title.Medium` | RobotoMedium | 16 | Medium |
  | Body Large | `Body.Large` | RobotoRegular | 16 | Regular |
  | Body Medium | `Body.Medium` | RobotoRegular | 14 | Regular |
  | Body Small | `Body.Small` | RobotoRegular | 12 | Regular |
  | Label Large | `Label.Large` | RobotoMedium | 14 | Medium |
  | Label Medium | `Label.Medium` | RobotoMedium | 12 | Medium |
  | Label Small | `Label.Small` | RobotoMedium | 11 | Medium |
  ```

  > All 10 entries are defined in `MaterialStyles.xaml` as `StyleClass` entries. `Label.Small` weight is Medium per MD3 spec.

  Add `EmptyState` component anatomy:

  ```markdown
  ## M3 Empty State component

  | Slot | BindableProperty | Control | Style |
  |---|---|---|---|
  | Illustration | `Illustration` (string icon name) | `dx:DXButton` (display-only) | `EmptyStateIllustration` |
  | Headline | `Headline` (string) | `Label` | `EmptyStateHeadline` |
  | Supporting text | `SupportingText` (string, optional) | `Label` | `Body.Medium` + `OnSurfaceVariant` |

  Usage:
  ```xml
  <states:EmptyState
      Illustration="nightlife_outlined"
      Headline="No items yet"
      IsVisible="{Binding IsEmpty}"
      Margin="32,32,32,80" />
  ```

  Namespace: `xmlns:states="clr-namespace:MyVocaList.UI.Components.States"`

  Fix NavDrawer section header typography:
  - **Was:** `RobotoMedium 14sp` (= Label Large)
  - **Correct:** `RobotoMedium 12sp` = Label Medium per MD3 Navigation Drawer spec
  - **In code:** `Style="{StaticResource NavDrawerSectionHeader}"` (sets 12sp Medium)

- [ ] **Step 2: Update `devexpress-patterns.md`**

  Add a "Named Styles" section listing all 9 new style keys with purpose:

  ```markdown
  ## Named Styles — complete list

  | Key | TargetType | Purpose |
  |---|---|---|
  | `StandardIconButton` | `dx:DXButton` | Trailing/action icon buttons (48×48, OnSurfaceVariant) |
  | `NavigationIconButton` | `dx:DXButton` | Leading/nav icon buttons (48×48, OnSurface) |
  | `Fab` | `dx:DXButton` | Floating action button (56×56, CornerRadius=16, Primary) |
  | `Divider` | `BoxView` | 1dp divider line (OutlineVariant). Uses `Color` not `BackgroundColor`. |
  | `SkeletonBone` | `dx:DXBorder` | Shimmer skeleton bone (56dp, CornerRadius=0, SurfaceContainerHighest) |
  | `BottomSheetDestructiveAction` | `dx:DXButton` | Destructive action in BottomSheet (Error text, Fill, 56dp) |
  | `BottomSheetCancelAction` | `dx:DXButton` | Cancel in BottomSheet (Primary text, Fill, 56dp) |
  | `EmptyStateHeadline` | `Label` | Headline in EmptyState (RobotoMedium 16sp, OnSurfaceVariant, centered) |
  | `EmptyStateIllustration` | `dx:DXButton` | Icon in EmptyState (display-only, 80×80, 64dp icon) |
  | `NavDrawerSectionHeader` | `Label` | Nav drawer group title (RobotoMedium 12sp, OnSurfaceVariant) |
  ```

  Add rename note:
  ```markdown
  ## ListItemLeadingMonogram (formerly ListItemLeadingAvatar)

  Renamed per MD3 terminology. MD3 distinguishes:
  - **Monogram**: initials text in a circle — `ListItemLeadingMonogram`
  - **Avatar**: photo/image of a person — not yet implemented

  BindableProperties: `Initials` (string), `MonogramColor` (Color), `InitialsColor` (Color)
  ```

  Add BoxView.Color clarification:
  ```markdown
  ## BoxView.Color vs BackgroundColor

  Always use `BoxView.Color` — it is BoxView's own fill property.
  `BackgroundColor` (from `VisualElement`) also renders but is semantically wrong.
  The `Divider` named style uses `Color` canonically.
  ```

- [ ] **Step 3: Update `crud-pages.md`**

  In the "Shimmer Skeleton" section, replace:
  > Use 6 bones.

  With:
  ```markdown
  Use 6 bones. Apply the `SkeletonBone` named style — no inline props needed:
  ```xml
  <dx:DXBorder Style="{StaticResource SkeletonBone}" />
  ```

  In the ViewModel Checklist section, update the ConfirmSheet note:
  ```markdown
  | `BottomSheetState ConfirmSheetState` | Drives `ConfirmSheet` component (if destructive action exists) |
  ```

  Add a note about the `EmptyState` component:
  ```markdown
  ## Empty State

  Use the `EmptyState` component for all empty/no-results states:
  ```xml
  <states:EmptyState
      Illustration="nightlife_outlined"
      Headline="No items registered"
      IsVisible="{Binding IsEmptyNoItems}"
      Margin="32,32,32,80" />
  ```
  Namespace: `xmlns:states="clr-namespace:MyVocaList.UI.Components.States"`

- [ ] **Step 4: Update `dialogs-validation.md`**

  In the "BottomSheet Patterns" section, add:
  ```markdown
  ## ConfirmSheet component

  Use `ConfirmSheet` for destructive action confirmations. It wraps `dx:BottomSheet` with
  standard MD3 styling and handles its own Show/Close based on the `SheetState` BindableProperty.

  ```xml
  <sheets:ConfirmSheet x:Name="confirmSheet"
                       SheetState="{Binding ConfirmSheetState, Mode=TwoWay}"
                       Message="{Binding ConfirmMessage}"
                       ActionText="{Binding ConfirmActionText}"
                       ActionCommand="{Binding ConfirmActionCommand}"
                       DismissCommand="{Binding DismissConfirmCommand}" />
  ```

  Namespace: `xmlns:sheets="clr-namespace:MyVocaList.UI.Components.Sheets"`

  The ViewModel retains the same `ConfirmSheetState`, `ConfirmMessage`, `ConfirmActionText`,
  `ConfirmActionCommand`, `DismissConfirmCommand` properties — no ViewModel changes needed.
  The page code-behind no longer needs to subscribe to `PropertyChanged` for sheet management.

  **Note:** If `ConfirmSheet` has z-order issues on a particular page (BottomSheet not overlaying
  correctly when wrapped in ContentView inside Grid), fall back to the inline `dx:BottomSheet`
  template from the "Confirm / Destructive Action Sheet" section below.

- [ ] **Step 5: Build**

  ```
  dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
  ```

  Expected: Build succeeded, 0 errors. (Rules files are docs — build checks XAML/CS only.)

- [ ] **Step 6: Commit**

  ```bash
  git add .claude/rules/m3-components.md
  git add .claude/rules/devexpress-patterns.md
  git add .claude/rules/crud-pages.md
  git add .claude/rules/dialogs-validation.md
  git commit -m "docs(rules): update m3-components, devexpress-patterns, crud-pages, dialogs-validation for Styles & Structure changes"
  ```

---

## Self-Review

**Spec coverage check:**

| Spec item | Task |
|---|---|
| 5 new MD3 type scale styles | Task 1 Step 1 |
| 9 named styles | Task 1 Steps 2-4 |
| SmallAppBar inline style removal | Task 2 Step 1 |
| SearchAppBar inline style removal | Task 2 Step 3 |
| FloatingToolbar inline style removal | Task 3 Step 1 |
| ListItem type scale classes | Task 3 Step 2 |
| ListItemLeadingMonogram rename | Task 4 |
| VenueFormPage TextEdit cleanup | Task 5 Steps 1-2 |
| AppShell inline style cleanup + NavDrawer fix | Task 6 |
| EmptyState component | Task 7 |
| ConfirmSheet component | Task 8 |
| VenuesPage — all 4 apply steps | Task 9 Steps 2-5 |
| Rules files updates | Task 10 |

**No gaps found.**

**Placeholder scan:** No TBDs, no "similar to" references, no missing code blocks found.

**Type consistency:**
- `ListItemLeadingMonogram` uses `MonogramColor` (renamed from `AvatarColor`) — consistent throughout Task 4
- `ConfirmSheet.SheetState` is `BottomSheetState` — matches ViewModel type throughout Task 9
- `EmptyState.Illustration` / `Headline` / `SupportingText` — consistent between XAML (Task 7 Step 1) and CS (Task 7 Step 2)
- `StandardIconButton` / `NavigationIconButton` keys — defined in Task 1, applied consistently in Tasks 2-3
