---
id: BUG-080
title: venuesSeedForTest migration ships ~504 fake venues and its Down wipes the table
status: 💡 Pending
severity: Major
target: 2026-09-21
section: BusinessFeatures
parent: venues
goal: A test-data seed migration sits in the production chain, so real installs get ~504 fake venues, and its Down() runs an unscoped DELETE FROM Venues that would destroy genuine user data on rollback.
gate: Helder decides whether the seed ships or is gated/removed before release, and Down() is scoped or made a no-op.
kind: bug
---

# venuesSeedForTest migration ships ~504 fake venues and its Down wipes the table

A test-data seed migration sits in the production chain, so real installs get ~504 fake venues, and its Down() runs an unscoped DELETE FROM Venues that would destroy genuine user data on rollback.


## How this was found

Helder pointed at `Infra/Migrations/20260211055225_venuesSeedForTest.cs` while discussing how to
reproduce BUG-079 — it is his largest dataset. Reading it surfaced two unrelated concerns. **Neither is
part of BUG-079.** Helder's instruction (2026-09-21): *"just record such concern. Let's move ahead…
perhaps you're pointing something that will be noticed only in the future."* So this is **registered,
not scheduled** — no work is planned until he decides.

## Concern 1 — a test seed sits in the production migration chain

The migration inserts **4 accented names + 500 randomly generated ones = ~504 venues**
(`:24-27` and `:59-100`). Its name says `ForTest`, but it is an ordinary EF migration in the chain, so
it runs on **every install**, including real users' devices.

A shipped user would open the app and find ~504 venues with generated names like `Björk Açúcar Lounge`,
`Praça 317`, `Casa do Fado`. Intentional today (useful test data, pre-release), but it becomes a
user-facing defect the moment the app ships with it.

**Open question for Helder:** does the seed ship, or is it gated (environment check / debug-only) or
removed before release? Note that **removing a migration from the chain after it has run is itself
awkward** — the migrations history table already records it — so the decision is easier taken sooner
than later.

## Concern 2 — `Down()` is an unscoped `DELETE FROM Venues` `[data-loss risk]`

```csharp
protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql("DELETE FROM Venues");
}
```

This deletes **every row in `Venues`**, not just the ~504 the `Up()` inserted. A rollback on a device
holding genuine user venues destroys them, with no way to distinguish seeded rows from real ones —
`Up()` records nothing that would let `Down()` target only its own inserts.

- **Probability: low.** Rolling back a migration is a deliberate developer action, rarely done on a
  device with real data.
- **Impact: total** for the `Venues` table.

Filed **Major** rather than Critical on that basis: `bug-tracking.md` reserves Critical for data loss,
and the loss here is real but requires an unusual deliberate trigger. **Escalate to Critical if a
rollback ever becomes part of a normal flow** (e.g. a downgrade path or a restore feature that reverts
migrations).

**Possible remedies** (Helder's call, not decided):
- Make `Down()` a no-op — the seed is throwaway, so reverting it need not delete anything.
- Or tag the seeded rows (a marker column / a known name prefix / a recorded id range) so `Down()` can
  scope its delete to exactly what `Up()` inserted.

## Why no fix is proposed yet

Both concerns hinge on a release-strategy decision that is Helder's to make, and the fix differs
depending on it: if the seed is being removed before release anyway, `Down()` barely matters; if the
seed stays, `Down()` must be scoped. Acting before that decision risks doing the work twice.

## Helder's answer on Concern 1 (2026-09-21) — a forward drop migration is planned

> *"About migration, before release to production, another migration will be created to drop it!"*

**Concern 1 has a plan and is not an open question.** The seed will be removed by a **forward
migration** before production release, not by rolling this one back. That is the right shape — it
avoids the "removing a migration after it has run" awkwardness noted above, since the history table
keeps both entries and the net effect is zero seeded rows.

### Why this also mostly answers Concern 2

If removal happens via a forward migration, `Down()` is only ever reached by a deliberate developer
rollback. Its unscoped `DELETE FROM Venues` stays a latent hazard but drops further down the priority
list. **Not closed** — a no-op `Down()` is still the safer form and costs one line — but it no longer
gates anything.

### One wrinkle worth knowing before the drop migration is written

The drop migration will face **the same identification problem** `Down()` has: nothing marks which rows
came from the seed.

- **On a fresh production install this does not matter.** Migrations run in order before the user can
  create anything, so at the moment the drop runs the only venues present *are* the seeded ones. Even a
  blunt `DELETE FROM Venues` is safe there.
- **On an existing install it does matter.** Any device that already ran the seed and has since had
  **real venues added** — Helder's own dev phones, and any pre-release tester — would lose those real
  venues too.

So the blunt form is safe for end users and destructive only for pre-release testers' own data. That
may well be an acceptable trade; it should just be a **conscious** one rather than a surprise.

If preserving pre-release data matters, the options are to match the seed's known generated-name
patterns (fragile — the word lists are in the migration, but a user could coincidentally use one) or to
delete by id range if the seeded ids are contiguous and identifiable. Neither is needed for correctness
on production installs.

**Action: none now.** Recorded so whoever writes the drop migration has this reasoning in hand rather
than rediscovering it.
