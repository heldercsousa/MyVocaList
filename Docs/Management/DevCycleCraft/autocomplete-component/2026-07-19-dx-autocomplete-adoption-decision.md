# Decision Record — Adopt DX `AutoCompleteEdit`, freeze `AutocompleteMobileField` (2026-07-19)

> **Spec updated [2026-07-19]:** dated delta record per the SDD Invariant — the original `design.md`/`requirements.md` for the custom `AutocompleteMobileField` remain as immutable history; this record supersedes their forward direction.

## Decision (approved by Helder, 2026-07-19)

**D-AC1:** The custom full-screen `AutocompleteMobileField` is **frozen**. All autocomplete consumers (SongFormPage Artist field, Person/Artist name dedup entries, and any future form) will use the DevExpress built-in **`AutoCompleteEdit`** (dropdown-style, async suggestions) for the MVP.

## Rationale

1. **The "MudBlazor-ready" premise was false.** A custom MAUI/XAML component does not port to Blazor/Razor. The only portable surface is the Service-layer search logic — which already exists and is UI-agnostic (constitutional "business logic in Services" is the actual migration insurance).
2. **Cost evidence:** the custom component produced BUG-043/044/045/046/047, a latent stacked-navigation defect family, and an unresolved "no match → add new" UX gap, while Critical BUG-027 (song registration impossible) stayed blocked behind it.
3. **Constitutional alignment:** DevExpress-first is a Non-Negotiable; the custom component was effectively an exception to it. DX `AutoCompleteEdit` is mature, themed, and good-enough MVP UX.
4. The full-screen Google-style search pattern (guideline ①) remains valid MD3 UX — retained as a **documented future enhancement**, no longer a gate for anything.

## What is retained

- Service-layer normalization work (BUG-046 whitespace normalization, persisted-string-trimming feature) — UI-agnostic, unaffected.
- Merged fixes for BUG-044/045/047 — remain on develop.

> **Spec updated [2026-07-19] (Helder directive, same day):** the pending Helder E2E steps for BUG-044/045/047 and the BUG-044 back-gesture UX decision are **CANCELLED** — all exist only because of the custom component. Rows archived as `🔵 Superseded` in `backlog-archive/BACKLOG-ARCHIVE-2026-07.md`. Because those fixes touched PersonFormPage navigation/entry behavior, a mandatory **first evaluation step of the DX replacement spec** is: verify whether the stacked-navigation / cursor / stale-popup defect family survives the DX migration on PersonFormPage (and SongFormPage), and add regression coverage for any survivor (new BACKLOG row 2026-07-19 tracks this).
- Guideline ① (`ux-patterns.md` / `m3-components.md` full-screen autocomplete pattern) — kept as future UX documentation.

## Follow-up

- New BACKLOG item: **Replace `AutocompleteMobileField` consumers with DX `AutoCompleteEdit`** — unblocks BUG-027 → Artists & Songs Catalog. Requires spec (brainstorming → spec-reviewer → Helder gate) before implementation.
- Verification of the outcome: replacement spec ACs + on-device smoke test 16C.1 green on SongFormPage.
- Note: the DevExpress MCP demo-app index returned empty for all autocomplete queries on 2026-07-19 — verify MCP index health before the implementation task (MCP Availability Gate).
