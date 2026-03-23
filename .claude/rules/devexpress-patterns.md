# DevExpress MAUI Component Patterns

## Namespace Declarations

```xml
xmlns:dx="http://schemas.devexpress.com/maui"
xmlns:dxe="clr-namespace:DevExpress.Maui.Editors;assembly=DevExpress.Maui.Editors"
xmlns:dxcv="clr-namespace:DevExpress.Maui.CollectionView;assembly=DevExpress.Maui.CollectionView"
xmlns:dxg="clr-namespace:DevExpress.Maui.DataGrid;assembly=DevExpress.Maui.DataGrid"
xmlns:dxc="clr-namespace:DevExpress.Maui.Charts;assembly=DevExpress.Maui.Charts"
```

## DXButton — confirmed in codebase

Five named styles in `MaterialStyles.xaml`:
- `FilledButton` — primary action, `BackgroundColor=Primary`, `CornerRadius=20`
- `FilledTonalButton` — secondary action, `BackgroundColor=SecondaryContainer`
- `OutlinedButton` — cancel/secondary, `BorderColor=Outline`
- `TextButton` — low-emphasis, transparent
- `FlyoutMenuButton` — navigation drawer items, `HorizontalContentAlignment=Start`

Icon-only button pattern (FAB, toolbar icons):
```xml
<dx:DXButton Icon="add_outlined"
             IconColor="{StaticResource OnPrimary}"
             BackgroundColor="{StaticResource Primary}"
             PressedBackgroundColor="{StaticResource PrimaryContainer}"
             WidthRequest="56" HeightRequest="56"
             CornerRadius="16"
             HorizontalOptions="End" VerticalOptions="End"
             Margin="0,0,16,16"
             Command="{Binding AddCommand}" />
```

Icon-only display button (no tap, just icon):
```xml
<dx:DXButton Icon="nightlife_outlined"
             IconColor="{dx:ThemeColor OnSurfaceVariant}"
             IconWidth="64" IconHeight="64"
             BackgroundColor="Transparent"
             InputTransparent="True"
             WidthRequest="80" HeightRequest="80" />
```

## DXCollectionView — confirmed in codebase

```xml
<dxcv:DXCollectionView x:Name="collectionView"
       ItemsSource="{Binding Items}"
       SelectedItems="{Binding SelectedItemsRaw}"
       IsPullToRefreshEnabled="True"
       IsRefreshing="{Binding IsRefreshing, Mode=TwoWay}"
       PullToRefreshCommand="{Binding RefreshCommand}"
       IsLoadMoreEnabled="{Binding HasMoreItems}"
       LoadMoreCommand="{Binding LoadMoreCommand}"
       SelectionMode="{Binding SelectionMode}"
       UseRippleEffect="True"
       AllowCascadeUpdate="True"
       ItemSeparatorThickness="0"
       Tap="OnItemTapped"
       LongPress="OnItemLongPressed"
       SwipeItemShowing="OnSwipeItemShowing"
       SelectionChanged="OnSelectionChanged">

    <dxcv:DXCollectionView.ItemTemplate>
        <DataTemplate x:DataType="dto:MyDto">
            <!-- item view -->
        </DataTemplate>
    </dxcv:DXCollectionView.ItemTemplate>

    <dxcv:DXCollectionView.SelectedItemTemplate>
        <DataTemplate x:DataType="dto:MyDto">
            <!-- selected item view (distinct border/check) -->
        </DataTemplate>
    </dxcv:DXCollectionView.SelectedItemTemplate>
</dxcv:DXCollectionView>
```

**SelectedItems binding:** DXCollectionView requires `IList` (non-generic). Use a wrapper property:
```csharp
public ObservableRangeCollection<MyDto> SelectedItems { get; }
public System.Collections.IList SelectedItemsRaw => SelectedItems;
```
Bind to `SelectedItemsRaw` in XAML.

**SelectionMode:** Bind to a computed property returning `SelectionMode.Multiple` or `SelectionMode.None`.

## DXCollectionView Multi-Select Pattern — confirmed in codebase

Canonical pattern: long press enters multi-select, short tap navigates (normal mode) or is handled natively by DXCollectionView (multi-select mode).

### Event wiring
```xml
Tap="OnItemTapped"
LongPress="OnItemLongPressed"
SwipeItemShowing="OnSwipeItemShowing"
SelectionChanged="OnSelectionChanged"
```

### Code-behind handlers
```csharp
private void OnItemTapped(object sender, CollectionViewGestureEventArgs e)
{
    // DXCollectionView handles selection natively in Multiple mode — do not double-toggle
    if (_viewModel.IsMultiSelectMode) return;
    if (e.Item is MyDto item)
        _viewModel.TapCommand.Execute(item);
}

private void OnItemLongPressed(object sender, CollectionViewGestureEventArgs e)
{
    if (e.Item is not MyDto item) return;
    _viewModel.EnterMultiSelectMode(item);
    HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
}

private void OnSwipeItemShowing(object sender, SwipeItemShowingEventArgs e)
{
    if (_viewModel.IsMultiSelectMode)
        e.Cancel = true;  // suppress swipe items during multi-select
}
```

### ViewModel TapCommand (navigates to edit in normal mode)
```csharp
private void OnItemTapped(MyDto item)
{
    if (item == null) return;
    _ = Shell.Current.GoToAsync(
        $"{Routes.MyForm}?entityId={item.Id}&entityName={Uri.EscapeDataString(item.Name)}");
}
```

### Contextual action bar in Shell.TitleView (5-column layout)
```xml
<Grid ColumnDefinitions="Auto,*,Auto,Auto,Auto"
      ColumnSpacing="4" Margin="0,0,8,0"
      VerticalOptions="Center"
      IsVisible="{Binding ShowMultiSelectToolbar}">

    <dx:DXButton Grid.Column="0" Content="Select all"
                 BackgroundColor="Transparent" TextColor="{StaticResource OnSurface}"
                 FontFamily="RobotoMedium" Padding="4,0" VerticalOptions="Center"
                 Command="{Binding SelectAllCommand}" />

    <Label Grid.Column="1" Text="{Binding SelectedCountText}"
           FontFamily="RobotoMedium" FontSize="18"
           TextColor="{StaticResource OnSurface}" VerticalOptions="Center" />

    <dx:DXButton Grid.Column="2" Icon="edit_outlined"
                 IconColor="{StaticResource OnSurface}" BackgroundColor="Transparent"
                 WidthRequest="40" HeightRequest="40" CornerRadius="20"
                 HorizontalOptions="Center" VerticalOptions="Center"
                 HorizontalContentAlignment="Center"
                 IsVisible="{Binding CanEditSelected}"
                 Command="{Binding EditSelectedCommand}" />

    <dx:DXButton Grid.Column="3" Icon="delete_outlined"
                 IconColor="{StaticResource Error}" BackgroundColor="Transparent"
                 WidthRequest="40" HeightRequest="40" CornerRadius="20"
                 HorizontalOptions="Center" VerticalOptions="Center"
                 HorizontalContentAlignment="Center"
                 Command="{Binding DeleteSelectedCommand}" />

    <dx:DXButton Grid.Column="4" Content="Cancel"
                 BackgroundColor="Transparent" TextColor="{StaticResource OnSurface}"
                 FontFamily="RobotoMedium" Padding="4,0" VerticalOptions="Center"
                 Command="{Binding CancelSelectionCommand}" />
</Grid>
```

### ItemTemplate CheckEdit note
In the `ItemTemplate` (unselected), `IsChecked="False"` is hardcoded — correct by design. DXCollectionView renders `SelectedItemTemplate` (with `IsChecked="True"`) for selected items. Do not try to bind `IsChecked` to a selection state in `ItemTemplate`.

## Swipe Actions — confirmed in codebase

```xml
<dxcv:SwipeContainer>
    <dxcv:SwipeContainer.EndSwipeItems>
        <dxcv:SwipeContainerItem Caption="Delete"
                                 BackgroundColor="{StaticResource Error}"
                                 FontColor="{StaticResource OnError}"
                                 Image="delete_outlined"
                                 Command="{Binding BindingContext.SwipeDeleteCommand, Source={x:Reference page}}"
                                 CommandParameter="{Binding}" />
    </dxcv:SwipeContainer.EndSwipeItems>
    <dxcv:SwipeContainer.ItemView>
        <!-- actual item content -->
    </dxcv:SwipeContainer.ItemView>
</dxcv:SwipeContainer>
```

Note: Swipe command must bind via `Source={x:Reference page}` to reach the page's BindingContext.

## ShimmerView (skeleton loading) — confirmed in codebase

```xml
<dx:ShimmerView IsLoading="{Binding IsInitialLoading}"
                WaveWidth="0.7"
                WaveOpacity="0.8">
    <dx:ShimmerView.LoadingView>
        <VerticalStackLayout Spacing="0">
            <dx:DXBorder BackgroundColor="{dx:ThemeColor SurfaceContainerHighest}"
                         CornerRadius="12" HeightRequest="52" Margin="16,4" />
        </VerticalStackLayout>
    </dx:ShimmerView.LoadingView>
    <dx:ShimmerView.Content>
        <!-- actual content shown when not loading -->
    </dx:ShimmerView.Content>
</dx:ShimmerView>
```

Note: Add `await Task.Yield()` before starting data load in `InitializeAsync()` to allow skeleton to render.

## DXBorder — confirmed in codebase

Used for cards, chips, badges, search bar wrappers:
```xml
<dx:DXBorder BackgroundColor="{StaticResource SurfaceContainerLow}"
             CornerRadius="12"
             Padding="16"
             Margin="16,4"
             HeightRequest="64">
    <!-- content -->
</dx:DXBorder>
```

Selected item variant (with border highlight):
```xml
<dx:DXBorder BackgroundColor="{StaticResource SurfaceContainerLow}"
             BorderColor="{dx:ThemeColor Primary}"
             BorderThickness="2"
             CornerRadius="12" ... />
```

## DXScrollView — confirmed in codebase

Drop-in replacement for MAUI ScrollView:
```xml
<dx:DXScrollView BackgroundColor="{StaticResource Surface}">
    <!-- content -->
</dx:DXScrollView>
```

## CheckEdit — confirmed in codebase

```xml
<dx:CheckEdit IsChecked="{Binding IsSelected, Mode=OneWay}"
              CheckedCheckBoxColor="{dx:ThemeColor Primary}"
              InputTransparent="True"
              VerticalOptions="Center" />
```

Note: `CheckedCheckBoxColor` requires `{dx:ThemeColor X}` — `{StaticResource X}` does not work here.

## TextEdit (Editors) — confirmed in codebase

```xml
<dxe:TextEdit Text="{Binding FieldValue, Mode=TwoWay}"
              LabelText="Field Label"
              PlaceholderText="Enter value"
              BoxMode="Outlined"
              FocusedBorderColor="{StaticResource Primary}"
              BorderColor="{StaticResource Outline}"
              BackgroundColor="{StaticResource SurfaceContainerHighest}"
              TextColor="{StaticResource OnSurface}"
              MaxCharacterCount="30"
              HasError="{Binding FieldHasError}"
              ErrorText="{Binding FieldErrorText}" />
```

Removed properties (DevExpress 25.1.3+):
- `BoxCornerRadius` — removed, do not use

Search bar inside a rounded container:
```xml
<dx:DXBorder BackgroundColor="{StaticResource SurfaceContainer}"
             CornerRadius="28" Padding="4" Margin="16,8">
    <dxe:TextEdit Text="{Binding SearchText, Mode=TwoWay}"
                  PlaceholderText="Search..."
                  StartIcon="search_outlined"
                  StartIconColor="{StaticResource OnSurfaceVariant}"
                  BoxMode="Outlined"
                  BorderColor="Transparent"
                  FocusedBorderColor="Transparent"
                  BackgroundColor="Transparent"
                  ClearIconVisibility="Auto"
                  ClearIconColor="{StaticResource OnSurfaceVariant}" />
</dx:DXBorder>
```

## BottomSheet — confirmed in codebase

See `.claude/rules/dialogs-validation.md` for full patterns.

Key properties:
- `AllowedState="HalfExpanded"` — locks to half expanded
- `HalfExpandedRatio="0.4"` — 40% of screen height (adjust per content, 0.28 for confirm sheets)
- `IsModal="True"` — dims background
- `ShowGrabber="True"` — shows drag handle
- `AllowDismiss="True"` — user can swipe down to dismiss
- `CornerRadius="28"` — rounded top corners
- `StateChanged` event — sync state back to ViewModel

## Theme Token Usage

Two ways to reference color tokens in XAML:

| Method | When to use |
|--------|-------------|
| `{StaticResource Primary}` | Standard layout properties (BackgroundColor, TextColor, etc.) |
| `{dx:ThemeColor Primary}` | DevExpress-specific properties (CheckedCheckBoxColor, BorderColor on DXBorder) |

Token names are identical — only the binding syntax differs.

## Shell Navigation Form Page — confirmed in VenueFormPage.xaml

For Add/Edit forms that require keyboard input: use a **dedicated Shell navigation page** instead of a BottomSheet.
This avoids BottomSheet/keyboard conflicts and keyboard avoidance is handled automatically by `SafeAreaEdges="All"` + `ScrollView`.

### XAML
```xml
<ContentPage SafeAreaEdges="All"
             x:DataType="vm:MyFormViewModel"
             Title="{Binding PageTitle}">
    <ScrollView>
        <VerticalStackLayout Padding="24" Spacing="16">
            <dxe:TextEdit Text="{Binding FieldValue, Mode=TwoWay}"
                          LabelText="Field Label"
                          HasError="{Binding FieldHasError}"
                          ErrorText="{Binding FieldErrorText}"
                          BoxMode="Outlined"
                          FocusedBorderColor="{StaticResource Primary}"
                          BorderColor="{StaticResource Outline}"
                          BackgroundColor="{StaticResource SurfaceContainerHighest}"
                          TextColor="{StaticResource OnSurface}" />

            <HorizontalStackLayout HorizontalOptions="End" Spacing="8">
                <dx:DXButton Content="Cancel"
                             Style="{StaticResource OutlinedButton}"
                             Padding="24,0"
                             Command="{Binding CancelCommand}" />
                <dx:DXButton Content="Save"
                             Style="{StaticResource FilledButton}"
                             Padding="24,0"
                             Command="{Binding SaveCommand}" />
            </HorizontalStackLayout>
        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

### ViewModel
```csharp
[QueryProperty(nameof(EntityId), "entityId")]
[QueryProperty(nameof(EntityName), "entityName")]
public partial class MyFormViewModel : ViewModelBase
{
    [ObservableProperty] private int? _entityId;
    [ObservableProperty] private string _entityName = string.Empty;
    [ObservableProperty] private bool _fieldHasError;
    [ObservableProperty] private string _fieldErrorText = string.Empty;
    [ObservableProperty] private bool _isBusy;

    public bool IsEditMode => EntityId.HasValue;
    public string PageTitle => IsEditMode ? "Edit X" : "New X";

    // Both commands navigate back
    private Task CancelAsync() => Shell.Current.GoToAsync("..");
    private async Task SaveAsync() { ... await Shell.Current.GoToAsync(".."); }
}
```

### Navigation (from list page)
```csharp
// Add
await Shell.Current.GoToAsync(Routes.MyForm);

// Edit — pass ID and current value via query string
await Shell.Current.GoToAsync($"{Routes.MyForm}?entityId={item.Id}&entityName={Uri.EscapeDataString(item.Name)}");
```

Register in `AppShell.xaml.cs`:
```csharp
Routing.RegisterRoute(Routes.MyForm, typeof(MyFormPage));
```

### Code-behind (focus first field on appear)
```csharp
protected override void OnAppearing()
{
    base.OnAppearing();
    nameEdit.Focus();
}
```

## SwipeContainerItem — use Tap event handler, not Command binding

`SwipeContainerItem.Command` + `CommandParameter` binding is unreliable when the command
lives on the ViewModel (not the item itself). Use the `Tap` event instead — confirmed fix.

```xml
<dxcv:SwipeContainerItem Caption="Delete"
                         BackgroundColor="{StaticResource Error}"
                         FontColor="{StaticResource OnError}"
                         Image="delete_outlined"
                         Tap="OnSwipeDeleteTapped" />
```

```csharp
private void OnSwipeDeleteTapped(object sender, SwipeItemTapEventArgs e)
{
    if (e.Item is MyDto item)
        _viewModel.DeleteCommand.Execute(item);
}
```

`SwipeItemTapEventArgs.Item` provides the underlying data object. This fires for both
partial-tap and full-swipe (when `FullSwipeMode="AllItems"` is set on `SwipeContainer`).

Enable full-swipe to trigger the action automatically.
`FullSwipeMode` is a `[Flags]` enum — valid values: `None`, `Start`, `End`, `Both` (default).
Use `End` when you only have `EndSwipeItems`, `Start` when only `StartSwipeItems`, `Both` for both sides:
```xml
<dxcv:SwipeContainer FullSwipeMode="End">
```

## DXCollectionView — IndicatorColor required for dark themes

The load-more spinner is invisible by default against dark backgrounds. Always set:

```xml
<dxcv:DXCollectionView ...
    IsLoadMoreEnabled="{Binding HasMoreItems}"
    LoadMoreCommand="{Binding LoadMoreCommand}"
    IndicatorColor="{StaticResource Primary}" />
```

## Compiled bindings — typed ViewModel property for cross-DataTemplate access

Inside `x:DataType`-constrained `DataTemplate`s, binding to `BindingContext.IsMultiSelectMode`
via `Source={x:Reference page}` chains through `BindingContext` typed as `object` — the
compiler cannot subscribe to change notifications, so the binding never updates after init.

Fix: expose a typed public property on the page's code-behind:
```csharp
public MyViewModel ViewModel => _viewModel;
```
Then bind: `{Binding ViewModel.IsMultiSelectMode, Source={x:Reference page}}`

The compiler now resolves the full type chain and subscribes to `INotifyPropertyChanged`.

## MD3 App Bar Components — confirmed in AppBars/

Two reusable `ContentView` components in `MyVocaList/UI/Components/AppBars/`. DevExpress has no built-in top app bar — these components use `DXButton` for icon buttons and `TextEdit`+`DXBorder` for the search field internally.

### SmallAppBar
Place inside `Shell.TitleView`. Supports: nav icon, title (22sp Regular, OnSurface),
subtitle (14sp Regular, OnSurfaceVariant), up to 3 trailing action icons (48×48, OnSurfaceVariant),
and scroll-elevation via `IsElevated` (Surface → SurfaceContainer).

Namespace: `xmlns:appbars="clr-namespace:MyVocaList.UI.Components.AppBars"`

```xml
<Shell.TitleView>
    <appbars:SmallAppBar
        Title="{Binding PageTitle}"
        NavigationIcon="arrow_back_outlined"
        NavigationCommand="{Binding BackCommand}"
        Action1Icon="search_outlined"
        Action1Command="{Binding OpenSearchCommand}"
        IsElevated="{Binding IsScrolled}" />
</Shell.TitleView>
```

BindableProperties: `Title`, `Subtitle`, `NavigationIcon`, `NavigationCommand`,
`Action1Icon/Command`, `Action2Icon/Command`, `Action3Icon/Command`, `IsElevated`

### SearchAppBar
The search field IS the app bar — replaces SmallAppBar when search is the primary page action (M3 "Search app bar" pattern). Place inside `Shell.TitleView`.

```xml
<Shell.TitleView>
    <appbars:SearchAppBar
        SearchText="{Binding SearchText, Mode=TwoWay}"
        Placeholder="Search venues..."
        IsElevated="{Binding IsScrolled}" />
</Shell.TitleView>
```

BindableProperties: `SearchText` (TwoWay), `Placeholder`, `LeadingIcon`, `LeadingCommand`,
`TrailingIcon`, `TrailingCommand`, `IsElevated`

### IsElevated — scroll detection pattern
The page is responsible for detecting scroll and setting `IsElevated` via a ViewModel property:

```csharp
// In page code-behind — listen to DXCollectionView scroll
private void OnCollectionViewScrolled(object sender, CollectionViewScrolledEventArgs e)
{
    _viewModel.IsScrolled = e.VerticalOffset > 0;
}
```

### VenuesPage MD3 non-compliance (tracked — not yet remediated)
| # | Issue | Current | M3 Spec |
|---|-------|---------|---------|
| 1 | Search placement | Below Shell navbar (content Row 0) | Search IS the app bar → use `SearchAppBar` in `Shell.TitleView` |
| 2 | Title typography | `FontSize="20"`, `RobotoMedium` | 22sp Regular (`textAppearanceTitleLarge`) |
| 3 | Scroll elevation | None | Surface → SurfaceContainer on scroll (`liftOnScroll`) |

---

## Known Gotchas

- `BoxCornerRadius` on `TextEdit` removed in DevExpress 25.1.3+ — do not use
- `CheckEdit.CheckedCheckBoxColor` requires `{dx:ThemeColor X}` not `{StaticResource X}`
- `DXCollectionView.SelectedItems` requires `IList` (non-generic) binding — use wrapper property
- `SwipeContainerItem.Command` binding is unreliable — always use the `Tap` event handler instead
- `SwipeContainer.FullSwipeMode="AllItems"` does NOT exist — valid values are `None`, `Start`, `End`, `Both`
- `DXCollectionView.IndicatorColor` defaults to invisible on dark themes — always set explicitly
- `ShimmerView` needs `await Task.Yield()` before data load so skeleton renders first
- `.NET MAUI 10`: `ContentPage` defaults to `SafeAreaEdges="None"` — add `SafeAreaEdges="Container"` explicitly
- Compiled bindings inside `x:DataType` DataTemplates: use a typed `ViewModel` property on the page, not `BindingContext.X`

## ContentView sub-components — BindableProperty wiring patterns

Two confirmed patterns for wiring BindableProperties to XAML elements in `ContentView` components:

### Pattern A: XAML self-binding (AppBarBase subclasses)
Use when the BP value maps 1:1 to a single XAML property with no derived logic.
```xml
<dx:DXButton Icon="{Binding NavigationIcon, Source={x:Reference self}}" ... />
```

### Pattern B: propertyChanged callback → direct element set (ListItem)
Use when derived logic is needed (visibility toggle, alignment update, multi-element change).
```csharp
public static readonly BindableProperty OverlineProperty =
    BindableProperty.Create(nameof(Overline), typeof(string), typeof(ListItem), "",
        propertyChanged: (b, _, n) =>
        {
            var item = (ListItem)b;
            item.overlineLabel.Text = (string)n;
            item.overlineLabel.IsVisible = !string.IsNullOrEmpty((string)n);
        });
```

**Rule:** Prefer Pattern B when `propertyChanged` has any logic beyond a direct value pass-through. Never duplicate logic in both a XAML binding AND a `propertyChanged` callback — pick one.

