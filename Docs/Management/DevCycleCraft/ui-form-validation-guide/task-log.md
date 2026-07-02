# Task Log — Form Validation Guide (ui-form-validation-guide)

---
## Task: 01 — Author the Form Validation Standard in `.claude/library/*`
**Plan:** `Docs/Management/DevCycleCraft/ui-form-validation-guide/plan.md`
**Status:** To Review
**Started:** 07/01/2026
**Completed:** 07/01/2026

### Summary
Established a single, canonical form-input validation standard for MyVocaList in the project's Claude
internal guideline files (`.claude/library/*.md`). Docs-only task — no `.cs`/`.xaml` source, no `CLAUDE.md`,
no `.claude/rules/*` touched.

The three mandatory pre-implementation fixes were applied before authoring (details in `plan.md § 0`):
- **Fix 1 (DX blur mechanism CONFIRMED):** Standalone `dxe:TextEdit`/`dxe:DateEdit` blur hook = the MAUI
  `Unfocused` event (inherited from `VisualElement`), confirmed via Context7 `/websites/devexpress_maui`
  (DevExpress 25.2.x) 2026-07-01. Authored as fact (not a spike stub). DX-native alternative for a future
  `DataFormView` migration: `ValidationMode="LostFocus"`. The **day/month-only (no-year) birthday** is the one
  genuinely OPEN item — `dxe:DateEdit` has no masked no-year entry — authored as a clearly-marked
  `> OPEN — Helder gate` stub with two candidate paths, not asserted as fact.
- **Fix 2 (UX skill consulted — `ux:interaction-design`):** Standard confirmed consistent with Nielsen
  heuristics. Two refinements folded in: (a) no blur error on a pristine/untouched field (dirty-tracking, H5);
  (b) pending indicator for async checks (H1) + specific/actionable error messages (H9).
- **Fix 3 (`.sln` GUID verified):** Actual highest GUID in `MyVocaList.sln` is `...029`; next free is `...030`.
  The `constraints-registry.md` "0014" note is stale. Registered the new DevCycleCraft solution folder with
  `...030`.

### Changed files:
- `.claude/library/dialogs-validation.md` — added `## Form Validation Standard` (timing, decision table,
  anti-patterns, VM+XAML wiring with confirmed `Unfocused` hook, inline-only surfacing, masked-date rules with
  no-year OPEN stub, Integer spec-incomplete stub); cross-referenced the existing "Never use DisplayAlert" rule.
- `.claude/library/crud-pages.md` — added `### Validation (law)` under Form Page + a Wall-of-Red entry in the
  Form Page `### Never` list (pointers to the single-sourced standard).
- `.claude/library/devexpress-patterns.md` — added the `Unfocused` blur-validation note under `## TextEdit
  (Editors)`; added a new `## DateEdit (Editors) — masked dates` section (picker vs masked TextEdit, no-year
  OPEN stub, locale note).
- `.claude/library/theme-locale.md` — added a locale-dependent date-format bullet under `## Locale`.
- `.claude/library/ux-patterns.md` — added a `## Form Validation Timing (pointer)` section carrying the two
  Fix-2 IxD refinements + pointer to the standard.
- `Docs/Management/DevCycleCraft/ui-form-validation-guide/plan.md` — finalized from the draft with the three fixes.
- `Docs/Management/DevCycleCraft/ui-form-validation-guide/task-log.md` — this entry.
- `MyVocaList.sln` — registered new DevCycleCraft solution folder `ui-form-validation-guide`
  (GUID `{FA1234BC-0001-4000-8000-000000000030}`) with `01-ui-form-validation-guide.md` (pre-added), `plan.md`,
  `task-log.md`; added the NestedProjects mapping under the DevCycleCraft parent.

### Verification evidence
- Build: SKIPPED — docs-only change (no `.cs`/`.xaml`/`.csproj` modified).
- Tests: SKIPPED — docs-only change (no code files modified).
- Post-edit re-read: confirmed — all five `.claude/library/*.md` edits and the two `.sln` edits reviewed in place.
- Spec compliance: confirmed — R1–R9 mapped to guideline sections (plan.md § 5); R10 (Integer) escalated as
  spec-incomplete stub; all Constitutional Constraints upheld (DevExpress-first, native-dialog ban, MD3 terms,
  English-only, business-logic-in-Services). No `CLAUDE.md` / `.claude/rules/*` / source files touched.

### Requirement traceability (R1–R10)
| # | Requirement | Guideline section | Status |
|---|-------------|-------------------|--------|
| R1 | Validate on blur | dialogs-validation § Validation timing | Done |
| R2 | Blur → keystroke-on-error | dialogs-validation § Validation timing + Wiring | Done |
| R3 | Submit = safety net only | dialogs-validation § Validation timing | Done |
| R4 | Keystroke+debounce for guidance | dialogs-validation § Validation timing (decision table) | Done |
| R5 | Inline errors, no Wall-of-Red/dialog | dialogs-validation § Error surfacing; crud-pages § Never | Done |
| R6 | Masks mandatory, never persisted | dialogs-validation § Masked inputs + devexpress-patterns § DateEdit | Done (impl later) |
| R7 | Locale date formats | theme-locale § Locale + dialogs-validation § Masked inputs | Done as future/constraint |
| R8 | Date special case — day/month, no year | dialogs-validation § Masked inputs (**OPEN — Helder gate**) | Structural stub |
| R9 | Reuse specialized date validator | devexpress-patterns § DateEdit | Done (full date; no-year OPEN) |
| R10 | Integer validation | dialogs-validation § Integer inputs | **Escalated — spec `<TODO>` incomplete** |

### Escalations for Helder
1. **Integer (R10):** requirements Integer section is `<TODO>`. Complete it before the integer subsection is authored.
2. **No-year birthday (R8):** choose the component on the emulator — masked `dxe:TextEdit` (`Mask="00/00"`) +
   `ValidateBirthdayInput`, or `dxe:DateEdit` + sentinel year + `DisplayFormat="{0:MM/dd}"`.
3. **Stale note:** `constraints-registry.md § Visual Studio Solution` says the GUID sequence is "currently 0014";
   the real value is `...030` after this task. Consider updating that note (a `.claude/rules/*` edit — deferred,
   out of this task's scope).

---
## Task: 06 — Character-counter threshold alignment — Song/Venue/Person services (+ trimmed-length VM feed)
**Plan:** `Docs/Management/BACKLOG.md` (Form Validation section, Task 06) — replicates the Task 05 ArtistService fix (commit `1e4a858`)
**Status:** To Review
**Started:** 07/02/2026
**Completed:** 07/02/2026

### Summary
Aligned `GetCharacterCounterInfo` `isError` in `SongService`, `VenueService`, and `PersonService` to their
validator boundary: error only when the length EXCEEDS the max (`> Max`), since each validator accepts
exactly-max-length input. Same one-character fix + rationale comment as `ArtistService` (Task 05).
Also aligned the three form ViewModels to feed the **trimmed** length to the counter helpers
(`value?.Trim().Length ?? 0`), replicating the `ArtistFormViewModel` pattern — the call shape was identical
(1:1) in all three VMs, so the Artist pattern applied cleanly.

### Per-service validator-boundary confirmation
| Service | Validator | Boundary evidence | Counter before | Counter after |
|---------|-----------|-------------------|----------------|---------------|
| SongService | `ValidateTitleInput` | rejects only `title.Length > MaxTitleLength` (100); existing test `ValidateTitleInput_MaxLength100_ReturnsTrue` proves 100 is valid | `isError = >= 100` | `isError = > 100` |
| VenueService | `ValidateNameInput` | rejects only `name.Length > MaxInputLength` (30) — exactly 30 is valid | `isError = >= 30` | `isError = > 30` |
| PersonService | `ValidateNameInput` | rejects only `name.Length > MaxInputLength` (200) — exactly 200 is valid | `isError = >= 200` | `isError = > 200` |

No validator was changed — counter aligned to validator only.

### TDD evidence (per service/VM, one at a time)
- SongService: `GetCharacterCounterInfo_AtMaxLength100_IsNotError` seen Red → one-char fix → Green; over-max test added, Green.
- VenueService: `GetCharacterCounterInfo_AtMaxLength30_IsNotError` seen Red → fix → Green (14/14); over-max test added, Green (15/15).
- PersonService: `GetCharacterCounterInfo_AtMaxLength200_IsNotError` seen Red → fix → Green (24/24); over-max test added, Green (25/25).
- SongFormViewModel: `OnSongTitleChanged_TrailingWhitespace_CounterUsesTrimmedLength` seen Red → trim fix → Green (31/31).
- VenueFormViewModel: `OnVenueNameChanged_TrailingWhitespace_CounterUsesTrimmedLength` seen Red → trim fix → Green (8/8).
- PersonFormViewModel: `OnPersonNameChanged_TrailingWhitespace_CounterUsesTrimmedLength` seen Red → trim fix → Green (31/31).

### Changed files:
- `Services/SongService.cs` — `GetCharacterCounterInfo` `isError` `>=` → `>` MaxTitleLength, rationale comment.
- `Services/VenueService.cs` — `GetCharacterCounterInfo` `isError` `>=` → `>` MaxInputLength, rationale comment.
- `Services/PersonService.cs` — `GetCharacterCounterInfo` `isError` `>=` → `>` MaxInputLength, rationale comment.
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` — `OnSongTitleChanged` feeds trimmed length to counter.
- `MyVocaList/UI/ViewModels/VenueFormViewModel.cs` — `OnVenueNameChanged` feeds trimmed length to counter.
- `MyVocaList/UI/ViewModels/PersonFormViewModel.cs` — `OnPersonNameChanged` feeds trimmed length to counter.
- `MyVocaList.Tests/Unit/Services/SongServiceTests.cs` — +2 counter threshold tests.
- `MyVocaList.Tests/Unit/Services/VenueServiceTests.cs` — +2 counter threshold tests.
- `MyVocaList.Tests/Unit/Services/PersonServiceTests.cs` — +2 counter threshold tests.
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` — +1 trimmed-counter test.
- `MyVocaList.Tests/Unit/ViewModels/VenueFormViewModelTests.cs` — +1 trimmed-counter test.
- `MyVocaList.Tests/Unit/ViewModels/PersonFormViewModelTests.cs` — +1 trimmed-counter test.
- `Docs/Management/DevCycleCraft/ui-form-validation-guide/task-log.md` — this entry (existing file, already
  registered in `MyVocaList.sln`; no `.sln` change required).

### Verification evidence
- Build: PASS — `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`: 0 errors (test project builds via `dotnet test`).
- Tests: PASS — full suite 425/425 (baseline 416 + 9 new: 6 service + 3 ViewModel tests), 0 failures.
- Post-edit re-read: confirmed for all 12 changed code files.
- Spec compliance: confirmed — counter/validator alignment per `dialogs-validation.md § Form Validation
  Standard` and the Task 05 reference in `form-validation-task-log.md`; no validator or business rule changed.

### Reported (not changed) — isWarning / ShowCounterAt observations
- **PersonService (B2 flag):** `isWarning > 190` vs `ShowCounterAt = 180`. Internally this is ordered
  (show at >180 → warn at >190 → error at >200) and mirrors the Artist pattern (50/55/60), so no drift of the
  Task 05 kind exists; the earlier B2 concern about the counter counting untrimmed input is now resolved at
  the VM (trimmed feed). The 190 literal (like Artist's 55, Song's 90, Venue's 27) is hardcoded rather than
  derived from `ShowCounterAt`/`Max` — left as-is; decision for Helder whether warning thresholds should be
  formalized as named constants.
- Song (80/90/100) and Venue (25/27/30) warning thresholds are likewise ordered and internally consistent —
  nothing beyond the isError alignment required.
