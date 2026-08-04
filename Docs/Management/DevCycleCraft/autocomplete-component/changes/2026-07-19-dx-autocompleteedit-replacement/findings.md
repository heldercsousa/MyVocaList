# Findings — DX AutoCompleteEdit replacement

## Pinned API (2026-07-19) — T1

Source of truth: installed package XML docs `~/.nuget/packages/devexpress.maui.editors/25.2.4/lib/net10.0/DevExpress.Maui.Editors.xml` (online sources unhealthy: DevExpress MCP index empty for autocomplete queries; Context7 partial — both flagged per MCP Availability Gate).

**Wiring decision: Option A** (`AsyncItemsSourceProvider`) — all required members exist.

| Role | Pinned member (DevExpress.Maui.Editors) |
|---|---|
| Control | `AutoCompleteEdit` (: `AutoCompleteEditBase` : `ComboBoxEditBase` : `ItemsEditBase` : `EditBase`) |
| Text (two-way) | `AutoCompleteEditBase.Text` |
| Provider slot | `AutoCompleteEditBase.ItemsSourceProvider` |
| Async provider | `AsyncItemsSourceProvider` — event `ItemsRequested`, props `RequestDelay`, `CharacterCountThreshold` |
| Request args | `ItemsRequestEventArgs.Text` / `.Request` (sync `Func`) / `.RequestAsync` (async) / `.CancellationToken` |
| Stale-result protection | `ItemsRequestEventArgs.CancellationToken` — provider cancels superseded requests (REQ-DXAC-07 natively satisfied) |
| Min length | `AsyncItemsSourceProvider.CharacterCountThreshold` (set to VM gate values: Artist per existing gate, Person = 2) — VM-side gates stay as defense in depth |
| Selection | `AutoCompleteEdit.SelectedItem`; `ItemsEditBase.SelectionChanged` event; **`ItemsEditBase.SelectionChangedCommand`** (bindable — prefer over code-behind) |
| Item template | `ItemsEditBase.ItemTemplate` (alt: `DisplayMember`) |
| Keep focus on select | `ItemsEditBase.KeepFocusOnItemSelection` |
| Error display | `EditBase.HasError` / `EditBase.ErrorText` (bindable props — REQ-DXAC-05 OK) |
| Label / box | `EditBase.LabelText`, `IsLabelFloating`, `BoxMode` + border/label color props (style per T2) |
| Loading state | `AutoCompleteEditBase.IsLoadingInProgress`, `LoadingProgressMode`, `WaitIndicatorColor` (REQ-DXAC-07 in-flight behavior) |
| Text-change command (Option B only) | `AutoCompleteEditBase.TextChangedCommand` — available but unused under Option A |
| Client-filter disable (REQ-DXAC-06) | Not applicable under Option A: `AsyncItemsSourceProvider` displays exactly what the request returns (no secondary client filter); `FilteredItemsSourceProvider` (the sync/filtering provider) is NOT used |

### Plan ⚠-marker resolutions

- `ItemsRequested="OnArtistItemsRequested"` — event name correct as planned.
- `SelectionChanged` — exists, but **use `SelectionChangedCommand="{Binding SelectArtistCommand}"` / `"{Binding SuggestionSelectedCommand}"` instead** (command parameter = selected item per DX convention; verify at build — if the command receives no/wrong parameter, fall back to the `SelectionChanged` code-behind handler from the plan).
- `HasError`/`ErrorText` — bindable one-way-to-target as planned.
- Fulfilment shape: prefer `e.RequestAsync = async () => await vm.<existing Service-backed search>` returning the suggestion collection, honoring `e.CancellationToken`. The VM's existing command path may be kept by exposing the already-tested search method; no VM logic changes.
