# Agent Brief — Task 3c: YouTubeSearchPage

**Feature:** Search Page Component  
**Phase:** 3c (third picker page — follows ArtistPickerPage pattern, adds thumbnail image)  
**Prerequisite:** Tasks 3a and 3b committed and building

---

## What you are building

A full-screen `ContentPage` named `YouTubeSearchPage` that follows the same structure as `ArtistPickerPage`, with these differences:
- Binds to `YouTubeSearchViewModel` (results are `YouTubeSearchResultDto`, not `MusicSearchResultDto`)
- Each list item shows a **48×48 thumbnail image** in the leading slot, video title as headline, and channel name as supporting text

---

## Files to create

- `MyVocaList/UI/Pages/Songs/YouTubeSearchPage.xaml`
- `MyVocaList/UI/Pages/Songs/YouTubeSearchPage.xaml.cs`

Do not touch any other file.

---

## Start by reading the reference implementation

Read `MyVocaList/UI/Pages/Artists/ArtistPickerPage.xaml` in full. This is your XAML template.

Also read:
- `MyVocaList/UI/ViewModels/YouTubeSearchViewModel.cs` — ViewModel binding source
- `Contracts/DTOs/List/YouTubeSearchResultDto.cs` — confirms properties: `VideoId`, `Title`, `ChannelName`, `DurationSeconds` (int?), `ThumbnailUrl`
- `MyVocaList/UI/Components/Lists/ListItemLeadingImage.xaml` and `.xaml.cs` — check if this component exists and what properties it exposes (WidthRequest, HeightRequest, ImageSource or similar)
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` lines 218–252 — existing YouTube result rows that use thumbnail images (reference for how images were rendered before)

---

## Differences from ArtistPickerPage

| Aspect | ArtistPickerPage | YouTubeSearchPage |
|--------|-----------------|-------------------|
| `x:Class` | `...Artists.ArtistPickerPage` | `...Songs.YouTubeSearchPage` |
| `x:DataType` | `vm:ArtistPickerViewModel` | `vm:YouTubeSearchViewModel` |
| `Placeholder` | `"Search artists..."` | `"Search YouTube karaoke..."` |
| DataTemplate `x:DataType` | `dto:MusicSearchResultDto` | `dtoList:YouTubeSearchResultDto` |
| ListItem Headline | `{Binding ArtistName}` | `{Binding Title}` |
| ListItem SupportingText | *(none)* | `{Binding ChannelName}` |
| ListItem leading slot | *(none)* | 48×48 image, `ThumbnailUrl`, `AspectFill` |
| SelectResultCommand | `SelectResultCommand` | `SelectResultCommand` (same) |

xmlns for `dtoList`: `xmlns:dtoList="clr-namespace:MyVocaList.Contracts.DTOs.List;assembly=MyVocaList.Contracts"`

---

## ListItem leading image

**Option A — if `ListItem` supports a `LeadingContent` ContentView slot:**
```xml
<lists:ListItem Headline="{Binding Title}" SupportingText="{Binding ChannelName}" ...>
    <lists:ListItem.LeadingContent>
        <Image Source="{Binding ThumbnailUrl}" WidthRequest="48" HeightRequest="48" Aspect="AspectFill" />
    </lists:ListItem.LeadingContent>
</lists:ListItem>
```

**Option B — if `ListItemLeadingImage` is the correct sub-component:**
Use `ListItemLeadingImage` as the template content wrapper. Read its API before deciding.

**Option C — if neither works:** Use a `Grid` template inside `DXCollectionView.ItemTemplate` with a 48×48 Image + two Labels — same as the pre-existing pattern in `SongFormPage.xaml` lines 222–251. This is the fallback; prefer Options A or B if available.

Read `ListItem.xaml` and `ListItemLeadingImage.xaml` to decide which option to use. Document your choice in the report.

---

## DurationSeconds formatting

The spec calls for `ChannelName + " · " + formatted duration` as supporting text. The `SecondsToMinutesConverter` exists in the project (used in `SongFormPage.xaml` line 237). However, for the supporting text binding, simply bind `ChannelName` alone — a multi-value binding for the combined string is out of scope. The duration can be omitted from the ListItem supporting text unless the project has a `StringFormatConverter` or similar that makes it trivial. Keep it simple.

---

## Code-behind

```csharp
namespace MyVocaList.UI.Pages.Songs;

public partial class YouTubeSearchPage : ContentPage
{
    public YouTubeSearchPage(YouTubeSearchViewModel viewModel)
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
git add MyVocaList/UI/Pages/Songs/YouTubeSearchPage.xaml
git add MyVocaList/UI/Pages/Songs/YouTubeSearchPage.xaml.cs
git commit -m "feat: add YouTubeSearchPage"
```

---

## Constitutional constraints

- `SafeAreaEdges="Container"` — mandatory
- English only
- Do NOT modify `SearchAppBar`, `ListItem`, `ListItemLeadingImage`, `EmptyState`, or any shared component
- Build must pass before committing
- Report which leading image option (A/B/C) was used and why
