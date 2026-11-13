# Task log: Deezer artwork in the Song form options and the Songs list

Branch `feature/deezer-artwork` · worktree `.claude/worktrees/deezer-artwork` · started 2026-11-13.

## Authority

Helder, 2026-11-13: keep every activity tracked in the proper docs, be autonomous until done, take
decisions alone unless his participation is crucial, work in a worktree, do not ask. Every gate below
marked "pre-authorised" rests on that instruction. The decisions taken for him are `design.md § 1`
(D1…D17). The device check (T11) is his.

### Checkpoint
- **Phase:** T0 spec — spec and plan reviews running.
- **Next:** apply review fixes → commit docs → dispatch T1.
- **Context manifest:** `requirements.md`, `design.md`, `plan.md`, `tasks.md`, this file, the two component READMEs (paths in `tasks.md`).

## T0 — Evaluation and spec (2026-11-13) — Done

- **Request part 1 (shared autocomplete on the Song form):** already in place. `SongFormPage.xaml:32`
  (`titleField`) and `:66` (`artistField`) are `AutocompleteDXField` since commit `6f7b6bbf`. No change.
- **MD3 review:** official m3.material.io lists, menus, search and text-field pages captured with
  Playwright on 2026-11-13 (lists and menus reused from the 2026-11-12 capture); verbatim quotes in
  `findings.md`. Internal guideline `.claude/library/m3-lists.md` compared: sizes confirmed; image
  corner radius, placeholder and several spacing values are not in the official docs (see `findings.md` Step D).
- **Deezer check (live public API, 2026-11-13):** `search/track` returns `album.cover_small|medium|big|xl`,
  `album.title`, `artist.picture_*`, `md5_image`; `search/artist` returns `picture_*`. All square,
  on `cdn-images.dzcdn.net`, content-addressed by a hash. No lyric field.
- **Code survey (Explore subagent):** no image data anywhere today (provider, DTOs, entities, list
  projections). The option row template is fixed inside `AutocompleteDXField`. The Songs list shows
  Featured artists as supporting text, not the artist. `ArtistRepository.SearchByNameAsync` projects a
  catalog count of 0, so every Artist option reads "No catalog" (fixed here as REQ-ART-10).
- **Other findings of the survey, not part of this item** (reported to Helder at close-out):
  the Artist form never binds its `NameOptionSource` (plain `TextEdit`, owned by the pending
  ArtistFormPage item); `ArtistFormPage.xaml` declares `ArtistListItemDto` while the ViewModel holds
  `ArtistListItem`; `ProviderBadgeText` expects `"deezer"` but `"Deezer"` is stored; `IX_Artists_ExternalId`
  is unique on the id alone, not per provider; the Deezer artist id is never stored on an artist.
- **Backlog:** this item set 🟡 In Progress; two component change items registered (m3-lists,
  autocomplete-component), each holding its four gates.
- **Gates:** spec approval, component Gate 4 and plan approval: pre-authorised by Helder 2026-11-13.

- **Baseline (worktree, before any code):** `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` → 967 passed, 0 failed, 0 skipped.
- **Spec review (fresh `spec-reviewer`):** PASS WITH FIXES, no blocker. Applied: device-check task
  number (T11); project decisions separated from MD3 rules (new D18 row height, D19 one-line text;
  D5, D8 and the § 2 table reworded); Deezer API evidence added to `findings.md`; image URL
  definition aligned with `ImageUrlRules` (host not checked); REQ-ART-14, 16 and 18 completed
  (pending images survive a refused Save and sheets; wording after X on Title; example for X on
  Artist then local pick); planned edit of four superseded tests stated in `design.md § 6`.
- **Plan review (fresh `plan-reviewer`):** FAIL with 2 blockers, both fixed in `plan.md`:
  (B1) C# expression trees cannot omit optional arguments, so every `IQueryable` projection of the
  three changed records passes all arguments, and the raw-SQL select gains the two columns;
  (B2) the two lookups that feed promoted rows (`FindByExternalIdsAsync`,
  `FindByTitleAndArtistNamesAsync`) are now in T2 with integration tests. Also applied: T5 brief made
  concrete (`SaveFormResolution.FillArtistImage`, private helper parameters, public signatures
  unchanged); REQ-ART-23 note in T6 and an offline step in T11; task interface table (consumes,
  produces, demo, review lane); the four test edits listed in T8.

### Changed files
- `requirements.md`, `design.md`, `findings.md`, `plan.md`, `tasks.md`, `task-log.md` (this folder), `README.md` (status)
- `Docs/Management/DevCycleCraft/m3-lists/changes/2026-11-13-listitem-leading-media-placeholder-on-listitemleadingimage-and-new-listitemleadingavatar/README.md`
- `Docs/Management/DevCycleCraft/autocomplete-component/changes/2026-11-13-autocompletedxfield-option-rows-optional-leading-image-or-avatar-and-trailing-text/README.md`
- `Docs/Management/BACKLOG.md` (generated), `MyVocaList.sln` (registration)
