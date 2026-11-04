---
id: BUG-072
title: Hook scripts invoked by relative path — guards break or go silently inert outside repo root
status: ✅ Fixed
severity: Major
target: 2026-08-04
section: DevCycleCraft
parent: spec-evolution-versioning
goal: The pre-tool constitutional guard is invoked by a relative path, so it fails whenever the working directory is not the repo root and takes Edit and Write down with it.
gate: Anchor hook commands to the project-dir variable, then verify both guards still run when started from a subdirectory.
closed: 2026-11
kind: bug
---

# Hook scripts invoked by relative path — guards break or go silently inert outside repo root

The pre-tool constitutional guard is invoked by a relative path, so it fails whenever the working directory is not the repo root and takes Edit and Write down with it.

## Fix (2026-11-03)

**Changed files:** `.claude/settings.json` (every script hook now `python "$CLAUDE_PROJECT_DIR/.claude/scripts/…"`).
**Verification:** run from `MyVocaList/` subdir — `constitutional-guard.py` exit 0 on a normal edit, `protect-generated.py` exit 2 on a snapshot edit; hook JSON parses. UI-free config fix → manual check, no unit test.
