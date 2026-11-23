---
id: BUG-088
title: Duplicate singer saved when the name is typed without picking the existing one
status: ✅ Done
severity: Major
target: 2026-11-19
section: BusinessFeatures
parent: persons
goal: "A singer with the same name, email and birthday must never be saved twice: enforced in the Person service and by a database unique index. Identity is name + email + birthday day/month; email and birthday are optional unless another singer has the same name."
gate: Helder passes the fix on device.
closed: 2026-11
kind: bug
---

# Duplicate singer saved when the name is typed without picking the existing one

A singer with the same name, email and birthday must never be saved twice: enforced in the Person service and by a database unique index. Identity is name + email + birthday day/month, and at least one of email or birthday is required.


## Origin

F2 of `changes/2026-11-06-person-form-follow-ups-from-acf-t7-run-2/`. In insert mode, typing an existing name without picking it from the menu, then Save, creates a second singer with the same name, birthday and email.

## Decisions (Helder, 2026-11-19)

- **Identity** is name + email + birthday (day and month). Email and birthday are each optional, but **at least one is required**, so two namesakes with neither cannot exist. Birthday exists for people without email (some elders) and as a disambiguator.
- **Two layers:** the Person service rejects the duplicate on Save and returns the usual `(false, message, null)` tuple; a unique index in the database is the backstop. A unique-constraint `DbUpdateException` is caught in the service and mapped to the same tuple.
- **Index:** name and email use the default collation (`CollationConstants.Default`), no C# normalization. Null email and null day/month are coalesced so that NULLs do not defeat the index. A `CHECK` constraint backs the "email or birthday" rule.
- **Soft warning:** a same-name person with a different email or birthday shows a bottom-sheet notice (use the existing person or continue as new), never a block, so homonyms stay possible.
- **Migration:** minimal. Helder is the only user, on one device, and deletes any leftover test duplicates in the app first. The migration creates the index and fails if a duplicate remains; no dedupe or merge logic.
- **Dependency:** the index uses the day and month columns from `changes/2026-11-19-person-form-birthday-as-day-and-month-dropdowns/`. Do that item first, or in the same migration.
- **Not a key:** birthday is not the link to social accounts. A future `PersonLink` table (platform, handle) would carry that.
- **Regression test:** Major, so a failing test first (Service with Moq; index test on a real SQLite file).

## Decisions (Helder, 2026-11-21, after the on-device test)

- Email and birthday are **optional**, so a host in a hurry can register a singer by name only.
- When another singer already has that name, the new entry needs an email, a birthday or both before it can be saved.
- The same-name sheet shows a singular or plural title, lists at most 3 matches, and counts the rest.
