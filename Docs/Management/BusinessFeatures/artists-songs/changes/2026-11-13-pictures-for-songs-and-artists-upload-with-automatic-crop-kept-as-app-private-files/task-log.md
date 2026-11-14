# Task log: pictures and tap-to-edit names in the Song and Artist forms

### Checkpoint
- **Now:** Wave 2 closing (T8 lists) and Wave 3 first half (T4, T5 Song form), resumed 2026-11-14 after the usage limit.
- **Next:** merge, then T6, T7 (Artist form), then T9 close-out.
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
