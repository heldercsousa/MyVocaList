# Task log: Venue duplicates are name and address together

### Checkpoint

- **Updated:** 2026-11-17, after the merge.
- **Phase:** close-out done. **Waiting for Helder's device check (VD4).**
- **Done:** VD1, VD2 committed as 4d38bf97 on `feature/venue-duplicate-pair` (pre-commit full suite green on the first try), merged into `develop` as 766eb860.
- **Next:** VD4 device check by Helder; then status Done and worktree removal.
- **Context manifest (≤ 8 files):** `requirements.md`, `design.md`, `tasks.md`, this file.

---
## VD1, VD2 (2026-11-17) — To Review
**Plan:** `Docs/Management/BusinessFeatures/venues/changes/2026-11-17-venue-duplicates-name-and-address-together-same-name-at-another-address-allowed/tasks.md`
**Status:** To Review. Committed 4d38bf97 by the main agent after the implementor stopped on an unrelated intermittent song-form test; the pre-commit full suite passed on the first try.
**Started:** 2026-11-17
**Completed:** 2026-11-17

### Changed files:
- `Domain/RepositoryInterface/IVenueRepository.cs` (VD1: `AddressExistsAsync` replaced by `NameAndAddressExistsAsync`)
- `Infra/Repository/VenueRepository.cs` (VD1: pair query, `v.Name == name` plus six address columns, no early return for empty address)
- `MyVocaList.Tests/Integration/Repositories/VenueRepositoryAddressTests.cs` (VD1)
- `Services/VenueService.cs` (VD2: `CreateVenueAsync`, `UpdateVenueAsync`, `SaveVenueAsync` each use one pair check; helpers `DuplicateNameAndAddressMessage`, `AddressOf`)
- `MyVocaList.Tests/Unit/Services/VenueServiceSaveTests.cs` (VD2)
- `MyVocaList.Tests/Unit/Services/VenueServiceTests.cs` (VD2)
- `Docs/.../venues/changes/2026-11-17-.../tasks.md` (VD1, VD2 ticked)
- `Docs/.../venues/changes/2026-11-17-.../design.md` (one `> **Spec updated**` note on `GetByNameAsync`)
- `Docs/.../venues/changes/2026-11-17-.../task-log.md` (this entry)

### Verification evidence
- Red: VD1 integration tests were written against a method that did not yet exist (compile failure, missing member). VD2 tests were written before the service change; the service then had to be changed to build. I did not capture a separate run of VD2 tests against the old service, so the red step for VD2 is by construction, not observed.
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`: 0 errors, 27 warnings (run before the last test-only edits; app code unchanged since).
- `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` after the final test edits: 1833 passed, 1 failed, total 1834. Failure: `SongFormViewModelTests.TitleOptionSource_SetsNoteThroughUiThreadHelper` (expected 1, actual 2). Across this session it failed in 2 of 4 full runs and passed in the other 2; it passes when run alone. It touches no venue code. Not edited.
- The venue tests (`VenueRepositoryAddressTests`, `VenueServiceSaveTests`, `VenueServiceTests`) all pass in every run.
- Device check, E2E emulator: not run (no UI change; VD4 is Helder's).

### Existing tests replaced or removed (REQ-VEN-19 / 20 / 21)
| File | Existing test | Change | Replacing requirement |
|------|---------------|--------|-----------------------|
| VenueRepositoryAddressTests | `AddressExistsAsync_SameSixFields_ReturnsTrue` | removed | REQ-VEN-41 (`NameAndAddressExistsAsync_SameNameAndSixFields_ReturnsTrue`) |
| VenueRepositoryAddressTests | `AddressExistsAsync_DifferentLetterCaseAndAccents_ReturnsTrue` | replaced | REQ-VEN-41 |
| VenueRepositoryAddressTests | `AddressExistsAsync_NullLine2OnBothSides_ReturnsTrue` | replaced | REQ-VEN-41 |
| VenueRepositoryAddressTests | `AddressExistsAsync_OnlyLine2Differs_ReturnsFalse` | replaced | REQ-VEN-42 |
| VenueRepositoryAddressTests | `AddressExistsAsync_DifferentCity_ReturnsFalse` | replaced | REQ-VEN-42 |
| VenueRepositoryAddressTests | `AddressExistsAsync_StoredPostalCodeSearchedNull_ReturnsFalse` | replaced | REQ-VEN-42 |
| VenueRepositoryAddressTests | `AddressExistsAsync_ExcludeIdOfTheOnlyMatch_ReturnsFalse` | replaced | REQ-VEN-41 |
| VenueRepositoryAddressTests | `AddressExistsAsync_ExcludeIdOfAnotherVenue_ReturnsTrue` | replaced | REQ-VEN-41 |
| VenueRepositoryAddressTests | `AddressExistsAsync_EmptyAddress_ReturnsFalseEvenWithEmptyVenuesStored` | replaced (inverted: empty equals empty) | REQ-VEN-44 |
| VenueServiceSaveTests | `SaveVenueAsync_DuplicateNameAndDuplicateAddress_ReportsNameFirst` | replaced | REQ-VEN-41 (`..._NameAndAddressPairExists_RefusedOnNameWithAddressMessage`) |
| VenueServiceSaveTests | `SaveVenueAsync_EmptyAddress_SavesWithoutAddressDuplicateCheck` | replaced | REQ-VEN-44 (+13) |
| VenueServiceSaveTests | `SaveVenueAsync_AddMode_DuplicateName_RefusedOnNameAndNothingStored` | replaced | REQ-VEN-44 |
| VenueServiceSaveTests | `SaveVenueAsync_AddMode_DuplicateAddress_RefusedOnStreetAddressAndNothingStored` | removed | REQ-VEN-42 and REQ-VEN-43 (two new saved tests) |
| VenueServiceSaveTests | `SaveVenueAsync_AddMode_AddressCheckReceivesNormalisedAddress` | replaced | REQ-VEN-41 |
| VenueServiceSaveTests | `SaveVenueAsync_EditMode_OwnName_IsAccepted` | replaced | REQ-VEN-41 |
| VenueServiceSaveTests | `SaveVenueAsync_EditMode_OtherVenuesName_RefusedOnName` | replaced | REQ-VEN-41 |
| VenueServiceSaveTests | `SaveVenueAsync_EditMode_AddressCheckExcludesOwnId` | replaced | REQ-VEN-41 |
| VenueServiceSaveTests | `SaveVenueAsync_EditMode_DuplicateAddress_RefusedOnStreetAddress` | removed | REQ-VEN-42 (`..._NoOtherVenueWithPair_IsSaved`) |
| VenueServiceTests | `CreateVenueAsync_DuplicateName_ReturnsFalse` | mocks moved, asserts exact message | REQ-VEN-44 |
| VenueServiceTests | `CreateVenueAsync_ValidName_ReturnsSuccess` | mock moved | REQ-VEN-45 |
| VenueServiceTests | `UpdateVenueAsync_DuplicateName_ReturnsFalse` | mock moved, asserts exact message | REQ-VEN-45 |
| VenueServiceTests | `UpdateVenueAsync_ValidUpdate_ReturnsSuccess` | mock moved | REQ-VEN-42 |

Mock-only edits (no test body change of meaning): the constructor setup of `VenueServiceSaveTests`; `SaveVenueAsync_Name61Characters_...` (Verify target, REQ-VEN-22 kept); `SaveVenueAsync_InvalidAddressAndDuplicateName_...` (REQ-VEN-14 kept, setup moved); `SaveVenueAsync_EditMode_FailedSave_WritesNothing` (REQ-VEN-11 kept, setup moved). `VenueFormViewModelProfileTests` was not touched; it compiles.

New test: `UpdateVenueAsync_NameAndStoredAddressPairExists_RefusedWithAddressMessage` (REQ-VEN-45).

### Traceability
| AC | Criterion | Implementation | Test |
|----|-----------|----------------|------|
| REQ-VEN-41 | Equal name and six address fields refused; Name field; message by address emptiness; own id excluded; collation | `VenueRepository.NameAndAddressExistsAsync`; `VenueService.SaveVenueAsync`, `DuplicateNameAndAddressMessage` | Repo: `NameAndAddressExistsAsync_SameNameAndSixFields_ReturnsTrue`, `_DifferentLetterCaseAndAccents_ReturnsTrue`, `_NullLine2OnBothSides_ReturnsTrue`, `_ExcludeIdOfTheOnlyMatch_ReturnsFalse`, `_ExcludeIdOfAnotherVenue_ReturnsTrue`. Service: `SaveVenueAsync_NameAndAddressPairExists_RefusedOnNameWithAddressMessage`, `SaveVenueAsync_EditMode_OtherVenuePairExists_RefusedOnNameWithAddressMessage`, `SaveVenueAsync_EditMode_PairCheckExcludesOwnId`, `SaveVenueAsync_AddMode_PairCheckReceivesTrimmedNameAndNormalisedAddress`, `SaveVenueAsync_EditMode_OwnNameAndAddress_IsAccepted` |
| REQ-VEN-42 | Same name at any other address (any field differs, or empty) saved | Same query (false) → save | Repo: `_SameNameOtherAddress_ReturnsFalse`, `_SameNameOnlyLine2Differs_ReturnsFalse`, `_SameNameStoredPostalCodeSearchedNull_ReturnsFalse`, `_SameNameStoredWithAddressSearchedEmpty_ReturnsFalse`. Service: `SaveVenueAsync_AddMode_SameNameAtAnotherAddress_IsSaved`, `SaveVenueAsync_EditMode_NoOtherVenueWithPair_IsSaved`, `UpdateVenueAsync_ValidUpdate_ReturnsSuccess` |
| REQ-VEN-43 | Same six address fields under another name saved | Same query (false) → save | Repo: `NameAndAddressExistsAsync_SameAddressOtherName_ReturnsFalse`. Service: `SaveVenueAsync_AddMode_SameAddressUnderAnotherName_IsSaved` |
| REQ-VEN-44 | Two address-less venues cannot share a name | Empty address passed to the same query (no early return) | Repo: `NameAndAddressExistsAsync_SameNameBothAddressesEmpty_ReturnsTrue`, `_SameNameAddressless_OtherNameAddressless_ReturnsFalse`. Service: `SaveVenueAsync_AddMode_SameNameBothAddressesEmpty_RefusedOnNameAndNothingStored`, `SaveVenueAsync_EmptyAddress_RunsPairCheckWithEmptyAddressAndSaves`, `CreateVenueAsync_DuplicateName_ReturnsFalse` |
| REQ-VEN-45 | Name-only paths use the address the venue will have | `CreateVenueAsync` passes `VenueAddress.Empty`; `UpdateVenueAsync` passes `AddressOf(stored venue)` | `CreateVenueAsync_ValidName_ReturnsSuccess`, `UpdateVenueAsync_DuplicateName_ReturnsFalse`, `UpdateVenueAsync_NameAndStoredAddressPairExists_RefusedWithAddressMessage` |

### Decisions the spec did not state
- `GetByNameAsync` is kept (spec: keep if another caller remains). It now has no production caller, only tests. Written back to `design.md`; Helder to decide whether to remove it.
- The pair query compares the name with plain `==`, the same as `GetByNameAsync` does. The collation does the case and accent work.
- On create, the name is passed to the repository untrimmed, as the old code did. The converter trims the stored value and the query parameter.
- On rename, the stored address is read through a new private helper `AddressOf(Venue)` in `VenueService`.
- The existing test file `VenueRepositoryAddressTests.cs` keeps its name even though it no longer tests addresses alone.

### Left undone
- Commit and push: blocked. The pre-commit hook runs the suite, and the song-form test above fails intermittently. I did not use `--no-verify`.
- Merge to develop, VD3 and VD4 (not in this task).
- **Context manifest:** `Infra/Repository/VenueRepository.cs`, `Domain/RepositoryInterface/IVenueRepository.cs`, `Services/VenueService.cs` (SaveVenueAsync ~l.235-297, CreateVenueAsync l.60, UpdateVenueAsync l.86), `MyVocaList.Tests/Integration/Repositories/VenueRepositoryAddressTests.cs`, `Domain/Rules/VenueAddressRules.cs`, `Contracts/DTOs/Venues/VenueAddress.cs`, the spec folder `requirements.md`/`design.md`.
