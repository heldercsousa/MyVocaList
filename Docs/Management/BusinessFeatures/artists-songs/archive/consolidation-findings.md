# Artists & Songs — Branch Consolidation Findings

> **Date:** 2026-06-19
> **Phase:** 1 — Discover & consolidate (no implementation)
> **Status:** ⏳ Awaiting Helder review before any branch deletion or merge

---

## Branch Map

| Branch | Ahead (develop) | Behind (develop) | Verdict |
|--------|-----------------|------------------|---------|
| `feature/artists-songs` | **1** | **565** | ⚠️ Stale — stray early commit, superseded by develop (see below) |
| `feature/song-import-resolution` | 0 | 61 | ✅ Fully merged into develop |
| `fix/artists-filter-regression` | 0 | 31 | ✅ Fully merged into develop |
| `origin/feature/song-import-resolution` | 0 | 61 | ✅ Fully merged into develop |
| `origin/fix/artists-filter-regression` | 0 | 31 | ✅ Fully merged into develop |

---

## The Single Stray Commit on `feature/artists-songs`

**SHA:** `8e74527`
**Message:** `feat: add SongKaraokeUrl entity and ISongKaraokeUrlRepository interface`
**Files changed (2):**
- `Domain/Entity/SongKaraokeUrl.cs`
- `Domain/RepositoryInterface/ISongKaraokeUrlRepository.cs`

### Verdict: DISCARD — both files superseded by develop

| File | Status in develop | Difference |
|------|-------------------|------------|
| `Domain/RepositoryInterface/ISongKaraokeUrlRepository.cs` | Present — **identical** to stray commit | None |
| `Domain/Entity/SongKaraokeUrl.cs` | Present — **more complete** than stray commit | Develop adds `DurationSeconds?` and `Label?` fields; stray commit is the early draft |

**Conclusion:** Develop already contains everything in `8e74527` and more. This commit was written during early YouTube Karaoke URL work and was later re-created (more completely) on the song-import-resolution branch that is now merged. Nothing would be lost by discarding it.

---

## Recommended Consolidation Steps

No integration branch is needed. Develop is already the single source of truth.

1. **Delete `feature/artists-songs`** — the 1 stray commit is stale and safe to discard.
   - Command: `git branch -d feature/artists-songs` (will fail if not merged — use `-D` intentionally after this review)
   - No cherry-pick needed.

2. **Delete `feature/song-import-resolution`** — 0 commits ahead of develop; fully merged.
   - Command: `git branch -d feature/song-import-resolution`
   - Also delete remote: `git push origin --delete feature/song-import-resolution`

3. **Delete `fix/artists-filter-regression`** — 0 commits ahead of develop; fully merged.
   - Command: `git branch -d fix/artists-filter-regression`
   - Also delete remote: `git push origin --delete fix/artists-filter-regression`

---

## Spec vs Reality — What's Lacking

### Core feature (`artists-songs/tasks.md`)

All phases 1–16B are checked off (complete). **Only Phase 16C remains:**

| Task | Status | Notes |
|------|--------|-------|
| 16C.1 — End-to-end emulator smoke test | ⏳ Pending | 10-step checklist in tasks.md |
| 16C.2 — Build 0 errors | ⏳ Pending | |
| 16C.3 — `/project:review` | ⏳ Pending | |
| 16C.4 — Update changelog | ⏳ Pending | |
| 16C.5 — `/project:commit` | ⏳ Pending | |

> **Note:** Song Import & Entity Resolution (also merged) had its own build/test verification (354 tests pass, 0 build errors). Phase 16C.2 may already be satisfied. Helder should confirm whether 16C.1 (the emulator smoke test) is still required independently of Song Import's own emulator gate.

### Bug rows still showing `📋 Spec` in BACKLOG — need status update

The Song Import & Entity Resolution feature (merged to develop) explicitly states it "Folds in BUG-004/005/006/007/008/009/010." Specifically:

| Bug | BACKLOG Status | Actual Status in develop |
|-----|---------------|--------------------------|
| BUG-004 (BottomSheetTitle style) | ✅ Done (already correct in BACKLOG) | Fixed |
| BUG-005 (New Song Save broken) | 📋 Spec ← **stale** | Fixed by Wave 4B (`dd36b58`) |
| BUG-006 (double-tap crash) | 📋 Spec ← **stale** | Fixed by Wave 4A (`9b37d2a`) |
| BUG-007 (duplicate back arrow) | 📋 Spec ← **stale** | Fixed by Wave 4A (`9b37d2a`) |
| BUG-008 (artist autocomplete) | 📋 Spec ← **stale** | Fixed by Wave 4B (`dd36b58`) |
| BUG-009 (add URL before save) | 📋 Spec ← **stale** | Fixed by Wave 4B (`dd36b58`) |
| BUG-010 (API auto-fill broken) | 📋 Spec ← **stale** | Fixed by Wave 4A (`9b37d2a`) |

These BACKLOG rows need to be updated to `✅ Fixed` in Phase 2.

---

## Open Questions for Helder (confirm before Phase 2)

1. **Delete `feature/artists-songs`?** The 1 stray commit is stale. Confirm it's safe to `git branch -D feature/artists-songs`.

2. **Delete merged branches?** `feature/song-import-resolution` and `fix/artists-filter-regression` are 0 ahead of develop. Safe to delete local + remote?

3. **Phase 16C emulator gate:** The Song Import & Entity Resolution merge already addressed BUG-005–010 and was verified to build (0 errors, 354 tests). Is the 16C.1 emulator smoke test still required as an independent gate before marking Artists & Songs ✅ Done? Or does the Song Import emulator gate (pending) count for both?

4. **Next priority after emulator gate:** The 6 bug rows (BUG-005–010) need BACKLOG status updates from `📋 Spec` → `✅ Fixed`. This is docs-only and can be done immediately — no code change needed.
