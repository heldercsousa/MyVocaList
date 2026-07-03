# MyVocaList — Master Emulator Test List
**Date Created:** 2026-07-03  
**Test Plan Coordinator:** Claude Code  
**Guideline:** Follow Form Validation Standard + E2E patterns from CLAUDE.md rules

---

## Overview

This is a consolidated test list for ALL pending emulator smoke tests. Each test entry includes:
- **Test ID** — unique identifier for tracking
- **Feature/Bug** — which component is being tested
- **Test Steps** — detailed procedure to execute
- **Expected Result** — what should happen on success
- **Source File** — where to update the status after passing
- **Status** — ✅ (done) / ⏳ (pending) / ❌ (failed)

**Guidelines for E2E Testing:**
1. Clear the emulator app data or use a fresh build
2. Build Release APK or debug APK as indicated
3. Test the golden path first, then edge cases
4. Check logcat for errors (especially `Glide FileNotFoundException`, `SecureStorage`, `BottomSheet` warnings)
5. Document any failures with screenshots/logcat
6. Update source files with completion status

---

## CRITICAL PATH: Phase 16C.1 + Song Import Wave 5.2 (ONE SESSION)

### TEST-001: Phase 16C.1 — Artists & Songs Full Feature Smoke Test
**Source File:** `Docs/Management/BusinessFeatures/artists-songs/tasks.md` (Phase 16C.1, lines 235–245)

**Test Steps:**
```
1. Open app → navigate to Artists menu item
2. Verify: Single "Artists" menu entry (not "Authors"/"Performers" duplicates)
3. Verify: Filter chips (Author/Performer) render correctly below AppBar
4. TAP "Register Artist" (FAB)
   - Enter artist name "Test Artist 1"
   - Leave optional fields blank
   - Tap Save
   - Verify: no errors, snackbar confirmation, artist appears in list
5. Register a 2nd artist "Test Artist 2" (repeat step 4)

**16C.1 errors**: 
16C.1 - 1) trailing button from ArtistsPage doesn't execute action when tapped. Prior bug was registered and is marked as done, despite it remains not working.
16C.1 - 2) artistformpage has a search right above the name entry. 
16C.1 - 	2.1) There ir a leading and trailling icons that are confusing. Also
16C.1 - 	2.2) It's not clear if it will search local song's data and/or 3rd API's database. I'm not sure, but I believe specs predicts searching 3rd APIs in addition. It sounds like a duplicated behavior with the songformpage. Maybe it's case to evaluate its removal. 
16C.1 - 	2.3) When tapped, opens a page for searching Artists, not songs. So, it's inconsistent.
16C.1 - 3) i'm not sure, but I guess specs predicts that whenever typed in ArtistFormPage Name entry, an autocomplete feature should appear to allow picking a artist name retrieved from 3rd api

6. Navigate to Songs menu item (flyout or trailing button) 
   - Verify: global Songs page, no artist requirement
   - Verify: back arrow shows (not hamburger)

7. TAP "Add Song" (FAB) → register song without artist pre-selected
   - Title: "Test Song A"
   - No artist selected
   - Verify: "Artist field required" inline error appears 
   - Select "Test Artist 1" from dropdown
   - Verify: inline error clears
   - Tap Save
   - Verify: song created, no duplicate error (title is unique globally)
**7 errors**: 
7 - Bug 1) "Verify: "Artist field required" inline error appears " doens't happen
7 - Bug 2) "Select "Test Artist 1" from dropdown" - neither on bug 1 nor while typing something in Artists entry, there is a dropdown
7 - Bug 3) when I type and Artist string and leave the entry, it clears the entry. I supose that in spec there is a predicted behavior to leave the entry full filled in a case the typed name does not exist either in local artists DB or in 3rd API. In such case, it would have to consider as a new artist, and should auto create a new artist in the local DB and, then, create/update the song registered in the form hooked to such artist just created.
7 - Bug 4) I supose specs says that, whenever a song title is typed in Title entry, a autocomplete should appear to pick a song record brought from 3rd API.
7 - Bug 5) there is a link to search music database. I'm not sure if it's predicted in specs. It also looks like duplicated if there is autocomplete in Title entry
7 - Bugs summary: Given that artist required field isn't allowed to be informed in current version, it's impossible to register a song. So, testing is blocked.


8. Register 2nd song "Test Song B" with "Test Artist 1" as artist
   - In the Songs list, verify BOTH songs appear (different artists or same artist)
   - Verify song titles are displayed and searchable

9. TEST API SEARCH STRIP (Artist form)
   - Edit "Test Artist 1" → Tap on Name field
   - Below the Name field, verify: Text input "Search music database" + "Search" button
   - Type "Radiohead" → Tap Search button
   - Verify: results list appears (or "No results" message if API fails)
   - Select one result from the list
   - Verify: artist name is populated into the Name field
   - Cancel edit (no save)
**9 errors:**
9 - 1) "Type "Radiohead" → Tap Search button". Tapping in leading search button crashs the app. The trailling button works, but no result appears when search page is loaded. Anyhow, see the details registered in error 16C.1 - 3.

10. TEST API SEARCH STRIP (Song form)
    - Edit "Test Song A"
    - Below Title field, verify: Text input "Search music database" + "Search" button
    - Type "Bohemian" → Tap Search
    - Verify: results list appears with Title/Artist/Thumbnail
    - If a result is selected: verify Title and FeaturedArtists are pre-filled
    - Cancel edit (no save)
**10 errors:**
10 - 1) Edit song blocked due to artists entry bug, avoiding add new song. Test done in add new mode. "Type "Radiohead" → 

11. CATALOG OPERATIONS
    - Open "Test Artist 1" via the trailing "queue_music_outlined" button
    - Verify: SongsPage opens in CATALOG MODE (only songs by Test Artist 1)
    - Verify: AppBar title shows artist name (not "Songs")
    - Tap "Add" (FAB) → SongPickerPage opens
    - Select "Test Song B" (from global, even if artist differs)
    - Verify: song is added to artist's catalog
    - Verify: page returns to catalog view, song appears in list
**11 errors:**
11 - 1) See error 16C.1 - 1

12. DELETE OPERATIONS (data integrity)
    - From Catalog view, remove "Test Song B" from the catalog
    - Verify: song disappears from artist's catalog
    - Verify: "Test Song B" still appears in global Songs list
    - Delete "Test Artist 1" from Artists list (swipe or select + trash)
    - Verify: artist is gone
    - Verify: both songs ("Test Song A", "Test Song B") still exist in Songs list
    - Delete "Test Song A" from global Songs list
    - Verify: song is gone from Songs list AND from all catalogs

12. EDIT OPERATIONS (Lyrics field)
    - Create a new song "Test Song C" with Lyrics: "Here goes the lyrics text"
    - Edit the song
    - Verify: Lyrics field is visible with the saved text
    - Edit the lyrics to "Updated lyrics here"
    - Tap Save
    - Close and re-open the song
    - Verify: Lyrics field still contains "Updated lyrics here" (not wiped)
```

**Expected Result:**  
✅ All steps complete without errors. Songs are searchable, artists can be linked to songs, API search integrations work, and data persistence is verified across edit/delete/catalog operations.

**Acceptance Criteria (BACKLOG.md):**
- Single Artists menu item navigates to ArtistsPage; filter chips work
- Register artist; verify no songs required
- Register song from global Songs page; verify title uniqueness
- API search strip works on ArtistFormPage and SongFormPage
- Add song to artist's Catalog via trailing button → Catalog page → FAB picker
- Remove song from Catalog; verify song still exists in global list
- Delete artist; verify songs not deleted; Catalog entries gone
- Delete song; verify it disappears from all Catalogs
- Edit song; verify Lyrics field visible and saveable
- Verify Songs menu item in flyout; search back arrow shows correctly

**Status:** ⏳ PENDING

---

### TEST-002: BUG-023 — SongForm BottomSheet State Sync (Resolution & Merge Sheets)
**Source File:** `Docs/Management/BusinessFeatures/artists-songs/bugs/BUG-023-songform-bottomsheet-broken/BUG-023-songform-bottomsheet-broken.md` (lines 33–42)

**Test Steps:**
```
1. Open Songs → Tap "Add Song"
2. Enter Title = an EXACT match of an existing song for the selected artist
   Example: create "Song X" for "Artist A", then edit/create another "Song X" for "Artist A"
3. Fill other required fields (artist, etc.)
4. Tap Save

5. EXPECTED: Resolution BottomSheet slides up from bottom (half-expanded)
   - Shows the matching candidate(s)
   - Shows "Save as new version" option / button

6. TAP: "Select" button on the candidate
   - EXPECTED: BottomSheet closes
   - EXPECTED: snackbar confirmation appears (success message)
   - EXPECTED: form clears or returns to list

7. REPEAT steps 1–4, but this time:
   - After BottomSheet opens (step 5), tap "Cancel"
   - EXPECTED: BottomSheet closes
   - EXPECTED: form returns to editable state (no data loss)
   - EXPECTED: Title, Artist, Lyrics still populated (no silent clear)

8. IF "Merge" scenario exists (song has manual edits + field diffs):
   - Follow the same open → action → close flow
   - EXPECTED: Merge BottomSheet opens instead of Resolution
   - EXPECTED: "Apply Selected Changes" closes sheet and merges
   - EXPECTED: "Cancel" closes sheet without merging
```

**Expected Result:**  
✅ BottomSheet opens on exact match. User can select resolution action or cancel. No silent data loss. Sheet closes cleanly with no animation lag or repeat.

**Visual/Logcat Check:**
- ❌ NO `DevExpress.Maui.Controls.BottomSheet is already a child...` errors
- ❌ NO animation stutter or Davey burst when opening/closing sheet

**Status:** ⏳ PENDING

---

### TEST-003: BUG-024 — SongForm Edit Mode Data Integrity (FeaturedArtists + Lyrics + Version)
**Source File:** `Docs/Management/BusinessFeatures/artists-songs/bugs/BUG-024-songform-edit-data-loss/BUG-024-songform-edit-data-loss.md` (lines 10–15)

**Test Steps:**
```
1. Create a song with:
   - Title: "Full Data Song"
   - Artist: any artist
   - FeaturedArtists: "Artist B, Artist C"
   - Lyrics: "Verse 1: Here are the lyrics..."
   - Version: (leave empty or set to "Original")

2. Close the form (save completes)

3. EDIT the song:
   - Tap Songs list → select the song → Tap Edit (or long-press)
   - EXPECTED: Form loads with ALL fields hydrated:
     * Title: "Full Data Song"
     * Artist: original artist
     * FeaturedArtists: "Artist B, Artist C" (NOT empty)
     * Lyrics: "Verse 1: Here are the lyrics..." (NOT empty)
     * Version: "Original" (if set; not empty)

4. Edit only the Title: "Full Data Song" → "Updated Title"

5. Tap Save

6. Close and re-open the song:
   - EXPECTED: Title is now "Updated Title"
   - EXPECTED: FeaturedArtists: "Artist B, Artist C" (still there)
   - EXPECTED: Lyrics: "Verse 1: Here are the lyrics..." (still there)
   - EXPECTED: Version: "Original" (still there)
   - ✅ NO SILENT DATA LOSS

7. ALTERNATE: Edit the Version field instead
   - Change Version from "Original" → "Acoustic"
   - Tap Save
   - Re-open
   - EXPECTED: Version now shows "Acoustic" (not ignored/lost)
```

**Expected Result:**  
✅ All fields (FeaturedArtists, Lyrics, Version) persist through edit cycles. No field is silently wiped.

**Status:** ⏳ PENDING

---

## FORM VALIDATION TESTS (E2E Verification)

### TEST-004: Venue Form Validation
**Source File:** `Docs/Management/BusinessFeatures/venues/form-validation-task-log.md`

**Test Steps (same pattern for all form tests):**
```
1. TAP "Register Venue" / Edit existing venue

2. **Tab through Venue Name field WITHOUT typing:**
   - EXPECTED: No error appears (pristine field, R8)

3. **Clear the Name field and Blur (Tab away or touch elsewhere):**
   - EXPECTED: Error inline under Name field: "Venue name is required"
   - EXPECTED: NO snackbar, NO dialog (R6, R9)

4. **With error showing, type a valid name (e.g., "Test Venue"):**
   - EXPECTED: Error clears immediately as you type (no need to blur, R2)
   - EXPECTED: No validation on keystroke before the error was triggered (R3)

5. **Clear the name, type 31+ characters, then Blur:**
   - EXPECTED: Error shows "Name is too long. Maximum 30 characters."
   - EXPECTED: Character counter shows >30 (red/warning color)

6. **Tap Save with an error showing:**
   - EXPECTED: Save does NOT proceed; error stays inline (R4, R6)
   - EXPECTED: Service is never called

7. **Fix the error, Tap Save:**
   - EXPECTED: Success snackbar appears
   - EXPECTED: New venue is created (or existing updated)

8. **Edit mode dirty-guard:**
   - Open an existing venue with a long/invalid name in legacy data
   - EXPECTED: No error flashes on page load (error only appears after user edits that field)
```

**Expected Result:**  
✅ Blur-first validation, keystroke-clear, Save safety-net, inline errors only (no dialogs/snackbars as validation channels).

**Status:** ⏳ PENDING

**TEST-004 - errors:** 
TEST-004 - 1) Step 5 above shows correctly the error message, but the character counter become duplicated (it becomes duplicated once reaching 26 chars typed). Evidences found at `Docs\Management\BusinessFeatures\venues\bugs\validation-error-26chars.jpg` and `Docs\Management\BusinessFeatures\venues\bugs\validation-error-31morechars.jpg`
TEST-004 - 2) Step 8 wasn't done once there isn't such record with more than 30 chars in DB. We must abandon this step.
---

### TEST-005: Singer (Person) Form Validation
**Source File:** `Docs/Management/BusinessFeatures/persons/form-validation-task-log.md` (lines 97–104)

**Test Steps:**
```
Apply the same 8 steps from TEST-004 to the Singer form, testing three fields:
- **Name:** required, max 200 chars
- **Birthday:** DD/MM format, validated by existing PersonService.ValidateBirthday
- **Email:** format + uniqueness check

1. Tab through each field without typing → no error (R8)
2. For each field: clear → blur → error inline (R1)
3. With error showing: type valid value → clears immediately (R2)
4. Type invalid then wait (no blur) → no error (R3)
5. Save with error → error stays, save blocked (R4)
6. Fix all errors → Save → success
7. Edit existing Singer (legacy data) → no error flash on load (dirty-guard)
8. SPECIAL: Email uniqueness error
   - Enter an email already used by another singer
   - Blur → EXPECTED: "Email already registered to another singer." inline
   - Edit to a unique email → error clears (keystroke re-validation)
   - Save → success
```

**Expected Result:**  
✅ 30/30 `PersonFormViewModelTests` green (12 pre-existing + 18 new per task-log).

**Verification Note:** Task-log states "DONE 2026-07-01, PASSED" for emulator E2E.

**Status:** ✅ DONE (Helder completed 2026-07-01) - restested 2026-07-03

**TEST-005 - errors:** 
Just a detail about the test-005 guideline above: "Apply the same 8 steps from TEST-004 to the Singer form, testing three fields:". It does not makes sense other than for Singer Name entry. Only some tests are sduitable for the other 2 entries.
TEST-005 - 1) Edit singer load page has a UI trouble in the full name entry as shown in the image `Docs\Management\BusinessFeatures\persons\bugs\edit-singer-load-page-issue.jpg`
TEST-005 - 2) There is a validation error that looks like it is expecting the slash within the string, but the mask shall not persist/deliver slash together the date/month. Probably validation service must expect only 4 chars number only. Evidence at `Docs\Management\BusinessFeatures\persons\bugs\edit-singer-load-page-issue.jpg\singer-bithday-validation-error.jpg`
TEST-005 - 3) When editing, after save, navigation to prior page is expected, as happens in Venues. Confirm it's the pattern for CRUDs. Singer form doesn't navigate after save, showing succes message correctly.
TEST-005 - 4) Email uniqueness error doesn't appears when entry is blured but only after Save tapped.
---

### TEST-006: Song Form Validation (Title + Version)
**Source File:** `Docs/Management/BusinessFeatures/artists-songs/form-validation-task-log.md` (lines 139–146)

**Test Steps:**
```
1. **Title field (required, max 100 chars):**
   - Tab through empty Title → no error (R8)
   - Clear Title → Blur → "Title is required" (R1)
   - With error showing, type valid title → clears immediately (R2)
   - Type invalid BEFORE error showing → no validation (R3)
   - Save with invalid Title → error shows, Save blocked (R4)
   - Fix Title → Save → success

2. **Version field (optional in main form, max 60 chars):**
   - Leave empty and blur → no error (it's optional, R1)
   - Type 61+ chars → Blur → "Version too long. Max 60 chars." (R1)
   - With error showing, shorten to ≤60 → clears immediately (R2)
   - Save with invalid Version → error shows, Save blocked (R4)
   - Fix Version → Save → success

3. **"Save as new version" flow (Version required in this context):**
   - Open Resolution BottomSheet (by entering exact match title)
   - Tap "Save as new version"
   - Leave Version empty → Blur or tap Confirm
   - EXPECTED: "Version is required for saving as a new version" inline (same R1 field)
   - Type a version → error clears
   - Tap "Save as new version" → success

4. **Edit mode hydration (no error on load):**
   - Edit existing song with empty Version field
   - EXPECTED: no error flashes on page load (even though Version is empty)
   - User edits Version → validation applies (dirty-guard)

5. **Title-only uniqueness check:**
   - Create Song "A" with Artist "X"
   - Try to create another Song "A" with Artist "X"
   - EXPECTED: Save encounters exact match → Resolution BottomSheet opens (title uniqueness is per-artist, not global)
```

**Expected Result:**  
✅ 403/403 tests passing (386 baseline + 17 new). Blur/keystroke/Save validation patterns confirmed.

**Status:** ⏳ PENDING

---

### TEST-007: Artist Form Validation (Name field only)
**Source File:** `Docs/Management/BusinessFeatures/artists-songs/form-validation-task-log.md` (lines 278–286)

**Test Steps:**
```
1. **Artist Name field (required, max 60 chars, unique):**
   - Tab through pristine Name → no error (R8)
   - Clear Name → Blur → "Artist name is required" (R1)
   - With error, type valid name → clears immediately (R2)
   - Type BEFORE error → no validation (R3)
   - Save with invalid → error shows, Save blocked (R4)

2. **Counter alignment (60 char limit):**
   - Type 60 characters exactly
   - EXPECTED: counter shows "60/60" WITHOUT error color
   - Type 1 more char (61) → input is capped (MaxCharacterCount=60)
   - EXPECTED: counter shows "60/60" WITH error color

3. **Trimming (trailing spaces don't count):**
   - Type "Artist Name    " (with trailing spaces)
   - EXPECTED: counter shows "11/60" (spaces trimmed)

4. **Duplicate name check:**
   - Create Artist "Test A"
   - Try to create another Artist "Test A"
   - Save → error inline: "An artist with this name already exists"
   - Change name to "Test A2" → Save → success

5. **Edit mode (no error on load):**
   - Edit existing artist
   - EXPECTED: no error flashes on page load
   - User edits Name → validation applies

6. **Music database picker:**
   - Tap "Search music database" button
   - Type "The Beatles" → Tap Search
   - Select from results → Name field is populated
   - EXPECTED: no spurious error appears from pre-fill

7. **Duplicate suggestions (picker):**
   - Tap "Duplicate suggestions" or picker icon
   - Select a suggestion → Name is populated
   - EXPECTED: no error from picker selection
```

**Expected Result:**  
✅ 416/416 tests passing (403 baseline + 13 new). Counter/validation/duplicate patterns confirmed.

**Status:** ⏳ PENDING

**errors:**
Test - 007 - 1: The very same trouble found in venues (TEST-004 - 1) happens in the artist name entry. Evidence at `Docs\Management\BusinessFeatures\artists-songs\bugs\artistis-validation-error-charcount-duplicated-01.jpg`

---

## BUG VERIFICATION TESTS

### TEST-008: BUG-017 — navigate_next Icon Missing SVG (Glide FileNotFoundException)
**Source File:** `Docs/Management/BusinessFeatures/artists-songs/bugs/BUG-017-artistscrud-emulator-debug-often-stops/` (fixed; no longer appears in main BACKLOG)

**Test Steps:**
```
1. Open Artists or Songs pages (forms)
2. Tap "Edit" on any item
3. Look for the "navigate_next" icon placeholder in the form
4. Check logcat for Glide errors:
   - ❌ NO "Glide FileNotFoundException navigate_next" messages
   - ✅ Icon renders cleanly (replaced with arrow_forward_outlined)
```

**Expected Result:**  
✅ No Glide errors. Icon renders without crashing the debugger.

**Status:** ⏳ PENDING (visual confirmation needed)

---

### TEST-009: BUG-019 — ArtistsPage List Item Button Noop (DataTemplate Cast Failure)
**Source File:** `Docs/Management/BusinessFeatures/artists-songs/bugs/BUG-019-artistspage-listitem-button-noop/`

**Test Steps:**
```
1. Open Artists page
2. Look at each artist in the list
3. EXPECTED: Artist name is visible (not null/blank)
4. Tap the trailing "queue_music_outlined" button on any artist
5. EXPECTED: Navigates to Songs/Catalog page for that artist (not a noop)
6. EXPECTED: Artist name appears in the page title or subtitle
```

**Expected Result:**  
✅ Artist names are visible. Trailing button navigates to catalog.

**Status:** ⏳ PENDING

---

### TEST-010: BUG-020 — SongsPage FAB Crash (SecureStorage.GetAsync Unguarded)
**Source File:** `Docs/Management/BusinessFeatures/artists-songs/bugs/BUG-020-songspage-fab-crash-secure-storage.md`

**Test Steps:**
```
1. Open Songs page
2. Tap "Add Song" FAB
3. EXPECTED: SongFormPage opens (form loads)
4. Check logcat:
   - ❌ NO "SecureStorage" exceptions
   - ❌ NO app crash
   - ✅ Form is responsive and interactive
5. Fill in song fields and Save (or Cancel)
6. Verify no "app terminated" condition in Android
```

**Expected Result:**  
✅ FAB opens SongFormPage. No SecureStorage crashes. App remains stable.

**Status:** ⏳ PENDING

---

### TEST-011: BUG-021 — SongsPage FAB Crash (ISimilarityScorer DI Missing)
**Source File:** `Docs/Management/BusinessFeatures/artists-songs/bugs/BUG-021-songspage-fab-crash/`

**Test Steps:**
```
1. Open Songs page
2. Tap "Add Song" FAB
3. EXPECTED: SongFormPage opens (no crash)
4. Check logcat:
   - ❌ NO "Unable to resolve service for type 'ISimilarityScorer'" errors
   - ❌ NO DependencyInjectionException
5. Form is responsive and interactive
```

**Expected Result:**  
✅ FAB opens SongFormPage. DI resolution succeeds. No crash.

**Status:** ⏳ PENDING

---

### TEST-012: BUG-011 — QueuePage BottomSheet Double-Add / Davey Burst
**Source File:** `Docs/Management/BusinessFeatures/queue-management/bugs/BUG-011-queuepage-bottomsheet-double-add.md` (lines 20–23)

**Test Steps:**
```
1. Navigate to Queue page (from main menu or flyout)
2. Wait for page to fully render
3. Check logcat:
   - ❌ NO "BottomSheet is already a child of Grid" errors

4. Navigate AWAY from Queue (tap another menu item)
5. Navigate BACK to Queue page
6. Check logcat again:
   - ❌ NO "BottomSheet is already a child" errors

7. Watch the frame rate:
   - ✅ No Davey burst (≤16ms per frame during nav)
   - ❌ NO "Choreographer Skipped" warnings

8. Tap "Finish" button on an active queue event:
   - EXPECTED: Confirmation BottomSheet opens
   - Tap "Confirm" → event marked FINISHED
   - OR Tap "Cancel" → BottomSheet closes, event unchanged

9. Repeat steps 4–8 (navigate away and back)
   - Confirm behavior is consistent, no lag
```

**Expected Result:**  
✅ Double-navigation produces no BottomSheet re-attach error. No UI jank. Confirmation sheet opens/closes cleanly.

**AC-011 Acceptance Criteria:**
- AC-BUG011-1: No `BottomSheet is already a child` logcat warning on 2nd navigation
- AC-BUG011-2: 2nd navigation causes no Davey burst (≤16ms per frame)
- AC-BUG011-3: Queue functionality (BottomSheet open/close, queue interaction) unaffected

**Status:** ⏳ PENDING

---

## SESSION CONTINUITY — LIVE DEMO (MANUAL GATE)

### TEST-013: Session Continuity Lease — Two-Terminal Live Demo
**Source File:** `Docs/Management/DevCycleCraft/session-continuity-leasing/demo-and-traceability.md` (lines 10–40)

**IMPORTANT:** This test requires two Claude Code CLI terminals open simultaneously. NOT an emulator test — a session-management test.

**Test Steps:**
```
PREREQUISITE: Two terminals, A and B, both in the MyVocaList repo root.

1. **TERMINAL A — Claim and work:**
   - Start a new session in Terminal A
   - Dispatch a small task (e.g., add a test to a single file)
   - Mark the task [~] in tasks.md
   - Set a resume pointer: `python .claude/scripts/lease/resume.py --set <A_sid> "Finished first assertion, next: cleanup"`
   - Perform 1–2 tool calls (Edit file, confirm)
   - Status: Heartbeat hook should fire, writing `.claude/leases/<A_sid>.json`

2. **TERMINAL B — See A fresh, pick different work:**
   - Start new session in Terminal B
   - Classify A's claim: `python .claude/scripts/lease/reclaim.py <B_sid> <A_sid>`
   - EXPECTED OUTPUT: **fresh** (A's last_active is within 30min TTL, A's pid is alive)
   - B does NOT take A's task; B selects a DIFFERENT available [ ] task
   - B starts work on its own task

3. **TERMINAL A — Interrupted (clear session):**
   - In Terminal A, type `/clear` to end the session
   - `session_id` retires; no more heartbeat updates
   - `.claude/leases/<A_sid>.json` stops advancing

4. **Wait for stale condition (fast-track on same host):**
   - Terminal B re-checks: `python .claude/scripts/lease/reclaim.py <B_sid> <A_sid>`
   - If enough time has passed (>TTL 1800s) OR if A's recorded pid is dead:
     - EXPECTED OUTPUT: **reclaimed** (B becomes new owner, but A's resume_pointer is preserved)
   - If A is still fresh (< TTL and pid alive):
     - Wait 30 min, or manually test with an OLD claim:
     - Create a stale claim file (2h old, dead pid 1) and test `reclaim.py` against it

5. **TERMINAL B — Resume A's work (no arbitration):**
   - B reads A's resume pointer: `python .claude/scripts/lease/resume.py <A_sid>`
   - EXPECTED OUTPUT: (example from demo-traceability.md line 123)
     ```
     RESUME POINTER: Finished first assertion, next: cleanup
     LAST COMMIT: <commit hash> <message>
     NEXT: read the active feature tasks.md, find the [~] step, and continue from the pointer.
     ```
   - B reads the pointer, opens tasks.md, finds the [~] task, and continues from that exact line
   - B completes the rest of the task (cleanup, final assertion, commit)
   - **NO Helder arbitration or manual context transfer needed**

6. **Verification:**
   - At NO point during steps 1–5 was there an attempt to acquire the same task lock
   - At NO point did a human need to mediate which terminal should work on what
   - A's resume_pointer survived the reclaim and allowed B to continue exactly where A left off
```

**Expected Result:**  
✅ Terminal B can:
1. Detect A's claim as fresh and avoid collision (step 2)
2. Detect A's claim as stale and reclaim ownership (step 4)
3. Read A's resume pointer and continue work seamlessly (step 5)

**Critical ACs:**
- AC-1.1: When A claims fresh, B picks a different task (no collision)
- AC-1.3: B does not wait for A; B starts work immediately on a different unit
- AC-2.2: Fast-path reclaim on same host if A's pid is dead
- AC-2.3: Owner/pid/last_active overwritten on reclaim; resume_pointer preserved
- AC-3.1: Heartbeat hook fires on every tool call (PostToolUse), updating last_active
- AC-3.2: `/clear` stops the heartbeat (claim stops advancing)
- AC-4.2: Resume pointer preserved through reclaim, allowing continuation
- AC-4.3: Resume pointer can be set and read programmatically

**Status:** ⏳ PENDING (Helder manual gate — live demo required)

---

## INTEGRATION & REGRESSION TESTS (Automated, for reference)

### TEST-014: Build & Test Suite
**Command:** 
```bash
dotnet build MyVocaList.sln
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj
```

**Expected Result:**
- ✅ Build: 0 errors (warnings from DevExpress trial license are pre-existing)
- ✅ Tests: All passing
  - Phase 16C: 157/157+ tests
  - Form Validation (Venue/Person/Song/Artist): 416/416+ tests
  - BUG-023/024 regression guards included
  - BUG-020/021 guards included

**Status:** ⏳ PENDING (run after all emulator tests pass)

---

## TEST EXECUTION CHECKLIST

Use this checklist to track progress:

```markdown
## EMULATOR TESTS

- [ ] TEST-001 — Phase 16C.1 Full Feature Smoke Test
- [ ] TEST-002 — BUG-023 BottomSheet State Sync
- [ ] TEST-003 — BUG-024 Edit Data Integrity
- [ ] TEST-004 — Venue Form Validation
- [ ] TEST-005 — Singer Form Validation (pre-done 2026-07-01)
- [ ] TEST-006 — Song Form Validation
- [ ] TEST-007 — Artist Form Validation
- [ ] TEST-008 — BUG-017 Icon Missing
- [ ] TEST-009 — BUG-019 List Item Button
- [ ] TEST-010 — BUG-020 SecureStorage Crash
- [ ] TEST-011 — BUG-021 DI Resolution
- [ ] TEST-012 — BUG-011 BottomSheet Double-Add
- [ ] TEST-013 — Session Continuity Live Demo (Helder gate)

## POST-TEST

- [ ] TEST-014 — Build & Test Suite (automated)
- [ ] All source files updated with ✅ Done status
- [ ] Screenshots/logcat captured for any failures
- [ ] BACKLOG.md statuses updated to ✅ Done
```

---

## HOW TO UPDATE SOURCE FILES AFTER COMPLETION

After each test passes, update the corresponding source file:

### Pattern 1: BACKLOG.md Updates
Find the feature/bug row (search by BUG-### or feature name) and change status from `⏳ Helder: emulator smoke...` to `✅ Done`.

**File:** `Docs/Management/BACKLOG.md`

**Example:**
```markdown
# Before:
| 2026-06-27 | ↳ BUG-023: SongForm resolution/merge BottomSheets | ⏳ Helder: emulator smoke test required |

# After:
| 2026-06-27 | ↳ BUG-023: SongForm resolution/merge BottomSheets | ✅ Done — verified 2026-07-03 |
```

### Pattern 2: Task-Log Entries
Find the task entry (search by test name or BUG-###) and update the **Status** field and **E2E verification** section.

**Files:**
- `Docs/Management/BusinessFeatures/artists-songs/tasks.md` (Phase 16C.1, line 235)
- `Docs/Management/BusinessFeatures/artists-songs/bugs/BUG-023*/BUG-023*.md`
- `Docs/Management/BusinessFeatures/artists-songs/form-validation-task-log.md`
- `Docs/Management/BusinessFeatures/queue-management/bugs/BUG-011*/`
- etc.

**Example:**
```markdown
# Before:
- [ ] **16C.1** End-to-end smoke test on emulator:
  - ⏳ Helder: See manual E2E steps

# After:
- [x] **16C.1** End-to-end smoke test on emulator:
  - ✅ Done 2026-07-03 — all steps passed, no logcat errors, verified data persistence
```

---

## QUICK REFERENCE: What to Look For in Logcat

**Good Signs (✅ no action needed):**
```
[MAIN] App starts, pages load, buttons respond
[Input] Tap → Navigation/BottomSheet opens smoothly
[Editor] Form fields accept input, validation shows inline
```

**Bad Signs (❌ capture and document):**
```
Glide FileNotFoundException navigate_next
BottomSheet is already a child of Grid
Unable to resolve service for type 'ISimilarityScorer'
SecureStorage exception / KeyStore error
Choreographer Skipped → Davey burst (frame drops)
NullReferenceException / Unhandled exception
```

---

## TEST SESSION NOTES

**Date:** 2026-07-03  
**Coordinator:** Claude Code  
**Tester:** (to be filled by Helder)  
**Device:** (emulator API level, manufacturer, etc.)  
**APK Build:** (Debug / Release, exact version)  
**Session Start Time:**  
**Session End Time:**  

**Issues Found (if any):**
- Issue 1: [description] — Severity [Critical/Major/Minor] — Screenshot/logcat attached
- Issue 2: ...

**Follow-Up Actions:**
- [ ] All tests passed; BACKLOG.md status updated
- [ ] One or more tests failed; issue logged in BACKLOG under "Regression"
- [ ] New bug discovered; register as BUG-### in BACKLOG

---

**END OF MASTER TEST LIST**
