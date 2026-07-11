# Autocomplete Component — Evaluation, Rebuild & Rollout Plan

> **Owner:** Helder (architect) · **Registered:** 2026-07-11
> **Why this folder exists:** BACKLOG rows were accumulating long descriptions that are not the
> BACKLOG's purpose. This folder holds the detail; BACKLOG keeps only lean rows pointing here.
> **Scope:** everything about making MyVocaList's autocomplete MD3-compliant — the current
> `AutocompleteField` does not work correctly. Covers the two DevCycleCraft foundation tasks
> (BACKLOG **①** guideline, **②** evaluation) plus the new-component build and its first application.

---

## 0. Governing decision — dependency inversion (2026-07-11)

**Previous ordering (now superseded):** ① *Autocomplete Mobile UX Pattern guideline* was the
predecessor of everything, and ② *AutocompleteField Component Evaluation* depended on ①.

**New ordering (Helder, 2026-07-11):** the guideline is written **last**, from proven concepts —
not first, from theory. Concretely:

- **② is done FIRST, before anyone.** It no longer depends on ①.
- **① now depends on ②** (and on the component build + first application below). The internal
  guideline is updated only after a working, MD3-compliant component proves the correct pattern for
  MD3, CRUD reuse, and code principles.

**Rationale:** guidelines written before a working reference tend to encode assumptions. Building the
real component first, then applying it, produces a proven concept from which the guideline can be
written correctly.

---

## 1. Task sequence (authoritative order)

Each step is gated on the previous one.

| # | Task | BACKLOG identity | Status |
|---|------|------------------|--------|
| 1 | **AutocompleteField Component Evaluation** — evaluate current component vs. the early guideline; decide adjust vs. replace | ② (DevCycleCraft) — **now first** | 💡 Pending |
| 2 | **Build the new MD3-compliant autocomplete component** — reuse what works, fix the failures | ↳ new nested task under ② | 💡 Pending |
| 3 | **Apply the new component to the simplest candidate** in the app | ↳ new nested task (likely maps to an existing task) | 💡 Pending |
| 4 | **Update the internal guideline** (`.claude/library/ux-patterns.md` + `m3-components.md` stub) from the proven concept | ① (DevCycleCraft) — **now last of the foundation work** | 💡 Pending |
| 5 | **Roll out to the remaining ("lacking") places** — all other autocomplete-bearing forms | downstream form tasks (already in BACKLOG) | 💡 Pending |

> The umbrella **Form & Autocomplete UX Overhaul** and its per-form conversion rows still depend on
> the autocomplete foundations being complete — but "the foundations" now means **steps 1–3 above**
> (proven component), with the guideline (step 4) trailing rather than leading.

---

## 2. Task ② — AutocompleteField Component Evaluation (do first)

**Goal:** produce an evaluation + decision (adjust the current `AutocompleteField`, or remove/replace
it) measured against the early guideline. Output = a `findings.md` in this folder + a recommendation
for the step-2 build.

**Governed component:** `AutocompleteField` is consumed by 2+ pages → the
`component-change-governance` four gates apply to any eventual change (dedicated task + MD3 review,
consumer map, per-consumer risk assessment, Helder approval). Known consumers to confirm during the
map: `PersonFormPage`, `SongFormPage` (verify by grep, not from memory).

### Evaluation steps

- **2.2.1 — Register the early guideline.** Done: `md3-autocomplete-mobile-ux-guideline.md` (Gemini 3
  Pro / MD3 official docs, verbatim) is registered in this folder as the expected-behavior early
  guideline for this task.
- **2.2.2 — Read the current implementation.** `MyVocaList/UI/Components/AutocompleteField/`
  (`AutocompleteField.xaml`, `AutocompleteField.xaml.cs`, `AutocompleteDebouncer.cs`). Record what
  works and what fails.
- **2.2.3 — Read the current autocomplete guidance in internal tooling.** Existing rules/skill
  guidance: `persons/autocomplete-design.md`, `persons/plan-autocomplete.md`, and any autocomplete
  notes in `.claude/library/*` (e.g. `crud-pages.md`, `ux-patterns.md`, `dialogs-validation.md`).
- **2.2.4 — Consult skills/MCPs for built-ins.** Check the DevExpress MAUI MCP
  (`mcp__devexpress-maui__*`) and the `myvocalist-coding` / `maui-current-apis` skills for a built-in
  autocomplete / SearchView / filtering component before deciding to hand-roll. Version-pin
  DevExpress 25.2.x per CLAUDE.md.
- **2.2.5 — Harvest reuse from CRUD List pages.** CRUD List pages are visually and behaviorally close
  to autocomplete. Identify what is genuinely shareable between LISTs and the AUTOCOMPLETE component.
  - **2.2.5.1 — Same pattern, fewer elements.** Autocomplete is NOT a full CRUD list: **no title, no
    back button, no bottom floating toolbar, no leading/trailing select checkbox/button on the list
    item.** But the **ListItem pattern is identical** (minus those elements). Likely reuse the very
    same components used by CRUD lists — e.g. `SearchAppBar`, `ListItem` — with the unneeded elements
    **hidden in component slots** when out of autocomplete scope. Confirm further common behavior
    during evaluation.
- **2.2.6 — Decide the render approach for typed-field autocomplete** across the three contexts:
  inside a **bottom sheet**, a **page input**, and a **modal**. Research skills/MCPs and, if needed,
  web/MD3 docs (Rule 6: Context7 first for framework docs, then WebSearch). Conclude the best
  approach — anchored on the early guideline's full-screen-expansion pattern for the compact/phone
  window class, exposed-dropdown for large/desktop.
- **2.2.7 — Gather all current consumers of autocomplete.** Note (do not change) every place using the
  current autocomplete, to evaluate whether each remains a candidate for the new component.

---

## 3. Task — Build the new MD3-compliant autocomplete component (nested under ②)

After the evaluation, create the component. Requirements:

- **Reuse what is well implemented** in the current `AutocompleteField`; **adapt the failures**, based
  on the inferences from the evaluation above.
- **Clear, single goal** for the component.
- **Completely MD3-compliant** — including how the code, elements, and terminology are structured
  (official MD3 names per CLAUDE.md § MD3 terminology; SearchBar/SearchView/Menus-filtering
  vocabulary).
- Reuse the shared CRUD-list components (`SearchAppBar`, `ListItem`, …) with out-of-scope slots
  hidden, per step 2.2.5.1 — subject to the component-change-governance gates.

---

## 4. Task — Apply to the simplest candidate (nested)

Apply the new component to the **simplest** autocomplete candidate in the app first — likely one that
already has a created task. This is the first real application and the source of the proven concept
for the guideline.

---

## 5. Task ① — Update the internal guideline (now last of the foundations)

Only after steps 2–4 succeed: update the internal guides based on the **proven concepts** from the
prior efforts — MD3, CRUD reuse, and code principles. Lightweight scope (per BACKLOG ①): a short
section in `.claude/library/ux-patterns.md` (+ cross-ref stub in `m3-components.md`), MD3 currency
checked inline against m3.material.io (SearchBar → SearchView / Menus filtering). No spike, no
separate spec folder — this folder is the record.

---

## 6. Task — Roll out to the lacking places

With the guideline updated, implement the remaining autocomplete-bearing places: the `form-ux-redesign`
autocomplete phases, BUG-027 (SongFormPage Artist field), and the Artist/Song/Person form autocomplete
work. These already exist as BACKLOG rows and stay there; they now consume the proven component +
guideline instead of gating on an up-front theoretical guideline.

---

## Cross-references

- Early guideline: `./md3-autocomplete-mobile-ux-guideline.md`
- Current component: `MyVocaList/UI/Components/AutocompleteField/`
- Parked redesign that consumes this: `BusinessFeatures/artists-songs/changes/2026-07-10-form-ux-redesign/`
- Governance: `.claude/rules/component-change-governance.md` (four gates for `AutocompleteField`)
- BACKLOG rows: **①** *Autocomplete Mobile UX Pattern guideline*, **②** *AutocompleteField Component
  Evaluation*, and the *Form & Autocomplete UX Overhaul* umbrella.
