---
id: ui-component-library-licensing
title: "**UI component library licensing — DevExpress 25.2 is paid; evaluate Syncfusion Community**"
status: "💡 Pending"
target: —
section: DevCycleCraft
kind: feature
order: 530
goal: "settle a licensed UI component library before the MVP ships (DevExpress free offer ended 2024-12-31)."
gate: "Pre-MVP blocker. Helder asks DevExpress about 25.2.4 first; switching vendor needs an architecture review (UI Component Priority is [Unamendable])."
pointer: cross-cutting/ui-component-library-licensing/
---

# UI component library licensing

Origin: Helder's point 1.8 (2026-11-21). He believes DevExpress retired the free MAUI license for solo developers and asked for the least labor-intensive alternative before the MVP: Syncfusion .NET MAUI or MudBlazor (Blazor Hybrid).

Research: `findings.md` (this folder).

Back-reference: none (new item).

## Next session scope (proposed, not started)

1. **Helder:** email DevExpress client services (clientservices@devexpress.com) and ask whether builds on 25.2.4 are licensed for him. Apply for the Syncfusion Community License.
2. **Architecture review:** "UI Component Priority — DevExpress first" in `CLAUDE.md` is `[Unamendable]`. Changing vendor needs a review and an `amend:` commit (old rule, new rule, rationale, effective date).
3. **Time-boxed spike** (done 2026-11-21, results in `findings.md` § 7): port `CrudListView` and one page (Venues, the reference list) to Syncfusion on a throwaway branch. Measure effort per wrapper, MD3 fit, and list performance on the device. Results go to `findings.md`.
4. **Layer leak:** DevExpress types are referenced from ViewModels (`CrudListViewModelBase`, `ICrudListViewModel`, `PersonsViewModel`, `VenuesViewModel`). Removing them helps any outcome.
