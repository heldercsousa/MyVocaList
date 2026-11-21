# Task log: Singers list shows birthday and email instead of participations

---
## Task: T1 — DTO IdentityLine + PeoplePage binding
**Plan:** Docs/Management/BusinessFeatures/persons/changes/2026-11-21-singers-list-shows-birthday-and-email-instead-of-participations/README.md
**Status:** To Review
**Started:** 2026-11-21
**Completed:** 2026-11-21

### Changed files:
- `Contracts/Models/PersonListItemDto.cs`
- `Tests/Unit/Contracts/PersonListItemDtoTests.cs` (new)
- `MyVocaList/UI/Pages/People/PeoplePage.xaml`
- `Docs/Management/BusinessFeatures/persons/changes/2026-11-21-singers-list-shows-birthday-and-email-instead-of-participations/tasks.md`
- `Docs/Management/BusinessFeatures/persons/changes/2026-11-21-singers-list-shows-birthday-and-email-instead-of-participations/task-log.md` (new)
- `MyVocaList.sln` (registers task-log.md in the change's Solution Folder)

### Build notes
Build: passed (0 errors; MAUI android build, clean obj/bin net10.0-android) | Tests: 2035 passed, 0 failed (full suite; 8 new PersonListItemDto tests) | Commit SHA: see git log on `task/singers-list-identity-line`

### Design notes
- REQ-PLIST-03: `BirthDay`, `BirthMonth` and `Email` on `PersonListItemDto` are auto-properties with no change notification, so no `PropertyChanged` for `IdentityLine` was added there (the brief says not to refactor them). The AC is met by the list reload after an edit, as the README allows.
- `IdentityLine` is `[JsonIgnore]`, like `ParticipationsAbsencesNumber`. Month abbreviation uses `CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName`. The email is not trimmed in the output; only blank or whitespace emails count as absent.
- `ParticipationsAbsencesNumber` and the `Participations`/`Absences` properties stay on the DTO and are untouched. The `ListItem` governed component was not edited.

### Verification evidence
- Tests written first (RED): `dotnet build` of Tests failed with CS1061 (`IdentityLine` not defined), as expected.
- GREEN: `PersonListItemDtoTests` 8 passed; full suite 2035 passed, 0 failed.
- Emulator (pixel_5_-_api_36_0_1, app com.myvocalist, clean obj/bin first): Singers list rows observed:
  - both values: "Carlos Both" — `14 Mar · carlos@example.co…` (long email truncated with ellipsis at line end). Screenshot: `scratchpad/t8-singers-all.png`
  - email only: "Ana Email" — `ana@example.com`. Screenshot: `scratchpad/t8-singers-all.png`
  - birthday only: "Bia Birthday" — `14 Mar`. Screenshot: `scratchpad/t8-singers-all.png`
  - name-only: "Helder Sousa" — no supporting line; the row is a single-line row, visibly more compact than the two-line rows, with no blank gap. Screenshots: `scratchpad/t8-singers-all.png`, `scratchpad/t8-selected.png`
  - Selected-item template (checkbox ticked on the name-only row) renders correctly with no supporting line. Screenshot: `scratchpad/t8-selected.png`
  - No participation/absence counts shown on any row.
- Emulator data: three test singers were added through the New Singer form (Ana Email, Bia Birthday, Carlos Both). A system "Bluetooth keeps stopping" dialog appeared at launch; it is unrelated to the app and was dismissed with "Close app".
- Emulator stopped after the check.

### AC traceability matrix
| AC ID | Criterion | Implementation location | Test method |
|-------|-----------|-------------------------|-------------|
| REQ-PLIST-01 | Both values → "{day} {Mon} · {email}" | `Contracts/Models/PersonListItemDto.cs` `IdentityLine` | `PersonListItemDtoTests.IdentityLine_BirthdayAndEmail_JoinsWithMiddleDot` |
| REQ-PLIST-01 | Birthday only → "{day} {Mon}" | same | `PersonListItemDtoTests.IdentityLine_BirthdayOnly_IsBirthday` |
| REQ-PLIST-01 | Email only → email | same | `PersonListItemDtoTests.IdentityLine_EmailOnly_IsEmail` |
| REQ-PLIST-01 | Neither → "" | same | `PersonListItemDtoTests.IdentityLine_NeitherBirthdayNorEmail_IsEmpty` |
| REQ-PLIST-01 | Whitespace email treated as absent | same | `PersonListItemDtoTests.IdentityLine_WhitespaceEmail_TreatedAsAbsent`, `IdentityLine_WhitespaceEmailWithBirthday_IsBirthdayOnly` |
| REQ-PLIST-01 | Single-digit day, no leading zero ("5 Dec") | same | `PersonListItemDtoTests.IdentityLine_SingleDigitDay_HasNoLeadingZero` |
| REQ-PLIST-01 | English month via InvariantCulture | same | `PersonListItemDtoTests.IdentityLine_MonthAbbreviation_IgnoresCurrentCulture` |
| REQ-PLIST-02 | SupportingText binds IdentityLine in both templates; participations removed from list | `MyVocaList/UI/Pages/People/PeoplePage.xaml` (lines 40, 58) | Emulator check (screenshots above) |
| REQ-PLIST-03 | IdentityLine refreshes on edit | No notification added (auto-properties); list reload after edit | Emulator check not run for edit; covered by reload design (see Design notes) |

### Design concern
- None.
