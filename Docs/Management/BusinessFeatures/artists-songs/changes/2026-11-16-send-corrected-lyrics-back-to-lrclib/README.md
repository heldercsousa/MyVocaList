---
id: send-corrected-lyrics-back-to-lrclib
title: Send corrected lyrics back to LRCLIB
status: 💡 Pending
target: 2026-11-16
section: BusinessFeatures
parent: artists-songs
goal: Lyrics the user wrote or corrected can be offered back to LRCLIB.
gate: Post-MVP. Needs the lyrics origin mark from the 2026-11-16 lyrics-on-first-open item, and LRCLIB publish rules verified first.
kind: change
horizon: post-mvp
---

# Send corrected lyrics back to LRCLIB

Lyrics the user wrote or corrected can be offered back to LRCLIB.

Decided by Helder on 2026-11-16: not part of the MVP. No requirements or design exist yet.

Before any spec:
- Verify how LRCLIB accepts contributions (endpoint, any proof-of-work or token step, request limits, required fields such as album and duration). Nothing about this is confirmed in the repo.
- This item depends on the lyrics edit mark delivered by `../2026-11-16-songs-list-searches-title-and-artist-deezer-versions-collapsed-deezer-artist-frozen-lyrics-on-first-open/`: only lyrics the user wrote or changed are candidates.
- Open question for Helder at that time: whether lyrics with the user's own annotations above the verses may be sent, or only the plain corrected text.

