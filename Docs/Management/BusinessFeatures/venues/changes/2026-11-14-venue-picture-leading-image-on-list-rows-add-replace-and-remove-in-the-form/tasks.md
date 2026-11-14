# Tasks: venue profile — picture, address and map pick

Spec: `requirements.md`, `design.md`. Log: `task-log.md`.
Branch `feature/venue-profile` (worktree `.claude/worktrees/venue-profile`); the place-lookup tasks use
`feature/venue-map` (worktree `.claude/worktrees/venue-map`, branched from `feature/venue-profile` after V1)
and are merged into `feature/venue-profile` before V5. DRY Onion order: Contracts/Domain → Infra → Services → UI.

Markers: `[ ]` available · `[~]` claimed · `[x]` committed.

## Items

| Key | Backlog item | Tasks |
|-----|--------------|-------|
| PIC | `venues/changes/2026-11-14-venue-picture-leading-image-on-list-rows-add-replace-and-remove-in-the-form/` (this folder) | V0, V2, V3, V4, V5, V6, V9c, V10 |
| ADDR | `venues/changes/2026-11-14-venue-address-international-fields-duplicate-name-and-duplicate-address-refused/` | V1, V2, V3, V4, V5, V6 |
| MAP | `venues/changes/2026-11-14-venue-form-pick-the-place-on-a-map-to-fill-address-and-name/` | V7a, V7b, V8, V9 |

## Phase 0 — Spec
- [x] V0 Research (maps and geocoding providers and their terms, .NET MAUI `HybridWebView`), code map, requirements, design, backlog items.

## Phase 1 — Contracts, Domain, Infra
- [ ] V1 **Contracts and address rules.** (REQ-VEN-13..16, 18, 24, 33)
  - Files owned: new `Contracts/DTOs/Venues/*.cs` (`VenueAddress`, `VenueField`, `VenueSaveRequest`, `VenueEditDto`, `VenuePlace`, `PlaceLookupStatus`, `PlaceLookupResult`, `PlaceSearchResult`), new `Domain/Rules/VenueAddressRules.cs`, new `MyVocaList.Tests/Unit/Domain/VenueAddressRulesTests.cs`.
  - Demo: `VenueAddressRules.Validate` of an address with only a country returns `(false, "Street address is required.", AddressLine1)`.
- [ ] V2 **Entity, EF configuration, venue image references.** After V1. (REQ-VEN-01, 03, 22)
  - Files owned: `Domain/Entity/Venue.cs`, `Infra/EntityEFConfig/VenueConfiguration.cs`, `Domain/Rules/ImageOwnerKind.cs`, `Domain/Rules/ImageReferenceRules.cs`, new `MyVocaList.Tests/Unit/Domain/ImageReferenceRulesVenueTests.cs`.
  - Demo: `ImageReferenceRules.Normalize("local:venues/" + 32 hex)` returns the same value.
- [ ] V2m **Migration `AddVenueProfile`** (main agent: `dotnet ef migrations add`; then the V3 implementor adjusts `Up`/`Down` per `design.md § 2.3`). After V2.
- [ ] V3 **Repository, image store, sweep, migration test.** After V2m. (REQ-VEN-02, 04, 05, 20, 21)
  - Files owned: `Infra/Migrations/*AddVenueProfile*` (hand adjustments only), `Domain/RepositoryInterface/IVenueRepository.cs`, `Infra/Repository/VenueRepository.cs`, `Services/ImageStore.cs`, `Services/ImageMaintenanceService.cs`, new tests `Integration/Migrations/VenueProfileMigrationTests.cs`, `Integration/Repositories/VenueRepositoryAddressTests.cs`, added tests for the venue folder in the image store / maintenance test files.
  - Demo: two venues with addresses equal but for case and accents → `AddressExistsAsync` is true.

## Phase 2 — Services
- [ ] V4 **`VenueService` save, read, mapper, list DTO.** After V3. (REQ-VEN-10, 11, 14..22)
  - Files owned: `Domain/ServicesInterfaces/IVenueService.cs`, `Services/VenueService.cs`, `Services/Mappers/VenueMapper.cs`, `Contracts/DTOs/List/VenueListItemDto.cs`, new `MyVocaList.Tests/Unit/Services/VenueServiceSaveTests.cs`, planned exceptions in `VenueServiceTests.cs` (`design.md § 7`).
  - Demo: `SaveVenueAsync` with a duplicate address returns `(false, "There is another venue registered at this address", AddressLine1)`.
- [ ] V7a **Nominatim provider and address mapper.** After V1, worktree `venue-map`. (REQ-VEN-32, 34)
  - Files owned: new `Services/Places/IGeocodingProvider.cs`, `GeocodingRecord.cs`, `NominatimGeocodingProvider.cs`, `NominatimAddressMapper.cs`, new tests `Unit/Services/Places/NominatimGeocodingProviderTests.cs`, `NominatimAddressMapperTests.cs`.
  - Demo: a Brazilian record with `road` "Rua Augusta" and `house_number` "100" maps to Street address "Rua Augusta, 100".
- [ ] V7b **Place lookup gate and service, DI.** After V7a, worktree `venue-map`. (REQ-VEN-25, 27, 28, 34)
  - Files owned: new `Domain/ServicesInterfaces/IPlaceLookupService.cs`, `Services/Places/PlaceLookupGate.cs`, `Services/Places/PlaceLookupService.cs`, `Domain/Constants/SearchConstants.cs` (one constant), `MyVocaList/Extensions/ServiceCollectionExtensions.cs`, new tests `PlaceLookupGateTests.cs`, `PlaceLookupServiceTests.cs`, one added test in `AppServicesRegistrationTests.cs`.
  - Demo: two reverse lookups 100 ms apart start 1,000 ms apart with fake time; the same lookup twice sends one request.

## Phase 3 — UI
- [ ] V8 **`VenueMapPickerViewModel`.** After V7b, worktree `venue-map`. (REQ-VEN-24..31)
  - Files owned: new `MyVocaList/UI/ViewModels/VenueMapPickerViewModel.cs`, `MyVocaList/UI/Services/IMapPickerPreferences.cs`, `MauiMapPickerPreferences.cs`, `MyVocaList/UI/Messages/VenuePlacePickedMessage.cs`, `MyVocaList/Navigation/Routes.cs` (one constant), new `Unit/ViewModels/VenueMapPickerViewModelTests.cs`.
  - Demo: a tap with the lookup stubbed to a known place sets the panel title to its name and "Use this place".
- [ ] V5 **`VenueFormViewModel` + `VenueFormPage.xaml`.** After V4 and V8 (merged). (REQ-VEN-07..12, 14, 17, 19, 20, 22, 23, 30, 31)
  - Files owned: `MyVocaList/UI/ViewModels/VenueFormViewModel.cs`, `MyVocaList/UI/Pages/Venues/VenueFormPage.xaml`, `VenueFormPage.xaml.cs`, new `Unit/ViewModels/VenueFormViewModelProfileTests.cs`, planned exceptions in `VenueFormViewModelTests.cs`, `component-safety-gate.md` consumer note if applicable.
  - Demo: add mode shows the default venue image and "Add picture"; Save with only Country filled shows "Street address is required." on Street address.
- [ ] V6 **Venues list rows.** After V4. (REQ-VEN-06, 18)
  - Files owned: `MyVocaList/UI/Pages/Venues/VenuesPage.xaml`.
  - Demo: rows show the venue picture or the default image and "Street address, City" under the name.
- [ ] V9 **`VenueMapPickerPage`, map assets, registration.** After V5. (REQ-VEN-23..29, 34, 35)
  - Files owned: new `MyVocaList/UI/Pages/Venues/VenueMapPickerPage.xaml` + `.xaml.cs`, new `MyVocaList/Resources/Raw/venuemap/*`, `MyVocaList/Resources/Images/map_outlined.svg` (if missing), `MyVocaList/AppShell.xaml.cs`, `MyVocaList/MauiProgram.cs`.
  - Demo: Venue form → "Pick on map" → map shows → tap a street → panel shows the address → "Use this place" → the form fields are filled.

## Close-out
- [ ] V9c Merge, full test run, Android build, verifier, traceability matrix, statuses, merge to `develop`.
- [ ] V10 **Device check (Helder)** — checklist in `task-log.md § Device check`.
