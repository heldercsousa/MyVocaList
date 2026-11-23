# Artists & Songs — spec changelog

Shipped specs in this folder are immutable; post-ship changes are recorded here and in `changes/`.

| Date | Change | Effect on shipped specs |
|------|--------|-------------------------|
| 2026-11-20 | `changes/2026-11-11-evaluate-removing-the-song-form-version-and-featured-artists-fields/` removes `Song.Version` and `Song.FeaturedArtists` (form, model, database, duplicate "Save as new version", Deezer `title_version`). Song identity becomes (Artist, Title). | Row-by-row in that folder's `supersession.md` (parent AC-9.3, AC-10.2, AC-11.2, AC-14.1, song-import-resolution INV-2, REQ-SFT-12/14/22/23/29, REQ-SLD-08, REQ-ART-12/20 and others). |
