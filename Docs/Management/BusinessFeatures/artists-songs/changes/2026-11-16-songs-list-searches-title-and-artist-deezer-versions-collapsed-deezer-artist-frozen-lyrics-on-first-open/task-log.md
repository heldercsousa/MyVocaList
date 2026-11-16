# Task log: Songs list search, one Deezer row per song, fixed Deezer artist, lyrics on first open

Spec: `requirements.md`, `design.md`. Plan: `plan.md`. Tasks: `tasks.md`.
Branch `feature/songs-list-deezer`, worktree `.claude/worktrees/songs-list-deezer`.

### Checkpoint

- **Updated:** 2026-11-16, implementation started.
- **Phase:** 1 — Contracts and Domain. T1 dispatched to an implementor; T2 next.
- **Done so far:** T0 (discussion, spec, spec review, plan, plan review). Docs on `develop` up to commit 83346485.
- **Next:** T2, then T3 (hand-off for the migration, plan § 9 item 6), T4.
- **Context manifest (≤ 8 files):** `plan.md`, `tasks.md`, `requirements.md`, `design.md`, this file.

## T0 — Spec and plan (2026-11-16) — Done

- Discussion with Helder on how Deezer data is kept. Id-only storage rejected; four certain pieces specified here; the Deezer section in the Songs list is a separate undecided trial; sending lyrics to LRCLIB is post-MVP.
- Helder's instruction of 2026-11-16: "You are authorized in advance to proceed with all next needed steps. Be autonomous." The spec and the plan are approved under it. The device check stays his.
- Spec review (fresh reviewer): pass with fixes; fixes applied (REQ-SLD-30, 31 added; decisions C14 to C19).
- Plan review (fresh reviewer): pass with fixes; fixes applied (`plan.md § 9`).
- Deezer API check: official reference behind a login wall, not read. A recorded real answer in the test suite shows 25 rows by default and `artist.id` on each row. No page-size parameter is sent (C20).

### For Helder to confirm or overrule

Decisions taken for him are C1 to C27 in `design.md § 1.2`. The ones most worth a look:

| # | Decision |
|---|----------|
| C14 | Add mode: changing the Title text after a Deezer pick makes the artist editable again (that song is saved as local-only by the existing rule). |
| C16 | Save waits for the first-open lyrics lookup to end before writing. |
| C17 | "None found" is permanent, also when the user had corrected the title or the lyrics were over the limit. |
| C22 | Mocks in `SongFormViewModelLyricsTests` are re-pointed to the new lookup method; no assertion changes. |
| C23 | The existing test of the Version reset on X is removed with the code it tests. |
| C25 | **Known gap:** add mode — change the Title, edit the artist, type the picked title back — saves a Deezer song with the edited artist. Not closed. |
| C27 | A Deezer option row keeps its album part; only the version label is gone. |
