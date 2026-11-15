# Task log: venue profile — picture, address and map pick

### Checkpoint
- 2026-11-15 — Complete. On `develop` and `main`. Device check (V10) passed on a physical device (Helder, 2026-11-15). Closed entries are in `task-log-archive-2026-11.md`.
- Context manifest: `requirements.md`, `design.md`, `tasks.md` (this folder).

## Device check (Helder) — V10

**Result (2026-11-15):** Helder ran this checklist on a physical device and reported it passed. No per-step notes were recorded.

1. Venues list: seeded venues show the default image; a venue with a picture shows it; a venue with an address shows "street, city" under the name.
2. New venue: default image and "Add picture"; add a photo (cropped square), replace it, remove it; Save without a picture works.
3. Type only a country → Save: "Street address is required." on Street address.
4. Save a second venue with the same name (different case/accents): refused on Name. Same six address fields: refused on Street address. Same address with a different Address line 2: accepted.
5. "Pick on map": the map shows with "© OpenStreetMap contributors"; search a bar by name, tap the result, "Use this place": name (when blank) and address filled; fields stay editable.
6. Tap an empty street spot: address without a name. Tap the sea: "No address found here." and "Use this point".
7. Edit the venue: "Change on map" opens on the stored point; Back changes nothing.
8. Airplane mode: the picker shows "The map needs an internet connection."; typing the address still works.
9. A 45-character name saves; a 61-character name is refused; the counter appears above 50.
