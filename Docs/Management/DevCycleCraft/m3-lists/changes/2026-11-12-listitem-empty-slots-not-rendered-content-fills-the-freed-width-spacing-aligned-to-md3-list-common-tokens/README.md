---
id: listitem-empty-slots-not-rendered-content-fills-the-freed-width-spacing-aligned-to-md3-list-common-tokens
title: "ListItem: empty slots not rendered, content fills the freed width; spacing aligned to MD3 List - Common tokens"
status: 💡 Pending
target: 2026-11-12
section: DevCycleCraft
parent: m3-lists
goal: "ListItem itself hides a leading or trailing slot that has no content, and the content slot takes the freed width. Spacing moves to the List - Common tokens: leading and trailing 16dp, between 12dp, top and bottom 10dp."
gate: "Governed component (33 usages in 8 files): four gates; Helder approves before coding."
kind: change
---

# ListItem: empty slots not rendered, content fills the freed width; spacing aligned to MD3 List - Common tokens

ListItem itself hides a leading or trailing slot that has no content, and the content slot takes the freed width. Spacing moves to the List - Common tokens: leading and trailing 16dp, between 12dp, top and bottom 10dp.


## Decision 2026-11-12: shared slot base (Claude, delegated by Helder)

`ListItem` and the future `MenuItem` derive from one small internal code-behind base. The base owns the `LeadingContent` and `TrailingContent` slots, the empty-slot collapse (an empty slot is not rendered and the content slot takes the freed width), and the selected and disabled states. Each component keeps its own XAML, tokens and text slots.

Rejected: a single `ListItem` with a list/menu variant flag. Every token differs (height, gaps, icon size, typography, inset, corners), so each one would need a branch. Menu-only parts (trailing text, badge, per-item command, close-on-tap) would also leak into `ListItem`, and one class would carry two MD3 component names. The base name must not use an MD3 term, e.g. `SlotItemBase`; the name is confirmed in the spec.

This change creates the base and moves `ListItem` onto it (governed: 33 usages in 8 files).
