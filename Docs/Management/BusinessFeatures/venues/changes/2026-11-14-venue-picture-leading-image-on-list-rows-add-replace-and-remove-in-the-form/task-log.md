# Task log: venue profile — picture, address and map pick

### Checkpoint
- 2026-11-14 — V1a, V1b, V2, V2m done (a63f5564); V7a done (54865d1f). V3a dispatched in worktree `venue-profile`; V7b in worktree `venue-map`. Disk C: is nearly full: worktree tasks run `dotnet test` only; the Android build runs at V5b and V9c.
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
