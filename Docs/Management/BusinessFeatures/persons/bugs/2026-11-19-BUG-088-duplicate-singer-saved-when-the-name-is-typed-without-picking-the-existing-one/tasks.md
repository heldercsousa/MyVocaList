# Tasks: BUG-088 Duplicate singer

Spec: `requirements.md`, `design.md`. Depends on `changes/2026-11-19-person-form-birthday-as-day-and-month-dropdowns/` T1. Branch: `task/person-birthday-and-identity`.

- [x] **T1: Regression tests red.** Starts after the birthday change's T1 is committed. Write the REQ-DUP-01 Service test and the REQ-DUP-04 index test, and record that they fail.
- [x] **T2: Infra + Services.** The `CK_Persons_EmailOrBirthday` CHECK, the `PersonIdentityUniqueIndex` migration (raw SQL, created after any table rebuild), the repository `IdentityExistsAsync`/`GetSameNameAsync`, the service rules (REQ-DUP-01..03/06), and `GetSameNamePersonsAsync`. Update the superseded tests. Tests green.
- [x] **T3: UI.** VM either/or inline error, the same-name sheet flow and commands, then `PersonFormPage.xaml` `sameNameSheet` plus the code-behind sync. Tests: REQ-DUP-07..10. Depends on T2.
- [ ] **T4: On-device gate (Helder).** Before installing, delete or edit leftover duplicates and any singer with neither an email nor a birthday, otherwise the migration fails at launch (REQ-DUP-11). Device checks: the either/or inline error, the duplicate error snackbar, and the same-name sheet (rows open the singer; Save as new or Save anyway; Cancel).
