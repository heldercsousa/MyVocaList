---
id: md3-overflow-menu-for-list-item-actions
title: MD3 overflow menu for list item actions
status: ✅ Done
target: 2026-11-12
section: DevCycleCraft
parent: crud-list-deduplication
goal: The list item overflow icon button opens an MD3 menu (overflow menu use case) with Edit, Delete and page-specific actions, using MD3 menu terminology and the denser vertical menu tokens. Expressive motion is out of scope unless DevExpress provides it built-in.
gate: MD3 review plus DevExpress-first check; Helder approves before coding.
closed: 2026-11
kind: change
---

# MD3 overflow menu for list item actions

The list item overflow icon button opens an MD3 menu (overflow menu use case) with Edit, Delete and page-specific actions, using MD3 menu terminology and the denser vertical menu tokens. Expressive motion is out of scope unless DevExpress provides it built-in.


## Decision 2026-11-12: MenuItem on the shared slot base

`MenuItem` derives from the slot base created by the ListItem change under `m3-lists`, and is its successor. It adds the vertical menu tokens, trailing text, badge and a per-item command. Expressive motion is out of scope unless DevExpress provides it built-in.

## Decision 2026-11-20: no menu component (supersedes 2026-11-12)

DevExpress has no menu control. The menu is a single `DXPopup` anchored to the overflow icon button, and its menu items are `DXButton`s with a `MenuItem` style. `MenuItem` and `SlotItemBase` are dropped: no current menu item needs trailing text, a badge or selection. The only new view is `ListItemOverflowButton`. Rationale and rejected alternatives: `design.md § Component decision`.

## Open for the spec (2026-11-14) — settled 2026-11-20 in `design.md § Tokens`

- Settle two token discrepancies with verbatim m3.material.io quotes: menu item height (48dp vs 44dp) and icon-to-label gap (8dp vs 12dp).
- Order: specced together with the ListItem, overflow button and CrudListView changes. Implemented after the ListItem change, alongside the overflow button.
