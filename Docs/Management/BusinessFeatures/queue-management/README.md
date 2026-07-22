---
id: queue-management
title: "**Queue Entry Point Redesign — QueuePage as CRUD event list**"
status: "💡 Pending"
target: 2026-06
section: BusinessFeatures
kind: feature
order: 300
goal: "QueuePage becomes the CRUD list of events (FAB creates a queue; tap opens QueueManagementPage); EventsPage deleted."
gate: "audit 2026-07-15 found NO implementation ever landed (registration only) — QueueManagementPage is unreachable in the app; Helder to re-prioritize."
pointer: BusinessFeatures/queue-management/task-log.md
---

# Queue Entry Point Redesign — QueuePage as CRUD event list

QueuePage becomes the CRUD list of events; EventsPage is deleted. Specs: `requirements.md`, `design.md`, `tasks.md`, `task-log.md`.
