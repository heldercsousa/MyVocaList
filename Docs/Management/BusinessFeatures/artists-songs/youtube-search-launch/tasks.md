# YouTube Search Launch Button — Implementation Tasks

> **Checkpoint:** Implementation guide — task dependencies organized by layer

---

## Phase 1 — ViewModel Commands

Order: Sequential (each ViewModel depends only on type definitions already in the codebase).

### Task 1.1: Add LaunchYouTubeSearch Command to SongFormViewModel

- [ ] **Implement LaunchYouTubeSearch command** [SEQUENTIAL]
  - **Produces:** `SongFormViewModel.LaunchYouTubeSearchCommand` · `SongFormViewModel.CanLaunchYouTubeSearch` property
  - **Consumes:** `SongFormViewModel` class (existing)
  - **Risk:** Low — isolated ViewModel method, no external dependencies
  - **Files owned:** `MyVocaList/UI/ViewModels/Songs/SongFormViewModel.cs`
  - **Demo:** Construct a test song with title and artist; tap command; verify YouTube URL is formed correctly and launcher is called
  - **Review lane:** Standard

### Task 1.2: Add LaunchYouTubeSearch Command to SongsViewModel

- [ ] **Implement LaunchYouTubeSearch command (with parameter)** [SEQUENTIAL]
  - **Produces:** `SongsViewModel.LaunchYouTubeSearchCommand`
  - **Consumes:** `SongsViewModel` class (existing) · `SongListItemDto` (existing)
  - **Risk:** Low — isolated ViewModel method
  - **Files owned:** `MyVocaList/UI/ViewModels/Songs/SongsViewModel.cs`
  - **Demo:** Unit test: pass a song DTO with title and artist; verify command constructs the correct URL
  - **Review lane:** Standard

### Task 1.3: Add LaunchYouTubeSearch Command to SongPickerViewModel

- [ ] **Implement LaunchYouTubeSearch command (with parameter)** [SEQUENTIAL]
  - **Produces:** `SongPickerViewModel.LaunchYouTubeSearchCommand`
  - **Consumes:** `SongPickerViewModel` class (existing) · `MusicSearchResultDto` (existing)
  - **Risk:** Low — isolated ViewModel method
  - **Files owned:** `MyVocaList/UI/ViewModels/Songs/SongPickerViewModel.cs`
  - **Demo:** Unit test: pass a search result DTO with title and artist; verify command constructs the correct URL
  - **Review lane:** Standard

---

## Phase 2 — XAML Buttons [SEQUENTIAL — waits for Phase 1]

Order: Parallel — all three pages can be updated simultaneously once ViewModel commands exist.

### Task 2.1: Add Launch Button to SongFormPage

- [ ] **Add XAML button + bindings to SongFormPage** [P]
  - **Produces:** Button in YouTube URLs section · binding to `LaunchYouTubeSearchCommand` · binding to `CanLaunchYouTubeSearch`
  - **Consumes:** `SongFormViewModel.LaunchYouTubeSearchCommand` (from Task 1.1)
  - **Risk:** Low — XAML-only change
  - **Files owned:** `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`
  - **Demo:** Open song form; verify button is visible and disabled when title/artist are empty; enabled when both are filled
  - **Review lane:** Standard

### Task 2.2: Add Launch Action to SongsPage List Items

- [ ] **Add trailing action button to SongsPage list item template** [P]
  - **Produces:** Trailing menu button on each song row in `SongsPage`
  - **Consumes:** `SongsViewModel.LaunchYouTubeSearchCommand` (from Task 1.2)
  - **Risk:** Medium — modifies list item template structure (may affect item height or alignment)
  - **Files owned:** `MyVocaList/UI/Pages/Songs/SongsPage.xaml`
  - **Demo:** Smoke test on list page; verify button appears on each row; tap button opens YouTube for that song
  - **Review lane:** Standard

### Task 2.3: Add Launch Action to SongPickerPage List Items

- [ ] **Add trailing action button to SongPickerPage list item template** [P]
  - **Produces:** Trailing menu button on each search result in `SongPickerPage`
  - **Consumes:** `SongPickerViewModel.LaunchYouTubeSearchCommand` (from Task 1.3)
  - **Risk:** Low — picker already has trailing interaction patterns
  - **Files owned:** `MyVocaList/UI/Pages/Songs/SongPickerPage.xaml`
  - **Demo:** Search for a song in picker; verify button appears on results; tap button opens YouTube; picker remains open
  - **Review lane:** Standard

---

## Phase 3 — Testing [Optional for Phase 1–2]

> Tests are recommended but not blocking. TDD rules apply only if services/repositories are modified.

### Task 3.1: Unit Tests — ViewModel Commands

- [ ] **Write tests for URL construction and encoding** [P]
  - **Produces:** Test cases in `MyVocaList.Tests/Unit/ViewModels/LaunchYouTubeSearchCommandTests.cs`
  - **Consumes:** `SongFormViewModel` · `SongsViewModel` · `SongPickerViewModel`
  - **Risk:** Low — unit tests only
  - **Files owned:** `MyVocaList.Tests/Unit/ViewModels/LaunchYouTubeSearchCommandTests.cs`
  - **Demo:** Run `dotnet test` and confirm all tests pass
  - **Review lane:** Standard
  - **AC Coverage:**
    - AC-1.3: Query constructed correctly with title + artist
    - AC-1.4: URL encoding is RFC-compliant
    - AC-1.8: Button disabled when title or artist is missing

---

## Checklist: Implementation Order

1. **Phase 1 (Sequential):** Implement all three ViewModel commands
   - Task 1.1 → Task 1.2 → Task 1.3
2. **Phase 2 (Parallel after Phase 1):** Add XAML buttons to all three pages
   - Tasks 2.1, 2.2, 2.3 can run in parallel
3. **Phase 3 (Optional):** Write unit tests to verify URL construction and encoding

---

## Demo Requirements

### For Phase 1
- Construct a mock song/result object in code
- Call each command and verify:
  - Query string is built correctly: `karaoke <title> <artist>`
  - Query is URL-encoded
  - Launcher URI is well-formed

### For Phase 2
- **SongFormPage:** Open form with title + artist; tap button; YouTube app/browser opens with search
- **SongsPage:** View list; tap menu on a song; YouTube opens with search for that song
- **SongPickerPage:** Search for a song; tap menu on result; YouTube opens; picker remains open

### For Phase 3
- Run tests: `dotnet test --filter "LaunchYouTube"`
- Verify all encoding tests pass

---

## Notes

- **No database changes.** This feature stores no data.
- **No new services.** All logic is ViewModel-level.
- **No migrations.** No EF Core changes needed.
- **MAUI APIs used:** `Launcher.TryOpenAsync` · `Browser.OpenAsync` · `Uri.EscapeDataString`
- **Component governance:** Task 2.2 and 2.3 assume inline trailing actions in list items. If a future refactoring extracts a reusable `CrudListView.TrailingActions` component, these tasks will need re-work.
