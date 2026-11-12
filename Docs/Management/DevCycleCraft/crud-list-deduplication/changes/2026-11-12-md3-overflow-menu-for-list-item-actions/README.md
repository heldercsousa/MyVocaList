---
id: md3-overflow-menu-for-list-item-actions
title: MD3 overflow menu for list item actions
status: 💡 Pending
target: 2026-11-12
section: DevCycleCraft
parent: crud-list-deduplication
goal: The list item overflow icon button opens an MD3 menu (overflow menu use case) with Edit, Delete and page-specific actions, using MD3 menu terminology and the denser vertical menu tokens. Expressive motion is out of scope unless DevExpress provides it built-in.
gate: MD3 review plus DevExpress-first check; Helder approves before coding.
kind: change
---

# MD3 overflow menu for list item actions

The list item overflow icon button opens an MD3 menu (overflow menu use case) with Edit, Delete and page-specific actions, using MD3 menu terminology and the denser vertical menu tokens. Expressive motion is out of scope unless DevExpress provides it built-in.


## Decision 2026-11-12: MenuItem on the shared slot base

`MenuItem` derives from the slot base created by the ListItem change under `m3-lists`, and is its successor. It adds the vertical menu tokens, trailing text, badge and a per-item command. Expressive motion is out of scope unless DevExpress provides it built-in.
