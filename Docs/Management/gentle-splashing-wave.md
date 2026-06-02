# Plan: UI Architecture Decision & Tooling Setup
**Scope:** Visual Theme Refresh → ui-2nd-refactor decision  
**Date:** 2026-06-02  
**Status:** Revised after Helder's Q&A + MudBlazor tooling research

---

## Context

The app UI is described as "too dark and monochromatic" for a Karaoke/Bandokê app. Two paths exist in BACKLOG:

- **Path A — Theme Refresh Only**: Keep MAUI native + DevExpress, apply a vibrant Karaoke-themed palette
- **Path B — ui-2nd-refactor**: Replace the entire UI layer with Blazor Hybrid + shared RCL

Helder ran a deep research session with Gemini AI (docs in `UI-2nd-refactor/`). Conclusion: **Blazor Hybrid + MudBlazor is the correct long-term architecture.** Hard constraints driving this:
1. DevExpress MAUI has **no Windows/WinUI3 support** (hard blocker, no roadmap)
2. A future commercial website must share **exact UI code** with the mobile app — only Blazor Hybrid (RCL) achieves this

---

## Architecture Decision (confirmed)

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

### Component Library: MudBlazor (confirmed)
- `MudTheme` C# class centralises all color tokens — inject Karaoke palette once, all components inherit
- AI coding (Claude Code) generates idiomatic MudBlazor reliably — vast training data
- GPU-accelerated CSS animations (`transform` + `opacity`) validated for pulsing speaker / floating notes UX
- Responsive layout (media queries + MudGrid) auto-adapts from 6" phone to ultrawide monitor

---

## Tooling Decision (corrected after research)

### What to install NOW

#### 1. MudMCP — community MCP server for MudBlazor
- **What**: Clones MudBlazor repo, parses via Roslyn, exposes 12 MCP tools to Claude Code
- **Why**: Prevents AI hallucination about MudBlazor component APIs; enables accurate component generation
- **Tools exposed**: `list_components`, `get_component_detail`, `get_component_parameters`, `get_component_examples`, `search_components`, `get_api_reference`, `get_enum_values` + 5 more
- **Install**: Clone `https://github.com/mcbodge/MudMCP`, run with dotnet
- **Caveat**: Community project, not official MudBlazor. First run clones MudBlazor repo (slow); subsequent runs use cache
- **Config** (`.mcp.json`):
```json
"mudblazor": {
  "command": "dotnet",
  "args": ["run", "--project", "<path>/MudMCP/src/MudBlazor.Mcp/MudBlazor.Mcp.csproj",
           "--", "--stdio", "--version", "9.0.0"]
}
```

#### 2. mcpmarket MudBlazor skill (to evaluate)
- URL: `https://mcpmarket.com/tools/skills/frontend-development-mudblazor-ui`
- Provides: standardised MudBlazor component/page generation patterns, semantic color mapping, loading/empty/error state patterns
- Install: sync from mcpmarket to Claude Code
- **Action**: Helder to visit URL and sync skill; evaluate vs existing dotnet-skills for overlap

### What NOT to install now (and why)

| Tool | Verdict | Reason |
|------|---------|--------|
| **Google Stitch MCP** | Skip until design phase | Stitch generates pure CSS/HTML/Tailwind — **not MudBlazor**. Gemini confirmed: raw CSS output creates instant technical debt when used with MudBlazor. Its value is design tokens (palette, typography) → MudTheme mapping. This requires (a) Google Cloud project setup, (b) design work in Stitch web UI first, (c) then MCP. Correct sequencing: design → Stitch web → export DESIGN.md → manual MudTheme mapping. No rush until we're building the real UI. |
| **Figma MCP** | Skip for now | $15/month minimum (6 calls/month on free = useless). No MudBlazor Code Connect support exists. Revisit post-MVP when web design begins. |
| Uno Platform | Skip | XAML-based; no web sharing advantage over Blazor |
| Syncfusion | Skip | XAML-only; same migration cost, no web sharing |
| UraniumUI | Skip | Best XAML option but no web sharing |
| DevExpress Blazor | Skip | Use MudBlazor; more AI training data, more community |

### Stitch Workflow (when we get there, post-spike)
Correct sequence when Stitch becomes relevant:
1. Create Google Cloud project + enable Stitch API + `gcloud auth login`
2. Open Stitch web UI → generate Karaoke Neon theme visually
3. Stitch exports `DESIGN.md` (color tokens, typography, spacing)
4. Claude Code reads `DESIGN.md` → maps hex values to `MudTheme` C# properties
5. **MudMCP** handles all actual component generation from that point on
6. Stitch's CSS output is **discarded** — only design tokens are used

---

## Timing Decision: Spike in Parallel (Helder's proposal — adopted)

**The question**: Theme refresh now vs post-MVP?

**Answer: Neither full option. Run a Blazor Hybrid spike in a parallel project.**

Helder's proposal is architecturally sound:
- Create `MyVocaList.Blazor.Spike` (new MAUI project, same solution)
- Create `MyVocaList.UI.Shared` (new RCL, same solution)
- Keep existing `MyVocaList` (DevExpress) untouched — Queue Management continues there
- Build 2–3 representative pages in Blazor Hybrid + MudBlazor
- Evidence-based decision: if spike succeeds → migration plan; if it fails → document blockers

**Why the spike approach beats both alternatives:**
- Queue Management (core MVP feature) builds in DevExpress without interruption
- Spike isolates risk — nothing touches production code
- By MVP launch, we have real Blazor Hybrid evidence, not just Gemini recommendations
- If migration is decided post-MVP, the spike code becomes Phase 1 of the RCL

**What the spike validates:**
1. `BlazorWebView` renders MudBlazor on Android/iOS without jank
2. Existing `IVenueService` / `IPersonService` DI injection works inside Razor components
3. Navigation patterns (shell equivalent in Blazor router)
4. MudBlazor's MudTheme accepts the Karaoke Neon palette without workarounds
5. CSS animations (transform + opacity) are smooth at 60fps on a real device

**Spike scope (per workflow.md spike task pattern):**
- Time-box: 2–3 days
- Pages: VenuesPage (list + search), PersonsPage (list)
- Produces: `findings.md` artifact — go/no-go evidence for migration spec
- NO production code touched

---

## Steps for This Session

### Step 1: Register prompt
Create `UI-2nd-refactor/prompt.md` with Helder's original prompt for decision continuity.

### Step 2: Install MudMCP
Clone `https://github.com/mcbodge/MudMCP` to a local tools folder.
Add entry to `.mcp.json` (project-level).
Verify: `claude mcp list` shows `mudblazor`.

### Step 3: Check mcpmarket MudBlazor skill
Helder visits `https://mcpmarket.com/tools/skills/frontend-development-mudblazor-ui` and syncs to Claude Code.

### Step 4: Update BACKLOG.md
- `ui-2nd-refactor` → `📋 Spec` (direction decided, spike + spec pending)
- Add spike entry in Dev Cycle Craft table

### Step 5: Note for CLAUDE.md
Add a single line under Stack: "Planned post-MVP: Blazor Hybrid + MudBlazor migration. See Docs/Management/BusinessFeatures/UI-2nd-refactor/."

---

## Files to Create This Session

1. `UI-2nd-refactor/prompt.md` — original prompt captured
2. `.mcp.json` — MudMCP entry added
3. `BACKLOG.md` — status updates
4. Optional: `CLAUDE.md` — 1-line note about Blazor Hybrid direction
