# Design: Songs list search, one Deezer row per song, fixed Deezer artist, lyrics on first open

> **Status: APPROVED 2026-11-16 under Helder's instruction of the same day** ("You are authorized in advance to proceed with all next needed steps. Be autonomous."). Reviewed by a fresh spec reviewer (pass with fixes; fixes applied, decisions C14 to C19). The on-device check stays Helder's. Requirements: `requirements.md` (REQ-SLD-01 to 31).
> Code facts below were read on `develop` on 2026-11-16; line numbers are a guide, not a contract.

## 1. Decisions

### 1.1 Decided by Helder (2026-11-16)

| # | Decision |
|---|----------|
| H1 | Storing only the Deezer id is rejected; a Deezer song stays a small local row. |
| H2 | The Songs list search should do the search the form autocomplete does. |
| H3 | Deezer versions are collapsed. An exact comparison of Deezer's own short title and artist id, with no normalisation, is acceptable under the "no C#-side text comparison for de-duplication" rule. Applies to every place that shows Deezer song rows. |
| H4 | The artist of a Deezer song is frozen in add and edit mode, with no escape. The artist stays editable only for local-only songs. |
| H5 | The frozen artist is shown as plain text in the page; the shared tap-to-edit component is not changed. The Service also refuses the change. |
| H6 | Lyrics of a Deezer song are fetched on first open, a copy is kept, and edits are marked. |
| H7 | A song that nothing references can be deleted; blocking deletion waits for the event-queue work. |
| H8 | Searching Deezer from the Songs list is a separate, undecided trial. Sending lyrics to LRCLIB is post-MVP. |

### 1.2 Decided by Claude for Helder (overrule any of them)

| # | Question | Decision | Why | Cost to reverse |
|---|----------|----------|-----|-----------------|
| C1 | Title of a Deezer song | Stays editable (REQ-SLD-17) | Helder froze the artist only; the 2026-09-26 rule says a correction must never need delete and re-create | Low: one more plain-text state in the page plus a Service check |
| C2 | Version on a Deezer pick | Left as the user had it, normally blank (REQ-SLD-08) | H3: versions carry no value; a kept "Live" label on a collapsed row would be misleading | Low |
| C3 | Which row of a group is kept | First with a blank version label, else first (REQ-SLD-07) | The plain recording is the best lyrics and artwork match | Low |
| C4 | Order of list search results | Best match first when full-text applies, by title otherwise (REQ-SLD-02) | Same ranking as the form; a title-ordered full-text result would bury the exact hit | Low: one ORDER BY |
| C5 | Re-ranking in memory | Not done in the list | The form re-ranks 5 rows with the similarity scorer; a paged list cannot be re-ranked across pages without loading everything | Medium |
| C6 | Cap on list search results | None | Every match stays reachable (REQ-SLD-03). The full-text match is broad, so weak matches appear at the end of a long scroll; accepted, checked on the device | Low: one named constant |
| C7 | Where the origin of lyrics is kept | One nullable column `LyricsOrigin` on `Songs`, four states (REQ vocabulary) | One small column answers both "was a lookup already tried" and "did the user write this" | Medium: migration |
| C8 | When a match on first open is stored | At once, by the Service (REQ-SLD-20) | "Keep a copy" must not depend on the user pressing Save | Low |
| C9 | Lookup on open for local-only songs | No (REQ-SLD-24) | A typed title of an unknown artist can match another song's lyrics | Low |
| C10 | Retry after *none found* | Never automatic | Avoids a request on every open of an instrumental or unknown song | Low |
| C11 | Emptying the Lyrics field | Origin becomes *user* (REQ-SLD-27) | Otherwise the next open would fetch the lyrics the user just removed | Low |
| C12 | Number of rows requested from Deezer for tracks | A named constant, value 25 | Deezer's own default page size as far as known; enough to leave 5 after grouping. **To confirm in the Deezer API reference at implementation (task 0)** | Low |
| C13 | Catalog mode search | Unchanged (REQ-SLD-05) | Different service and query; not what was asked | Low |
| C14 | Artist after the Title text is changed following a Deezer pick (add mode) | Becomes editable, text kept (REQ-SLD-14) | By REQ-PIC-27 that song is saved as local-only, and Helder keeps the artist of local-only songs editable. Raised by the spec review | Low |
| C15 | What a frozen form sends on Save in edit mode | The stored artist id; no resolution, no sheet (REQ-SLD-30) | A name resolution could match another artist or raise a sheet for a field the user cannot change | Low |
| C16 | Save pressed while the lookup on open runs | Save waits for the lookup to end, then writes (REQ-SLD-31) | Two writers on one song row and one database context must not overlap; no lyrics are lost | Low |
| C17 | "None found" after the user corrected the title, or for lyrics over the limit | Permanent, as for any no-match | No stored data tells these cases apart; the user can type or paste lyrics | Low |
| C18 | No page-size parameter in the Deezer API | Group the rows of the default answer (REQ-SLD-10) | The feature still works with fewer rows | Low |
| C19 | Exact member names and signatures | Fixed in `plan.md`, from the shapes in § 3 | Names follow the code style reference, read at plan time | None |
| C20 | Page-size parameter of the Deezer request | Not sent; up to 25 rows of the default answer are grouped | The official reference could not be read; a recorded real answer shows 25 rows by default; the existing REQ-SFT-35 URL test stays valid. Replaces the "to confirm" of C12 | Low |
| C21 | Shape of the list search in the repository | Inside the existing `GetPagedAsync`; no new interface member, no Service change | Keeps two existing tests and a hand-written repository fake valid; every REQ is met | Low |
| C22 | The form's pick lookup calls the new lookup method | Yes; mocks in `SongFormViewModelLyricsTests` are re-pointed without changing any assertion | The form must tell "no match" from "no answer" (REQ-SLD-26 rule 3) | Low |
| C23 | Existing test of the Version reset on X | Removed with the code it tests | Its precondition no longer exists (REQ-SLD-08) | Low |
| C24 | How the song service gets the lyrics service | **Revised 2026-11-16 during T9a:** resolved from the unit-of-work scope's provider inside `EnsureLyricsAsync` only (non-throwing), and used after that read scope has closed | The first choice, an optional constructor parameter, made the container build the lyrics service for every `SongService`; 32 existing tests failed because their hosts do not register the network-status reader. Lazy resolution is the house convention for this service's collaborators and touches no existing test. The lyrics service and its provider hold nothing the scope disposes | Low |
| C25 | Add mode: change the Title, edit the artist, type the picked title back | Not closed; the form holds the pick again with the edited artist | Closing it changes the identity rule of REQ-PIC-27/28. **Known gap for Helder** | Low |
| C26 | Pick outcome after a changed Title text | Kept; dropped only by X and by a newer pick | Lyrics that still equal the lyrics source's text are a source copy | Low |
| C27 | Album part of a Deezer option row | Stays (REQ-ART-07) | Only the version label was decided | Low |
| C28 | The form sends the Deezer provider and id as a pair | Both when the pick is held, neither otherwise; decided during T10 | Before, each part was sent on its own check, so a half identity was possible in theory. A half identity is no identity everywhere else (duplicate check, unique index, `IsDeezerSong`). Deezer rows always carry both, so only a theoretical case changes | Low |

### 1.3 Added after the merge (2026-11-16)

Helder decided: wrap the edit-mode load so a failure cannot leave the form half-usable, and show a loading indicator that does not block the screen (REQ-SLD-32 to 34).

| # | Question | Decision (Claude, overrule if wanted) | Why |
|---|----------|----------------------------------------|-----|
| C29 | Which indicator | `dx:ShimmerView` around the form's field area, as the lists and the YouTube search page already use | DevExpress first; the house pattern for a first load |
| C30 | What a failed load does | Message through the existing snackbar component, then navigate back | An edit form without its stored values must not stay open: Save would overwrite stored data with blanks (the BUG-008/024 risk) |
| C31 | "Song not found" | Treated like a failed load (today: logged, form left open and empty) | Same risk, same answer |
| C32 | Save while loading | Ignored (REQ-SLD-33) | Same risk |

Design: `SongFormViewModel` gains `IsSongLoading` (true from the moment edit mode is entered until `LoadSongForEditAsync` ends, in a `try/finally`); the load body is wrapped in `try/catch` that logs, shows the message and navigates back through the navigation service the ViewModel already uses; `SaveAsync` returns at once while `IsSongLoading`. `SongFormPage.xaml` wraps the field area in `dx:ShimmerView` bound to `IsSongLoading`, with a loading view of a few rounded blocks. No shared component is edited. Tests: ViewModel unit tests for the flag (pending load, completed load), Save ignored while loading, failed load and not-found (message shown once, navigation back, no save); the shimmer itself is a device check.

### 1.4 After Helder's device check (2026-11-17)

Helder's decisions: a short search text matches inside the title; a Deezer song's title cannot be
changed; the C25 gap must be closed. REQ-SLD-35 to 40. Claude's decisions for him:

| # | Question | Decision | Why |
|---|----------|----------|-----|
| C33 | Does a short text also search the artist name? | Yes, title or artist name contains | The long-text search already finds by artist; "U2" has two characters. Helder named titles only: overrule if unwanted |
| C34 | Order of short-text results | Title starts with the text first, then title, then id | The old prefix results stay on top; the id keeps pages disjoint |
| C35 | How the short-text match is done | `LIKE '%text%'` under the column collation, in the database, with `%`, `_` and the escape character of the typed text escaped | The trigram index needs 3 characters; a scan of a single-device library is cheap. No text is normalised in C# |
| C36 | Title of a Deezer song, edit mode | Plain text; the Service refuses a change (same shape as the artist rule) | Helder. C1 is withdrawn |
| C37 | Title after a Deezer pick, add mode | Stays editable; any change of the text drops the pick for good (REQ-SLD-39) | Closes C25 in the ViewModel alone. Locking the field against typing would need a change of the shared autocomplete component (governed). The X on Title keeps working as Helder checked it |
| C38 | A Deezer song whose title was already changed | Left as it is (REQ-SLD-40) | Restoring it needs a Deezer request by id for one known row; delete and add again does it today |

Architecture notes:
- **Repository** (`SongRepository.GetPagedAsync`, the branch for a text without a 3-character word and the missing-index fallback): contains match on `Title` or the artist `Name`, order of C34, count query with the same filter. Catalog mode's query is not touched.
- **`SongService`** edit branch: for a Deezer song, the title check runs beside the artist check of REQ-SLD-15, before any other work; failure on the Title member of the save-field enum.
- **`SongFormViewModel`**: `IsTitleFrozen` (edit mode: the loaded song is a Deezer song, and true until the load ended, as `IsArtistFrozen`), `IsTitleEditable`; the edit request of a frozen title carries the loaded title; in add mode a Title text different from the picked title clears the pending identity and the picked title (the lyrics pick outcome is kept, REQ-SLD-26).
- **Page**: a plain `Label` in the Title's text style visible when `IsTitleFrozen`; `titleTapField` visible otherwise. No shared component is edited.

## 2. Approaches considered

**Songs list search**
- *Chosen:* a paged form of the existing shared title search in the repository (full-text when possible, collated prefix otherwise).
- *Rejected:* widening the current `LIKE` to title-or-artist. Simpler, but a second search behaviour beside the form's, and no ranking.
- *Rejected:* calling the suggestion service from the list. It returns 5 re-ranked suggestion rows, not pages of list rows.

**Collapse**
- *Chosen:* group in the suggestion service on (Deezer artist id, short title), ordinal comparison, before the library check.
- *Rejected:* dropping every row with a version label. No comparison at all, but album repeats stay and a song that exists only as a live track disappears.
- *Rejected:* grouping in the provider. The provider stays a plain mapper of the API; grouping is a business rule and belongs in the Service.

**Frozen artist**
- *Chosen:* page-level plain text plus a Service rule (H5).
- *Rejected:* a "not tappable" option on `TapToEditField` (governed, 2 consumer pages) — a second governed task for one consumer.

**Lyrics origin**
- *Chosen:* one origin column (C7).
- *Rejected:* a boolean "edited" plus a "lookup tried" timestamp: two columns for the same information.
- *Rejected:* comparing stored lyrics with the lyrics source at send time (post-MVP): needs the network and cannot tell a correction from a different record.

## 3. Architecture

Order of work follows the layers: Contracts and Domain, then Infra, then Services, then UI.

### 3.1 Contracts

- `MusicSearchResultDto`: new `string? ExternalArtistId` (the Deezer artist id as text). Used for grouping only; it is not stored.
- `SongSuggestionDto`: unchanged. A Deezer row leaves the service with `Version = ""`.
- `SearchConstants`: new `RemoteSongFetchCount` (C12). `MaxSearchResults` (5) is unchanged.
- New `LyricsLookupResult` (status `Found` with the text, `NoMatch`, `NoAnswer`) for REQ-SLD-22/23.
- Save request of the form (`SaveFromFormAsync` request type): new optional member carrying the outcome of the pick lookup — the lyrics it returned, or "no match" — for REQ-SLD-26. The Service, not the ViewModel, derives the origin from it.
- New result for the open lookup (`EnsureLyricsAsync`, § 3.4): the stored lyrics or nothing.

### 3.2 Domain

- `Song`: new `LyricsOrigin? LyricsOrigin`. Enum `LyricsOrigin { LyricsSource, User, NoneFound }`; null means *not recorded*.
- `Song` gains no album and no duration.
- A domain rule class (beside `ImageReferenceRules`) holds the two pure decisions so they are unit-tested without mocks:
  - `IsDeezerSong(externalProvider, externalId)` — both non-blank;
  - `LyricsOriginRules.OnCreate(savedLyrics, pickLookup)` and `OnUpdate(storedLyrics, storedOrigin, savedLyrics)` — REQ-SLD-26/27.
  Comparison of lyrics text is an ordinal equality of two values the app itself holds; it is not search or de-duplication.

### 3.3 Infra

- **Migration** `AddSongLyricsOrigin`: nullable column on `Songs`, no default, no backfill (REQ-SLD-28). `Migrations/*` and `AppDbContext.cs` are sequential-only files: one writer.
- `SongConfiguration`: map the enum (stored as a short string so the database stays readable).
- `ISongRepository` / `SongRepository`: new paged search for the list (REQ-SLD-01 to 04), built on the existing `SearchTitleCoreAsync` logic:
  (Superseded in shape by C21: this lives inside the existing `GetPagedAsync`, with no new interface member.)
  - full-text branch: the existing `SongSearch MATCH` query with the existing `bm25(SongSearch, 10.0, 1.0), length(s.Title)` order, plus `s.Id` as a last tie-breaker so pages never overlap, `LIMIT/OFFSET`, and a count query with the same `MATCH`;
  - prefix branch (no word of 3+ characters, or the index table is missing): today's `GetPagedAsync` query;
  - projection to `SongListItemDto` exactly as `GetPagedAsync` does (including `s.ImageUrl ?? artist.ImageUrl`).
  All matching stays in the database (collation and the full-text index); no text is normalised in C#.
- No new repository write method: `EnsureLyricsAsync` (§ 3.4) re-reads the song through the existing `GetByIdAsync` inside a write scope of the song service's unit of work, sets lyrics and origin, and saves through the existing `UpdateAsync` (the context does not track). The row is written with the values just read in the same transaction, so only those two columns change value (REQ-SFT-46 as extended in the Supersedes table).

### 3.4 Services

- **`DeezerProvider.SearchSongsAsync`**: reads `artist.id`; keeps up to the new constant's number of rows instead of the shared `MaxResults = 5`. The request URL is unchanged (C20). `SearchArtistsAsync` keeps 5. The provider still maps one API row to one DTO.
- **`SongSuggestionService.SearchTitleRemoteAsync`** (shared by every caller, H3): after the Deezer fetch and before `DedupAgainstLibraryAsync`, group the rows (REQ-SLD-06, 07, 11), set `Version` to blank on the kept rows (REQ-SLD-08), then run the existing library check and the existing `Take(MaxSearchResults)`. Grouping is a pure private step with ordinal comparison only.
- **`SongService.GetPagedSongsForListAsync`**: unchanged (C21); the repository decides by the query.
- **`SongService` edit branch of `SaveFromFormAsync` and `UpdateSongCoreAsync`**: for a Deezer song the only accepted artist is the stored artist id. The check runs right after the song is loaded and **before any artist resolution**: a request with another artist id, with a typed name and no id, or with a new artist name is refused (REQ-SLD-15) with the existing failure tuple and the existing Artist member of the save-field enum. Ids are compared; names never are. When the id matches, artist resolution is skipped altogether (REQ-SLD-30).
- **`SongService` create and update**: set `LyricsOrigin` through `LyricsOriginRules` (REQ-SLD-26, 27). One write path as today (REQ-SFT-46).
- **`ISongLyricsService`**: new method returning `LyricsLookupResult`; the existing `FindLyricsAsync` becomes a thin wrapper over it so REQ-LYR behaviour and its tests stay as they are. `NoMatch` only for a definite answer of the source (404, instrumental, blank, over the limit); everything else is `NoAnswer`.
- **`SongService.EnsureLyricsAsync(songId, ct)`** (new, REQ-SLD-19 to 24): loads the song; returns at once unless it is a Deezer song with blank lyrics and no origin; runs the lookup with title and artist name; on `Found` stores lyrics + origin *lyrics source*; on `NoMatch` stores origin *none found*; on `NoAnswer` stores nothing. Before storing it re-reads the song and stores only when its lyrics are still blank and its origin still null, so a Save that happened meanwhile wins. Never throws for expected failures; rethrows caller cancellation only.
  - Preconditions checked in order: song exists; is a Deezer song; lyrics blank; origin null; stored title and artist name non-blank. Any failing precondition returns "nothing" with no request.
  - Cancellation is honoured while waiting for the lyrics source. Once the answer is in hand, the store step runs to its end with a non-cancellable token (REQ-SLD-20).
  - Returns the lyrics stored on the song during this call (by it, or by a concurrent call found at the re-read), or nothing. A song that already had lyrics returns nothing at the precondition; the form has them from its own load.
  - The HTTP request runs outside any unit-of-work scope; no transaction is open while waiting.
- **Lyrics origin on update** compares the saved lyrics with the row as read inside the update (REQ-SLD-27), not with what the form loaded. Because the form applies a stored match to its blank field before Save (§ 3.6), an untouched copy compares equal and keeps *lyrics source*.

### 3.5 Integration contracts

- **Deezer** `GET search/track?q=…` — request unchanged; additionally read `data[].artist.id` (present in a recorded real answer, which also shows 25 rows by default). The official reference is behind a login wall and was not read (C20).
- **LRCLIB** `GET api/get?track_name=&artist_name=` — unchanged; one request per lookup on open.

### 3.6 ViewModels

- **`SongsViewModel`**: no change expected beyond what the Service returns; `IsEmptyNoResults` works as today.
- **`SongFormViewModel`**:
  - `IsArtistFrozen` — in edit mode: the loaded song is a Deezer song. In add mode: the same condition `BuildSaveRequest` uses today to keep the identity (`keepsIdentity`), extracted into one property so the screen and the request can never disagree (REQ-SLD-14). Raised on pick, X on Title, Title text change and load.
  - Edit load no longer sets the tap-to-edit lock for a Deezer song's artist as its only protection; the frozen state decides what the page shows.
  - In edit mode with a frozen artist, `BuildSaveRequest` sends the loaded artist id and no artist text (REQ-SLD-30).
  - After the edit load of a song that meets REQ-SLD-19, call `EnsureLyricsAsync` without blocking the form; show the REQ-LYR-09 hint only while that call runs; apply the returned lyrics only if the Lyrics field is still blank and the form still shows that song (REQ-SLD-21, 25). The call is cancelled when the form is left.
  - Save (REQ-SLD-31): cancel the lookup on open, await its task, apply a returned match to a blank field, then build the request. The ViewModel keeps no "loaded lyrics" baseline; the Service compares with the stored row.
  - `BuildSaveRequest` passes the outcome of the pick lookup of the held pick (REQ-SLD-26); a released pick passes none. The ViewModel decides nothing about the origin.
  - `ApplyRemotePick` no longer writes the Version field (REQ-SLD-08).

### 3.7 Page

`SongFormPage.xaml` only (one file, then build):
- a `Label` with `StyleClass="Title.Large"` bound to the artist name, visible when `IsArtistFrozen`. It has no error state (REQ-SLD-12): the only artist failure left for a frozen artist is the refusal of REQ-SLD-15, which the form cannot cause. A stock `Label` is used because DevExpress MAUI has no plain-text display control; the page's other texts are `Label`s already;
- the existing `artistTapField` visible when not frozen.
No shared component is edited. No page is added, so `SafeAreaEdges` is untouched. The option row template of the Title search shows no version for Deezer rows because the DTO's version is blank; if the template builds "artist · version · Deezer" from fixed parts, the empty part is dropped the way it is today for local rows without a version.

## 4. Interaction flows

1. **List search.** Type "chit" → debounce (unchanged) → Service → repository full-text page 1, best first → scroll → page 2 with the same order.
2. **Form search.** Type "evid" → local rows → Deezer answer of up to 25 rows → groups → kept rows with blank version → library check → up to 5 Deezer rows, each song once.
3. **Deezer pick, add mode.** Title filled, artist shown as plain text, Version untouched, pick lookup starts (REQ-LYR). X on Title, or a changed Title text → artist field editable again, text kept.
4. **Edit a Deezer song.** Artist plain text. Title, Version, Featured artists, Lyrics, picture and YouTube links editable.
5. **First open without lyrics.** Form shows at once; hint "Looking for lyrics…"; match → stored, field filled; no match → origin *none found*, hint gone, no message.
6. **Save with changed lyrics.** Origin becomes *user*.

## 5. Invariants and postconditions

- A Deezer song's `ArtistId` never changes after creation through any Service path.
- `LyricsOrigin = LyricsSource` implies the stored lyrics are exactly what the lyrics source returned (trimmed) and the user has not changed them since.
- `LyricsOrigin = User` is set only by a Save, never by a lookup.
- `EnsureLyricsAsync` never overwrites non-blank lyrics and never changes an origin that is not null.
- At most one lookup on open runs per form instance.
- A Deezer suggestion row always has a blank version.
- List search pages are disjoint and their union is the full match set.

## 6. Governed components

None is edited. `CrudListView`, `ListItem`, `TapToEditField` and `AutocompleteDXField` are used as they are. If the plan finds that any step cannot be done without editing one of them, that step stops and becomes its own governed task with Helder's approval first.

## 7. Testing

| Area | Level | Tests |
|------|-------|-------|
| Grouping in `SongSuggestionService` (REQ-SLD-06 to 11) | A | Unit, Moq provider: album repeats, live + plain, only-live, missing artist id, same title by two artists, case-different titles stay apart, order kept, library check sees the blank version |
| `DeezerProvider` (artist id, row count) | B | Unit with the existing stub handler pattern |
| Artist freeze in `SongService` (REQ-SLD-15, 16, 17, 18, 30) | A | Unit: Deezer song + other artist id refused, + typed name refused, + new artist name refused; stored id accepted with no artist resolution call; title change of a Deezer song accepted; local-only song accepted; add-mode "use existing artist" decision stores a Deezer song with that artist; "save as new version" stores no identity |
| `LyricsOriginRules` (REQ-SLD-26, 27) | A | Unit, every rule of REQ-SLD-26 in order and every row of the REQ-SLD-27 table |
| `EnsureLyricsAsync` (REQ-SLD-19 to 24) | A | Unit: each precondition, Found, NoMatch, NoAnswer, Save-in-between wins, store completes after cancellation once the answer is in hand, second call returns the stored lyrics |
| Catalog mode (REQ-SLD-05) | C | No new test: the catalog service and its query are not edited; noted in the task-log |
| `ISongLyricsService` result mapping (REQ-SLD-22, 23, 29) | A | Unit: 404, instrumental, blank, over limit → NoMatch; offline, timeout, 5xx → NoAnswer; existing REQ-LYR tests untouched |
| Paged list search (REQ-SLD-01 to 04) | B | Repository tests on real SQLite: artist match, ranking order, disjoint pages, count, prefix branch, missing index table |
| Migration (REQ-SLD-28) | B | Existing rows keep null |
| `SongFormViewModel` (REQ-SLD-08, 12 to 14, 19, 21, 25, 26, 30, 31) | A | Unit: frozen state per mode, X releases, Title text change releases and keeps the artist text, frozen edit request carries the stored artist id, lookup on open applied only to a blank field, hint only while the lookup runs, the three outcomes of REQ-SLD-31, request carries the pick outcome and none after a released pick, Version untouched by a pick |
| Page and option row (REQ-SLD-08, 12, 13) | — | On-device check by Helder: Deezer song shows plain artist text; local-only song opens the editor; a Deezer option row shows no version label |

Every test's first comment line carries its AC id (`// [AC] REQ-SLD-NN: …`). The traceability matrix goes in `task-log.md` at review.

**On-device check (Helder):** search the Songs list by an artist name; search a well-known title in the form and see each song once; pick one and see the artist as plain text; save, reopen, confirm the artist cannot be edited and the title can; open an older Deezer song without lyrics online, see lyrics arrive, switch to airplane mode, reopen, lyrics still there.

## 8. Open points for the plan

- Task 0 is closed by C20.
- Exact names of the new members and the shape of the save-request addition follow the code style reference at plan time.
