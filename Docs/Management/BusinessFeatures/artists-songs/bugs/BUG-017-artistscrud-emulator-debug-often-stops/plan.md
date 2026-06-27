# Plan: BUG-017 — `navigate_next` icon causes Glide FileNotFoundException on every render

**Severity:** Major
**Feature:** Artists & Songs Catalog
**Registered:** 2026-06-27
**Source:** `artistis-crud-manual-tests-log.txt`

---

## Root Cause

`ArtistFormPage.xaml:72` sets `Icon="navigate_next"` on a DX `ListItem` (artist-picker trigger row). The string `navigate_next` has **no corresponding SVG file in `Resources/Images/`**. DevExpress falls back to treating it as a file-system path and hands it to Glide, which then attempts three load strategies in sequence:

- `InputStream` — fails → `FileNotFoundException: /navigate_next`
- `ParcelFileDescriptor` — fails → `FileNotFoundException: /navigate_next`
- `AssetFileDescriptor` — fails → `FileNotFoundException: /navigate_next`

This produces **3 exceptions per render**, logged at `INFO Glide` level, and visible in the debug logcat as:

```
java.io.FileNotFoundException: /navigate_next
```

The same bug exists in `SongFormPage.xaml:78` and `:171` (two occurrences).

**ANR contribution:** Each Glide failure path involves three synchronous I/O probes on the main thread. Combined with emulator software-rendering overhead, these accumulate into the `HWUI Davey!` frame jank events (2522 ms, 1216 ms, 1700 ms, 3211 ms) and the Choreographer `Skipped N frames` warnings (147, 70, 101, 71 skips) seen in the log. Repeated jank spikes above Android's 5-second threshold trigger the "app stops responding" ANR dialog.

---

## Fix

Replace `Icon="navigate_next"` with `Icon="arrow_forward_outlined"`. The file `arrow_forward_outlined.svg` exists in `Resources/Images/` and is the semantically correct icon for a navigation-forward trigger row.

### Files to change

| File | Line(s) | Change |
|------|---------|--------|
| `MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml` | 72 | `Icon="navigate_next"` → `Icon="arrow_forward_outlined"` |
| `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` | 78, 171 | same replacement (2 occurrences) |

---

## Implementation Steps

1. Replace the three icon usages as listed above.
2. Build (`dotnet build -f net10.0-android`) — confirm 0 errors.
3. Run the app on the emulator, open ArtistFormPage and SongFormPage.
4. Confirm logcat shows **zero** `Glide FileNotFoundException` for `navigate_next`.
5. Confirm the arrow-forward icon renders visually on all three trigger rows.
6. Commit: `fix: replace missing navigate_next icon with arrow_forward_outlined [BUG-017]`

---

## Regression Test

**Severity is Major** — a regression test is mandatory for testable-layer bugs per `bug-tracking.md`. However this bug lives purely in XAML/UI (icon string value); there is no service or repository layer to unit-test. The required verification is the emulator smoke test described above (step 5). Document the manual E2E step in the task-log per `bug-tracking.md § Major`.

---

## Verification

- Logcat: zero `FileNotFoundException` for `navigate_next` after fix
- Visual: arrow-forward icon visible on ArtistFormPage trigger row and on both SongFormPage trigger rows
- ANR dialog: should no longer appear from this root cause during normal CRUD flow
