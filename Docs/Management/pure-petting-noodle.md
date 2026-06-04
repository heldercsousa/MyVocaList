# UI Bug Fixes — Post-Artists-Songs Testing

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix four UX bugs discovered during Artists & Songs feature testing: missing back buttons on list pages, wrong flyout menu label, confusing search app-bar interaction, and non-functional autocomplete artist field on SongFormPage.

**Architecture:** All changes are contained to UI layer (XAML, code-behind, ViewModel) except the NavigationConfig rename (Navigation layer). No new services or migrations. The SearchAppBar behavior change aligns it with MD3's "search replaces app bar" pattern.

**Tech Stack:** .NET MAUI 10, DevExpress MAUI v25.2.4, CommunityToolkit.Mvvm, SmallAppBar / SearchAppBar custom components

---

## MD3 Research Findings (m3.material.io/components/search/guidelines)

For the **"search as secondary action"** pattern (trailing search icon → search replaces app bar):
- The leading icon in the search bar must be a **back arrow immediately** when search mode is active — not a search icon.
- MD3 spec: *"Focus is released when the back icon is selected"* — tapping the back arrow dismisses search (returns to normal app bar), it does NOT navigate away from the page.
- A leading "search icon" that transitions to "back arrow" only after focus is correct for **persistent inline search bars**, NOT for the "app bar swap" pattern used in this app.

This finding must be saved to `.claude/library/m3-components.md` (see Task 3).

---

## Issue Inventory

| # | Issue | Files affected |
|---|-------|---------------|
| 1 | No back button on Venues, People (Singers), Artists, Songs list pages | `VenuesViewModel.cs`, `PersonsViewModel.cs`, `ArtistsViewModel.cs`, `SongsViewModel.cs`, 4 XAML pages |
| 2 | Flyout menu says "People" — should be "Singers" | `NavigationConfig.cs` |
| 3 | SearchAppBar shows search icon in leading position (confusing), should show back arrow immediately | `SearchAppBar.xaml.cs`, `m3-components.md` |
| 4 | AutocompleteField on SongFormPage never shows suggestions; "Artist is required" when saving with typed text | `SongFormViewModel.cs` |

---

## Task 1 — Add back button to all CRUD list pages

**Files:**
- Modify: `MyVocaList/UI/ViewModels/VenuesViewModel.cs`
- Modify: `MyVocaList/UI/ViewModels/PersonsViewModel.cs`
- Modify: `MyVocaList/UI/ViewModels/ArtistsViewModel.cs`
- Modify: `MyVocaList/UI/ViewModels/SongsViewModel.cs`
- Modify: `MyVocaList/UI/Pages/Venues/VenuesPage.xaml`
- Modify: `MyVocaList/UI/Pages/People/PeoplePage.xaml`
- Modify: `MyVocaList/UI/Pages/Artists/ArtistsPage.xaml`
- Modify: `MyVocaList/UI/Pages/Songs/SongsPage.xaml`

**Context:**
- These pages are pushed via `Navigation.PushAsync` from the flyout menu.
- `Shell.BackButtonBehavior IsVisible=False` suppresses the Shell-rendered back button.
- `SmallAppBar` has `NavigationIcon` (string) and `NavigationCommand` (ICommand) BPs; `HasNavigationIcon` returns true when `NavigationIcon` is not empty.
- `CrudListPageBase.OnBackButtonPressed` handles multi-step back logic (confirm sheet → search mode → default pop).

**Pattern for each ViewModel:** Add a `GoBackCommand` that replicates the default back behavior (page pop). The ViewModel can use `Shell.Current.GoToAsync("..")` — this pattern is already used elsewhere (e.g., `SongFormViewModel.CancelAsync`).

- [ ] **Step 1: Add GoBackCommand to all four ViewModels**

In each of `VenuesViewModel.cs`, `PersonsViewModel.cs`, `ArtistsViewModel.cs`, `SongsViewModel.cs`, add:
```csharp
public IAsyncRelayCommand GoBackCommand { get; } = 
    new AsyncRelayCommand(() => Shell.Current.GoToAsync(".."));
```
Register it in the constructor (or as a field initializer — consistent with existing commands in each VM).

- [ ] **Step 2: Wire SmallAppBar in all four XAML pages**

For each page, add `NavigationIcon` and `NavigationCommand` to the existing `SmallAppBar` element:
```xml
<appbars:SmallAppBar
    x:Name="smallAppBar"
    Title="{Binding AppBarTitle}"
    NavigationIcon="arrow_back_outlined"
    NavigationCommand="{Binding GoBackCommand}"
    Action1Icon="search_outlined"
    Action1Command="{Binding OpenSearchCommand}"
    IsElevated="{Binding IsScrolled}"
    IsVisible="{Binding IsSearchMode, Converter={StaticResource InverseBoolConverter}}" />
```

- [ ] **Step 3: Build and confirm no errors**
```
dotnet build MyVocaList/MyVocaList.csproj
```
Expected: 0 errors, 0 warnings (except pre-existing).

- [ ] **Step 4: Commit**
```
git add MyVocaList/UI/ViewModels/VenuesViewModel.cs \
        MyVocaList/UI/ViewModels/PersonsViewModel.cs \
        MyVocaList/UI/ViewModels/ArtistsViewModel.cs \
        MyVocaList/UI/ViewModels/SongsViewModel.cs \
        MyVocaList/UI/Pages/Venues/VenuesPage.xaml \
        MyVocaList/UI/Pages/People/PeoplePage.xaml \
        MyVocaList/UI/Pages/Artists/ArtistsPage.xaml \
        MyVocaList/UI/Pages/Songs/SongsPage.xaml
git commit -m "fix: add back button to CRUD list pages (Venues, Singers, Artists, Songs)"
```

---

## Task 2 — Rename flyout menu item "People" → "Singers"

**Files:**
- Modify: `MyVocaList/Navigation/NavigationConfig.cs` line 27

- [ ] **Step 1: Change the label**

In `NavigationConfig.cs`, `BuildMenuGroups` method:
```csharp
// Before:
new MenuItemDescription("People", "group_outlined", Routes.People, navigateCommand),

// After:
new MenuItemDescription("Singers", "group_outlined", Routes.People, navigateCommand),
```

- [ ] **Step 2: Build and commit**
```
dotnet build MyVocaList/MyVocaList.csproj
git add MyVocaList/Navigation/NavigationConfig.cs
git commit -m "fix: rename flyout menu item People to Singers"
```

---

## Task 3 — Fix SearchAppBar leading icon — always show back arrow

**MD3 rationale:** In the "search icon replaces app bar" pattern, the leading icon must be `arrow_back_outlined` immediately when search mode is active. The current `search_outlined → arrow_back_outlined on focus` transition is correct for persistent inline search bars only. See MD3 research findings above.

**Files:**
- Modify: `MyVocaList/UI/Components/AppBars/SearchAppBar.xaml.cs`
- Modify: `.claude/library/m3-components.md` (knowledge registration)

- [ ] **Step 1: Simplify SearchAppBar leading icon logic**

In `SearchAppBar.xaml.cs`, replace the `UpdateLeadingIcon` method and the state logic:

```csharp
// Remove: private bool _isSearchFocused;
// Remove: UpdateLeadingIcon() method
// Remove: OnSearchEditFocused, OnSearchEditUnfocused handlers (or keep unfocused for backCommand only)
// Remove: SearchTextProperty.propertyChanged calling UpdateLeadingIcon

// Simplify OnLeadingButtonClicked:
private void OnLeadingButtonClicked(object sender, EventArgs e)
{
    SearchText = string.Empty;
    searchEdit.Unfocus();
    BackCommand?.Execute(null);
}
```

The leading button icon should be set statically to `arrow_back_outlined` in `SearchAppBar.xaml` (the XAML file), removing the `x:Name="leadingButton"` dynamic icon assignment.

Also update the XAML `leadingButton`:
```xml
<dx:DXButton x:Name="leadingButton"
             Icon="arrow_back_outlined"
             SemanticProperties.Description="Back"
             ... />
```

- [ ] **Step 2: Auto-focus search field when SearchAppBar becomes visible**

The user experience for "search replaces app bar" requires the search field to receive focus automatically when the SearchAppBar becomes visible, so the keyboard opens and the user can type immediately.

Add a `PropertyChanged` override or `IsVisible` trigger in `SearchAppBar.xaml.cs`:
```csharp
protected override void OnPropertyChanged(string propertyName = null)
{
    base.OnPropertyChanged(propertyName);
    if (propertyName == nameof(IsVisible) && IsVisible)
        searchEdit?.Focus();
}
```

- [ ] **Step 3: Register MD3 pattern knowledge in m3-components.md**

In `.claude/library/m3-components.md`, update the `## M3 Search App Bar` section to add a subsection:

```markdown
### Pattern: Search replaces app bar (secondary action via trailing icon)

**When:** A trailing search icon in SmallAppBar triggers IsSearchMode → SmallAppBar hides, SearchAppBar shows.

**MD3 rule (confirmed m3.material.io/components/search/guidelines):**
- Leading icon must be `arrow_back_outlined` **immediately** when SearchAppBar becomes visible — never `search_outlined`.
- "Focus is released when the back icon is selected" — tapping back dismisses search (returns to SmallAppBar), NOT page navigation.
- Auto-focus the text field when SearchAppBar becomes visible so the keyboard opens immediately.

**The `search → back on focus` transition** applies only to **persistent inline search bars** (always present, not replacing the app bar). Do not use it for the app-bar-swap pattern.
```

- [ ] **Step 4: Build and commit**
```
dotnet build MyVocaList/MyVocaList.csproj
git add MyVocaList/UI/Components/AppBars/SearchAppBar.xaml.cs \
        MyVocaList/UI/Components/AppBars/SearchAppBar.xaml \
        .claude/library/m3-components.md
git commit -m "fix: SearchAppBar always shows back arrow in leading position per MD3 search spec

Per m3.material.io/components/search/guidelines: in the 'search replaces app bar'
pattern the leading icon must be arrow_back immediately. Also auto-focuses the field
on show. Knowledge registered in m3-components.md."
```

---

## Task 4 — Fix AutocompleteField artist suggestions not appearing

**Root cause analysis:**
`SearchArtistsAsync` is an async method invoked via the `AutocompleteDebouncer`, which fires its callback from a background timer thread. After the `await _artistService.SearchArtistsByNameAsync(...)` call, the continuation runs on a thread-pool thread (no SynchronizationContext). Setting `ArtistSuggestions = ...` on a background thread raises `PropertyChanged` off the UI thread. MAUI requires UI mutations (including setting `CollectionView.ItemsSource` via binding) on the main thread. The result is a silent failure — the overlay card never becomes visible.

**Fix:** Wrap the suggestion assignment in `RunOnUiThread`.

**Secondary UX fix:** The error message "Artist is required" is misleading when the user typed an artist name but didn't select from suggestions. Change to "Search and select an artist from the list."

**Files:**
- Modify: `MyVocaList/UI/ViewModels/SongFormViewModel.cs`

- [ ] **Step 1: Wrap ArtistSuggestions assignment in RunOnUiThread**

In `SongFormViewModel.cs`, method `SearchArtistsAsync`:
```csharp
private async Task SearchArtistsAsync(string term)
{
    if (string.IsNullOrWhiteSpace(term)) { ArtistSuggestions = []; return; }
    var results = await _artistService.SearchArtistsByNameAsync(term, maxResults: 5);
    RunOnUiThread(() =>
        ArtistSuggestions = results
            .Select(a => new AutocompleteSuggestion(a.Name, a.CatalogCountText, a))
            .ToList());
}
```

- [ ] **Step 2: Improve the artist validation error message**

In `SongFormViewModel.SaveAsync`, the artist validation block:
```csharp
// Before:
ArtistErrorText = "Artist is required";

// After:
ArtistErrorText = string.IsNullOrWhiteSpace(ArtistSearchText)
    ? "Artist is required"
    : "Search and select an artist from the list";
```

- [ ] **Step 3: Build and confirm tests pass**
```
dotnet build MyVocaList/MyVocaList.csproj
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj
```

- [ ] **Step 4: Commit**
```
git add MyVocaList/UI/ViewModels/SongFormViewModel.cs
git commit -m "fix: artist autocomplete suggestions now appear (UI thread dispatch) and improve error message"
```

---

## Verification

After all tasks, deploy to Android emulator and verify:

1. **Back button** — navigate from flyout to Venues, Singers, Artists, Songs; confirm back arrow is visible in top-left and tapping it returns to the previous page.
2. **Flyout label** — open flyout drawer; confirm the Singers entry shows "Singers" not "People".
3. **Search interaction** — on any list page, tap the trailing search icon; confirm:
   - SearchAppBar appears with a back arrow (not search icon) in the leading position
   - Keyboard opens automatically
   - Typing shows search results
   - Tapping the back arrow clears text and returns to SmallAppBar (does NOT navigate away from the page)
4. **Artist autocomplete** — open SongFormPage (New Song); type 2+ characters of an artist name; confirm the suggestion overlay appears below the field; select a suggestion; confirm the field locks with the artist name; tap Save — confirm the song is saved without the "Artist" error.
