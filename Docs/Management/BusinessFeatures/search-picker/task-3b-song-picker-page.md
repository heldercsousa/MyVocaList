# Agent Brief — Task 3b: SongPickerPage

**Feature:** Search Page Component  
**Phase:** 3b (second picker page — follows ArtistPickerPage pattern)  
**Prerequisite:** Task 3a committed and building (`ArtistPickerPage.xaml` exists at `MyVocaList/UI/Pages/Artists/ArtistPickerPage.xaml`)

---

## What you are building

A full-screen `ContentPage` named `SongPickerPage` that follows the exact same structure as `ArtistPickerPage`, with one difference in the results list: each item is a **two-line** `ListItem` showing song title (headline) and artist name (supporting text).

---

## Files to create

- `MyVocaList/UI/Pages/Songs/SongPickerPage.xaml`
- `MyVocaList/UI/Pages/Songs/SongPickerPage.xaml.cs`

Do not touch any other file.

---

## Start by reading the reference implementation

Read `MyVocaList/UI/Pages/Artists/ArtistPickerPage.xaml` in full. This is your XAML template. Copy its structure exactly, then apply the differences listed below.

Also read `MyVocaList/UI/ViewModels/SongPickerViewModel.cs` to confirm the ViewModel properties this page binds to.

---

## Differences from ArtistPickerPage

| Aspect | ArtistPickerPage | SongPickerPage |
|--------|-----------------|----------------|
| `x:Class` | `...Artists.ArtistPickerPage` | `...Songs.SongPickerPage` |
| `x:DataType` | `vm:ArtistPickerViewModel` | `vm:SongPickerViewModel` |
| `Placeholder` | `"Search artists..."` | `"Search songs..."` |
| `Action1Command` | `{Binding SearchCommand}` | `{Binding SearchCommand}` (same) |
| DataTemplate `x:DataType` | `dto:MusicSearchResultDto` | `dto:MusicSearchResultDto` (same) |
| ListItem Headline | `{Binding ArtistName}` | `{Binding SongTitle}` |
| ListItem SupportingText | *(none)* | `{Binding ArtistName}` |
| SelectResultCommand | `SelectResultCommand` | `SelectResultCommand` (same) |

Everything else is identical: `SafeAreaEdges="Container"`, `Shell.NavBarIsVisible="False"`, shimmer skeleton, `EmptyState` bound to `IsShowEmptyState` and `EmptyStateMessage`.

---

## ListItem two-line shape

`SongTitle` from `MusicSearchResultDto` may be null. MAUI Label renders null as empty string — no null guard needed in XAML.

Use the same `SupportingText` (or equivalent property name confirmed in 3a) to show `ArtistName` as the secondary line.

---

## Code-behind

```csharp
namespace MyVocaList.UI.Pages.Songs;

public partial class SongPickerPage : ContentPage
{
    public SongPickerPage(SongPickerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
```

---

## Build verification

Run: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`  
Expected: 0 errors.

---

## Commit

```bash
git add MyVocaList/UI/Pages/Songs/SongPickerPage.xaml
git add MyVocaList/UI/Pages/Songs/SongPickerPage.xaml.cs
git commit -m "feat: add SongPickerPage"
```

---

## Constitutional constraints

- `SafeAreaEdges="Container"` — mandatory
- English only
- Do NOT modify `SearchAppBar`, `ListItem`, `EmptyState`, or any shared component
- Build must pass before committing
