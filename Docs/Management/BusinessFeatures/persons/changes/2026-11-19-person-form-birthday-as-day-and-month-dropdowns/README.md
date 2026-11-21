---
id: person-form-birthday-as-day-and-month-dropdowns
title: Person form birthday as day and month dropdowns
status: ✅ Done
target: 2026-11-19
section: BusinessFeatures
parent: persons
goal: Replace the free-text birthday with two dropdowns (day, month) in English, no year, stored as two nullable integers; Gregorian only, other calendars are a non-goal.
gate: Helder passes the dropdowns on device.
closed: 2026-11
kind: change
---

# Person form birthday as day and month dropdowns

Replace the free-text birthday with two dropdowns (day, month) in English, no year, stored as two nullable integers; Gregorian only, other calendars are a non-goal.


## Origin

F3 of `changes/2026-11-06-person-form-follow-ups-from-acf-t7-run-2/`. Helder, who knows the field, still typed MM/YYYY instead of DD/MM, and other locales order dates differently.

## Decisions (Helder, 2026-11-19)

- **Two dropdowns, not a date editor.** Do not use the DX date editor. Use a DevExpress dropdown editor (confirm `DXComboBoxEdit` at the `.csproj` version via the `devexpress-maui` MCP) so the UI Component Priority rule holds.
- **Month:** Jan to Dec in English. **Day:** 1 to 31 limited by the chosen month; February allows 29.
- **No year.** Stored as two nullable integers, `BirthDay` and `BirthMonth`. Both or neither (Service rule plus a database `CHECK`).
- **Gregorian only.** Lunar, Buddhist-era, Persian and Hijri calendars are an explicit non-goal. The admin enters the date, and it is an identity tag, so consistency matters, not calendar correctness. A free-text birthday note for traditional dates is a possible later addition, not part of this item.
- **English only for now.** A later multi-language task will cover all app labels, including the month names.
- **Migration:** converts the existing `Birthday` column in the simplest way (single user, single device).
- **Feeds** `bugs/2026-11-19-BUG-088-*`: the duplicate-singer index uses these columns.
