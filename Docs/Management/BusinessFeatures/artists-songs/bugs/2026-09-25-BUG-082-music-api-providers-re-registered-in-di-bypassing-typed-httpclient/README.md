---
id: BUG-082
title: Music API providers re-registered in DI, bypassing typed HttpClient
status: 🟡 In Progress
severity: Major
target: 2026-09-25
parent: artists-songs
goal: A second DI registration hands the MusicBrainz and Deezer providers an HttpClient with no base URL, so every third-party search fails silently and returns nothing.
gate: Providers resolve with the configured client; a DI regression test fails first then passes.
kind: bug
---

# Music API providers re-registered in DI, bypassing typed HttpClient

A second DI registration hands the MusicBrainz and Deezer providers an HttpClient with no base URL, so every third-party search fails silently and returns nothing.


## Evidence

Found by the read-only evaluation for the title-first song form
(`../../changes/2026-09-24-song-form-title-first-one-search-over-local-and-music-apis/findings.md` § 1):
`ServiceCollectionExtensions.cs:34-45` sets up typed HttpClients for MusicBrainz and Deezer, then
re-registers the providers so that the resolved instances get an unconfigured client. The failure
is caught and surfaces as "No songs found". It was reproduced in a scratch console app. No test
covers it, because every provider test uses mocks.

Helder, 2026-09-24: the picker pages "never ever worked as expected", which is consistent with this.
The pickers are being deleted, but this fix is gate **G1** of the title-first item: the new
autocomplete uses the same providers.
