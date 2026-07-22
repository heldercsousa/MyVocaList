---
id: BUG-043
title: "BUG-043: release build returns zero autocomplete suggestions (Critical)"
status: "✅ Fixed"
severity: Critical
target: 2026-07-12
section: DevCycleCraft
parent: autocomplete-component
kind: bug
order: 90
closed: 2026-07
goal: "Root cause: manual SetValue severed the OneWay `Suggestions` binding; fixed via `ClearSuggestionsPresentation()`, on-device verified. Follow-up defects registered separately (BUG-044–047)."
pointer: DevCycleCraft/autocomplete-component/bugs/bug-043/
---

# BUG-043: release build returns zero autocomplete suggestions

Manual `SetValue` severed the OneWay `Suggestions` binding, so the Release build showed
no suggestions at all. Fixed via `ClearSuggestionsPresentation()`, merged to develop and
verified on device by Helder. Follow-up defects are tracked separately as BUG-044–047.
Evidence: the screenshots and debug log in this folder.

> **Spec updated [2026-07-22]:** the Notes text is condensed to fit the row template's sentence
> budget; the full narrative is preserved in the parent folder's task log and in the archive
> fixture. Nothing was reworded in meaning.
