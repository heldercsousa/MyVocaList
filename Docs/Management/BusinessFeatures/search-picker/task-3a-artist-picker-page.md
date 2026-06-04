# Agent Brief — Task 3a: ArtistPickerPage

**Feature:** Search Page Component  
**Phase:** 3a (first picker page — establishes the XAML pattern)  
**Prerequisite:** Phase 2B committed (ArtistPickerViewModel, SongPickerViewModel, YouTubeSearchViewModel all exist and tests pass)

---

## What you are building

A full-screen `ContentPage` named `ArtistPickerPage` that:
- Shows a `SearchAppBar` in `Shell.TitleView` with an explicit search button (Action1)
- Shows a shimmer skeleton while searching
- Shows `ListItem` results (artist name only, single line) after search
- Shows `EmptyState` when search returned nothing or errored
- Sends `ArtistPickedMessage` via IMessenger when a result is tapped, then pops

**This page does NOT register routes or DI** — that is Phase 4.

---

## Files to create

- `MyVocaList/UI/Pages/Artists/ArtistPickerPage.xaml`
- `MyVocaList/UI/Pages/Artists/ArtistPickerPage.xaml.cs`

Do not touch any other file.

---

## Before writing XAML — read these first

1. `MyVocaList/UI/Components/AppBars/AppBarBase.cs` — confirms `Action1Icon` and `Action1Command` bindable properties
2. `MyVocaList/UI/Components/AppBars/SearchAppBar.xaml` — confirms `SearchText`, `BackCommand`, `Placeholder` bindable properties
3. `MyVocaList/UI/Components/Lists/ListItem.xaml` and `ListItem.xaml.cs` — find the exact bindable property names for: headline text, command, command parameter, leading icon, trailing icon
4. An existing search page such as `MyVocaList/UI/Pages/Artists/ArtistsPage.xaml` or `MyVocaList/UI/Pages/Songs/SongsPage.xaml` — see how `dx:ShimmerView` and `EmptyState` are used
5. `MyVocaList/UI/ViewModels/ArtistPickerViewModel.cs` — the ViewModel this page binds to; note: `IsShowEmptyState` property controls EmptyState visibility

---

## SearchAppBar wiring (no component modification allowed)

The `SearchAppBar` does not expose `SearchCommand` directly. Use the `Action1Command` slot from `AppBarBase`:

```xml
<Shell.TitleView>
    <appbars:SearchAppBar
        SearchText="{Binding SearchText, Mode=TwoWay}"
        BackCommand="{Binding BackCommand}"
        Action1Icon="search_outlined"
        Action1Command="{Binding SearchCommand}"
        Placeholder="Search artists..." />
</Shell.TitleView>
```

Do NOT modify `SearchAppBar.xaml` or `SearchAppBar.xaml.cs`.

---

## Page structure

```xml
<ContentPage
    ...
    x:DataType="vm:ArtistPickerViewModel"
    BackgroundColor="{StaticResource Surface}"
    SafeAreaEdges="Container"
    Shell.NavBarIsVisible="False">

    <Shell.TitleView>
        <!-- SearchAppBar as above -->
    </Shell.TitleView>

    <Grid>
        <!-- 1. Shimmer skeleton — visible while IsLoading -->
        <dx:ShimmerView IsActive="{Binding IsLoading}" IsVisible="{Binding IsLoading}">
            <!-- 5 BoxView skeleton rows, HeightRequest="56", CornerRadius="8" -->
        </dx:ShimmerView>

        <!-- 2. Results list — visible while HasResults -->
        <dx:DXCollectionView ItemsSource="{Binding Results}" IsVisible="{Binding HasResults}">
            <dx:DXCollectionView.ItemTemplate>
                <DataTemplate x:DataType="dto:MusicSearchResultDto">
                    <!-- ListItem: Headline=ArtistName, Command=SelectResultCommand, CommandParameter=. -->
                </DataTemplate>
            </dx:DXCollectionView.ItemTemplate>
        </dx:DXCollectionView>

        <!-- 3. EmptyState — visible when IsShowEmptyState -->
        <!-- Bind Message to EmptyStateMessage property -->
    </Grid>
</ContentPage>
```

**SafeAreaEdges="Container" is mandatory** — MAUI 10 breaking change, content renders behind status bar without it.

---

## ListItem — headline only (no leading, no trailing, no supporting)

`ArtistPickerPage` shows a single-line result: artist name only. The `ListItem` component has separate sub-components for leading/trailing slots. For this page, use only the headline binding — no leading icon, no trailing icon, no supporting text.

Read `ListItem.xaml.cs` to confirm the exact property name for the headline text (it may be `Headline`, `HeadlineText`, or similar).

---

## EmptyState visibility

The `ArtistPickerViewModel` exposes `IsShowEmptyState` (computed: `HasSearched && !HasResults && !IsLoading`). Bind directly:

```xml
<components:EmptyState
    Message="{Binding EmptyStateMessage}"
    IsVisible="{Binding IsShowEmptyState}" />
```

Read `EmptyState.xaml.cs` to confirm the property name for the message string.

---

## Code-behind

```csharp
namespace MyVocaList.UI.Pages.Artists;

public partial class ArtistPickerPage : ContentPage
{
    public ArtistPickerPage(ArtistPickerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
```

---

## Required xmlns declarations

```xml
xmlns:appbars="clr-namespace:MyVocaList.UI.Components.AppBars"
xmlns:lists="clr-namespace:MyVocaList.UI.Components.Lists"
xmlns:components="clr-namespace:MyVocaList.UI.Components"
xmlns:dto="clr-namespace:MyVocaList.Contracts.DTOs;assembly=MyVocaList.Contracts"
xmlns:vm="clr-namespace:MyVocaList.UI.ViewModels"
xmlns:dx="http://schemas.devexpress.com/maui"
```

Verify the `components` namespace against the `EmptyState` component's actual namespace (read its file).

---

## Build verification

Run: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`  
Expected: 0 errors. Fix all XAML binding and namespace errors before committing.

---

## Commit

```bash
git add MyVocaList/UI/Pages/Artists/ArtistPickerPage.xaml
git add MyVocaList/UI/Pages/Artists/ArtistPickerPage.xaml.cs
git commit -m "feat: add ArtistPickerPage (picker pattern reference implementation)"
```

---

## Constitutional constraints

- `SafeAreaEdges="Container"` — mandatory, no exceptions
- English only in all text, comments, bindings
- No `DisplayAlert`, `DisplayActionSheet`, `DisplayPromptAsync`
- DevExpress first: use `dx:DXCollectionView` for the results list, `dx:ShimmerView` for loading
- Do NOT modify `SearchAppBar`, `ListItem`, `EmptyState`, or any other shared component
- Incremental XAML: build after this file before any other XAML is touched

---

## Report back

- **Status:** DONE | DONE_WITH_CONCERNS | BLOCKED | NEEDS_CONTEXT
- Files changed
- Build result (exact command + pass/fail)
- Any property name discrepancies found (e.g. if `Headline` is actually `HeadlineText` in ListItem)
- Concerns if any
