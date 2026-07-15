# CRUD Page Design Laws — Confirm-Delete BottomSheet, Shimmer, DI Registration, Empty State

> Section file split from `crud-pages.md` on 2026-07-14 (token-scoped reads). Index + never-miss rules: `crud-pages.md`.

## Confirm-Delete BottomSheet

> **Note:** As of Step 7, the confirm BottomSheet lives inside `CrudListView`. Do not add a
> `dx:BottomSheet` to page XAML. The ViewModel properties `ConfirmSheetState`, `ConfirmMessage`,
> `ConfirmActionText`, `ConfirmActionCommand`, and `DismissConfirmCommand` are still required on
> the ViewModel — CrudListView binds them in its internal XAML directly via `BindingContext`
> (runtime binding — these are NOT on `ICrudListViewModel`; they resolve through the concrete VM).

Use the standard confirm BottomSheet for any destructive action. `HalfExpandedRatio=0.28` for single-message confirmations.

The VM holds `ConfirmSheetState`, `ConfirmMessage`, `ConfirmActionText`, `ConfirmActionCommand`, `DismissConfirmCommand`. CrudListView observes `ConfirmSheetState` via `ICrudListViewModel.PropertyChanged` to open/close the sheet internally — do not wire this in the page code-behind. The remaining four properties (`ConfirmMessage`, etc.) are bound by CrudListView's internal XAML via BindingContext and do not require the interface.

See `dialogs-validation.md` for the XAML snippet (internal to CrudListView — do not copy to page XAML).

---

## Shimmer Skeleton

> **Note:** As of Step 7, the shimmer skeleton is internal to `CrudListView`. Do not add
> `ShimmerView` or `SkeletonBone` elements to page XAML. The ViewModel `IsInitialLoading`
> property is still required — CrudListView binds it directly in its internal XAML via
> `BindingContext` (runtime binding — `IsInitialLoading` is NOT on `ICrudListViewModel`;
> it resolves through the concrete VM type at runtime).

Skeleton bones match the `ListItem` height: `HeightRequest="56"`, `CornerRadius="0"`, `Margin="0,1"` (1dp separator gap). Use 6 bones. Apply the `SkeletonBone` named style — no inline props needed (internal reference):

```xml
<dx:DXBorder Style="{StaticResource SkeletonBone}" />
```

Always `await Task.Yield()` before the first data fetch in `InitializeAsync()` so the shimmer renders before the load begins.

---

## DI Registration

| Type | Lifetime | Reason |
|------|----------|--------|
| List page | `AddTransient` | Fresh instance per navigation |
| Form page | `AddTransient` | Fresh instance per navigation |
| List ViewModel | `AddTransient` | Fresh instance per navigation |
| Form ViewModel | `AddTransient` | Fresh instance per navigation |
| Service | `AddScoped` | Per-lifetime scope |
| Repository | `AddScoped` | Per-lifetime scope |

---

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

Two standard ViewModel properties drive visibility:
- `IsEmptyNoItems` — no records exist at all
- `IsEmptyNoResults` — search returned no matches
