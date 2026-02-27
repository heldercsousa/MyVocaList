# VenuesPage Multi-Select UX Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Align VenuesPage multi-select with the DevExpress canonical pattern — long press enters multi-select, short tap navigates to edit, contextual action bar gets Select All + Cancel buttons, and swipe is suppressed during multi-select.

**Architecture:** Three-file change: XAML (layout + event wires), code-behind (gesture routing + swipe suppression), ViewModel (tap navigates instead of entering multi-select). No new files needed. No service or domain changes.

**Tech Stack:** .NET MAUI 10 · DevExpress MAUI DXCollectionView · CommunityToolkit.Mvvm · Shell navigation

---

## Decisions already approved by Helder

| # | Decision |
|---|----------|
| 1 | Short tap → navigate to `VenueFormPage` (edit). Long press → enter multi-select. |
| 2 | Remove "Select All" checkbox row below search. Add "Select all" text button to left of contextual action bar in `Shell.TitleView`. |
| 3 | Disable swipe items while in multi-select mode via `SwipeItemShowing` event (`e.Cancel = true`). |

---

## Pre-flight: files to read before touching anything

- `MyVocaList/UI/Pages/Venues/VenuesPage.xaml` — full layout
- `MyVocaList/UI/Pages/Venues/VenuesPage.xaml.cs` — code-behind
- `MyVocaList/UI/ViewModels/VenuesViewModel.cs` — ViewModel
- `MyVocaList/Navigation/Routes.cs` — route constants

---

## Task 1: ViewModel — change TapCommand to navigate to edit

**Files:**
- Modify: `MyVocaList/UI/ViewModels/VenuesViewModel.cs`

**What to change:**
The current `TapCommand` calls `OnItemTapped` which enters multi-select mode. Multi-select is now entered via long press (handled in code-behind). Tap must now navigate to `VenueFormPage` in edit mode.

**Step 1: Change `OnItemTapped` in ViewModel**

Find this method (~line 240):
```csharp
private void OnItemTapped(VenueListItemDto item)
{
    if (item == null) return;

    if (IsMultiSelectMode)
    {
        if (SelectedVenues.Contains(item))
            SelectedVenues.Remove(item);
        else
            SelectedVenues.Add(item);
        return;
    }

    EnterMultiSelectMode(item);
}
```

Replace with:
```csharp
private void OnItemTapped(VenueListItemDto item)
{
    if (item == null) return;
    _ = Shell.Current.GoToAsync(
        $"{Routes.VenueForm}?venueId={item.Id}&venueName={Uri.EscapeDataString(item.Name)}");
}
```

**Step 2: Build and verify**

```
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -c Debug
```
Expected: 0 errors. The IsMultiSelectMode guard in code-behind (`if (_viewModel.IsMultiSelectMode) return;`) already prevents `TapCommand` from firing during multi-select, so we don't need to guard here.

**Step 3: Commit**
```
git add MyVocaList/UI/ViewModels/VenuesViewModel.cs
git commit -m "refactor(venues): tap navigates to edit instead of entering multi-select"
```

---

## Task 2: Code-behind — add LongPress handler + haptic + swipe suppression

**Files:**
- Modify: `MyVocaList/UI/Pages/Venues/VenuesPage.xaml.cs`

**What to change:**
1. Add `OnItemLongPressed` — enters multi-select, adds the long-pressed item, triggers haptic feedback.
2. Add `OnSwipeItemShowing` — cancels swipe item display when in multi-select mode.
3. Keep `OnItemTapped` exactly as-is from the previous fix (guards on `IsMultiSelectMode`, calls `TapCommand` when not in multi-select).

**Step 1: Add the two new event handlers**

After the existing `OnItemTapped` method, add:

```csharp
private void OnItemLongPressed(object sender, CollectionViewGestureEventArgs e)
{
    if (e.Item is not VenueListItemDto item) return;
    _viewModel.EnterMultiSelectMode(item);
    HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
}

private void OnSwipeItemShowing(object sender, SwipeItemShowingEventArgs e)
{
    if (_viewModel.IsMultiSelectMode)
        e.Cancel = true;
}
```

**Step 2: Build and verify**

```
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -c Debug
```
Expected: 0 errors. (The XAML event wires don't exist yet — that's Task 3. Build still passes because the handlers are defined; XAML errors only surface at runtime if wired incorrectly, not at build time for Android.)

Actually: missing XAML event wires means these methods are unreferenced — that is fine, no compiler error.

**Step 3: Commit**
```
git add MyVocaList/UI/Pages/Venues/VenuesPage.xaml.cs
git commit -m "feat(venues): add LongPress handler with haptic + swipe suppression in multi-select"
```

---

## Task 3: XAML — update DXCollectionView event wires

**Files:**
- Modify: `MyVocaList/UI/Pages/Venues/VenuesPage.xaml`

**What to change:**
Add `LongPress="OnItemLongPressed"` and `SwipeItemShowing="OnSwipeItemShowing"` to the `DXCollectionView`.
The existing `Tap="OnItemTapped"` stays (it now drives navigation in normal mode, returns early in multi-select).

**Step 1: Locate the DXCollectionView opening tag (~line 124)**

Current:
```xml
<dxcv:DXCollectionView x:Name="collectionView"
       ItemsSource="{Binding Venues}"
       ...
       Tap="OnItemTapped"
       SelectionChanged="OnSelectionChanged">
```

Add the two new events:
```xml
<dxcv:DXCollectionView x:Name="collectionView"
       ItemsSource="{Binding Venues}"
       ...
       Tap="OnItemTapped"
       LongPress="OnItemLongPressed"
       SwipeItemShowing="OnSwipeItemShowing"
       SelectionChanged="OnSelectionChanged">
```

**Step 2: Build**

```
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -c Debug
```
Expected: 0 errors.

**Step 3: Commit**
```
git add MyVocaList/UI/Pages/Venues/VenuesPage.xaml
git commit -m "feat(venues): wire LongPress and SwipeItemShowing events on DXCollectionView"
```

---

## Task 4: XAML — remove "Select All" row + update Grid rows

**Files:**
- Modify: `MyVocaList/UI/Pages/Venues/VenuesPage.xaml`

**What to change:**
1. Remove `RowDefinitions="Auto,Auto,*"` → `"Auto,*"` on the main Grid.
2. Remove the entire "Select all row" block (`Grid.Row="1"`, ~lines 83–100).
3. Update every `Grid.Row="2"` reference → `Grid.Row="1"`.
4. Update `BottomSheet Grid.RowSpan="3"` → `Grid.RowSpan="2"`.

**Step 1: Update RowDefinitions**

```xml
<!-- BEFORE -->
<Grid RowDefinitions="Auto,Auto,*">

<!-- AFTER -->
<Grid RowDefinitions="Auto,*">
```

**Step 2: Remove the "Select all row" block entirely**

Remove this entire block (~lines 83–100):
```xml
<!-- Select all row (multi-select mode only) -->
<Grid Grid.Row="1"
      ColumnDefinitions="Auto,*" ColumnSpacing="12"
      Margin="32,4,16,4"
      IsVisible="{Binding IsMultiSelectMode}">
    <Grid.GestureRecognizers>
        <TapGestureRecognizer Command="{Binding SelectAllCommand}" />
    </Grid.GestureRecognizers>
    <dx:CheckEdit Grid.Column="0"
                  IsChecked="{Binding IsAllSelected, Mode=OneWay}"
                  CheckedCheckBoxColor="{dx:ThemeColor Primary}"
                  InputTransparent="True"
                  VerticalOptions="Center" />
    <Label Grid.Column="1"
           Text="Select all"
           FontFamily="RobotoRegular" FontSize="16"
           TextColor="{StaticResource OnSurface}"
           VerticalOptions="Center" />
</Grid>
```

**Step 3: Update all Grid.Row="2" → Grid.Row="1"**

Four occurrences:
- `<dx:ShimmerView Grid.Row="2"` → `Grid.Row="1"`
- `<VerticalStackLayout Grid.Row="2" IsVisible="{Binding IsEmptyNoVenues}"` → `Grid.Row="1"`
- `<VerticalStackLayout Grid.Row="2" IsVisible="{Binding IsEmptyNoResults}"` → `Grid.Row="1"`
- `<dx:DXButton Grid.Row="2" Icon="add_outlined"` (FAB) → `Grid.Row="1"`

**Step 4: Update BottomSheet RowSpan**

```xml
<!-- BEFORE -->
<dx:BottomSheet x:Name="confirmSheet"
                Grid.RowSpan="3"

<!-- AFTER -->
<dx:BottomSheet x:Name="confirmSheet"
                Grid.RowSpan="2"
```

**Step 5: Build**

```
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -c Debug
```
Expected: 0 errors.

**Step 6: Commit**
```
git add MyVocaList/UI/Pages/Venues/VenuesPage.xaml
git commit -m "refactor(venues): remove Select All checkbox row, update grid rows"
```

---

## Task 5: XAML — redesign contextual action bar in Shell.TitleView

**Files:**
- Modify: `MyVocaList/UI/Pages/Venues/VenuesPage.xaml`

**What to change:**
Replace the current 3-column multi-select toolbar Grid with a 5-column version:
`[Select all (Auto)] [Count (*)] [Edit (Auto)] [Delete (Auto)] [Cancel (Auto)]`

**Step 1: Locate the multi-select toolbar Grid inside Shell.TitleView**

Current (~line 27):
```xml
<Grid ColumnDefinitions="*,Auto,Auto"
      ColumnSpacing="8"
      Margin="0,0,16,0"
      VerticalOptions="Center"
      IsVisible="{Binding ShowMultiSelectToolbar}">

    <Label Grid.Column="0"
           Text="{Binding SelectedCountText}"
           FontFamily="RobotoMedium" FontSize="18"
           TextColor="{StaticResource OnSurface}"
           VerticalOptions="Center" />

    <dx:DXButton Grid.Column="1"
                 Icon="edit_outlined"
                 IconColor="{StaticResource OnSurface}"
                 BackgroundColor="Transparent"
                 WidthRequest="40" HeightRequest="40"
                 CornerRadius="20"
                 HorizontalOptions="Center" VerticalOptions="Center"
                 HorizontalContentAlignment="Center"
                 IsVisible="{Binding CanEditSelected}"
                 Command="{Binding EditSelectedCommand}" />

    <dx:DXButton Grid.Column="2"
                 Icon="delete_outlined"
                 IconColor="{StaticResource Error}"
                 BackgroundColor="Transparent"
                 WidthRequest="40" HeightRequest="40"
                 CornerRadius="20"
                 HorizontalOptions="Center" VerticalOptions="Center"
                 HorizontalContentAlignment="Center"
                 Command="{Binding DeleteSelectedCommand}" />
</Grid>
```

Replace with:
```xml
<Grid ColumnDefinitions="Auto,*,Auto,Auto,Auto"
      ColumnSpacing="4"
      Margin="0,0,8,0"
      VerticalOptions="Center"
      IsVisible="{Binding ShowMultiSelectToolbar}">

    <!-- Select all — far left -->
    <dx:DXButton Grid.Column="0"
                 Content="Select all"
                 BackgroundColor="Transparent"
                 TextColor="{StaticResource OnSurface}"
                 FontFamily="RobotoMedium"
                 Padding="4,0"
                 VerticalOptions="Center"
                 Command="{Binding SelectAllCommand}" />

    <!-- Selected count — center -->
    <Label Grid.Column="1"
           Text="{Binding SelectedCountText}"
           FontFamily="RobotoMedium" FontSize="18"
           TextColor="{StaticResource OnSurface}"
           VerticalOptions="Center" />

    <!-- Edit — icon, only when exactly 1 selected -->
    <dx:DXButton Grid.Column="2"
                 Icon="edit_outlined"
                 IconColor="{StaticResource OnSurface}"
                 BackgroundColor="Transparent"
                 WidthRequest="40" HeightRequest="40"
                 CornerRadius="20"
                 HorizontalOptions="Center" VerticalOptions="Center"
                 HorizontalContentAlignment="Center"
                 IsVisible="{Binding CanEditSelected}"
                 Command="{Binding EditSelectedCommand}" />

    <!-- Delete — icon -->
    <dx:DXButton Grid.Column="3"
                 Icon="delete_outlined"
                 IconColor="{StaticResource Error}"
                 BackgroundColor="Transparent"
                 WidthRequest="40" HeightRequest="40"
                 CornerRadius="20"
                 HorizontalOptions="Center" VerticalOptions="Center"
                 HorizontalContentAlignment="Center"
                 Command="{Binding DeleteSelectedCommand}" />

    <!-- Cancel — far right -->
    <dx:DXButton Grid.Column="4"
                 Content="Cancel"
                 BackgroundColor="Transparent"
                 TextColor="{StaticResource OnSurface}"
                 FontFamily="RobotoMedium"
                 Padding="4,0"
                 VerticalOptions="Center"
                 Command="{Binding CancelSelectionCommand}" />
</Grid>
```

**Step 2: Build**

```
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -c Debug
```
Expected: 0 errors.

**Step 3: Commit**
```
git add MyVocaList/UI/Pages/Venues/VenuesPage.xaml
git commit -m "feat(venues): contextual action bar — Select All + Cancel buttons, 5-column layout"
```

---

## Task 6: Post-implementation

**Step 1: Run `/project:review`**

**Step 2: Update `.claude/rules/devexpress-patterns.md`**

Add a new section "DXCollectionView Multi-Select Pattern" documenting:
- LongPress trigger
- HapticFeedback.LongPress
- SwipeItemShowing e.Cancel = true
- Contextual action bar layout [Select All | Count | Actions | Cancel]

**Step 3: Run `/project:commit` then `/project:changelog`**

---

## Reference

- DevExpress canonical example: https://github.com/DevExpress-Examples/maui-collectionview-long-tap
- DevExpress scenario page: https://docs.devexpress.com/MAUI/404354/scenarios/long-tap
- `SwipeItemShowingEventArgs.Cancel` (inherits `CancelEventArgs`) — set to `true` to suppress swipe item
