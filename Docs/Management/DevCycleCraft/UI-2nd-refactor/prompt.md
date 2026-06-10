# Session Prompt — UI Architecture Decision
**Date:** 2026-06-02  
**Author:** Helder Carvalho de Sousa

---

## Original Prompt

> Read the @Docs/Management/BusinessFeatures/ui-2nd-refactor\ documents that is a chat I had to gemini 3 pro about this app UI current solution. This chat is about the task nested in @Docs/Management/BACKLOG.md visual theme task, where this and another task are nested, being the another a simple theme refactory, and the otherone a complete refactory to another approach. First, read the folder files (if not able to read .docx format, I can transform it into a readble one. Install any MCP server and/or Skill that are relyable by comminity and is relatted to usage of Blazor Hibrid in MAIU 10 apps and at the same time in web apps, both mobile and desktop, covering the struggle for having resposivity and a unique UI shared by all ecosystem we can ever plan to create later. After understanding, use the set of current installed MCP server(s) and/or Skill(s) to evaluate the information these files contain, and evaluate any other set of options by seeking in the web. Also, seek for the usage of google's stitch to at least plan the pages + using its MCP server tool to integrato to claude code, and/or FIGMA usage, both ones with the pros and cons, and also considering that we'll going to use blazor hibrid or another option you found while seeking for options. Let's plan with caution. Register this propmpt in a file nested to its folder for keeping our decision flow accurate.

---

## Follow-up Q&A (2026-06-02)

**Q1: Figma account?**  
A: No account. Can get one but cost is a concern. Needs evaluation.

**Q2: MudBlazor vs Fluent UI preference?**  
A: MudBlazor is the one recommended by Gemini and is the choice. But wants MCP/Skills evaluated for MudBlazor before starting any spec or plan. Also look for existing skills or MCP servers that are a good idea to install.

**Q3: Theme refresh timing — now vs post-MVP?**  
A: In doubt. Proposes a **parallel spike approach**: create a new MAUI project + RCL within the same solution targeting MudBlazor, completely isolated from the existing DevExpress MAUI project. This allows testing the approach while Queue Management proceeds in the DevExpress project without risk.

**Q4: Google Cloud for Stitch MCP?**  
A: No Google Cloud project. Helder noted (from Gemini research): Stitch must be designed first in its web interface before the MCP is useful. Gemini also warned that Stitch generates pure CSS (not MudBlazor), making the MCP less directly useful when using MudBlazor as the component library. Figma has similar limitations regarding MudBlazor code generation.

---

## Decisions Made This Session

1. **Architecture**: Blazor Hybrid + MudBlazor (post-MVP)
2. **Component library**: MudBlazor (confirmed)
3. **MCP installed**: MudMCP (`mcbodge/MudMCP`) — cloned to `C:/Users/helde/.claude/tools/MudMCP`
4. **Stitch MCP**: Deferred — needs Google Cloud setup + design phase first; mismatch with MudBlazor confirmed
5. **Figma MCP**: Deferred — $15/month minimum, no MudBlazor Code Connect support
6. **Execution approach**: Parallel spike within same solution before deciding on full migration

---

## Research Files in This Folder

- `Design App Karaoke - MD3 vs Alternativas research - chat 1.md` — Gemini chat: 3 architectural routes evaluated; Blazor Hybrid conclusion
- `Design App Karaoke - MD3 vs Alternativas research - chat 2.md` — Gemini chat: deep architecture; monorepo design; MudBlazor + Stitch workflow
- `Design App Karaoke -MD3 vs Alternativas research.md` — Formal Gemini research report: color tokens, UraniumUI/Syncfusion/Grial comparison, concurrency, Stitch MCP integration
