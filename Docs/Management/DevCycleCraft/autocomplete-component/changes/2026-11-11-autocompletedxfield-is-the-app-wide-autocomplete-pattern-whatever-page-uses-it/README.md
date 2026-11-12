---
id: autocompletedxfield-is-the-app-wide-autocomplete-pattern-whatever-page-uses-it
title: AutocompleteDXField is the app-wide autocomplete pattern, whatever page uses it
status: 💡 Pending
target: 2026-11-11
section: DevCycleCraft
parent: autocomplete-component
goal: "Every autocomplete or suggestion field in the app uses AutocompleteDXField, and its timing uses shared constants: no page-level DX AutoCompleteEdit glue and no literal debounce values."
gate: A census finds no raw AutoCompleteEdit or suggestion glue outside the component (Artist form included), and the list-search debounce uses a shared constant.
kind: change
---

# AutocompleteDXField is the app-wide autocomplete pattern, whatever page uses it

Every autocomplete or suggestion field in the app uses AutocompleteDXField, and its timing uses shared constants: no page-level DX AutoCompleteEdit glue and no literal debounce values.

## Origin

Helder, 2026-11-11, at close-out of the shared autocomplete field item
(`../2026-09-24-shared-autocomplete-field-pick-lock-via-x-centralised-behaviour/`). Its two follow-ups are registered
here as one rule: `AutocompleteDXField` is the pattern for the component, whatever page uses it.

## Scope (to be specified)

- **Rollout:** census every `AutoCompleteEdit` / suggestion field in the tree (delegated, not by the orchestrator) and move
  each one onto `AutocompleteDXField`. Known candidate: the Artist form duplicate-name suggestions. The Song form Title and
  Artist are already being moved by the song-form title-first item.
- **Shared timing constants:** the list-search debounce in `CrudListViewModelBase` is a literal; it should use a shared
  constant, as `SearchConstants.AutocompleteDebounceMilliseconds` does for the component.
- **Governance:** once the Song form binds, the component has two consumers and is governed; every change here goes through
  the four gates in `component-change-governance.md`.
