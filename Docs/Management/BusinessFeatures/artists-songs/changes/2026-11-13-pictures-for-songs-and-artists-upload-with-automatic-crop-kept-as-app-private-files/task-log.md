# Task log: pictures and tap-to-edit names in the Song and Artist forms

### Checkpoint
- **Now:** T9 done; `feature/form-pictures` merged to `develop`; release build to Google Drive.
- **Next:** T10 device check (Helder).
- **Context manifest:** `requirements.md`, `design.md`, `tasks.md` (this folder).

## T0 — Spec (2026-11-13) — Done

Research results that shaped the design:
- DevExpress MAUI 25.2.4 (`.csproj` version; the online docs describe 26.1, same API for what is used):
  `dxe:ImageEdit` is a manual crop editor (`CropAreaShape`, `CropAreaRatioX/Y`, `SaveAsStream`), not a
  file picker. `dx:DXImage` displays. No DevExpress control opens the photo library.
- .NET MAUI 10 `MediaPicker`: `PickPhotosAsync` replaces the obsolete `PickPhotoAsync`;
  `MediaPickerOptions` has `SelectionLimit`, `MaximumWidth`, `MaximumHeight`, `CompressionQuality`,
  `RotateImage` (Microsoft Learn, "Media picker for photos and videos", net-maui-10.0).
- `Microsoft.Maui.Graphics`: `IImage.Resize(w, h, ResizeMode.Bleed)` "fills the target dimensions,
  potentially cropping the image"; `Save(stream, ImageFormat.Jpeg, quality)`.
- Code map (2026-11-13): no image infrastructure exists; `ImageUrlRules` accepts `https` only; the
  Song service already supports an artist change in edit mode; `ArtistFormViewModel` already has the
  Name search members, unbound; `Headline.Medium` is not defined.

### Changed files
- `requirements.md`, `design.md`, `tasks.md`, `task-log.md` (this folder); six `README.md` (new backlog items).

## T1 — Image infrastructure (2026-11-13) — To Review

Commits `9e02d3be`, `59d62d7c` on `feature/form-pictures`.

### Changed files
- Domain: `Rules/ImageReferenceRules.cs`, `Rules/ImageSize.cs`, `Rules/ImageOwnerKind.cs`, `ServicesInterfaces/IImageStore.cs` (holds `ImageStoreOptions`), `IImageProcessor.cs`, `IImageMaintenanceService.cs`, `RepositoryInterface/ISongRepository.cs`, `IArtistRepository.cs`
- Infra: `Repository/SongRepository.cs`, `ArtistRepository.cs`
- Services: `ImageStore.cs`, `ImageMaintenanceService.cs`
- App: `UI/Services/IPhotoPicker.cs`, `MauiPhotoPicker.cs`, `PlatformImageProcessor.cs`, `AppImagePaths.cs`, `UI/Converters/ImageReferenceConverter.cs`, `App.xaml`, `App.xaml.cs`, `MauiProgram.cs`, `Platforms/Android/AndroidManifest.xml`, `Platforms/iOS/Info.plist`
- Tests (new): `ImageReferenceRulesTests`, `ImageStoreTests`, `ImageMaintenanceServiceTests` (2), `LocalImageReferencesRepositoryTests` (4, real SQLite); 70 cases in total

### Implementation decisions
- All image services are registered in `MauiProgram.cs` (not `AddAppServices`): the store needs the platform-bound options and processor.
- `IImageProcessor` quality is a 0-1 float (0.75 thumbnail, 0.80 large), as `Microsoft.Maui.Graphics` `Save` takes.
- The sweep also deletes stale `*.tmp` leftovers of an interrupted import.
- `PlatformImageProcessor` compiles its body for Android, iOS and Windows only; the plain `net10.0` test head throws `PlatformNotSupportedException`.
- Permissions: Android `READ_EXTERNAL_STORAGE` up to API 32 only (`READ_MEDIA_IMAGES` was added by the implementor and removed by the coordinator in `59d62d7c`: the Android 13+ photo picker needs no permission and Google Play restricts that permission). iOS `NSPhotoLibraryUsageDescription`. Source: Microsoft Learn, "Media picker for photos and videos" (net-maui-10.0).

### Existing test file touched (exception, no assertion changed)
- `Integration/UnitOfWork/SongServiceNewArtistUnitOfWorkTests.cs`: the hand-written `ThrowOnAddSongRepository` decorator implements `ISongRepository`, so it gained a forwarder for the new `GetLocalImageReferencesAsync`.

### Verification evidence
- Full suite: 1147 passed, 0 failed (70 new).
- Not verified off device: photo picker, crop, EXIF rotation, output sizes, permission prompt, start-up sweep (T10).
- Android build (`dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`, coordinator, at `59d62d7c`): 0 errors, 34 warnings, 3 min 44 s.

## T2a / T2b — TapToEditField and ImagePickerField (2026-11-13) — To Review

Commits `5da6447c` (T2a), `92c49ae8` (T2b) on `feature/form-pictures-ui`.

### Changed files
- `MyVocaList/UI/Components/TextFields/TapToEditField.xaml`, `.xaml.cs`, `TapToEditFieldLogic.cs` (new)
- `MyVocaList/UI/Components/Media/ImagePickerField.xaml`, `.xaml.cs` (new)
- `MyVocaList/Resources/Styles/MaterialStyles.xaml` (`Headline.Medium`: 28 sp, line height 1.29 = 36/28)
- `MyVocaList.Tests/Unit/Components/TapToEditFieldLogicTests.cs` (20 cases)
- No existing component was changed.

### Implementation decisions (beyond `design.md` § 2.5)
- **Focus also starts editing:** when the inner editor gains focus, `IsEditing` becomes true. Without it, a blank field would switch to text at the first typed character.
- **A pick ends editing one dispatcher turn later**, so `AutocompleteDXField` finishes its own pick handling before its host is hidden.
- **A focused editor is unfocused before it is hidden**, so the keyboard is not left open.
- `TapToEditField` exposes a read-only `IsEditorVisible` for pages. `TapToEditFieldLogic` also holds `ShouldReleasePick`, `ShouldEndEditingAfterBlur` and `BlurSettleMilliseconds = 250`.
- `ImagePickerField` buttons are 48 dp high (touch target) where the form's other buttons use the style's 40 dp. "Remove picture" uses the existing `TextButton` style.
- No unit test for `ImagePickerField` (pure XAML, level C).

### Verification evidence
- Android build: 0 errors. Suite on that branch: 1097 passed, 0 failed (20 new).
- Device-only behaviours (focus and keyboard timing, pick versus blur order, hiding a focused editor) are listed in § Device check.

## T3 — Services (2026-11-13) — To Review

Commit `0d4f44d5` on `feature/form-pictures`.

### Changed files
- Domain: `Resolution/SongFormSaveRequest.cs` (`bool ImageChanged = false`), `ServicesInterfaces/IArtistService.cs` (`GetArtistByIdAsync`, `SetArtistImageAsync`)
- Contracts: `DTOs/Suggestions/ArtistSuggestionDto.cs`
- Services: `SongService.cs`, `ArtistService.cs`, `ArtistSuggestionService.cs`
- Tests (new): `SongServiceSaveFromFormPictureTests`, `SongServiceSaveFromFormPictureRealStackTests`, `ArtistServicePictureTests`

### Implementation decisions
- The song picture goes through `ImageReferenceRules.Normalize`. In edit mode it is written only when `ImageChanged` is true; the default `false` keeps every existing caller and test unchanged.
- An artist change in edit mode that collides with an existing song returns `Failed` on `SaveField.Artist` with "A song with this title and version already exists for this artist".
- In edit mode the artist picture is written only for an artist created or moved to by that save, not when the artist is unchanged.
- `SetArtistImageAsync` returns `(bool success, string message)`.

### Existing test file touched (exception)
- `Unit/Services/ArtistSuggestionServiceImageTests.cs`: one test asserted that local rows carry no picture (REQ-ART-24). REQ-PIC-34 replaces that behaviour, so the assertion and the test name changed. Helder to confirm.

### Doubts for review
- The rule "artist picture only for an artist created or moved to in edit mode" has no requirement of its own; its tests are tagged REQ-PIC-40.
- The collision message is the same text used for the same-artist duplicate.

### Verification evidence
- Full suite on `feature/form-pictures`: 1192 passed, 0 failed.

## T8 — List rows (2026-11-14) — To Review

Commit `45911696` on `feature/form-pictures-ui`.

### Changed files
- `MyVocaList/UI/Pages/Artists/ArtistsPage.xaml`: leading `ListItemLeadingAvatar` bound to `ImageUrl` through `ImageReferenceConverter`; the checkbox moved to the trailing slot, after the catalog button (button unchanged, BUG-086 untouched).
- `MyVocaList/UI/Pages/Songs/SongsPage.xaml`: both templates bind the leading image through `ImageReferenceConverter`.
- `.claude/library/component-safety-gate.md`: `ListItemLeadingAvatar` added to the governed list (second consumer).

### Implementation decisions
- No margin on the avatar; the 40 dp circle is centred by `ListItem`.
- No governed component was changed. `ArtistsPage.xaml.cs` has no reference to the checkbox or the leading slot.
- Process deviation: the two XAML files were built once together, not one build per file.

### Verification evidence
- Android build: 0 errors. Suite on that branch (before the T3 merge): 1167 passed, 0 failed.
- No unit test (pure XAML, level C). Row height, placeholder, selection and the catalog button are in § Device check.

## T6 / T7 — Artist form (2026-11-14) — To Review

Commits `1debc4ed` (T6), `f3b4b9ab` (T7) on `feature/form-pictures-ui`.

### Changed files
- `MyVocaList/UI/ViewModels/ArtistFormViewModel.cs`
- `MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml`, `.xaml.cs`
- `MyVocaList.Tests/Unit/ViewModels/ArtistFormViewModelImageTests.cs` (new, 31 cases)
- `MyVocaList.Tests/Unit/ViewModels/ArtistFormViewModelTests.cs` (constructor call in `CreateSut()` only; no assertion changed)
- `.claude/library/component-safety-gate.md` (`TapToEditField`, `ImagePickerField` added to the governed list)

### Implementation decisions (beyond `design.md`)
- Constructor gains `IImageStore`, `IPhotoPicker`. New members: `IsNameEditing`, `IsImageBusy`, `FormImageSource`, `FormImageThumbnailSource`, `HasFormImage`, `ImageHintText`, `PickImageCommand`, `RemoveImageCommand`.
- **The Deezer picture follows the Name text:** it is the form picture only while the trimmed Name equals the picked name. Editing the Name removes it from the form, so the form always shows what Save would store.
- **Picture store fails after a successful create:** the message shows and the form becomes the edit form of the new artist with the picture still pending, so a second Save cannot create a duplicate.
- **Picture store fails after a successful update:** the message shows and the form stays open; the rename is already saved (two service calls).
- Edit mode loads the artist only for its picture; the Name still comes from the route parameter.
- Add mode focuses the Name editor on open while it is visible; blur validation runs through `BlurWithoutPickCommand`.
- The duplicate-suggestion template uses `readModels:ArtistListItem`.

### Doubts for review
- **Typing is no longer capped at 60 characters.** The old `TextEdit` had `MaxCharacterCount="60"`; `AutocompleteDXField` has no such property. The validator and the counter still reject a longer name. A cap needs a governed-component change (own task).
- **REQ-PIC-37 against REQ-SFT-49:** REQ-PIC-37 mentions keeping the external identity; REQ-SFT-49 and its existing test say the Artist form stores none. The implementation follows REQ-SFT-49: only the picture is kept. REQ-PIC-37 wording to be corrected.

### Verification evidence
- Android build: 0 errors, 34 warnings. Suite on that branch: 1243 passed, 0 failed.
- Tests per requirement: REQ-PIC-34 (3), 16 (1), 05 (1), 10 (1), 35 (11), 36 (10), 37 (4).
- Not checked on a device (§ Device check).

## T4 / T5 — Song form (2026-11-14) — To Review

Commits `61cfda0c` (T4), `23a1982a` (T5) on `feature/form-pictures`.

### Changed files
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs`
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`, `.xaml.cs`
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelPictureTests.cs` (new, 34 cases), `MyVocaList.Tests/Infrastructure/TestImageStore.cs` (new)

### Existing test files touched (exception, no assertion changed)
- `SongFormViewModelTests.cs`, `SongFormViewModelImageTests.cs`, `SongSuggestionTitleSearchRealStackTests.cs`: the constructor call gains `IImageStore`, `IPhotoPicker`.
- `Unit/DependencyInjection/AppServicesRegistrationTests.cs`: two mock registrations (`IImageStore`, `IPhotoPicker`) added, as that test already does for the other services registered only in `MauiProgram.cs`. Helder to confirm.

### Implementation decisions (beyond `design.md`)
- Constructor gains `IImageStore`, `IPhotoPicker`. New members: `FormImageSource`, `FormImageThumbnailSource`, `HasFormImage`, `ImageHintText`, `IsImageBusy`, `IsTitleEditing`, `IsArtistEditing`, `PickImageCommand`, `RemoveImageCommand`.
- **Save request:** `ImageUrl` is always the form picture; `ImageChanged` is true only in edit mode after an upload or a remove; `ArtistImageUrl` is never sent in edit mode.
- **Title release:** the Deezer identity is sent only in add mode and only while the Title equals the picked title. A release clears nothing.
- **Artist release:** unlocking outside hydration and outside X sets `SelectedArtistId = 0` and keeps the text, so Save sends the text.
- **Deezer artist picture** is sent only while the Artist text equals the picked artist name.
- An exception from the platform picker is caught, logged and shown as "Could not use this picture.".
- `OnAppearing` focuses the Title only in add mode while its editor is visible; in edit mode the song loads after `OnAppearing`.
- The two picture sources are strings (as `SongsPage` binds them).

### Verification evidence
- Android build: 0 errors. Suite on that branch: 1246 passed, 0 failed.
- Tests per requirement: REQ-PIC-17 (2), 18 (5), 13 (1), 16 (1), 19 (2), 20 (4), 21 (4), 22 (1), 10 (1), 05 (1), 15 (1), 27 (2), 28 (3), 31 (3), 32 (1), 39 (2).
- Flaky, not caused by this work: `SongRepositoryTests` and `VenueRepositoryTests` fail now and then with `SqliteException: SQLite Error 5` and pass on rerun.
- REQ-PIC-30 and the blur timing are component behaviour, checked on a device only.

## T9 — Close-out (2026-11-14) — To Review

Merged branch `feature/form-pictures` at `574e549d` (contains `feature/form-pictures-ui`).

### Changed files
- `requirements.md`: REQ-PIC-33, 37 and 40 amended (see "Spec amendments").
- `tasks.md`, `task-log.md`; README frontmatter of the backlog items (statuses).

### Verification evidence
- Full suite at `574e549d` (coordinator): 1277 passed, 0 failed.
- Android build at `574e549d` (coordinator, `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`): 0 errors, 27 warnings, 2 min.
- Independent verifier (read the diff `develop...feature/form-pictures`, ran nothing): **PASS WITH FINDINGS**. No Critical or Major defect, no spec contradiction, no non-negotiable violated, no governed component modified, no existing assertion changed beyond the declared one (`ArtistSuggestionServiceImageTests.cs`).

### Verifier findings (all Minor, not fixed in this change)
1. `Services/ImageStore.cs` `TryDelete` catches only `IOException`. An `UnauthorizedAccessException` during cleanup would escape `ImportAsync`; both view models still show "Could not use this picture.".
2. `ArtistFormViewModel` starts `LoadArtistForEditAsync` without awaiting and without a catch. A failed load shows no stored picture and is not logged.
3. `Services/ImageMaintenanceService.cs` injects two repositories it never uses (it resolves them from the unit-of-work scope).
4. `SongFormViewModel.BuildSaveRequest` compares the untrimmed Title with the picked title; a trailing space drops the Deezer identity.
5. The edit-mode artist-picture paths in `SongService` cannot be reached from the Song form, which sends no artist picture in edit mode.

### Spec amendments (2026-11-14, spec not shipped: amended in place)
- **REQ-PIC-33:** the Name field no longer stops typing at 60 characters; the length rule and the counter refuse a longer Name. A typing cap needs a change to the governed `AutocompleteDXField` (own task).
- **REQ-PIC-37:** the Artist form stores no external identity (REQ-SFT-49); the Deezer picture is the form picture only while the Name equals the picked name.
- **REQ-PIC-40:** the edit-mode artist-picture rule of `SongService` is now stated.

### AC traceability matrix
Test classes: IRR `ImageReferenceRulesTests`, IST `ImageStoreTests`, IMS `ImageMaintenanceServiceTests`, LIR `LocalImageReferencesRepositoryTests`, SFP `SongFormViewModelPictureTests`, AFI `ArtistFormViewModelImageTests`, SSP `SongServiceSaveFromFormPictureTests`, SSR `SongServiceSaveFromFormPictureRealStackTests`, ASP `ArtistServicePictureTests`, ASI `ArtistSuggestionServiceImageTests`, TTF `TapToEditFieldLogicTests`. "Device" = XAML or platform only, checked in § Device check.

| AC | Criterion | Implementation | Tests |
|----|-----------|----------------|-------|
| REQ-PIC-01 | One value in `ImageUrl`, no new column | `ImageReferenceRules` | IRR |
| REQ-PIC-02 | Invalid reference becomes null | `ImageReferenceRules.Normalize`, `SongService`, `ArtistService.SetArtistImageAsync` | IRR, ASP, SSP, SSR |
| REQ-PIC-03 | Display source by size | `ImageReferenceRules.ToDisplaySource` | IRR, IST |
| REQ-PIC-04 | Deezer picture stays a URL | `ToDisplaySource`, `Normalize` | IRR (through 02, 03) |
| REQ-PIC-05 | Pick from the library; cancel changes nothing | `MauiPhotoPicker`, both `PickImage` | SFP, AFI; picker: Device |
| REQ-PIC-06 | Centred square crop | `PlatformImageProcessor` | Device |
| REQ-PIC-07 | 256 / 512 px, JPEG 0.75 / 0.80 | `ImageReferenceRules`, `ImageStore` | IRR, IST; no enlarging: Device |
| REQ-PIC-08 | EXIF orientation | `MauiPhotoPicker` | Device |
| REQ-PIC-09 | App-private files, random names | `AppImagePaths`, `ImageStore.ImportAsync` | IRR, IST |
| REQ-PIC-10 | Failure and 25 MB limit keep no file | `ImageStore`, view model snackbar | IST, SFP, AFI |
| REQ-PIC-11 | Orphan sweep after 24 h | `ImageMaintenanceService`, `ImageStore.DeleteUnreferencedAsync`, repositories, `App.xaml.cs` | IMS, IST, LIR |
| REQ-PIC-12 | Picture field position | `SongFormPage.xaml`, `ArtistFormPage.xaml` | Device |
| REQ-PIC-13 | 120 dp, shape, large size | `ImagePickerField`, `FormImageSource` | SFP; shape: Device |
| REQ-PIC-14 | Placeholder by default | `ImagePickerField` | AFI; Device |
| REQ-PIC-15 | Add / Replace / Remove | `ImagePickerField`, view model commands | SFP; buttons: Device |
| REQ-PIC-16 | Hint only without a picture | `ImagePickerField`, `ImageHintText` | SFP, AFI |
| REQ-PIC-17 | Nothing stored before Save | view model commands | SFP |
| REQ-PIC-18 | Song form picture order | `SongFormViewModel.FormImageReference` | SFP |
| REQ-PIC-19 | An upload outlives picks | `ApplyRemotePick`, `ClearTitle` | SFP, AFI |
| REQ-PIC-20 | Remove | `RemoveImage` | SFP, AFI |
| REQ-PIC-21 | Save stores the form picture | `BuildSaveRequest`, `SongService` | SSP, SSR, SFP |
| REQ-PIC-22 | A failed Save keeps the form picture | `SongService`, view model | SSP, SSR, SFP |
| REQ-PIC-23 | Text state | `TapToEditFieldLogic.ShowsText`, `TapToEditField` | TTF; type size: Device |
| REQ-PIC-24 | Editor state | same | TTF |
| REQ-PIC-25 | Entering the text state | `TapToEditField`, `ShouldEndEditingAfterBlur` | TTF; timing: Device |
| REQ-PIC-26 | A tap opens the editor, text kept | `TapToEditField`, `ShouldReleasePick` | TTF, SFP, AFI; keyboard: Device |
| REQ-PIC-27 | Releasing a picked Title | `BuildSaveRequest` | SFP |
| REQ-PIC-28 | Releasing a picked Artist | `OnIsArtistLockedChanged` | SFP |
| REQ-PIC-29 | Search rules unchanged | unchanged members | existing REQ-SFT tests |
| REQ-PIC-30 | Reusable component | `TapToEditField` | TTF; Device |
| REQ-PIC-31 | Artist of a stored song can change | `OnIsArtistLockedChanged`, `SongService` | SFP, SSR |
| REQ-PIC-32 | No duplicate by changing the artist | `SongService` | SSR, SSP, SFP |
| REQ-PIC-33 | Name is the autocomplete field | `ArtistFormPage.xaml`, `SelectName` | existing REQ-SFT tests; Device |
| REQ-PIC-34 | Name options show the avatar | `ArtistSuggestionService`, `ToNameOption` | ASI, AFI |
| REQ-PIC-35 | Artist form picture | `ArtistFormViewModel.FormImageReference` | AFI |
| REQ-PIC-36 | Save stores the artist picture | `ArtistFormViewModel.Save`, `ArtistService` | AFI, ASP |
| REQ-PIC-37 | Name as tap-to-edit text | `ArtistFormViewModel`, `ArtistFormPage.xaml` | AFI |
| REQ-PIC-38 | Artists list avatar, trailing checkbox | `ArtistsPage.xaml` | Device |
| REQ-PIC-39 | Uploaded pictures show everywhere | `ImageReferenceConverter`, option mappers | SFP, AFI |
| REQ-PIC-40 | A Deezer pick persists the artist picture | `SongService`, Artist form | SSR, SSP |

## Device check (T10, Helder)

Nothing in this change was run on a device.

**Pictures (both forms)**
- [ ] "Add picture" or a tap on the picture opens the photo library. Cancel changes nothing.
- [ ] A landscape and a portrait photo are cropped to the centre and shown upright (EXIF).
- [ ] With a picture: "Replace picture" and "Remove picture" show and the hint is hidden. Without one: placeholder, "Add picture" and the hint.
- [ ] A corrupt file shows one "Could not use this picture." and leaves the picture unchanged.
- [ ] Android 12 or lower asks for the storage permission; Android 13+ asks for none.
- [ ] Uploaded pictures show from their files (form, lists, options).

**Song form**
- [ ] New song: Title editor has the keyboard; placeholder picture with the hint; Artist is an empty editor.
- [ ] Deezer Title pick: Title becomes large text with the edit icon, the cover shows, Artist becomes text, the keyboard closes.
- [ ] A tap on the Title or Artist text opens the editor with the text and the cursor in it. Blur without a change shows no error.
- [ ] Saving with a changed Title stores the song without the Deezer identity; the cover is still stored.
- [ ] Edit mode: Title and Artist open as text, no keyboard, stored picture shown.
- [ ] Edit mode: pick another artist and save (song relinked); type a new artist and save (created); move to an artist that already has this title and version (error on Artist).
- [ ] Upload, then a Deezer pick or X on Title: the upload stays. Remove, then a Deezer pick: the cover is set again.
- [ ] Edit mode: Save without touching the picture keeps it; Remove then Save clears it; upload then Save replaces it.

**Artist form**
- [ ] Add mode: Name editor focused; options show avatars for local and Deezer rows.
- [ ] Deezer pick: Name becomes Headline Medium text; the picture shows in the circle. Changing the Name removes the Deezer picture; X on Name keeps an upload.
- [ ] Save in add mode stores the picture; edit mode shows it; Remove then Save clears it.
- [ ] Edit mode: Name opens as text; a tap allows a rename; no search runs.
- [ ] A Name over 60 characters shows the error and the counter; the duplicate-suggestion buttons still work.

**Lists**
- [ ] Artists rows: leading avatar or placeholder, row height unchanged, checkbox trailing after the catalog button, selection and the catalog button still work.
- [ ] Songs rows show uploaded pictures.
