# Task log

---
## Task: T1 + T2 — BUG-088 identity (Domain, Infra, Services, regression tests)
**Plan:** Docs/Management/BusinessFeatures/persons/bugs/2026-11-19-BUG-088-duplicate-singer-saved-when-the-name-is-typed-without-picking-the-existing-one/tasks.md
**Status:** Check build
**Started:** 2026-11-20
**Completed:** 2026-11-20
**Branch/worktree:** `task/bug-088-identity` at `.claude/worktrees/bug-088-identity` (base b9024ff8, depends on T1 of the birthday change)

### Changed files:
- `Domain/Exceptions/ConstraintViolationException.cs` (new)
- `Domain/RepositoryInterface/IPersonRepository.cs` (+2 members: IdentityExistsAsync, GetSameNameDifferentIdentityAsync)
- `Domain/ServicesInterfaces/IPersonService.cs` (+2 members: ValidateContact, GetSameNamePersonsAsync; no existing signature changed)
- `Infra/EntityEFConfig/PersonConfiguration.cs` (CK_Persons_EmailOrBirthday; comment pointing to the index migration)
- `Infra/Migrations/20261120234431_PersonEmailOrBirthdayCheck.cs` (+ `.Designer.cs`, new; EF scaffold)
- `Infra/Migrations/20261120234436_PersonIdentityUniqueIndex.cs` (+ `.Designer.cs`, new; raw IX_Persons_Identity)
- `Infra/Migrations/AppDbContextModelSnapshot.cs`
- `Infra/UnitOfWork/UnitOfWork.cs` (SQLite 19 → ConstraintViolationException in the save path)
- `Infra/Repository/PersonRepository.cs` (IdentityExistsAsync, GetSameNameDifferentIdentityAsync, shared identity predicate)
- `Services/PersonService.cs` (DuplicateIdentityMessage, EmailOrBirthdayRequiredMessage constants, ValidateContact, identity checks in Create/Update, catch → tuple, GetSameNamePersonsAsync)
- `Tests/Unit/Services/PersonServiceTests.cs` (REQ-DUP-01/02/03/06 tests; `UpdatePersonAsync_Valid` given an email)
- `Tests/Integration/Repositories/PersonIdentityIndexTests.cs` (new; REQ-DUP-04/05, migrated real SQLite)
- `Tests/Integration/UnitOfWork/PersonIdentityConstraintTranslationTests.cs` (new; UoW translation)
- `Tests/Integration/Repositories/PersonRepositoryTests.cs` (superseded test renamed and updated; helper gives an email; identity query tests)
- `Tests/Integration/Repositories/PersistedStringTrimmingTests.cs` (email or birthday added)
- `Tests/Integration/Migrations/PersonBirthDayMonthMigrationTests.cs` (seed rows get an email)
- `Tests/Integration/UnitOfWork/Bug068RegressionTests.cs` (email on create and update)
- `Tests/Integration/UnitOfWork/PersonServiceReadScopeTests.cs` (email on create)
- `Docs/.../bugs/2026-11-19-BUG-088-.../design.md` (3 `Spec updated 2026-11-20` notes)
- `Docs/.../bugs/2026-11-19-BUG-088-.../tasks.md` (T1, T2 ticked)
- `Docs/.../bugs/2026-11-19-BUG-088-.../task-log.md`

### Build notes
Build: `dotnet build Tests/MyVocaList.Tests.csproj` 0 errors; `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` 0 errors | Tests: 1977 passed, 0 failed, 0 skipped (`dotnet test Tests/MyVocaList.Tests.csproj`; T1 baseline 1958 + 19 new) | Commit SHA: see git log (commit follows this entry)
Files written and re-read: Domain/RepositoryInterface/IPersonRepository.cs, Infra/Repository/PersonRepository.cs (grep + rebuild), Infra/UnitOfWork/UnitOfWork.cs (grep of all three save sites), Infra/EntityEFConfig/PersonConfiguration.cs, Services/PersonService.cs (grep of all new members), Infra/Migrations/20261120234436_PersonIdentityUniqueIndex.cs (read in full), Tests/Integration/Repositories/PersonIdentityIndexTests.cs (edited after RED).
E2E: emulator not run in this task. Save-result behaviour changes, but there is no XAML or VM change (T3). The on-device gate is T4 (Helder). Status is `Check build` until then.

### RED evidence (before the fix, `dotnet test --filter "CreatePersonAsync_IdentityExists|PersonIdentityIndexTests"`)
Result: 4 failed, 2 passed, 0 compile errors. Failures are behavioural:
- `PersonServiceTests.CreatePersonAsync_IdentityExists_ReturnsDuplicateMessage` (REQ-DUP-01): `Assert.False() Failure, Expected: False, Actual: True`. The service saved the duplicate.
- `PersonIdentityIndexTests.IdentityIndex_SameNameCaseAndAccentDifferent_NullEmailSameBirthday_IsRejected` (REQ-DUP-04): `No exception was thrown`. The duplicate row was inserted.
- `PersonIdentityIndexTests.IdentityIndex_ExistsInSqliteMasterAfterMigrate` (REQ-DUP-04/11): `Expected: ["IX_Persons_Identity"] Actual: []`.
- `PersonIdentityIndexTests.EmailOrBirthdayCheck_NeitherEmailNorBirthday_IsRejected` (REQ-DUP-05): `No exception was thrown`.
- Passing before the fix (guards, not regressions): `IdentityIndex_SameNameDifferentEmail_IsAllowed`; `IdentityIndex_SameNameSameEmailDifferentCase_IsRejected` (already rejected by the existing `IX_Persons_Email`, see deviations).
- The red run used the RED-only repository signatures (no service check). Those were the only pre-fix production changes.

### GREEN evidence
Full run after the fixes: `Total: 1977, Aprovado: 1977, Com falha: 0`. The four RED tests and the two guards pass. `IX_Persons_Identity` is present in `sqlite_master` after `MigrateAsync`.

### Migration-rebuild finding (discovered in T2)
The first single-migration attempt (CHECK + raw index in `PersonIdentityUniqueIndex`) failed the index test. EF's SQLite generator emits a `SqlOperation` while a `People` rebuild is pending, and the rebuild runs at the end of the migration, so it dropped the index. Verified with `dotnet ef migrations script`. Fix: split into `PersonEmailOrBirthdayCheck` (CHECK, rebuild) and `PersonIdentityUniqueIndex` (raw index only). The unapplied first migration was removed with `dotnet ef migrations remove`. The migration names differ from the briefing (see deviations).

### UnitOfWork DbUpdateException audit (before the translation change)
- Production: no `catch (DbUpdateException)` anywhere. The only `SqliteException` catches are in `ArtistRepository`/`SongRepository` (query-side, not the save path).
- Tests: every `ThrowsAsync<DbUpdateException>` calls `_db.SaveChangesAsync()` directly (repository tests: ArtistRepositoryTests, CatalogRepositoryTests, PersonRepositoryTests, SongKaraokeUrlRepositoryTests, SongRepositoryResolutionTests, SongRepositoryTests). None goes through the UoW, so none needed an update.
- `SongMigrationTests` lines 103 and 155 use raw SQL with `ThrowsAnyAsync<Exception>` and unwrap the inner `SqliteException`. They do not go through the UoW.

### AC traceability
| AC | Criterion | Implementation location | Test method |
|----|-----------|-------------------------|-------------|
| REQ-DUP-01 | Create rejects an existing identity, nothing saved | Services/PersonService.cs CreatePersonAsync; Infra PersonRepository.IdentityExistsAsync | PersonServiceTests.CreatePersonAsync_IdentityExists_ReturnsDuplicateMessage (RED→GREEN) |
| REQ-DUP-02 | Update rejects another person's identity; own identity passes | PersonService.UpdatePersonAsync (excludeId = id) | PersonServiceTests.UpdatePersonAsync_OtherPersonHasIdentity_ReturnsDuplicateMessage; UpdatePersonAsync_OwnIdentity_Passes |
| REQ-DUP-03 | Create and Update reject neither email nor birthday | PersonService.ValidateContact, called in Create and Update | CreatePersonAsync_NoEmailNoBirthday_…; UpdatePersonAsync_NoEmailNoBirthday_…; ValidateContact_EmailOnly / BirthdayOnly / NeitherEmailNorBirthday |
| REQ-DUP-04 | DB unique index IX_Persons_Identity rejects a duplicate identity | Infra Migrations/20261120234436_PersonIdentityUniqueIndex.cs | PersonIdentityIndexTests.IdentityIndex_SameNameCaseAndAccentDifferent_NullEmailSameBirthday_IsRejected (RED→GREEN); IdentityIndex_SameNameDifferentEmail_IsAllowed; IdentityIndex_SameNameSameEmailDifferentCase_IsRejected |
| REQ-DUP-05 | CHECK CK_Persons_EmailOrBirthday | Infra PersonConfiguration.cs; Migrations/20261120234431_PersonEmailOrBirthdayCheck.cs | PersonIdentityIndexTests.EmailOrBirthdayCheck_NeitherEmailNorBirthday_IsRejected (RED→GREEN) |
| REQ-DUP-06 | Constraint violation at save → duplicate tuple, no throw | Infra UnitOfWork.SaveTranslatedAsync (19 → ConstraintViolationException); Services PersonService catch (Create, Update) | PersonIdentityConstraintTranslationTests.ExecuteAsync_UniqueViolationOnSave_ThrowsConstraintViolationAndRollsBack; PersonServiceTests.CreatePersonAsync_ConstraintViolationOnSave_ReturnsDuplicateMessage; UpdatePersonAsync_ConstraintViolationOnSave_ReturnsDuplicateMessage |
| REQ-DUP-11 | Migration creates the index with no dedupe; a remaining duplicate fails the migration | Migrations/20261120234436 (raw CREATE, no dedupe) | PersonIdentityIndexTests.IdentityIndex_ExistsInSqliteMasterAfterMigrate (index existence after MigrateAsync). The failure-on-duplicate path is not tested directly |
| REQ-DUP-09 (service half) | Same-name query excludes the exact identity match | PersonRepository.GetSameNameDifferentIdentityAsync; PersonService.GetSameNamePersonsAsync | PersonRepositoryTests.GetSameNameDifferentIdentityAsync_ReturnsNamesakeButNotExactMatch |
| (infra) | IdentityExistsAsync case-insensitive, excludes self | PersonRepository.IdentityExistsAsync | PersonRepositoryTests.IdentityExistsAsync_SameIdentityDifferentCase_ReturnsTrue; IdentityExistsAsync_ExcludeSelf_ReturnsFalse |

REQ-DUP-07, 07a, 08, 09 (form and sheet), 10 are UI (T3). Not covered in T2.

### Deviations and spec gaps
- **Migration split (non-blocking; proceeded).** The briefing and design asked for one migration. EF's SQLite generator defers the rebuild to the end of the migration (see the finding above), so the index must be in a later migration. The CHECK is `PersonEmailOrBirthdayCheck`; the index is `PersonIdentityUniqueIndex`. Documented in design.md as `Spec updated 2026-11-20`.
- **Literal collation in migration (non-blocking).** The migration uses the literal `NOCASE_NOACCENT`, not `CollationConstants.Default`, so it stays frozen. Documented in design.md.
- **Repository predicate (non-blocking; bug found in tests).** `p.Email ?? ""` returned no match: the Email trim converter turns the empty constant into NULL. The email is now matched as `Email IS NULL` when empty, through one shared predicate. Documented in design.md.
- **REQ-DUP-06 integration test not built as the design describes (non-blocking).** `UnitOfWorkTestHost` builds its schema with `EnsureCreated`, which has no raw `IX_Persons_Identity`. A service test with a bypassed pre-check therefore cannot reach the DB backstop. Instead: the UoW translation test uses the EnsureCreated `IX_Persons_Email` unique index, and the catch is covered by unit tests. Recommendation: if Helder wants the backstop exercised through the service, add a host variant that runs `MigrateAsync`. Blocking: No.
- **`SameEmailDifferentCase` guard (non-blocking).** The design's test is listed under REQ-DUP-04. It already passes before the fix, because `IX_Persons_Email` rejects a case-only email difference. It is kept as a guard, not as a RED regression.
- **Renamed superseded test.** `AddAsync_SameNameNullBirthday_AllowsDuplicate` is now `AddAsync_SameNameDifferentEmail_AllowsDuplicate`, because the AC changed.
- **Design's superseded list named `AddAsync_SameNameSameBirthday_ThrowsDbUpdateException`.** It does not exist in the repo, so there was nothing to update.
- **Create with a duplicate email and identity.** The existing email-taken check runs first, so an identical email gives "Email already registered…" and not the duplicate message. This follows the spec's step order. Flagged for the UI (T3) review. Blocking: No.
- **Interface additions only.** IPersonService and IPersonRepository gained members. No existing signature changed. No ViewModel or XAML was touched.

### Design concern
- None beyond the migration split above.
