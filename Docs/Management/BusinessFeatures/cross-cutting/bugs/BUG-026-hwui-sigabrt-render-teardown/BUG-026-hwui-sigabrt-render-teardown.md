# BUG-026: HWUI native crash (SIGABRT) — pthread_mutex_lock on destroyed mutex in hwuiTask0

**Registered:** 2026-07-03
**Severity:** Major (see classification note below)
**Status:** 💡 Pending — investigation not started
**Parent:** Cross-cutting (no single business feature — native Android render pipeline, could surface on any page)

## Symptom

Captured in an emulator logcat session during the "frozen UI in emulator" investigation
(see `Docs/Management/Chat with Opus about frozen UI in emulator - TO DO.txt`, lines 293–299):

```
19:41:48 libc  FORTIFY: pthread_mutex_lock called on a destroyed mutex …
         tid 8390 (hwuiTask0) → Fatal signal 6 (SIGABRT)
```

This is a native-layer crash in Android's HWUI render pipeline (`hwuiTask0` render thread),
distinct from the ANR ("Input dispatching timed out") investigated in the same session.

## Why it is registered separately from the ANR investigation

The ANR investigation (same log capture) was resolved as a Debug-build + emulator host-load
artifact — confirmed by a clean Release build on physical hardware (Samsung S23) and by ANR
severity scaling with host resource pressure. This HWUI SIGABRT is a **different signal**:
a native mutex-corruption crash, not a main-thread stall. It doesn't fit the same
"debugger/JIT overhead" explanation and needs its own root-cause path.

## Severity classification note

Classified **Major**, not Critical, because:
- The crash fired at the exact moment "the process was force-stopped" — i.e., concurrent
  with VS/vsdbg tearing down the debug session — not during live, undisturbed user
  interaction. This raises a real possibility it is debugger-teardown noise (the debugger
  or ART killing threads out of order) rather than a defect that would occur in a shipped
  Release build.
- No data loss or corruption is implicated — it is a render-thread abort at shutdown.
- Per `bug-tracking.md`, Critical requires a crash reachable in normal use; that has not
  been confirmed here. Reclassify to Critical if reproduced during a live (non-teardown)
  session.

## Investigation needed before a fix is attempted

1. Reproduce (or rule out) outside of a debugger-forced teardown:
   - Capture a Release logcat on the S23 device (already planned for the ANR
     investigation) and check for the same `pthread_mutex_lock` / `hwuiTask0` signature.
   - Capture an emulator logcat for a normal app-close (Back button / swipe-away) rather
     than a VS "Stop Debugging" kill, to see if the same signature appears.
2. If it reproduces only under debugger-forced kill: downgrade to informational / no fix
   needed, close as environment artifact (same disposition as the ANR).
3. If it reproduces during normal teardown/backgrounding on Release: this is a genuine
   native rendering-layer defect — escalate to Critical and start an Explore subagent
   trace of any custom render-thread/image-decode code paths active at teardown time
   (`code-principles.md` boundaries apply — no orchestrator code reads).

## Regression test

Not yet applicable — no root cause confirmed. Per `bug-tracking.md`, a Critical/Major fix
requires a regression test only once the fix is scoped; a native ART/HWUI-layer crash may
not be reachable from a C# unit test (similar to BUG-023, where the underlying defect was
not unit-testable and a guard test was used instead). Revisit once investigation step 1–2
above determines whether this is fixable application code or platform/emulator noise.
