# BUG-088 Duplicate singer: Requirements

> This changes the shipped `persons/requirements.md`. It removes the "Composite key enforcement at service level", which was deferred to Option C under Out of Scope, and the "Future either/or consideration" note, because both are now in scope. The `IX_Persons_Name_Birthday` index row is replaced by `IX_Persons_Identity`. Decisions are in `README.md` (Helder, 2026-11-19). This depends on `changes/2026-11-19-person-form-birthday-as-day-and-month-dropdowns/` for the `BirthDay`/`BirthMonth` columns.

## Identity

A singer's identity is **FullName + Email + (BirthDay, BirthMonth)**. FullName and Email are compared with the default collation (`CollationConstants.Default`, case- and accent-insensitive), with no C# normalization. A missing email or birthday counts as the empty value for comparison, so two namesakes who both have no email and the same birthday are duplicates.

## Acceptance Criteria

- **REQ-DUP-01 (service, create):** `CreatePersonAsync` shall return `(false, "This singer is already registered.", null)` when another person has the same identity, and shall not save.
- **REQ-DUP-02 (service, update):** `UpdatePersonAsync` shall return `(false, "This singer is already registered.")` when a *different* person, excluding the one being edited, has the resulting identity.
- **REQ-DUP-03 (service, either/or):** Create and Update shall return `(false, "Enter an email or a birthday.", …)` when both the email and the birthday are empty, and shall not save.
- **REQ-DUP-04 (database backstop):** A unique index `IX_Persons_Identity` on `(FullName COLLATE default, COALESCE(Email,'') COLLATE default, COALESCE(BirthDay,0), COALESCE(BirthMonth,0))` shall reject a duplicate identity, even if the service check is bypassed.
- **REQ-DUP-05 (database either/or):** A `CHECK` constraint `CK_Persons_EmailOrBirthday` shall reject a row where Email is null and BirthDay is null.
- **REQ-DUP-06 (exception mapping):** When the save hits a unique-constraint violation (SQLite error 19, `SQLITE_CONSTRAINT`), for example in a race the service pre-check missed, Create and Update shall return the REQ-DUP-01/02 message instead of throwing. This also covers a racing `IX_Persons_Email` violation: a generic duplicate message is accepted for that unreachable case.
- **REQ-DUP-07 (form, either/or):** When Save is pressed with both email and birthday empty, the form shall show "Enter an email or a birthday." inline on the Email field and shall not call the service.
- **REQ-DUP-07a (precedence):** The either/or error and the birthday one-of-two error (REQ-BDAY-06) are independent, so both can show at the same time.
- **REQ-DUP-08 (form, duplicate):** When the service returns the REQ-DUP-01/02 message, the form shall show it as an error snackbar and stay on the page.
- **REQ-DUP-09 (soft warning):** When Save is pressed with all fields valid and one or more *other* persons share the same FullName (default collation) but not the same identity, the form shall open a bottom sheet titled "A singer with this name already exists". It lists each same-name singer (name and `GetDisplayIdentifier()`) and offers:
  - Tapping a listed singer opens that singer in edit mode, the same as a name-suggestion pick (AC-2.3).
  - **Save as new** (insert mode), labelled **Save anyway** in edit mode, saves the form as a new singer or as the edit.
  - **Cancel**, or dismissing the sheet, closes it and stays on the form without saving.
- **REQ-DUP-10:** The soft warning shall never block. In edit mode, it shall appear only when the FullName was changed from the loaded value.
- **REQ-DUP-11 (migration):** The migration shall create the index and the CHECK with no dedupe logic. If a duplicate or an either/or violation remains, the migration fails. Before installing, Helder deletes or edits any such leftover rows on the device.

## Regression tests (Major)

These tests must fail before the fix and pass after it:
- REQ-DUP-01: a Service test with Moq.
- REQ-DUP-04: an index test on a real SQLite file using `MigrateAsync`, because an expression index is a migration-only object. Red = the duplicate row is inserted when the schema is migrated only up to `PersonBirthDayMonth`, which is a behavioural failure, not a compile error. Green = the insert throws once `PersonIdentityUniqueIndex` is applied.

These tests start only after the birthday change's T1 is committed on the shared branch.
