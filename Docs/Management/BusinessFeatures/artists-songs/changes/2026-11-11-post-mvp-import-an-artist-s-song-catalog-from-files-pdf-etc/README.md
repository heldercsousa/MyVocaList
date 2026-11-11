---
id: post-mvp-import-an-artist-s-song-catalog-from-files-pdf-etc
title: "Post-MVP: import an artist's song catalog from files (PDF etc.)"
status: 💡 Pending
target: 2026-11-11
section: BusinessFeatures
parent: artists-songs
goal: "An artist uploads files such as PDFs holding their song catalog; the app extracts title, artist and lyric, registers the songs as that artist's catalog, and skips songs already registered, attaching them to the uploading artist's catalog. Gate: Post-MVP; design before any code."
kind: change
---

# Post-MVP: import an artist's song catalog from files (PDF etc.)

An artist uploads files such as PDFs holding their song catalog; the app extracts title, artist and lyric, registers the songs as that artist's catalog, and skips songs already registered, attaching them to the uploading artist's catalog. Gate: Post-MVP; design before any code.


## Origin
Helder, 2026-11-11, while settling song identity (see `../2026-11-11-discuss-song-identity-by-lyric-version-de-duplication-and-richer-deezer-data-in-the-song-ux/`). Post-MVP; design before any code.

## Idea
Artists who perform with MyVocaList may want to upload an existing song catalog from files (PDF and similar) instead of typing it. The app reads each file and imports song info automatically: **title + artist + lyric**.
- The imported songs are registered as the **catalog of the uploading artist**.
- Songs already registered (same identity: title + artist + lyric) are **bypassed, not duplicated**; the existing song is attached to the uploading artist's catalog instead.
- The app may hold **n performer catalogs** (several artists uploading to the same app database); a song can sit in more than one catalog.

## Questions for later
1. File formats and extraction: PDF text vs scanned images (OCR), Word, plain text; how to split a file into songs and find title/artist/lyric reliably; a review step before saving.
2. Identity match during import: title + artist + lyric, with what tolerance on whitespace, casing and punctuation? What if the lyric differs slightly (see the lyric-change flow in the discussion item)?
3. Catalog model: an artist-performer catalog as a many-to-many relation to songs; relation to the "song writes propagate to the artist catalog" item (`../2026-08-04-song-writes-propagate-to-the-artist-catalog/`).
4. Privacy and rights: uploaded files may hold third-party lyrics; local-only processing preferred.
