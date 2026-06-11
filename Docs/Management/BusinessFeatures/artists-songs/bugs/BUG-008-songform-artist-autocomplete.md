# BUG-008 — SongFormPage Artist Field Must Use AutocompleteField with Blur-Clear

**Filed:** 2026-06-11
**Feature area:** Artists & Songs — SongFormPage (Add and Edit modes)
**Severity:** Medium — artist field is already wired to `AutocompleteField` in the XAML, but
critical UX rules (blur-clear on no-selection, edit-mode pre-population, `IsArtistLocked` lock)
are missing from the ViewModel, leaving the field in a half-implemented state.

---

## Current Behavior vs Expected Behavior

| Aspect | Current behavior | Expected behavior |
|--------|-----------------|-------------------|
| XAML component | `AutocompleteField` is used (correct) | `AutocompleteField` — no change |
| User types and blurs without selecting | Text stays in the field; `SelectedArtistId` remains null; Save shows an error ("Search and select an artist from the list") but the typed text stays, misleading the user into thinking something was chosen | Field text is cleared on blur if no artist was selected from the dropdown; field reverts to empty placeholder |
| Edit mode: existing artist pre-population | `ArtistIdRaw` and `ArtistName` query parameters are received via `[QueryProperty]` and `OnArtistIdChanged` sets `SelectedArtistId` + `ArtistSearchText`; however the mapping uses `ArtistName` (the raw QueryProperty) not `SelectedArtistName`, so in some navigation paths the artist name may not display if `ArtistName` arrives after `ArtistId` | On page load in Edit mode, `ArtistSearchText` reliably shows the current artist name, and `SelectedArtistId` is set to the artist's id so Save succeeds without re-selecting |
| Edit mode: artist replacement | User can type a new artist name and select from dropdown; `SelectArtist` command correctly updates `SelectedArtistId` and `SelectedArtistName` | Same — this path is correct and must remain working |
| `IsArtistLocked` | Property exists in ViewModel and `IsEnabled` binding is present in XAML, but `IsArtistLocked` is never set to `true` anywhere in the ViewModel — the lock for API-imported artists is dead code | When a song is opened in Edit mode that originated from an API import (`Song.HasManualEdits == false`), `IsArtistLocked` should be `true`, disabling the field (as specced in `design.md`) |

---

## Root Cause

This is a **partially-implemented feature**, not a regression. The XAML integration of
`AutocompleteField` was completed correctly (component is wired, bindings are present). Three
ViewModel-side behaviors were never implemented:

1. **Blur-clear rule** — `AutocompleteField.OnSearchEditUnfocused` hides the overlay but does not
   call back to the ViewModel. The ViewModel must detect the blur (or use a dedicated
   `BlurredWithoutSelectionCommand`) to clear `ArtistSearchText` and `SelectedArtistId` when the
   user leaves the field without having tapped a suggestion.

2. **`IsArtistLocked` setter** — `OnSongIdChanged` triggers `LoadKaraokeUrlsAsync` but never sets
   `IsArtistLocked`. The lock must be derived from whether the song was externally imported
   (`HasManualEdits == false` and a non-null `ExternalId`). This requires loading the song in Edit
   mode to read those flags (currently not done — the form is populated only via query parameters).

3. **Edit mode load** — in Edit mode, the ViewModel receives `ArtistId` and `ArtistName` as query
   parameters from the calling page, so no service call is needed just for the artist. However the
   `IsArtistLocked` decision requires loading the full `Song` entity (to read `HasManualEdits` and
   `ExternalId`). This load is missing.

---

## AutocompleteField Component API (what exists, confirmed)

The component at `MyVocaList/UI/Components/AutocompleteField/AutocompleteField.xaml.cs` exposes:

| Member | Type | Notes |
|--------|------|-------|
| `Text` | `string` (TwoWay BindableProperty) | Mirrors what is in the `dxe:TextEdit` |
| `Suggestions` | `IEnumerable<AutocompleteSuggestion>` | Setting to `null`/empty hides the overlay |
| `SearchRequestedCommand` | `ICommand` | Fired after debounce when text length ≥ 2 |
| `SuggestionSelectedCommand` | `ICommand<AutocompleteSuggestion>` | Fired when user taps a suggestion row |
| `HasError` / `ErrorText` | `bool` / `string` | Delegates to inner `dxe:TextEdit` |
| `IsEnabled` | inherited `ContentView` property | Drives the `IsArtistLocked` binding |
| `OnSearchEditUnfocused` | internal event handler | Hides overlay, but does NOT call back to the ViewModel |

**Missing bindable property:** There is no `BlurredWithoutSelectionCommand` on the component. This
property must be added to `AutocompleteField` to notify the ViewModel that the user left the field
without selecting.

---

## Fix Approach

### File 1 — `AutocompleteField.xaml.cs`

Add a `BlurredWithoutSelectionCommand` bindable property (type `ICommand`). In
`OnSearchEditUnfocused`, after the `await Task.Yield()` guard, if `_isTappingSuggestion` is false
(i.e., blur happened without a tap), invoke `BlurredWithoutSelectionCommand?.Execute(null)` in
addition to hiding the overlay.

```csharp
// New BindableProperty
public static readonly BindableProperty BlurredWithoutSelectionCommandProperty =
    BindableProperty.Create(nameof(BlurredWithoutSelectionCommand), typeof(ICommand),
        typeof(AutocompleteField), null);

public ICommand BlurredWithoutSelectionCommand
{
    get => (ICommand)GetValue(BlurredWithoutSelectionCommandProperty);
    set => SetValue(BlurredWithoutSelectionCommandProperty, value);
}

// Updated OnSearchEditUnfocused
private async void OnSearchEditUnfocused(object sender, FocusEventArgs e)
{
    await Task.Yield();
    if (!_isTappingSuggestion)
    {
        overlayCard.IsVisible = false;
        BlurredWithoutSelectionCommand?.Execute(null);
    }
}
```

### File 2 — `SongFormPage.xaml`

Bind the new `BlurredWithoutSelectionCommand` on the `AutocompleteField` element:

```xml
<autocomplete:AutocompleteField
    LabelText="Artist"
    Placeholder="Search artists..."
    Text="{Binding ArtistSearchText, Mode=TwoWay}"
    Suggestions="{Binding ArtistSuggestions}"
    SearchRequestedCommand="{Binding SearchArtistsCommand}"
    SuggestionSelectedCommand="{Binding SelectArtistCommand}"
    BlurredWithoutSelectionCommand="{Binding ArtistBlurredWithoutSelectionCommand}"
    HasError="{Binding ArtistHasError}"
    ErrorText="{Binding ArtistErrorText}"
    IsEnabled="{Binding IsArtistLocked, Converter={StaticResource InverseBoolConverter}}" />
```

### File 3 — `SongFormViewModel.cs`

**3a. Add `ArtistBlurredWithoutSelectionCommand`** that clears the text field and the suggestions if
no artist is locked in:

```csharp
public IRelayCommand ArtistBlurredWithoutSelectionCommand { get; }

// In constructor:
ArtistBlurredWithoutSelectionCommand = new RelayCommand(OnArtistBlurredWithoutSelection);

// Implementation:
private void OnArtistBlurredWithoutSelection()
{
    // If the user blurred without selecting a valid artist, clear the field
    if (!SelectedArtistId.HasValue || SelectedArtistId.Value == 0)
    {
        ArtistSearchText = string.Empty;
        ArtistSuggestions = [];
    }
    // If an artist was already selected (editing or previously chosen), restore the name
    else
    {
        ArtistSearchText = SelectedArtistName ?? string.Empty;
        ArtistSuggestions = [];
    }
}
```

**3b. Add Edit mode song load** — when `SongId` is set in Edit mode, load the full `Song` to
determine `IsArtistLocked`. Add a call to `ISongService.GetSongByIdAsync` (or equivalent) inside
`OnSongIdChanged`, then set `IsArtistLocked = !song.HasManualEdits && song.ExternalId != null`.

> **Note:** If `ISongService` does not yet expose a `GetSongByIdAsync` method, add it to the
> interface and implement it before this fix — or read the `HasManualEdits` flag from the query
> parameter if the calling page passes it. Check `ISongService` before implementing.

**3c. Refine `OnArtistIdChanged`** — the existing handler sets `ArtistSearchText = ArtistName`
which relies on the `ArtistName` query property arriving after `ArtistId`. This is MAUI Shell
query-property order — it is not guaranteed. Consolidate both via an initialization method called
from `OnNavigatedTo` or `OnAppearing`:

```csharp
// Called from SongFormPage.OnAppearing after all query properties are set
public void InitializeArtistField()
{
    if (ArtistId > 0)
    {
        SelectedArtistId = ArtistId;
        SelectedArtistName = ArtistName;
        ArtistSearchText = ArtistName;
    }
}
```

Call `vm.InitializeArtistField()` from `SongFormPage.OnAppearing` after `vm.RefreshApiKeyFlagAsync()`.

### File 4 — `SongFormPage.xaml.cs`

Call `vm.InitializeArtistField()` in `OnAppearing`:

```csharp
protected override async void OnAppearing()
{
    base.OnAppearing();
    titleEdit.Focus();
    if (BindingContext is SongFormViewModel vm)
    {
        await vm.RefreshApiKeyFlagAsync();
        vm.InitializeArtistField();
    }
}
```

---

## Edit Mode: Pre-Population Rules

| Condition | `ArtistSearchText` | `SelectedArtistId` | `IsArtistLocked` |
|-----------|-------------------|-------------------|-----------------|
| Add mode (no `songId`) | `""` | `null` | `false` |
| Edit mode — manual song (`HasManualEdits == true` OR `ExternalId == null`) | Artist name from query param | Artist id from query param | `false` — user may change the artist |
| Edit mode — API-imported song (`HasManualEdits == false` AND `ExternalId != null`) | Artist name from query param | Artist id from query param | `true` — field disabled |

---

## Blur-Clear Behavior Rules

| User action | Resulting state |
|-------------|----------------|
| Types in field, taps a suggestion | `ArtistSearchText` = selected name; `SelectedArtistId` = selected id; overlay hidden |
| Types in field, blurs without tapping a suggestion (no prior selection) | `ArtistSearchText` cleared to `""`; `SelectedArtistId` = null; overlay hidden |
| Types in field, blurs without tapping a suggestion (prior selection exists — e.g., user started re-typing but gave up) | `ArtistSearchText` restored to `SelectedArtistName`; `SelectedArtistId` preserved; overlay hidden |
| Field is locked (`IsArtistLocked = true`) | Field is disabled; blur has no effect |

---

## Acceptance Criteria

| AC ID | Criterion | Format |
|-------|-----------|--------|
| AC-BUG008-01 | Given the Artist field is empty and the user types at least 2 characters, When the user blurs the field without selecting a suggestion, Then `ArtistSearchText` is cleared to empty and `SelectedArtistId` remains null | Given/When/Then |
| AC-BUG008-02 | Given the user has previously selected Artist A, and the user focuses the field and starts typing a new search term but then blurs without selecting, Then `ArtistSearchText` is restored to Artist A's name and `SelectedArtistId` remains Artist A's id | Given/When/Then |
| AC-BUG008-03 | Given the form is opened in Edit mode for a song with artist "Guns N' Roses", Then the Artist field shows "Guns N' Roses" on page load without any typing required | Given/When/Then |
| AC-BUG008-04 | Given the form is opened in Edit mode for a song created manually (not via API import), Then the Artist field is enabled and the user can clear it and select a different artist | Given/When/Then |
| AC-BUG008-05 | Given the form is opened in Edit mode for a song that was imported from an external API (`HasManualEdits == false` and `ExternalId != null`), Then the Artist field is disabled (read-only) | Given/When/Then |
| AC-BUG008-06 | Given the Artist field is empty and the user taps Save, Then the error message "Artist is required" appears and the field is not cleared | Given/When/Then |
| AC-BUG008-07 | Given the user typed a search term but did not select from the dropdown and then taps Save, Then the error "Search and select an artist from the list" appears and after dismissal the field is cleared (blur-clear applies on next focus loss) | Given/When/Then |

---

## Affected Files Summary

| File | Change type | What changes |
|------|-------------|-------------|
| `MyVocaList/UI/Components/AutocompleteField/AutocompleteField.xaml.cs` | Add | `BlurredWithoutSelectionCommand` bindable property; invoke it in `OnSearchEditUnfocused` |
| `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` | Edit | Add `BlurredWithoutSelectionCommand` binding to `AutocompleteField` element |
| `MyVocaList/UI/ViewModels/SongFormViewModel.cs` | Edit | Add `ArtistBlurredWithoutSelectionCommand`; add `InitializeArtistField()`; add `IsArtistLocked` setter logic in `OnSongIdChanged` |
| `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs` | Edit | Call `vm.InitializeArtistField()` in `OnAppearing` |
| `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` | Add | Tests for blur-clear rule, edit-mode pre-population, and lock state |

---

## Out of Scope for This Fix

- Adding `GetSongByIdAsync` to `ISongService` if it does not exist (check first; if missing, open a
  separate task)
- `ArtistFormPage` — this bug is scoped to `SongFormPage` only
- The `PersonFormPage` autocomplete field — separate feature, separate scope
- Changing the debounce delay or search threshold (both are working correctly)
- MAUI Shell query-property ordering changes — the `InitializeArtistField()` approach absorbs
  ordering uncertainty without requiring Shell changes
