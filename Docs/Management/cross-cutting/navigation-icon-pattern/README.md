---
id: navigation-icon-pattern
title: "**Navigation Icon Pattern — Root Pages vs Pushed Pages**"
status: "✅ Done"
target: 2026-06
section: DevCycleCraft
kind: feature
closed: 2026-06
order: 110
goal: "standardize leading AppBar icon: root flyout pages show hamburger; pushed detail pages show back arrow. Dynamic icon shipped in CrudListPageBase."
pointer: cross-cutting/navigation-icon-pattern/
---

# Navigation Icon Pattern — Root Pages vs Pushed Pages

Standardized leading AppBar icon behavior: root flyout pages show hamburger (menu icon, opens
drawer); pushed detail pages show back arrow (pops stack). All 4 CRUD list pages previously
hardcoded the back arrow even when reached from the flyout. Shipped via a dynamic icon in
`CrudListPageBase.OnNavigatedTo`, based on the navigation stack depth.

> Migrated from the 2026-06 archive row (T12a Wave M, F-1a batch 3). Goal text is transcribed
> verbatim from the archived BACKLOG Notes cell (`closed: 2026-06`).
