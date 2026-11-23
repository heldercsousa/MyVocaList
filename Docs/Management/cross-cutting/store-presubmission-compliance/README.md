---
id: store-presubmission-compliance
title: "Play Store + Samsung Galaxy Store pre-submission compliance"
status: "💡 Pending"
target: —
section: DevCycleCraft
kind: feature
order: 320
goal: "pass store automated pre-review (target API, permissions, data safety, assets)."
pointer: cross-cutting/store-presubmission-compliance/
---

# Play Store + Samsung Galaxy Store pre-submission compliance

Migrated from the pre-migration BACKLOG.md row (no prior spec folder).

Back-reference: `Docs/Management/cross-cutting-log.md` (retained; not migrated).

## Third-party content terms (added 2026-11-21)

Source: `../../BusinessFeatures/music-sourcing-strategy/findings.md`. Checked against the code on 2026-11-21.

| Point | Rule | Current state | Action |
|---|---|---|---|
| Deezer logo | "Each application using Deezer API/SDKs must … include a clearly visible Deezer Logo" (developers.deezer.com/guidelines/logo) | **Gap.** No logo; only a plain "Deezer" text label on suggestion rows (`SongFormViewModel`, `ArtistFormViewModel`) | Add the official logo where Deezer results appear |
| Deezer titles and names (re-checked 2026-11-22) | Terms §III.4: no "local copy of any Content"; §I counts metadata as Content. The FAQ that allowed names now answers 404 | **Gap.** Deezer titles and artist names are saved in local song and artist rows | **Helder decides before release:** store the Deezer id only, or change source after the Deezer vs MusicBrainz comparison (Deezer stays until then). See `findings.md` § 5 |
| Deezer images | FAQ (now 404): names may be stored locally; images may not | **OK.** `Song/Artist.ImageUrl` store the URL string; `ImageReferenceRules` passes the URL to a plain `<Image>`. Only Wikidata venue pictures and user-picked photos are written to disk | Keep it that way; never download Deezer artwork into `ImageStore` |
| Deezer non-commercial use | Terms §III.1: private, non-commercial only | **OK** while the app is free | A paid version needs a Deezer agreement or another catalog source |
| Lyrics display | LRCLIB grants no display license; lyrics are copyrighted (Lei 9.610/98) | **Risk.** LRCLIB lyrics are copied into `Song.Lyrics` and shown in full on `SongFormPage` (`LrclibLyricsProvider` → `SongLyricsService`). Nothing is sent back to LRCLIB | **Helder decides before release:** keep (risk accepted for a free app), show only on demand, or user-supplied lyrics only |
| YouTube | No scraping; the app only opens a YouTube search externally | **OK** | Any future embed must follow the IFrame rules (Referer, no overlays) |
