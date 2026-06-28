# BUG-018 Task Log

**Bug:** ArtistFormPage Edit Save crash (EF Core ChangeTracker pollution)  
**Severity:** Critical  
**Fix Status:** ✅ Complete  
**Plan:** `/Docs/Management/BusinessFeatures/artists-songs/bugs/BUG-018-artistformpage-edit-save-crash/plan.md`

---

## Execution Summary

**Workflow:** TDD (Red → Green → Verification)

**Red Phase (Regression Test):**
- ✅ Added regression test `GetPagedAsync_ExplicitNoTracking_DoesNotPollutTracker_AndUpdateSucceeds`
- ✅ Test verified FAILING before fix (ChangeTracker polluted without explicit `.AsNoTracking()`)

**Green Phase (Implementation):**
- ✅ Global `QueryTrackingBehavior = NoTracking` added to `AppDbContext` constructor
- ✅ `ArtistListItem` record created in `Domain/ReadModels/` with computed properties `CatalogCountText` and `ProviderBadgeText`
- ✅ `IArtistRepository` interface updated: return types → `ArtistListItem`
- ✅ `ArtistRepository` implementation:
  - `GetPagedAsync`: explicit `.AsNoTracking()` + SQL projection to `ArtistListItem`
  - `SearchByNameAsync`: explicit `.AsNoTracking()` + projection to `ArtistListItem`
  - `GetByIdAsync`: added `.AsTracking()` for edit-path tracking
  - `GetByExternalIdAsync`: added `.AsNoTracking()`
  - `UpdateAsync`: removed unreliable ChangeTracker guard
- ✅ `IArtistService` interface updated: return types → `ArtistListItem`
- ✅ `ArtistService`: simplified methods to return repository results directly (no DTO mapping)
- ✅ All consumers updated to use `ArtistListItem`:
  - `ArtistsViewModel`: generic type parameter updated
  - `ArtistFormViewModel`: command parameter type updated
  - `SongFormViewModel`: artist suggestion type updated

**Verification:**
- ✅ Build: 0 errors, 0 warnings (package warnings only)
- ✅ Tests: 360/360 passing (regression test Green, all existing tests passing)
- ✅ Post-edit re-read: all changes verified correct
- ✅ Spec compliance: all AC-compliant changes

---

## Changed Files

| File | Change |
|------|--------|
| `Infra/AppDbContext.cs` | Added global `QueryTrackingBehavior = NoTracking` in constructor |
| `Domain/ReadModels/ArtistListItem.cs` | **NEW** — Read model with computed properties |
| `Domain/RepositoryInterface/IArtistRepository.cs` | Updated `GetPagedAsync` and `SearchByNameAsync` return types |
| `Infra/Repository/ArtistRepository.cs` | Explicit `.AsNoTracking()` on list methods, `.AsTracking()` on `GetByIdAsync`, guard removed from `UpdateAsync` |
| `Domain/ServicesInterfaces/IArtistService.cs` | Updated return types to `ArtistListItem` |
| `Services/ArtistService.cs` | Simplified methods to return repository results directly |
| `MyVocaList/UI/ViewModels/ArtistsViewModel.cs` | Generic type → `ArtistListItem` |
| `MyVocaList/UI/ViewModels/ArtistFormViewModel.cs` | Command parameter type → `ArtistListItem` |
| `MyVocaList/UI/ViewModels/SongFormViewModel.cs` | Artist suggestion type → `ArtistListItem` |
| `MyVocaList.Tests/Integration/Repositories/ArtistRepositoryTests.cs` | Regression test added; existing test updated for detached instance scenario |

> **Deletable (manual step after code review):** `Contracts/DTOs/List/ArtistListItemDto.cs` — no longer used, can be removed in a separate cleanup task.

---

## AC Traceability

| AC ID | Criterion | Implementation | Test |
|-------|-----------|-----------------|------|
| BUG-018-1 | GetPagedAsync must not add entities to ChangeTracker | `.AsNoTracking()` on `GetPagedAsync` query | `GetPagedAsync_ExplicitNoTracking_DoesNotPollutTracker_AndUpdateSucceeds` |
| BUG-018-2 | UpdateAsync must work with detached instances | Removed guard, use EF Core's `.Update()` | `UpdateAsync_DetachedInstance_Updates` |
| BUG-018-3 | Global NoTracking prevents concurrent query pollution | `ChangeTracker.QueryTrackingBehavior = NoTracking` in constructor | Coverage via regression test + all 360 unit tests |

---

## Build & Test Evidence

**Build:** ✅ Clean  
**Tests:** ✅ 360/360 passing (0 failures, 0 skipped)  
**Regression Test:** ✅ `GetPagedAsync_ExplicitNoTracking_DoesNotPollutTracker_AndUpdateSucceeds` PASSING

---

## Notes

- The fix prevents the root cause (ChangeTracker pollution) rather than adding workarounds. List operations use global NoTracking; edit operations use explicit `.AsTracking()` on `GetByIdAsync`.
- `ArtistListItemDto` in Contracts is now unused and can be deleted in a follow-up cleanup task.
- All existing tests verified passing. One existing test (`UpdateAsync_EntityAlreadyTracked_DoesNotThrow`) was updated to reflect the proper detached instance scenario after the fix.
- Regression test confirms that explicit `.AsNoTracking()` on list methods is a defense-in-depth layer independent of the global setting.

---

**Completed:** 2026-06-27  
**Session:** BUG-018 implementation  
**Commit:** `fix: BUG-018 — artists list entity tracking crash`
