# Requirements — Song title limit raised from 100 to 250 characters

> Change to `artists-songs`; cross-references the in-progress SFT change
> (`../2026-09-24-song-form-title-first-one-search-over-local-and-music-apis/requirements.md` lines 215 and 411,
> which state "≤ 100 chars after trim"). Helder ruling 2026-11-11: allow 250 characters so long titles
> (API imports with "(feat. …) – Remastered … – Live at …", classical works, medleys, other cultures) fit.

## Scope
Only the maximum length of `Song.Title`. No other title rule changes (still required; trimmed before the check).

## Requirements
| ID | Requirement |
|----|-------------|
| REQ-STL-01 | `SongService.MaxTitleLength` is 250. A title of 250 characters after trim is valid; 251 is rejected with "Title is too long. Maximum 250 characters." |
| REQ-STL-02 | The title counter (`n/250`) and its error state use the same limit (no second constant). |
| REQ-STL-03 | The EF model for `Song.Title` declares max length 250, with a migration, and existing rows are unchanged. |
| REQ-STL-04 | Search, near-duplicate detection and the unique/identity indexes behave identically for titles of 101–250 characters. |

## Acceptance criteria
- AC-STL-01: 250-char title saves; 251-char title is rejected with the 250 message. (REQ-STL-01)
- AC-STL-02: counter reads `250/250` without error at 250 and shows the error state at 251. (REQ-STL-02)
- AC-STL-03: after the migration, the model snapshot shows `HasMaxLength(250)` for `Song.Title` and the migration applies on an existing database without data loss. (REQ-STL-03)
- AC-STL-04: a repository test with a 250-char title finds it by exact and trigram search. (REQ-STL-04)
