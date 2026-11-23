# BUG-088 Duplicate singer: Design

Root cause: in insert mode, `PersonService.CreatePersonAsync` checks only that the email is unique. The single DB index, `IX_Persons_Name_Birthday`, applied only when a birthday was set, so the same name typed again without a pick was saved as a new row.

## Infra: `PersonConfiguration.cs`, `IPersonRepository` / `PersonRepository`, migration
- CHECK in the model: `t.HasCheckConstraint("CK_Persons_EmailOrBirthday", "Email IS NOT NULL OR BirthDay IS NOT NULL")`. This puts it in `EnsureCreated` schemas too. The Email column uses `TrimValueConverters.Optional` (`TrimForStorageOrNull`: empty or whitespace-only becomes null, verified 2026-11-20), so a blank email is stored as NULL and the CHECK sees it. REQ-DUP-05.
- **Expression unique index, migration-only** (EF `HasIndex` cannot express `COALESCE`). The precedent is the raw-SQL `SongSearch` objects, whose tests use `MigrateAsync`. The table is `People`:
  ```csharp
  migrationBuilder.Sql($"CREATE UNIQUE INDEX IX_Persons_Identity ON People (" +
      $"FullName COLLATE {CollationConstants.Default}, " +
      $"COALESCE(Email,'') COLLATE {CollationConstants.Default}, " +
      "COALESCE(BirthDay,0), COALESCE(BirthMonth,0));");
  ```
  - The `Down` method drops the index.
  - Add a comment in `PersonConfiguration` pointing to the migration, so nobody re-adds an equivalent model index. It also warns that any later migration that rebuilds `People` drops this index and must re-create it.
  - Guard: the REQ-DUP-04 test also asserts that `sqlite_master` contains `IX_Persons_Identity` after migrating to the latest migration. This fails if a later rebuild drops the index.
  - Migration name: `PersonIdentityUniqueIndex`. It is added after `PersonBirthDayMonth`. REQ-DUP-04/11.
  - If adding the CHECK makes EF rebuild the table, the rebuild drops raw-SQL indexes. Order the migration so `CREATE UNIQUE INDEX` runs **after** the rebuild, and verify the index exists after migrating (`sqlite_master`).
  > **Spec updated 2026-11-20:** one migration is not enough. EF's SQLite generator defers the `People` rebuild to the end of the migration, so a raw index in the same migration is dropped even when it is written after the CHECK. The CHECK is therefore in `PersonEmailOrBirthdayCheck` and the raw index in `PersonIdentityUniqueIndex` (a later migration, so the rebuild has already finished). Verified by the `IX_Persons_Identity` test after `MigrateAsync`.
  > **Spec updated 2026-11-20:** the migration uses the literal `NOCASE_NOACCENT`, not the `CollationConstants.Default` constant, so the migration stays frozen if the constant changes.
- Repository:
  - `Task<bool> IdentityExistsAsync(string fullName, string? email, int? day, int? month, int? excludeId, CancellationToken ct)`. Use `EF.Functions.Collate(p.FullName, CollationConstants.Default) == fullName`. Compare email with the same Collate and treat null as `''`: `(p.Email ?? "")`. Day and month compare with `?? 0`.
    > **Spec updated 2026-11-20:** `(p.Email ?? "")` is not translated correctly. The Email trim converter turns the empty constant into NULL, so the comparison never matches. A missing email is matched as `p.Email == null` instead (the converter stores blank as NULL, so `''` is never stored). Both repository queries share one private identity predicate; `GetSameNameDifferentIdentityAsync` excludes it as a subquery.
  - `Task<IReadOnlyList<Person>> GetSameNameDifferentIdentityAsync(string fullName, string? email, int? day, int? month, int? excludeId, CancellationToken ct)`. It returns the same-name persons (same collation, NoTracking) *excluding* the exact-identity match, which uses the same predicate as `IdentityExistsAsync`, negated.
  - Never `ToLower` or `ToUpper` in C# (constraints-registry HARD RULE).

## Services: `PersonService`
- Create and Update, in order:
  1. The existing validators.
  2. Either/or (REQ-DUP-03).
  3. Inside the UoW, the email-taken check (existing), then `IdentityExistsAsync` (REQ-DUP-01/02).
  4. Save.
- **Layering:** Services references no EF Core and no Microsoft.Data.Sqlite, and that stays true. Infra translates the exception:
  - The Infra `UnitOfWork` save path catches `DbUpdateException` whose inner exception is `SqliteException { SqliteErrorCode: 19 }` (SQLITE_CONSTRAINT).
  - It rolls back and rethrows a new Domain exception, `ConstraintViolationException : Exception` (in `Domain/Exceptions/`, holding the original as `InnerException`).
  - Before changing the UoW, the implementor greps every test and production `catch`/`ThrowsAsync<DbUpdateException>` that goes **through the UoW**, and updates or reports them. Repository tests that call `SaveChanges` directly are unaffected.
  - `PersonService` Create and Update catch `ConstraintViolationException` and return the duplicate tuple (REQ-DUP-06). This is the allowed "expected business failure → tuple" pattern. Log at Warning.
  - A CHECK violation or a racing email-unique violation also arrives here. Both are unreachable after steps 2 and 3, and the generic message is accepted for them (REQ-DUP-06).
- `Task<IReadOnlyList<Person>> GetSameNamePersonsAsync(string fullName, string? email, int? birthDay, int? birthMonth, int? excludeId, CancellationToken ct)` is the soft-warning query. It calls `GetSameNameDifferentIdentityAsync`. The decision to warn belongs to the VM flow: the service returns only same-name persons whose identity differs. Any exact match is excluded, because Save will reject it anyway.
- Messages are constants in `PersonService`, so the VM and the tests share them.

## UI: `PersonFormViewModel` + `PersonFormPage.xaml(.cs)`
- `SaveAsync`:
  1. Validators.
  2. Either/or inline error on Email (REQ-DUP-07). The rule is `IPersonService.ValidateContact(string? email, int? day, int? month)` → `(bool, string)`, the same pattern as the other `Validate*`, so the VM holds no rule. The birthday and either/or errors are applied independently (REQ-DUP-07a).
  3. If insert, or the name changed in edit mode, call `GetSameNamePersonsAsync(name, email, day, month, PersonId)`. If any results come back, fill `SameNameMatches` (`ObservableRangeCollection<AutocompleteSuggestion>` or a small record) and set `IsSameNameSheetVisible = true`, then return (REQ-DUP-09/10).
  4. Otherwise call `SaveCoreAsync()`.
- Commands:
  - `SaveAsNewCommand`: close the sheet, then `SaveCoreAsync()`. The button text binds to a VM property: `Save as new` in insert mode, `Save anyway` in edit mode.
  - `OpenSameNameCommand(match)`: close the sheet, then run the same navigation as `SuggestionSelectedAsync`. Extract a shared private method.
  - `DismissSameNameSheetCommand`.
- `ApplyAsyncFailureAsync`: the duplicate message goes to the error snackbar (it does not contain "Email", so the current routing already sends it there; assert this in a test). REQ-DUP-08.
- XAML: an in-page `dx:BottomSheet x:Name="sameNameSheet"` built on the `SongFormPage.xaml` `artistMatchSheet` pattern:
  - `IsModal`, grabber, `AllowDismiss`, `CornerRadius="28"`, `HalfExpandedRatio` around 0.5.
  - A title label.
  - A list of matches. Use `BindableLayout` or `DXCollectionView` per `constraints-reference § DevExpress/UI`, with a tap command for each row.
  - Buttons: `Save as new` (filled) and `Cancel`.
  - Code-behind syncs `IsSameNameSheetVisible` and `Show`/`Close` behind a syncing guard. A user dismissal calls `DismissSameNameSheetCommand`.
  - `ContentPage` keeps `SafeAreaEdges="All"` (text input).
  - Never `DisplayAlert`.

> **Spec updated 2026-11-20:** the same-name rows are `AutocompleteSuggestion` records (`Headline` = FullName, `SupportingText` = `GetDisplayIdentifier()`, `Data` = the `Person`), so the sheet reuses the existing suggestion model. The sheet is `HalfExpandedRatio="0.5"`. <!-- impl decision: reuse the suggestion row type rather than a new record -->
> **Spec updated 2026-11-20:** the edit-mode "name changed" baseline is the trimmed FullName captured once in `CompleteHydration()` (the first call only) and compared ordinal with the trimmed name at Save. <!-- impl decision: the loaded value must be captured when the query hydration ends, not on every OnAppearing -->
> **Spec updated 2026-11-20:** the either/or check runs after the Email format check and only sets the Email error when it fails, so it never clears a format error. The same-name query runs inside the busy window, so a second Save tap cannot open the sheet twice. <!-- impl decision: keep the Save flow re-entrancy guard across the async query -->
> **Spec updated 2026-11-20:** REQ-DUP-07a (both errors together) is asserted at the VM level with mocked rules. With the rule in `ValidateContact` (either/or fires only when day and month are both empty) and `ValidateBirthday` (fires only when exactly one of them is set), the two errors cannot occur together on real data. The independence is implemented, not a co-occurrence. <!-- impl decision: VM applies both errors independently; real-data exclusivity is a property of the rules -->

> **Spec updated 2026-11-21 (emulator E2E):** a same-name row is no longer tappable as a whole. Each row is a Grid: the Headline and SupportingText stack in column 0, and a `dx:DXButton` **Open** (`OutlinedButton` style, `Padding="16,0"`, `VerticalOptions="Center"`) in column 1, bound to `OpenSameNameCommand` with `CommandParameter="{Binding .}"`. The `TapGestureRecognizer` on the row is removed, because it did nothing on device. The shipped wiring is `Clicked="OnSameNameOpenClicked"` (option B). The handler in `PersonFormPage.xaml.cs` reads the row's `BindingContext` as `AutocompleteSuggestion` and calls `_viewModel.OpenSameNameCommand.Execute(match)`; the logic stays in the ViewModel. Why: the command bindings did not fire on device from inside `dx:BottomSheet` content. The `RelativeSource AncestorType` binding did nothing, and so did option A, a `Source={x:Reference page}` binding to `ViewModel.OpenSameNameCommand` (tried first and reverted). Option A was not a true fix because the command never ran, so the failure is in the command binding inside the sheet, not only in the ancestor walk. The cause is not traced further (no binding trace). <!-- impl decision: Clicked forwarder in code-behind (option B) because command bindings inside dx:BottomSheet content did not fire on device -->
> **Follow-up (not changed here):** `SongFormPage.xaml` resolution-sheet and artist-match buttons still use the `RelativeSource AncestorType` pattern (around lines 180, 302, 377). They are likely affected by the same failure; Helder to decide on the fix.

## Tests
| AC | Test |
|----|------|
| REQ-DUP-01 | `PersonServiceTests.CreatePersonAsync_IdentityExists_ReturnsDuplicateMessage` (Moq; **regression, red first**) |
| REQ-DUP-02 | `UpdatePersonAsync_OtherPersonHasIdentity_ReturnsDuplicateMessage` (Moq), plus a test that the person's own identity passes (excludeId). |
| REQ-DUP-03 | Create and Update with neither email nor birthday (Moq). |
| REQ-DUP-04 | `Tests/Integration/Repositories/PersonIdentityIndexTests.cs` (real SQLite, `MigrateAsync`): <ul><li>same name with different case or accents, plus null email and the same birthday, throws (**regression, red first**)</li><li>same name with a different email is allowed</li><li>same name + the same email in a different case is rejected (`IX_Persons_Identity`; the email part uses the default collation)</li><li>the index exists in `sqlite_master` after `MigrateAsync`</li></ul> |
| REQ-DUP-05 | Repository test: inserting a row with no email and no birthday throws. |
| REQ-DUP-06 | Service integration test (real SQLite plus the real UoW, like `SongSaveTrigramDuplicateTests`): seed a person, then wrap the real `IPersonRepository` in a decorator whose `IdentityExistsAsync` always returns `false`. Create the same identity, then assert `(false, DuplicateMessage, null)` and that no exception is thrown. |
| REQ-DUP-07/08/09/10 | `PersonFormViewModelTests`: the either/or inline error; same-name → sheet visible, no save; SaveAsNew → save called; Open → navigation; edit mode with an unchanged name → no query. |
| REQ-DUP-11 | Covered by the REQ-DUP-04 tests (the index exists after `MigrateAsync`). |

Existing tests whose AC is superseded must be updated as part of this spec change:
- `PersonRepositoryTests.AddAsync_SameNameNullBirthday_AllowsDuplicate`: now this test needs an email difference.
- `AddAsync_SameNameSameBirthday_ThrowsDbUpdateException`: now this case is covered by the identity index.
- ~~Any service or VM test that creates a person with neither email nor birthday: add an email.~~ Superseded 2026-11-21 by REQ-DUP-12.

## Spec updated 2026-11-21 (Helder, after the on-device test): name-only singers + sheet cap

REQ-DUP-03/05/07/07a are withdrawn; REQ-DUP-12/13/14 are new; REQ-DUP-09 is changed (see `requirements.md`).

### Infra
- `PersonConfiguration`: remove the `CK_Persons_EmailOrBirthday` `HasCheckConstraint`. Keep `CK_Persons_Birthday`.
- **Two migrations**, for the same reason as on 2026-11-20 (EF defers the `People` rebuild to the end of a migration, and the rebuild drops raw-SQL indexes):
  1. `PersonDropEmailOrBirthdayCheck`: scaffolded by `dotnet ef migrations add`. It drops the CHECK, and on SQLite that rebuilds `People`.
  2. `PersonIdentityUniqueIndexRestore`: an empty scaffold filled by hand.
     - `Up`: `DROP INDEX IF EXISTS IX_Persons_Identity;` then the same `CREATE UNIQUE INDEX` SQL as `PersonIdentityUniqueIndex`, with the literal `NOCASE_NOACCENT`.
     - `Down`: `DROP INDEX IF EXISTS IX_Persons_Identity;`.
  - Update the `PersonConfiguration` comment so it names the newest migration that creates the index.
- Repository: this is the contract, and there are no alternatives.
  - Keep `IdentityExistsAsync` as it is.
  - Add `Task<bool> SameNameExistsAsync(string fullName, int? excludeId, CancellationToken ct)`. It matches any other person with the same FullName (Collate default), whatever the identity.
  - Add `Task<int> CountSameNameAsync(string fullName, string? email, int? day, int? month, int? excludeId, bool includeIdentityMatch, CancellationToken ct)`.
  - Replace `GetSameNameDifferentIdentityAsync` with `Task<IReadOnlyList<Person>> GetSameNameAsync(string fullName, string? email, int? day, int? month, int? excludeId, bool includeIdentityMatch, int take, CancellationToken ct)`. It is NoTracking, ordered by `Id`, and returns at most `take` rows. With `includeIdentityMatch == false`, it excludes the exact identity match using the shared predicate, as today.
  - Update the callers and tests of the old name.

### Services
- Remove the either/or step from Create and Update, and remove `ValidateContact` from `IPersonService` and `PersonService`. Delete the `EitherOrMessage` constant if nothing else uses it.
- `IsNameOnly(email, day, month)` is a private `PersonService` helper. It is true when the email is null or whitespace and day and month are both null.
- **Create**, new step before the identity check: if `IsNameOnly` and `SameNameExistsAsync(name, null)`, return `NamesakeNeedsContactMessage` = "Another singer has this name. Enter an email or a birthday to save a new one." and do not save.
- **Update**, same step: it applies only when all three hold:
  - `IsNameOnly`;
  - the trimmed name differs (ordinal) from the stored `FullName`, or the stored person had an email or birthday;
  - `SameNameExistsAsync(name, id)`.
- In every other case, the identity check (step 3) and `DuplicateMessage` are unchanged.
- The `ConstraintViolationException` catch keeps returning `DuplicateMessage`. That race is unreachable, and the generic message is accepted.
- `Task<SameNameResult> GetSameNamePersonsAsync(string fullName, string? email, int? birthDay, int? birthMonth, int? excludeId, CancellationToken ct)` returns `SameNameResult(IReadOnlyList<Person> Matches, int TotalCount)`, a record in `Contracts/Models`.
  - It sets `includeIdentityMatch = IsNameOnly(email, birthDay, birthMonth)`.
  - It calls `CountSameNameAsync`. With a count of 0, it returns an empty result.
  - Otherwise it calls `GetSameNameAsync` with `take: SameNameListLimit` (`public const int SameNameListLimit = 3` on `PersonService`).

### UI
- VM: remove the either/or inline error and its `ValidateContact` call.
- `SameNameSheetTitle`, a string property set when the sheet opens: "A singer with this name already exists" when TotalCount is 1, otherwise $"{n} singers with this name already exist".
- `SameNameMoreText`: $"…and {n - PersonService.SameNameListLimit} more. Find them in the Singers list." It is empty when TotalCount ≤ SameNameListLimit.
- `HasMoreSameName` (bool) drives the label's `IsVisible`.
- `CanSaveAsNewFromSheet` (bool): true when the form has an email or a birthday. When it is false, the sheet hides the Save-as-new button and shows the REQ-DUP-13a hint label. The VM uses the same "has contact" input as the service call (email non-blank, or day/month set). That is only a display decision; the service enforces the rule.
- XAML: bind the sheet title label to `SameNameSheetTitle`, and add one `Label` under the list (`BodyMedium` style, OnSurfaceVariant) bound to `SameNameMoreText`, with `IsVisible` bound to `HasMoreSameName`.

### Tests (the ones listed here replace the deleted ones; every other existing test stays)
| AC | Test |
|----|------|
| REQ-DUP-12 | Service (Moq): Create with no email and no birthday, and no namesake, succeeds. Repository (real SQLite, `MigrateAsync` to latest): a row with no email and no birthday inserts. |
| REQ-DUP-13 | Service (Moq), one test per case:<ul><li>Create, name-only, a namesake has an email: `NamesakeNeedsContactMessage`, no save.</li><li>Create, name-only, an exact name-only namesake: `NamesakeNeedsContactMessage`, not `DuplicateMessage`.</li><li>A whitespace email counts as empty.</li><li>Update, name-only, name changed to a namesake's: rejected.</li><li>Update, the email is cleared while a namesake exists: rejected.</li><li>Update of an already name-only person, name unchanged: saves.</li><li>Update with excludeId: the person's own row is not a namesake.</li></ul> Repository (real SQLite): `SameNameExistsAsync` matches different case or accents and honours excludeId. |
| REQ-DUP-13a | VM: name-only form with a namesake → sheet open, `CanSaveAsNewFromSheet` false, in insert and in edit mode (Save anyway hidden). With an email → true. Service: name-only → `includeIdentityMatch: true` is passed to the count and list queries. Repository: `includeIdentityMatch: true` returns the exact name-only match; `false` excludes it. |
| REQ-DUP-14 | Migration test: after `MigrateAsync`, the `People` SQL in `sqlite_master` has no `CK_Persons_EmailOrBirthday`, and `IX_Persons_Identity` exists. |
| REQ-DUP-04 | `PersonIdentityIndexTests`: two name-only rows with the same name (different case) → the second throws. The existing `sqlite_master` guard after `MigrateAsync` still passes. |
| REQ-DUP-09 | VM: 1 match → singular title, no more-line. 2 matches → "2 singers…". TotalCount 4 with 3 matches → 3 rows and exactly "…and 1 more. Find them in the Singers list.", `HasMoreSameName` true. TotalCount 3 → no more-line. Service: passes take 3 and returns TotalCount. Repository: ordering by Id and take. |

**Delete (withdrawn ACs):** the tests tagged REQ-DUP-03, REQ-DUP-05, REQ-DUP-07 and REQ-DUP-07a. Delete the `ValidateContact` tests.

> **Spec updated 2026-11-21 (T5 implementation):** `SameNameResult` lives in `Domain/ReadModels/` (namespace `MyVocaList.Domain.ReadModels`), not `Contracts/Models/`. `MyVocaList.Contracts` does not reference `MyVocaList.Domain`, and the record holds `Person`, so the Contracts location cannot compile. <!-- impl decision: Contracts cannot reference Person; the Domain read-model folder is the existing home for the same-name read shapes -->
> **Spec updated 2026-11-21 (T5 implementation):** the sheet's more-line count is `TotalCount - Matches.Count`, which equals `TotalCount - SameNameListLimit` whenever the list is capped. The VM does not reference the concrete `PersonService.SameNameListLimit`. <!-- impl decision: keep the VM on the interface; the count is the same number -->
> **Spec updated 2026-11-21 (T5 implementation):** `CanSaveAsNewFromSheet` is evaluated when the sheet opens, from the form's email (non-blank) or birthday (either part set). It mirrors the service's `IsNameOnly`. The hint is a `Label` bound to `CanSaveAsNewFromSheet` through the existing `InverseBoolConverter`, so no extra VM property exists. <!-- impl decision: display-only mirror of the service rule; the service stays authoritative -->
> **Spec updated 2026-11-21 (T5 implementation):** the Update namesake check uses `HasContact(stored)` = stored email non-blank, or BirthDay or BirthMonth set, together with the trimmed-name change (ordinal) and `IsNameOnly` of the edit. <!-- impl decision: "the stored person had an email or birthday" read as any stored contact field -->
> **Spec updated 2026-11-21 (T5 implementation):** the migrations are `20261121063641_PersonDropEmailOrBirthdayCheck` (scaffolded, drops the CHECK) and `20261121063704_PersonIdentityUniqueIndexRestore` (hand-filled: `DROP INDEX IF EXISTS IX_Persons_Identity;` then the `CREATE UNIQUE INDEX` with the literal `NOCASE_NOACCENT`; `Down` drops the index). `PersonConfiguration` comment names the restore migration as the creator. <!-- impl decision: literal collation in the migration, as on 2026-11-20 -->
> **Spec gap (not blocking, found on the emulator 2026-11-21):** a device whose history stops at `PersonBirthDayMonth` (never applied `PersonEmailOrBirthdayCheck`) and holds a name-only row (the seeded `Helder Sousa` on the emulator) fails at launch in `PersonEmailOrBirthdayCheck` with `CHECK constraint failed: CK_Persons_EmailOrBirthday`. The withdrawn REQ-DUP-11 "no cleanup" note therefore does not hold for that path. Helder's device is past that step, so this blocks nothing now. Question for Helder: should that path get a cleanup step, or is the state unreachable in practice?

> **Spec updated 2026-11-21 (T8 implementation):** a loaded singer also becomes the edit baseline when it arrives in the same form after hydration. A pick or a sheet Open re-applies the query to the form, and the page instance stays (the name field kept focus after Open on device), so `CompleteHydration` alone leaves the baseline at the Add-mode empty name. The `PersonIdRaw` write arms the capture, and the next `PersonName` write sets `_loadedName`. The `PersonName` setter captures on every write, not only on a change, so a loaded name equal to the typed text still counts. This relies on the query order (personId before personName), which `NavigateToEditAsync` and `PersonsViewModel` both use. <!-- impl decision: query-driven baseline capture on pick and Open; the one-time CompleteHydration capture stays for the first hydration -->
> **Spec updated 2026-11-21 (T8 implementation):** the sheet's Open button unfocuses the name field before it runs `OpenSameNameCommand`, as a pick does (`AutocompleteDXField` unfocuses after `SelectCommand`). Without it the loaded name, written while the field has focus, reads as typing (the component's RC-B path). The governed `AutocompleteDXField` is unchanged. <!-- impl decision: page-level unfocus in the Clicked forwarder, no change to the governed component -->
> **Spec updated 2026-11-21 (T8 implementation):** the name suggestions are cleared in `NavigateToEditAsync`, which the pick and the sheet Open both call, instead of only in the pick handler (REQ-DUP-16). <!-- impl decision: one shared navigation path for pick and Open -->
> **Spec updated 2026-11-22 (T9, defensive fix):** the name field's drop-down state is a VM property, `IsNameDropDownOpen` (TwoWay to the governed `AutocompleteDXField.IsDropDownOpen`, bound from `PersonFormPage.xaml`; the component is unchanged). It is set false in `ShowSameNameSheet` (Save opens the sheet with the drop-down still open) and in `NavigateToEditAsync` (pick and sheet Open load a singer). The device symptom was not reproduced on the emulator (task-log, T9 2026-11-22), so this closes the drop-down defensively rather than fixing a confirmed root cause. <!-- impl decision: close the drop-down through a bound VM property, not a component change -->
