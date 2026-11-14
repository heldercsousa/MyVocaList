# Task log: venue profile — picture, address and map pick

### Checkpoint
- 2026-11-14 — Complete, including Phase 6 (venue picture from a map pick). Merged to `develop` (46e41532): 1652 tests pass, Android build 0 errors, all flows checked on the Android emulator (two "Emulator check" entries). The checklist at the end stays as an optional pass on a physical phone.
- Context manifest: `requirements.md`, `design.md`, `tasks.md` (this folder).

## 2026-11-14 — V0 Research and spec (Done)

- **Google Maps / Places rejected.** Places policy (`developers.google.com/maps/documentation/places/web-service/policies`,
  read 2026-11-14): "Don't pre-fetch, index, store, or redistribute Google Maps Content"; do not "copy and save
  business names, addresses, or user reviews"; only the place ID may be stored. The .NET MAUI `Map` control on
  Android needs a Google Maps API key in the manifest (Microsoft Learn, .NET MAUI 10 `Map` page).
- **OpenStreetMap chosen.** Nominatim usage policy (`operations.osmfoundation.org/policies/nominatim`, read
  2026-11-14): at most 1 request per second, identifying `User-Agent`, results cached, no autocomplete,
  attribution, requests triggered by the end user. Tile usage policy (`…/policies/tiles`): distinct
  `User-Agent` naming the app, no bulk or offline download, visible attribution.
- **Logo from the map: not possible** (OpenStreetMap has none; Google forbids storing photos).
- **`HybridWebView`** (.NET MAUI 10, Microsoft Learn): web content under `Resources/Raw/<HybridRoot>`,
  `SendRawMessage` / `RawMessageReceived` for messaging.
- Code map: `Venue` has only `Id`, `Name`; no DB uniqueness on the name (service check under `NOCASE_NOACCENT`);
  the form receives the name by query string and has no read-by-id; `ImageOwnerKind`, `ImageReferenceRules`,
  `ImageStore` and `ImageMaintenanceService` know only songs and artists; the DEBUG seed inserts 504 venues
  with possibly repeated names; no map, location or WebView code exists.
- The tasks file doubles as the plan (files owned + demo per task); no separate `plan.md`.

## 2026-11-14 — V1a Contracts (Done)

### Changed files
- `Contracts/DTOs/Venues/` (new): `VenueAddress.cs`, `VenueField.cs`, `VenueSaveRequest.cs`, `VenueEditDto.cs`, `VenuePlace.cs`, `PlaceLookupStatus.cs`, `PlaceLookupResult.cs`, `PlaceSearchResult.cs`.

### Verification evidence
- Commit `feb9ea48` on `feature/venue-profile`. Android build 0 errors; `dotnet test` 1277 passed, 0 failed (baseline). Orchestrator re-check: diff holds only the eight files; Contracts project builds.
- Level C (records and enums, no logic): no unit test. Namespace `MyVocaList.Contracts.DTOs.Venues`.

## 2026-11-14 — V1b Address and place rules (Done)

### Changed files
- New `Domain/Rules/VenueAddressRules.cs`, `Domain/Rules/VenuePlaceRules.cs` (holds `MapView`)
- New tests `Unit/Domain/VenueAddressRulesTests.cs`, `Unit/Domain/VenuePlaceRulesTests.cs` (53 cases)

### Verification evidence
- Commit `983c4723` on `feature/venue-profile`. Red: test project failed to compile on the missing types. Android build 0 errors; `dotnet test` 1330 passed, 0 failed.
- Orchestrator re-check: diff holds only the four files; rule tests 53 passed; `VenueAddressRules` read against REQ-VEN-13..16 (order, messages, exact maximum valid).

### Traceability
| AC | Criterion | Implementation | Tests |
|----|-----------|----------------|-------|
| REQ-VEN-13 | empty address is valid | `VenueAddressRules.IsEmpty`, `Validate` | `IsEmpty_*`, `Validate_EmptyAddress_IsValid` |
| REQ-VEN-14 | street and city required, street first | `Validate` | `Validate_OnlyCountry_*`, `Validate_BlankCity_*`, `Validate_BlankLine1AndBlankCity_ReportsLine1First` |
| REQ-VEN-15 | maximum lengths and message | `Validate` | `Validate_EveryFieldAtMaximum_IsValid`, `Validate_OneCharacterOverMaximum_ReportsTooLong` (6) |
| REQ-VEN-16 | trim, blank → null | `Normalize` | `Normalize_*` (3) |
| REQ-VEN-18 | "street, city" summary | `Summary` | `Summary_*` |
| REQ-VEN-24 | search text; first view priority | `ToSearchText`, `VenuePlaceRules.FirstView` | `ToSearchText_*` (3), `FirstView_*` (4) |
| REQ-VEN-30 | merge of a picked place | `VenuePlaceRules.Merge` | `Merge_*` (7) |
| REQ-VEN-33 | coordinate pair and range | `NormalizeCoordinates` | `NormalizeCoordinates_*` |

### Decisions made in the task (kept)
- Length checks use the trimmed length. `Merge` trims the place name and returns "" when both names are blank; a null place address keeps the form's address. Zoom constants are public (`PlaceZoom` 17, `LastPlaceZoom` 15, `WorldZoom` 2).

## 2026-11-14 — V2 Entity, EF configuration, venue image references + V2m migration (Done)

### Changed files
- `Domain/Entity/Venue.cs` (nine nullable members), `Infra/EntityEFConfig/VenueConfiguration.cs` (Name 60; address columns with default collation and `TrimValueConverters.Optional`; coordinates REAL; `ImageUrl` as on Song/Artist; no index)
- `Domain/Rules/ImageOwnerKind.cs` (`Venue`), `Domain/Rules/ImageReferenceRules.cs` (`VenuesFolder`, `public static IReadOnlyList<string> Folders`, explicit `FolderOf`, parser loops over `Folders`)
- New `Infra/Migrations/20261114133332_AddVenueProfile.cs` + Designer, `AppDbContextModelSnapshot.cs` (generated with `dotnet ef migrations add`)
- New test `Unit/Domain/ImageReferenceRulesVenueTests.cs` (10)

### Verification evidence
- Commit `a63f5564` on `feature/venue-profile`. Red: the new tests failed to compile on `ImageOwnerKind.Venue` and `ImageReferenceRules.Folders`. The implementor was stopped by a full disk before the full run and the commit; the orchestrator freed space, read the diff, saw the expected `PendingModelChangesWarning` failures before the migration, generated the migration and ran the full suite: 1344 passed, 0 failed.
- The generated `Up` holds nine `AddColumn` calls (text columns with collation `NOCASE_NOACCENT`) and no change to `Name` (SQLite does not store the length). `Down` still uses EF `DropColumn`; V3a replaces it with native statements. No existing test was edited (the planned exception was not needed).
- The Android-target build was not run in this worktree (disk space); it runs at V5b / V9c.

### Traceability
| AC | Criterion | Implementation | Tests |
|----|-----------|----------------|-------|
| REQ-VEN-03 | `local:venues/<32 hex>` valid; malformed → null; display paths | `ImageReferenceRules` | `ImageReferenceRulesVenueTests` (10) |
| REQ-VEN-01, 05, 22 | columns, null for existing rows, name 60 | `Venue`, `VenueConfiguration`, migration | Level C here; migration test in V3a, service test in V4a |

## 2026-11-14 — V7a Nominatim provider (Done)

### Changed files
- New `Services/Places/IGeocodingProvider.cs`, `GeocodingRecord.cs`, `NominatimGeocodingProvider.cs` (namespace `MyVocaList.Services.Places`)
- New test `Unit/Services/Places/NominatimGeocodingProviderTests.cs` (18)

### Verification evidence
- Commit `54865d1f` on `feature/venue-map`. The implementor was stopped by the usage limit after writing the files; the orchestrator read the provider against `design.md § 2.5`, ran the full suite (1348 passed, 0 failed) and committed.

### Traceability
| AC | Criterion | Implementation | Tests |
|----|-----------|----------------|-------|
| REQ-VEN-25, 26, 27, 28, 34 | search and reverse request shape, mapping, nothing found, failures surface | `NominatimGeocodingProvider` | `NominatimGeocodingProviderTests` (18) |

## 2026-11-14 — V7b Address mapper (Done)

### Changed files
- New `Services/Places/NominatimAddressMapper.cs`; new test `Unit/Services/Places/NominatimAddressMapperTests.cs` (39)

### Verification evidence
- Commit `6ba88e74` on `feature/venue-map`. Red: 33 of 39 failed on assertions against an empty stub. `dotnet test` 1387 passed, 0 failed. Orchestrator re-check: diff holds only the two files; mapper tests 39 passed.

### Traceability
| AC | Criterion | Implementation | Tests |
|----|-----------|----------------|-------|
| REQ-VEN-32 | street order by country, fallbacks for road, line 2, city, region; cuts to maximum; accents kept | `NominatimAddressMapper.ToPlace`, `BuildStreet` | `ToPlace_NumberFirstCountry_…`, `…_CommaCountryBrazil_…`, `…_OtherCountry_…`, fallback and cut tests |
| REQ-VEN-26 | place name only for venue categories; one-line address | `BuildName`, `BuildAddressLine` | `ToPlace_VenueCategory_KeepsName` (7), `…_NonVenueCategory_…`, `…_AllFourParts_…`, `…_NoAddressParts_…` |
| REQ-VEN-30 | name cut to 60 | `Cut` | `ToPlace_LongName_CutTo60Characters` |

## 2026-11-14 — V3a Repository and migration test (Done)

### Changed files
- `Domain/RepositoryInterface/IVenueRepository.cs`, `Infra/Repository/VenueRepository.cs` (`AddressExistsAsync`, `GetLocalImageReferencesAsync`)
- `Infra/Migrations/20261114133332_AddVenueProfile.cs` (`Down` only: nine native `ALTER TABLE "Venues" DROP COLUMN` statements)
- New tests `Integration/Migrations/VenueProfileMigrationTests.cs` (3), `Integration/Repositories/VenueRepositoryAddressTests.cs` (10)

### Verification evidence
- Commit `f5cd7d5d` on `feature/venue-profile`. Red: test project failed to compile on the two missing repository members. `dotnet test` 1357 passed, 0 failed. Orchestrator re-check: diff holds only the five files; query read against `design.md § 2.3`; the 13 new tests pass.
- The case- and accent-insensitive duplicate test passes with plain `==` on the collated columns (real SQLite), as the design assumed.
- Known limit, kept: the migration tests pass with either `Down` body, so they do not prove the native statements are needed. The native form is kept because it never rebuilds `Venues`, which the events tables reference.

### Traceability
| AC | Criterion | Implementation | Tests |
|----|-----------|----------------|-------|
| REQ-VEN-20 | six equal fields refuse; case and accents ignored; null equals only null; line 2 distinguishes; own id excluded | `VenueRepository.AddressExistsAsync` | `AddressExistsAsync_SameSixFields_…`, `…_DifferentLetterCaseAndAccents_…`, `…_OnlyLine2Differs_…`, `…_NullLine2OnBothSides_…`, `…_DifferentCity_…`, `…_StoredPostalCodeSearchedNull_…`, `…_ExcludeIdOfTheOnlyMatch_…`, `…_ExcludeIdOfAnotherVenue_…` |
| REQ-VEN-21 | empty address never collides | guard in `AddressExistsAsync` | `…_EmptyAddress_ReturnsFalseEvenWithEmptyVenuesStored` |
| REQ-VEN-04 | local venue references listed | `GetLocalImageReferencesAsync` | `GetLocalImageReferencesAsync_ReturnsOnlyLocalVenueReferences` |
| REQ-VEN-05 | existing rows kept with null columns; collation on text columns; rollback | migration | `AddVenueProfile_ExistingVenues_…`, `…_Up_AddsNineNullableColumns_…`, `…_Down_DropsNineColumns…` |

## 2026-11-14 — V3b Image store and sweep know venues (Done)

### Changed files
- `Services/ImageStore.cs` (sweep loops over `ImageReferenceRules.Folders`), `Services/ImageMaintenanceService.cs` (venue references join the live set; constructor gains `IVenueRepository`)
- New tests `Unit/Services/ImageStoreVenueTests.cs` (7), `Unit/Services/ImageMaintenanceServiceVenueTests.cs` (2)
- Planned exception, wider than planned: `ImageMaintenanceServiceTests.cs` gained a venue repository mock (empty list), passed to the constructor and to the unit-of-work stub, because the service resolves its repositories from the unit-of-work scope. No assertion changed.

### Verification evidence
- Commit `28d1c69d` on `feature/venue-profile`. Red: the two sweep tests failed because `venues/` was not scanned; five import/display tests were green before any change (import already used `FolderOf`). The two `ImageMaintenanceServiceVenueTests` were not seen red (they would not compile before the constructor change). `dotnet test` 1366 passed, 0 failed.
- One unrelated test (`ArtistSuggestionServiceTests.SearchNameRemoteAsync_CallerCancelled_Rethrows`) failed once in the commit hook and passed on the next run and alone; treated as a timing flake, not investigated.
- Kept as found: the sweep matches live files by name across folders, not per folder (names are random GUIDs).

### Traceability
| AC | Criterion | Implementation | Tests |
|----|-----------|----------------|-------|
| REQ-VEN-02 | venue import writes both files under `venues/`; failure message; display paths | `ImageStore.ImportAsync` (unchanged) | `ImportAsync_Venue_WritesBothFilesUnderVenuesAndReturnsVenueReference`, `ImportAsync_Venue_ProcessorThrows_…`, `ResolveDisplaySource_VenueReference_UsesVenuesFolder` |
| REQ-VEN-04 | referenced venue files survive; old orphans deleted; young orphans kept; live set includes venues | `ImageStore.DeleteUnreferencedAsync`, `ImageMaintenanceService.SweepOrphansAsync` | `DeleteUnreferencedAsync_…` (4), `SweepOrphansAsync_…` (2) |

## 2026-11-14 — V7c Place lookup gate, service, DI (Done)

### Changed files
- New `Domain/ServicesInterfaces/IPlaceLookupService.cs`, `Services/Places/PlaceLookupGate.cs`, `Services/Places/PlaceLookupService.cs`; `Domain/Constants/SearchConstants.cs` (`PlaceLookupTimeoutMilliseconds`); `MyVocaList/Extensions/ServiceCollectionExtensions.cs` (Nominatim typed client, `IGeocodingProvider`, `TimeProvider`, gate singleton, `IPlaceLookupService`)
- New tests `Unit/Services/Places/PlaceLookupGateTests.cs` (6), `PlaceLookupServiceTests.cs` (17); one test added to `AppServicesRegistrationTests.cs`

### Verification evidence
- Commit `2827ab2c` on `feature/venue-map`. Red: 24 failed against compile-only stubs. `dotnet test` 1411 passed, 0 failed.
- `develop` (with the lyrics feature) merged into `feature/venue-map` as `573ae33b`: three add/add conflicts (`ServiceCollectionExtensions.cs`, `SearchConstants.cs`, `AppServicesRegistrationTests.cs`) resolved by keeping both sides; full suite 1478 passed. `develop` merged into `feature/venue-profile` as `ddaeed6f`: 1433 passed.

### Traceability
| AC | Criterion | Implementation | Tests |
|----|-----------|----------------|-------|
| REQ-VEN-25 | search: 3-character minimum, 5 results, order kept | `PlaceLookupService.SearchAsync` | `SearchAsync_TwoCharacters_…`, `…_ThreeCharacters_…`, `…_TwoRecords_…`, `…_EmptyAnswer_…` |
| REQ-VEN-26 | reverse: mapped place with the tapped coordinates | `ReverseCoreAsync` | `ReverseAsync_RecordWithAddress_ReturnsFoundWithTappedCoordinates` |
| REQ-VEN-27 | nothing found → `NoAddress` with coordinates | `NoAddressAt` | `ReverseAsync_ProviderReturnsNull_…`, `…_RecordWithEmptyAddress_…` |
| REQ-VEN-28 | failure, offline, 5,000 ms bound → `Unavailable`; caller cancel rethrown | catch chain | `…_ProviderThrowsHttpRequestException_…`, `…_Offline_…` (2), `…_ProviderNeverAnswers_…`, `…_CallerCancelled_…`, `SearchAsync_ProviderThrows_…` |
| REQ-VEN-34 | 1 request per second; last 100 answers remembered; failures not remembered; language; client identification | `PlaceLookupGate.RunAsync`, DI | `RunAsync_…` (6), `ReverseAsync_SamePointTwice_…`, `…_FailedThenSameReverse_…`, `…_PassesCurrentUICultureAsLanguage`, `SearchAsync_SameTextTwice_…`, `AddAppServices_PlaceLookup_…` |

### Decisions made in the task (kept)
- An answer that arrives after the 5,000 ms bound counts as unavailable and is not remembered. The search minimum (3) and limit (5) are private constants of the service.

## 2026-11-14 — V4a VenueService save and read (Done)

### Changed files
- `Domain/ServicesInterfaces/IVenueService.cs`, `Services/VenueService.cs` — `GetVenueForEditAsync`, `SaveVenueAsync`, `ValidateAddressInput`; name limit 60, counter above 50, warning above 55.
- `MyVocaList.Tests/Unit/Services/VenueServiceSaveTests.cs` (new, 42 tests); `VenueServiceTests.cs` (planned exception: three tests 31 → 61).

### Verification evidence
- Commit `2662058b` on `feature/venue-profile`. Red: 39 failed of 58 against throwing stubs. Green: full suite 1475 passed, 0 failed. Orchestrator read `VenueService.cs`: check order name → address → load → duplicate name → duplicate address → write, as design § 2.4.

### Traceability
REQ-VEN-10, 11, 13..17, 19..22, 33, 36 → `VenueServiceSaveTests` (one test per criterion row; full matrix at V9c).

### Decisions
- `GetVenueForEditAsync` maps inside the service (mapper file belongs to V4b). The stored name is written as given; the EF converter trims it.

## 2026-11-14 — V8 VenueMapPickerViewModel and plumbing (Done)

### Changed files
- `MyVocaList/UI/ViewModels/VenueMapPickerViewModel.cs`, `UI/Services/IMapPickerPreferences.cs`, `UI/Services/MauiMapPickerPreferences.cs`, `UI/Messages/VenuePlacePickedMessage.cs` (new); `Navigation/Routes.cs` (`VenueMapPicker`); `Resources/Images/map_outlined.svg` (new, hand-drawn outline, not the Material glyph).
- `MyVocaList.Tests/Unit/ViewModels/VenueMapPickerViewModelTests.cs` (new, 20 tests).

### Verification evidence
- Commit `5f11acd2` on `feature/venue-map` (contains merge `573ae33b`). Red: 18 failed of 20 against a stub. Green: full suite 1498 passed, 0 failed. Orchestrator read the ViewModel: no location API, one message only on confirm, newer tap cancels the older lookup.

### Traceability
REQ-VEN-24..31 → `VenueMapPickerViewModelTests` (full matrix at V9c). REQ-VEN-35: by inspection here; architecture test in V9b.

### Decisions
- Constructor takes `INavigationService` (back navigation testable). Preferences use `IPreferences`, keys `map_picker_last_lat` / `map_picker_last_lon`. The `q` value is not copied into the search field. Registration of the page, ViewModel and preferences is V9b.

## 2026-11-14 — V4b List DTO and mapper (Done)

### Changed files
- `Contracts/DTOs/List/VenueListItemDto.cs` (+`ImageUrl`, `AddressSummary`), `Services/Mappers/VenueMapper.cs`, `MyVocaList.Tests/Unit/Services/VenueMapperTests.cs` (new, 5 tests).

### Verification evidence
- Commit `a38a24ba` on `feature/venue-profile`. Red: compile failure on the missing members. Green: full suite 1480 passed, 0 failed.

### Traceability
REQ-VEN-06, 18 → `VenueMapperTests`.

## 2026-11-14 — V9a Map web assets (Done)

### Changed files
- `MyVocaList/Resources/Raw/venuemap/` (new): `index.html`, `map.js`, Leaflet 1.9.4 files unmodified (`leaflet.js`, `leaflet.css`, `images/*`), `LICENSE-leaflet.txt`. The existing `MauiAsset` glob covers the folder; `.csproj` unchanged.

### Verification evidence
- Commit `d93440cf` on `feature/venue-map`. Full suite 1498 passed. Level C (static assets): no test; behaviour is checked on the emulator at V9c. Bootstrap tag `_framework/hybridwebview.js` taken from the Microsoft Learn HybridWebView page for .NET MAUI 10.

### Decisions
- First view is the world at zoom 2 until the first `view` message. Leaflet's default attribution prefix is kept.

## 2026-11-14 — Merge `venue-map` into `venue-profile`

- Merge commit on `feature/venue-profile`, no conflicts. Full suite 1581 passed, 0 failed.

## 2026-11-14 — V5a VenueFormViewModel (Done)

### Changed files
- `MyVocaList/UI/ViewModels/VenueFormViewModel.cs` (rewritten); `MyVocaList.Tests/Unit/ViewModels/VenueFormViewModelProfileTests.cs` (new, 16 tests); `VenueFormViewModelTests.cs` (planned exceptions: constructor arguments; two tests now verify `SaveVenueAsync` instead of `CreateVenueAsync`, one gained a `ValidateAddressInput` stub).

### Verification evidence
- Commit `1cbfcb97` on `feature/venue-profile`. Red: test project did not compile. Green: full suite 1600 passed on rerun; the first run had one failure in `SongMigrationTests.FullMigrate_AllSevenFtsTriggersExist`, which passes alone and on rerun (unrelated, flaky).

### Traceability
REQ-VEN-07..12, 14, 17, 19, 20, 23, 30, 31 → `VenueFormViewModelProfileTests` (full matrix at V9c).

### Decisions
- `PickOnMapCommand` navigates with `Shell.Current`, as the form's existing navigation does; the route string is the public `MapPickerRoute`. The navigation call itself has no unit test.
- The address is validated through `ValidateAddressInput` before the save call, so a field error shows without a write attempt.
- The 60-character cut of a picked place name is in `NominatimAddressMapper` (`VenueNameMax`), not in `VenuePlaceRules.Merge`.

## 2026-11-14 — V9b VenueMapPickerPage and registration (Done)

### Changed files
- `MyVocaList/UI/Pages/Venues/VenueMapPickerPage.xaml(.cs)` (new); `MyVocaList/AppShell.xaml.cs` (route); `MyVocaList/MauiProgram.cs` (page, ViewModel, `IMapPickerPreferences`); `MyVocaList.Tests/Architecture/NoLocationPermissionTests.cs` (new, 3 tests).

### Verification evidence
- Commit `9bb864c2` on `feature/venue-map`; merged into `feature/venue-profile` (`02e4bfe2`), full suite 1603 passed, 0 failed. The platform code that sets the web view User-Agent (`#if ANDROID` / `#elif IOS`) is not compiled by the test build; the Android build at V5b covers the Android part.

### Traceability
REQ-VEN-35 → `NoLocationPermissionTests`. Page behaviour (REQ-VEN-23..31, 34): emulator and device check.

### Decisions
- Page title "Pick the venue location" through `ContentPage.Title`. Two activity indicators (search, tap lookup). Search on submit through `ReturnCommand`.
- Open point passed to V5b: the raw-message handler must ignore a malformed message instead of throwing.

## 2026-11-14 — V5b Venue pages (Done)

### Changed files
- `MyVocaList/UI/Pages/Venues/VenueFormPage.xaml` (name limit 60, picture field, "Address" heading, "Pick on map" button and helper text, six address fields); `VenuesPage.xaml` (leading image, address as supporting text); `VenueMapPickerPage.xaml.cs` (a malformed map message is logged and ignored).

### Verification evidence
- Commit `2bbdcfa1`. Full suite 1603 passed, 0 failed. Android build (`net10.0-android`, Debug): 0 errors.

### Decisions
- The list reference for `ListItemLeadingImage` was `SongsPage.xaml` (the artists list uses an avatar). Postal code has no keyboard restriction.

## 2026-11-14 — V9c Close-out (Done; device check open)

### Verification evidence
- Verifier subagent on `4f144a1f`: PASS WITH FIXES, no blocker. Fixes applied in `1a9e5053`: picture is a rounded square (removed `IsCircle`, REQ-VEN-07); search end icon bound to `SearchCommand` (REQ-VEN-25); a failed edit load is logged and shown in a snackbar (REQ-VEN-17). Not changed: the messenger registration is weak and is not unregistered.
- After the fixes: full suite 1603 passed, 0 failed; Android build 0 errors, 27 warnings (existing NuGet advisories).
- Merged to `develop` as `b33703ff`. Governed components unchanged against `develop`.
- No emulator was running: the screens were not exercised. This is the device check (V10) below.

### Traceability (summary; one test class per row holds one test per criterion)
| AC | Implementation | Tests |
|----|----------------|-------|
| REQ-VEN-01..05 | `Venue`, `VenueConfiguration`, migration `AddVenueProfile`, `ImageStore`, `ImageMaintenanceService`, `VenueRepository` | `VenueProfileMigrationTests`, venue repository tests, `ImageStoreTests`, `ImageMaintenanceServiceTests` (01: through 05) |
| REQ-VEN-06, 18 | `VenueMapper`, `VenueAddressRules.Summary`, `VenuesPage.xaml` | `VenueMapperTests`, `VenueAddressRulesTests` |
| REQ-VEN-07, 12 | `VenueFormPage.xaml` | UI only: device check |
| REQ-VEN-08..11, 17, 23 | `VenueFormViewModel` | `VenueFormViewModelProfileTests`, `VenueFormViewModelTests` |
| REQ-VEN-10, 11, 13..17, 19..22, 33, 36 | `VenueService`, `VenueAddressRules`, `VenueRepository.AddressExistsAsync` | `VenueServiceSaveTests`, `VenueServiceTests`, `VenueAddressRulesTests`, venue repository tests |
| REQ-VEN-24..31 | `VenueMapPickerViewModel`, `VenuePlaceRules` | `VenueMapPickerViewModelTests`, `VenuePlaceRulesTests` |
| REQ-VEN-28, 29, 32, 34 | `Services/Places/*`, DI registration | Nominatim provider, address mapper, gate and lookup service tests, `AppServicesRegistrationTests` |
| REQ-VEN-35 | no location API or permission | `NoLocationPermissionTests` |

## 2026-11-14 — Emulator check (Android, Pixel 5 API 36) and fixes (Done)

### Found on the emulator and fixed (commit `a4369a4c`, merged to `develop` as `21ba511b`)
- Tapping the map closed the app: the web view raises its message event off the UI thread. `VenueMapPickerPage.xaml.cs` now marshals `ready` and `tap` to the UI thread.
- Search results never showed: visibility was bound to `Results.Count`, which `ReplaceRange` does not notify. `VenueMapPickerViewModel.HasResults` added (test `SearchCommand_WithPlaces_ShowsResultsUntilOneIsSelected`).
- A result row did not react to a tap: rows are now a `BindableLayout` (not `DXCollectionView`), and the row command binds to the ViewModel by ancestor type.

### Changed files
- `MyVocaList/UI/Pages/Venues/VenueMapPickerPage.xaml(.cs)`, `MyVocaList/UI/ViewModels/VenueMapPickerViewModel.cs`, `MyVocaList.Tests/Unit/ViewModels/VenueMapPickerViewModelTests.cs`.

### Verification evidence (after the fixes; full suite 1604 passed, Android build 0 errors)
| Step | Result on the emulator |
|------|------------------------|
| Venues list | Existing venues show the default venue image; a venue with an address shows "street, city" under the name |
| New venue form | Picture field with "Add picture" and hint; "Address" heading; "Pick on map" with helper text; six address fields with counters |
| Map picker | Map tiles with "© OpenStreetMap contributors"; search "Bar Brahma Sao Paulo" lists two places with addresses |
| Select a result | Marker placed; panel "Bar Brahma", its address, "Use this place" |
| Tap the map | Panel "Selected place", "Rua dos Timbiras, 421, São Paulo, São Paulo, Brazil" |
| Use this place | Back on the form: all six fields filled (street, line 2, city, region, postal code, country); button reads "Change on map" |
| Save | Venue saved; list row shows "Rua dos Timbiras, 421, São Paulo" |
| Duplicate name (other letter case) | Refused on Name: "There is another venue registered with this name" |
| Duplicate address (other case, no accents) | Refused on Street address: "There is another venue registered at this address" |
| Edit mode | Stored name and address loaded; "Change on map" opens on the stored point with a marker |
| Picture | Photo picked, cropped to the centre square; "Replace picture" / "Remove picture" shown; saved as `files/images/venues/<id>_l.jpg` (13 KB) and `_t.jpg` (5 KB); list row shows it |
| Offline | Picker shows "The map needs an internet connection." |

Not checked on the emulator: iOS, removing a picture then saving, the 61-character name (unit-tested).

## 2026-11-14 — V11 Picture source and resolver (Done)

### Changed files
- `Contracts/DTOs/Venues/VenuePictureSource.cs` (new), `VenuePlace.cs` (+`PictureSource`); `Services/Places/GeocodingRecord.cs`, `NominatimGeocodingProvider.cs` (`extratags=1`), `NominatimAddressMapper.cs`; `Domain/ServicesInterfaces/IVenuePictureLookupService.cs`, `Services/Places/VenuePictureLookupService.cs` (new); DI registration; tests under `Unit/Services/Places/` and one DI test.

### Verification evidence
- Commit `d5cd6c37`. Full suite 1638 passed, 1 failed (`SongMigrationTests.M1_TableRebuild_KeepsUniqueCollatedIndexes`, unrelated, passes alone; green in the V12 run). The implementor did not observe a red run (implementation written before the first run).
- Planned exception: two expected request addresses in `NominatimGeocodingProviderTests.cs` gained `&extratags=1`.

### Traceability
REQ-VEN-37 → mapper and provider tests; REQ-VEN-39, 40 → `VenuePictureLookupService` tests, `AddAppServices_VenuePictureLookup_HasConfiguredHttpClient`.

### Decisions
- An invalid `brand:wikidata` does not fall back to `wikidata`. A failed Wikidata request ends the resolution (no fall-through to the Commons file or image address). The resolver returns the address only; the download is V12.

## 2026-11-14 — V12 Form fills the picture from a confirmed place (Done)

### Changed files
- `MyVocaList/UI/ViewModels/VenueFormViewModel.cs`; `Domain/ServicesInterfaces/IRemoteImageFetcher.cs`, `Services/RemoteImageFetcher.cs` (new: https only, image content type, 25 MB cap, 5,000 ms bound, app User-Agent); `MyVocaList/Extensions/ServiceCollectionExtensions.cs` (registration); tests `VenueFormViewModelProfileTests.cs` (+8), `RemoteImageFetcherTests.cs` (new, 5); constructor arguments in `VenueFormViewModelTests.cs` (planned exception).

### Verification evidence
- Commit `544fcd8e`, merged to `develop` as `46e41532`. Red: the fill test and the fetch test failed against stubs. Green: full suite 1652 passed, 0 failed. Android build 0 errors.
- No reusable download existed (the song form keeps the Deezer cover as a remote address), so `IRemoteImageFetcher` was added; the bytes go through `IImageStore.ImportAsync(stream, ImageOwnerKind.Venue)`, the same crop and compression as a picked photo.
- Open: no DI test resolves `IRemoteImageFetcher` (Level C; the emulator run below exercises the real registration).

### Traceability
REQ-VEN-38, 39 → `VenueFormViewModelProfileTests` (PlacePicked_* tests), `RemoteImageFetcherTests`.

### Decisions
- While the picture is being fetched the picture buttons are busy, so the user cannot pick at the same time; a removal during the fetch wins over the late result.

## 2026-11-14 — Emulator check, second pass (current build `544fcd8e`) (Done)

The emulator had restarted from an older saved state with an old app version; the current build was reinstalled before these checks.

| Step | Result on the emulator |
|------|------------------------|
| Search "Blue Note Jazz Club New York" → select → "Use this place" on a blank form | Name filled "Blue Note Jazz Club"; address "131 West 3rd Street", "Manhattan", "New York" |
| "Change on map" on that form | Map opens on the stored point with a marker (zoomed in on the street) |
| Tap venue icons drawn on the map | Panel shows the commercial name and address: "Village Underground" (West 3rd Street) and a "... Shack" (MacDougal Street); a third icon answered "Selected place" with an address only |
| Add picture → Save → reopen → Remove picture → Save → reopen | Form shows "Add picture" again; the list row falls back to the default image |
| Search "Hard Rock Cafe London" → select → "Use this place" on a blank form | Name, address and the Hard Rock Cafe logo filled (centre square crop); "Replace picture" / "Remove picture" shown; saved as `_l.jpg` 34 KB and `_t.jpg` 14 KB |

Coverage note: the picture comes only when the OpenStreetMap place names a Wikidata entry, a Commons file or an image address. "Bar Brahma" has none, so its picture stays empty by design.

## Device check (Helder) — V10

1. Venues list: seeded venues show the default image; a venue with a picture shows it; a venue with an address shows "street, city" under the name.
2. New venue: default image and "Add picture"; add a photo (cropped square), replace it, remove it; Save without a picture works.
3. Type only a country → Save: "Street address is required." on Street address.
4. Save a second venue with the same name (different case/accents): refused on Name. Same six address fields: refused on Street address. Same address with a different Address line 2: accepted.
5. "Pick on map": the map shows with "© OpenStreetMap contributors"; search a bar by name, tap the result, "Use this place": name (when blank) and address filled; fields stay editable.
6. Tap an empty street spot: address without a name. Tap the sea: "No address found here." and "Use this point".
7. Edit the venue: "Change on map" opens on the stored point; Back changes nothing.
8. Airplane mode: the picker shows "The map needs an internet connection."; typing the address still works.
9. A 45-character name saves; a 61-character name is refused; the counter appears above 50.
