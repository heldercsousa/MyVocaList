---
id: listitemcheckbox-leading-slot-when-the-row-has-a-trailing-action-trailing-otherwise
title: "ListItemCheckbox: leading slot when the row has a trailing action, trailing otherwise"
status: 🔵 Superseded
closed: 2026-11
target: 2026-11-12
section: DevCycleCraft
parent: crud-list-selection-gestures
goal: "Apply the placement derived from the MD3 lists specs configurations: no example pairs a checkbox with a trailing action. CRUD rows get the leading position because the overflow button takes the trailing slot."
gate: "Superseded 2026-11-12: each row template puts the checkbox in the ListItem leading or trailing slot; covered by the page tasks."
kind: change
---

# ListItemCheckbox: leading slot when the row has a trailing action, trailing otherwise

Apply the placement derived from the MD3 lists specs configurations: no example pairs a checkbox with a trailing action. CRUD rows get the leading position because the overflow button takes the trailing slot.


> **Superseded 2026-11-12.** Helder: ListItem already has the MD3 structure: container, leading slot, content, trailing slot. The checkbox is slot content; the row template that renders the item chooses the slot. Giving the checkbox its own placement logic would break MD3 terminology and architecture. The placement rule belongs to the page tasks.
