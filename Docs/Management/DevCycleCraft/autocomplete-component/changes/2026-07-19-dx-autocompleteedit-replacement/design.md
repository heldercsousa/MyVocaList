# Design — Replace `AutocompleteMobileField` consumers with DX `AutoCompleteEdit`

> Dated change spec (2026-07-19). Approach A (direct per-page use) approved by Helder in brainstorming, 2026-07-19.

## Architecture

Behavior-preserving control swap. All search/selection/validation logic already lives in ViewModels + Services (constitutional layering) and is untouched:

```
SongFormPage.xaml ──┐                                  ┌─ IArtistService.SearchArtistsByNameAsync(query, 5)
                    ├─ dxe:AutoCompleteEdit ── existing VM commands/properties ──┤
PersonFormPage.xaml ┘                                  └─ IPersonService.SearchPersonsStartsWithAsync(term, 5)
```

- **No new custom component.** Each page uses `dxe:AutoCompleteEdit` directly (DevExpress-first Non-Negotiable; only 2 consumers today).
- `Contracts/Models/AutocompleteSuggestion.cs` stays as the ItemsSource item type.
- One style resource in `MaterialStyles.xaml` aligns the editor with the Outlined `TextEdit` form convention (REQ-DXAC-12). Adding a *style* is not a governed-component change (no shared custom component is created or edited).
- Services are unaffected; BUG-046 whitespace normalization and persisted-string-trimming work are retained as-is.

## Binding map

| Concern | SongFormPage (Artist) | PersonFormPage (Full Name) |
|---|---|---|
| Text | `ArtistSearchText` | `PersonName` |
| Suggestions (ItemsSource) | `ArtistSuggestions` | `Suggestions` |
| Search trigger | `SearchArtistsCommand` | `SearchPersonsCommand` |
| Selection | `SelectArtistCommand` | `SuggestionSelectedCommand` |
| Blur w/o selection | `ArtistBlurredWithoutSelectionCommand` | `ValidateNameCommand` |
| Error state | `HasError` / `ErrorText` | existing error bindings |
| Enable/lock | `IsArtistLocked` | — |

## Behavior decisions

1. **Command-driven async suggestions:** text change → debounce (~300 ms; prefer DX built-in async delay) → existing search command → Service (max 5) → results bound to the editor. Stale-result protection: latest-query-wins (existing VM pattern or cancellation token).
2. **DX client-side filtering disabled** — Service output is authoritative (avoids re-introducing whitespace/diacritic mismatches the DB collation + BUG-046 fix solved).
3. **Typed text is inviolable** (REQ-DXAC-03): no code path may clear/replace user text on blur or dismiss.
4. **Suggestion item template:** two-line MD3 look from `AutocompleteSuggestion.Headline` + `SupportingText`; independent of the frozen `ListItem` wiring.
5. **Dropdown on all form factors** (per D-AC1) — no full-screen modal, no `PushModalAsync`, which removes the entire `MobileFieldReopenGuard`/stacked-navigation mechanism behind BUG-041–047.

### API-pinning gate (MCP Availability Gate)

On 2026-07-19 the DevExpress MCP demo-app index returned empty for all autocomplete queries and Context7 coverage of `AutoCompleteEdit` is thin (existence, async mode, `FilteredItemsSourceProvider` confirmed; exact async-provider/delay/error property names not). **Task 1 pins the exact DX 25.2.4 API names before any XAML is written.** If neither doc source can confirm them, escalate to Helder — do not guess member names.

## Frozen component handling

`UI/Components/AutocompleteField/` (8 files) + 6 test files in `MyVocaList.Tests/Unit/Components/` are excluded from compilation (`<Compile Remove>` / `<MauiXaml Remove>` in the respective `.csproj`s) and retained on disk as reference for future guideline ① (Helder decision 2026-07-19). Files remain `.sln`-visible; a README note in the component folder states the frozen status and points to the decision record.

## Wrapper promotion trigger (recorded decision)

The future queue-entry form will need Venue/Artist/Singer autocomplete pickers (Helder direction, agreed 2026-07-19: inline autocomplete beats navigate-away entity pickers for in-form selection). When that form — or any third consumer — adopts this pattern, a **dedicated task evaluates extracting a shared `FormAutocompleteField` wrapper**, under component-change governance, ideally combined with the "no match → add new" action-row work so the wrapper's surface is designed once. Until then, per-page use is deliberate YAGNI.

## Error handling

Unchanged: Service tuple returns / existing VM error properties; editor error display via DX error properties. No new exception paths; search failures follow the existing VM behavior.

## Testing (risk-tiered, testing.md)

- **Level A (existing):** VM tests pass unchanged — REQ-DXAC-08.
- **Level A/B (new):** only if VM glue changes (e.g. debounce relocation) — unit tests for it.
- **Level C:** XAML swap itself — build + manual E2E, no mandatory unit test (documented in task-log).
- **Manual E2E:** BUG-044/045/047 family checklist on both pages (REQ-DXAC-09) + smoke 16C.1 (REQ-DXAC-10).

## Traceability

| AC | Where implemented | Verified by |
|---|---|---|
| 01, 04, 05, 12 | SongFormPage.xaml + style | build, existing VM tests, manual E2E |
| 02, 04, 05 | PersonFormPage.xaml | build, existing VM tests, manual E2E |
| 03, 06, 07 | editor configuration (+ VM debounce if needed) | manual E2E + unit tests for new glue |
| 08 | no VM contract change | `dotnet test` unchanged suite |
| 09 | evaluation checklist | task-log evidence |
| 10 | end-to-end | Helder device smoke test |
| 11 | csproj exclusions + README note | build green |
