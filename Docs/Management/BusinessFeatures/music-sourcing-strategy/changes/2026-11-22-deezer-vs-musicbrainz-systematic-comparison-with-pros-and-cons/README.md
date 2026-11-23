---
id: deezer-vs-musicbrainz-systematic-comparison-with-pros-and-cons
title: Deezer vs MusicBrainz — systematic comparison with pros and cons
status: 💡 Pending
target: 2026-11-22
section: BusinessFeatures
parent: music-sourcing-strategy
goal: Compare exactly what Deezer and MusicBrainz offer (data, terms, limits, cost, storage rights) to decide the catalog source, including the earlier dual-source idea with MusicBrainz as the fallback when Deezer is offline.
gate: Helder evaluates the comparison before any change of source; Deezer stays meanwhile.
kind: change
---

# Deezer vs MusicBrainz — systematic comparison with pros and cons

Compare exactly what Deezer and MusicBrainz offer (data, terms, limits, cost, storage rights) to decide the catalog source, including the earlier dual-source idea with MusicBrainz as the fallback when Deezer is offline.


Origin: Helder, 2026-11-22. Early versions planned a dual source: Deezer first, MusicBrainz only when Deezer is offline. Before any change, Helder wants to know exactly what MusicBrainz offers, compared with Deezer. **Deezer stays the source until he decides.**

Starting evidence: `../../findings.md` § 5 (Deezer terms re-checked; MusicBrainz considerations). Those are considerations, not a plan.

## What the comparison must cover
- **Data:** search quality for Brazilian and international songs; titles, artists, albums, works, durations, ISRC; artwork; genres; popularity and charts (regional or not).
- **Terms:** commercial use (free phase and paid phase); local storage of names and ids; combining with YouTube video; attribution and logo duties.
- **Access:** keys or registration; rate limits; reliability and outages; cost (MetaBrainz plans; self-hosting from the CC0 dumps).
- **Fit:** the duplicate-title problem; the band repertoire (Bandokê); the existing Deezer code paths and stored rows.
- **Dual source:** what a fallback needs (shared ids, matching rules, which fields can differ).
- **Output:** a pros and cons table per criterion, live API checks marked V/I as in `findings.md`, and a recommendation for Helder.
