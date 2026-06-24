# BUG-015 — ArtistsPage: Trailing Button Does Not Trigger

**Severity:** Major — artist catalog navigation via list-item button is non-functional  
**Discovered:** 2026-06-24 — emulator smoke test  
**Reporter:** Helder  
**Status:** Open

---

## Symptom

Tapping the trailing icon button on any row in `ArtistsPage` produces no visible response — the app does not navigate to the Songs catalog filtered for that artist.

## Expected

Tapping the trailing `queue_music_outlined` button navigates to `SongsPage` in catalog mode (`?artistId=...&artistName=...`), showing all songs performed by that artist.

## Suspected Root Cause

`ArtistsPage.xaml` binds the trailing button via:

```xml
Command="{Binding Source={RelativeSource AncestorType={x:Type vm:ArtistsViewModel}}, Path=ViewCatalogCommand}"
```

`RelativeSource AncestorType` walks the **visual element tree** for an element of the given type. `ArtistsViewModel` is not a visual element — it is the `BindingContext` of `ArtistsPage`. MAUI's RelativeSource cannot resolve a ViewModel type this way and silently fails to bind the command; every tap is a no-op.

This pattern may have worked before Step 7e migrated `ArtistsPage.xaml` to `CrudListView` (a `ContentView`), if the DataTemplate was previously in a context where x:Reference to the page was available. After migration the visual tree changed, making the already-fragile binding unresolvable.

**Files to inspect:**
- `MyVocaList/UI/Pages/Artists/ArtistsPage.xaml` — both `ItemTemplate` and `SelectedItemTemplate` trailing button bindings
- `MyVocaList/UI/ViewModels/ArtistsViewModel.cs` — `ViewCatalogCommand` / `NavigateToCatalog`

## Fix Direction

Replace the RelativeSource binding with a pattern that MAUI can resolve inside a `DataTemplate` inside a `ContentView`:

**Option A — x:Reference to the page:**
```xml
<!-- Add x:Name="artistsPage" to the ContentPage -->
Command="{Binding Source={x:Reference artistsPage}, Path=BindingContext.ViewCatalogCommand}"
```

**Option B — DevExpress CommandBindingContext on the DXCollectionView:**
Set `CommandBindingContext` on the `DXCollectionView` inside `CrudListView` to the page's `BindingContext`, then bind `Command="{Binding Path=ViewCatalogCommand, Source={...}}"` normally.

**Option C — Shell messaging / WeakReferenceMessenger:** Send a message from the `ListItem` code-behind instead of binding a command.

Verify fix by tapping the button and confirming navigation to `SongsPage` with `IsCatalogMode=true` for the selected artist.

## Regression Test

Integration: verify `ArtistsViewModel.ViewCatalogCommand` fires and calls `Shell.Current.GoToAsync` with correct route when invoked with a known `ArtistListItemDto`.
