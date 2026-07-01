# Plan — Form Validation Guide (Task 01 of "Form validation" feature)

> **Scope:** This document defines the exact edits that establish a single form-input validation
> standard across all MyVocaList form pages, recorded in the project's Claude internal tooling
> (`.claude/library/*.md`). Task 01 authors those guideline edits — it is docs-only (no `.cs`/`.xaml`
> source, no `CLAUDE.md`, no `.claude/rules/*`).
>
> **Requirements source:** `Docs/Management/DevCycleCraft/ui-form-validation-guide/01-ui-form-validation-guide.md`
> **Reference implementation:** Venue form (single-field) and Person/"Singer" form (multi-field, with date + email).
>
> **Revision (2026-07-01):** Finalized from the draft plan with the three mandatory pre-implementation
> fixes applied — see §0.

---

## 0. Pre-implementation fixes applied (2026-07-01)

The draft plan was internally contradictory (it authored the on-blur wiring as fact while deferring
confirmation of the DX blur hook to a later spike) and carried a stale `.sln` GUID assumption. The
following three fixes were resolved BEFORE authoring the guide:

### Fix 1 — DX blur mechanism CONFIRMED (no longer deferred)
Confirmed via **DevExpress MAUI MCP** (demo-app docs/code — not indexed for this query) and
**Context7** (`/websites/devexpress_maui`, DevExpress 25.2.x) on 2026-07-01:

- **Blur hook for standalone editors — CONFIRMED.** `dxe:TextEdit` and `dxe:DateEdit` inherit from
  MAUI `VisualElement`, which exposes the **`Unfocused`** event ("Occurs when this element is
  unfocused. Inherited from VisualElement" — DevExpress DateEdit / editor member docs). This is the
  confirmed, MAUI-current blur hook for the app's current standalone-editor form architecture. The
  page code-behind subscribes to `Unfocused` on the editor and invokes a VM `Validate<Field>Command`.
- **DX-native alternative (documented, not currently used).** If a form is ever migrated to
  `dxdf:DataFormView`, the built-in mechanism is `ValidationMode="LostFocus"` (also `Input` /
  `Manually`) with the `ValidateProperty` event. The app does **not** use `DataFormView` today —
  it uses standalone editors — so `Unfocused` is the standard. `ValidationMode="LostFocus"` is
  recorded as the migration-time equivalent.
- **Full masked dates — CONFIRMED.** Masked text entry is `dxe:TextEdit` with the `Mask` property
  (e.g. `Mask="+1 (000) 000-0000"`, `MaskPlaceholderChar`). `dxe:DateEdit` is picker-based: its
  `Date` property is a full `DateTime`, with `DisplayFormat` (e.g. `{0:MMMM dd}`), `MinDate`,
  `MaxDate`. DateEdit therefore cannot produce an out-of-range date via the picker (built-in
  validity), satisfying "reuse a specialized component" (R9) for full dates.
- **Day/month-only (no-year) birthday — OPEN (Helder gate).** DevExpress docs show **no** masked
  day/month-only text-entry mode on `dxe:DateEdit` (its value is always a full `DateTime`; a year is
  always present even if `DisplayFormat` hides it). Two viable paths exist and the choice is a UX
  decision, so the guide authors this subsection **structurally** with the exact component left as a
  clearly-marked `> OPEN — confirm on emulator (Helder gate)` stub:
  1. Masked `dxe:TextEdit` (`Mask="00/00"`) + service-side month/day validation (`ValidateBirthdayInput`), or
  2. `dxe:DateEdit` with a fixed sentinel year + `DisplayFormat="{0:MM/dd}"` (leans on R9's "reuse the
     specialized validator").

  This is the only OPEN item in the timing/wiring standard; the blur hook itself is **fact**, not a guess.

### Fix 2 — UX skill consulted (`ux:interaction-design`, 2026-07-01)
The requirements doc (~line 9) is a "Crucial" instruction to validate against the project UX skill.
Applying Nielsen's heuristics confirmed the standard (blur → keystroke-on-error → submit-safety-net,
inline per-field errors) is consistent. Two material refinements were folded into the guide:
1. **Error prevention (H5) — no blur error on a pristine field.** Do not fire a blur validation error
   on a field the user only tabbed through without editing (dirty-tracking). Validate on blur only
   once the field is dirty, or on Save. This prevents the "impatient teacher" firing on untouched fields.
2. **Visibility of status (H1) + error recovery (H9).** Async guidance/availability/uniqueness checks
   must surface a pending indicator, and every error message must be specific and actionable (say what
   is wrong and how to fix it) — never a bare "Invalid".

### Fix 3 — `.sln` GUID verified against the actual file (2026-07-01)
`constraints-registry.md § Visual Studio Solution` claims the sequence is "currently `0014`" — that
note is **stale**. The actual `MyVocaList.sln` highest-used GUID is
`{FA1234BC-0001-4000-8000-000000000029}` (`backlog-first-registration`, under DevCycleCraft). The next
free value is therefore `{FA1234BC-0001-4000-8000-000000000030}` — which happens to match the draft's
value. This plan registers `plan.md` and `task-log.md` and confirms `01-ui-form-validation-guide.md`
under a new DevCycleCraft solution folder using GUID `...030`.

---

## 1. Context established during research

### 1.1 What the requirements doc mandates (UX)
1. **Validate on blur** is the standard for standard fields (NN/g, Baymard).
2. **Gold standard — "punish late, reward early":** validate on *blur* for initial input; once a field
   is in an error state, switch that field to validate *on keystroke* so the error clears the instant it is fixed.
3. **Submit** is a final safety net only — reserved for cross-field checks (e.g. confirm-password, inventory)
   and heavy server-side auth. Never the primary channel ("Wall of Red" anti-pattern is forbidden).
4. **Keystroke (with ~500 ms debounce)** is allowed only for real-time guidance: character counter,
   password-strength meter, username/availability lookups.
5. **Masks are mandatory and never persisted.** Dates are masked in the UI (auto `/`), stored as a DB date
   type, and re-formatted on display. The user manipulates only the day/month/year numbers, never the separators.
6. **Dates are locale-dependent:** `MM/dd/yyyy` (English), `dd/MM/yyyy` (pt-BR, future). 6 languages planned,
   including Japanese — the standard must not hard-code a single date format.
7. **Special case:** MyVocaList has a date typed **without the year** (day/month only — the Person birthday).
8. **Reuse specialized components** for date validation (component-provided month/day/year validation) rather
   than hand-rolled validators.
9. **Integer** validation — the requirements doc section is INCOMPLETE (`<TODO> - complete Integer and append any`).

### 1.2 What the codebase currently does (reference forms)
| Aspect | Current behavior (Venue + Person forms) | Requirements target | Delta |
|--------|------------------------------------------|---------------------|-------|
| **When validation fires** | On **Save** only. Keystroke merely *clears* the error. No on-blur validation exists anywhere. | On **blur** (dirty fields), then keystroke while in error; Save as safety net. | **Behavior gap** — reference forms must be upgraded (later tasks). DX blur hook now CONFIRMED = `Unfocused`. |
| **Error surfacing** | Inline per-field via `dxe:TextEdit HasError`/`ErrorText`. No summary, no dialog. | Same. | **Already compliant.** |
| **Service validation** | Tuple `(bool isValid, string message)` per field; CRUD guards re-validate. | Same. | **Already compliant.** |
| **Error routing** | Person `SaveAsync` routes a single service message to a field by substring match — fragile. | Field-addressed errors. | Minor risk; flagged in guide as anti-pattern. |
| **Character counter** | Keystroke-driven; warning/error color triggers. | Keystroke-with-guidance exception. | **Already compliant.** |
| **Date field** | Person birthday = plain `dxe:TextEdit`, placeholder `DD/MM`, regex `^(\d{1,2})/(\d{1,2})$`, day/month only, **no mask**. | Masked, component-validated, locale-aware, never-stored separators. | **Gap + OPEN** — no-year special case needs a Helder decision (Fix 1). |
| **Locale** | `useLocalization:false`, English only, no `.resx`. | 6 languages incl. Japanese; locale-aware date format. | **Constraint** — locale-aware masks are future work. |

### 1.3 Constitutional constraints the standard must honour (from CLAUDE.md)
- DevExpress-first (`dxe:TextEdit`, `dxe:DateEdit`, `dx:BottomSheet`) — no stock MAUI unless DX has no equivalent.
- Native-dialog ban — never `DisplayAlert`/`DisplayActionSheet`/`DisplayPromptAsync` for validation feedback.
- MD3 terminology for any component/style names.
- Business logic in Services — validation rules live in the Service (`Validate*` methods), never in ViewModel/page.
  ViewModel only *invokes* validation and maps the result to `HasError`/`ErrorText`.
- English-only text.

---

## 2. The canonical validation standard this plan mandates

> Single pattern that forms 02–05 (Person/Singer, Songs, Artists — plus the existing Venue reference)
> must follow. Primary home: `.claude/library/dialogs-validation.md`; cross-referenced from
> `crud-pages.md`, `devexpress-patterns.md`, `theme-locale.md`, and `ux-patterns.md`.

**Timing (per field):**
1. **On blur** (field `Unfocused`): if the field is **dirty** (has been edited), run the field's service
   validator; set `HasError`/`ErrorText`. Do NOT fire an error on a pristine field the user only tabbed
   through (Fix 2, H5).
2. **While a field is in error:** validate that field **on every keystroke** so the error clears the
   moment it becomes valid ("reward early"). On `TextChanged`, if `<Field>HasError` is currently true,
   re-run the validator; clear on valid. Do NOT run full validation on keystroke for a field not yet in
   error ("impatient teacher" anti-pattern).
3. **On Save:** re-run all field validators (safety net) plus cross-field / uniqueness / DB checks
   (duplicate name, email-taken). Save-time service failures map back to the offending field's
   `HasError`/`ErrorText`.

**Surfacing:** inline, per field, via `dxe:TextEdit`/`dxe:DateEdit` `HasError` + `ErrorText`. Never a
summary banner, dialog, or snackbar for *validation* errors (snackbar is success/non-blocking feedback only).
Error text must be **specific and actionable** (Fix 2, H9).

**Blur hook — CONFIRMED:** subscribe to the editor's `Unfocused` event (MAUI `VisualElement`, inherited by
`dxe:TextEdit`/`dxe:DateEdit`) in page code-behind → invoke VM `Validate<Field>Command`. If migrated to
`dxdf:DataFormView`, use `ValidationMode="LostFocus"` instead.

**Guidance exceptions (keystroke + ~500 ms debounce):** character counter (existing), plus future
availability/strength lookups — with a pending status indicator (Fix 2, H1). Debounce follows the existing
`VenuesViewModel` search-debounce pattern.

**Service layer:** one `Validate<Field>Input(...)` method per field returning `(bool isValid, string message)`;
CRUD methods re-validate and return `(bool success, string message[, entity])`. No C#-side normalization for
uniqueness (constraints-registry HARD RULE — use DB collation).

**Dates:** full dates use `dxe:DateEdit` (picker + `DisplayFormat`, built-in validity) or masked
`dxe:TextEdit` (`Mask`); separators are display-only and never persisted; format is locale-driven (future —
English `MM/dd/yyyy`). The **day/month no-year special case** (Person birthday) is an OPEN item (Helder gate) —
DateEdit has no masked no-year entry; the two candidate paths are documented in §6.

**Integers:** placeholder subsection only, marked "spec-incomplete — see §6"; cannot be authored until the
requirements doc's Integer section is completed by Helder.

---

## 3. Files to change and exact edits

### 3.1 `.claude/library/dialogs-validation.md` — PRIMARY
New top-level section **`## Form Validation Standard`** after `## TextEdit Validation (HasError / ErrorText)`:
- **`### Validation timing — blur first, keystroke on error, submit as safety net`** — the three-phase rule,
  the dirty-field refinement (Fix 2), the decision table (field type → when it validates), and the
  anti-patterns table (Wall-of-Red, Impatient-Teacher).
- **`### Wiring the pattern (ViewModel + XAML)`** — VM template (`<Field>HasError`/`<Field>ErrorText`,
  `Validate<Field>()` calling `_service.Validate<Field>Input(...)`, on-blur command, keystroke-on-error
  handler); XAML with `Unfocused` → `Validate<Field>Command` (CONFIRMED hook); DataForm `LostFocus`
  migration note.
- **`### Error surfacing — inline only`** — inline `HasError`/`ErrorText`; no summary/dialog/snackbar;
  specific + actionable messages; field-addressed errors over substring routing (call out Person `SetInlineError`).
- **`### Masked inputs — dates`** — mandate mask (never persisted); DB date → display format; locale-driven;
  full date via `dxe:DateEdit`/masked `dxe:TextEdit`; **day/month no-year OPEN stub (Helder gate)**.
- **`### Integer inputs`** — spec-incomplete stub (R10 escalation).
- Update the existing "Never use `DisplayAlert` for validation errors" line to cross-reference the new standard.

### 3.2 `.claude/library/crud-pages.md` — `## Form Page — Laws and Variants`
- Add **`### Validation (law)`** — one-paragraph pointer to the standard in `dialogs-validation.md`
  (single-sourced, not a copy).
- In the existing **`### Never`** list: "Do not validate on Save only (Wall-of-Red) — see Validation Standard."

### 3.3 `.claude/library/devexpress-patterns.md`
- Under `## TextEdit (Editors)`: note that field-level blur validation is wired via the MAUI `Unfocused`
  event (CONFIRMED) calling a VM `Validate<Field>Command`.
- New `## DateEdit (Editors) — masked dates`: `dxe:DateEdit` (picker, `DisplayFormat`, `MinDate`/`MaxDate`)
  vs masked `dxe:TextEdit` (`Mask`), cross-ref the substitution-table row `DatePicker → dxe:DateEdit`; flag
  the day/month-only no-year case as OPEN.

### 3.4 `.claude/library/theme-locale.md` — `## Locale`
- Bullet: date input/display format is locale-dependent (English `MM/dd/yyyy`, pt-BR `dd/MM/yyyy`, Japanese
  TBD); localization currently disabled; locale-aware masks are future work — the Standard states intent only.

### 3.5 `.claude/library/ux-patterns.md`
- Short `## Form Validation Timing (pointer)` — the Fix 2 refinements (no blur error on pristine field;
  pending indicator + actionable messages) + pointer to the primary standard.

### 3.6 NO change to `CLAUDE.md` or `.claude/rules/*.md`
The canonical pattern lives entirely in `.claude/library/*`. The tuple `(bool isValid, string message)`
idiom is already in `.claude/rules/code-principles.md § Service Return Patterns` and needs no edit. Keeping
the standard out of `.claude/rules/*` avoids triggering the `amend:` process. IF a future review decides the
`Validate<Field>Input` naming convention belongs in `code-principles.md`, that edit triggers the
Amending-These-Rules process (`amend:` prefix + changelog + human authorship) — NOT part of task 01.

---

## 4. How the Venue form maps onto the standard (reference implementation)

| Standard element | Venue form today | Action for reference status |
|------------------|------------------|-----------------------------|
| Inline error (`HasError`/`ErrorText`) | ✅ `NameHasError`/`NameErrorText` on `dxe:TextEdit` | none |
| Service validator tuple | ✅ `VenueService.ValidateNameInput` → `(bool,string)` | none |
| Character counter (keystroke exception) | ✅ `ShouldShowCharacterCounter`/`GetCharacterCounterInfo` | none |
| Submit safety net + uniqueness | ✅ `CreateVenueAsync`/`UpdateVenueAsync` re-validate + duplicate check | none |
| **Blur validation** | ❌ only clears error on keystroke; validates on Save | **Add blur wiring** (`Unfocused` → `ValidateNameCommand`) — later task |
| **Keystroke-on-error** | ❌ keystroke clears unconditionally | **Change** name-changed handler to re-validate when `NameHasError` — later task |

Venue is the simplest (single field, no date) → canonical single-field reference once blur + keystroke-on-error are added.

### 4.1 What forms 02–05 must copy
- **02 — Person/"Singer"** (`PersonFormPage`, `PersonFormViewModel`, `PersonService`): multi-field reference
  (name via `AutocompleteField`, **birthday date**, email). Add blur + keystroke-on-error, resolve the no-year
  birthday component (Helder gate), replace `SetInlineError` substring routing with field-addressed errors.
- **03 — Songs** (`SongFormPage`): copy for text fields; date/integer follow those subsections (integer blocked).
- **04 — Artists** (`ArtistFormPage`): copy for text fields.
- **05** — any remaining form: same standard.

---

## 5. Requirement → guideline-section traceability

| # | Requirement | Guideline section | Status |
|---|-------------|-------------------|--------|
| R1 | Validate on blur (standard) | dialogs-validation § Validation timing | Mapped |
| R2 | Gold standard: blur→keystroke on error | dialogs-validation § Validation timing + Wiring | Mapped |
| R3 | Submit = cross-field/heavy safety net only | dialogs-validation § Validation timing | Mapped |
| R4 | Keystroke+debounce for counter/availability | dialogs-validation § Validation timing (exceptions) | Mapped |
| R5 | Inline errors, no Wall-of-Red, no native dialog | dialogs-validation § Error surfacing; crud-pages § Form Page Never | Mapped (partly already compliant) |
| R6 | Masks mandatory, never persisted; DB date auto-formatted | dialogs-validation § Masked inputs + devexpress-patterns § DateEdit | Mapped (impl later) |
| R7 | Locale date formats (MM/dd vs dd/MM; Japanese) | theme-locale § Locale + dialogs-validation § Masked inputs | Mapped as **future/constraint** |
| R8 | Date special case — day/month, no year | dialogs-validation § Masked inputs (**OPEN — Helder gate**) | Mapped, decision pending |
| R9 | Validate day/month/year via specialized component (reuse) | devexpress-patterns § DateEdit | Mapped (DateEdit = full date; no-year OPEN) |
| **R10** | **Integer validation** | dialogs-validation § Integer inputs (stub) | **CANNOT MAP — requirements incomplete (`<TODO>`). Escalated to Helder.** |

---

## 6. Gaps, risks, and open items

1. **Requirements Integer section incomplete (BLOCKER for full coverage).** Ends with `<TODO> - complete
   Integer and append any`. R10 cannot be authored. **Action:** Helder completes the Integer + any remaining
   types before that subsection is written. Task 01 ships an explicit "spec-incomplete" stub for integers.
2. **On-blur validation is a behavior change, not just docs.** The guide describes the target; the reference
   forms (Venue, Person) need code changes in later tasks. The DX blur hook is **CONFIRMED = `Unfocused`**
   (Fix 1) — no spike needed for the hook itself.
3. **DateEdit vs the no-year special case — OPEN (Helder gate).** DevExpress has no masked day/month-only
   text entry on `dxe:DateEdit` (value is always a full `DateTime`). Candidate paths: (a) masked `dxe:TextEdit`
   `Mask="00/00"` + `ValidateBirthdayInput`; (b) `dxe:DateEdit` + sentinel year + `DisplayFormat="{0:MM/dd}"`.
   Confirm on emulator with Helder before the Person-form birthday is implemented.
4. **Locale is disabled.** 6-language + Japanese date formats require localization (`useLocalization:false`,
   no `.resx`). Locale-aware masks are future work; the guide states intent only (R7).
5. **Requirements doc is untracked.** `01-ui-form-validation-guide.md` exists only as an untracked file in the
   develop working copy; it is not committed and (until now) not in `MyVocaList.sln`. Task 01 pre-registers its
   `.sln` entry alongside `plan.md`/`task-log.md` so it becomes visible in VS once Helder commits it. Task 01
   does **not** author or move that file (out of edit scope).
6. **Person `SetInlineError` substring routing is fragile.** The standard prescribes field-addressed errors;
   the substring-match approach is flagged as the anti-pattern to remove during the Person-form upgrade.

---

## 7. `.sln` registration

New Solution Folder `ui-form-validation-guide` under the **DevCycleCraft** parent
(`{0C4BA720-519E-4818-BD9B-34AC19E4FCD7}`), GUID `{FA1234BC-0001-4000-8000-000000000030}` (verified next-free —
see Fix 3; `constraints-registry.md`'s "0014" note is stale). Registers `plan.md`, `task-log.md`, and
`01-ui-form-validation-guide.md` (pre-added so it is visible once committed).

---

## 8. Suggested downstream task breakdown (not part of task 01)

- **01 (this)** — author the Validation Standard in `.claude/library/*` per §3 (docs-only; DevCycleCraft item).
- **[DECISION — Helder]** — choose the no-year birthday component (masked `dxe:TextEdit` vs sentinel-year
  `dxe:DateEdit`) on the emulator.
- **02** — upgrade Venue reference form (add blur + keystroke-on-error).
- **03** — upgrade Person/"Singer" form (blur, keystroke-on-error, birthday component, field-addressed errors).
- **04–05** — apply the standard to Songs and Artists forms.
- **(blocked)** — Integer validation guideline + application, pending Helder completing the requirements doc.
