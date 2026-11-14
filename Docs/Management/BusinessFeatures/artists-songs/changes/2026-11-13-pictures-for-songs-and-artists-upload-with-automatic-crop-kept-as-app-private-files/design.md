# Design: pictures and tap-to-edit names in the Song and Artist forms

> **Status: APPROVED 2026-11-13** under Helder's instruction of the same day (see `requirements.md`).

## 1. Decisions taken for Helder (2026-11-13)

Each row can be overruled; the cost of reversing is noted.

| # | Question | Decision | Why | Cost to reverse |
|---|----------|----------|-----|-----------------|
| P1 | Where do Deezer pictures live? | **Remote, by URL, in the platform image cache** (unchanged from the artwork item). | Cheapest: no storage owned by the app, no download code, the cache is bounded and evicted by the OS. A row seen once shows offline while cached. | Low: a later item can copy them to files under the same reference scheme. |
| P2 | Where do uploaded pictures live? | **Files in `<app data>/images/{songs,artists}/`**, never in the database. | The app data directory is private to the app on Android and iOS (no file explorer access). The database and its backups do not grow. | Medium (move files). |
| P3 | What does the database hold? | **One short reference in the existing `ImageUrl` column**: an `https` URL or `local:<folder>/<name>`. No migration. | Every projection and fallback rule of the artwork item keeps working unchanged; a user upload naturally replaces the Deezer URL. | Low. |
| P4 | Which sizes? | **List rows and options: 250 px (Deezer `250x250`) / 256 px (upload). Form picture: 500 px (Deezer `500x500`) / 512 px (upload).** | A 56 dp row slot needs at most 224 px; a 120 dp form slot needs at most 480 px on the densest phones. | Low: constants. |
| P5 | How is the Deezer 500 px picture obtained? | **By rewriting the size segment of the stored 250 px URL** (`/250x250-` → `/500x500-`) at display time. Nothing else is stored. | Deezer image URLs carry the size in the path; no extra API call, no second column. If the large URL fails, the thumbnail shows under it. | Low. |
| P6 | Compression | **JPEG, quality 75 (thumbnail) and 80 (large)**; the picker first limits the photo to 1600 px and applies the EXIF rotation. | About 10-15 kB and 35-60 kB per picture; a thousand uploads stay under 75 MB. | Low: constants. |
| P7 | Crop | **Automatic centred square** (`ResizeMode.Bleed` of `Microsoft.Maui.Graphics`). | Helder asked for an automatic crop from the centre. | — |
| P8 | Which DevExpress control? | **`dx:DXImage` for icons and the existing layered-image pattern of `ListItemLeadingImage` for bitmaps; `dx:DXButton` for actions. `dxe:ImageEdit` is not used.** | DevExpress MAUI 25.2.4 has no "pick a file" control. `ImageEdit` is a manual crop editor (pan, zoom, crop frame); Helder asked for an automatic crop, so it would add a step. Picking uses .NET MAUI `MediaPicker` (no DevExpress equivalent). | Low: a later item can open `ImageEdit` after the pick. |
| P9 | Image library | **None added.** `MediaPicker` (.NET 10 options: `MaximumWidth/Height`, `CompressionQuality`, `RotateImage`) + `Microsoft.Maui.Graphics` `PlatformImage`. | SkiaSharp is only a transitive Debug dependency; adding it grows the app. | Low. |
| P10 | What cleans up files? | **One sweep at app start** deletes unreferenced files older than 24 hours. No delete call in any Save, Delete or form-close path. | One rule covers replace, remove, record delete and abandoned forms; nothing else needs to know about files. | Low. |
| P11 | Recommended size shown to the user | **"Square picture (1:1), 512 x 512 px or larger. Other shapes are cropped to the centre."** | Ratio and resolution in one line; high resolution is not needed. | — |
| P12 | Form picture size and place | **120 dp, below Title / Name**, song square with 12 dp corners, artist circle. | Helder: "right in the top below title". | Low. |
| P13 | Type styles | **Title and Name: Headline Medium (28/36). Artist: Title Large (22/28).** `Headline.Medium` is added to `MaterialStyles.xaml`. | MD3 type scale; larger than the 16 sp editors, Artist between the two. | Low. |
| P14 | How is the tap-to-edit behaviour shared? | **A new wrapper component `TapToEditField`** that holds any editor as its content. | No change to the governed `AutocompleteDXField`; works around `dxe:TextEdit` too. | Low. |
| P15 | What does a tap do to a picked value? | **Releases the picked state and keeps the text.** The Deezer identity survives only if the text is unchanged at Save. | Helder: the cursor must be in the entry "always" when the text is tapped. A read-only picked field cannot hold a cursor. | Medium (VM rules). |
| P16 | Title search in edit mode | **Stays off** (REQ-SFT-27). | A local pick opens another song, which makes no sense while editing one. | Low. |
| P17 | Artists list checkbox | **Moves to the trailing slot**, as on the Songs list. | The leading slot now holds the avatar; the overflow-menu items will rework both lists the same way later. | Low. |
| P18 | Does the form show the artist picture for a song without a cover? | **No**: the form picture is the song's own picture or the placeholder. | The field edits the song's picture; showing the artist's would hide that the song has none (and hide the size hint). List rows keep the artist fallback. | Low. |
| P19 | Uploaded artist picture in the Song form | **Not offered**; the artist picture is edited in the Artist form. | One place per record. | — |

## 2. Architecture

Layering stays Domain → Infra → Services → UI. Business rules live in Domain rules and Services.

```
MediaPicker ─► IPhotoPicker (UI) ─► ViewModel ─► IImageStore.ImportAsync ─► IImageProcessor (platform)
                                        │               └─ writes <root>/<folder>/<name>_t.jpg, _l.jpg
                                        │ reference "local:songs/<name>"
                                        ▼
                         SongFormSaveRequest(ImageUrl, ImageChanged) ─► SongService ─► Song.ImageUrl
                         IArtistService.SetArtistImageAsync            ─► Artist.ImageUrl

display: reference ─► ImageReferenceRules.ToDisplaySource(reference, size, root) ─► URL or file path
App start: IImageMaintenanceService.SweepOrphansAsync ─► repositories (live local references) ─► IImageStore.DeleteUnreferenced
```

### 2.1 Domain

- `Domain/Rules/ImageReferenceRules.cs` (new, static, pure):
  - `LocalPrefix = "local:"`, `SongsFolder = "songs"`, `ArtistsFolder = "artists"`,
    `ThumbnailSuffix = "_t.jpg"`, `LargeSuffix = "_l.jpg"`, `ThumbnailPixels = 256`, `LargePixels = 512`.
  - `bool IsLocal(string? reference)` — well-formed local reference only.
  - `string? Normalize(string? value)` — a well-formed local reference, else `ImageUrlRules.Normalize(value)`.
  - `string CreateLocal(ImageOwnerKind kind, Guid id)` — `local:<folder>/<id:N>`.
  - `string? ToDisplaySource(string? reference, ImageSize size, string imagesRoot)` — REQ-PIC-03.
  - `bool TryGetLocalFiles(string? reference, string imagesRoot, out string thumbnailPath, out string largePath)`.
- `Domain/Rules/ImageSize.cs` (`Thumbnail`, `Large`), `Domain/Rules/ImageOwnerKind.cs` (`Song`, `Artist`).
- `ImageUrlRules` is unchanged (it stays the Deezer provider boundary).
- `Domain/ServicesInterfaces/IImageStore.cs`:
  - `Task<(bool success, string message, string? reference)> ImportAsync(Stream source, ImageOwnerKind kind, CancellationToken ct = default);`
  - `string? ResolveDisplaySource(string? reference, ImageSize size);`
  - `Task<int> DeleteUnreferencedAsync(IReadOnlyCollection<string> liveReferences, TimeSpan minimumAge, CancellationToken ct = default);`
- `Domain/ServicesInterfaces/IImageProcessor.cs`: `byte[] CropSquareJpeg(byte[] source, int maxSidePixels, float quality);`
  (centre crop to a square of `min(maxSidePixels, shorter side)`; throws on undecodable input).
- `Domain/ServicesInterfaces/IImageMaintenanceService.cs`: `Task<int> SweepOrphansAsync(CancellationToken ct = default);`
- `SongFormSaveRequest` gains one optional trailing parameter `bool ImageChanged = false`.
- Repository interfaces: `ISongRepository.GetLocalImageReferencesAsync`, `IArtistRepository.GetLocalImageReferencesAsync`
  (`Task<List<string>>`; `ImageUrl` starting with `local:`).
- `IArtistService` gains two methods (existing signatures untouched, so existing mocks and tests compile):
  - `Task<Artist?> GetArtistByIdAsync(int id, CancellationToken ct = default);`
  - `Task<(bool success, string message)> SetArtistImageAsync(int id, string? imageReference, CancellationToken ct = default);`

### 2.2 Services

- `Services/ImageStore.cs` (`IImageStore`): constructor `(ImageStoreOptions options, IImageProcessor processor, ILogger<ImageStore>)`;
  `ImageStoreOptions(string RootDirectory)` record in Domain or Services. Plain `System.IO`.
  - `ImportAsync`: copy the stream to memory (stop above 25 MB → failure); call the processor twice;
    write both files to temporary names in the target folder, then rename; on any failure delete what
    was written and return `(false, "Could not use this picture.", null)`. A decode failure is an
    expected business failure: caught, logged at Warning, returned as the tuple (allowed pattern:
    translate an expected failure at a boundary; see `code-style-reference.md § Exception Handling`).
  - `ResolveDisplaySource` → `ImageReferenceRules.ToDisplaySource(reference, size, options.RootDirectory)`.
  - `DeleteUnreferencedAsync`: enumerate `songs/` and `artists/`; a file is live when its name without
    suffix matches a live reference; delete files that are not live and whose last write time is older
    than `minimumAge`; return the count. Missing folders → 0.
- `Services/ImageMaintenanceService.cs`: reads both repositories inside a read unit of work, calls
  `DeleteUnreferencedAsync(refs, TimeSpan.FromHours(24))`.
- `SongService.SaveFromFormAsync`:
  - the song image is normalised with `ImageReferenceRules.Normalize` (artist image stays `ImageUrlRules.Normalize`);
  - edit mode: when `request.ImageChanged`, `UpdateSongCoreAsync` receives the normalised reference and
    assigns `song.ImageUrl`; otherwise it does not touch it (existing edit-mode image tests stay green
    because `ImageChanged` defaults to false);
  - edit mode with a changed artist (REQ-PIC-32): a collision on (artist, title, version) returns
    `Failed` with a message, never an exception. The implementor first reads what `UpdateSongCoreAsync`
    does today; if a guard exists, a test pins it, else the guard is added.
  - edit mode with `ArtistImageUrl` (REQ-ART-16 in edit mode): an artist **created** by the update gets
    the image; filling an existing empty artist follows the same `FillArtistImage` rule as add mode.
- `ArtistService`: `GetArtistByIdAsync` (read, no tracking); `SetArtistImageAsync` (normalises with
  `ImageReferenceRules.Normalize`, tracked write as in T10a of the artwork item, updates `UpdatedAt`).
- `ArtistSuggestionService`: local rows carry `ImageUrl` from `ArtistListItem.ImageUrl` (REQ-PIC-34).

### 2.3 Infra

- `SongRepository` / `ArtistRepository`: `GetLocalImageReferencesAsync` —
  `Where(x => x.ImageUrl != null && x.ImageUrl.StartsWith("local:")).Select(x => x.ImageUrl)`.
  This is a prefix filter on a technical value, not search or de-duplication.
- No migration, no configuration change (a local reference is 45 characters; the column holds 500).

### 2.4 App (MAUI project)

- `UI/Services/IPhotoPicker.cs` + `MauiPhotoPicker.cs`: `Task<Stream?> PickPhotoAsync(CancellationToken ct = default)`;
  `MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions { SelectionLimit = 1, MaximumWidth = 1600, MaximumHeight = 1600, CompressionQuality = 90, RotateImage = true })`
  (`PickPhotoAsync` is obsolete in .NET 10); first result → `OpenReadAsync()`; empty list → null.
- `UI/Services/PlatformImageProcessor.cs` (`IImageProcessor`): `PlatformImage.FromStream` →
  `Resize(side, side, ResizeMode.Bleed, disposeOriginal: true)` → `Save(stream, ImageFormat.Jpeg, quality)`.
- `UI/Services/AppImagePaths.cs`: `ImagesRoot => Path.Combine(FileSystem.AppDataDirectory, "images")`.
- `UI/Converters/ImageReferenceConverter.cs`: `string reference` → display source string for
  `ConverterParameter` `Thumbnail` (default) or `Large`, through `ImageReferenceRules.ToDisplaySource`
  with `AppImagePaths.ImagesRoot`. Registered in the app resources beside the other converters.
- `MauiProgram.cs`: registers `ImageStoreOptions`, `IImageStore` (singleton), `IImageProcessor`,
  `IPhotoPicker`, `IImageMaintenanceService`. Platform statics stay out of `AddAppServices`.
- `App.xaml.cs`: after the migration, the sweep runs in the background; a failure is logged and never
  blocks start-up.
- Android manifest: `READ_EXTERNAL_STORAGE` with `android:maxSdkVersion="32"` (photo picker fallback on
  old Android; Android 13+ uses the system photo picker without permission). iOS `Info.plist`:
  `NSPhotoLibraryUsageDescription`. The implementor confirms both against the .NET MAUI 10 MediaPicker
  page on Microsoft Learn before editing.

### 2.5 Components (new; records in their own items)

**`UI/Components/TextFields/TapToEditField.xaml(.cs)` + `TapToEditFieldLogic.cs`** — record:
`Docs/Management/DevCycleCraft/autocomplete-component/changes/2026-11-13-new-component-taptoeditfield-a-defined-value-shows-as-text-a-tap-opens-its-editor/`.

| BindableProperty | Type | Default | Meaning |
|------------------|------|---------|---------|
| `Editor` (content property) | `View` | null | the wrapped editor |
| `Text` | `string` | "" | the value shown in the text state |
| `TextStyleClass` | `string` | `Body.Large` | type style class of the text |
| `MaxLines` | `int` | 3 | lines before the ellipsis |
| `IsEditIconVisible` | `bool` | true | trailing `edit_outlined` icon |
| `HasError` | `bool` | false | while true the editor stays visible |
| `IsEditing` | `bool`, TwoWay | false | editor state requested by a tap; reset on blur or pick |
| `ReleasesPickOnEdit` | `bool` | true | a tap sets the wrapped `AutocompleteDXField.IsLocked` to false |

- `TapToEditFieldLogic.ShowsText(text, isEditing, hasError)` = `!isEditing && !hasError && !string.IsNullOrWhiteSpace(text)` (pure, unit-tested).
- Tap on the text: `ReleasesPickOnEdit` and the editor is a locked `AutocompleteDXField` → `IsLocked = false`
  (TwoWay to the ViewModel); `IsEditing = true`; then focus: `AutocompleteDXField.FocusEditor()`, else the
  first `InputView` / DevExpress `EditBase` descendant `.Focus()`.
- Leaving the editor state: the wrapper subscribes (on `Loaded`, unsubscribes on `Unloaded`) to
  `Unfocused` of the first DevExpress `EditBase` / `InputView` found in the editor's visual tree. On
  `Unfocused` it waits 250 ms on the dispatcher and, if that editor is not focused again, sets
  `IsEditing = false`. The delay lets a pick, the X icon and blur validation finish first.
- A pick: when the wrapped `AutocompleteDXField.IsLocked` becomes true, `IsEditing = false`.
- The component adds no public member to `AutocompleteDXField` and does not change it.
- Accessibility: the text container has `SemanticProperties.Description` = the text and
  `SemanticProperties.Hint` = "Double tap to edit"; minimum height 48 dp.

**`UI/Components/Media/ImagePickerField.xaml(.cs)`** — record:
`Docs/Management/DevCycleCraft/crud-form-action-pattern/changes/2026-11-13-new-component-imagepickerfield-form-picture-with-add-replace-and-remove/`.

| BindableProperty | Type | Default | Meaning |
|------------------|------|---------|---------|
| `ImageSource` | `ImageSource` | null | large picture |
| `ThumbnailSource` | `ImageSource` | null | drawn under the large picture (shows first / when the large one fails) |
| `PlaceholderIcon` | `string` | `music_note_outlined` | icon under both |
| `IsCircle` | `bool` | false | circle (artist) or rounded square (song) |
| `HasImage` | `bool` | false | switches buttons and hint |
| `HintText` | `string` | "" | shown only while `HasImage` is false |
| `PickCommand`, `RemoveCommand` | `ICommand` | null | actions |
| `IsBusy` | `bool` | false | disables the actions while a picture is imported |

Layout: centred column: the 120 dp picture (tap = `PickCommand`), then the buttons ("Add picture", or
"Replace picture" + "Remove picture"; `dx:DXButton`, existing button styles), then the hint
(`Body.Small`, `OnSurfaceVariant`, centred). The picture layering copies `ListItemLeadingImage`
(placeholder `dx:DXImage` under stock `Image`, `AspectFill`), which is the proven pattern here.

**Style:** `Headline.Medium` (28 sp, line height 36, Regular) added to `Resources/Styles/MaterialStyles.xaml`.

### 2.6 ViewModels

**`SongFormViewModel`** (constructor gains `IImageStore imageStore, IPhotoPicker photoPicker`):
- State: `_uploadedImageReference`, `_storedImageReference` (edit mode, loaded in `LoadSongForEditAsync`),
  `_imageRemoved`, existing `_pendingImageUrl`.
- `FormImageReference` = `_imageRemoved ? null : _uploadedImageReference ?? (IsEditMode ? _storedImageReference : _pendingImageUrl)`.
- Observable for the page: `FormImageSource`, `FormImageThumbnailSource` (strings from
  `IImageStore.ResolveDisplaySource`), `HasFormImage`, `IsImageBusy`, constant `ImageHintText`.
- `PickImageCommand`: picker → null: return; `ImportAsync(stream, Song)` → success: set the upload,
  `_imageRemoved = false`; failure: snackbar with the message. `RemoveImageCommand`: REQ-PIC-20.
- `ApplyRemotePick`: sets `_imageRemoved = false` (a later pick sets a picture again) and never clears the upload.
- `BuildSaveRequest`: add mode `ImageUrl = FormImageReference`; edit mode `ImageUrl = FormImageReference`,
  `ImageChanged = _uploadedImageReference != null || _imageRemoved`.
- Tap-to-edit: `IsTitleEditing`, `IsArtistEditing` (bound TwoWay to the wrappers).
- Releasing a pick (the wrapper sets `IsTitleLocked` / `IsArtistLocked` to false with the text kept):
  - Title: the VM remembers the picked title text at `ApplyRemotePick`; `BuildSaveRequest` sends the
    external identity only when `SongTitle` still equals it (ordinal). Nothing else is cleared.
  - Artist: when `IsArtistLocked` turns false outside hydration and outside `ClearArtist`, the VM sets
    `SelectedArtistId = 0` so Save resolves by text. The VM remembers the picked Deezer artist name;
    `ArtistImageUrl` is sent only when `ArtistSearchText` equals it (ordinal). These comparisons guard
    which pending value is sent; they are not search or de-duplication.
- Artist options and Title options: `ImageUrl` of each `AutocompleteSuggestion` is passed through
  `ResolveDisplaySource(…, Thumbnail)` (REQ-PIC-39).
- Edit mode: `LoadSongForEditAsync` keeps setting `IsArtistLocked = true` (the stored artist is a pick);
  the tap releases it. Existing lock tests stay valid.

**`ArtistFormViewModel`** (constructor gains `IImageStore`, `IPhotoPicker`): same picture state and
commands with `ImageOwnerKind.Artist`; edit mode loads the artist with `GetArtistByIdAsync` for the
stored picture; `SelectName` keeps `dto.ImageUrl` as the pending picture and remembers the picked name;
`ClearName` drops the pending picture; Name options carry `LeadingKind = Avatar`, the resolved
thumbnail and the placeholder. Save: existing create / update call, then, when a picture must be
stored (add mode: form picture not null; edit mode: uploaded or removed), `SetArtistImageAsync`. A
failure of that second call shows its message and keeps the form open in edit mode. `IsNameEditing`.

### 2.7 Pages

- `SongFormPage.xaml`: Title wrapped in `TapToEditField` (`Headline.Medium`); `ImagePickerField` below
  the character counter; Artist wrapped in `TapToEditField` (`Title.Large`). Nothing else moves.
  Code-behind: `OnAppearing` focuses the Title editor only when Title is in the editor state.
- `ArtistFormPage.xaml`: `nameEdit` (`dxe:TextEdit`) is replaced by `AutocompleteDXField` bound to the
  existing ViewModel members (`NameOptionSource`, `IsNameLocked`, `IsNameSearchEnabled`,
  `NameSupportingText`, `SelectNameCommand`, `ClearNameCommand`, blur → `ValidateNameCommand`), wrapped
  in `TapToEditField` (`Headline.Medium`); `ImagePickerField` (`IsCircle`) below the counter. The
  duplicate-suggestion buttons keep their behaviour; their template `x:DataType` is corrected to the
  ViewModel's item type.
- `ArtistsPage.xaml`: both templates: leading `ListItemLeadingAvatar` bound to `ImageUrl` through
  `ImageReferenceConverter`; trailing = catalog button + `ListItemCheckbox` in a horizontal stack, as
  `SongsPage.xaml`. The catalog button's binding is not touched (BUG-086 owns it).
- `SongsPage.xaml`: the `ImageSource` binding goes through `ImageReferenceConverter`.

## 3. Governed components

- `AutocompleteDXField`, `ListItem`, `ListItemCheckbox`, `ListItemLeadingImage`: **not changed**.
- `ListItemLeadingAvatar` gains its second consumer (`ArtistsPage`) → added to the governed list in
  that commit. `TapToEditField` and `ImagePickerField` have two or more consumers once both forms use
  them → added to the governed list in the commit of the second consumer.

## 4. Error handling

Expected failures return tuples / outcomes. Only `ImageStore.ImportAsync` (decode and file errors) and
the start-up sweep catch exceptions, log them and continue. A missing local file shows the placeholder.

## 5. Testing (risk tiers per `testing.md`)

| Area | Level | Tests |
|------|-------|-------|
| `ImageReferenceRules` | A | all branches of `IsLocal`, `Normalize`, `ToDisplaySource`, path traversal rejected |
| `ImageStore` | A | temp directory + fake processor: two files written, reference shape, 25 MB limit, processor failure leaves no file, sweep keeps live and young files, deletes old orphans |
| `ImageMaintenanceService` | B | passes the union of both repositories' references |
| `SongService.SaveFromFormAsync` | A | REQ-PIC-02, 21 (add, edit changed, edit removed, edit untouched), 22, 32 |
| `ArtistService` new methods | A | REQ-PIC-36 normalisation, not found, persisted (real SQLite for the tracked write) |
| Repositories | B | real SQLite: local references listed, URLs excluded |
| `ArtistSuggestionService` | B | local rows carry the picture |
| `TapToEditFieldLogic` | A | truth table |
| `SongFormViewModel` | A | REQ-PIC-18, 19, 20, 21, 22, 27, 28, 31, 39 |
| `ArtistFormViewModel` | A | REQ-PIC-34, 35, 36, 37 |
| Components, pages, picker, processor | C | no unit test (XAML and platform code); device checklist T10 |

Every test carries `// [AC] REQ-PIC-NN: …`. Existing tests are not edited. **Planned exceptions**
(each superseded by a requirement of this spec, listed in the task-log when touched):
`Save_EditModeAfterDeezerPick_SendsNullImages` (REQ-ART-17 → REQ-PIC-21: edit mode has no Deezer pick,
the request now carries the stored reference with `ImageChanged = false`) and any constructor call of
the two ViewModels in tests (two new constructor parameters). If any other existing test fails, the
task stops and reports.
