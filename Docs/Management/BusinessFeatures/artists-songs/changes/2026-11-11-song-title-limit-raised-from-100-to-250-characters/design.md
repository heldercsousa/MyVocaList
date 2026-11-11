# Design — Song title limit raised from 100 to 250 characters

## Approach
Single source of truth stays `SongService.MaxTitleLength` (`Services/SongService.cs:21`, currently 100); it feeds
`ValidateTitleInput`, the error text and the counter (`SongService.cs` ~58–59, ~688–692). Change it to 250.

## Layers touched
- **Services:** `MaxTitleLength => 250`. No other logic change (Level A: validation).
- **Infra:** `Song.Title` max length in the EF configuration, plus a migration. The snapshot currently has
  `HasMaxLength(100)` on `Song.Title`. SQLite does not enforce TEXT length, so the migration changes model metadata
  only; verify the generated migration contains no table rebuild that could drop the FTS5 trigram index or
  triggers (see `20261104161213_AddSearchIndexes`). If it rebuilds, stop and escalate to Helder.
- **UI:** no change. The Song form (SFT 5.5) takes the limit from the service; it has no hard keystroke cap.
- **Tests:** update the 100/101 boundary tests to 250/251 (`SongServiceTests`, `SongFormViewModelTests`
  message strings, `SongServiceNewArtistUnitOfWorkTests` 101-char injection comment/value).

## Risks
- Migration is a Sequential-only file; one writer, after SFT 5.5 merges.
- FTS/trigram indexes: AC-STL-04 covers a long title.
