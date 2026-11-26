---
id: BUG-092
title: "Song form: the Artist field shows no suggestions"
status: 💡 Pending
severity: Major
target: 2026-11-26
section: BusinessFeatures
parent: artists-songs
goal: Typing in the Song form Artist field lists matching artists again.
gate: Helder confirms suggestions on the phone. Starts after the DX to Syncfusion migration lands.
kind: bug
---

# Song form: the Artist field shows no suggestions

Typing in the Song form Artist field lists matching artists again.

Found by Helder's phone check of 2026-11-26 (debug APK `com.myvocalist-debug-20261126-173207-b410cb1f.apk`, step 6 of the search follow-ups test list): typing in the Song form Artist field lists no artists.

## What is known

- The field is the Syncfusion `AutocompleteField` (`SongFormPage.xaml`, `OptionSource="{Binding ArtistOptionSource}"`), fed by `SongFormViewModel.SearchArtistOptionsAsync` -> `SearchArtistsCoreAsync`.
- The search follow-ups branch changed `SearchArtistsCoreAsync`: it passes the cancellation token (B1), uses `SearchConstants.MaxSearchResults` (B2, also 5), runs the read through `Task.Run` (B4) and removed the unused `ArtistSuggestions` / `SearchArtistsCommand` (B12). The Title field got the same B4 offload and works.
- Not yet checked whether develop before that branch has the same defect; the DX to Syncfusion migration also changed this field.

## Severity

Registered as Major: adding a local song needs the artist. Downgrade to Minor if typing the full name and saving still resolves the artist.

## How to fix

1. Check whether the defect exists on develop before the search follow-ups merge, to find which change caused it.
2. Write a failing test for the cause (Red), then fix it (Green).

Helder's direction (2026-11-26): fix it after the DX to Syncfusion migration really completes.
