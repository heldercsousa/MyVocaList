# Plan: UI Architecture Decision & Tooling Setup
**Scope:** Visual Theme Refresh → ui-2nd-refactor decision  
**Date:** 2026-06-02  
**Status:** Reviewed — blockers resolved, warnings documented, pending manual amends

---

## Context

The app UI is described as "too dark and monochromatic" for a Karaoke/Bandokê app. Two paths exist in BACKLOG:

- **Path A — Theme Refresh Only**: Keep MAUI native + DevExpress, apply a vibrant Karaoke-themed palette
- **Path B — ui-2nd-refactor**: Replace the entire UI layer with Blazor Hybrid + shared RCL, enabling one codebase for mobile (Android/iOS/Windows) and future web

Helder ran a deep research session with Gemini AI (docs in `UI-2nd-refactor/`). Conclusion: **Blazor Hybrid + MudBlazor is the target post-MVP direction** pending spike validation. Hard constraints driving this:
1. DevExpress MAUI has **no Windows/WinUI3 support** (hard blocker, no roadmap)
2. A future commercial website must share **exact UI code** with the mobile app — only Blazor Hybrid (RCL) achieves this

> **Pending spike validation** — the DevExpress-first constitutional constraint remains in full effect until the spike produces a go decision.

---

## Architecture Decision (direction set, pending spike go/no-go)

### Target Solution Structure

```
MyVocaList.UI.Shared     (Razor Class Library — ALL UI lives here)
  ├── Pages/*.razor
  ├── Components/*.razor
  ├── wwwroot/ (CSS, fonts, icons)
  ├── MudBlazorTheme.cs  ← Karaoke Neon palette via MudTheme
  └── References: Services, Contracts

MyVocaList               (MAUI host — thin shell only)
  ├── MainPage.xaml → <BlazorWebView HostPage="wwwroot/index.html" />
  ├── AndroidManifest, Info.plist, MauiProgram.cs
  └── References: UI.Shared

MyVocaList.Web           (future — Blazor Web App, zero UI rewrite)
  └── References: UI.Shared
```

All non-UI layers (Domain, Infra, Services, Contracts, Tests) survive unchanged.

### Component Library: MudBlazor 9.5.0
- Targets .NET 8/9/10 — confirmed .NET 10 compatible (NuGet `MudBlazor 9.5.0`, May 2026)
- `MudTheme` C# class centralises all color tokens — inject Karaoke palette once, all components inherit
- AI coding (Claude Code) generates idiomatic MudBlazor reliably — vast training data
- GPU-accelerated CSS animations (`transform` + `opacity`) validated for pulsing speaker / floating notes UX
- Responsive layout (media queries + MudGrid) auto-adapts from 6" phone to ultrawide monitor

### Known Tradeoffs (documented by review)

| Tradeoff | Details | Mitigation |
|----------|---------|-----------|
| WebView performance on low-end Android | BlazorWebView adds a rendering layer. On budget Android devices, jank may be visible in lists. | Spike must test on a mid-range 2021 Android device. Pass threshold: 60fps scroll in VenuesPage list with 100+ items. Fail = return to spike for optimisation or reconsider migration. |
| Hot reload limitations | MAUI Hot Reload does not apply to Razor component changes inside BlazorWebView the same way it applies to XAML. Developer velocity is slower during spike/migration. | Accept as a known DX regression. Use browser F5 refresh pattern during development. |
| No JS interop needed | Razor components call C# services directly via DI — no JS bridge for SQLite/services. This is correct and expected. | No mitigation needed; documenting to avoid future confusion. |

---

## Tooling

### MudMCP — installed and fixed
- Cloned to `C:/Users/helde/.claude/tools/MudMCP`
- Working tree fixed to `v9.5.0` tag (reviewer found initial clone had HEAD at main, only 8 components indexed)
- Stale `index.json` deleted — next `dotnet run` rebuilds with all ~100 components
- `.mcp.json` updated: `--version 9.5.0`
- 12 tools: `list_components`, `get_component_detail`, `get_component_parameters`, `get_component_examples`, `search_components`, `get_api_reference`, `get_enum_values` + 5 more
- **Activate only during Blazor Hybrid / MudBlazor work** — not for current MAUI-native development

### Stitch MCP — deferred (correct)
- Stitch generates pure CSS/HTML/Tailwind, **not MudBlazor** — confirmed by Gemini research
- Its value is design token export (palette, typography) → manual `MudTheme` mapping
- Requires Google Cloud project + Stitch API + design work in Stitch web UI first
- Correct sequence when Stitch becomes relevant (post-spike):
  1. Create Google Cloud project + `gcloud auth login`
  2. Design Karaoke Neon theme in Stitch web UI
  3. Export `DESIGN.md` (color tokens)
  4. Map hex values to `MudTheme` C# properties manually
  5. MudMCP handles all component generation from that point

### Figma MCP — deferred
- $15/month minimum (6 calls/month on free tier = useless for real work)
- No MudBlazor Code Connect support
- Revisit post-MVP when web design phase begins

---

## Timing Decision: Parallel Spike (approved)

The spike approach runs in parallel with Queue Management (the core MVP feature):
- `MyVocaList.Blazor.Spike` — new MAUI project in same solution
- `MyVocaList.UI.Shared` — new RCL in same solution
- Existing `MyVocaList` (DevExpress) unchanged — Queue Management proceeds there

### Spike scope (extended after review)

Initial scope (VenuesPage + PersonsPage) was flagged as insufficient — it doesn't cover the interaction patterns most divergent from DevExpress. Extended scope:

| Item | Why it validates |
|------|-----------------|
| VenuesPage (list + search) | Basic rendering, MudDataGrid/MudList, DI injection |
| PersonsPage (list) | Confirms DI pattern across multiple pages |
| **Add Person dialog** (BottomSheet equivalent) | Validates `MudDialog`/`MudDrawer` as replacement for `dx:BottomSheet` constitutional constraint |
| **VenueFormPage or PersonFormPage** | Validates FluentValidation + MudBlazor form pattern |
| Performance: 100-item scroll on mid-range Android | Validates 60fps threshold (new explicit pass/fail criterion) |

### Spike output
- `Docs/Management/BusinessFeatures/UI-2nd-refactor/findings.md` — go/no-go evidence
- Time-box: 3–4 days (extended from 2–3 to cover dialog + form pages)

---

## Pending Manual Amends (write-protected files)

These cannot be committed by Claude Code — require Helder to manually apply via `amend:` commit.

### 1. CLAUDE.md — fix "will be replaced" wording + annotate DevExpress-first constraint

Current line added this session:
```
**Post-MVP UI migration:** Blazor Hybrid + MudBlazor + shared RCL. DevExpress MAUI will be replaced (no Windows/WinUI3 support). Research + decision: `Docs/Management/BusinessFeatures/UI-2nd-refactor/`.
```

**Change to:**
```
**Post-MVP UI migration (pending spike):** Blazor Hybrid + MudBlazor + shared RCL is the target direction. DevExpress MAUI is the target for replacement (no Windows/WinUI3 support) pending spike go decision. Research + decision: `Docs/Management/BusinessFeatures/UI-2nd-refactor/`.
```

**Add annotation to Constitutional Constraints — UI Component Priority:**
After `DevExpress first, always.` add: ` Note: Blazor Hybrid migration under evaluation (see Stack § Post-MVP UI migration). DevExpress-first rule remains in full effect until spike produces a go decision.`

### 2. CLAUDE.md — add MudMCP to MCP & Skills section

In `### MCP Security Stance`, add to approved server list:
```
- MudMCP (`mudblazor`) — community server `mcbodge/MudMCP`, cloned locally at `C:/Users/helde/.claude/tools/MudMCP`; provides 12 tools for MudBlazor component docs and API reference. **Activate only during Blazor Hybrid / MudBlazor spike or migration work**. Do not activate for current MAUI-native development sessions.
```

In `### MCP Context Budget`, add:
```
- Blazor Hybrid / MudBlazor work: MudMCP only (deactivate DevExpress MCP — no overlap)
```

### 3. `.claude/rules/constraints-registry.md` — two new entries

**Add to `## DevExpress / UI` section:**
```
- **DevExpress MAUI — no Windows/WinUI3 support (hard architectural constraint):** DevExpress MAUI components do not support Windows/WinUI3. Any feature requiring Windows desktop support must use an alternative framework. This is the architectural driver for the post-MVP Blazor Hybrid + MudBlazor migration. See `Docs/Management/BusinessFeatures/UI-2nd-refactor/`.
```

**Add new `## Design / Prototyping Tools` section at the bottom:**
```
## Design / Prototyping Tools

- **Stitch MCP — generates CSS/HTML/Tailwind, NOT MudBlazor:** Stitch converts designs to pure CSS + HTML or Tailwind — no MudBlazor Code Connect integration. Do NOT use Stitch to generate MudBlazor components. Use MudMCP (`mudblazor` server key) for component docs and generation. Stitch's only value in this project is design-token export (palette, typography) → manual mapping to `MudTheme`. Evaluated 2026-06-02.
- **Figma MCP — no MudBlazor Code Connect; $15/month minimum:** Figma MCP requires paid plan ($15/month Professional) for useful call volume. No MudBlazor Code Connect plugin exists. Not suitable for MudBlazor code generation. Evaluated 2026-06-02.
```
