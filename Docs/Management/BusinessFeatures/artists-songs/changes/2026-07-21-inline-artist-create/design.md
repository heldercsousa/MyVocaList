# Design — Inline "create new artist" on the Song form

**Status:** approved design (Helder, 2026-07-21). Derived from the plan-mode design; decisions locked: affordance = synthetic dropdown row (Option A), scope = minimal.

## Architecture

No new layers. The capability already exists in the Service layer; this feature is **UI/ViewModel plumbing that calls existing `ArtistService` methods**. Business logic stays in Services (constitutional).

- **Service (unchanged):** `IArtistService` / `ArtistService`
  - `CreateArtistAsync(string name, …) → (bool success, string message, Artist? artist)` — re-validates, trims, exact-dup guard (`ExistsByNameAsync`), creates + saves.
  - `ValidateNameInput(name) → (bool isValid, string message)` — empty + max 60.
  - `SearchArtistsByNameAsync(query, maxResults)` — already backs the autocomplete.
- **ViewModel:** `SongFormViewModel` (already injects `_artistService`).
- **View:** `SongFormPage.xaml` / `.xaml.cs` (DX `AutoCompleteEdit`, `OnArtistItemsRequested`, `OnArtistSelectionChanged`).
- **Model:** `AutocompleteSuggestion` (add a create-sentinel discriminator).

## Interfaces / changes

1. **`AutocompleteSuggestion` sentinel discriminator.** Add a boolean (e.g. `IsCreateNew`) — default `false`. A create row is one instance with `IsCreateNew = true`, `Headline = "Add \"{text}\" as a new artist"`, carrying the raw typed text. This keeps selection routing unambiguous and lets the `ItemTemplate` render it distinctly.

2. **`OnArtistItemsRequested` (page code-behind).** After awaiting `SearchArtistsCommand`, append one create-sentinel suggestion built from `e.Text` to the returned list (last position). The existing search/mapping is otherwise unchanged (REQ-ACREATE-01/02). On empty results the list contains only the create row (REQ-ACREATE-03).
   - Code-behind stays glue-only: it builds the sentinel from the typed text and appends; it does not create the artist or contain business logic.

3. **`OnArtistSelectionChanged` (page code-behind).** If the selected item `IsCreateNew`, forward to a new VM command `CreateArtistInlineCommand` with the typed text; otherwise the existing `SelectArtistCommand` path (unchanged).

4. **`SongFormViewModel.CreateArtistInlineAsync(string name)` (new `AsyncRelayCommand<string>`).**
   - `var (success, message, artist) = await _artistService.CreateArtistAsync(name);`
   - success → reuse the existing lock path (set `SelectedArtistId`/`SelectedArtistName`, `IsArtistLocked = true`, clear `ArtistSuggestions`, clear `ArtistHasError`/`ArtistErrorText`) — ideally by calling the same private method `SelectArtist` already uses to lock, to avoid a second lock implementation.
   - failure → `ArtistHasError = true; ArtistErrorText = message;` retain `ArtistSearchText` (REQ-ACREATE-05). No dialog.

5. **`ItemTemplate` (XAML).** Add a `DataTrigger`/style path so a row with `IsCreateNew` renders with a leading ➕ and a top divider, distinct from real matches (REQ-ACREATE-02). Incremental single-file XAML edit; build after.

6. **Blur behavior.** `OnArtistBlurredWithoutSelection` must **retain** the typed text for the Song artist field (REQ-ACREATE-03) instead of clearing (current BUG-008 path). Adjust only the no-locked-artist branch to keep `ArtistSearchText`; the "restore prior selection" branch is unchanged.

## Interaction flow

```
type → OnArtistItemsRequested → SearchArtistsByNameAsync
     → [matches…] + [➕ Add "text" as a new artist]   (REQ-ACREATE-02)
pick a match      → SelectArtistCommand            (unchanged)
pick the ➕ row    → CreateArtistInlineCommand(text)
                      → CreateArtistAsync(text)
                        success → lock artist, clear error   (REQ-ACREATE-04/08)
                        failure → error text, keep typed text (REQ-ACREATE-05)
blur, no selection → keep typed text, no clear      (REQ-ACREATE-03)
```

## Implementation gate (DX capability check — do first)

Before wiring Option A, confirm via **Context7 (DevExpress MAUI 25.2.4)** that `AutoCompleteEdit` will render and allow selection of a suggestion **not** derived from the typed text (a synthetic row). If DX filters the provider results against the input or refuses to surface such a row:
- **fall back to Option B** — on no-match blur, retain the text and reveal an "Add «text» as a new artist" `DXButton` below the field (mirrors ArtistForm's suggestion-area layout);
- record the fallback + evidence in the task-log; do **not** invent a third pattern.

## Error handling

Tuple returns from `CreateArtistAsync` (no exceptions for business failures). Failure → mapped to the existing `ArtistHasError`/`ArtistErrorText` bindings. No `DisplayAlert`/native dialogs (constitutional).

## Constitutional check

Business logic in Services ✅ (creation/validation stay in `ArtistService`) · DevExpress-first ✅ (reuses `AutoCompleteEdit`) · No native dialogs ✅ · English-only ✅ · SafeAreaEdges already `Container` on the page ✅ · Incremental XAML edits ✅.

## Testing

Level A (SongFormViewModel, Moq `IArtistService`):
- no local match → typed text retained, create affordance data present.
- `CreateArtistInlineAsync` success → artist locked (`SelectedArtistId` set, `IsArtistLocked` true), error cleared.
- `CreateArtistInlineAsync` failure (service returns `success=false`) → `ArtistHasError` true, `ArtistErrorText` = message, text retained, no lock.
- save after inline create → song persisted with the new `ArtistId` (existing save test extended).

On-device E2E: novel artist → ➕ → created + locked + song saves; exact-existing name via ➕ → duplicate error, no orphan.
