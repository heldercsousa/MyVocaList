# Tasks: song writes propagate to the artist catalog (CATALOG-PROP)

Spec: `requirements.md`, `design.md`. Steps: `plan.md`. Log: `task-log.md`.

**Gate 0:** do not start until `task/remove-song-version-featured` (worktree `.claude/worktrees/rmv-song-version`) is merged to `develop`. It owns `ISongRepository.cs`, `SongRepository.cs`, `CatalogRepository.cs` and `Song.cs`.

Branch `feature/catalog-prop` (worktree `.claude/worktrees/catalog-prop`). Tasks run in order in that one
worktree. DRY Onion order: Domain/Infra → Services → UI.

Markers: `[ ]` available · `[~]` claimed · `[x]` committed.

## Phase 0 — Spec
- [x] C0 Code map, requirements, design, spec review, Helder approval (2026-11-20).

## Phase 1 — Domain + Infra
- [x] C1 **Repository queries.** Review lane: Standard. Size note: 6 files (interface + implementation pairs). (REQ-CATPROP-05, 06, 07, 10)
  - Files owned: `ICatalogRepository.cs`, `CatalogRepository.cs`, `ISongRepository.cs`, `SongRepository.cs`, `CatalogRepositoryTests.cs`, `SongRepositoryTests.cs`.
  - Demo: `RemoveCoversAsync(A, [own, cover])` returns `(1, 1)` on real SQLite.
- [x] C2 **Backfill migration.** After C1. Review lane: Elevated. Sequential-only (`Infra/Migrations/*`). (REQ-CATPROP-09, 11)
  - Files owned: new `Infra/Migrations/*_BackfillDerivedCatalogRows.cs` (+ Designer), new `CatalogBackfillMigrationTests.cs`, `ArtistRepositoryTests.cs`.
  - Demo: a song with no catalog row gains its author's row after `MigrateAsync`, and a second apply inserts nothing.

## Phase 2 — Services
- [x] C3a **Test setup: `ICatalogRepository` in SongService test scopes** (Tester role, no AC). After C1. Review lane: Standard.
  - Size note: about 11 test files, each a one-argument mechanical addition; no assertion changed.
  - Files owned: every `PassthroughUnitOfWork.Over(...)` call that builds a `SongService` (list in `plan.md § Task 3a`, confirm by grep).
  - Demo: the full test run stays green.
- [x] C3b **SongService propagation.** After C3a. Review lane: Elevated. (REQ-CATPROP-01, 02, 03, 04)
  - Files owned: `Services/SongService.cs`, new `SongServiceCatalogPropagationTests.cs`.
  - Demo: create a song → its author's catalog lists it; change the author → the row moves.
- [x] C4 **CatalogService guard + bulk remove.** After C1. Review lane: Standard. (REQ-CATPROP-06, 07)
  - Files owned: `ICatalogService.cs`, `Services/CatalogService.cs`, `CatalogServiceTests.cs`.
  - Demo: the bulk remove of an own song + a cover returns `Removed 1 song from the catalog. 1 kept: <A> wrote it.`
- [x] C5 **ArtistService delete check.** After C1. Review lane: Standard. (REQ-CATPROP-10)
  - Files owned: `Services/ArtistService.cs`, `ArtistServiceTests.cs`, `PagedListConcurrencyTests.cs` (constructor call), new `ArtistServiceDeleteIntegrationTests.cs`.
  - Demo: deleting an artist who owns songs is refused with the AC-6.2 text; a cover-only artist is deleted.

## Phase 3 — UI
- [x] C6 **ViewModels.** After C4, C5. Review lane: Standard. (REQ-CATPROP-08, 10)
  - Files owned: `SongsViewModel.cs`, `ArtistsViewModel.cs`, new `SongsViewModelCatalogDeleteTests.cs`, new `ArtistsViewModelDeleteConfirmTests.cs`.
  - Demo: catalog-mode Delete never calls `DeleteSongsAsync`.

## Close-out
- [x] C7 Merge to `develop`, full test run, Android build, traceability matrix, status.
- [x] C8 **Device check (Helder)**, checklist in `plan.md § Task 7`.
