# Requirements: Venue duplicates are name and address together

> **Status: APPROVED 2026-11-17.** The rule is Helder's own words after his device check of 2026-11-17:
> "the key to refuse is venue name+address: a same-name venue may have 1+ addresses, so name may be
> duplicated, but name+address not … No problem having 2 venues with different names having the same
> address". Decisions Claude took for him are listed under "Decisions" so he can overrule them.

Predecessor spec (shipped, not edited):
`../2026-11-14-venue-picture-leading-image-on-list-rows-add-replace-and-remove-in-the-form/requirements.md`
(REQ-VEN-19, 20, 21).

## Goal

MyVocaList stores one address per venue. A company with several locations is stored as several venues
with the same name, one per address. Two companies may share one address. So neither the name alone
nor the address alone identifies a venue; the pair does.

## Acceptance criteria

- **REQ-VEN-41: Duplicate name and address refused.** (Helder)
  IF another venue has an equal name **and** all six address fields equal (a blank field equals only a
  blank field; an empty address equals an empty address), THEN Save is refused, nothing is stored, and
  the Name field shows:
  - "There is another venue registered with this name at this address" when the address is not empty;
  - "There is another venue registered with this name" when the address is empty.
  In edit mode the venue being edited is excluded. Name and address are compared under the database
  collation (letter case and accents ignored), as the two old rules were.
  - Given venue A "Jazz Club" at ("Rua Augusta, 100", "", "São Paulo", "SP", "", "Brasil"), When a new
    venue "jazz club" is saved at ("rua augusta, 100", "", "Sao Paulo", "sp", "", "brasil"), Then Save
    is refused.
- **REQ-VEN-42: The same name at another address is allowed.** (Helder)
  Given venue A "Jazz Club" at an address, When a venue "Jazz Club" is saved at a different address
  (any of the six fields differs) or with an empty address, Then it is saved.
- **REQ-VEN-43: The same address under another name is allowed.** (Helder)
  Given venue A "Jazz Club" at an address, When a venue "Blues Bar" is saved at the same six address
  fields, Then it is saved.
- **REQ-VEN-44: Two venues without an address cannot share a name.**
  Given venue A "Jazz Club" with an empty address, When a venue "Jazz Club" with an empty address is
  saved, Then Save is refused with "There is another venue registered with this name" (REQ-VEN-41: the
  pair is equal).
- **REQ-VEN-45: Name-only save paths follow the same rule.**
  The service methods that save a name without an address apply REQ-VEN-41 with the address the venue
  will have after the save: an empty address on create, the stored address on rename.

## Decisions (Claude, for Helder to overrule)

| # | Question | Decision | Why |
|---|----------|----------|-----|
| D1 | Which field carries the message? | Name | One message, one field; the name is the first thing the user would change |
| D2 | Two address-less venues with one name | Refused (REQ-VEN-44) | The pair is equal; otherwise the list would show two rows that cannot be told apart |
| D3 | Enforced where? | `VenueService`, no unique index | Same reason as V7 of the predecessor: development databases hold duplicate seeded names; single device |
| D4 | Existing data | Untouched | Rows that were valid under the old rules stay valid; the new rule is weaker for names and for addresses |

## Supersedes (shipped specs are not edited)

| Shipped requirement | Change |
|---------------------|--------|
| REQ-VEN-19 (duplicate name refused), AC-1.6, AC-3.4 | **Replaced** by REQ-VEN-41, 42, 44. |
| REQ-VEN-20 (duplicate address refused, message on Street address) | **Replaced** by REQ-VEN-41, 43. |
| REQ-VEN-21 (an empty address never collides) | **Replaced** by REQ-VEN-44 for venues with an equal name. |

## Out of scope

- Several addresses for one venue.
- A database unique index.
- The map picker and the address fields themselves.
