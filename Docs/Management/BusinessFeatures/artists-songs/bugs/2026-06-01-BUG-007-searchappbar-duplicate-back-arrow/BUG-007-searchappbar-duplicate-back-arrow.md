# BUG-007 — SearchAppBar Renders Duplicate Back Arrow in Picker Pages

**Filed:** 2026-06-11
**Feature area:** Artists & Songs — Picker pages (SongPickerPage, ArtistPickerPage, PersonPickerPage, QueueSongPickerPage)
**Severity:** Medium — visual defect; navigation is functional but the leading area shows two stacked back-arrow icons

---

## Symptom

When navigating to any picker page that uses `SearchAppBar` via `Shell.TitleView` (e.g., `SongPickerPage`,
`ArtistPickerPage`, `PersonPickerPage`, `QueueSongPickerPage`), two back-arrow icons appear side by side
in the leading position of the app bar:

- A **smaller, native-rendered arrow** injected by the MAUI Shell chrome because the page is on the
  navigation back-stack and `Shell.BackButtonBehavior` is not suppressed
- A **full-size `arrow_back_outlined` DXButton** rendered by `SearchAppBar`'s own `leadingButton` at
  `Grid.Column="0"` (hardcoded, always visible)

Both arrows are independently tappable. The Shell arrow calls `Shell.GoToAsync("..")` via the native
back-gesture; the `SearchAppBar` `leadingButton` calls `OnLeadingButtonClicked`, which clears
`SearchText`, unfocuses the input, and then calls `BackCommand.Execute(null)`. The result is two active
back controls stacked in the same 48 dp leading zone.

---

## Root Cause

### Primary — Shell chrome injects its own back button when `Shell.TitleView` is set without suppressing `BackButtonBehavior`

MAUI Shell does **not** automatically remove its native back-navigation chrome just because a custom
`Shell.TitleView` is provided. When a page is pushed onto the Shell navigation stack, the Shell chrome
layer renders its own back button (or Android system back affordance) at the leading edge of the
navigation bar area. `Shell.TitleView` replaces the **title content** only — it does not suppress
the Shell's navigation back button chrome.

`SearchAppBar.xaml` renders `leadingButton` at `Grid.Column="0"` as a hardcoded `DXButton` with
`Icon="arrow_back_outlined"` and no `IsVisible` binding — it is always rendered:

```xml
<!-- SearchAppBar.xaml — line 21–26 -->
<dx:DXButton Grid.Column="0"
             x:Name="leadingButton"
             Icon="arrow_back_outlined"
             Style="{StaticResource NavigationIconButton}"
             SemanticProperties.Description="Back"
             Clicked="OnLeadingButtonClicked" />
```

Because the Shell chrome also renders a back arrow at the same horizontal position, the two icons
overlap or appear adjacent depending on the platform's layout of `TitleView` content relative to the
Shell chrome leading area.

### Secondary — no `Shell.BackButtonBehavior` is set on picker pages

None of the four picker pages (`SongPickerPage`, `ArtistPickerPage`, `PersonPickerPage`,
`QueueSongPickerPage`) set `Shell.BackButtonBehavior` to suppress or override the Shell's native
back button. The correct pattern for a page that owns its own back button is:

```xml
<Shell.BackButtonBehavior>
    <BackButtonBehavior IsVisible="False" IsEnabled="False" />
</Shell.BackButtonBehavior>
```

Without this, Shell renders its default back chrome alongside the custom `TitleView` content.

---

## Affected Files

| File | Location | What is affected |
|------|----------|-----------------|
| `SongPickerPage.xaml` | `MyVocaList/UI/Pages/Songs/SongPickerPage.xaml` | Missing `Shell.BackButtonBehavior` |
| `ArtistPickerPage.xaml` | `MyVocaList/UI/Pages/Artists/ArtistPickerPage.xaml` | Missing `Shell.BackButtonBehavior` |
| `PersonPickerPage.xaml` | `MyVocaList/UI/Pages/Queue/PersonPickerPage.xaml` | Missing `Shell.BackButtonBehavior` |
| `QueueSongPickerPage.xaml` | `MyVocaList/UI/Pages/Queue/QueueSongPickerPage.xaml` | Missing `Shell.BackButtonBehavior` |

`SearchAppBar.xaml` and `SearchAppBar.xaml.cs` are **not** modified by this fix — the `leadingButton`
is correct by design (SearchAppBar is responsible for its own back affordance). The fix lives entirely
in the consuming pages.

---

## Fix Approach

Add `Shell.BackButtonBehavior` with `IsVisible="False" IsEnabled="False"` to every picker page that
uses `SearchAppBar` in `Shell.TitleView`. This suppresses the Shell chrome's native back button,
leaving `SearchAppBar`'s own `leadingButton` as the sole back affordance.

### Change per page (identical for all four pages)

```xml
<!-- Add inside <ContentPage ...> before <Shell.TitleView> -->
<Shell.BackButtonBehavior>
    <BackButtonBehavior IsVisible="False" IsEnabled="False" />
</Shell.BackButtonBehavior>
```

**Why `IsEnabled="False"` in addition to `IsVisible="False"`:**
`IsVisible="False"` hides the visual button on iOS but on Android the hardware/gesture back action
can still be intercepted by the Shell. `IsEnabled="False"` ensures the Shell does not handle the
back event, leaving `SearchAppBar`'s `BackCommand` (bound to the ViewModel's `BackCommand` →
`Shell.GoToAsync("..")`) as the sole handler.

### No changes to `SearchAppBar.xaml` or `SearchAppBar.xaml.cs`

`SearchAppBar`'s `leadingButton` must remain always visible and hardcoded — that is the MD3
"search-replaces-appbar" pattern: the back arrow is always present in a full-screen search view.
The fix is in the pages that host `SearchAppBar`, not in the component itself.

### Pages to edit (one XAML file each — follow incremental edit rule)

1. `MyVocaList/UI/Pages/Songs/SongPickerPage.xaml`
2. `MyVocaList/UI/Pages/Artists/ArtistPickerPage.xaml`
3. `MyVocaList/UI/Pages/Queue/PersonPickerPage.xaml`
4. `MyVocaList/UI/Pages/Queue/QueueSongPickerPage.xaml`

---

## Acceptance Criteria

| AC ID | Criterion | Verification |
|-------|-----------|-------------|
| AC-BUG007-01 | Given the user is on SongPickerPage (navigated from SongFormPage), When the page renders, Then exactly one back-arrow icon is visible in the app bar leading area | Manual test on emulator: navigate from SongFormPage → SongPickerPage, observe the app bar |
| AC-BUG007-02 | Given the user taps the single back arrow on SongPickerPage, Then the page navigates back to SongFormPage (no double-navigation, no crash) | Manual test: tap back arrow once, confirm one navigation step |
| AC-BUG007-03 | Given the user is on ArtistPickerPage, When the page renders, Then exactly one back-arrow icon is visible in the app bar | Manual test on emulator |
| AC-BUG007-04 | Given the user is on PersonPickerPage, When the page renders, Then exactly one back-arrow icon is visible | Manual test on emulator |
| AC-BUG007-05 | Given the user is on QueueSongPickerPage, When the page renders, Then exactly one back-arrow icon is visible | Manual test on emulator |
| AC-BUG007-06 | SearchAppBar's `leadingButton` DXButton remains always visible (not conditionally shown) — the component itself is not changed | Code review: `SearchAppBar.xaml` `leadingButton` has no `IsVisible` binding |

---

## Out of Scope for This Fix

- Any other pages that use `SmallAppBar` with `NavigationIcon` — those pages use `SmallAppBar.HasNavigationIcon`
  which is already conditional (`IsVisible="{Binding HasNavigationIcon}"`) and do not have this issue
- The Android hardware back-gesture behavior on pages that do NOT use `SearchAppBar` — those are unaffected
- Keyboard/accessibility back navigation — `IsEnabled="False"` on `BackButtonBehavior` is the correct
  approach per MAUI docs; no additional accessibility changes are needed
- `SearchAppBar` `Placeholder` auto-focus on `IsVisible` change — unrelated feature behavior, not a bug

---

## Related Files (read-only, not modified by fix)

- `MyVocaList/UI/Components/AppBars/SearchAppBar.xaml` — hardcoded `leadingButton` is correct; no change
- `MyVocaList/UI/Components/AppBars/SearchAppBar.xaml.cs` — `OnLeadingButtonClicked` is correct; no change
- `MyVocaList/UI/Components/AppBars/AppBarBase.cs` — base class; no change
- `MyVocaList/UI/Components/AppBars/SmallAppBar.xaml` — unaffected; uses conditional `HasNavigationIcon`
