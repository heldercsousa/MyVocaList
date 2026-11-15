---
id: targeted-file-reads-and-task-log-rotation-hooks
title: Targeted file reads and task-log rotation hooks
status: ✅ Done
target: 2026-11-14
section: DevCycleCraft
parent: docs-context-scope-control
goal: Read calls on files over 200 lines need a line range, and a task log over 300 lines rotates closed entries to a monthly archive file; both enforced by hooks.
closed: 2026-11
kind: change
---

# Targeted file reads and task-log rotation hooks

Read calls on files over 200 lines need a line range, and a task log over 300 lines rotates closed entries to a monthly archive file; both enforced by hooks.
