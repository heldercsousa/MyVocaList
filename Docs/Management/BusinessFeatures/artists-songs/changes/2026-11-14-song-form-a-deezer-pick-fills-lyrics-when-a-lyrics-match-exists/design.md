# Design: a Deezer pick fills Lyrics

> **Status: APPROVED 2026-11-14** under Helder's instruction of the same day (see `requirements.md`).

## 1. Decisions taken for Helder (2026-11-14)

Each row can be overruled; the cost of reversing is noted.

| # | Question | Decision | Why | Cost to reverse |
|---|----------|----------|-----|-----------------|
| L1 | Where do the lyrics come from? | **LRCLIB** (`https://lrclib.net/api/get`). | Deezer's public API has no lyrics text; its internal lyrics endpoint needs a logged-in user's cookie. LRCLIB is free, needs no key, and matches by title, artist, album and duration, which a Deezer pick provides. | Low: one provider class behind `ILyricsProvider`. |
| L2 | Which text? | **`plainLyrics` only.** | The Lyrics field is plain text; synced lyrics carry time stamps. | Low. |
| L3 | Does a lookup replace existing lyrics? | **Never.** It runs only when Lyrics is blank and fills only if it is still blank. | Helder: "fill the lyrics entry"; typed text is the admin's work. | Low. |
| L4 | Lyrics longer than the 10,000 limit | **Not used** (field stays empty). | A cut text would look complete. | Low: constant + rule. |
| L5 | Feedback | **Placeholder "Looking for lyrics…" while it runs; nothing on no match or failure.** | Helder: "when not available, just leave the entry empty". Same silent style as the Title search (REQ-SFT-07). | Low. |
| L6 | Time bound | **5,000 ms** for the whole lookup (both attempts). | LRCLIB can be slower than Deezer; the field stays editable, so a longer bound costs nothing. | Low: constant. |
| L7 | Retry | **One second attempt with title + artist only** after a 404. | Album and duration narrow the match; Deezer and LRCLIB durations can differ by more than LRCLIB's ±2 s tolerance. | Low. |
| L8 | X on Title / Artist | **Removes pick-filled lyrics only if untouched.** | Mirrors the Version rule (REQ-SFT-14). | Low. |
| L9 | Where is the duration from? | **`duration` of the Deezer `search/track` row**, carried in the suggestion DTOs, never stored. | Already in the response the app receives; no extra request (REQ-ART-04 style). | Low. |

## 2. Architecture

Layers touched: Contracts (DTOs), Domain (constants), Services (provider, service), UI (ViewModel, one XAML attribute). No Infra change, no migration.

```
SelectTitle ─► ApplyRemotePick(dto) ─► StartLyricsLookup(dto)          (SongFormViewModel)
                                              │ LyricsQuery(title, artist, album, duration)
                                              ▼
                              ISongLyricsService.FindLyricsAsync       (Services: offline, bound, retry, length)
                                              ▼
                              ILyricsProvider.FetchLyricsAsync         (Services: LrclibLyricsProvider, HTTP)
```

Business rules (offline skip, bound, retry, length limit, trim) live in `SongLyricsService`. The
ViewModel only decides whether the form may still take the answer (REQ-LYR-04, 10, 11, 12).

### 2.1 Contracts

- `Contracts/DTOs/LyricsQuery.cs` (new): `public sealed record LyricsQuery(string Title, string ArtistName, string? AlbumTitle = null, int? DurationSeconds = null);`
- `MusicSearchResultDto` gains one optional trailing parameter `int? DurationSeconds = null`.
- `SongSuggestionDto` gains one optional trailing parameter `int? DurationSeconds = null`.

### 2.2 Domain

- `Domain/Constants/SearchConstants.cs`: `public const int LyricsLookupTimeoutMilliseconds = 5000;`
- `Domain/Constants/SearchConstants.cs`: `public const int LyricsMaxLength = 10000;`

### 2.3 Services

- `DeezerProvider`: `DeezerTrack` deserializes `duration` (`int`, seconds); `SearchSongsAsync` maps it to
  `DurationSeconds` (null when ≤ 0).
- `SongSuggestionService`: remote rows copy `DurationSeconds` from `MusicSearchResultDto` to
  `SongSuggestionDto`. Local and promoted rows leave it null.
- `Services/ILyricsProvider.cs` (existing placeholder, no implementation, no consumer; signature replaced):
  ```csharp
  public interface ILyricsProvider
  {
      /// Plain lyrics of the best record for the query, or null when the source has no usable record.
      /// Throws on transport or protocol failure; rethrows OperationCanceledException.
      Task<string?> FetchLyricsAsync(LyricsQuery query, CancellationToken ct = default);
  }
  ```
- `Services/LrclibLyricsProvider.cs` (new, `public sealed class LrclibLyricsProvider : ILyricsProvider`),
  constructor `(HttpClient httpClient, ILogger<LrclibLyricsProvider> logger)`.
- `Services/ISongLyricsService.cs` + `Services/SongLyricsService.cs` (new):
  ```csharp
  public interface ISongLyricsService
  {
      /// Lyrics for the query, or null (offline, no match, over the limit, bound reached, source failure).
      /// Never throws except OperationCanceledException when the caller's token is cancelled.
      Task<string?> FindLyricsAsync(LyricsQuery query, CancellationToken ct = default);
  }
  ```
  Constructor `(ILyricsProvider provider, INetworkStatus networkStatus, ILogger<SongLyricsService> logger)`.
  Steps: blank `Title` or `ArtistName` → null; `!networkStatus.HasInternetAccess` → null (no call);
  linked CTS with `CancelAfter(LyricsLookupTimeoutMilliseconds)`; call the provider with the full query;
  null and (`AlbumTitle` or `DurationSeconds` present) → call again with `query with { AlbumTitle = null, DurationSeconds = null }`;
  result trimmed; blank or longer than `LyricsMaxLength` → null. Caller cancellation is rethrown; the
  bound and every other exception are caught, logged at Warning and returned as null (allowed pattern:
  translate an expected failure at a boundary, as `SongSuggestionService.TryFetchDeezerAsync`).
- DI (`MyVocaList/Extensions/ServiceCollectionExtensions.cs`, inside `AddAppServices`, beside the Deezer client):
  ```csharp
  services.AddHttpClient<LrclibLyricsProvider>(client =>
  {
      client.BaseAddress = new Uri("https://lrclib.net/");
      client.DefaultRequestHeaders.UserAgent.ParseAdd("MyVocaList/1.0 (https://github.com/heldercsousa/myvocalist)");
  });
  services.AddScoped<ILyricsProvider>(sp => sp.GetRequiredService<LrclibLyricsProvider>());
  services.AddScoped<ISongLyricsService, SongLyricsService>();
  ```
  The interface is resolved through the typed-client factory, as for Deezer (BUG-082); never `AddScoped<ILyricsProvider, LrclibLyricsProvider>()`.

### 2.4 Integration contract: LRCLIB

| Item | Value |
|------|-------|
| Request | `GET api/get?track_name={title}&artist_name={artist}[&album_name={album}][&duration={seconds}]`, values escaped with `Uri.EscapeDataString`; `duration` sent only when 1–3600 |
| 200 | JSON object; fields read: `plainLyrics` (string or null), `instrumental` (bool). `instrumental == true` or blank `plainLyrics` → provider returns null |
| 404 | body `{"name":"TrackNotFound",…}` → provider returns null |
| Any other status (429, 5xx, …) | provider throws `HttpRequestException` |
| Rate limit | about 90 requests/min per IP for an identified client; the app sends at most 2 per pick |
| Key | none |

### 2.5 ViewModel (`SongFormViewModel`)

Constructor gains `ISongLyricsService songLyricsService` (last parameter).

- State: `CancellationTokenSource? _lyricsLookupCts`, `string? _pickFilledLyrics`,
  `[ObservableProperty] bool _isLyricsLoading`, `public Task LyricsLookupTask { get; private set; } = Task.CompletedTask;`
  (lets a test await the lookup; the pick itself does not await it).
- `public string LyricsPlaceholder => IsLyricsLoading ? "Looking for lyrics…" : "Song lyrics (optional)";`
  notified when `IsLyricsLoading` changes.
- `ApplyRemotePick(dto)` calls `StartLyricsLookup(dto)` on **every** path, including the early return taken
  when `dto.LocalArtistId` has a value (call it before the artist branch, or restructure so both branches
  reach it). Every pick that reaches `ApplyRemotePick` starts a lookup, with a null duration when the row has none:
  1. `CancelLyricsLookup()`.
  2. If `Lyrics` equals `_pickFilledLyrics` (ordinal) and `_pickFilledLyrics` is not null → `Lyrics = null` (REQ-LYR-12). `_pickFilledLyrics = null`.
  3. If `Lyrics` is not blank → return (REQ-LYR-04).
  4. New CTS; `IsLyricsLoading = true`; `LyricsLookupTask = RunLyricsLookupAsync(query, cts)` with
     `query = new LyricsQuery(dto.Title, dto.ArtistName, dto.AlbumTitle, dto.DurationSeconds)`.
- `RunLyricsLookupAsync(query, cts)`: awaits `FindLyricsAsync(query, cts.Token)`; `OperationCanceledException`
  is swallowed (a cancelled lookup is the expected outcome of REQ-LYR-10). The continuation is marshalled with
  the UI-thread helper the ViewModel already uses for its search notes (inline in unit tests). Inside it,
  **everything is guarded by** `ReferenceEquals(_lyricsLookupCts, cts) && !cts.IsCancellationRequested`:
  when the guard holds, `IsLyricsLoading = false`, and if the result is not null and `Lyrics` is blank →
  `Lyrics = result; _pickFilledLyrics = result;`. When the guard fails (stale answer) nothing is touched,
  so a newer lookup keeps its hint.
- `CancelLyricsLookup()`: cancels, disposes and nulls `_lyricsLookupCts`; `IsLyricsLoading = false`.
- `ClearTitle` and `ClearArtist`: `CancelLyricsLookup()`; if `Lyrics` equals `_pickFilledLyrics` (ordinal,
  not null) → `Lyrics = null`; `_pickFilledLyrics = null` (REQ-LYR-11). These comparisons guard which
  pending value is dropped; they are not search or de-duplication.
- Save: `CancelLyricsLookup()` is the first statement of the Save command handler (the method the Save button
  runs, before validation). The lookup is not restarted when Save does not complete (REQ-LYR-10).
- Edit mode and local picks never reach `ApplyRemotePick`, so REQ-LYR-14 holds by construction.

### 2.6 Page

`SongFormPage.xaml`: the Lyrics `Editor` `Placeholder` becomes `{Binding LyricsPlaceholder}`. Nothing else changes.

## 3. Interaction flows

1. Add mode → user types a title → picks a Deezer row → Title, Artist, Version and pictures fill as today →
   placeholder reads "Looking for lyrics…" → answer within 5 s → Lyrics shows the text (or stays empty).
2. During the lookup the user pastes lyrics → the answer is ignored.
3. User taps X on Title → lookup cancelled; untouched pick-filled lyrics are removed.

## 4. Invariants and postconditions

- A lyrics lookup never writes to the database and never changes a non-blank Lyrics field.
- At most one lookup is active per form; a cancelled lookup never changes the form.
- `SongLyricsService.FindLyricsAsync` returns null or a trimmed text of 1–10,000 characters.
- No exception from the lyrics source reaches the ViewModel.

## 5. Governed components

None changed. The Lyrics field is the stock `Editor` the page already uses; this change only binds its
`Placeholder`. Replacing it with a DevExpress editor is not part of this change.

## 6. Testing (risk tiers per `testing.md`)

| Area | Level | Tests |
|------|-------|-------|
| `LrclibLyricsProvider` | A | stub `HttpMessageHandler`: request URL and escaping, `duration` omitted when null or out of range, 200 with lyrics, 200 instrumental, 200 blank, 404 → null, 500 → throws, `User-Agent` set by DI (REQ-LYR-01, 03, 15) |
| `SongLyricsService` | A | Moq provider + `INetworkStatus`: offline no call, match trimmed, over 10,000 → null, exactly 10,000 kept, retry only after null with album/duration, no retry for a bare query, provider throws → null, bound → null, the bound covers both attempts (the second attempt runs under the same linked token), caller cancel rethrown (REQ-LYR-02, 03, 05, 06, 07, 08) |
| `DeezerProvider` / `SongSuggestionService` | B | `duration` mapped to `DurationSeconds`; remote rows carry it; local rows null (REQ-LYR-01) |
| `SongFormViewModel` | A | REQ-LYR-01 (a pick with and a pick without `LocalArtistId` each start one lookup carrying title, artist, album, duration), 02, 03, 04 (non-blank at pick; typed during the lookup; typed then emptied is filled), 09 (exact strings "Looking for lyrics…" / "Song lyrics (optional)"), 10 (new pick, X on Title, X on Artist, Save; a late answer of pick 1 after pick 2 started changes neither Lyrics nor the hint), 11, 12, 13 (no `ISongService` save call during a lookup), 14 (a local pick navigates and starts no lookup) |
| DI | B | `ILyricsProvider` resolves with base address and `User-Agent` (REQ-LYR-15) |
| `SongFormPage.xaml` | C | no unit test; device check L4 |

Every test carries `// [AC] REQ-LYR-NN: …`. Existing tests are not edited. **Planned exceptions:** the
`new SongFormViewModel(...)` calls in `SongFormViewModelTests.cs`, `SongFormViewModelImageTests.cs` and
`SongFormViewModelPictureTests.cs` and `Integration/Services/SongSuggestionTitleSearchRealStackTests.cs` gain
the new constructor argument (a `Mock<ISongLyricsService>` whose default returns null). `SelectTitle_RemoteOption_LeavesFeaturedLyricsAndKaraokeUrlsAndWritesNothing`
stays as written and stays green (no match → Lyrics unchanged). If any other existing test fails, the
task stops and reports.
