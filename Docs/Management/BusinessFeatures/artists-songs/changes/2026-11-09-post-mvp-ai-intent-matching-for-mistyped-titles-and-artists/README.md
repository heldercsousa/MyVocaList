---
id: post-mvp-ai-intent-matching-for-mistyped-titles-and-artists
title: "Post-MVP: AI intent matching for mistyped titles and artists"
status: 💡 Pending
target: 2026-11-09
section: BusinessFeatures
parent: artists-songs
goal: An AI step ranks or suggests the intended song or artist when the typed text is wrong, next to the fuzzy matcher that stays as the offline baseline and duplicate guard.
gate: "Post-MVP; design first: re-rank versus embeddings versus cloud fallback, offline behaviour, privacy, cost."
kind: change
---

# Post-MVP: AI intent matching for mistyped titles and artists

An AI step ranks or suggests the intended song or artist when the typed text is wrong, next to the fuzzy matcher that stays as the offline baseline and duplicate guard.

**Origin:** Helder, 2026-11-09 (after the device run of the song form save flow).

**Position:** AI supplements the fuzzy and trigram matching; it does not replace it. Fuzzy stays as the instant, offline, deterministic baseline and as the duplicate guard on Save (a data-integrity rule must not depend on a model).

**Where AI adds value:** phonetic or transliterated spellings, wrong word order, misheard lyrics, artist nicknames, songs known by their chorus; and ranking by singer performance history (the history that is kept for future suggestions).

**Options to design (none chosen):**
1. Re-rank fuzzy and trigram candidates and offer "Did you mean...?" (model never writes data).
2. Local embeddings of titles and artists (works offline; costs storage and app size).
3. Cloud model fallback only when nothing matches (online only; cost, privacy, latency; needs a visible searching state).

**Open questions:** offline behaviour, privacy of typed text, cost per query, who owns the model call (Services layer only), consent, how a suggestion is confirmed before anything is saved.
