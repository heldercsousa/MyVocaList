# Tasks: venue profile — picture, address and map pick

Spec: `requirements.md`, `design.md`. Log: `task-log.md`.
Branch `feature/venue-profile` (worktree `.claude/worktrees/venue-profile`); the place-lookup tasks (V7a–V8)
use `feature/venue-map` (worktree `.claude/worktrees/venue-map`, branched from `feature/venue-profile` after
V1b) and are merged into `feature/venue-profile` before V5a. DRY Onion order: Contracts/Domain → Infra → Services → UI.
`MyVocaList.sln` and everything under `Docs/` are written by the orchestrator on `develop`, never by a task.

Markers: `[ ]` available · `[~]` claimed · `[x]` committed.

## Items

| Key | Backlog item | Tasks |
|-----|--------------|-------|
| PIC | `venues/changes/2026-11-14-venue-picture-leading-image-on-list-rows-add-replace-and-remove-in-the-form/` (this folder) | V0, V2, V2m, V3a, V3b, V4a, V4b, V5a, V5b, V9c, V10 |
| ADDR | `venues/changes/2026-11-14-venue-address-international-fields-duplicate-name-and-duplicate-address-refused/` | V1a, V1b, V2, V2m, V3a, V4a, V4b, V5a, V5b |
| MAP | `venues/changes/2026-11-14-venue-form-pick-the-place-on-a-map-to-fill-address-and-name/` | V7a, V7b, V7c, V8, V9a, V9b |

## Phase 0 — Spec
- [x] V0 Research (maps and geocoding providers and their terms, .NET MAUI `HybridWebView`), code map, requirements, design, backlog items, spec review.

## Phase 1 — Contracts, Domain, Infra
- [x] V1a **Contracts.** (REQ-VEN-10, 12, 26, 30) Size note: eight records/enums of one to three lines each, no logic, no tests (Level C).
  - Files owned: new `Contracts/DTOs/Venues/VenueAddress.cs`, `VenueField.cs`, `VenueSaveRequest.cs`, `VenueEditDto.cs`, `VenuePlace.cs`, `PlaceLookupStatus.cs`, `PlaceLookupResult.cs`, `PlaceSearchResult.cs`.
  - Demo: the solution builds with the new types.
- [~] V1b **Address and place rules.** After V1a. (REQ-VEN-13..16, 18, 24, 30, 33)
  - Files owned: new `Domain/Rules/VenueAddressRules.cs`, `Domain/Rules/VenuePlaceRules.cs`, new tests `MyVocaList.Tests/Unit/Domain/VenueAddressRulesTests.cs`, `VenuePlaceRulesTests.cs`.
  - Demo: `VenueAddressRules.Validate` of an address with only a country returns `(false, "Street address is required.", AddressLine1)`.
- [ ] V2 **Entity, EF configuration, venue image references.** After V1b. (REQ-VEN-01, 03, 22) Size note: two of the files change by one member each.
  - Files owned: `Domain/Entity/Venue.cs`, `Infra/EntityEFConfig/VenueConfiguration.cs`, `Domain/Rules/ImageOwnerKind.cs`, `Domain/Rules/ImageReferenceRules.cs`, new `MyVocaList.Tests/Unit/Domain/ImageReferenceRulesVenueTests.cs`, and the existing image-reference test that pins `songs` / `artists` as the only folders, if one exists (planned exception, `design.md § 7`).
  - Demo: `ImageReferenceRules.Normalize("local:venues/" + 32 hex)` returns the same value.
- [ ] V2m **Migration `AddVenueProfile`** (orchestrator: `dotnet ef migrations add`; generates the migration, its Designer file and `AppDbContextModelSnapshot.cs` — none of them hand-written). After V2.
- [ ] V3a **Repository and migration test.** After V2m. (REQ-VEN-04, 05, 20, 21)
  - Files owned: `Infra/Migrations/*_AddVenueProfile.cs` (hand adjustments to `Up`/`Down` only, per `design.md § 2.3`), `Domain/RepositoryInterface/IVenueRepository.cs`, `Infra/Repository/VenueRepository.cs`, new tests `Integration/Migrations/VenueProfileMigrationTests.cs`, `Integration/Repositories/VenueRepositoryAddressTests.cs`.
  - Demo: two venues with addresses equal but for case and accents → `AddressExistsAsync` is true.
- [ ] V3b **Image store and sweep know venues.** After V3a. (REQ-VEN-02, 04)
  - Files owned: `Services/ImageStore.cs`, `Services/ImageMaintenanceService.cs`, new tests `Unit/Services/ImageStoreVenueTests.cs`, `Unit/Services/ImageMaintenanceServiceVenueTests.cs`, constructor argument in existing `ImageMaintenanceService` tests (planned exception).
  - Demo: a file under `images/venues/` referenced by a venue survives the sweep; an unreferenced one older than 24 h is deleted.

## Phase 2 — Services
- [ ] V4a **`VenueService` save and read.** After V3b. (REQ-VEN-10, 11, 14..17, 19..22, 33, 36)
  - Files owned: `Domain/ServicesInterfaces/IVenueService.cs`, `Services/VenueService.cs`, new `MyVocaList.Tests/Unit/Services/VenueServiceSaveTests.cs`, planned exceptions in `VenueServiceTests.cs` (30 → 60 limit and counter thresholds).
  - Demo: `SaveVenueAsync` with a duplicate address returns `(false, "There is another venue registered at this address", AddressLine1)`.
- [ ] V4b **List DTO and mapper.** After V4a. (REQ-VEN-06, 18)
  - Files owned: `Contracts/DTOs/List/VenueListItemDto.cs`, `Services/Mappers/VenueMapper.cs`, new `MyVocaList.Tests/Unit/Services/VenueMapperTests.cs`.
  - Demo: a venue with street and city maps to `AddressSummary` "street, city" and carries `ImageUrl`.
- [ ] V7a **Nominatim provider.** After V1b, worktree `venue-map`. (REQ-VEN-34)
  - Files owned: new `Services/Places/IGeocodingProvider.cs`, `GeocodingRecord.cs`, `NominatimGeocodingProvider.cs`, new test `Unit/Services/Places/NominatimGeocodingProviderTests.cs`.
  - Demo: a stubbed reverse answer yields a `GeocodingRecord` with its `address` keys; an `error` body yields null; a 500 throws.
- [ ] V7b **Address mapper.** After V7a, worktree `venue-map`. (REQ-VEN-32)
  - Files owned: new `Services/Places/NominatimAddressMapper.cs`, new test `Unit/Services/Places/NominatimAddressMapperTests.cs`.
  - Demo: a Brazilian record with `road` "Rua Augusta" and `house_number` "100" maps to Street address "Rua Augusta, 100".
- [ ] V7c **Place lookup gate, service, DI.** After V7b, worktree `venue-map`. (REQ-VEN-25, 27, 28, 34) Size note: seven files; the constant and the DI test are one-line additions.
  - Files owned: new `Domain/ServicesInterfaces/IPlaceLookupService.cs`, `Services/Places/PlaceLookupGate.cs`, `Services/Places/PlaceLookupService.cs`, `Domain/Constants/SearchConstants.cs` (one constant), `MyVocaList/Extensions/ServiceCollectionExtensions.cs`, new tests `Unit/Services/Places/PlaceLookupGateTests.cs`, `PlaceLookupServiceTests.cs`, one added test in `Unit/DependencyInjection/AppServicesRegistrationTests.cs`.
  - Demo: two reverse lookups 100 ms apart are sent 1,000 ms apart with fake time; the same lookup twice sends one request.

## Phase 3 — UI
- [ ] V8 **`VenueMapPickerViewModel` and its plumbing.** After V7c, worktree `venue-map`. (REQ-VEN-24..31) Size note: seven files; four are a constant, a message record, a two-member interface and its `Preferences` wrapper, and one is an icon.
  - Files owned: new `MyVocaList/UI/ViewModels/VenueMapPickerViewModel.cs`, `MyVocaList/UI/Services/IMapPickerPreferences.cs`, `MauiMapPickerPreferences.cs`, `MyVocaList/UI/Messages/VenuePlacePickedMessage.cs`, `MyVocaList/Navigation/Routes.cs` (one constant), `MyVocaList/Resources/Images/map_outlined.svg` (if missing), new `Unit/ViewModels/VenueMapPickerViewModelTests.cs`.
  - Demo: a tap with the lookup stubbed to a known place sets the panel title to its name and the button to "Use this place".
- [ ] V5a **`VenueFormViewModel`.** After V4b and V8 (`venue-map` merged into `venue-profile`). (REQ-VEN-08..11, 14, 17, 19, 20, 23, 30, 31, 33)
  - Files owned: `MyVocaList/UI/ViewModels/VenueFormViewModel.cs`, new `Unit/ViewModels/VenueFormViewModelProfileTests.cs`, planned exceptions in `VenueFormViewModelTests.cs`.
  - Demo: Save with only Country filled sets the Street address error to "Street address is required."; a picked-place message fills the six fields.
- [ ] V5b **Venue pages.** After V5a. (REQ-VEN-06, 07, 09, 12, 18, 22, 23) One XAML file at a time: edit → build → next.
  - Files owned: `MyVocaList/UI/Pages/Venues/VenueFormPage.xaml`, `VenueFormPage.xaml.cs`, `MyVocaList/UI/Pages/Venues/VenuesPage.xaml`.
  - Demo: add mode shows the default venue image, "Add picture", the Address heading, "Pick on map" and six fields; list rows show the picture or the default image and "street, city".
- [ ] V9a **Map web assets.** After V8. (REQ-VEN-26, 34)
  - Files owned: new `MyVocaList/Resources/Raw/venuemap/*` (`index.html`, `map.js`, Leaflet 1.9.4 files, `LICENSE-leaflet.txt`).
  - Demo: `index.html` opened in a desktop browser shows the OpenStreetMap map with the attribution; a click logs a `tap` message.
- [ ] V9b **`VenueMapPickerPage` and registration.** After V5b and V9a; sole writer of `MauiProgram.cs` and `AppShell.xaml.cs` in this feature. (REQ-VEN-23..29, 34, 35)
  - Files owned: new `MyVocaList/UI/Pages/Venues/VenueMapPickerPage.xaml` + `.xaml.cs`, `MyVocaList/AppShell.xaml.cs`, `MyVocaList/MauiProgram.cs`, new `MyVocaList.Tests/Architecture/NoLocationPermissionTests.cs`.
  - Demo: Venue form → "Pick on map" → map shows → tap a street → panel shows the address → "Use this place" → the form fields are filled.

## Close-out
- [ ] V9c Merge, full test run, Android build, emulator check, verifier, traceability matrix, statuses, merge to `develop`.
- [ ] V10 **Device check (Helder)** — checklist in `task-log.md § Device check`.
