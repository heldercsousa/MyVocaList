# Supersession map: removing Song.Version and Song.FeaturedArtists

Companion to `requirements.md` of this folder (REQ-RMV-01..11). Lists, per shipped spec, what the removal supersedes. Shipped specs are not edited. Line numbers are those of the file as it stands on 2026-11-20. Identity after this change: `(Artist, Title)`.

Path abbreviations (all under `Docs/Management/`):

- **ARS-REQ / ARS-DES** = `BusinessFeatures/artists-songs/requirements.md` / `design.md`
- **SIR-REQ / SIR-DES** = `BusinessFeatures/artists-songs/song-import-resolution/requirements.md` / `design.md` (the folder lives under `artists-songs/`)
- **SFT-REQ / SFT-DES** = `BusinessFeatures/artists-songs/changes/2026-09-24-song-form-title-first-one-search-over-local-and-music-apis/requirements.md` / `design.md`
- **ART-REQ / ART-DES** = `.../changes/2026-11-11-show-deezer-artwork-as-list-item-icons-in-song-and-artist-search-results/requirements.md` / `design.md`
- **PIC-REQ / PIC-DES** = `.../changes/2026-11-13-pictures-for-songs-and-artists-upload-with-automatic-crop-kept-as-app-private-files/requirements.md` / `design.md`
- **LYR-REQ / LYR-DES** = `.../changes/2026-11-14-song-form-a-deezer-pick-fills-lyrics-when-a-lyrics-match-exists/requirements.md` / `design.md`
- **SLD-REQ / SLD-DES** = `.../changes/2026-11-16-songs-list-searches-title-and-artist-deezer-versions-collapsed-deezer-artist-frozen-lyrics-on-first-open/requirements.md` / `design.md`
- **FUX-REQ** = `.../changes/2026-07-10-form-ux-redesign/requirements.md`

## Table

| Spec file | ID / line | What it says | Effect |
|---|---|---|---|
| ARS-REQ | l.9 (header note) | "Song.Version and 3-col unique index added to data model" | Removed (historic note; Version and the 3-column index leave the model) |
| ARS-REQ | AC-9.3 (l.234) | Catalog list row shows `Title` as headline and `FeaturedArtists` as supporting text | Reduced to: headline `Title`; supporting text per REQ-RMV-09 (`Artist` only, where the row shows it) |
| ARS-REQ | AC-10.2 (l.250) | Add form shows Artist, Title, `Featured Artists` (optional), Lyrics, API search strip, Cancel, Save | Reduced to: Artist, Title, Lyrics, Cancel, Save (no Featured Artists). The API-search-strip clause was already superseded by FUX-REQ REQ-FORMUX-26 |
| ARS-REQ | AC-11.2 (l.275) | An API result populates `Title`, `FeaturedArtists` and `Artist` | Reduced to: populates Title and Artist (already superseded by REQ-FORMUX-22..24 / REQ-SFT-12; the `FeaturedArtists` part is removed in any case) |
| ARS-REQ | US-12 story (l.288) | "I want to edit a song's title, featured artists, or lyrics" | Reworded: "title or lyrics" (AC-12.1..12.4 unaffected) |
| ARS-REQ | AC-14.1 (l.323) | Lyrics editor "below `FeaturedArtists`" | Reworded: Lyrics editor in the Song form, below Title (field order per REQ-RMV-01) |
| ARS-REQ | Data model, Song table `FeaturedArtists` (l.368) | `string?`, maxLen 200 | Removed |
| ARS-REQ | Data model, Song table `Version` (l.369) | `string?` variant label | Removed |
| ARS-REQ | Indexes, `IX_Songs_ArtistId_Title_Version` (l.379) | Composite unique on `(ArtistId, Title, Version)` | Reduced to `IX_Songs_ArtistId_Title`, unique on `(ArtistId, Title)` (REQ-RMV-03) |
| ARS-REQ | Validation, Song / Title row (l.423 area, "unique per artist") | "This artist already has a song with this title." | Kept unchanged; it becomes the only uniqueness rule (note: SFT-REQ's validation table and the create path used "title and version" wording, see below) |
| ARS-REQ | Validation, Song / `FeaturedArtists` (l.428) | optional; maxLen 200; "Featured artists text too long..." | Removed (with message) |
| ARS-DES | l.12 (header note) | "Song.Version added; IX_Songs_ArtistId_Title_Version replaces 2-col index" | Removed (historic note) |
| ARS-DES | l.72 | `Song.FeaturedArtists` property | Removed |
| ARS-DES | l.73 | `Song.Version` property | Removed |
| ARS-DES | l.164, l.174 | `FeaturedArtists` in two DTO/request records | Removed |
| ARS-DES | l.214 | EF config for `FeaturedArtists` (TEXT, maxLen 200) | Removed |
| ARS-DES | l.219 | `HasIndex(ArtistId, Title, Version)` named `IX_Songs_ArtistId_Title_Version` | Reduced to `HasIndex(ArtistId, Title)` named `IX_Songs_ArtistId_Title` |
| ARS-DES | l.317 | `CreateSongAsync(..., string? featuredArtists = null, ...)` | Reduced to a signature without `featuredArtists` |
| ARS-DES | l.318 | `UpdateSongAsync(id, title, string? featuredArtists, ...)` | Reduced to a signature without `featuredArtists` |
| ARS-DES | l.402 | Song form control table: "FeaturedArtists field / TextEdit / Optional" | Removed |
| ARS-DES | l.512 | `[ObservableProperty] string? _featuredArtists` | Removed |
| ARS-DES | l.549 | Flow step "Admin types title; optionally fills FeaturedArtists and Lyrics" | Reworded: "types title; optionally fills Lyrics" |
| SIR-REQ | l.13 (Purpose 2) | Avoid duplication while allowing deliberate version variants (live, acoustic, remix) as distinct records | Removed (no variants as distinct records; same artist and title cannot coexist) |
| SIR-REQ | Vocabulary "Version / Variant" (l.24) | `Song.Version` label; `""` = canonical | Removed (term retired) |
| SIR-REQ | Vocabulary "Exact local match" (l.26) | Match on `(ArtistId, Title, Version)` under `NOCASE_NOACCENT` | Reduced to `(ArtistId, Title)` |
| SIR-REQ | Vocabulary "Resolution" (l.23) | `NoMatch`, `ExactExternalMatch`, `ExactLocalMatch`, `FuzzyCandidates` | Kept (list unchanged; note the shipped Song form no longer uses the merge/update paths, see SFT-REQ REQ-SFT-24/37/38) |
| SIR-REQ | AC-1.1 (l.36) | Exact `(ArtistId, Title, Version="")` match offers "Update existing", "Save as new version" or "Cancel" | Reduced to: exact `(Artist, Title)` match offers *use existing* (open) and *cancel* only (REQ-RMV-06/07). "Update existing" is already gone (REQ-SFT-24) |
| SIR-REQ | AC-1.2 (l.37) | "Save as new version" requires a non-empty `Version` | Removed |
| SIR-REQ | AC-1.3 (l.38) | Unique `(ArtistId, Title, Version)` triple inserts directly with no sheet | Reduced to `(Artist, Title)` |
| SIR-REQ | AC-1.4 (l.39) | Fuzzy near-duplicate lists candidates, option to proceed as a new record | Kept (REQ-RMV-07 keeps `SaveAsNewSong` for near-duplicates) |
| SIR-REQ | AC-2.2 (l.45) | API result with no external match but exact `(ArtistId, Title, Version)` local match gives `ExactLocalMatch` | Reduced to `(ArtistId, Title)` |
| SIR-REQ | AC-5.3 (l.71) | `(ArtistId, Title, Version)` unique index enforces dedup at DB level | Reduced to the `(ArtistId, Title)` unique index |
| SIR-REQ | AC-5.4 (l.73) | `Version` non-nullable, default `""` (avoids SQLite multi-NULL) | Removed |
| SIR-REQ | AC-5.5 (l.72) | Migration introducing `Version` and the 3-col index must fail loudly on pre-existing duplicates; test that the 3-col index rejects a duplicate triple | Removed (historic one-off, shipped). The new migration instead renames collisions and never fails (REQ-RMV-04); its own test is REQ-RMV-04's migration test |
| SIR-REQ | INV-2 (l.105) | No two songs share `(ArtistId, Title, Version)` under `NOCASE_NOACCENT` | Reduced to `(ArtistId, Title)` |
| SIR-REQ | Out of Scope, cross-artist dedup (l.99) | Same title under two different artists is intentionally two records | Kept (identity is `(Artist, Title)`) |
| SIR-DES | l.27-31 (§2 "Song.Version (new)") | `Version` non-null, default `""`, max 60; M1 collation; replaces `IX_Songs_ArtistId_Title` with `..._Title_Version`; `""` = canonical | Removed |
| SIR-DES | l.35-38 (§2 migration, M3) | Add `Version` column, drop 2-col index, create 3-col index, "fail loudly" precondition, no data wipe | Removed (historic; superseded by REQ-RMV-03/04) |
| SIR-DES | l.49-50 | `SongCandidate(Title, Version, FeaturedArtists, ...)` | Reduced to a candidate without `Version` and `FeaturedArtists` |
| SIR-DES | l.65 | `SongMatch(SongId, Title, Version, ArtistName, Score)` | Reduced to a record without `Version` |
| SIR-DES | l.102, l.108 | `ResolutionChoice { CreateNew, CreateNewVersion, UpdateExisting, AttachExternalId }` | Reduced: `CreateNewVersion` removed (REQ-RMV-07 `SongDecision.SaveAsNewVersion`) |
| SIR-DES | l.116 | Exact local match via `ExistsByTitleVersionForArtistAsync(artistId, title, version)` | Reduced to an `(artistId, title)` check (method renamed; no Version argument) |
| SIR-DES | l.118 | `titlePrefixToken` example "Bohemian Rhapsody (Live)", "tolerant of trailing variant suffixes" | Reworded (example and variant-suffix rationale lose meaning; superseded in any case by the trigram pool, SFT-REQ REQ-SFT-23 note) |
| SIR-DES | l.121 | `CreateNew` / `CreateNewVersion` pass the chosen `Version`; `CreateNewVersion` requires non-empty Version (AC-1.2) | Reduced to `CreateNew` only, no Version |
| SIR-DES | l.123 | Mergeable field set: `Title`, `FeaturedArtists`, `Lyrics`, `Version` | Reduced to `Title`, `Lyrics` (the merge sheet itself is already removed, REQ-SFT-38) |
| SIR-DES | l.139 | Resolution sheet actions: "Update existing - Save as new version (reveals Version entry) - Cancel" | Reduced to: use existing / cancel (plus *Save as new song* on near-duplicates). Already partly superseded by REQ-SFT-22/24 |
| SIR-DES | l.147 | `SongCandidate(Title, SongVersion, FeaturedArtists, Lyrics, ...)` built in `SaveAsync` | Removed (Version and FeaturedArtists parts) |
| SIR-DES | l.149 | `CreateSongWithUrlsAsync(..., Title, Version, FeaturedArtists, Lyrics, ...)` call | Reduced to a call without the two arguments |
| SIR-DES | l.154 | "Save as new version": `ConfirmSaveAsNewVersionCommand` validates non-empty `SongVersion` | Removed |
| SIR-DES | l.165 | SongPickedMessage handler "populate title/featured" | Reworded: "populate title" |
| SIR-DES | l.170 | `CreateSongWithUrlsAsync(artistId, title, version, featured, lyrics, ...)` | Reduced to a signature without `version`, `featured` |
| SIR-DES | l.173 | `ExistsByTitleVersionForArtistAsync(artistId, title, version, ct)` and `excludeId` overload | Reduced to `(artistId, title, [excludeId])` |
| SIR-DES | l.179 | Service tests: "CreateNewVersion empty-version rejection" | Removed |
| SIR-DES | l.180 | Infra tests: 3-column index, `ExistsByTitleVersionForArtistAsync`, "duplicate `(ArtistId, Title, Version)`" | Reduced to the 2-column equivalents |
| SIR-DES | l.183 | Smoke gate "update vs new-version" | Reworded: "use existing vs save as new song" |
| SIR-DES | l.192-193 | Wave table: Domain `Song.Version`; Infra "Version + 3-col unique index" | Removed (historic wave plan) |
| SIR-DES | l.202 (§10 key decision 1) | "Version variants are first-class via `Song.Version`; near/exact matches prompt a confirm sheet" | Removed (decision reversed by Helder, 2026-11-19) |
| SFT-REQ | REQ-SFT-01 (l.73-75) | Field order Title, Artist, Version, Featured Artists, Lyrics, YouTube | Reduced to Title, Artist, Lyrics, YouTube. Note: REQ-RMV-01 writes "Artist, Title, Lyrics"; REQ-SFT-01 and the pictures change put Title first. See Ambiguity 1 |
| SFT-REQ | REQ-SFT-04 (l.86-101) | Option supporting text "artist · version · In your library/Deezer"; example "Don't Cry" with Version "" and "Live" gives two local options | Reduced to artist only (later changed by REQ-ART-07/08 and REQ-SLD-08; the Version part and the two-option example are removed) |
| SFT-REQ | REQ-SFT-05(b) (l.105-107) | "Exists locally" = collated match on title, artist name **and version** (Deezer: `title_short` + normalised `title_version`) | Reduced to title and artist name only |
| SFT-REQ | REQ-SFT-05 spec note (l.110-112) | (b) compares the same three values a pick would save | Removed |
| SFT-REQ | REQ-SFT-12 (l.172-197) | Pick fills Title = `title_short`, **Version** = normalised `title_version` (bracket stripping, Q17; overwrite with "", Q20); Version not locked; Save persists Title and Version; identity (artist, title, version); example "Creep (Live)" | Reduced to: Title = `title_short` (fallback `title`), Artist and pending identity as today; Version bullets, normalisation, example and identity wording removed (REQ-RMV-08). Replaced for Version already by REQ-SLD-08 |
| SFT-REQ | REQ-SFT-12 bullet l.182-183 | "Version stays an ordinary editable field. Featured Artists and Lyrics are unchanged." | Reduced to "Lyrics unchanged" (REQ-LYR-01 already replaced Lyrics) |
| SFT-REQ | REQ-SFT-14 (l.205-209) | X on Title also resets a pick-filled Version; X on Artist leaves Version; discard-sheet consequence | Removed (Version reset paragraph; already inert after REQ-SLD-08) |
| SFT-REQ | REQ-SFT-22 (l.267-271) | Exact duplicate sheet offers **Open existing** and **Save as new version** (requires non-empty Version, AC-1.2) | Reduced to: *Open existing* and *Cancel* (REQ-RMV-07). Match on `(artist, title)` |
| SFT-REQ | REQ-SFT-23 (l.272-275) | Near-duplicate sheet offers **Save as new song**, "no Version requirement" | Reworded: drop "with no Version requirement"; the action stays |
| SFT-REQ | REQ-SFT-25 (l.279-284) | Exact-duplicate sheet "(REQ-SFT-22) offers Open existing" for a Deezer id held by another song | Kept (sheet reference still valid; Save-as-new-version is no longer an alternative) |
| SFT-REQ | REQ-SFT-26 note (l.288) | With explicit `SaveAsNewVersion` or `SaveAsNewSong` the identity is nulled and the save succeeds (D1) | Reduced to `SaveAsNewSong` only |
| SFT-REQ | REQ-SFT-27 (l.300-302) | Edited title checked for uniqueness "with Version"; example with Version "" | Reworded: uniqueness on `(Artist, Title)` |
| SFT-REQ | REQ-SFT-29 (l.306-309) | Edit-save uniqueness includes Version (absorbed bug) | Removed (identity is `(Artist, Title)`; the edit-save check on title and artist stays and needs no Version clause) |
| SFT-REQ | REQ-SFT-39 note (l.340) | Row from explicit `SaveAsNewVersion` / `SaveAsNewSong` has NULL external identity | Reduced to `SaveAsNewSong` only |
| SFT-REQ | US/decision l.356 | "REQ-SFT-29 covers the fourth bug (edit-save ignores Version)" | Removed (bug note for the removed REQ) |
| SFT-REQ | Validation table, Version row (l.411) | optional, <= 60, required for "Save as new version", a pick fills it | Removed |
| SFT-REQ | Validation table, Song identity row (l.413) | unique `(ArtistId, Title, Version)` on create and update; "REQ-SFT-29 (update: changed)" | Reduced to unique `(ArtistId, Title)` |
| SFT-REQ | Failure modes (l.422) | Deezer `title_version` > 60 chars: pick fills as is; Save shows the Version length message | Removed |
| SFT-REQ | Decision log l.494-496 (Q4, Q5) | Q4: "Save as new song, no Version required"; Q5 = B: pick fills Version, Version persisted, identity artist+title+version | Q4 Reworded (drop "no Version required"); Q5 Removed (reversed by Helder 2026-11-19) |
| SFT-REQ | Decision log l.511-526 (Q17, Q20, Q21) | Version normalisation, pick overwrites Version with "", X on Title resets Version; l.524-526 featuring stays inside `title_short` | Removed (Q17, Q20, Q21 answers); l.524-526 note kept as history |
| SFT-REQ | Traceability l.550 | REQ-SFT-22..24, 42 map to `FindIdByTitleVersionForArtistAsync` | Reworded: repository method without Version (REQ-SFT-42 itself is unaffected: it matches the stored id of the collated `(artist, title)` match) |
| SFT-REQ | Traceability l.554 | REQ-SFT-29 row (`SongService.UpdateSongAsync`, regression test) | Removed |
| SFT-REQ | Traceability l.562 | REQ-SFT-12 (Q5 mapping): `DeezerProvider` title_short + normalised title_version, provider tests | Reduced to `title_short` mapping only |
| SFT-DES | l.15 | Q5/Q7 additions: `MusicSearchResultDto.Version`; `DeezerProvider` maps `title_version` | Reduced: `MusicSearchResultDto.Version` and the `title_version` mapping removed |
| SFT-DES | l.35 | Evidence: create checks title + version (`SongService.cs:219`) | Removed (historic evidence of the pre-change state) |
| SFT-DES | l.52, l.62-67 | `SongSearchHit(... Version ...)`; `FindByTitleAndArtistNamesAsync(... (Title, ArtistName, Version) ...)`; `FindIdByTitleVersionForArtistAsync(artistId, title, version, excludeId, ct)` | Reduced to signatures without Version (`FindIdByTitleVersionForArtistAsync` is renamed) |
| SFT-DES | l.99-108 (§3.2) | `CreateSongWithUrlsAsync(..., title, string version, string? featuredArtists, ...)`; `UpdateSongAsync(id, title, featuredArtists, ..., string? version)`; "Uniqueness = (effective artist, title, effective version)" | Reduced to signatures and uniqueness without version / featured |
| SFT-DES | l.112 | Update duplicate message becomes "A song with this title and version already exists for this artist" | Reworded: message names title and artist only (see Ambiguity 2) |
| SFT-DES | l.121-122 | `SongSuggestionDto` gains `Version` (remote: normalised `title_version`) | Removed |
| SFT-DES | l.154 | Library check on (artist name, normalised version) per REQ-SFT-05(b)/12 | Reduced to (title, artist name) |
| SFT-DES | l.176-182 | `DeezerProvider` maps `title_version`; `MusicSearchResultDto` trailing `Version`; `NormalizeVersion(title_version)` bracket rules | Removed |
| SFT-DES | l.218, l.224, l.231 | `SaveFromFormRequest(Title, Version, FeaturedArtists, Lyrics...)`; `SongDecision { None, SaveAsNewVersion, SaveAsNewSong }`; `SaveField { Title, Version, Artist, None }` | Reduced: request without Version/FeaturedArtists; `SaveAsNewVersion` and `SaveField.Version` removed |
| SFT-DES | l.247, l.256, l.261, l.265 | SaveFromFormAsync rules: validate version; `SaveAsNewVersion` -> `ValidateVersionInput(isRequired: true)` or `Failed(Version)`; `ValidateVersionInput(v,false)` incl. > 60; "SaveAsNewVersion requires a non-empty Version; SaveAsNewSong does not" | Removed (Version validation and the SaveAsNewVersion rule); rule order otherwise unchanged |
| SFT-DES | l.272 | Ruling D1: null identity when `SongDecision` is `SaveAsNewVersion` or `SaveAsNewSong` | Reduced to `SaveAsNewSong` only |
| SFT-DES | l.327, l.329, l.351 | `TitleOptionSource` supporting text "artist · version · In your library / Deezer"; `ApplyRemotePick` sets Version = normalised version | Reduced to artist (and later album/trailing text per ART); `ApplyRemotePick` Version write removed |
| SFT-DES | l.340 | VM members incl. `ConfirmSaveAsNewVersionCommand` | Removed (`ConfirmSaveAsNewVersionCommand`) |
| SFT-DES | l.398 | Flow: `ApplyRemotePick` sets `Version` = normalised `title_version` ("Original") | Removed (Version part) |
| SFT-DES | l.413-414 | Flow: exact sheet "Open existing / Save as new version"; "Save as new version/song" re-invokes the VM | Reduced to "Open existing / Cancel"; Save as new song stays for fuzzy |
| SFT-DES | l.439 | Edit save: `UpdateSongAsync` enforces (artist, title, version) uniqueness, REQ-SFT-29 | Reduced to (artist, title); REQ-SFT-29 reference removed |
| SFT-DES | l.630, l.632-633 | Test strategy: version-aware update (REQ-SFT-29 regression); `NormalizeVersion` theory; "pick fills Version, X resets pick-filled Version (Q21)" | Removed (tests encode superseded behaviour) |
| SFT-DES | l.654, l.662, l.666 | Deleted/rewritten test table: `CommitAsync_CreateNewVersion_*`; "dedup now artist- and version-scoped"; mocks of `ExistsByTitleVersionForArtistAsync` | Reworded (artist-scoped only; method renamed) |
| SFT-DES | l.729 (§12) | "Existing INV-1...INV-3 (`song-import-resolution`) hold" | Reworded: INV-2 now `(ArtistId, Title)` (see SIR-REQ INV-2) |
| ART-REQ | l.36-37 (User story 2) | Album separates options "with the same title, artist and version" | Reworded: "same title and artist" |
| ART-REQ | REQ-ART-07 (l.71-79) | Title-option supporting text = `artist · version · album`; examples with version "1999 Remaster" and "" | Reduced to `artist · album` (album only on Deezer options); examples reworded |
| ART-REQ | REQ-ART-12 (l.103-104) | A Deezer pick leaves **Featured artists**, Lyrics and karaoke URLs unchanged, writes nothing to the DB | Reduced to: leaves karaoke URLs unchanged and writes nothing (Lyrics already replaced by REQ-LYR-01/02/04); the Featured-artists clause is removed (REQ-RMV-11) |
| ART-REQ | REQ-ART-15 (l.118-121) | New song stores its image, also for "Save as new version" or "Save as new song" | Reduced: "Save as new song" only |
| ART-REQ | REQ-ART-18 (l.133-138) | Failed Save keeps pending images; later Save "including Save as new version and Save as new song" | Reduced: "Save as new song" only |
| ART-REQ | REQ-ART-20 (l.146-150) | Songs-list supporting text = non-empty parts of `artist · version · feat. <featured artists>` with examples "Radiohead · Live" | Reduced to `Artist` only (REQ-RMV-09); examples removed |
| ART-REQ | l.195-196 (Out of scope) | Version de-duplication of Deezer rows owned by the discussion item | Kept (historic pointer) |
| ART-REQ | l.209 (supersedes table) | REQ-SFT-04 marker "artist · version · In your library / Deezer" replaced by REQ-ART-07/08 | Reworded: read "artist · album" for the replacement text |
| ART-DES | D9 (l.25) | Separator "Cover + `artist · version · album` + source as trailing text"; "Version rows of one song differ by version text, album and cover" | Reduced to `artist · album` |
| ART-DES | D10 (l.26) | A pick overwrites Title, **Version**, Artist, identity, pending images; Featured artists, Lyrics, URLs never touched | Reduced to Title, Artist, identity, pending images |
| ART-DES | D12 (l.28) | Image kept when identity is dropped ("Save as new version / new song") | Reduced: "Save as new song" |
| ART-DES | D14 (l.30) | Songs list supporting text `artist · version · feat. <featured>` | Reduced to `artist` |
| ART-DES | l.82, l.111, l.127-128 | `SongListItemDto (+Version, ...)`; constructor `s.Version`; optional `string? Version = null`; computed `SupportingText` per REQ-ART-20 | Reduced: `Version` dropped from the DTO; `SupportingText` returns the artist only |
| ART-DES | l.142 | Identity nulled for "Save as new version / new song" (REQ-ART-15) | Reduced: "Save as new song" |
| ART-DES | l.165 | `ToTitleOption` supporting text = non-empty parts of artist, version, album | Reduced to artist, album |
| ART-DES | l.215, l.217 | Repository test "carries Version (plain and catalog)"; `SongListItemDto.SupportingText` all combinations of empty parts | Reduced: Version part dropped; the combinations test shrinks to artist presence |
| ART-DES | l.222-226 | Four `SongFormViewModelTests` on the "artist · version · marker" text (`...SupportingTextHasArtistVersionAndLibraryMarker`, `...EmptyVersion_OmitsVersionPart` x2, `...IsArtistVersionAndDeezer`) | Removed (the `EmptyVersion_OmitsVersionPart` pair), the other two rewritten to artist-only text |
| 2026-11-09 `song-list-item-shows-artist-name-version-and-featured-artists` | README l.2, 3, 8, 9, 14, 16, 19 | Item title, goal, gate and delivery note: row shows artist plus version plus featured artists, format `artist · version · feat. <featured artists>` | Reduced to artist only (shipped via REQ-ART-20; the version and featured parts are removed by REQ-RMV-09). Item title and goal become historic |
| 2026-11-11 `discuss-showing-long-song-titles...` | README l.8, 14 | Free the secondary text line "possibly by removing Version and Featured artists" | Kept (premise now decided: both removed, REQ-RMV-01) |
| PIC-REQ | REQ-PIC-21 (l.118-119) | Add-mode save keeps the picture also for "Save as new version" and "Save as new song" | Reduced: "Save as new song" only |
| PIC-REQ | REQ-PIC-32 (l.165-166) | Changing the artist is refused if two songs would share artist, title **and version** | Reduced to artist and title |
| PIC-REQ | l.230 (Out of scope) | "Version, Featured artists, Lyrics and YouTube URLs of the Song form: unchanged." | Reduced to "Lyrics and YouTube URLs ... unchanged" |
| PIC-DES | l.93 | Edit mode, changed artist: collision on (artist, title, version) returns `Failed` | Reduced to (artist, title) |
| LYR-REQ | REQ-LYR-11 (l.84) | "(Same rule as Version in REQ-SFT-14.)" | Reworded: the cross-reference no longer has a target; the rule itself (X removes untouched pick-filled lyrics) stands |
| LYR-REQ | l.127 (supersedes table, REQ-ART-12) | "Featured artists and karaoke URLs stay untouched" | Reduced: "karaoke URLs stay untouched" |
| LYR-DES | L8 (l.18) | X on Title/Artist removes pick-filled lyrics only if untouched; "Mirrors the Version rule (REQ-SFT-14)" | Reworded: drop the mirror reference |
| LYR-DES | l.142 | Flow: pick fills "Title, Artist, Version and pictures" | Reduced to "Title, Artist and pictures" |
| LYR-DES | l.173 | Test name `SelectTitle_RemoteOption_LeavesFeaturedLyricsAndKaraokeUrlsAndWritesNothing` | Removed or renamed with the Featured part (test stays green only if the name is changed or the test rewritten; see Ambiguity 3) |
| SLD-REQ | l.29 (Facts) | Deezer row carries "a short title without the version label, and the version label" | Reduced: the version label is no longer read |
| SLD-REQ | l.32-33 (Facts) | Versions carry no value; the version concern lives in YouTube links | Kept (rationale now realised) |
| SLD-REQ | Vocabulary "Version label" (l.43) | Deezer's `title_version` | Removed |
| SLD-REQ | REQ-SLD-06 (l.88-93) | One row per song group (Deezer artist id + short title) | Kept (REQ-RMV-08: results sharing a title are listed once per distinct `(Artist, Title)`; the artist-id key is a stricter, Deezer-side proxy, see Ambiguity 4) |
| SLD-REQ | REQ-SLD-07 (l.94-96) | Kept row = first with a **blank version label**, else first | Reduced to: kept row is the first row of the group in Deezer's order |
| SLD-REQ | REQ-SLD-08 (l.97-100) | A Deezer pick carries no version; Version field stays "for songs and versions the user types" | Removed (the Version field is gone; a pick carries no version by construction) |
| SLD-REQ | REQ-SLD-09 (l.101-106) | Library check runs on kept rows "with a blank version"; a library song stored only with "Live" does not hide the group | Reduced: library check on `(artist, title)`; the "Live"-only clause is removed |
| SLD-REQ | REQ-SLD-10, 11 (l.107-114) | 25-row grouping limit; row without artist id is its own group | Kept |
| SLD-REQ | REQ-SLD-18 (l.147-150) | Song stored through "Save as new version" or "Save as new song" is local-only | Reduced: "Save as new song" only |
| SLD-REQ | REQ-SLD-37 note (l.266) | Create from the form ("Save as new version", "Save as new song") is not an update | Reduced: "Save as new song" |
| SLD-REQ | Failure modes (l.296) | Only labelled versions of a song: one row, the first, shown without the label | Reworded: "Several rows of one song: one row, the first" |
| SLD-REQ | Supersedes table l.325-327, 332 | REQ-SFT-04 note replaced by REQ-SLD-06..08; REQ-SFT-12 replaced for Version; REQ-SFT-05 "applied after grouping with a blank version"; REQ-SFT-14 "Version reset no longer applies" | Reworded: the Version clauses are now moot (REQ-SFT-05 compares `(artist, title)`; REQ-SFT-14 has no Version part) |
| SLD-DES | H3 (l.14), C2 (l.26), C3 (l.27), C23 (l.47), C27 (l.51) | Collapse decision; version on a Deezer pick left as the user had it; kept row = first with blank version label; removed test of the Version reset on X; album part stays "Only the version label was decided" | H3 Kept; C2 Removed; C3 Reduced to "first row of the group"; C23 Kept (already removed); C27 Reworded: album stays, no version part exists |
| SLD-DES | l.97 | Rejected alternative "dropping every row with a version label" | Kept (history) |
| SLD-DES | l.116 | `SongSuggestionDto`: unchanged; a Deezer row leaves the service with `Version = ""` | Reduced: `SongSuggestionDto` loses `Version` (REQ-RMV-08/10) |
| SLD-DES | l.146 | `SearchTitleRemoteAsync` groups rows, "set `Version` to blank on the kept rows", then library check | Reduced: grouping and kept-row choice stay; the blank-Version step is removed |
| SLD-DES | l.173, l.180, l.185-187 | `ApplyRemotePick` no longer writes Version; option row template "artist · version · Deezer"; flow "kept rows with blank version"; edit a Deezer song: "Title, Version, Featured artists, Lyrics, picture and YouTube links editable" | Reduced: Version mentions removed; Edit editable set becomes Title, Lyrics, picture, YouTube links |
| SLD-DES | l.198 (invariant) | "A Deezer suggestion row always has a blank version." | Removed |
| SLD-DES | l.209, l.211, l.218-219 | Test strategy: grouping "library check sees the blank version"; "save as new version stores no identity"; VM "Version untouched by a pick"; on-device "Deezer option row shows no version label" | Reduced: drop the Version-specific assertions; "save as new version" case Removed |
| FUX-REQ | l.124 (validation) | Song Title "unique per `(ArtistId, Title, Version)`" | Reduced to `(ArtistId, Title)` |
| FUX-REQ | l.126 | "Lyrics / FeaturedArtists: unchanged" | Reduced to "Lyrics: unchanged" |
| FUX-REQ | l.170 (Out of scope) | Resolution engine itself (`Song.Version` logic) "consumed unchanged" | Removed (the Version logic no longer exists) |
| 2026-09-24 README | l.40, 62, 69, 80 | Origin notes: save-as-new-version resolution a removal candidate; "version" goes in Version and is shown in search results; SIMPLIFY sheet to "Open existing / Save as new version"; edit-save uniqueness ignores Version | Removed (historic context; l.40 is the first hint of this removal) |
| 2026-06-30 `form-validation-update-songs` / `-artists` READMEs | whole files | No Version or FeaturedArtists text | None (no effect; listed because requested) |
| 2026-07-10, 2026-11-11 (artistform, title-limit), 2026-11-13 (artists-list avatar, artist-form picture, backfill, song-form picture), 2026-11-16 (send-corrected-lyrics, thin-song-row) | READMEs / requirements / design | No requirement mentions Version or FeaturedArtists, apart from the rows above (`send-corrected-lyrics` README l.22 and `thin-song-row` README l.17, 49 only name the sibling folder "versions collapsed") | None |
| `DevCycleCraft/persisted-string-trimming/requirements.md` | REQ-TRIM-05 (l.72-76), 06 (l.77-80), 07 (l.81-83) | Name-like Song values trimmed and whitespace-collapsed; optional string empty -> null via `ValueConverter` | None to the text. REQ-TRIM-07 is generic ("e.g. Person.Email, birthday") and names neither `Version` nor `FeaturedArtists`; the converters on those two properties disappear with the properties (REQ-RMV-10). See Ambiguity 5 |

## Terms and vocabulary retired or renamed

| Shipped term | Effect |
|---|---|
| *Save as new version* (sheet action, `SongDecision.SaveAsNewVersion`, `ConfirmSaveAsNewVersionCommand`, `ResolutionChoice.CreateNewVersion`) | Removed. *Save as new song* stays, for near-duplicate (trigram) matches only |
| *Version* / *Variant* / *Version label* (`Song.Version`, Deezer `title_version`, `SongSuggestionDto.Version`, `MusicSearchResultDto.Version`, `SaveField.Version`, `SongCandidate.Version`, `SongMatch.Version`, `SongListItemDto.Version`) | Removed |
| *Featured artists* (`Song.FeaturedArtists`, form field, `feat. <featured artists>` supporting-text part, request/DTO members) | Removed |
| `ValidateVersionInput`, `MaxVersionLength` (60), Version length message | Removed |
| `ExistsByTitleVersionForArtistAsync`, `FindIdByTitleVersionForArtistAsync` (Version in the name) | Renamed to drop "Version" (e.g. `ExistsByTitleForArtistAsync`, `FindIdByTitleForArtistAsync`); name choice belongs to the design |
| `IX_Songs_ArtistId_Title_Version`, "3-column unique index" | Replaced by `IX_Songs_ArtistId_Title`, "2-column unique index" |
| *Song identity* (artist + title + version) | Becomes `(Artist, Title)` |
| *Exact local match* `(ArtistId, Title, Version)` | Becomes `(ArtistId, Title)` |
| Song supporting text `artist · version · feat. <featured>` / option text `artist · version · album` | Becomes `Artist` / `artist · album` |
| Duplicate message "A song with this title and version already exists for this artist" (SFT-DES l.112) | Becomes a title-and-artist message (wording to be fixed in this change's design) |

## Ambiguities needing a decision

1. **Field order.** REQ-RMV-01 says "Artist, Title, Lyrics", but REQ-SFT-01 (and the picture/title-first forms) put Title first, then Artist. Confirm the intended order.
2. **Duplicate message wording.** The create path says "A song with this title and version already exists for this artist"; ARS-REQ keeps "This artist already has a song with this title." Pick one string.
3. **Test names.** LYR-DES l.173 names a test with "FeaturedLyrics..."; removing FeaturedArtists forces a rename of that test, which the Builder may not do without the spec saying so. State it in the change's tasks.
4. **Grouping key vs. REQ-RMV-08.** REQ-SLD-06 groups Deezer rows on the Deezer artist id and short title, while REQ-RMV-08 says "once per distinct `(Artist, Title)`". Confirm that the SLD grouping stays as is (Deezer artist id) and the library check alone uses the collated `(Artist, Title)`.
5. **REQ-TRIM-07.** REQ-RMV-10 calls it "the FeaturedArtists trim/validation (REQ-TRIM-07)", but REQ-TRIM-07 never names that field. No edit to the trimming spec is needed; REQ-RMV-10 could cite "the `ValueConverter` on `Song.FeaturedArtists` and `Song.Version`" instead.
6. **`song-import-resolution` live status.** Its merge sheet, "Update existing", `ResolveAsync`/`CommitAsync` and `ResolutionChoice` were largely removed or replaced by the title-first change (REQ-SFT-24, 37, 38, 51). Rows above mark only the Version/FeaturedArtists effect; REQ-RMV-07's `SaveAsNewSong` wording assumes the SFT-era `SongDecision` enum is the live one.
