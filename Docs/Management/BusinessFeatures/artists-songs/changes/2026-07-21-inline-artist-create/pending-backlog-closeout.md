# PENDING — INLINE-AC BACKLOG closeout (apply AFTER T10 re-run is all-green)

> **Created 2026-07-22** by the INLINE-AC fix-wave session. This is a *deferred* action list: do **nothing** here until Helder's on-device **T10 re-run passes all items**. A fresh terminal may perform this closeout — everything needed is captured below.

## ⚠️ CRITICAL — how to edit BACKLOG.md (do NOT hand-edit)

As of 2026-07-22 the **SPEC-EVO** feature (branch `feature/backlog-migration`, worktree `../mvl-backlog-migration`) **owns `BACKLOG.md` + the 5 `backlog-archive/*.md` generated regions.** Migration was additive through T10a with T10b in flight — so rows may be inside `BACKLOG:GENERATED` fences.

**A hand-edit inside a `BACKLOG:GENERATED` fence is silently overwritten by `regen` — NOT merge-conflicted.** Therefore:

- **Change a row's status** → `python .claude/scripts/... backlog_gen.py status <BUG-ID> <new-status>` (confirm exact script path/args against the SPEC-EVO tooling — see its `tasks.md`/`README`), **or** edit the item's `README.md` frontmatter and regen.
- **Do NOT** open `BACKLOG.md` and edit the row text directly if it sits within a generated fence.
- If unsure whether a given row is fenced or still hand-editable, **coordinate with the SPEC-EVO owner session first** (LEDGER row SPEC-EVO). Ownership ends when `feature/backlog-migration` merges — if it has merged by closeout time, re-read `BACKLOG.md` to learn the then-current edit mechanism.

## ⚠️ STATUS 2026-07-23 — T10 re-run #2 NOT green; closeout is NOT yet due
T10 re-run #2 (Helder, on device): a, e, j, C1, C2 ✅ — but **b/c/i still fail** and **new defects found**. See `handoff.md § T10 re-run #2` for the full triage. Do the closeout below ONLY after a subsequent fully-green T10.

### New/reopened bugs to REGISTER (via `backlog_gen.py`, NOT hand-edit; confirm IDs against current BACKLOG highest first)
- **INLINE-AC (this worktree) — ALL FIXED (`b0e45da`, 523/523):** BUG-060 (unlock-on-clear, REQ-ACREATE-15), BUG-057 (error text), BUG-061 (lingering row). Verify on device (T10 re-run #3), then CLOSE with the others.
- **BUG-059 → CANCELLED** (Helder 2026-07-23, works-as-designed). Set status `Cancelled`/`Superseded`; reframed as a NEW enhancement — register via `backlog_gen.py register` (kind=change, parent=Artists & Songs Catalog) from seed `BusinessFeatures/artists-songs/ENHANCEMENT-artist-owned-song-catalog-autolink.md`.
- **Songs LIST (separate feature, governed component):** BUG-062 (line-selector checkbox must be leading per MD3), BUG-063 (trailing action button has no action).

## What to apply on T10 all-green

**Precondition:** T10 re-run (items a, b, c, e, i, j + inline-create C1/C2) all pass on-device (Helder).

1. **Close these bugs** (status → resolved/done, via the mechanism above):
   - BUG-027 (parent — clear-on-blur, closed by REQ-ACREATE-03)
   - BUG-050, BUG-051, BUG-052 (T7 defects, fixed T1–T4)
   - BUG-053 (FormattedString crash, fixed `8d33547`)
   - BUG-054, BUG-055, BUG-056, BUG-057, BUG-058, BUG-059 (T10 fix wave)
2. **Unblock the Artists & Songs Catalog** — it is 🔴 Blocked pending BUG-027; set it back to its ready/in-progress state.
3. **Mark the INLINE-AC change complete** (this feature-folder change) — status ✅.
4. **LEDGER:** set the INLINE-AC row Status `merged`, move to Completed section, record the merge commit.

## Merge / git closeout (orchestrator, after T10 green — separate from BACKLOG)
- Merge `feat/inline-artist-create` → `develop` (on conflict in the 3 stale feature-doc files, take develop's versions).
- Push branch + develop (creds cached 2026-07-22 — verify with `git status -sb`, don't pipe).
- Remove the worktree `../MyVocaList-inline-ac`.

## Source of truth for the fixes
`task-log.md § T10 outcome (2026-07-22)` (root causes, file:line) + the per-bug `## Bug: BUG-0NN` entries the fix wave appends.
