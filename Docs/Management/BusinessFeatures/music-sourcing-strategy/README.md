---
id: music-sourcing-strategy
title: "**Music sourcing strategy — Deezer / LRCLIB / YouTube / Bandokê / BYO Audio**"
status: "💡 Pending"
target: —
section: BusinessFeatures
kind: feature
order: 380
goal: "evaluate possibilities for YouTube compliance, AI pre-fill, Bandokê and BYO audio (not decided)."
gate: "Discuss with Helder before any action."
pointer: BusinessFeatures/music-sourcing-strategy/
---

# Music sourcing strategy

Origin: a chat held on 2026-11-14 with the Gemini web assistant about MyVocaList. The content of `estrategia-gemini-2026-11-14.md` is a set of **possibilities to evaluate**, not decisions. Some features already exist in the app; the rest are candidate additions to be discussed later.

Back-reference: none (new item, no prior spec folder).

Update 2026-11-16: Helder decided that Deezer song data stays a small local row (storing only the Deezer id was rejected) and that missing lyrics are copied from LRCLIB on first open. Spec: `../artists-songs/changes/2026-11-16-songs-list-searches-title-and-artist-deezer-versions-collapsed-deezer-artist-frozen-lyrics-on-first-open/`. Searching Deezer from the Songs list is an undecided trial: `../artists-songs/changes/2026-11-16-songs-list-searches-deezer-one-tap-saves-a-thin-song-row-lyrics-copy-with-edit-mark/`. The other possibilities in this item stay undecided.

Update 2026-11-22: Helder made the catalog (band repertoire for Bandokê) and in-app YouTube integration MVP scope, and ruled that compliance overrides the 2026-11-16 choice. The Deezer terms re-checked that day ban any local copy of Content, including metadata. `findings.md` § 5 holds the evidence, MusicBrainz as the candidate source and the revised triage.

Update 2026-11-21: research on Deezer terms, YouTube without an API key and lyrics coverage, with an MVP triage of Helder's points 1–1.7: `findings.md`. Key results: Deezer terms are non-commercial only, require a visible Deezer logo and forbid storing images. LRCLIB grants no lyrics display license. A YouTube embed works without a key if the WebView sends the app id as Referer.
