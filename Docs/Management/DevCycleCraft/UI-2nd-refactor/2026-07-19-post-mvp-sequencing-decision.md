# Decision Record — MudBlazor Unification Is Post-MVP (2026-07-19)

> **Spec updated [2026-07-19]:** dated delta record per the SDD Invariant; prior research/`ui-arch-decision-plan.md` remain history. This record fixes the sequencing.

## Decision (approved by Helder, 2026-07-19)

**D-AC2:** The Blazor Hybrid + MudBlazor + shared RCL unification is **explicitly post-MVP**. The MVP ships on .NET MAUI + DevExpress; no UI replatforming before release.

## Rationale

- The architecture is genuine and Microsoft-supported — not abandoned. But as a pre-MVP replatform it is oversized for a one-person team: full page rewrite, MD3-fidelity re-solving (MudBlazor is not fully MD3), WebView-rendered mobile UX trade-offs, and discarding the mature DevExpress investment — months of solo effort before any user has the MVP.
- The original drivers (no DX Windows renderer; future browser UI) are real but not MVP requirements for a mobile-first karaoke host tool.

## Sequencing (the middle path)

1. **Ship MVP on MAUI + DevExpress.**
2. **Migration insurance stays architectural, not UI-level:** business logic in Services, thin ViewModels — the portable surface under any future UI.
3. **First web need (natural trigger: Singer self-registration)** → build a plain **Blazor web app sharing Services/Contracts**, without touching the mobile UI.
4. **Re-evaluate full unification post-MVP** via the planned spike, with real usage data; treat "one UI everywhere" as a candidate v2 / community-worthy project.

## BACKLOG effects

- `UI-2nd-refactor` stays 📋 Spec, gated post-MVP.
- **Windows version** stays 🔴 Blocked (deprioritized; not an MVP need).
- Custom-autocomplete rows re-scoped per the companion record `../autocomplete-component/2026-07-19-dx-autocomplete-adoption-decision.md`.
