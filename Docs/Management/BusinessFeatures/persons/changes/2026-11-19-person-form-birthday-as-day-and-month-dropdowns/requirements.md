# Person birthday as day and month dropdowns: Requirements

> Change to the shipped `persons/requirements.md`. It supersedes AC-1.2 (Birthday field part), AC-1.8, AC-4.4 (Birthday part), the `BirthdayDayMonth` row of the Data Model and the four `BirthdayDayMonth` Validation Rules rows. Decisions: `README.md` (Helder, 2026-11-19).

## Scope

The free-text `DD/MM` birthday becomes two optional dropdowns, Day and Month. Values are stored as two nullable integers. There is no year. The calendar is Gregorian only, and all labels are in English.

## Acceptance Criteria

- **REQ-BDAY-01:** The Person form shall show the birthday as two DevExpress dropdowns side by side, in this order: `Birth month` (January to December, English names) and `Birth day` (1 to 31). It shall not show a free-text or masked birthday field.

> **Order changed 2026-11-21 (Helder):** month before day, so the day list is already scoped to the month.
- **REQ-BDAY-02:** The `Birth day` list shall contain only the days valid for the selected month: 31 for Jan/Mar/May/Jul/Aug/Oct/Dec, 30 for Apr/Jun/Sep/Nov, and 29 for February. When no month is selected, it shall contain 1 to 31.
- **REQ-BDAY-03:** When the user selects a month and the already-selected day is not valid for it (for example 31, then April), the day selection shall be cleared.
- **REQ-BDAY-04:** Each dropdown can be cleared, so the birthday stays optional.
- **REQ-BDAY-05 (service):** `ValidateBirthday(day, month)` shall accept both null. It shall reject exactly one null with "Select both day and month". It shall reject a month outside 1 to 12 with "Month must be between 1 and 12", and a day outside 1 to 31 or invalid for the month with "Invalid date for this month" (February allows 29).
- **REQ-BDAY-06 (form):** When Save is pressed with only one of day or month selected, the form shall show "Select both day and month" inline on the empty dropdown and shall not save.
- **REQ-BDAY-07 (persistence):** `Person` shall store `BirthDay` (`int?`) and `BirthMonth` (`int?`). The `BirthdayDayMonth` string column shall be removed. A database `CHECK` constraint shall enforce: both null, or (month 1 to 12 and day 1 to 31).
- **REQ-BDAY-08 (migration):** The migration shall convert each existing `BirthdayDayMonth` value of the form `DD/MM` (or `DDMM`) into `BirthDay` and `BirthMonth`. Any other value, or a pair that is not a valid date for its month (for example `30/02` or `31/04`), shall become null in both.
- **REQ-BDAY-09 (edit):** Opening an existing singer in edit mode (from the list or from a name-suggestion pick) shall pre-select their day and month.
- **REQ-BDAY-10 (display):** `Person.GetDisplayIdentifier()` shall render a birthday as `(15 Mar)`, with the day number and the English three-letter month, instead of `(15/03)`. The priority order (email, birthday, ID) is unchanged.

## Non-goals

- Year of birth.
- Other calendars (lunar, Buddhist-era, Persian, Hijri).
- Localized month names (a later multi-language task).
- A free-text birthday note.
- February always offers 29: no birth year is stored, so leap years cannot be computed (Helder, 2026-11-21: a year field would be a separate item).
