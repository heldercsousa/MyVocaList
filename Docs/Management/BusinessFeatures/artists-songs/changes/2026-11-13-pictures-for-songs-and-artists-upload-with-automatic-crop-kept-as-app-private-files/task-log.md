# Task log: pictures and tap-to-edit names in the Song and Artist forms

### Checkpoint
- **Now:** Wave 1 dispatch (T1 image infrastructure, T2a/T2b components).
- **Next:** merge Wave 1, then Wave 2 (T3 services, T8 lists).
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
