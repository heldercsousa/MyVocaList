# Plan: Navigation Icon Pattern — Root Pages vs Pushed Pages

## Context

**Pattern definition (not a bug):**
- **Root flyout pages** (reached via hamburger menu) → leading icon = hamburger (`menu`)
  - Tapping hamburger reopens the drawer (`Shell.Current.FlyoutIsPresented = true`)
  - Allows re-access to navigation menu from the root page
  
- **Pushed detail pages** (navigated from root or another page) → leading icon = back arrow (`arrow_back_outlined`)
  - Tapping back arrow pops the navigation stack (`Shell.Current.GoToAsync("..")`)
  - Provides depth cueing and normal back navigation

**Current state:**
All four CRUD list pages (`VenuesPage`, `PeoplePage`, `SongsPage`, `ArtistsPage`) hardcode 
`NavigationIcon="arrow_back_outlined"` on their `SmallAppBar`, so they always show back arrow 
regardless of how they were reached.

**Why this matters:**
BUG-001 (2026-06-03) fixed `ArtistsPage` having *no* leading icon by hardcoding back arrow.
That fix was then replicated to all CRUD pages during the `CrudListViewModelBase` / `CrudListPageBase` 
migration, making it a universal pattern. Now that the migration is stable, we can upgrade this 
pattern to be context-aware.

---

## BACKLOG Status

✅ Registered in `Docs/Management/BACKLOG.md` under **Dev Cycle Craft**:
- Status: `🟢 Ready` (plan approved, ready for implementation)
- Entry: "Navigation Icon Pattern — Root Pages vs Pushed Pages"

---

## Fix Design

### Approach: context-aware icon in `CrudListViewModelBase<TItem>` + `CrudListPageBase`

Because all 4 pages already share `CrudListViewModelBase<TItem>` and `CrudListPageBase`,
the fix can be centralized there.

**Step 1 — Add observable properties to `CrudListViewModelBase<TItem>`**

```csharp
[ObservableProperty] private string _appBarNavigationIcon = "arrow_back_outlined";
[ObservableProperty] private ICommand _appBarNavigationCommand;
```

Initialize `_appBarNavigationCommand` in the constructor to a no-op (will be set by the page
on `OnNavigatedTo`).

**Step 2 — Override `OnNavigatedTo` in `CrudListPageBase`**

```csharp
protected override void OnNavigatedTo(NavigatedToEventArgs args)
{
    base.OnNavigatedTo(args);
    var isRootPage = Shell.Current?.Navigation?.NavigationStack?.Count <= 1;
    if (ViewModel is ICrudListViewModel vm)
    {
        if (isRootPage)
        {
            vm.AppBarNavigationIcon = "menu";
            vm.AppBarNavigationCommand = new Command(
                () => Shell.Current.FlyoutIsPresented = true);
        }
        else
        {
            vm.AppBarNavigationIcon = "arrow_back_outlined";
            vm.AppBarNavigationCommand = new Command(
                async () => await Shell.Current.GoToAsync(".."));
        }
    }
}
```

**Step 3 — Update SmallAppBar bindings on all 4 XAML pages**

Change from hardcoded:
```xml
NavigationIcon="arrow_back_outlined"
NavigationCommand="{Binding GoBackCommand}"
```

To bound:
```xml
NavigationIcon="{Binding AppBarNavigationIcon}"
NavigationCommand="{Binding AppBarNavigationCommand}"
```

**Step 4 — Remove now-unused `GoBackCommand` from `ArtistsViewModel`**

`GoBackCommand` was added solely for BUG-001. After this fix the command is replaced by
the dynamic `AppBarNavigationCommand`. Remove it from `ArtistsViewModel.cs`.

---

## Files to modify

| File | Change |
|------|--------|
| `MyVocaList/UI/Base/CrudListViewModelBase.cs` | Add `AppBarNavigationIcon` + `AppBarNavigationCommand` observable properties |
| `MyVocaList/UI/Base/CrudListPageBase.cs` | Override `OnNavigatedTo` to set icon + command based on stack depth |
| `MyVocaList/UI/Pages/VenuesPage.xaml` | Bind SmallAppBar `NavigationIcon` + `NavigationCommand` to VM properties |
| `MyVocaList/UI/Pages/PeoplePage.xaml` | Same |
| `MyVocaList/UI/Pages/SongsPage.xaml` | Same |
| `MyVocaList/UI/Pages/ArtistsPage.xaml` | Same |
| `MyVocaList/UI/ViewModels/ArtistsViewModel.cs` | Remove `GoBackCommand` |
| `Docs/Management/BACKLOG.md` | Register new bug entry |

---

## Edge case — ArtistsPage accessed from SongsPage

`ArtistsPage` can be pushed from `SongsPage` via `ViewCatalogCommand`. In that case
`NavigationStack.Count > 1`, so the back-arrow is correctly shown. No special handling needed.

---

## Verification

1. Launch app on emulator.
2. Tap any flyout item (e.g. Venues) — leading AppBar icon must be hamburger; tapping it must open the flyout.
3. Navigate into a form (e.g. tap a venue row or FAB) — back-arrow appears on form page.
4. Press back — returns to list; leading icon reverts to hamburger.
5. From SongsPage, tap "View Catalog" → ArtistsPage opens with back arrow (pushed context).
6. Tap back → returns to SongsPage.

---

## Commit message pattern (pattern standardization — spec-exempt)

```
feat: navigation icon pattern — context-aware hamburger vs back button on CRUD list pages

Pattern rule: Root flyout pages show hamburger icon (opens drawer); pushed pages show back arrow 
(pops stack). Currently all 4 CRUD pages hardcode back arrow.
Implementation: CrudListPageBase.OnNavigatedTo sets AppBarNavigationIcon + AppBarNavigationCommand 
dynamically based on Shell.Navigation.NavigationStack.Count (≤1 = root/hamburger, >1 = pushed/back).
Regression risk: Low — ArtistsPage push-from-SongsPage path continues to show back arrow correctly.
```
