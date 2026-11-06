---
id: person-form-follow-ups-from-acf-t7-run-2
title: Person form follow-ups from ACF T7 run 2
status: 💡 Pending
target: 2026-11-06
section: BusinessFeatures
parent: persons
goal: "Fix three Person-form issues Helder found on device 2026-11-06: menu reappears on edit load and after a pick, duplicate singer saved without a pick, confusing birthday field."
gate: Helder passes each fix on device.
kind: change
---

# Person form follow-ups from ACF T7 run 2

Fix three Person-form issues Helder found on device 2026-11-06: menu reappears on edit load and after a pick, duplicate singer saved without a pick, confusing birthday field.


## Origin

Helder, on-device, 2026-11-06, during the T7 re-run (run 2) of
`DevCycleCraft/autocomplete-component/changes/2026-09-24-shared-autocomplete-field-pick-lock-via-x-centralised-behaviour/`.
Ruling: record these as tasks nested under the Person feature and tackle them in a later session.

## Tasks (not started; triage severity and spec each before fixing)

- [ ] **F1: autocomplete menu reappears on an edit-mode load and after a pick.** It still reproduces after
  ACF T8, which added "programmatic text closes the menu" (REQ-ACF-24/25). The Song form Artist fix is
  the reference (`SongFormViewModel` `IsArtistDropDownOpen = false` on every programmatic assignment,
  plus `SelectedItem = null` in `SongFormPage.xaml.cs` `OnAppearing`). The fix lives in the shared
  component `MyVocaList/UI/Components/TextFields/AutocompleteDXField.xaml.cs`. If a second page binds to
  it by then, the component is governed and the four gates apply (`.claude/rules/component-change-governance.md`).
- [ ] **F2: duplicate singer saved.** In insert mode, typing an existing name without picking it from
  the menu, then Save, creates a second singer with the same name + birthday + email. The duplicate
  rule must be enforced on Save (Service layer). Triage as a bug: likely Major (data integrity), which
  needs a regression test.
- [ ] **F3: birthday field is confusing and locale-bound.** Helder, who knows the field, still tried to
  type MM/YYYY instead of DD/MM, and other locales (e.g. Chinese) order dates differently. Needs a UX
  decision (input format, picker, locale-aware mask, or a different action) before any spec.
