# Tasks: BUG-088 Duplicate singer

Spec: `requirements.md`, `design.md`. Depends on `changes/2026-11-19-person-form-birthday-as-day-and-month-dropdowns/` T1. Branch: `task/person-birthday-and-identity`.

- [x] **T1: Regression tests red.** Starts after the birthday change's T1 is committed. Write the REQ-DUP-01 Service test and the REQ-DUP-04 index test, and record that they fail.
- [x] **T2: Infra + Services.** The `CK_Persons_EmailOrBirthday` CHECK, the `PersonIdentityUniqueIndex` migration (raw SQL, created after any table rebuild), the repository `IdentityExistsAsync`/`GetSameNameAsync`, the service rules (REQ-DUP-01..03/06), and `GetSameNamePersonsAsync`. Update the superseded tests. Tests green.
- [x] **T3: UI.** VM either/or inline error, the same-name sheet flow and commands, then `PersonFormPage.xaml` `sameNameSheet` plus the code-behind sync. Tests: REQ-DUP-07..10. Depends on T2.
- [x] **T4: On-device gate (Helder).** Passed 2026-11-21, apart from the sheet title and cap and the new name-only decision (T5). Original text: Before installing, delete or edit leftover duplicates and any singer with neither an email nor a birthday, otherwise the migration fails at launch (REQ-DUP-11). Device checks: the either/or inline error, the duplicate error snackbar, and the same-name sheet (rows open the singer; Save as new or Save anyway; Cancel).

## Follow-up from the on-device test (2026-11-21)
- [x] **T5: Name-only singers + sheet cap** (`design.md § Spec updated 2026-11-21`). Infra (two migrations), Services, then the VM and XAML (one file at a time). Delete the withdrawn-AC tests and add the new ones. Emulator E2E: register a name-only singer; saving another name-only singer with the same name opens the sheet with no Save as new and the hint, and the service rejects it (REQ-DUP-13); adding an email makes Save as new appear and save; the sheet with 2 matches shows the plural title; with more than 3 it shows the more-line.
- [ ] **T6: On-device gate (Helder)** for T5. No data cleanup needed before installing.
- [x] **T7: No internal ID in display** (REQ-DUP-15). Change `Person.GetDisplayIdentifier()` so the fallback returns `string.Empty`, and make `GetDisplayName()` return just the `FullName` when the identifier is empty. Update the fallback test (now `REQ-DUP-15`, red first). Check that an empty `SupportingText` renders cleanly in the sheet row and in the suggestions on the emulator.
- [x] **T8: Form flow regressions found on device** (REQ-DUP-10 clarified, REQ-DUP-16).
  - (a) Add mode, type an existing name, pick the suggestion, Save: the sheet appears as if in Add mode. Expected: no sheet, because the name is unchanged from the picked singer.
  - (b) Add mode, type an existing name without picking, Save, sheet, Open: the singer loads, but the name suggestions open for the loaded name. Expected: no suggestions.
  - Regression tests (VM) red first.
