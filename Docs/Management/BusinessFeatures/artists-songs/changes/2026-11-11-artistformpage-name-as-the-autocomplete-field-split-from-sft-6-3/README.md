---
id: artistformpage-name-as-the-autocomplete-field-split-from-sft-6-3
title: ArtistFormPage Name as the autocomplete field (split from SFT 6.3)
status: ✅ Done
target: 2026-11-11
section: BusinessFeatures
parent: artists-songs
goal: Typing metalica on New Artist lists Metallica from the library first, then Deezer artists. A local pick opens its edit form; a Deezer pick fills and locks Name. Waits for the shared autocomplete work in the other terminal.
closed: 2026-11
kind: change
---

# ArtistFormPage Name as the autocomplete field (split from SFT 6.3)

Typing metalica on New Artist lists Metallica from the library first, then Deezer artists. A local pick opens its edit form; a Deezer pick fills and locks Name. Waits for the shared autocomplete work in the other terminal.


## Observation

Closed on 2026-11-16 at Helder's confirmation that the work is done. The code shipped in f3b4b9ab, which also registered TapToEditField and ImagePickerField in the governed-component list (component-safety-gate.md).
