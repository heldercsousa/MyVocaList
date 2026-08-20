---
id: BUG-076
title: Flaky ObjectDisposedException in the UOW SQLite test harness
status: 💡 Pending
severity: Major
target: 2026-08-18
section: DevCycleCraft
parent: read-model-notracking-guidelines
goal: Integration tests intermittently fail with ObjectDisposedException on SQLitePCL.sqlite3 during EnsureCreated, landing on a different test each run; affected tests pass in isolation.
gate: Suspected pooling/disposal interaction in the temp-file SQLite harness. Threatens the Phase 3.1 suite-green gate, whose whole value is a trustworthy signal.
kind: bug
---

# Flaky ObjectDisposedException in the UOW SQLite test harness

Integration tests intermittently fail with ObjectDisposedException on SQLitePCL.sqlite3 during EnsureCreated, landing on a different test each run; affected tests pass in isolation.

## Symptom

Running the full suite, an integration test occasionally fails with:

```
System.ObjectDisposedException : Cannot access a disposed object.
Object name: 'SQLitePCL.sqlite3'.
```

The stack originates inside `Database.EnsureCreated()` — i.e. during **harness construction**, before
the test body runs.

Two properties make this a flake rather than a defect in any one test:

- **It moves.** A different integration test is hit on each run; there is no stable victim.
- **It vanishes in isolation.** Every affected test passes when run alone (single-test filter).

Observed repeatedly across UOW Phase 2 (2026-08-18). It did not block Phase 2 — a re-run was always
green — which is precisely what makes it dangerous rather than merely annoying.

## Suspected cause

`MyVocaList.Tests/Infrastructure/UnitOfWorkTestHost.DisposeAsync` calls:

```csharp
SqliteConnection.ClearAllPools();
```

`ClearAllPools()` is **process-global**, not scoped to this host's connection string. Every
`UnitOfWorkTestHost` gets its own temp file (`uow_test_{guid}.db`), so the intent is clearly "release
*my* file's handles" — but the API released *everyone's*.

xUnit runs distinct test collections in parallel by default. So while host A is disposing and calling
`ClearAllPools()`, host B in another collection may be inside `Build()` → `EnsureCreated()` holding a
pooled connection. Clearing the pool disposes the underlying `sqlite3` handle out from under it, and
the next use throws `ObjectDisposedException: 'SQLitePCL.sqlite3'`.

This hypothesis explains all three observations at once: the victim is random (whoever happens to be
mid-`EnsureCreated`), it never reproduces in isolation (no concurrent host to be clobbered), and the
throw site is `EnsureCreated` rather than the test body (that is where a fresh pooled connection is
first opened).

**Not yet proven** — it is a hypothesis consistent with the evidence, not a confirmed root cause.
Confirm before fixing (`systematic-debugging`).

## Why it matters

Phase 3.1 of the unit-of-work pilot is a **HARD GATE** whose criterion is literally "the suite is
green". A suite that goes red at random destroys the evidentiary value of that gate in both
directions:

- A genuine regression introduced by the refactor can be dismissed as "the flake again".
- A green run cannot be distinguished from a lucky run, so "green" stops being evidence.

The only available workaround — re-run until green — is exactly the habit that hides real
regressions, which is why this is filed **Major** rather than Minor despite being test-only code.
It also undermines `verification-before-completion`, which the project relies on for every task
completion claim.

## Investigation notes for whoever picks this up

- Reproduce with repetition first: run the full suite in a loop and record which test is hit each
  time. A moving victim under parallel execution supports the hypothesis; a fixed victim refutes it.
- Try, in order of increasing cost:
  1. Drop `ClearAllPools()` and set `Pooling=False` in the harness connection string, so no pool
     exists to clear. The comment in `DisposeAsync` explains the call exists only to stop Windows
     file-delete blocking — disabling pooling addresses that cause directly and locally.
  2. If pooling must stay, confine teardown to this host's own connections rather than the process.
  3. As a fallback only, place the integration tests in a single xUnit collection to serialise them —
     this trades suite wall-clock time for determinism and does not fix the underlying sharing.
- Check whether `File.Delete` still succeeds without `ClearAllPools()`; the `catch (IOException)` is
  already best-effort, and a leaked temp file is a far smaller problem than a flaky suite.

## Files likely involved

- `MyVocaList.Tests/Infrastructure/UnitOfWorkTestHost.cs` (`DisposeAsync`, `Build`, `Configure`)
- possibly the test project's xUnit collection/parallelism configuration

**Not fixed here** — registered only, per the Phase 2 → Phase 3 hand-off scope.

---

## Reproduction attempt — 2026-08-20 (negative result)

Five consecutive full-suite runs on `develop` (`6a0e3eb8`), all green:

```
Com falha: 0, Aprovado: 592, Ignorado: 0, Total: 592   (x5)
```

The flake did **not** reproduce. This is evidence of low frequency, **not** evidence the bug is
fixed — nothing was changed that would plausibly fix it, and the recorded symptom is explicitly
intermittent and lands on a different test each run.

**Status deliberately left `💡 Pending`.** Two notes for whoever picks it up:

- It was previously associated with `QueueRepositoryTests`. That association looks incidental: the
  record itself says the failure lands on a different test each run, so this is a **shared
  harness** defect, not a Queue/Event one. It is therefore **not** covered by the 2026-08-20
  Event/Queue freeze and stays live.
- Chasing it with repeated full-suite runs is unproductive at this frequency. If it becomes worth
  fixing, drive it from the harness side (temp-file SQLite connection pooling / disposal ordering
  in the test fixture) rather than by trying to reproduce it.
