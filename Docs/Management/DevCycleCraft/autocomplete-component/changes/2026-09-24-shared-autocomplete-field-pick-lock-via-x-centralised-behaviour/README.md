---
id: shared-autocomplete-field-pick-lock-via-x-centralised-behaviour
title: Shared autocomplete field — pick-lock via X, centralised behaviour
status: 🟢 Ready
target: 2026-09-24
section: DevCycleCraft
parent: autocomplete-component
goal: "One reusable DX autocomplete owns all autocomplete behaviour app-wide: a picked option is read-only until X is tapped, X looks enabled, and timing and threshold use shared constants."
gate: Helder approves the spec, then SongFormPage moves onto the component and passes on device.
kind: change
---

# Shared autocomplete field — pick-lock via X, centralised behaviour

One reusable DX autocomplete owns all autocomplete behaviour app-wide: a picked option is read-only until X is tapped, X looks enabled, and timing and threshold use shared constants.


## Origin

Helder, on-device, 2026-09-24, while verifying BUG-081. All points below are his, recorded as
requirements input for the spec. **Status: spec drafted (`requirements.md`, `design.md`, `tasks.md`), in review.**

## Behaviour Helder requires (app-wide default for every autocomplete field)

1. **Pick-lock via X.** Once an option is picked, its text is **not editable**, in both add and edit
   modes. The only way to change it is to tap **X** first. X clears the field and enables typing again.
   - This is **stricter than shipped REQ-ACREATE-15**, which let the user type over a locked
     field and restored the prior artist on blur. REQ-ACREATE-15 is shipped history, so this item
     supersedes that allowance rather than editing it in place.
   - Technical tension the design must solve: DX `AutoCompleteEdit.IsReadOnly = true` gives the
     non-editable text but also disables the clear icon. That is exactly BUG-081, fixed on
     2026-09-24 by removing `IsReadOnly`, which is why typing over a pick is possible again today.
     The component needs "text read-only, X still live". Verify the options against DX 25.2.x
     (Context7 + `devexpress-maui` MCP) before designing.
2. **X must look enabled.** When usable, the X icon currently has the same colour as when disabled,
   and the same colour as the trailing search icon.
3. **Trailing search icon: evaluate removing it.** Search triggers automatically while typing, so it
   may add nothing. Confirm whether DX renders it by default.
4. **Centralise.** Autocomplete logic currently lives in `SongFormPage.xaml` + `.xaml.cs` (items
   request glue, create-new sentinel, selection routing, blur/clear forwarding, the BUG-061
   `SelectedItem` reset). It moves into one customised, reusable DX autocomplete component, shared by
   every page that needs one. Business rules stay in ViewModels/Services.
5. **Shared constants (F3 extension, 2026-09-23 ruling).** Replace `RequestDelay="300"` and
   `CharacterCountThreshold="1"` with shared constants. Note that `1` contradicts
   `SearchConstants.MinimumLocalQueryLength` (2).

## Related open points to settle in the spec

- **BUG-070 still reproduces (2026-09-24):** typing a non-existent artist and blurring shows
  "Search and select an artist from the list". Since inline create exists, that copy is misleading.
  The copy needs Helder's decision. It belongs to this component's validation contract.
- **Governance:** a brand-new component with one consumer is not governed. It becomes governed the
  moment a second page binds to it. From then on, the four gates in `component-change-governance.md`
  apply.
- Census needed (delegated, not by the orchestrator): every `AutoCompleteEdit`/autocomplete usage in
  the tree, to size the rollout.
