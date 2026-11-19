---
id: imagepickerfield-bigger-centred-picture-tap-to-pick-corner-remove-button
title: "ImagePickerField: bigger centred picture, tap to pick, corner remove button"
status: ✅ Done
target: 2026-11-18
section: DevCycleCraft
parent: new-component-imagepickerfield-form-picture-with-add-replace-and-remove
goal: "The form picture grows to a centred square inset from the form margins, tapping it adds or replaces the picture, and a small remove icon button over its top-right corner replaces the three text buttons. Consumers: Venue, Song and Artist forms."
gate: Helder passes the device check.
closed: 2026-11
kind: change
---

# ImagePickerField: bigger centred picture, tap to pick, corner remove button

The form picture grows to a centred square inset from the form margins, tapping it adds or replaces the picture, and a small remove icon button over its top-right corner replaces the three text buttons. Consumers: Venue, Song and Artist forms.

## Origin
Helder, 2026-11-18: the form picture is too small to see photos and logos well, and the three
text buttons clutter the form. Make the picture bigger (centred, narrower than the other form
fields), let a tap on the picture add or replace it, and remove it with an icon over the
top-right corner. Removal takes effect only on Save. Helder pre-authorised the change and the
component edit; he checks it on a device and asks for enhancements if anything looks wrong.

## Supersedes (shipped specs stay unchanged)
- `artists-songs/changes/2026-11-13-pictures-for-songs-and-artists-…/requirements.md`:
  **REQ-PIC-13** (120 x 120 dp size) and **REQ-PIC-15** (the button set) are replaced by
  REQ-IPF-01..04 below. REQ-PIC-12, 14, 16, 17, 20, 21 stand.
- `venues/changes/2026-11-14-venue-picture-…/requirements.md`: the size in **REQ-VEN-07**, the
  "Add picture" button in **REQ-VEN-08** and the button set in **REQ-VEN-09** are replaced by
  REQ-IPF-01..04. The hint rule of REQ-VEN-09, REQ-VEN-10 and REQ-VEN-11 stand.

## Requirements
- **REQ-IPF-01 Size.** The form picture is a square (a circle on the Artist form), centred in
  the form. Its side is the form's content width minus 40 dp on each side, at most 360 dp.
  Width and height change together; the picture never stretches out of 1:1.
- **REQ-IPF-02 Tap.** WHEN there is no form picture, tapping the picture opens the picker
  (add). WHEN there is one, tapping it opens the picker and a pick replaces it. There are no
  "Add picture", "Replace picture" or "Remove picture" buttons.
- **REQ-IPF-03 Remove.** WHEN there is a form picture, a small round icon button (trash-can
  icon, tonal background, icon centred) sits over the top-right corner of the picture. Tapping it removes the form
  picture: the placeholder shows. WHEN there is no form picture, the button is hidden. The
  record changes on Save only (REQ-PIC-17 / REQ-VEN-10); no file is deleted at tap or Save —
  unreferenced files are removed by the startup sweep (REQ-PIC-11).
- **REQ-IPF-04 Busy.** WHILE the picture is busy (import in progress), tapping the picture and
  the remove button do nothing; the remove button shows disabled.
- The hint below an empty picture (REQ-PIC-16) and the stored 512 px large size are unchanged.

## Design
- Only `UI/Components/Media/ImagePickerField.xaml(.cs)` changes. Its BindableProperties stay
  (`RemoveCommand` now drives the corner button), so the three pages need no edit.
- **Sizing:** the component computes the side from its own allocated width
  (`SizeChanged`/`OnSizeAllocated`): `side = Math.Min(width - 2 * 40, 360)`, applied to the
  frame and both image layers (width and height). The 120 dp literals go.
- **Corner button:** `dx:DXButton` with the project's icon-button style and `delete_outlined`
  (trash-can) icon, centred in the button, tonal background, about 40 dp, overlaid in a `Grid` with the frame, aligned top-end
  with a small inset so it stays inside the square's corner. On the circle it sits at the
  bounding-box corner over the edge (the usual badge spot). Accessibility description:
  "Remove picture".
- **Picture tap:** the existing `TapGestureRecognizer` stays. Semantic hint: "Double tap to
  choose a picture" when empty, "Double tap to replace the picture" when filled.
- DevExpress first: `dx:DXButton` for the remove action (no DX overlay-badge control fits).

## Consumer map (grep `media:ImagePickerField`, 2026-11-18)
| Consumer | Risk | Verification |
|----------|------|--------------|
| `UI/Pages/Venues/VenueFormPage.xaml:46` | Square too wide inside the 24 dp page padding; map-pick picture import (IsBusy) leaves remove enabled | Device: add/replace/remove, map pick, Save and Cancel |
| `UI/Pages/Songs/SongFormPage.xaml:93` | Inside a `ShimmerView` — width may be 0 on first layout, so the side must recompute when width arrives; Deezer pending picture must show the corner button | Device: add mode with Deezer pick, edit mode, remove then Save |
| `UI/Pages/Artists/ArtistFormPage.xaml:64` (`IsCircle`) | Corner button over the circle edge may look detached | Device: add/replace/remove on the circle, Save |

## Approval
Pre-authorised by Helder on 2026-11-18 (component edit included); he can overrule after the
device check. Size and icon are Claude's calls, open to Helder's adjustment.

> **Spec updated 2026-11-18 (after Helder's device check):** the corner icon is the trash can
> (`delete_outlined`), not the close icon, which read as an "X" character; and the icon must sit
> centred in the round button (it showed off-centre horizontally).
