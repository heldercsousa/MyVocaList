---
id: stop-hook-scanner-removal
title: 'Bug: "To Review tasks need attention" rewakes every session (Stop hook noise)'
status: "✅ Fixed"
target: 2026-06
section: DevCycleCraft
kind: change
closed: 2026-06
order: 10
goal: "remove the To-Review-tasks scanner from the Stop hook (session-continuity-leasing). Scanner removed 2026-06-27."
pointer: DevCycleCraft/session-continuity-leasing/task-log.md
---

# Bug: "To Review tasks need attention" rewakes every session (Stop hook noise)

The To-Review-tasks scanner was removed from the Stop hook. Fixed 2026-06-27.

> Migrated from the 2026-06 archive row (T12a Wave C re-triage, blocker #4). The parent
> `session-continuity-leasing/README.md` describes the overall feature and did not already cover
> this specific Stop-hook-noise fix, so it is filed as its own `changes/` item under
> `session-continuity-leasing/` with `pointer:` kept on the shared `task-log.md` (REQ-SEV-27).
> Folder shape, slug, title and `order` are agent-authored — flagged for the gate audit. Goal text
> is transcribed verbatim from the archived BACKLOG Notes cell (`closed: 2026-06`).
