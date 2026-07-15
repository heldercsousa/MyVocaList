# DevExpress MAUI Component Patterns — SwipeContainerItem, IndicatorColor, compiled bindings

> Section file split from `devexpress-patterns.md` on 2026-07-14 (token-scoped reads). Index + never-miss rules: `devexpress-patterns.md`.

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
