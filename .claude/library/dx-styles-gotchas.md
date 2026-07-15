# DevExpress MAUI Component Patterns — Styles Must Exist Before Use + Known Gotchas

> Section file split from `devexpress-patterns.md` on 2026-07-14 (token-scoped reads). Index + never-miss rules: `devexpress-patterns.md`.

## Styles Must Exist Before Use

> ⚠️ Before adding `Style="{StaticResource SomeKey}"` to any XAML file, verify the key is defined in `MaterialStyles.xaml` or `MaterialColors.xaml`.

**Rules:**
1. Search `MaterialStyles.xaml` for the key **before** referencing it in XAML.
2. If the key is missing: add it to the correct resource dictionary in the same commit that introduces the XAML reference.
3. Never add a `StaticResource` reference and leave the definition for "later" — the app will crash at runtime on that page.
4. If the style is BottomSheet-specific, add it near the `BottomSheetDestructiveAction` / `BottomSheetCancelAction` styles.

**Known styles that have been added (previously missing):**
- `BottomSheetTitle` — `Label`, titleLarge (22sp, RobotoRegular, OnSurface, Padding="24,16,24,8"). Added 2026-06-13.

---

## Known Gotchas

- `BoxCornerRadius` on `TextEdit` removed in DevExpress 25.1.3+ — do not use
- **DX ThemeManager provides colors only — not typography scale.** `ThemeManager` generates a tonal palette + semantic color tokens (`Primary`, `OnSurface`, etc.). It does NOT define MD3 type scale styles (`Title.Large`, `Body.Large`, etc.) for MAUI `Label`. All type scale entries must be in the app's own `MaterialStyles.xaml`. Adding them is never redundant with DX. (Confirmed via DX docs 2026-03-30.)
- **Implicit styles apply by CLR type, not xmlns alias.** `dx:TextEdit` (schema `http://schemas.devexpress.com/maui`) and `dxe:TextEdit` (`clr-namespace:DevExpress.Maui.Editors`) resolve to the same CLR type. The implicit `Style TargetType="dx:TextEdit"` in `MaterialStyles.xaml` applies to `dxe:TextEdit` in pages — explicit property re-declarations in pages that duplicate what the implicit style already sets are redundant and must be removed.
- **`BoxView.Color` vs `BoxView.BackgroundColor`**: `Color` is the BoxView-specific fill property. `BackgroundColor` (from `VisualElement`) also works visually but is semantically incorrect for BoxView. Always use `Color` on BoxView — especially in the `Divider` named style.
- `FontFamily`/`FontSize`/`InputFontFamily`/`InputFontSize` are NOT valid on `TextEdit` — font is inherited from the app theme; do not set it explicitly
- `CheckEdit.CheckedCheckBoxColor` requires `{dx:ThemeColor X}` not `{StaticResource X}`
- `DXCollectionView.SelectedItems` requires `IList` (non-generic) binding — use wrapper property
- **`AllowCascadeUpdate="True"` causes full list re-render on every `Reset` notification — confirmed ANR root cause (8,651 ms UI block).** `AllowCascadeUpdate` cascades item-level `INotifyPropertyChanged` events; our DTOs are `record` types (immutable), so it has zero benefit. `ObservableRangeCollection.ReplaceRange/ClearRange` fires `CollectionChanged(Reset)`, and with `AllowCascadeUpdate="True"` DX re-measures and re-renders every item. **Never set `AllowCascadeUpdate="True"` — omit it (default is `False`).**
- **`SelectedItems` — assign in code-behind only, no XAML binding.** `SelectedItems="{Binding ...}"` in XAML runs during `InitializeComponent` then is immediately overridden by the `OnAppearing` code-behind assignment, leaving a dangling MAUI binding listener. Remove the XAML attribute; assign only in `OnAppearing` using the `IList` wrapper property (e.g. `SelectedVenuesRaw`).
- `SwipeContainerItem.Command` binding is unreliable — always use the `Tap` event handler instead
- `SwipeContainer.FullSwipeMode="AllItems"` does NOT exist — valid values are `None`, `Start`, `End`, `Both`
- `DXCollectionView.IndicatorColor` defaults to invisible on dark themes — always set explicitly
- `ShimmerView` needs `await Task.Yield()` before data load so skeleton renders first
- `.NET MAUI 10`: `ContentPage` defaults to `SafeAreaEdges="None"` — add `SafeAreaEdges="Container"` explicitly
- Compiled bindings inside `x:DataType` DataTemplates: use a typed `ViewModel` property on the page, not `BindingContext.X`
- `DXCollectionView.Scrolled` event args type is `DXCollectionViewScrolledEventArgs` (NOT `CollectionViewScrolledEventArgs`); vertical offset property is `e.Offset` (NOT `e.VerticalOffset`):
  ```csharp
  private void OnCollectionViewScrolled(object sender, DXCollectionViewScrolledEventArgs e)
      => _viewModel.IsScrolled = e.Offset > 0;
  ```
