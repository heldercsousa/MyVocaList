# DevExpress MAUI Component Patterns — DXCollectionView + multi-select + swipe actions

> Section file split from `devexpress-patterns.md` on 2026-07-14 (token-scoped reads). Index + never-miss rules: `devexpress-patterns.md`.

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
