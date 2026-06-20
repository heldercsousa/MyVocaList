# Spec Cleanup — Artists & Songs Phase 2 Reconciliation

> **Created:** 2026-06-20
> **Found by:** Phase 2 spec-vs-reality cross-check (same session as Phase 16C gate identification)
> **Do before:** Phase 16C.5 commit (so the spec matches the shipped code)
> **Estimated effort:** < 30 min (docs only, no code changes)

## Six divergences to fix

### 1 — requirements.md AC-1.16 — filter control type

| | |
|---|---|
| **Spec says** | "The page shall show a **top tab bar** with three tabs: All \| Authors \| Performers" (spec-updated 2026-05-15c) |
| **Implemented** | `dxe:FilterChipGroup` with two chips (Authors, Performers); deselecting both = All |
| **Fix** | Rewrite AC-1.16: "The page shall show a **FilterChipGroup** with two filter chips: 'Authors' (artists with ≥1 song) and 'Performers' (artists with ≥1 Catalog entry). When neither chip is selected, all artists are shown. When a chip is selected, only the matching role subset is shown." |

### 2 — requirements.md Overview / Artist Roles — two-entry menu table

| | |
|---|---|
| **Spec says** | "The admin UI exposes both roles via two menu entries: Authors \| Performers" (table at line ~28) |
| **Implemented** | Single "Artists" flyout menu entry (no mode query param). Role filter exposed on the page via chips. |
| **Fix** | Replace the two-entry table with: "The admin UI exposes Artists via a single 'Artists' flyout menu entry. Role filtering is done on the page itself via filter chips (Authors / Performers)." |

### 3 — design.md Page Structure (ArtistsPage) — "Role filter" row

| | |
|---|---|
| **Spec says** | "Top tab bar (DXTabView or equivalent DevExpress tab component) \| All / Authors / Performers; pre-selected from mode query param" |
| **Implemented** | `dxe:FilterChipGroup` two chips; no mode query param (removed in Phase 16A.2) |
| **Fix** | Update row: "FilterChipGroup \| Authors / Performers chips; deselect both = All; drives `ArtistRoleFilter` on ViewModel" |

### 4 — design.md AppShell code block — old three-item Catalog group

| | |
|---|---|
| **Spec says** | `new MenuGroup("Catalog", [ "Authors"…, "Performers"…, "Songs"… ])` |
| **Implemented** | Single `"Artists"` entry + `"Songs"` entry (Phase 16A.2) |
| **Fix** | Replace the code block comment and example with the actual NavigationConfig outcome |

### 5 — design.md Song entity — missing `Version` property

| | |
|---|---|
| **Spec says** | Song entity has Id, Title, ArtistId, FeaturedArtists, Lyrics, ExternalId, ExternalProvider, HasManualEdits |
| **Implemented** | Song Import Wave 1.1 added `public string? Version { get; set; }` |
| **Fix** | Add `Version string?` to the Song entity block in design.md; add a note: "Added by Song Import & Entity Resolution. Used to distinguish version variants (live, acoustic, remix) within the same artist-title pair." |

### 6 — design.md SongConfiguration — stale unique index

| | |
|---|---|
| **Spec says** | `IX_Songs_ArtistId_Title` composite unique (2-col) |
| **Implemented** | Song Import Wave 2.2 replaced with `IX_Songs_ArtistId_Title_Version` (3-col) |
| **Fix** | Update the HasIndex line in the SongConfiguration block to the 3-col index; add migration name `AddSongVersion` as a note |

---

## Files to edit

| File | Changes |
|------|---------|
| `Docs/Management/BusinessFeatures/artists-songs/requirements.md` | AC-1.16 rewrite; Overview menu-entry paragraph |
| `Docs/Management/BusinessFeatures/artists-songs/design.md` | Page Structure role-filter row; AppShell code block; Song entity block; SongConfiguration HasIndex line |

## Out of scope

- `ISongService`/`ISongRepository`/`IArtistRepository` extension methods added by Song Import — tracked in `song-import-resolution/design.md`; no change to parent design.md needed (cross-reference note is optional)
- No code changes — docs only
