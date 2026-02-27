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

## Known Gotchas

- `BoxCornerRadius` on `TextEdit` removed in DevExpress 25.1.3+ — do not use
- `CheckEdit.CheckedCheckBoxColor` requires `{dx:ThemeColor X}` not `{StaticResource X}`
- `DXCollectionView.SelectedItems` requires `IList` (non-generic) binding — use wrapper property
- Swipe commands in item templates must use `Source={x:Reference page}` to reach ViewModel
- `ShimmerView` needs `await Task.Yield()` before data load so skeleton renders first
- `.NET MAUI 10`: `ContentPage` defaults to `SafeAreaEdges="None"` — add `SafeAreaEdges="Container"` explicitly
