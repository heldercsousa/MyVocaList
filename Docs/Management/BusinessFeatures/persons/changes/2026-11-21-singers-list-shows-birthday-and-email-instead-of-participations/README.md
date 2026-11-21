---
id: singers-list-shows-birthday-and-email-instead-of-participations
title: Singers list shows birthday and email instead of participations
status: ✅ Done
target: 2026-11-21
section: BusinessFeatures
parent: persons
goal: Each row in the Singers list shows the singer's birthday and email under the name, so namesakes can be told apart; participation and absence counts leave the list.
gate: Helder passes the list on device.
closed: 2026-11
kind: change
---

# Singers list shows birthday and email instead of participations

Each row in the Singers list shows the singer's birthday and email under the name, so namesakes can be told apart; participation and absence counts leave the list.


## Decisions (Helder, 2026-11-21)

- The participations and absences line is removed from the Singers list rows. It isn't useful there.
- The second line shows birthday, then email, on a single line: `14 Mar · helder@example.com`.
  - The birthday reads `14 Mar`, the same as the same-name sheet and the name suggestions.
  - If only one of the two exists, the line shows just that one.
  - If neither exists, the line is empty.
  - A long email truncates at the end of the line.
- Text only for now. Cake and mail icons are a possible follow-up.

## Requirements (light ceremony: 1 DTO property + 1 XAML file)

- **REQ-PLIST-01:** `PersonListItemDto.IdentityLine` returns `"{day} {Mon}"` for the birthday, with the English three-letter month from InvariantCulture, then `" · "`, then the email. With only one of them, it returns that one alone. With neither, it returns `""`. A blank or whitespace email counts as absent.
- **REQ-PLIST-02:** `PeoplePage.xaml` binds `SupportingText` to `IdentityLine` in both the normal and the selected item template. `ParticipationsAbsencesNumber` is no longer shown in the list. The property stays on the DTO, and any other consumer is untouched.
- **REQ-PLIST-03:** When `IdentityLine` changes (Email, BirthDay or BirthMonth set), the DTO raises `PropertyChanged` for `IdentityLine`, so an edited row refreshes. This applies only if those setters already raise notifications; if they don't, the list reloads after an edit and this AC is met by the reload.

## Tests
- REQ-PLIST-01: unit tests on the DTO (both values, birthday only, email only, neither, whitespace email, single-digit day).
- REQ-PLIST-02/03: UI. Emulator check, then the on-device gate.
