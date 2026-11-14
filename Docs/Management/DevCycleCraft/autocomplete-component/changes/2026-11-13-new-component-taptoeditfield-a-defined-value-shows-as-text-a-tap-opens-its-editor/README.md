---
id: new-component-taptoeditfield-a-defined-value-shows-as-text-a-tap-opens-its-editor
title: "New component TapToEditField: a defined value shows as text, a tap opens its editor"
status: 🟡 In Progress
target: 2026-11-13
section: DevCycleCraft
parent: autocomplete-component
goal: "A wrapper for any form editor: while the value is defined and not being edited it shows as text in a chosen type style; a tap shows the editor with the cursor in it. First consumers: Song form Title and Artist, Artist form Name."
gate: Governed from its second consumer; Helder passes the device check.
kind: change
---

# New component TapToEditField: a defined value shows as text, a tap opens its editor

A wrapper for any form editor: while the value is defined and not being edited it shows as text in a chosen type style; a tap shows the editor with the cursor in it. First consumers: Song form Title and Artist, Artist form Name.

## Origin
Helder, 2026-11-13: a defined Title, Artist or Name must read as larger text and open for editing when tapped, and the behaviour must be reusable around other form entries.
Spec: `Docs/Management/BusinessFeatures/artists-songs/changes/2026-11-13-pictures-for-songs-and-artists-upload-with-automatic-crop-kept-as-app-private-files/requirements.md` (REQ-PIC-23 to 30) and `design.md` § 2.5 (BindableProperty table, focus and blur rules). Task T2a in that folder's `tasks.md`.

## Component record
- **New component, no consumer yet**, so the four gates do not apply to its creation. It becomes governed with its second consumer (Song form Title and Artist, then Artist form Name) and is added to `.claude/library/component-safety-gate.md` in that commit.
- **It wraps `AutocompleteDXField` without changing it** (no new member, no XAML change), so no governed-component change is made.
- **MD3:** Material Design 3 has no component for this pattern; the name describes the behaviour. The text uses MD3 type styles (Headline Medium, Title Large) and the 48 dp minimum touch target.
- **Planned consumers:** `SongFormPage` (Title, Artist), `ArtistFormPage` (Name).
- **Approval:** pre-authorised by Helder on 2026-11-13; he can overrule after the device check.
