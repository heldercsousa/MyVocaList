# Requirements: venue profile — picture, address and map pick

> **Status: APPROVED 2026-11-14 under Helder's instruction of the same day** ("you're authorized in
> advance for all phases … consider any plan you create as already approved … use your recommended
> options"). Every decision taken for Helder is listed in `design.md § 1` so he can overrule it.
> The on-device check (`tasks.md` V10) stays his.

This spec covers three backlog items (see `tasks.md § Items`). Shipped specs it builds on (not edited):
Venues CRUD `../../requirements.md` (AC-n.m) and the pictures spec
`Docs/Management/BusinessFeatures/artists-songs/changes/2026-11-13-pictures-for-songs-and-artists-upload-with-automatic-crop-kept-as-app-private-files/` (REQ-PIC-NN).

## Goal

1. A venue can have a picture, chosen by the user, shown on the Venues list rows and in the Venue form.
2. A venue can have an address made of fields that fit most countries.
3. The admin can point at the place on a map; the address, and the name when the point is a known
   place, are filled from the map.
4. Two venues never share a name or an address.

## Domain vocabulary

| Term | Meaning |
|------|---------|
| **Venue picture** | The image of a venue, stored as an **image reference** in `Venue.ImageUrl` (REQ-PIC-01 vocabulary). |
| **Default venue image** | The placeholder shown when a venue has no picture: the icon `nightlife_outlined`. |
| **Address** | The six address fields of a venue: Street address, Address line 2, City, State / Province / Region, Postal code, Country. |
| **Empty address** | All six address fields blank. |
| **Coordinates** | Latitude and longitude of the point picked on the map (decimal degrees, WGS 84). |
| **Map picker** | The page that shows a map, lets the user search and tap a point, and returns a place. |
| **Place** | What the map picker returns: coordinates, the six address fields found for the point (each may be blank) and a place name (may be blank). |
| **Known place** | A point whose map data has a name and is a business or public venue (bar, restaurant, club, hotel, shop, theatre and similar). |
| **Lookup** | One request to the address service: reverse (coordinates → place) or search (text → places). |
| **Address service** | Nominatim, the OpenStreetMap geocoding service. |
| **Blank** | Null, empty, or white space only. |
| **Equal (text)** | Equal under the database collation `NOCASE_NOACCENT` (letter case and accents ignored), after trimming. |

## User stories

1. As an admin, I recognise a venue on the list by its picture.
2. As an admin, I add, replace or remove the picture of a venue; a venue without one shows a default image.
3. As an admin, I record where a venue is, in the address format of its country.
4. As an admin, I tap the place on a map instead of typing the address.
5. As an admin, I am stopped from registering the same venue twice, by name or by address.

## Acceptance criteria

### A. Venue picture — storage

- **REQ-VEN-01: One stored value.** The picture of a venue SHALL be stored as one image reference in a
  new nullable column `Venue.ImageUrl` (at most 500 characters). Image bytes never enter the database.
- **REQ-VEN-02: Same pipeline as songs and artists.** A picked photo SHALL go through the existing
  import (REQ-PIC-05 to 10): centred square crop, thumbnail 256 x 256 px JPEG quality 75, large
  512 x 512 px JPEG quality 80, EXIF orientation applied, 25 MB limit, message "Could not use this
  picture." on failure. The files are written under `images/venues/`.
- **REQ-VEN-03: Local references for venues are valid.** `local:venues/<32 lowercase hex>` SHALL be a
  well-formed local reference: it is accepted on save, resolved to its thumbnail and large files for
  display, and rejected (stored as null) when malformed.
  Given `local:venues/../../x`, Then it is stored as null.
- **REQ-VEN-04: Venue pictures survive the orphan sweep.** The start-up sweep (REQ-PIC-11) SHALL treat a
  file referenced by a stored venue as live, and SHALL delete files under `images/venues/` that no
  stored venue references and that are older than 24 hours.
- **REQ-VEN-05: Existing and seeded venues.** The migration SHALL add the column as null for every
  existing row. The seed migration is not changed. A venue with a null reference shows the default
  venue image.

### B. Venue picture — list and form

- **REQ-VEN-06: List rows show the picture.** Each Venues list row SHALL show a leading image
  (56 x 56 dp, 4 dp corners, as Songs list rows) with the venue picture's thumbnail, or the default
  venue image when there is none, with and without search text and in the selected state. The
  checkbox stays in the trailing slot.
- **REQ-VEN-07: Picture field in the form.** The Venue form SHALL show the picture field directly
  below Name: a 120 x 120 dp rounded square showing the large picture, or the default venue image.
- **REQ-VEN-08: Default image in add mode.** WHEN the form opens in add mode, the picture field SHALL
  show the default venue image and the button "Add picture".
- **REQ-VEN-09: Actions.** With no form picture the field shows "Add picture" and the size hint
  "Square picture (1:1), 512 x 512 px or larger. Other shapes are cropped to the centre." With a form
  picture it shows "Replace picture" and "Remove picture" and no hint. Both hold in add and edit mode.
  A picture is never required.
- **REQ-VEN-10: Nothing is stored before Save.** Picking, replacing or removing changes the form
  picture only. On Save: add mode stores the form picture's reference (null when none); edit mode
  changes the stored reference only when the user picked or removed a picture.
- **REQ-VEN-11: A failed Save keeps the form picture** and changes no stored reference.

### C. Address fields

- **REQ-VEN-12: Six optional fields.** The Venue form SHALL show, under a heading "Address", in this order:

  | Field label | Max length | Keyboard |
  |-------------|-----------|----------|
  | Street address | 100 | text |
  | Address line 2 (optional) | 100 | text |
  | City | 80 | text |
  | State / Province / Region | 80 | text |
  | Postal code | 20 | text (letters and digits) |
  | Country | 60 | text |

  Street address holds the street and number in the order of the venue's country. Address line 2 holds
  a unit, floor, neighbourhood or district. No field has a country-specific format rule.
- **REQ-VEN-13: Address is optional as a whole.** A venue SHALL save with an empty address.
- **REQ-VEN-14: A started address needs street and city.** IF any address field is not blank, THEN
  Street address and City are required. A blank Street address shows "Street address is required." on
  that field; a blank City shows "City is required." on that field. Save does not proceed.
- **REQ-VEN-15: Length limits.** Each field refuses input beyond its maximum length. A value longer
  than the maximum reaching the service (for example from the map) is refused with
  "{Field label} is too long. Maximum {N} characters." on that field.
- **REQ-VEN-16: Stored trimmed; blank stored as null.** Each address field is trimmed on save and a
  blank field is stored as null.
- **REQ-VEN-17: Edit mode loads the stored venue.** WHEN the form opens in edit mode, it SHALL load the
  venue by id and show its name, picture, six address fields; all of them are editable.
  IF the venue no longer exists, THEN the form shows the snackbar "Venue not found" and navigates back.
- **REQ-VEN-18: List rows show the address.** A Venues list row of a venue whose address is not empty
  SHALL show a supporting text line: Street address and City joined by ", " (blank parts skipped).
  A venue with an empty address shows no supporting text.

### D. Duplicates

- **REQ-VEN-19: Duplicate name refused (unchanged rule, kept).** IF another venue has an equal name,
  THEN Save is refused with "There is another venue registered with this name" on the Name field. In
  edit mode the venue being edited is excluded (AC-1.6, AC-3.4).
- **REQ-VEN-20: Duplicate address refused.** IF the address is not empty and another venue has all six
  address fields equal (a blank field equals only a blank field), THEN Save is refused with
  "There is another venue registered at this address" on the Street address field. In edit mode the
  venue being edited is excluded.
  - Given venue A at ("Rua Augusta, 100", "", "São Paulo", "SP", "", "Brasil"), When a new venue is saved
    with ("rua augusta, 100", "", "Sao Paulo", "sp", "", "brasil"), Then Save is refused.
  - Given the same venue A, When a new venue is saved with Address line 2 "Loja 2" and the other five
    fields equal, Then Save succeeds (two businesses in one building).
- **REQ-VEN-21: Empty addresses never collide.** Any number of venues MAY have an empty address.

### E. Venue name length

- **REQ-VEN-22: Name limit raised to 60.** The venue name maximum SHALL be 60 characters (was 30), so a
  place name from the map fits. Longer names are refused with "Name is too long. Maximum 60
  characters." The character counter appears above 50 characters and turns to the warning colour from
  56; as today, the error colour shows only for a length above the maximum, so a 60-character name
  shows the warning colour and is saved. Minimum 2 characters is unchanged.
- **REQ-VEN-36: Success messages are unchanged.** A successful add returns "Venue '{name}' successfully
  created!" (AC-1.7) and a successful edit returns "Venue name successfully updated to '{name}'!"
  (AC-3.5), whatever fields changed; the form keeps its snackbars "Venue created" / "Venue updated".

### F. Map picker

- **REQ-VEN-23: Entry point.** The Address section of the Venue form SHALL show, above the six fields,
  a button "Pick on map" with a leading map icon, and under it the text "Find the place on the map to
  fill the address, or type it below." WHEN the venue has coordinates, the button reads "Change on map".
- **REQ-VEN-24: The picker opens centred on what is known.** WHEN the user taps the button, the map
  picker opens centred on the first that exists of: (1) the form's coordinates, zoom 17; (2) the first
  search result for the form's non-empty address (fields joined by ", "), zoom 17; (3) the last place
  the user confirmed on this device, zoom 15; (4) the whole world, zoom 2. Cases (1) and (2) show the
  marker at the centre. IF the search of case (2) finds nothing or is unavailable, THEN case (3) applies.
  That search is one lookup and obeys REQ-VEN-34.
- **REQ-VEN-25: Search.** The picker SHALL show a search field "Search a place or address". WHEN the
  user submits text of 3 or more characters (keyboard search key or the search icon), the system
  performs one search lookup and lists at most 5 results (name and address line). Tapping a result
  centres the map on it, zoom 17, moves the marker there, hides the result list and shows the result in
  the bottom panel as the selected place (REQ-VEN-26 panel); no reverse lookup is sent for it. IF the
  search finds nothing, THEN the list area reads "No place found." No lookup is sent while the user types.
- **REQ-VEN-26: Tap selects a point.** WHEN the user taps the map, a marker moves to the point and the
  system performs one reverse lookup for it. A panel at the bottom then shows the place: its name when
  it is a known place, its address on one line, and the button "Use this place".
- **REQ-VEN-27: Lookup with no address.** IF the reverse lookup finds no address (open sea, unmapped
  area), THEN the panel reads "No address found here." and the button reads "Use this point"; the
  place then carries coordinates only.
- **REQ-VEN-28: Lookup unavailable.** IF a lookup fails (offline, timeout after 5,000 ms, HTTP status
  other than 200), THEN the panel reads "Address lookup isn't available right now. You can type the
  address in the form." and the button "Use this point" returns coordinates only. The picker stays usable.
- **REQ-VEN-29: Map unavailable.** IF the device has no internet access when the picker opens, THEN
  the picker shows "The map needs an internet connection." in place of the map; Back returns to the form unchanged.
- **REQ-VEN-30: Confirm fills the form.** WHEN the user taps "Use this place" / "Use this point", the
  picker closes and the form receives the place:
  - coordinates replace the form's coordinates;
  - WHEN the place has at least one non-blank address field, all six address fields are replaced by the
    place's values (blank where the place has none); WHEN the place has no address field, the form's
    address fields are unchanged;
  - WHEN the place has a name and the form's Name is blank, Name becomes the place name; a non-blank
    Name is never changed.
  Nothing is stored before Save. Every filled field stays editable. A place name longer than 60
  characters arrives cut to 60. A place with a city but no street fills the fields as they are; Save then
  asks for the street (REQ-VEN-14), which the user types.
- **REQ-VEN-31: Back changes nothing.** Leaving the picker with Back SHALL leave the form unchanged.
- **REQ-VEN-32: Address fields from map data.** The six fields SHALL be built from the address service's
  answer by the rules of `design.md § 2.5` (street and house number ordered by country; first existing
  of the listed keys for line 2, city and region).
- **REQ-VEN-33: Coordinates are stored.** `Venue.Latitude` and `Venue.Longitude` (nullable) SHALL be
  saved with the venue. WHEN the user changes any address field by hand after a map pick, the stored
  coordinates are kept (they mark where the user pointed).
- **REQ-VEN-34: Service usage rules.** Every request to the address service SHALL carry the
  `User-Agent` `MyVocaList/1.0 (https://github.com/heldercsousa/myvocalist)`; the app SHALL send at
  most 1 request per second; a lookup identical to one of the last 100 answered lookups of the app
  session is answered from memory without a request (failed lookups are not remembered, so a retry
  sends a request). The 5,000 ms bound of REQ-VEN-28 starts when the request is sent, not while it
  waits for its turn. The map SHALL show the attribution "© OpenStreetMap contributors" at all times.
- **REQ-VEN-35: No location permission.** The map picker SHALL not request the device location.

## Validation rules

| Field | Rule | Error message (field) |
|-------|------|-----------------------|
| Name | required, 2–60 characters after trim | existing messages; "Name is too long. Maximum 60 characters." (Name) |
| Name | no other venue with an equal name | "There is another venue registered with this name" (Name) |
| Street address | required when any address field is not blank; ≤ 100 | "Street address is required." (Street address) |
| City | required when any address field is not blank; ≤ 80 | "City is required." (City) |
| Address line 2, Region, Postal code, Country | ≤ 100 / 80 / 20 / 60 | "{Field label} is too long. Maximum {N} characters." (that field) |
| Address | not empty → no other venue with six equal fields | "There is another venue registered at this address" (Street address) |
| `Venue.ImageUrl` | null, an image URL, or a well-formed local reference; anything else → null | — (REQ-VEN-03) |
| Latitude / Longitude | both null, or −90…90 and −180…180; one alone or out of range → both null | — |

## Failure modes

| Situation | Behaviour |
|-----------|-----------|
| Photo unreadable or over 25 MB | Message, form picture unchanged (REQ-VEN-02) |
| Local reference whose files are missing | Default venue image |
| Save fails (validation, duplicate) | Message on the field; nothing stored; form picture kept (REQ-VEN-11) |
| Venue deleted while its edit form opens | Snackbar "Venue not found", navigate back (REQ-VEN-17) |
| Offline when the picker opens | Message in place of the map (REQ-VEN-29) |
| Lookup fails or finds nothing | Panel message; coordinates can still be used (REQ-VEN-27, 28) |
| Map tiles fail to load | Grey map; search, tap and confirm still work |

## Out of scope

- **Venue logo from the map.** No source allows it: OpenStreetMap holds no logos, and Google's Places
  policy forbids storing its photos, names and addresses. The picture is always user-picked.
- Google Maps and any map or geocoding service that needs an API key or billing.
- Device location ("my location" button), directions, opening the address in a maps app.
- Address autocomplete while typing (forbidden by the address service's usage policy).
- Country picker with ISO codes, per-country postal-code formats, address validation against a postal database.
- A database unique index on name or address (existing development databases hold duplicate seeded names).
- Changes to the seed migration; venue pictures inside backups (same follow-up item as song and artist pictures).
- Delete rules (AC-4.4) and the pending row-layout item `../2026-11-12-venues-list-rows-leading-checkbox-trailing-overflow-button-no-empty-leading-inset/`.
- Contact info (phone, website, opening hours).

## Supersedes (shipped specs are not edited)

| Shipped requirement | Change |
|---------------------|--------|
| Venues AC-1.2 (form shows a single Name field) | **Replaced** by REQ-VEN-07, 12, 23. |
| Venues AC-1.5, AC-1.8 (30-character limit, counter at 25/28/30) | **Replaced** by REQ-VEN-22. |
| Venues AC-2.11 (row shows the name and a leading icon) | **Replaced** by REQ-VEN-06, 18. |
| Venues AC-3.2 (edit form pre-populated with the current name) | **Extended** by REQ-VEN-17. |
| Venues "Out of Scope: venue address, geolocation" | **Replaced** by this spec. |
| REQ-PIC-09 / 11 (folders `songs/`, `artists/`) | **Extended** by REQ-VEN-02, 04 (`venues/`). |

## Addendum 2026-11-14 — Venue picture from a map pick (REQ-VEN-37..40)

This addendum replaces the earlier statement that a venue logo cannot come from the map. OpenStreetMap places sometimes name a Wikidata entry, a Wikimedia Commons file or an image address. When they do, the app can fetch a picture.

### REQ-VEN-37 — A looked-up place carries its picture source
- Every place lookup (reverse and search) asks the provider for the extra tags of the place.
- A place carries a picture source made of three optional values: a Wikidata id (tag `brand:wikidata` first, else `wikidata`; accepted only when it matches `Q` followed by digits), a Commons file name (tag `wikimedia_commons`, only when it starts with `File:`), and an image address (tag `image`, only when it is an absolute `https` address).
- Given a place with `brand:wikidata = Q918151` and `wikidata = Q1`, then the Wikidata id is `Q918151`.
- Given a place with no such tags, then the picture source is empty.

### REQ-VEN-38 — "Use this place" fills the picture when the form has none
- When a confirmed place reaches the form, and the form shows no picture (no stored picture and no picked picture), and the place has a picture source, the app resolves one picture address, downloads it and stores it through the same path as a photo picked by the user (centre square crop, same size limits and compression). The picture field then shows it as a changed picture; "Replace picture" and "Remove picture" work as for a picked photo.
- Resolution order: (1) Wikidata id: the logo statement (P154), else the image statement (P18), read with one request to the Wikidata claims API; the file is fetched from `https://commons.wikimedia.org/wiki/Special:FilePath/<file>?width=512`. (2) Commons file name, fetched the same way. (3) The image address.
- Given the form already shows a picture, when a place with a picture source is confirmed, then the picture is unchanged and no picture request is sent.
- Given a place with an empty picture source, then no picture request is sent.
- The address and name fill (REQ-VEN-30) never waits for the picture: the fields fill first.

### REQ-VEN-39 — A picture that cannot be fetched is skipped silently
- Given no internet access, an answer slower than 5,000 ms, an answer that is not an image, or a store refusal, then the form keeps no picture, shows no error, and the address fill is unaffected. The failure is logged.

### REQ-VEN-40 — Usage rules for the picture requests
- Requests to Wikidata and Commons carry the User-Agent `MyVocaList/1.0 (https://github.com/heldercsousa/myvocalist)`.
- Picture requests are sent only after the user confirms a place, at most one resolution per confirmed place.
