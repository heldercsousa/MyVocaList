# Tasks: Person birthday as day and month dropdowns

Spec: `requirements.md`, `design.md`. Order follows the DRY Onion. One branch is shared with BUG-088: `task/person-birthday-and-identity`.

- [x] **T1: Domain + Infra + Services.** `Person.BirthDay`/`BirthMonth` and the display format, `PersonConfiguration` with the CHECK, the `PersonBirthDayMonth` migration with data conversion, `ValidateBirthday(int?, int?)`, `DaysInMonth`, the Create/Update signatures, and the DTO. Tests: REQ-BDAY-05/07/08/10.
- [x] **T2: UI.** VM `Days`/`Months`/selection/validation/hydration, the query-param navigations, then `PersonFormPage.xaml` with the two `ComboBoxEdit`s (one XAML file → build). Tests: REQ-BDAY-02/03/06/09. Depends on T1.
- [ ] **T3: On-device gate (Helder).** REQ-BDAY-01/04: the two dropdowns render, the clear icon empties each one, February offers 29 days, choosing April clears day 31, and the edit form pre-selects the stored day and month.
