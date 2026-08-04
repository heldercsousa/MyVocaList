---
id: md3-devexpress-compliance-gap
title: "**MD3/DevExpress Compliance Gap — Internal Guidelines**"
status: "✅ Done"
target: 2026-06
section: DevCycleCraft
kind: feature
closed: 2026-06
order: 60
goal: "pre-implementation DX component audit checklist to catch missing MD3-compliant component usage before code review."
pointer: cross-cutting/md3-devexpress-compliance-gap/
---

# MD3/DevExpress Compliance Gap — Internal Guidelines

Discovered: developers coded filter UI without verifying DX has built-in MD3 components
available (SongsPage used plain `DXButton` instead of `dxe:FilterChipGroup`; `BottomSheetTitle`
style missing from `MaterialStyles.xaml`). Enhanced `devexpress-patterns.md` + `m3-components.md`
with a pre-implementation DX component audit pattern to catch this class of error before code
review.

> Migrated from the 2026-06 archive row (T12a Wave L, F-1a batch 2). Goal text is reworded from
> the archived Notes cell (verbatim text tripped the file-path-beyond-pointer banned pattern via
> `workflow.md`); meaning preserved.
