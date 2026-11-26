# Task log — YouTube without key + MusicBrainz + Wikidata artist images

---
## Task: T1 MusicBrainzProvider + rate limiter + ProviderNames (REQ-MSRC-02, -10)
**Plan:** `plan.md` (this folder)
**Status:** To Review
**Started:** 2026-11-26
**Completed:** 2026-11-26

### Changed files:
- `Domain/Constants/ProviderNames.cs`
- `Services/MusicBrainzProvider.cs`
- `Services/MusicBrainzRateLimiter.cs`
- `Services/MusicBrainzResponses.cs`
- `Tests/Unit/Services/MusicBrainzProviderTests.cs`
- `MyVocaList.sln` (task-log registration)

### AC traceability
| AC | Implementation | Test |
|----|----------------|------|
| REQ-MSRC-02 | `MusicBrainzProvider.SearchSongsAsync` / `SearchArtistsAsync` | `SearchSongsAsync_WithArtistHint_SendsLuceneQuery`, `SearchArtistsAsync_MapsMbidAndName` |
| REQ-MSRC-10 | `MusicBrainzRateLimiter`, `BuildUserAgent`, 503 -> `MusicProviderException` | `WaitAsync_TwoCalls_SecondWaitsAtLeast1000ms`, `SearchSongsAsync_Http503_ThrowsMusicProviderException`, `BuildUserAgent_MissingContact_ReturnsBareProductToken` |

### Build notes
Tests: 6 passed, 0 failed (MusicBrainzProviderTests). Android build: see report. Tests were written together with the implementation (no separate Red run). Not wired into DI (T2b). The one-warning-on-missing-contact behaviour is left to the DI/UA wiring in T2b; `BuildUserAgent` only formats.
