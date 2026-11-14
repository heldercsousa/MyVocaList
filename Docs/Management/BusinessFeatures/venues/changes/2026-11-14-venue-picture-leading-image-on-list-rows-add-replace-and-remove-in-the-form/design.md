# Design: venue profile — picture, address and map pick

> **Status: APPROVED 2026-11-14** under Helder's instruction of the same day (see `requirements.md`).

## 1. Decisions taken for Helder (2026-11-14)

Each row can be overruled; the cost of reversing is noted.

| # | Question | Decision | Why | Cost to reverse |
|---|----------|----------|-----|-----------------|
| V1 | Which map? | **OpenStreetMap**: Leaflet 1.9.4 inside a .NET MAUI `HybridWebView`, tiles from `tile.openstreetmap.org`, addresses from Nominatim. | Google's Places policy forbids copying and saving business names and addresses, which is what the prefill does; the .NET MAUI `Map` control on Android needs a billed Google key. OpenStreetMap data (ODbL) may be stored, needs no key and no account. | Medium: the map page and one provider class. The tile URL and the provider sit behind one constant and one interface. |
| V2 | Venue logo from the map | **Not built.** | OpenStreetMap holds no logos; Google forbids storing its photos. Helder asked for it only "if really possible". | — |
| V3 | DevExpress first? | **No DevExpress map control exists for .NET MAUI 25.2.4**, so the stock `HybridWebView` is used (implementor confirms through the `devexpress-maui` MCP before coding; a hit stops the task). Every other control on the page is DevExpress. | Constitutional constraint "UI Component Priority". | — |
| V4 | Address fields | **Street address, Address line 2, City, State / Province / Region, Postal code, Country**; free text, no per-country rule. | The six-field layout used by international forms; fits Brazil (street + number, bairro in line 2, cidade, UF, CEP), the US, Europe and Asia. | Low. |
| V5 | Required fields | **Address optional as a whole; once started, Street address and City are required.** | Existing venues have no address; a half address (only a country) helps nobody. | Low. |
| V6 | Duplicate address | **All six fields equal under the database collation; an empty address never collides.** Address line 2 distinguishes two businesses in one building. | Helder: "the very same address". | Low. |
| V7 | Where are duplicates enforced? | **In `VenueService`, as the name rule is today.** No unique index. | Development databases hold duplicate seeded names (BUG-080 seed), so a unique index migration would fail there; the app is single-device (no concurrent writers). | Low: a later migration can add indexes after a clean-up. |
| V8 | Venue name limit | **Raised from 30 to 60.** | Place names from the map are often longer than 30. | Low. |
| V9 | Default venue image | **The existing icon `nightlife_outlined`** (the Venues menu icon), through the placeholder slot of the shared picture components. | Same mechanism as songs and artists; no new raster asset; seeded venues need nothing. | Low. |
| V10 | Picture shape | **Rounded square** in the form (120 dp) and on rows (56 dp, `ListItemLeadingImage`), as songs. | Logos and facades are square or wide, not portraits. | Low. |
| V11 | How does the user find the map? | **A full-width tonal button "Pick on map" with a map icon at the top of the Address section**, with one helper line, before the six fields. | Seen before the fields, so the user learns the shortcut before typing; typing stays possible. | Low. |
| V12 | Map picker as page or sheet? | **A pushed Shell page.** | A map inside a bottom sheet fights the sheet's drag gesture; the form already uses pushed pages. | Low. |
| V13 | What does a pick overwrite? | **Address fields: always (when the place has an address). Name: only when blank.** | The user pointed at the place, so its address is what they want; a typed name is deliberate. | Low. |
| V14 | Device location | **Not used; no permission asked.** | The admin registers venues from anywhere; a permission prompt is friction. The picker opens on the stored point, the typed address or the last pick. | Low: a later item can add a "my location" button. |
| V15 | Search in the picker | **On submit only, never while typing.** | Nominatim's usage policy forbids autocomplete. | — |
| V16 | Coordinates | **Stored** (`Latitude`, `Longitude`). | Reopening the picker lands on the venue; cost is two nullable columns. | Low. |
| V17 | List row supporting text | **"Street address, City"** when the venue has an address. | Tells apart venues with similar names. | Low. |
| V18 | Leading image vs the pending "leading checkbox" item | **Leading image now, checkbox stays trailing**, as Songs and Artists rows are today. The pending row-layout item will rework all lists together. | Helder: "as found in songs and artists CRUD lists". | Low. |

## 2. Architecture

Layering stays Domain → Infra → Services → UI. Business rules live in Domain rules and Services.

```
VenueFormPage ─► VenueFormViewModel ─► IVenueService.SaveVenueAsync(VenueSaveRequest) ─► IVenueRepository ─► Venues
        │                │   └► IImageStore.ImportAsync(stream, ImageOwnerKind.Venue)  → "local:venues/<name>"
        │                └► GoToAsync(venue-map-picker) … ◄─ VenuePlacePickedMessage(VenuePlace) via IMessenger
        ▼
VenueMapPickerPage (HybridWebView + Leaflet) ─► VenueMapPickerViewModel ─► IPlaceLookupService ─► IGeocodingProvider (Nominatim)
```

### 2.1 Contracts (new files under `Contracts/DTOs/Venues/` unless noted)

```csharp
public sealed record VenueAddress(string? Line1, string? Line2, string? City, string? Region, string? PostalCode, string? Country)
{ public static VenueAddress Empty { get; } = new(null, null, null, null, null, null); }

public enum VenueField { None, Name, AddressLine1, AddressLine2, City, Region, PostalCode, Country }

public sealed record VenueSaveRequest(int? Id, string Name, VenueAddress Address,
    double? Latitude = null, double? Longitude = null, string? ImageReference = null, bool ImageChanged = false);

public sealed record VenueEditDto(int Id, string Name, string? ImageUrl, VenueAddress Address, double? Latitude, double? Longitude);

public sealed record VenuePlace(double Latitude, double Longitude, VenueAddress Address, string? Name, string AddressLine);

public enum PlaceLookupStatus { Found, NoAddress, Unavailable }
public sealed record PlaceLookupResult(PlaceLookupStatus Status, VenuePlace? Place);
public sealed record PlaceSearchResult(PlaceLookupStatus Status, IReadOnlyList<VenuePlace> Places);
```

`Contracts/DTOs/List/VenueListItemDto.cs` gains `string? ImageUrl` and `string? AddressSummary`.

### 2.2 Domain

- `Domain/Entity/Venue.cs` gains `string? ImageUrl`, `string? AddressLine1`, `string? AddressLine2`,
  `string? City`, `string? Region`, `string? PostalCode`, `string? Country`, `double? Latitude`, `double? Longitude`.
- `Domain/Rules/VenueAddressRules.cs` (new, static, pure):
  - `Line1Max = 100`, `Line2Max = 100`, `CityMax = 80`, `RegionMax = 80`, `PostalCodeMax = 20`, `CountryMax = 60`.
  - `VenueAddress Normalize(VenueAddress? address)` — each field trimmed, blank → null; null input → `Empty`.
  - `bool IsEmpty(VenueAddress address)` — all six blank.
  - `(bool isValid, string message, VenueField field) Validate(VenueAddress address)` — REQ-VEN-13, 14, 15, in
    the order Line1 required, City required, then lengths in field order. Labels: "Street address",
    "Address line 2", "City", "State / Province / Region", "Postal code", "Country".
  - `string? Summary(VenueAddress address)` — REQ-VEN-18.
  - `string? ToSearchText(VenueAddress address)` — non-blank fields joined by ", " (REQ-VEN-24); null when empty.
  - `(double? latitude, double? longitude) NormalizeCoordinates(double? latitude, double? longitude)` — validation table.
- `Domain/Rules/ImageOwnerKind.cs`: adds `Venue`. `Domain/Rules/ImageReferenceRules.cs`: `VenuesFolder = "venues"`;
  `FolderOf`, the local-reference parser and every folder list accept it (REQ-VEN-03).
- `IVenueRepository` gains:
  - `Task<bool> AddressExistsAsync(VenueAddress address, int? excludeId, CancellationToken ct = default);`
  - `Task<List<string>> GetLocalImageReferencesAsync(CancellationToken ct = default);`
- `IVenueService` gains (existing members untouched, so existing mocks compile):
  - `Task<VenueEditDto?> GetVenueForEditAsync(int id, CancellationToken ct = default);`
  - `Task<(bool success, string message, VenueField field)> SaveVenueAsync(VenueSaveRequest request, CancellationToken ct = default);`
  - `(bool isValid, string message, VenueField field) ValidateAddressInput(VenueAddress address);`
- `Domain/ServicesInterfaces/IPlaceLookupService.cs` (new):
  - `Task<PlaceLookupResult> ReverseAsync(double latitude, double longitude, CancellationToken ct = default);`
  - `Task<PlaceSearchResult> SearchAsync(string text, CancellationToken ct = default);`
  Both never throw except `OperationCanceledException` on caller cancellation.

### 2.3 Infra

- `VenueConfiguration`: `Name` `HasMaxLength(60)`; `ImageUrl` TEXT 500 nullable; the six address columns TEXT,
  nullable, with their maximum lengths; `Latitude`, `Longitude` REAL nullable. Every string column keeps the
  default collation `NOCASE_NOACCENT` (applied by `AppDbContext.SetDatabaseCollation`). No index is added (V7).
- Migration `AddVenueProfile` (generated by the main agent with `dotnet ef migrations add`):
  `AddColumn` for the nine columns, all nullable, no default. The `Name` length change follows the precedent
  of `20261111144142_SongTitleMaxLength250` (the implementor reads that migration first and applies the same
  technique; SQLite does not enforce the declared length). `Down` drops the nine columns with native
  `ALTER TABLE … DROP COLUMN`, as `20261113032549_AddImageUrlToSongsAndArtists` does. `venuesSeedForTest` is not touched.
- `VenueRepository`:
  - `AddressExistsAsync`: `Venues.AnyAsync(v => (excludeId == null || v.Id != excludeId) && v.AddressLine1 == a.Line1 && v.AddressLine2 == a.Line2 && v.City == a.City && v.Region == a.Region && v.PostalCode == a.PostalCode && v.Country == a.Country)`
    with the normalised address (EF null semantics make null equal only null; the column collation gives the
    case- and accent-insensitive comparison). The caller never passes an empty address.
  - `GetLocalImageReferencesAsync`: `ImageUrl` starting with `local:` (same query as the song and artist repositories).

### 2.4 Services

- `VenueService`:
  - `MaxInputLength => 60`, `ShowCounterAt => 50`, warning from 56 (REQ-VEN-22).
  - `GetVenueForEditAsync`: read unit of work, `GetByIdAsync`, maps to `VenueEditDto`; null when missing.
  - `SaveVenueAsync` (one unit of work): (1) `ValidateNameInput` → failure with `VenueField.Name`;
    (2) `VenueAddressRules.Normalize` + `Validate` → failure with its field; (3) edit mode: load, missing →
    `(false, "Venue not found", None)`; (4) name duplicate (`GetByNameAsync`, own id excluded) → `Name`;
    (5) address not empty and `AddressExistsAsync` → `(false, "There is another venue registered at this address", AddressLine1)`;
    (6) assign name, address, `NormalizeCoordinates`, and the image: add mode `ImageReferenceRules.Normalize(request.ImageReference)`;
    edit mode only when `request.ImageChanged`; (7) `AddAsync` or explicit `UpdateAsync`. Success messages:
    "Venue '{name}' successfully created!" / "Venue '{name}' successfully updated!".
  - `CreateVenueAsync(string)` and `UpdateVenueAsync(int, string)` keep their signatures and messages; an update
    by name leaves every other column unchanged.
  - `ValidateAddressInput` → `VenueAddressRules.Validate(VenueAddressRules.Normalize(address))`.
- `VenueMapper.ToListDto`: sets `ImageUrl` and `AddressSummary` (`VenueAddressRules.Summary`).
- `ImageStore.DeleteUnreferencedAsync` scans `venues/` too. `ImageMaintenanceService` gains `IVenueRepository`
  and adds its references to the live set (REQ-VEN-04).
- Place lookup (new, `Services/Places/`):
  - `IGeocodingProvider`:
    `Task<GeocodingRecord?> ReverseAsync(double latitude, double longitude, string language, CancellationToken ct = default);`
    `Task<IReadOnlyList<GeocodingRecord>> SearchAsync(string text, string language, int limit, CancellationToken ct = default);`
    Throws on transport or protocol failure.
  - `GeocodingRecord(double Latitude, double Longitude, string? Name, string? Category, string? DisplayName, IReadOnlyDictionary<string, string> Address)`.
  - `NominatimGeocodingProvider` (typed `HttpClient`), `NominatimAddressMapper` (static, pure: `GeocodingRecord` → `VenuePlace`, § 2.5).
  - `PlaceLookupGate` (singleton): serialises requests, waits so that two requests start at least 1,000 ms
    apart (`TimeProvider`), and keeps the last 100 answers in memory keyed by the request (coordinates rounded
    to 5 decimals + language, or trimmed search text + language) (REQ-VEN-34).
  - `PlaceLookupService` (`IPlaceLookupService`), constructor `(IGeocodingProvider, PlaceLookupGate, INetworkStatus, ILogger<PlaceLookupService>)`:
    offline → `Unavailable` without a request; linked CTS with `CancelAfter(PlaceLookupTimeoutMilliseconds = 5000)`
    (`SearchConstants`); provider null or a record with no usable address field → `NoAddress` (place with
    coordinates only); provider exception or bound → `Unavailable`, logged at Warning; search text shorter than
    3 characters → `Found` with an empty list, no request; `SearchAsync` asks for 5 results. Language =
    `CultureInfo.CurrentUICulture.Name`.
- DI (`ServiceCollectionExtensions.AddAppServices`): typed client
  `AddHttpClient<NominatimGeocodingProvider>` (base `https://nominatim.openstreetmap.org/`, `User-Agent`
  `MyVocaList/1.0 (https://github.com/heldercsousa/myvocalist)`), `AddScoped<IGeocodingProvider>(sp => sp.GetRequiredService<NominatimGeocodingProvider>())`,
  `AddSingleton<PlaceLookupGate>()`, `AddScoped<IPlaceLookupService, PlaceLookupService>()`, `TimeProvider.System` if not registered.

### 2.5 Integration contract: Nominatim

| Item | Value |
|------|-------|
| Reverse | `GET reverse?format=jsonv2&lat={lat}&lon={lon}&zoom=18&addressdetails=1&accept-language={language}` (invariant culture, 6 decimals) |
| Search | `GET search?format=jsonv2&q={text}&limit=5&addressdetails=1&accept-language={language}` |
| 200 reverse | object with `lat`, `lon` (strings), `name`, `category`, `display_name`, `address{…}`; an object with an `error` member means nothing found → null |
| 200 search | array of the same objects (may be empty) |
| Other status | provider throws `HttpRequestException` (403 and 429 mean the service refused the app: treated as unavailable) |
| Policy | ≤ 1 request/second, identifying `User-Agent`, results cached, no autocomplete, attribution shown |

Mapping (`NominatimAddressMapper`), keys read from `address`:

- **Street address** from `road` (else `pedestrian`, else `footway`) and `house_number`, by `country_code`:
  number first, space (`"{number} {road}"`) for `us, ca, gb, ie, au, nz, fr, lu, za, sg, in`;
  road, comma, number (`"{road}, {number}"`) for `br, pt, es, it, mx, ar, cl, co, pe, uy`;
  road, space, number (`"{road} {number}"`) for every other country. Without a number: the road alone. Without a road: blank.
- **Address line 2**: first non-blank of `suburb`, `neighbourhood`, `quarter`, `city_district`.
- **City**: first non-blank of `city`, `town`, `village`, `municipality`, `hamlet`, `county`.
- **Region**: first non-blank of `state`, `region`, `state_district`, `province`.
- **Postal code**: `postcode`. **Country**: `country`.
- **Name**: the record's `name` when it is not blank and `category` is one of `amenity`, `leisure`, `tourism`,
  `shop`, `club`, `craft`, `office`; else null (a road or a building number is not a venue name).
- **AddressLine** (panel and result rows): Street address, City, Region, Country joined by ", " (blank parts
  skipped); when all are blank, the record's `display_name`.
- Each value is cut to its field maximum (a cut value stays editable in the form).

### 2.6 App (MAUI project)

- `MyVocaList/Resources/Raw/venuemap/`: `index.html`, `map.js`, `leaflet.js`, `leaflet.css`, `images/marker-icon.png`,
  `images/marker-icon-2x.png`, `images/marker-shadow.png`, `LICENSE-leaflet.txt` (Leaflet 1.9.4, BSD-2-Clause,
  copied from the official distribution; nothing is loaded from a CDN).
  - Tile layer `https://tile.openstreetmap.org/{z}/{x}/{y}.png`, `maxZoom: 19`, attribution "© OpenStreetMap contributors" always visible.
  - JS → C# raw messages (JSON): `{"type":"ready"}`, `{"type":"tap","lat":n,"lon":n}`.
  - C# → JS raw messages (JSON): `{"type":"view","lat":n,"lon":n,"zoom":n,"marker":bool}`.
  - Only `SendRawMessage` / `RawMessageReceived` are used (no generic `InvokeJavaScriptAsync`, so no JSON type info is needed under trimming).
- `Navigation/Routes.cs`: `VenueMapPicker = "venue-map-picker"`; `AppShell.xaml.cs` registers it; `MauiProgram.cs`
  registers `VenueMapPickerPage`, `VenueMapPickerViewModel` (transient) and `IMapPickerPreferences`.
- `UI/Services/IMapPickerPreferences.cs` + `MauiMapPickerPreferences.cs`: `(double latitude, double longitude)? LastPlace { get; set; }` over `Preferences`.
- `UI/Messages/VenuePlacePickedMessage.cs`: `sealed record VenuePlacePickedMessage(VenuePlace Place)`.
- The `HybridWebView` user agent is set to the app's `User-Agent` string in the page's `HandlerChanged`
  (Android `WebView.Settings.UserAgentString`, iOS `WKWebView.CustomUserAgent`), so tile requests identify the app (tile usage policy).
- Icon `map_outlined.svg` added to `Resources/Images` when missing (Material Symbols, Apache-2.0, same style as the existing icons).
- No manifest or `Info.plist` change (`INTERNET` exists; no location permission, REQ-VEN-35).

### 2.7 ViewModels

**`VenueFormViewModel`** — constructor `(IVenueService, ISnackbarComponent, ILogger<VenueFormViewModel>, IImageStore, IPhotoPicker, IMessenger)`.
- Address: `AddressLine1`, `AddressLine2`, `City`, `Region`, `PostalCode`, `Country` (observable strings); per
  field `…HasError` + `…ErrorText`; `double? Latitude`, `double? Longitude`; `MapButtonText` ("Pick on map" / "Change on map").
- A change of a field's text clears that field's error. Save validates everything through the service.
- Picture: the same members as `ArtistFormViewModel` (`_uploadedImageReference`, `_storedImageReference`,
  `_imageRemoved`, `FormImageReference`, `FormImageSource`, `FormImageThumbnailSource`, `HasFormImage`,
  `IsImageBusy`, `ImageHintText`, `PickImageCommand`, `RemoveImageCommand`) with `ImageOwnerKind.Venue`; no Deezer branch.
- Edit mode: when `VenueId` is set, `LoadAsync` calls `GetVenueForEditAsync`; null → snackbar "Venue not found" + `GoToAsync("..")`.
  The `venueName` query parameter is still accepted and shows until the load completes.
- Save: builds `VenueSaveRequest` (`ImageChanged = _uploadedImageReference != null || _imageRemoved`), calls
  `SaveVenueAsync`, routes a failure to the field named by `VenueField` (`None` → snackbar error), keeps the busy
  guard (BUG-049) and the existing snackbars "Venue created" / "Venue updated".
- `PickOnMapCommand`: `GoToAsync` to `Routes.VenueMapPicker` with `lat`, `lon` (when set) and `q`
  (`VenueAddressRules.ToSearchText`), invariant culture, escaped.
- Receives `VenuePlacePickedMessage` → `ApplyPlace(place)` (REQ-VEN-30) on the UI thread.

**`VenueMapPickerViewModel`** — constructor `(IPlaceLookupService, IMapPickerPreferences, INetworkStatus, IMessenger, ILogger<…>)`.
- Query properties `lat`, `lon`, `q`. `InitializeAsync` decides the first view (REQ-VEN-24) and raises `ViewRequested(lat, lon, zoom, marker)`.
- `SearchText`, `SearchCommand` (REQ-VEN-25), `Results` (`ObservableRangeCollection<VenuePlace>`), `SelectResultCommand`.
- `OnMapTappedAsync(lat, lon)`: cancels the previous lookup, `IsLookingUp = true`, reverse lookup, sets `SelectedPlace`,
  `PanelTitle`, `PanelText`, `ConfirmText` (REQ-VEN-26, 27, 28).
- `ConfirmCommand`: stores `LastPlace`, sends `VenuePlacePickedMessage`, navigates back.
- `IsOffline` (REQ-VEN-29).

### 2.8 Pages

- `VenueFormPage.xaml` (stays `ContentPage`, `SafeAreaEdges="All"`, `ScrollView`): Name (`MaxCharacterCount="60"`) and
  counter → `media:ImagePickerField` (`PlaceholderIcon="nightlife_outlined"`) → `Label` "Address" (`Title.Medium`) →
  `dx:DXButton` "Pick on map" (tonal style when the theme has one, else `OutlinedButton`; icon `map_outlined`) →
  helper `Label` (`Body.Small`, `OnSurfaceVariant`) → six `dxe:TextEdit` (label, `MaxCharacterCount`, `HasError`,
  `ErrorText`) → Cancel / Save. `OnAppearing` focuses Name only in add mode with a blank Name.
- `VenuesPage.xaml`, both templates: `lists:ListItemLeadingImage` with
  `ImageSource="{Binding ImageUrl, Converter={StaticResource ImageReferenceConverter}}"`, `PlaceholderIcon="nightlife_outlined"`,
  `Margin="0,8"`; `ListItem` supporting text bound to `AddressSummary` (the implementor uses the `ListItem`
  property the Songs rows use for their second line). Trailing checkbox unchanged.
- `VenueMapPickerPage.xaml` (new `ContentPage`, `SafeAreaEdges="All"`, title "Pick the venue location"): rows:
  search `dxe:TextEdit` (end icon search, `ReturnType="Search"`); results `dx:DXCollectionView` of `lists:ListItem`
  (visible when there are results); `HybridWebView` (`HybridRoot="venuemap"`) or the offline `Label`; bottom panel
  (`Border`, `SurfaceContainer`): title, text, `dx:DXButton` (`FilledButton`). Code-behind only bridges raw
  messages ↔ ViewModel and sets the user agent.

## 3. Interaction flows

1. **Map first:** New venue → "Pick on map" → search "Bar Brahma São Paulo" → tap the result → panel shows the
   name and address → "Use this place" → form shows Name and the six fields filled → add a picture → Save.
2. **Type first:** New venue → type name and address → Save. The map is never opened.
3. **Refine:** Edit venue → "Change on map" → picker opens on the stored point → tap the right door → "Use this
   place" → address replaced, name kept → Save.
4. **Duplicate:** Save with an address equal to another venue's → message on Street address; nothing stored.

## 4. Invariants and postconditions

- No two venues have equal names; no two venues with a non-empty address have six equal address fields
  (enforced on every `SaveVenueAsync`, `CreateVenueAsync`, `UpdateVenueAsync`).
- A stored address is empty, or has Street address and City.
- `Latitude` and `Longitude` are both null or both in range.
- `Venue.ImageUrl` is null, an image URL, or a well-formed local reference.
- The map picker writes nothing to the database; the form writes only on Save.
- No exception from the address service reaches a ViewModel.

## 5. Governed components

- `ImagePickerField`, `ListItemLeadingImage`, `ListItem`, `CrudListView`: **used as they are, not changed**.
  `VenueFormPage` becomes a consumer of `ImagePickerField`; `VenuesPage` a consumer of `ListItemLeadingImage`
  (consumer lists in `component-safety-gate.md` are updated in the same commits, if that file lists consumers).
- If a task finds it cannot meet a requirement without changing a governed component, it stops and reports.

## 6. Error handling

Expected failures return tuples / statuses. Only `PlaceLookupService` (provider failures) and the existing
`ImageStore.ImportAsync` catch exceptions, log them and continue.

## 7. Testing (risk tiers per `testing.md`)

| Area | Level | Tests |
|------|-------|-------|
| `VenueAddressRules` | A | normalise, empty, validate (each branch and order), summary, search text, coordinates (REQ-VEN-13..16, 18, 24, 33) |
| `ImageReferenceRules` (venue) | A | `local:venues/…` accepted, traversal rejected, display paths (REQ-VEN-03) |
| `ImageStore` / `ImageMaintenanceService` | A / B | import writes under `venues/`; sweep keeps referenced venue files, deletes old orphans there (REQ-VEN-02, 04) |
| Migration | B | real SQLite: columns added as null, existing rows kept, seed untouched (REQ-VEN-05); model has no pending changes |
| `VenueRepository` | B | real SQLite: `AddressExistsAsync` (case, accents, null vs null, line 2 differs, `excludeId`), local references (REQ-VEN-20, 21, 04) |
| `VenueService` | A | `SaveVenueAsync` every step and field routing, image rules add/edit, coordinates, `GetVenueForEditAsync`, name 60 and counter (REQ-VEN-10, 11, 14, 15, 16, 17, 19, 20, 21, 22) |
| `VenueMapper` | B | `ImageUrl`, `AddressSummary` (REQ-VEN-06, 18) |
| `NominatimAddressMapper` | A | the three street orders, fallbacks, name by category, cut to maximum (REQ-VEN-32) |
| `NominatimGeocodingProvider` | A | stub handler: URLs, `error` body → null, non-200 throws (REQ-VEN-34) |
| `PlaceLookupGate` / `PlaceLookupService` | A | 1 s spacing with fake time, cache hit sends no request, offline, bound, statuses, short search text (REQ-VEN-25, 27, 28, 34) |
| `VenueFormViewModel` | A | picture (REQ-VEN-08..11), edit load (17), field error routing (14, 19, 20), `ApplyPlace` (30), map button text (23) |
| `VenueMapPickerViewModel` | A | first view order (24), search (25), tap (26, 27, 28), confirm (30), offline (29) |
| DI | B | new services resolve; Nominatim client has base address and `User-Agent` |
| Pages, `map.js`, user agent | C | no unit test; emulator check + device check V10 |

Every test carries `// [AC] REQ-VEN-NN: …`. Existing tests are not edited. **Planned exceptions** (each
superseded by a requirement of this spec, listed in the task-log when touched):
- tests pinning the 30-character limit and the 25/28/30 counter thresholds (AC-1.5, AC-1.8 → REQ-VEN-22);
- `VenueFormViewModelTests`: the constructor call, and tests asserting `CreateVenueAsync` / `UpdateVenueAsync`
  calls from Save (the form now calls `SaveVenueAsync`, REQ-VEN-10);
- constructor calls of `ImageMaintenanceService` in tests (new `IVenueRepository` parameter);
- a test asserting that only `songs` and `artists` are valid local-reference folders (REQ-VEN-03).
If any other existing test fails, the task stops and reports.
