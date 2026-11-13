# Design: Deezer artwork in the Song form options and the Songs list

> **Status: APPROVED 2026-11-13** under Helder's instruction of the same day (see `requirements.md`).
> Requirements: `requirements.md`. MD3 evidence: `findings.md` (verbatim quotes, captured 2026-11-13).

## 0. What was already true (no work needed)

`SongFormPage.xaml` already uses the shared `AutocompleteDXField` for Title (`titleField`) and Artist
(`artistField`) since commit `6f7b6bbf` (2026-11-11). Part 1 of Helder's request needs no change.

## 1. Decisions taken for Helder (2026-11-13)

Helder asked the orchestrator to decide. Each row can be overruled; the cost of reversing is noted.

| # | Question | Decision | Why | Cost to reverse |
|---|----------|----------|-----|-----------------|
| D1 | Store image bytes, store the URL, or load on demand? | **Store the URL only** (`Song.ImageUrl`, `Artist.ImageUrl`); the platform image cache keeps the bytes. | No growth of the database or its backups; no third-party artwork bytes kept by the app; Deezer image URLs are content-addressed (a hash in the path), so they are stable. A stored URL also works for rows that never come from a new search (the Songs list). | Low: a later item can add a local byte cache keyed by the same URL. |
| D2 | Which Deezer size? | **`*_medium` (250 x 250 px)** for both song and artist. | A 56 dp slot needs up to 224 px on the densest phones; 250 px covers it with one URL, and stays small (about 10 kB). | Low: the size is one path segment of the URL. |
| D3 | Square, circle or rectangle? | **Song: 56 x 56 dp square image. Artist: 40 x 40 dp circular avatar.** | MD3 lists guidelines: "Use square or rectangular images for other content, such as products or videos." and "Avatars: List items can include images in circular or expressive shapes to represent a person or entity." Tokens: "leading image width 56dp", "leading image height 56dp", "leading avatar size 40dp" (`findings.md` Q1). Covers are square by nature, so the square image is used, not a rectangle. | Low. |
| D4 | What is "the image of a song"? | **The cover of its album**, then **the artist image**, then a **placeholder icon**. | Deezer has no per-track picture; a track's own `md5_image` is its album cover. Helder asked for song first, then artist or album. | Low. |
| D5 | Which row shows which shape when a song falls back to the artist image? | **A song row keeps the 56 dp square**, even when it shows the artist image. | Project reading (the rule covers position, not shape) of MD3: "Place supporting visuals and primary text in the same position in each list item." / "Don't vary the position of elements within a list." (`findings.md` Q4). | Low. |
| D6 | Placeholder for a missing image | **A 24 dp icon on a `SurfaceContainerHighest` background, same size and shape as the image** (`music_note_outlined` for songs, `account_circle_outlined` for artists). | The official docs give no fallback rule (`findings.md` Q5: NOT FOUND). **Project decision, not an MD3 rule.** The icon follows "A leading icon should provide a quick visual cue that relates to the item's label text". | Low. |
| D7 | Image corner radius | **4 dp** (kept from the existing `ListItemLeadingImage`). | The official shape token value was not captured (`findings.md` Q1: NOT FOUND). **Project decision, not an MD3 rule.** | Low. |
| D8 | May a dropdown (menu) row hold an image? | **Yes, as menu slot content.** | MD3 menus guidelines: "Menus have custom slots that support more flexible item layouts." "Slots work best with simple content such as: Images / Progress indicators / Color swatches"; constraints "Keep the same menu item padding", "Targets should be 48x48dp or larger", "Don't add buttons, switches, or other direct actions into the menu item." (`findings.md` Q6). The row keeps its existing padding, grows to its content (project outcome: about 60 dp with an avatar, 72 dp with an image) and gains no interactive element. | Low. |
| D9 | What separates two Title options? | **Cover + `artist · version · album` + source as trailing text.** | Version rows of one song differ by version text, album and cover. The source (`Library` / `Deezer`) is meta-information: MD3 menus anatomy has "Trailing text (optional)"; lists: trailing text is "additional meta-information". Moving it out of the supporting text keeps it visible when the text is truncated. | Low: text building is one method. |
| D10 | What does a Deezer pick overwrite? | **Unchanged from REQ-SFT-12** (Title, Version, Artist, external identity) **plus the pending images**. Featured artists, Lyrics and karaoke URLs are never touched. Nothing is stored before Save. | The pick defines the song's identity; everything else is the user's input. | — |
| D11 | Does a pick overwrite an existing artist's image? | **No. It fills an empty one only**, and only on an exact name match, never for an artist chosen in the near-duplicate sheet. | Stored data is never silently replaced (REQ-SFT-24 spirit); a near-duplicate may be a different artist. | Low. |
| D12 | Is the image dropped when the identity is dropped ("Save as new version / new song")? | **No, the image is kept.** | The identity is dropped to keep the unique index valid; the cover is still the right picture for the user. | Low. |
| D13 | Does an edit ever change a song's image? | **No** (edit mode has no Title search, so there is no pick). | Song rows carry history and are edited in place. | — |
| D14 | Songs list supporting text | **`artist · version · feat. <featured>`** (delivers the pending 2026-11-09 item). | With an artist image as fallback, a row must name its artist; the DTO and template are touched anyway. | Low. |
| D15 | Songs and artists saved before this item | **No backfill now**; they show the artist image when there is one, else the placeholder. A backfill item is registered at close-out. | A backfill needs extra Deezer calls and a matching rule for songs without identity; it deserves its own spec. | — |
| D16 | Artists list avatars | **Not in this item**; follow-up registered at close-out. | The Artists row layout is being reworked (row-actions items, BUG-086 in progress in another session). | — |
| D18 | Songs list row height with a 56 dp image | **72 dp: the image plus 8 dp above and below.** | MD3 gives the two-line height ("Two lines ... 72dp") and says "List item size is determined by the tallest element within the list item." The lists tokens give 10 dp top and bottom space (56 + 20 = 76 dp); the official docs do not state the row height with a 56 dp image (`findings.md` Q2: NOT FOUND). **Project decision:** keep the official 72 dp two-line height. | Low: one margin value. |
| D19 | Option text lines | **Label text and supporting text are one line each, cut with an ellipsis.** | MD3 permits it ("Both the label and supporting text can wrap or be truncated.") but does not require it. **Project decision**, so rows keep one height. It changes Person form options from wrapping to truncating. | Low. |
| D17 | Privacy | Accepted: showing a stored image asks Deezer's image CDN for that image. | Title search already sends the typed text to Deezer; image requests add no new recipient. | — |

## 2. MD3 mapping

| Element | Value | Source (`findings.md`) |
|---------|-------|------------------------|
| Song leading image | 56 x 56 dp, square | [SPEC] "leading image width 56dp" / "leading image height 56dp"; [RULE] "Use square or rectangular images for other content" |
| Artist leading avatar | 40 x 40 dp, circle | [SPEC] "leading avatar size 40dp"; [RULE] "images in circular or expressive shapes to represent a person or entity" |
| Songs list row | two-line, at least 72 dp, image centred | [SPEC] "Two lines ... 72dp"; "Leading element alignment (vertical) Center"; [RULE] "List item size is determined by the tallest element within the list item." The 8 dp above and below the image is project decision D18. |
| Menu row with image | padding kept from the existing code (`Padding="16,8"`), 12 dp between elements, at least 48 dp target | [RULE] "Keep the same menu item padding"; [SPEC] "Padding between elements within a list item 12dp"; "Targets should be 48x48dp or larger" |
| Label / supporting text | one line each, truncated at the end (project decision D19) | MD3 permits it: [RULE] "Both the label and supporting text can wrap or be truncated." |
| Source marker | trailing text, Label Small (11/16, weight 500), on-surface-variant | [RULE] menus anatomy "Trailing text (optional)"; type and colour from the lists tokens for trailing supporting text (`findings.md` Q3); lists trailing text = "additional meta-information" |
| Placeholder, image corner radius | project decisions D6, D7 | NOT FOUND in the official docs |

The menu row uses a 12 dp gap after the leading element (menus spec) while `ListItem` uses 16 dp for
its leading slot today. The difference is intended: the option row is a menu item, and `ListItem`'s
own spacing is owned by another pending item.

**Internal guideline check** (`.claude/library/m3-lists.md`), from `findings.md` Step D: the 40 dp
circle avatar and 56 dp square image are confirmed. `ListItemLeadingImage CornerRadius=4` is
unsupported by the crawl (kept as D7). The library names `ListItemLeadingAvatar`, which does not
exist in code; this item creates it. "Headline" is the internal name of MD3 "Label text";
`ListItem.Headline` and `AutocompleteSuggestion.Headline` are not renamed here (owned by the item
"Correct the MD3 list, selection and toolbar rules in the library guidelines"). `ListItem` itself is
not changed; its spacing is owned by the pending item "ListItem: empty slots not rendered ...".

## 3. Architecture

Layering stays Domain → Infra → Services → UI. Business rules live in Services (`SongService`,
suggestion services) and one Domain rule class; ViewModels only hold pending values and map DTOs to
options.

```
Deezer JSON ──► DeezerProvider ──► MusicSearchResultDto (+ImageUrl, ArtistImageUrl, AlbumTitle)
                                        │
        SongSuggestionService ◄─────────┘   local hits: SongSearchHit (+ImageUrl, ArtistImageUrl)
                │
        SongSuggestionDto (+ImageUrl, ArtistImageUrl, AlbumTitle)
                │
        SongFormViewModel ── ToTitleOption ──► AutocompleteSuggestion (+LeadingKind, ImageUrl,
                │                                   PlaceholderIcon, TrailingText) ──► AutocompleteDXField row
                │ pending images
                ▼
        SongFormSaveRequest (+ImageUrl, ArtistImageUrl) ──► SongService.SaveFromFormAsync
                                                              ├─ Song.ImageUrl (add mode only)
                                                              └─ Artist.ImageUrl (created, or exact match and empty)

SongRepository / CatalogRepository ──► SongListItemDto (+Version, ImageUrl = song ?? artist) ──► SongsPage row
ArtistRepository ──► ArtistListItem (+ImageUrl, real CatalogCount in search) ──► Artist option
```

### 3.1 Domain

- `Song.ImageUrl` (`string?`), `Artist.ImageUrl` (`string?`).
- `Domain/Rules/ImageUrlRules.cs` (new, static): `public const int MaxLength = 500;`
  `public static string? Normalize(string? value)` returns the trimmed value when it is an absolute
  `https` URI of at most `MaxLength` characters, else null (REQ-ART-03). Placed in Domain so the
  provider (Services) and `SongService` share it. If the Domain project already has a folder for
  such static rules, the implementor uses that folder and records it in the task-log.
- `SongFormSaveRequest` gains two optional trailing parameters: `string? ImageUrl = null`,
  `string? ArtistImageUrl = null` (optional so existing call sites and tests compile unchanged).
- `SongSearchHit` gains optional trailing `string? ImageUrl = null, string? ArtistImageUrl = null`.
- `ArtistListItem` gains optional trailing `string? ImageUrl = null`.

### 3.2 Infra

- `SongConfiguration` / `ArtistConfiguration`: `ImageUrl` optional, `HasMaxLength(ImageUrlRules.MaxLength)`.
  No index, no collation (never searched).
- Migration `AddImageUrlToSongsAndArtists`: two nullable TEXT columns. The FTS tables and triggers of
  `AddSearchIndexes` are not touched (they index title and name only).
- Projections (single SQL statement each, no per-row query):
  - `SongRepository.GetPagedAsync` and `CatalogRepository` paged query:
    `new SongListItemDto(..., s.Version, s.ImageUrl ?? s.OriginalArtist.ImageUrl)`.
  - every query that builds a `SongSearchHit` (`SearchTitleCandidatesAsync` with its raw-SQL select
    and its EF fallback, `FindByExternalIdsAsync`, `FindByTitleAndArtistNamesAsync`) fills `ImageUrl`
    and `ArtistImageUrl`, so a Deezer row promoted to a local option shows the stored images.
  - C# expression trees cannot omit optional arguments: every `new SongListItemDto / SongSearchHit /
    ArtistListItem` inside an `IQueryable` `Select` passes all arguments explicitly.
  - `ArtistRepository.SearchByNameAsync`: `new ArtistListItem(a.Id, a.Name, a.ExternalProvider, a.CatalogEntries.Count(), a.ImageUrl)`
    (fixes the constant 0, REQ-ART-10); `GetPagedAsync` adds `a.ImageUrl`.

### 3.3 Contracts

- `MusicSearchResultDto`: optional trailing `string? ImageUrl = null, string? ArtistImageUrl = null, string? AlbumTitle = null`.
  For an artist search result `ImageUrl` is the artist image.
- `SongSuggestionDto`: optional trailing `string? ImageUrl = null, string? ArtistImageUrl = null, string? AlbumTitle = null`;
  computed `public string? RowImageUrl => ImageUrl ?? ArtistImageUrl;`.
- `ArtistSuggestionDto`: optional trailing `string? ImageUrl = null`.
- `SongListItemDto`: optional trailing `string? Version = null, string? ImageUrl = null` (`ImageUrl`
  is already the row image); computed `public string SupportingText` per REQ-ART-20.
- `AutocompleteSuggestion`: see the component change (§ 4.2).

### 3.4 Services

- `DeezerProvider`: the private JSON records gain `album { title, cover_medium }` and
  `artist { picture_medium }` on tracks and `picture_medium` on artists; every URL goes through
  `ImageUrlRules.Normalize`. Still one request per search (REQ-ART-04).
- `SongSuggestionService`: `MapLocal` copies the hit's two URLs; the remote mapping copies the three
  new provider values. Dedup, promotion, limits and status logic are unchanged. A Deezer row promoted
  to a local option shows the **stored** song's images (it is a local option).
- `ArtistSuggestionService`: the remote mapping copies `ImageUrl`.
- `SongService.SaveFromFormAsync` (the one write path, REQ-SFT-46):
  - add mode: `Song.ImageUrl = ImageUrlRules.Normalize(request.ImageUrl)`; not cleared when the
    identity is nulled for "Save as new version / new song" (REQ-ART-15);
  - artist (REQ-ART-16), with `img = ImageUrlRules.Normalize(request.ArtistImageUrl)` not null:
    created artist → `ImageUrl = img`; existing artist reached by `request.ArtistId` or by the exact
    collated name match, with `ImageUrl` null → `ImageUrl = img`; existing artist chosen through the
    near-duplicate decision (`ArtistDecision.UseExisting`) → unchanged. `SaveFromFormAsync` resolves
    the artist in separate branches (by id, by the use-existing decision, by exact match, by
    create); the private `SaveFormResolution` gains `bool FillArtistImage`, false only for the
    use-existing branch, and the private create helper receives the two normalised URLs and that
    flag. Public `ISongService` signatures are unchanged;
  - edit mode: `Song.ImageUrl` is not assigned (REQ-ART-17). The artist rule above does not run in
    edit mode because the ViewModel has no pending image there.
  - All inside the existing unit of work, so a failed Save stores nothing (REQ-ART-18).

### 3.5 ViewModel (`SongFormViewModel`)

- Two private pending values: `_pendingImageUrl`, `_pendingArtistImageUrl`.
- `ApplyRemotePick`: sets both from `dto.ImageUrl` / `dto.ArtistImageUrl` (REQ-ART-11).
- `ClearTitle`: clears `_pendingImageUrl` (REQ-ART-13). `ClearArtist`: clears both.
- `BuildSaveRequest`: add mode sends both; edit mode sends null for both. The pending values are
  not cleared by a refused or failed Save, nor by closing a sheet (REQ-ART-18); only `ClearTitle`,
  `ClearArtist` and a new Deezer pick change them.
- `ToTitleOption` (REQ-ART-06/07/08): `LeadingKind = Image`, `ImageUrl = dto.RowImageUrl`,
  `PlaceholderIcon = "music_note_outlined"`, `TrailingText = dto.IsRemote ? "Deezer" : "Library"`,
  supporting text = non-empty parts of artist, version, album (album only when `IsRemote` and not
  equal to the title, `StringComparison.OrdinalIgnoreCase`; this is display text, not search or
  de-duplication, so the collation rule does not apply). Building these strings is presentation
  formatting, as `ToTitleOption` already does today; no business decision moves into the ViewModel.
- Artist options (REQ-ART-09/10): `LeadingKind = Avatar`, `ImageUrl = a.ImageUrl`,
  `PlaceholderIcon = "account_circle_outlined"`, supporting text `a.CatalogCountText` as today.
- Icon names and the two source words are constants in the ViewModel (no magic strings repeated).

### 3.6 UI

- `SongsPage.xaml`: both templates (normal and selected) replace `ListItemLeadingIcon` with
  `<lists:ListItemLeadingImage ImageSource="{Binding ImageUrl}" PlaceholderIcon="music_note_outlined" Margin="0,8" />`
  and bind `SupportingText="{Binding SupportingText}"`. The `Margin="0,8"` on the slot content gives
  the 72 dp row (REQ-ART-21) without changing `ListItem`. The trailing content is unchanged.
- No other page changes. `PersonFormPage` options carry `LeadingKind = None` (the default).

### 3.7 Image loading and cache (REQ-ART-22/23)

A URL string bound to an `ImageSource` property becomes a `UriImageSource`, which uses the platform
loader and its cache (the same path `YouTubeSearchPage` thumbnails use today). No image package is
added. The placeholder sits **under** the image in the same cell: it is visible until the image has
drawn over it, and stays visible when the source is null or the load fails. So no loading-state code
and no error handling are needed, and a failed load cannot leave an empty square.

## 4. Component changes (governed, own tasks, own records)

Both records hold the four gates (MD3 review, consumer map, per-consumer risk, approval).

### 4.1 `ListItemLeadingImage` placeholder + new `ListItemLeadingAvatar`

Record: `Docs/Management/DevCycleCraft/m3-lists/changes/2026-11-13-listitem-leading-media-placeholder-on-listitemleadingimage-and-new-listitemleadingavatar/README.md`.

### 4.2 `AutocompleteDXField` option rows

Record: `Docs/Management/DevCycleCraft/autocomplete-component/changes/2026-11-13-autocompletedxfield-option-rows-optional-leading-image-or-avatar-and-trailing-text/README.md`.

## 5. Error handling

No new try/catch. Missing JSON fields deserialize to null. Image load failures are handled by the
platform loader and hidden by the placeholder. Save failures follow the existing outcomes.

## 6. Testing (risk tiers per `testing.md`)

| Area | Level | Tests |
|------|-------|-------|
| `ImageUrlRules.Normalize` | A | unit: https kept, http / relative / empty / whitespace / over 500 → null, trimming |
| `DeezerProvider` mapping | B | unit with the existing `StubHandler`: fields mapped; missing album / artist / field → null; invalid URL → null; one request per search |
| Suggestion services | B | unit: local hit and remote row carry the URLs and album; promoted row shows stored images; remote artist suggestion carries the image (REQ-ART-24) |
| `SongService.SaveFromFormAsync` | A | unit: REQ-ART-15 (incl. both "Save as new" decisions), REQ-ART-16 (created, exact match empty, exact match filled, near-duplicate choice), REQ-ART-17, REQ-ART-18 |
| `SongFormViewModel` | A | unit: REQ-ART-06/07/08 option content, REQ-ART-09/10, REQ-ART-11/12/13/14, request content in add and edit mode |
| Repositories | B | integration on real SQLite: list projection coalesces song → artist image and carries Version (plain and catalog); title candidates carry both URLs; artist search returns the real catalog count and the image |
| Migration | B | integration: both columns exist and are null for existing rows |
| `SongListItemDto.SupportingText` | A | unit: all combinations of empty parts |
| Components, `SongsPage.xaml` | C | no unit test (pure XAML, no logic). REQ-ART-19 (selected state, catalog mode on screen), 21, 22 and 23 are verified by the T11 device checklist and recorded in the traceability matrix as manual E2E |

Every test carries `// [AC] REQ-ART-NN: …` as its first line. The new optional parameters keep
existing tests compiling. **One planned exception to "tests are not edited":** four tests of
`SongFormViewModelTests` assert the REQ-SFT-04 supporting text that REQ-ART-07/08 replace
(`TitleOptionSource_LocalHit_SupportingTextHasArtistVersionAndLibraryMarker`,
`TitleOptionSource_LocalHitEmptyVersion_OmitsVersionPart`,
`TitleOptionSource_RemoteHit_SupportingTextIsArtistVersionAndDeezer`,
`TitleOptionSource_RemoteHitEmptyVersion_OmitsVersionPart`). They are updated in T8 to the new
text, each `[AC]` line renamed to REQ-ART-07 or REQ-ART-08, and listed in the task-log. All other
existing tests stay unedited; if one fails, the task stops and reports.

## 7. Out of scope

See `requirements.md § Out of scope`.
