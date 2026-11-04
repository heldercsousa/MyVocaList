---
id: agent-type-taskcreated-taskcompleted-hooks-spawn-an-agent-per-task-event
title: Agent-type TaskCreated/TaskCompleted hooks spawn an agent per task event
status: ✅ Done
target: 2026-11-03
section: DevCycleCraft
parent: hooks-redesign
goal: Two hooks start a full agent on every task create and complete, then run git add -A, commit and push plus a build, using stale plan paths. That costs tokens on trivia and can push unverified work.
gate: "Helder decides: replace with command hooks, trim to the task-log write only, or remove."
closed: 2026-11
kind: change
---

# Agent-type TaskCreated/TaskCompleted hooks spawn an agent per task event

Two hooks start a full agent on every task create and complete, then run git add -A, commit and push plus a build, using stale plan paths. That costs tokens on trivia and can push unverified work.
