# Design: Venue duplicates are name and address together

Requirements: `requirements.md` (REQ-VEN-41 to 45).

## Approach

One query answers the rule. The two separate checks in `VenueService.SaveAsync` (name through
`GetByNameAsync`, address through `AddressExistsAsync`) are replaced by one call. Alternatives dropped:
keeping the two repository calls and combining their answers in the service (two venues could each
match one half, which is not a duplicate); a unique index (decision D3).

## Infra

- `IVenueRepository` / `VenueRepository`: new
  `Task<bool> NameAndAddressExistsAsync(string name, VenueAddress address, int? excludeId, CancellationToken ct = default)`.
  One `AnyAsync` over `Venues`: `Id != excludeId`, `Name == name`, and plain `==` on the six address
  columns, exactly as `AddressExistsAsync` compares them (null matches only NULL; the column collation
  ignores letter case and accents). No early return for an empty address: empty equals empty
  (REQ-VEN-44). No text is normalised in C#.
- `AddressExistsAsync` loses its only production caller. It is removed together with its tests if
  nothing else calls it; `GetByNameAsync` stays if another caller remains.
  > **Spec updated 2026-11-17:** `AddressExistsAsync` removed (no other caller). `GetByNameAsync` kept, but after this change its only callers are tests (`VenueRepositoryTests`, `UnitOfWorkCompositionTests`); flagged for Helder to decide whether to remove it.

## Services

- `VenueService.SaveAsync`: after the "Venue not found" check, one call to
  `NameAndAddressExistsAsync(trimmedName, address, request.Id, ct)`. On true:
  `(false, <message>, VenueField.Name)`, with the message chosen by `VenueAddressRules.IsEmpty(address)`
  (REQ-VEN-41).
- `CreateVenueAsync(name)` and `UpdateVenueAsync(id, newName)`: the same call with
  `VenueAddress.Empty` on create and the stored venue's address on rename (REQ-VEN-45), same messages.

## ViewModel and page

No change. The form already shows a `VenueField.Name` failure on the Name field.

## Testing

| Area | Level | Tests |
|------|-------|-------|
| `NameAndAddressExistsAsync` | B | Real SQLite: equal pair true; letter case and accents ignored; same name other address false; same address other name false; both empty true; one empty false; own id excluded |
| `VenueService` (REQ-VEN-41 to 45) | A | Unit, Moq: refused with each message; allowed cases save; edit excludes own id; name-only paths pass the right address |

Existing tests that assert the replaced rules (REQ-VEN-19, 20, 21: name-only refusal, address-only
refusal, the `AddressExistsAsync` tests) are replaced by tests of REQ-VEN-41 to 45. Each replaced or
removed test is listed in `task-log.md` with the requirement that replaces it.

## Device check (Helder)

Save two venues with the same name at different addresses (allowed); save a venue with another name at
an existing address (allowed); save the same name at the same address (refused, message on Name).
