# Step 7c — Migrate PeoplePage.xaml to CrudListView

**Depends on:** Step 7b (VenuesPage migrated, build green)  
**Unblocks:** Step 7d (SongsPage migration)  
**Risk:** Low — one notable difference: leading content uses `ListItemLeadingMonogram` (initials) instead of an icon, and the support text shows `ParticipationsAbsencesNumber`

---

## Entity-Specific Details

| Property | Value |
|----------|-------|
| `ItemsSource` | `{Binding Persons}` |
| `SelectedItemsSource` | `{Binding SelectedPersonsRaw}` |
| `IsEmptyNoItems` | `{Binding IsEmptyNoPeople}` |
| `SearchPlaceholder` | `"Search people..."` |
| `EmptyNoItemsIllustration` | `"person_outlined"` |
| `EmptyNoItemsHeadline` | `"No person registered"` |
| `FabCommand` | `{Binding AddPersonCommand}` |
| `FabDescription` | `"Add person"` |
| `AppBarSubtitle` | not set |
| `FilterContent` | not set |
| `ItemTapCommand` | not set |

**Item template leading:** `ListItemLeadingMonogram` (uses `FullName` binding for initials)  
**Item template support text:** `{Binding ParticipationsAbsencesNumber}`  
**Item template trailing (unselected):** `CheckEdit IsChecked="False"`  
**Item template trailing (selected):** `CheckEdit IsChecked="True"` with `CheckedCheckBoxColor`

> Note: `ListItemLeadingMonogram` is an entity-specific choice inside the DataTemplate —
> it stays inside the DataTemplate passed to `CrudListView.ItemTemplate`. No special
> BindableProperty needed; it is purely a template detail.

---

## Files Owned

| Action | File |
|--------|------|
| Edit | `MyVocaList/UI/Pages/People/PeoplePage.xaml` |
| Edit | `MyVocaList/UI/Pages/People/PeoplePage.xaml.cs` |

---

## Tasks

- [x] **Edit `PeoplePage.xaml`**
  - Move ItemTemplate (with `ListItemLeadingMonogram` + `ParticipationsAbsencesNumber`) to keyed resource or inline in CrudListView
  - Move SelectedItemTemplate — same
  - Replace Grid body with `<views:CrudListView ...>` with all required BindableProperties
  - Keep Shell.BackButtonBehavior and Shell.TitleView unchanged
  - Add `xmlns:views` namespace

- [x] **Edit `PeoplePage.xaml.cs`**
  - Remove both event subscription lambdas from constructor (same as 7b)

- [x] **`dotnet build` — 0 errors**
- [x] **`dotnet test` — 0 failures**
- [ ] **Emulator smoke test**
  - People list loads with monogram initials visible
  - ParticipationsAbsencesNumber shown in support text
  - Select + delete confirmation works
  - FAB navigates to PersonFormPage

---

## Demo

> Open PeoplePage on emulator. Verify monogram initials are shown in list items. Select one
> person. Confirm delete confirmation sheet opens. Cancel. Verify person still in list.

---

## Review lane: Standard
