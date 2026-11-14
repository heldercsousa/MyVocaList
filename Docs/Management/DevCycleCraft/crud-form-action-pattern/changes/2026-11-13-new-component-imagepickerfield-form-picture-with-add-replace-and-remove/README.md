---
id: new-component-imagepickerfield-form-picture-with-add-replace-and-remove
title: "New component ImagePickerField: form picture with add, replace and remove"
status: 🟡 In Progress
target: 2026-11-13
section: DevCycleCraft
parent: crud-form-action-pattern
goal: "A form field that shows a square or round picture with a placeholder icon, buttons to add, replace or remove it, and a size hint shown only while there is no picture. Consumers: Song form and Artist form."
gate: Governed from its second consumer; Helder passes the device check.
kind: change
---

# New component ImagePickerField: form picture with add, replace and remove

A form field that shows a square or round picture with a placeholder icon, buttons to add, replace or remove it, and a size hint shown only while there is no picture. Consumers: Song form and Artist form.

## Origin
Helder, 2026-11-13: the Song form and the Artist form show the picture at the top with a way to add or replace it, and a small size hint shown only while there is no picture.
Spec: `Docs/Management/BusinessFeatures/artists-songs/changes/2026-11-13-pictures-for-songs-and-artists-upload-with-automatic-crop-kept-as-app-private-files/requirements.md` (REQ-PIC-12 to 16) and `design.md` § 2.5. Task T2b in that folder's `tasks.md`.

## Component record
- **New component, no consumer yet**; governed from its second consumer (Song form, then Artist form), added to `.claude/library/component-safety-gate.md` in that commit.
- **DevExpress first:** `dx:DXButton` for the actions and `dx:DXImage` for the placeholder icon. The bitmap uses the layered stock `Image` of `ListItemLeadingImage`. `dxe:ImageEdit` is not used: it is a manual crop editor and the crop here is automatic (`design.md` P8).
- **Planned consumers:** `SongFormPage`, `ArtistFormPage`.
- **Approval:** pre-authorised by Helder on 2026-11-13; he can overrule after the device check.
