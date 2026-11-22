---
id: ui-component-library-licensing
title: "**UI library migration — DevExpress → Syncfusion, coexisting page by page**"
status: "📋 Spec"
target: —
section: DevCycleCraft
kind: feature
order: 530
goal: "replace every DevExpress control with Syncfusion before the MVP launch, keeping the app working at every commit."
gate: "Pre-MVP blocker. Spec under review; Helder approves the design and the rule amendment before any code."
pointer: cross-cutting/ui-component-library-licensing/
---

# UI component library licensing

Origin: Helder's point 1.8 (2026-11-21). He believes DevExpress retired the free MAUI license for solo developers and asked for the least labor-intensive alternative before the MVP: Syncfusion .NET MAUI or MudBlazor (Blazor Hybrid).

Research: `findings.md` (this folder).

**Decision (Helder, 2026-11-21):** migrate to Syncfusion (Toolkit + Community-License suite), with zero DevExpress before launch. DX and Sf copies coexist; pages cut over one by one (approach B). Spec: `requirements.md`, `design.md`.

Back-reference: none (new item).

## Next session scope (proposed, not started)

1. **Helder:** email DevExpress client services (clientservices@devexpress.com) and ask whether builds on 25.2.4 are licensed for him. Apply for the Syncfusion Community License.
2. **Architecture review:** "UI Component Priority — DevExpress first" in `CLAUDE.md` is `[Unamendable]`. Changing vendor needs a review and an `amend:` commit (old rule, new rule, rationale, effective date).
3. **Time-boxed spike** (done 2026-11-21, results in `findings.md` § 7): port `CrudListView` and one page (Venues, the reference list) to Syncfusion on a throwaway branch. Measure effort per wrapper, MD3 fit, and list performance on the device. Results go to `findings.md`.
4. **Layer leak:** DevExpress types are referenced from ViewModels (`CrudListViewModelBase`, `ICrudListViewModel`, `PersonsViewModel`, `VenuesViewModel`). Removing them helps any outcome.
