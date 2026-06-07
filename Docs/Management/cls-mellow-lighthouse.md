# Plan: Hamburger vs Back Button — Flyout Root Pages

## Context

All four CRUD list pages (`VenuesPage`, `PeoplePage`, `SongsPage`, `ArtistsPage`) hardcode
`NavigationIcon="arrow_back_outlined"` on their `SmallAppBar`. This means they always render
a back-arrow in the leading AppBar slot — even when the user arrived via the hamburger/flyout menu,
where a hamburger icon (to reopen the drawer) should appear instead.

**Desired behavior:**
- Page reached from flyout → leading icon is a hamburger (`menu`), tapping reopens the flyout
- Page reached by push from another page → leading icon is back arrow (`arrow_back_outlined`), tapping pops the stack

**Root cause:** BUG-001 fixed `ArtistsPage` having *no* leading icon by hardcoding
`arrow_back_outlined`. That fix was then replicated to all CRUD pages via the
`CrudListViewModelBase` / `CrudListPageBase` migration, making it universal.

No existing BACKLOG entry covers this pattern. A new entry must be registered.

---

## BACKLOG entry to register

Add to `Docs/Management/BACKLOG.md` under **Dev Cycle Craft** (navigation pattern rule):

```
| 2026-06 | **Bug: Flyout root pages show back button instead of hamburger icon** | 💡 Pending | All 4 CRUD list pages hardcode `arrow_back_outlined` on SmallAppBar — shows back button even when reached from flyout. Fix: dynamic leading icon based on navigation context. See plan `cls-mellow-lighthouse.md`. |
```

---

## Fix design

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

## Commit message pattern (bug fix — spec-exempt)

```
fix: flyout root pages — show hamburger icon when accessed from menu, back button when pushed

Root cause: NavigationIcon="arrow_back_outlined" was hardcoded on all 4 CRUD list pages; no
distinction between flyout vs push navigation context.
Fix: CrudListPageBase.OnNavigatedTo sets AppBarNavigationIcon + AppBarNavigationCommand
dynamically based on Shell.Navigation.NavigationStack.Count.
Regression risk: Low — ArtistsPage push-from-SongsPage path still shows back arrow correctly.
```
