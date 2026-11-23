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

---
## Task: T3 — UI (BUG-088 same-name sheet, either/or inline error)
**Plan:** Docs/Management/BusinessFeatures/persons/bugs/2026-11-19-BUG-088-duplicate-singer-saved-when-the-name-is-typed-without-picking-the-existing-one/tasks.md
**Status:** Check build
**Started:** 2026-11-20
**Completed:** 2026-11-20
**Branch/worktree:** `task/person-birthday-and-identity` at `.claude/worktrees/person-identity` (base 74097713)

### Changed files:
- `MyVocaList/UI/ViewModels/PersonFormViewModel.cs` (SaveAsync flow, either/or on Email, same-name sheet state, SaveAsNew / OpenSameName / DismissSameNameSheet commands, NavigateToEditAsync extracted)
- `MyVocaList/UI/Pages/People/PersonFormPage.xaml` (root wrapped in Grid; `sameNameSheet` dx:BottomSheet)
- `MyVocaList/UI/Pages/People/PersonFormPage.xaml.cs` (sheet Show/Close sync with guard; user dismissal → DismissSameNameSheetCommand)
- `Tests/Unit/ViewModels/PersonFormViewModelBug088Tests.cs` (new; 17 tests, REQ-DUP-07/07a/08/09/10)
- `Tests/Unit/ViewModels/PersonFormViewModelTests.cs` (fixture adaptation: constructor defaults for ValidateContact valid and GetSameNamePersonsAsync empty; `using MyVocaList.Domain.Entity`)
- `Docs/.../bugs/2026-11-19-BUG-088-.../design.md` (4 `Spec updated 2026-11-20` notes in UI)
- `Docs/.../bugs/2026-11-19-BUG-088-.../tasks.md` (T3 ticked)
- `Docs/.../bugs/2026-11-19-BUG-088-.../task-log.md`

### Build notes
Build: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` 0 errors, 3 warnings (pre-existing, none in edited files) | Tests: 2007 passed, 0 failed, 0 skipped (`dotnet test Tests/MyVocaList.Tests.csproj`; branch head 74097713 baseline 1990 + 17 new REQ-DUP tests = 2007) | Commit SHA: see git log (commit follows this entry)
Files written and re-read: PersonFormViewModel.cs (grep of the stub, command wiring, CompleteHydration, SaveAsync, SaveCoreAsync, NavigateToEditAsync), PersonFormPage.xaml (`git diff -w` shows insertions only, no removed lines), PersonFormPage.xaml.cs (grep of all new members), PersonFormViewModelTests.cs (constructor), design.md (notes read in place), tasks.md (T3 ticked).
E2E: emulator not available on this machine (verified). Status is `Check build`. The device checks (either/or error, duplicate snackbar, same-name sheet rows, Save as new / Save anyway, Cancel) belong to T4 (Helder).

### Red / green evidence
RED (after the test file and the stub members, before the Save flow was implemented): `dotnet test --filter PersonFormViewModelBug088Tests` gave 17 total, 9 failed, 8 passed. The 9 failures are behavioural: the either/or test, the either/or cleared on next save, the EmailAndBirthdayEmpty test, both edit-mode same-name tests, CreateMode query-with-form-values, CreateMode match opens sheet, DismissSheet, OpenSameName.
- Passed in RED, as guards: InvalidEmailFormat, CreateMode_NoSameNameMatch saves without sheet, EditMode_NameUnchanged does not query, SameNameSaveButtonText (create and edit), ServiceReturnsDuplicateMessage (existing snackbar routing already sends it there, REQ-DUP-08).
- Caveat: the two SaveAsNew tests also passed in RED, but only because the sheet never opened and the save went straight through. They are real checks only after GREEN.
GREEN: the same filter gave 17 passed, 0 failed. Full suite: 2007 passed, 0 failed (after the Save flow, the fixture adaptation and the XAML/code-behind).
Full-suite RED before the fixture adaptation: 5 existing PersonFormViewModelTests save-success tests failed because the unmocked `ValidateContact` returned invalid and the unmocked same-name query returned null. Fixed by the constructor defaults (fixture adaptation, not an assertion change).

### AC traceability (UI part of REQ-DUP)
| AC | Criterion | Implementation location | Test method |
|----|-----------|-------------------------|-------------|
| REQ-DUP-07 | Save with email and birthday empty shows the either/or error on Email, no service save | PersonFormViewModel.SaveAsync, ApplyContactValidation | SaveCommand_EmailAndBirthdayEmpty_ShowsEitherOrErrorOnEmailAndDoesNotCallService; SaveCommand_EitherOrResolvedOnNextSave_ClearsEmailError |
| REQ-DUP-07a | Either/or and birthday errors applied independently | SaveAsync (ApplyBirthdayValidation then ApplyContactValidation, no early return between them); ApplyContactValidation sets only on failure | SaveCommand_EitherOrAndBirthdayErrorBothFail_ShowsBothErrors (mock-level, see spec note); SaveCommand_InvalidEmailFormat_ShowsFormatErrorNotEitherOr |
| REQ-DUP-08 | Service duplicate message shows as error snackbar, page stays | PersonFormViewModel.ApplyAsyncFailureAsync (existing routing) and SaveCoreAsync | SaveCommand_ServiceReturnsDuplicateMessage_ShowsErrorSnackbarAndStaysOnPage |
| REQ-DUP-09 | Create with same-name singer opens sheet, no save | SaveAsync → GetSameNamePersonsAsync → ShowSameNameSheet | SaveCommand_CreateMode_SameNameMatchExists_OpensSheetWithoutSaving; SaveCommand_CreateMode_NoSameNameMatch_SavesWithoutSheet; SaveCommand_CreateMode_QueriesSameNameWithFormValues |
| REQ-DUP-09 | Sheet rows open the singer in edit mode (suggestion-pick navigation) | OpenSameNameAsync → NavigateToEditAsync (shared with SuggestionSelectedAsync) | OpenSameNameCommand_ListedSinger_ClosesSheetAndNavigatesToEdit |
| REQ-DUP-09 | Save as new (insert) / Save anyway (edit) saves; button text | SaveAsNewAsync, SameNameSaveButtonText, sheet Save button | SaveAsNewCommand_CreateMode_ClosesSheetAndInserts; SaveAsNewCommand_EditMode_ClosesSheetAndUpdates; SameNameSaveButtonText_CreateMode_IsSaveAsNew; SameNameSaveButtonText_EditMode_IsSaveAnyway |
| REQ-DUP-09 | Cancel or dismissal closes the sheet, nothing saved | DismissSameNameSheetCommand; code-behind OnSameNameSheetStateChanged | DismissSameNameSheetCommand_ClosesSheetWithoutSaving |
| REQ-DUP-10 | Edit with the loaded name unchanged: no same-name check | CompleteHydration (_loadedName), SaveAsync condition | SaveCommand_EditMode_NameUnchanged_DoesNotQuerySameName |
| REQ-DUP-10 | Edit with a changed name: check, excluding self, and the sheet does not update yet | SaveAsync (PersonId passed as excludeId) | SaveCommand_EditMode_NameChanged_QueriesSameNameExcludingSelf; SaveCommand_EditMode_NameChangedMatchExists_OpensSheetWithoutUpdating |
| REQ-DUP-01..06, 11 | Service, repository, index, CHECK, UoW translation | T2 (see the T1 + T2 entry above) | T2 matrix, unchanged in T3 |

### Deviations and spec gaps
- **Optional REQ-DUP-06 backstop test: skipped (non-blocking).** `Tests/Infrastructure/UnitOfWorkTestHost.cs` always calls `EnsureCreated`, so it has no raw `IX_Persons_Identity`. A migrated variant needs a new host factory in shared test infrastructure, which is outside the T3 scope. Recommendation: a follow-up test host `CreateMigrated` with `MigrateAsync`, plus a decorator for `IPersonRepository`. Blocking: No.
- **Spec gap: REQ-DUP-07a cannot co-occur on real data (non-blocking, recorded in design.md).** `ValidateContact` fires only when day AND month are both empty, and `ValidateBirthday` fires only when exactly one is set. So a day-only or month-only birthday counts as a birthday for either/or, and the two errors are mutually exclusive. The VM applies both independently (mock-level test). Question for Helder: keep the AC as written (independence), or drop "both can show at the same time"? Blocking: No.
- **Either/or error lifetime (observation, non-blocking).** The Email error clears on any valid Email edit (existing OnPersonEmailChanged behaviour). The either/or error is re-checked on the next Save, not on each birthday change. This follows the "validate on Save and reward early" rule.
- **Fixture adaptation in PersonFormViewModelTests.cs:** constructor defaults only, no assertion changed.
- **Page structure:** the `ContentPage` root is now a `Grid` holding the existing `ScrollView` and the sheet, the same as SongFormPage. No existing element was removed (diff -w shows insertions only).
- **Sheet rows:** a `VerticalStackLayout` with `BindableLayout` and a `TapGestureRecognizer` (not DXCollectionView), because the list is short and the Song sheet pattern uses BindableLayout (constraints-reference § DevExpress/UI).
- **No Services, Infra or Domain change** was needed. Domain/Services were used as they stood after T2.
- **Commit:** `fix: duplicate singer saved when the name is typed without picking` (T3 commit, Item: BUG-088). SHA: see git log.

### Design concern
- None beyond the REQ-DUP-07a co-occurrence gap above.

---
## Task: Fix B - same-name sheet Open button does nothing (E2E finding, 2026-11-21)
**Plan:** orchestrator emulator E2E finding (no plan file)
**Status:** To Review
**Started:** 2026-11-21
**Completed:** 2026-11-21

### Outcome
- Option A (`x:Name="page"` on the ContentPage, `Command` bound with `Source={x:Reference page}, Path=ViewModel.OpenSameNameCommand`) was applied and deployed. Tapping Open did nothing: the sheet stayed open, logcat showed no binding exception, and the command did not run (`OpenSameNameAsync` closes the sheet first, so a run would have closed it).
- Option B worked: the Open button uses `Clicked="OnSameNameOpenClicked"`; the handler in `PersonFormPage.xaml.cs` forwards the row `BindingContext` (`AutocompleteSuggestion`) to `_viewModel.OpenSameNameCommand.Execute(match)`. The option A x:Name and xmlns were removed. Logic stays in the ViewModel.
- design.md 2026-11-21 note updated to option B and the reason (option A also failed inside the sheet).

### Changed files:
- `MyVocaList/UI/Pages/People/PersonFormPage.xaml` (Open button: `Clicked` handler, column 1 of the row Grid)
- `MyVocaList/UI/Pages/People/PersonFormPage.xaml.cs` (`OnSameNameOpenClicked` forwarder)
- `Docs/Management/BusinessFeatures/persons/bugs/2026-11-19-BUG-088-duplicate-singer-saved-when-the-name-is-typed-without-picking-the-existing-one/design.md` (2026-11-21 note: option B, reason, SongFormPage follow-up)
- `Docs/Management/BusinessFeatures/persons/bugs/2026-11-19-BUG-088-duplicate-singer-saved-when-the-name-is-typed-without-picking-the-existing-one/requirements.md` (REQ-DUP-09 Open-button bullet, carried from the previous uncommitted state)
- `Docs/Management/BusinessFeatures/persons/bugs/2026-11-19-BUG-088-duplicate-singer-saved-when-the-name-is-typed-without-picking-the-existing-one/task-log.md` (this entry)

### Build notes
Build: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` after a clean net10.0-android (obj and bin deleted) - 0 errors, 10 warnings (pre-existing, none in the edited files) | Tests: `dotnet test Tests/MyVocaList.Tests.csproj` 2007 passed, 0 failed, 0 skipped | Commit SHA: see git log (commit carries `Item: BUG-088`)
Files written and re-read: PersonFormPage.xaml (grep: Clicked line present, no x:Name page, no xmlns:pages), PersonFormPage.xaml.cs (handler lines 43-50 re-read), design.md (2026-11-21 note re-read).

### Emulator verification (emulator-5554, app not uninstalled)
- Clean Android build, deployed with `dotnet build -t:Run -f net10.0-android`. The install step completed (`dumpsys` lastUpdateTime matched the deploy), but the dotnet process hung after install on two runs; the second run was stopped and the app was not reinstalled. The relaunch after the final run was taken as the launch step.
- Scenario 1: New Singer, "Helder Sousa", Jan/1, Save -> sheet with "Helder Sousa (14 Mar)" and Open -> Open lands on "Edit Singer", Birth day 14, Birth month March. Screenshots: `e2e-fixB2-01-sheet.png`, `e2e-fixB2-03-after-open.png` (scratchpad). Cancel back to the list.
- Scenario 2: New Singer, "Helder Sousa", Jan/1, Save -> sheet -> Save as new -> Singers list shows two "Helder Sousa" rows. Screenshot `e2e-fixB2-04-after-saveasnew.png`. Row 2 was opened via Edit to confirm 1 January, then deleted through More options -> Delete (snackbar "1 singer(s) successfully removed!"). Final list: one "Helder Sousa"; Edit shows 14 March. Screenshots `e2e-fixB2-05-overflow.png`, `e2e-fixB2-06-delete-confirm.png`, `e2e-fixB2-07-final-list.png`.
- Scratchpad path for all screenshots: `C:\Users\helde\AppData\Local\Temp\claude\78c429b059350e8f\15c50708-c550-4362-8273-50a7a757e7cf\scratchpad\`

### Test results
- Full suite (`Tests/MyVocaList.Tests.csproj`): 2007 passed, 0 failed, 0 skipped.
- Flaky-test check: `SongFormViewModelFrozenArtistTests.EditMode_LocalOnlySongAfterLoad_ArtistBecomesEditableAndNotifies` passed in the full run (run 1 of the check) and in three isolated runs of its class (18 of 18 each time). The earlier single failure did not reproduce, so no flakiness is established. No `.cs` change touched this test on the branch.

### Follow-up for Helder (not changed)
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` resolution-sheet and artist-match buttons still use `RelativeSource AncestorType={x:Type vm:SongFormViewModel}` (lines ~180 RemoveUrlCommand, ~302 OpenExistingSongCommand, ~377 UseExistingArtistCommand). They are likely affected by the same failure: option A did not fire inside the BottomSheet. Not verified on device; not changed here.

### Design concern
- The cause of the non-firing command binding inside `dx:BottomSheet` is not traced (no binding trace taken). The Clicked route works, so the touch reaches the button.

### Spec gap: none open for Fix B
Helder's option A/B choice was applied per the orchestrator fallback rule. Nothing blocking.

---
## Task: T5 — BUG-088 name-only singers + sheet cap (implementor)
**Plan:** Docs/Management/BusinessFeatures/persons/bugs/2026-11-19-BUG-088-duplicate-singer-saved-when-the-name-is-typed-without-picking-the-existing-one/design.md § Spec updated 2026-11-21
**Status:** To Review
**Started:** 2026-11-21
**Completed:** 2026-11-21

### Changed files:
- `Domain/RepositoryInterface/IPersonRepository.cs` (SameNameExistsAsync, CountSameNameAsync, GetSameNameAsync replace GetSameNameDifferentIdentityAsync)
- `Domain/ServicesInterfaces/IPersonService.cs` (ValidateContact removed; GetSameNamePersonsAsync returns SameNameResult)
- `Domain/ReadModels/SameNameResult.cs` (new record; spec said Contracts/Models, see the spec gap note in design.md)
- `Infra/EntityEFConfig/PersonConfiguration.cs` (CK_Persons_EmailOrBirthday removed; comment names the restore migration)
- `Infra/Migrations/20261121063641_PersonDropEmailOrBirthdayCheck.cs` + `.Designer.cs` (scaffolded)
- `Infra/Migrations/20261121063704_PersonIdentityUniqueIndexRestore.cs` + `.Designer.cs` (scaffolded; Up/Down filled by hand)
- `Infra/Migrations/AppDbContextModelSnapshot.cs` (regenerated by dotnet ef: CHECK removed)
- `Infra/Repository/PersonRepository.cs` (SameName queries; shared SameNameQuery; identity predicate unchanged)
- `Services/PersonService.cs` (either/or removed; IsNameOnly, HasContact; NamesakeNeedsContactMessage; SameNameListLimit = 3; GetSameNamePersonsAsync count + capped list)
- `MyVocaList/UI/ViewModels/PersonFormViewModel.cs` (either/or inline error removed; SameNameSheetTitle, SameNameMoreText, HasMoreSameName, CanSaveAsNewFromSheet)
- `MyVocaList/UI/Pages/People/PersonFormPage.xaml` (title binding; more-line Label; Save-as-new IsVisible; hint Label via InverseBoolConverter; comment)
- `Tests/Unit/Services/PersonServiceTests.cs` (REQ-DUP-03 and ValidateContact tests deleted; REQ-DUP-12/13/13a/09 tests added)
- `Tests/Unit/ViewModels/PersonFormViewModelBug088Tests.cs` (REQ-DUP-07/07a tests deleted; title, cap, name-only tests added)
- `Tests/Unit/ViewModels/PersonFormViewModelTests.cs` (fixture moved to the new service signature)
- `Tests/Integration/Repositories/PersonRepositoryTests.cs` (same-name query tests renamed and extended)
- `Tests/Integration/Repositories/PersonIdentityIndexTests.cs` (REQ-DUP-05 test deleted; REQ-DUP-14, 12, 04 name-only tests added)
- `Tests/Integration/Migrations/PersonBirthDayMonthMigrationTests.cs` (stale comment only)
- `Docs/Management/BusinessFeatures/persons/bugs/2026-11-19-BUG-088-duplicate-singer-saved-when-the-name-is-typed-without-picking-the-existing-one/design.md` (T5 implementation notes)
- `Docs/Management/BusinessFeatures/persons/bugs/2026-11-19-BUG-088-duplicate-singer-saved-when-the-name-is-typed-without-picking-the-existing-one/tasks.md` (T5 ticked; T6 untouched)
- `Docs/Management/BusinessFeatures/persons/bugs/2026-11-19-BUG-088-duplicate-singer-saved-when-the-name-is-typed-without-picking-the-existing-one/task-log.md` (this entry)

### Build notes
Build: clean net10.0-android (obj/bin deleted), then `dotnet build ... -t:Run` deploy: 0 errors, 3 pre-existing warnings | Tests: `dotnet test Tests/MyVocaList.Tests.csproj` 2026 passed, 0 failed, 0 skipped (re-run after the final comment-only test edit) | Commit SHA: this commit on `task/bug088-name-only-and-sheet-cap` (reported in the handback)
Files written and re-read: every changed `.cs`, `.xaml` section and doc section was re-read after its edit; CRLF line endings preserved.

### Migrations (verified)
- `dotnet ef migrations add` ran for `PersonDropEmailOrBirthdayCheck` (scaffolded) and `PersonIdentityUniqueIndexRestore` (scaffold, Up/Down filled by hand). Up = `DROP INDEX IF EXISTS IX_Persons_Identity;` then the 2026-11-20 `CREATE UNIQUE INDEX` SQL with the literal `NOCASE_NOACCENT`; Down = `DROP INDEX IF EXISTS`.
- `PersonIdentityIndexTests` (real SQLite, `MigrateAsync`) pass: `IX_Persons_Identity` exists after latest; the `People` SQL no longer contains `CK_Persons_EmailOrBirthday`.
- Emulator DB after a fresh launch: history ends with `PersonEmailOrBirthdayCheck`, `PersonIdentityUniqueIndex`, `PersonDropEmailOrBirthdayCheck`, `PersonIdentityUniqueIndexRestore`; `IX_Persons_Identity` present; CHECK absent.

### Emulator E2E (emulator-5554, pixel_5 AVD, clean net10.0-android build)
Note: the emulator DB was in a pre-CHECK state (history stopped at `PersonBirthDayMonth`, with a leftover name-only row `Helder Sousa`). The launch migration failed at `PersonEmailOrBirthdayCheck` (see the spec gap note in design.md). Backed up to the scratchpad as `emu_db_backup_before_t5_reset.db`, then deleted the app DB file so the full chain ran on a fresh DB. The app was not uninstalled.
- REQ-DUP-12: "Lia Namesake", name only, Save → saved, list shows the row with no email.
- REQ-DUP-13a / 09: second name-only "Lia Namesake" → sheet "A singer with this name already exists", row with Open, hint "To save a new singer with this name, enter an email or a birthday.", no Save as new, Cancel. Screenshot `t5-12-E2E-sheet-nameonly.png` (scratchpad).
- Email added → Save → sheet shows Save as new and no hint; Save as new saved the singer. Screenshot `t5-13-E2E-sheet-email.png`.
- Plural: name-only again with 2 matches → "2 singers with this name already exist", 2 rows, hint. Screenshot `t5-14-E2E-plural.png`.
- Cap: 4 same-name singers, name-only → "4 singers with this name already exist", 3 rows oldest first (ID 1, 2, 3), "…and 1 more. Find them in the Singers list.", hint, no Save as new. Screenshot `t5-17-E2E-cap.png`.
- Observation (not a blocker): at half-expanded height the more-line sits at the bottom edge of the sheet and is reached by scrolling the sheet content. Not changed (no spec asks for a layout change).
- Not exercised on device: the service rejection of a name-only namesake (REQ-DUP-13) is reachable only if the sheet is bypassed; covered by the service unit tests. Editing an existing singer onto a namesake's name was not driven on device (unit-tested).
- Test-driving mistakes during E2E (a name typed into the email field, and an autocomplete suggestion tap that opened an Edit page) saved nothing extra; the final DB holds the four intended rows.

### Acceptance-criteria traceability (T5 scope)
| AC | Criterion (short) | Implementation location | Test method(s) / evidence |
|----|-------------------|-------------------------|---------------------------|
| REQ-DUP-09 | Title singular/plural; list max 3, oldest first; more-line | `PersonFormViewModel.ShowSameNameSheet`; `PersonService.GetSameNamePersonsAsync`; `PersonRepository.GetSameNameAsync` (OrderBy Id, Take); `PersonFormPage.xaml` title and more-line | VM: `SaveCommand_OneSameNameSinger_SingularTitleAndNoMoreLine`, `SaveCommand_TwoSameNameSingers_PluralTitleWithCount`, `SaveCommand_FourSameNameSingers_ListsThreeAndShowsMoreLine`, `SaveCommand_ThreeSameNameSingers_NoMoreLine`; Service: `GetSameNamePersonsAsync_MoreThanLimit_ListsThreeAndReportsTotal`, `GetSameNamePersonsAsync_NoSameName_ReturnsEmptyWithoutListQuery`; Repo: `GetSameNameAsync_OrderedByIdAndTakeLimitsRows_CountIsFullTotal`; E2E t5-14, t5-17 |
| REQ-DUP-12 | Name-only singer accepted when no namesake; DB accepts the row | `PersonService.CreatePersonAsync` / `UpdatePersonAsync` (either/or step removed); `PersonConfiguration` (CHECK removed) | Service: `CreatePersonAsync_NameOnlyNoNamesake_Saves`, `UpdatePersonAsync_NameOnlyRenameWithoutOtherNamesake_Saves`; VM: `SaveCommand_NameOnlyNoNamesake_SavesWithoutSheet`; DB: `IdentityIndex_NameOnlyRow_IsAccepted`; E2E: first "Lia Namesake" saved |
| REQ-DUP-13 | Name-only create/update blocked by a namesake, before the identity check; update only when it creates the condition | `PersonService.CreatePersonAsync` and `UpdatePersonAsync` (namesake step, `IsNameOnly`, `HasContact`); `NamesakeNeedsContactMessage` | Service: `CreatePersonAsync_NameOnlyWithNamesake_ReturnsNamesakeMessageAndDoesNotSave`, `CreatePersonAsync_NameOnlyExactMatch_ReturnsNamesakeMessageNotDuplicate`, `CreatePersonAsync_WhitespaceEmailWithNamesake_ReturnsNamesakeMessage`, `CreatePersonAsync_WithEmailAndNamesake_DoesNotCheckNamesakeRule`, `UpdatePersonAsync_NameOnlyRenamedToNamesake_ReturnsNamesakeMessageAndDoesNotSave`, `UpdatePersonAsync_EmailClearedWhileNamesakeExists_ReturnsNamesakeMessageAndDoesNotSave`, `UpdatePersonAsync_AlreadyNameOnlyUnchangedName_SavesWithoutNamesakeCheck`, `UpdatePersonAsync_NameOnlyRenameWithoutOtherNamesake_Saves`; Repo: `SameNameExistsAsync_DifferentCaseAndAccent_ReturnsTrueUnlessExcluded`; E2E: sheet hint shown in place of Save as new (service path not driven on device) |
| REQ-DUP-13a | Name-only sheet hides Save as new, shows the hint, lists the exact name-only match; count includes it | `PersonFormViewModel.ShowSameNameSheet` (`CanSaveAsNewFromSheet`); `PersonService.GetSameNamePersonsAsync` (`includeIdentityMatch`); `PersonRepository.SameNameQuery`; `PersonFormPage.xaml` (IsVisible, hint Label) | VM: `SaveCommand_NameOnlyWithNamesake_SheetHidesSaveAsNew`, `SaveCommand_EditModeNameOnlyWithNamesake_SheetHidesSaveAnyway`, `SaveCommand_WithEmailAndNamesake_SheetKeepsSaveAsNew`, `SaveCommand_WithBirthdayOnlyAndNamesake_SheetKeepsSaveAsNew`; Service: `GetSameNamePersonsAsync_NameOnly_IncludesIdentityMatch`, `GetSameNamePersonsAsync_WithEmail_ExcludesIdentityMatch`; Repo: `GetSameNameAsync_NameOnlyIncludeIdentityMatch_ReturnsExactNameOnlyMatch`, `CountSameNameAsync_NameOnlyWithAndWithoutIdentityMatch_CountsAccordingly`; E2E t5-12, t5-13, t5-17 |
| REQ-DUP-14 | Migration drops the CHECK; `IX_Persons_Identity` exists after migrating to latest | Migrations `PersonDropEmailOrBirthdayCheck`, `PersonIdentityUniqueIndexRestore`; `PersonConfiguration` | Integration: `EmailOrBirthdayCheck_DroppedAfterMigrate_TableSqlHasNoCheck`, `IdentityIndex_ExistsInSqliteMasterAfterMigrate`; E2E: fresh device DB after launch (history, `IX_Persons_Identity` present, CHECK absent) |

REQ-DUP-04 stays covered: `IdentityIndex_TwoNameOnlyRowsSameName_IsRejected` was added; the four existing tests are unchanged.

### Withdrawn tests deleted (REQ-DUP-03, 05, 07, 07a)
- REQ-DUP-03: `CreatePersonAsync_NoEmailNoBirthday_...`, `UpdatePersonAsync_NoEmailNoBirthday_...`, `ValidateContact_EmailOnly_ReturnsValid`, `ValidateContact_BirthdayOnly_ReturnsValid`, `ValidateContact_NeitherEmailNorBirthday_ReturnsEitherOrMessage`.
- REQ-DUP-05: `PersonIdentityIndexTests.EmailOrBirthdayCheck_NeitherEmailNorBirthday_IsRejected`.
- REQ-DUP-07 and 07a: the `PersonFormViewModelBug088Tests` either/or section (four tests).

### Test-first note (process deviation, disclosed)
The Infra and Services production changes were written before their replacement tests in this task, so the Red step was not observed for T5. The replacement tests follow the spec test table and were confirmed Green. testing.md's Red-before-Green order was not followed for this change.

### Design concern
- `SameNameResult` placement: Contracts cannot reference Domain (Person). Implemented in `Domain/ReadModels/` as a Blocking: No assumption; see the spec gap note in design.md.

### Spec gap: CHECK migration on a pre-CHECK device with a name-only row (not blocking)
**Location:** `design.md § Spec updated 2026-11-21` (REQ-DUP-11 withdrawn clause in `requirements.md`)
**Gap description:** A device whose history stops at PersonBirthDayMonth and holds a name-only row fails at `PersonEmailOrBirthdayCheck` (the 2026-11-20 CHECK migration), so the "no cleanup" claim does not hold on that path.
**Options:**
- Option A: keep as is; the state does not occur on Helder's device (it applied the CHECK on 2026-11-21), no code change.
- Option B: add a cleanup or edit step before the CHECK migration; needs a spec decision.
**Recommendation:** Option A, unless a device in that state is known to exist.
**Blocking:** No, proceeding with Option A as the documented assumption.

### Evidence summary
- Build: 0 errors. Full test suite: 2026 passed, 0 failed.
- Emulator E2E: all four scenario groups observed; screenshots in the scratchpad (`t5-05`, `t5-06`, `t5-12` to `t5-17`).

---
## Task: BUG-088 T7 — No internal ID in display (REQ-DUP-15)
**Plan:** `tasks.md` T7 (this folder)
**Status:** To Review
**Started:** 2026-11-21
**Completed:** 2026-11-21

### Changed files:
- `Domain/Entity/Person.cs`
- `Tests/Unit/Domain/PersonBirthdayTests.cs`
- `Docs/Management/BusinessFeatures/persons/bugs/2026-11-19-BUG-088-duplicate-singer-saved-when-the-name-is-typed-without-picking-the-existing-one/task-log.md`
- `Docs/Management/BusinessFeatures/persons/bugs/2026-11-19-BUG-088-duplicate-singer-saved-when-the-name-is-typed-without-picking-the-existing-one/tasks.md`

### Verification evidence
- RED: `GetDisplayIdentifier_NoEmailNoBirthday_ReturnsEmpty` failed (`"(ID: 7)"` vs `""`); `GetDisplayName_NoEmailNoBirthday_ReturnsFullNameOnly` failed (`"John Doe (ID: 7)"` vs `"John Doe"`).
- GREEN: `PersonBirthdayTests` 17 passed, 0 failed.
- Full suite: `dotnet test Tests/MyVocaList.Tests.csproj` — 2027 passed, 0 failed.
- Grep for `(ID:` in all C# sources (excluding obj/bin): no remaining matches. No other test encoded the fallback; `PersonFormViewModelBug044Tests` has no ID assertions.
- Emulator (clean `obj/bin net10.0-android`, pixel_5 API 36, `com.myvocalist`): deployed and launched.
  - Created name-only singer "T7 NameOnly" (no email, no birthday). Saved.
  - Created a second "T7 NameOnly", Save → same-name sheet shows the row as "T7 NameOnly" only, no ID, no visible broken gap (`t7-05-samename.png`).
  - Full Name typed "T7" → suggestion row shows "T7 NameOnly" with no ID, but a blank band below the headline where the supporting line would be (`t7-06-suggest.png`). See Design concern.
  - Screenshots: `C:\Users\helde\AppData\Local\Temp\claude\78c429b059350e8f\15c50708-c550-4362-8273-50a7a757e7cf\scratchpad\t7-00-start.png` … `t7-06-suggest.png`.
  - Emulator stopped after the check. Test singer "T7 NameOnly" remains in the emulator DB (test data only).

### Design concern (report, not edited)
- Empty `SupportingText` in the suggestion popup (`AutocompleteField`, governed component) leaves an empty band under the headline. The sheet row (`ListItem`-style) renders cleanly. Not edited: governed component, per `component-change-governance.md`. Helder to decide whether a governed-component task is needed (e.g., collapse the supporting line when empty).

### Traceability
| AC | Criterion | Implementation | Test |
|----|-----------|----------------|------|
| REQ-DUP-15 | No email and no birthday shows no identifier | `Domain/Entity/Person.cs` `GetDisplayIdentifier` (returns `string.Empty`) | `PersonBirthdayTests.GetDisplayIdentifier_NoEmailNoBirthday_ReturnsEmpty` |
| REQ-DUP-15 | Name shown alone, no trailing space | `Domain/Entity/Person.cs` `GetDisplayName` | `PersonBirthdayTests.GetDisplayName_NoEmailNoBirthday_ReturnsFullNameOnly` |

Build notes: Build passed (0 errors; pre-existing EF1002 warning in `PersonIdentityIndexTests.cs`) | Tests: 2027 passed, 0 failed | Android deploy: passed.
Files written and re-read: `Domain/Entity/Person.cs`, `Tests/Unit/Domain/PersonBirthdayTests.cs`, `task-log.md`, `tasks.md`.

---
## Task: T8 — BUG-088 form flow regressions found on device (REQ-DUP-10 clarified, REQ-DUP-16)
**Plan:** Docs/Management/BusinessFeatures/persons/bugs/2026-11-19-BUG-088-duplicate-singer-saved-when-the-name-is-typed-without-picking-the-existing-one/tasks.md
**Status:** To Review
**Started:** 2026-11-21
**Completed:** 2026-11-21
**Branch:** `task/bug088-t8-form-flow` (from develop c81cf879). Not merged.

### Root cause (a): picked singer saved through the same-name sheet
- The edit baseline `_loadedName` is set only by `CompleteHydration()` on its first call (the Add-mode hydration, empty name). A pick loads the singer into the same form through the query (`NavigateToEditAsync`), with no new hydration, so the baseline stayed empty.
- Save with the unchanged name then counted as a change, and `GetSameNamePersonsAsync` ran. The service excluded the loaded id correctly, but the name-only namesake was still listed.
- Confirmed on device before the fix. The sheet listed only the other namesake, so the loaded id was excluded and the page was in edit mode; the stale value was the baseline. The sheet Save button was hidden by the name-only rule (REQ-DUP-13a), so the label Save as new / Save anyway could not be read on that screen.
- Fix: a `PersonIdRaw` write arms `_awaitingLoadedName`; the `PersonName` setter captures the next write as `_loadedName` (on every write, so an equal name is also captured). `CompleteHydration` keeps the first-call capture.

### Root cause (b): Open leaves the name suggestions active
- A pick ends the input: `AutocompleteDXField.OnSelectionChanged` writes the name, then unfocuses the editor and hides the keyboard. The Open path did neither, so the name field kept focus. The query then wrote the loaded name while focused, which the component reads as typed input (RC-B path, `RestartSearch`).
- Confirmed on device before the fix: after Open the name field kept the caret and the keyboard toolbar (screenshots `t8-b9-open-after-popup.png`, `t8-b10-later.png`). The suggestion popup itself was not captured in my runs, so the popup symptom is inferred from the focus path, not seen.
- Fix: `PersonFormPage.xaml.cs` `OnSameNameOpenClicked` calls `nameField.Unfocus()` before `OpenSameNameCommand`. `NavigateToEditAsync` now clears `Suggestions` (shared by the pick and the sheet Open, REQ-DUP-16). Note: `Suggestions` is not bound in the page XAML (the field uses `NameOptionSource`), so that clear is the shared-path step, not the device fix.

### Regression tests (Major, VM level) — red then green
- `PersonFormViewModelBug088Tests.SaveCommand_SingerLoadedIntoSameForm_NameUnchanged_DoesNotQuerySameName` — `// [AC] REQ-DUP-10`
- `PersonFormViewModelBug088Tests.OpenSameNameCommand_ClearsNameSuggestions` — `// [AC] REQ-DUP-16`
- RED (before the fix): `dotnet test --filter FullyQualifiedName~PersonFormViewModelBug088Tests`: 2 failed, 22 passed. Failures for the right reasons: Verify GetSameNamePersonsAsync Never failed (the same-name query ran), and `Assert.Empty` on Suggestions failed (one item left).
- GREEN: `--filter FullyQualifiedName~PersonFormViewModel`: 75 passed, 0 failed. Full suite `dotnet test Tests/MyVocaList.Tests.csproj`: 2037 passed, 0 failed.

### Changed files:
- `MyVocaList/UI/ViewModels/PersonFormViewModel.cs` (PersonIdRaw arms the baseline capture; PersonName is now an explicit property that captures the loaded name; OnPersonNameChanged is private; Suggestions cleared in NavigateToEditAsync; SuggestionSelectedAsync delegates)
- `MyVocaList/UI/Pages/People/PersonFormPage.xaml.cs` (OnSameNameOpenClicked unfocuses the name field before the command)
- `Tests/Unit/ViewModels/PersonFormViewModelBug088Tests.cs` (two regression tests above)
- `Docs/Management/BusinessFeatures/persons/bugs/2026-11-19-BUG-088-duplicate-singer-saved-when-the-name-is-typed-without-picking-the-existing-one/design.md` (three `Spec updated 2026-11-21 (T8 implementation)` notes)
- `Docs/Management/BusinessFeatures/persons/bugs/2026-11-19-BUG-088-duplicate-singer-saved-when-the-name-is-typed-without-picking-the-existing-one/tasks.md` (T8 ticked)
- `Docs/Management/BusinessFeatures/persons/bugs/2026-11-19-BUG-088-duplicate-singer-saved-when-the-name-is-typed-without-picking-the-existing-one/task-log.md` (this entry)

### Design concern
- The baseline capture depends on query order: personId must arrive before personName. `NavigateToEditAsync` and `PersonsViewModel` both send personId first, so this holds. A future route that sends personName first would leave a stale baseline on a reused page. Helder to decide whether to keep this or move to an order-free query property (`PersonNameRaw`), which would change the tested seam.

### Traceability
| AC | Criterion | Implementation location | Test method |
|----|-----------|-------------------------|-------------|
| REQ-DUP-10 | Loaded singer (pick or Open) is the baseline; unchanged name opens no sheet | `PersonFormViewModel.cs` `PersonIdRaw` setter, `PersonName` setter, `_awaitingLoadedName` | `SaveCommand_SingerLoadedIntoSameForm_NameUnchanged_DoesNotQuerySameName` |
| REQ-DUP-16 | Open clears name suggestions, the same as a pick | `PersonFormViewModel.cs` `NavigateToEditAsync` (`Suggestions = []`) | `OpenSameNameCommand_ClearsNameSuggestions` |
| REQ-DUP-16 | Open does not leave the name field active (device) | `PersonFormPage.xaml.cs` `OnSameNameOpenClicked` (`nameField.Unfocus()`) | Emulator E2E below (no unit seam for focus) |

### Emulator E2E (emulator-5554, AVD pixel_5_-_api_36_0_1, clean obj/bin net10.0-android, app not uninstalled; emulator stopped after)
Before the fix (original build):
- Pick `Helder Sousa` (name-only) → Save → same-name sheet "A singer with this name already exists", the other namesake `h2@x.com` listed, hint "To save a new singer with this name, enter an email or a birthday." (`t8-b3-after-save.png`). Bug (a) reproduced.
- Type `Helder Sousa` without a pick → Save → sheet → Open → Edit Singer with the name field still focused, caret and keyboard toolbar visible (`t8-b9-open-after-popup.png`, `t8-b10-later.png`).
After the fix (clean rebuild, install 17:15):
- Pick `Helder Sousa` (name-only) → title Edit Singer → Save → saved, back to the Singers list, no sheet (`t8-fa2-after-save.png`). Scenario (a) passes.
- Type `Helder Sousa` without a pick → Save → sheet with 2 rows → Open (first row) → Edit Singer, no caret, no keyboard toolbar, no popup (`t8-fb3-open-later.png`) → Save with the name unchanged → saved, back to the list, no sheet (`t8-fb4-save-after-open.png`). Scenario (b) passes on the focus and sheet path. Suggestions popup not seen before or after; the caret and keyboard change is what was observed.
- Screenshots are in the scratchpad folder `C:\Users\helde\AppData\Local\Temp\claude\78c429b059350e8f\15c50708-c550-4362-8273-50a7a757e7cf\scratchpad\` (t8-*.png).

Note for Helder: in the scenario (a) pre-fix run the sheet Save button was hidden (name-only form), so the reported label Save as new could not be read on that screen. In edit mode the label is Save anyway (`SameNameSaveButtonText`).

### Build notes
Build: passed (0 errors, app deploy `dotnet build -t:Run -f net10.0-android`) | Tests: 2037 passed, 0 failed | Commit SHA: see git log (this commit)
Files written and re-read: `PersonFormViewModel.cs` (PersonIdRaw, PersonName, OnPersonNameChanged, NavigateToEditAsync, SuggestionSelectedAsync), `PersonFormPage.xaml.cs` (OnSameNameOpenClicked), `PersonFormViewModelBug088Tests.cs` (two new tests), design.md (appended notes), tasks.md (T8 ticked), task-log.md (this entry).

## Task: BUG-088 T9 — suggestions reappear after Open when the drop-down was open at Save (REQ-DUP-16)
**Plan:** Docs/Management/BusinessFeatures/persons/bugs/2026-11-19-BUG-088-duplicate-singer-saved-when-the-name-is-typed-without-picking-the-existing-one/tasks.md
**Status:** blocked: repro not confirmed
**Started:** 2026-11-22
**Completed:** 2026-11-22
**Branch:** `worktree-agent-ab7380a3b56e22623` from develop 37a2a7c7 (T8 content present; old hash 8d43d71d not used)

### Outcome
No code change. The symptom did not reproduce on the emulator in four variants, so no root cause is confirmed. Per systematic-debugging, no fix was applied to an unreproduced cause. The VM regression test was not written (no fix to test). AutocompleteDXField was not edited.

### Evidence (emulator-5554, AVD pixel_5_-_api_36_0_1, clean obj/bin net10.0-android, unfixed develop build, app not uninstalled)
- Run A (Helder's path): Add, type `Helder Sousa` (drop-down open with one row, `r1-typed.png`), tap Save without blurring. Same-name sheet opens; the drop-down is not visible (`r2-after-save.png`). Open → Edit Singer, name field focused with caret, no drop-down at 4 s or 7 s (`r3-after-open.png`, `r4-later.png`).
- Run B (blurred control: tap blank area before Save): Save → Open → Edit Singer, caret on name field, no drop-down (`r5-control-after-open.png`).
- Run C (Cancel instead of Open): Save → sheet → Cancel → New Singer, name field refocused with caret, no drop-down (`r6-cancel-sheet.png`).
- Run D (blur by tapping blank area, then tap name field again): no drop-down (`r7-refocus.png`).
- Observations: the open drop-down is visible before Save and not visible after Save. Focus returns to the name field after the sheet closes, in both the blurred and unblurred variants, so focus alone does not separate them here.
- Not reproduced: the drop-down reappearing after Open with the prior list.
- Screenshots: `C:\Users\helde\AppData\Local\Temp\claude\78c429b059350e8f\15c50708-c550-4362-8273-50a7a757e7cf\scratchpad\` (r1..r7 .png).

### Code facts (read only)
- `AutocompleteDXField.IsDropDownOpen` is a public TwoWay bindable property. It is set false in ApplyMode, RestartSearch (rejected query), OnTextChanged (non-user change), and OnSelectionChanged (pick). OnUnfocused does not set it false.
- `PersonFormPage.xaml` does not bind `Suggestions`; the field is fed by `NameOptionSource`. So the VM's `Suggestions = []` in `NavigateToEditAsync` does not reach the popup.
- Open navigates with `../person-form?...` (`NavigateToEditAsync`, `PersonFormViewModel.cs`), which pops the form and pushes a new transient `PersonFormPage`.
- The earlier agent's notes cite `AutocompleteField.xaml.cs` with a mobile copy of Suggestions, and a stale-on-focus comment near line 55. Neither exists in this checkout (`MyVocaList/UI/Components/TextFields/` holds only `AutocompleteDXField.*`, `TapToEditField.*`). Those notes are not evidence.

### Open questions for Helder
1. On the device, does the drop-down stay visible above the same-name sheet after Save? On the emulator it is not visible once Save is tapped. Which device and keyboard?
2. Does the list reappear only after a keystroke, or after a tap that moves focus into the name field following Open? On the emulator, plain focus did not bring it back.
3. Decision needed: an instrumented diagnostic build (log `IsDropDownOpen`, `Suggestions`/`ItemsSource` at Save, sheet open, Open, and the new page's first Text write) run on the device, or defer.

### Candidate fix (not implemented)
If the device shows a stale open drop-down, the page/VM option is to close it when the same-name sheet opens and before Open navigates: a VM-bound `IsDropDownOpen` (TwoWay) set false in `SaveAsync` when the sheet is shown, and in `OpenSameNameAsync`. That is VM-testable. The `nameField.Unfocus()` from T8 is kept.

### Changed files:
- `Docs/Management/BusinessFeatures/persons/bugs/2026-11-19-BUG-088-duplicate-singer-saved-when-the-name-is-typed-without-picking-the-existing-one/task-log.md`

### Build notes
Build: deploy of the unfixed develop build succeeded (`dotnet build -t:Run -f net10.0-android`, exit 0). Tests: not run (no code change). Commit: docs only, see git log.

## Task: BUG-088 T9 (2026-11-22, fix) — name drop-down open at Save reappears after Open (REQ-DUP-16)
**Plan:** Docs/Management/BusinessFeatures/persons/bugs/2026-11-19-BUG-088-duplicate-singer-saved-when-the-name-is-typed-without-picking-the-existing-one/tasks.md
**Status:** To Review
**Started:** 2026-11-22
**Completed:** 2026-11-22
**Branch:** `worktree-agent-a0ee5d3810b12f260` from develop 67a0341b

### Outcome
Defensive fix, pre-authorised by Helder. The symptom (drop-down reappears after Open) was not reproduced on the emulator by the T9 repro run above, so no root cause is confirmed. The fix closes the name drop-down on the two paths the briefing named, through a bound VM property. `AutocompleteDXField` (governed) is unchanged.

### Changed files:
- `MyVocaList/UI/ViewModels/PersonFormViewModel.cs`
- `MyVocaList/UI/Pages/People/PersonFormPage.xaml`
- `Tests/Unit/ViewModels/PersonFormViewModelBug088Tests.cs`
- `Docs/Management/BusinessFeatures/persons/bugs/2026-11-19-BUG-088-duplicate-singer-saved-when-the-name-is-typed-without-picking-the-existing-one/design.md`
- `Docs/Management/BusinessFeatures/persons/bugs/2026-11-19-BUG-088-duplicate-singer-saved-when-the-name-is-typed-without-picking-the-existing-one/tasks.md`
- `Docs/Management/BusinessFeatures/persons/bugs/2026-11-19-BUG-088-duplicate-singer-saved-when-the-name-is-typed-without-picking-the-existing-one/task-log.md`

### Changes
- `PersonFormViewModel`: new `[ObservableProperty] bool _isNameDropDownOpen` (`IsNameDropDownOpen`). Set false in `ShowSameNameSheet` (Save with the drop-down open) and in `NavigateToEditAsync` (pick and sheet Open).
- `PersonFormPage.xaml`: `nameField` gets `IsDropDownOpen="{Binding IsNameDropDownOpen, Mode=TwoWay}"`.
- The optional "start of SaveAsync" reset was not added: the sheet path already clears it.
- T8's `nameField.Unfocus()` in `OnSameNameOpenClicked` is kept.

### Verification evidence
**Tests (REQ-DUP-16, `// [AC] REQ-DUP-16`):**
- `PersonFormViewModelBug088Tests.SaveCommand_SameNameSheetOpens_ClosesNameDropDown`
- `PersonFormViewModelBug088Tests.OpenSameNameCommand_ClosesNameDropDown`

**Red (re-run 2026-11-22):** the two reset lines (`IsNameDropDownOpen = false;`) were removed from `PersonFormViewModel.cs` in the working tree, keeping the property and the XAML binding. Both tests FAIL with `Assert.False() Failure, Expected: False, Actual: True`. Class result: 2 failed, 24 passed, 26 total. The earlier compile-stage red (CS1061 before the property existed) was recorded by the previous session and was not re-run.

**Green (re-run 2026-11-22):** the VM file was restored from the index (`git checkout`), which is the staged version.
- `PersonFormViewModelBug088Tests`: 26 passed, 0 failed.
- Full suite (`Tests/MyVocaList.Tests.csproj`): 2039 passed, 0 failed.
- Android build `MyVocaList/MyVocaList.csproj -f net10.0-android` after deleting `obj/Debug/net10.0-android` and `bin/Debug/net10.0-android`: 0 errors.

**Emulator E2E** (emulator-5554, AVD pixel_5_-_api_36_0_1, `com.myvocalist` versionName 0.3.0-alpha.0.72, deployed from this worktree build; screenshots in the session scratchpad as f1a-f1c, c1a-c1b, g1a-g1c):
- Main flow: title `New Singer`, drop-down open with `Helder Sousa` in the field, Save tapped without blur. The sheet "A singer with this name already exists" shows with no drop-down behind it. Open loads `Edit Singer` with no drop-down.
- Check 1: title `New Singer`, suggestion picked (title becomes `Edit Singer`), Save: no sheet, toast "Helder Sousa updated successfully!".
- Blurred variant: title `New Singer`, name typed, focus moved to Email (drop-down closed), Save: sheet with no drop-down. Open loads `Edit Singer` with no drop-down.
- The phone symptom (drop-down reappears after Open) was not reproduced on the emulator before or after the fix.

### Design concern
- The emulator shows no drop-down after Save with the sheet, even before the fix (T9 repro). The fix is defensive: it closes the drop-down state through a bound property, and it does not explain the phone symptom.

### Build notes
Build: passed (0 errors) | Tests: 2039 passed, 0 failed | Commit SHA: see the `fix: PersonForm` commit in git log.
Files written and re-read: `PersonFormViewModel.cs` (property at line ~99, `ShowSameNameSheet` ~429, `NavigateToEditAsync` ~517), `PersonFormPage.xaml` (line 29), `PersonFormViewModelBug088Tests.cs` (end of file), `design.md` (T9 note), `tasks.md` (T9 line).
