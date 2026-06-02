# Venues — Implementation Tasks

> **Status:** All tasks complete ✓
> **Last updated:** 2026-03-29

---

## Phase 1: Domain + Contracts

- [x] T-01: Add `Venue` entity (`Domain/Entity/Venue.cs`) — `Id`, `Name`, `Events` navigation
- [x] T-02: Add `VenueListItemDto` record (`Contracts/DTOs/List/VenueListItemDto.cs`)
- [x] T-03: Add `IVenueRepository` interface extending `IBaseRepository<Venue>` (`Domain/RepositoryInterface/`)
- [x] T-04: Add `IVenueService` interface (`Domain/ServicesInterfaces/IVenueService.cs`)

## Phase 2: Infrastructure

- [x] T-05: Implement `VenueRepository` in Infra — `GetPagedWithEventInfoAsync` uses EF projection for SQL COUNT
- [x] T-06: Add `Venues` DbSet to `AppDbContext`; configure `Name` max length via Fluent API
- [x] T-07: Add EF Core migration for `Venues` table
- [x] T-08: Register `IVenueRepository → VenueRepository` in `MauiProgram.cs` (`AddScoped`)

## Phase 3: Service

- [x] T-09: Implement `VenueService` — `ValidateNameInput`, `CreateVenueAsync`, `UpdateVenueAsync`, `DeleteVenuesAsync`, `GetPagedVenuesForListAsync`, character counter helpers
- [x] T-10: Register `IVenueService → VenueService` in `MauiProgram.cs` (`AddScoped`)

## Phase 4: UI — Form Page

- [x] T-11: Add `VenueFormPage.xaml` — `SafeAreaEdges="All"`, `ScrollView`, `TextEdit` with `MaxCharacterCount=30`, character counter label with triggers, inline `Cancel`+`Save` buttons
- [x] T-12: Add `VenueFormViewModel` — `[QueryProperty]` for `venueId`/`venueName`, `SaveCommand`, `CancelCommand`, validation → inline error, character counter update
- [x] T-13: Register route `Routes.VenueForm` in `AppShell.xaml.cs`
- [x] T-14: Register `VenueFormPage` + `VenueFormViewModel` as `AddTransient` in `MauiProgram.cs`

## Phase 5: UI — List Page (MD3 rebuild)

- [x] T-15: Add `VenuesViewModel` — always-on selection model, `IsSearchMode`/`IsScrolled`/`AppBarTitle`, paging + search + debounce, `FloatingToolbar` commands, confirm-delete BottomSheet state
- [x] T-16: Rebuild `VenuesPage.xaml` — `SmallAppBar`+`SearchAppBar` in Shell.TitleView, `ShimmerView`+`DXCollectionView`, `ListItem` rows with `ListItemLeadingIcon`+`CheckEdit`, `FloatingToolbar`, FAB, confirm BottomSheet
- [x] T-17: Update `VenuesPage.xaml.cs` — `OnCollectionViewScrolled`, `OnSelectionChanged`, `OnConfirmSheetStateChanged`, `OnBackButtonPressed` priority chain (confirm sheet → search → default), `SelectedItems` assigned in `OnAppearing`
- [x] T-18: Register `VenuesPage` + `VenuesViewModel` as `AddTransient` in `MauiProgram.cs`

## Phase 6: Fix-ups

- [x] T-19: Verify `InverseBoolConverter` exists and is registered in `App.xaml`
- [x] T-20: Add `OnBackButtonPressed` `IsSearchMode` guard (closes search on Android back)
- [x] T-21: Wire `Action2IsSelected`/`Action3IsSelected` on `FloatingToolbar` (Edit/Delete visual feedback)
- [x] T-22: Build verification — 0 errors
- [x] T-23: Push to remote

---

## Notes

- `DXCollectionView.Scrolled` event args type is `DXCollectionViewScrolledEventArgs` (not `CollectionViewScrolledEventArgs`); offset property is `e.Offset` (not `e.VerticalOffset`).
- `SelectionMode="Multiple"` is hardcoded in XAML — no ViewModel property needed; removing it from the ViewModel is cleaner.
- `SelectedItems` is assigned in code-behind `OnAppearing`, not via XAML binding, because `DXCollectionView.SelectedItems` requires a non-generic `IList`.
