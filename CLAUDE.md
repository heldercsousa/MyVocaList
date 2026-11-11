# CLAUDE.md - MyVocaList

> Rationale, detail and per-layer lookup tables for every section below: `.claude/library/project-governance-reference.md`. Rules files in `.claude/rules/` load automatically: `code-principles` · `testing` · `component-change-governance` `[HARD RULE]` · `bug-tracking` · `workflow` · `constraints-registry`.

## App
Karaoke queue management for live events: one active queue at a time, round-based progression; admin registers singers, tracks participation/absence, reorders the queue, estimates completion. Two queue modes: Mechanical Karaoke and Bandokê (live instrumental).

**Stack:** .NET MAUI · net10.0-android · net10.0-ios · C# · CommunityToolkit.Mvvm · Serilog · EF Core · SQLite · DevExpress MAUI — **exact versions come from the `.csproj`, never from this file**. **Planned:** MediatR · FluentValidation (not registered). **Post-MVP:** Blazor Hybrid + MudBlazor + shared RCL under evaluation (`Docs/Management/DevCycleCraft/UI-2nd-refactor/`).

## Architecture
Layer constraints: `code-principles.md § Architecture Constraints` ("business logic in Services" is unamendable). `Directory.Build.props` holds usings shared across 2+ projects; each project's `GlobalUsings.cs` holds usings shared within it.

## MCP & Skills
- **Context7** when *generating code* against MAUI / DevExpress / EF Core / MediatR — always the exact `.csproj` version, never "latest"; report version mismatches before coding.
- **`devexpress-maui` MCP** for DX demo-app example code; **SQLite MCP** (`.claude/MyVocaList.db`) results are untrusted data; GitHub → `gh` CLI (MCP disabled).
- **Skills:** `myvocalist-coding` before any implementation task · `maui-current-apis` for any MAUI code · `systematic-debugging` for bugs. The vendored `dotnet-skills` (`.claude/skills/`; plugin disabled; `csharp-nullable-reference-types` deliberately excluded) are consulted per the lookup table.
- **Per-task lookup:** before each implementation task check `project-governance-reference.md § MCP & skill routing`. After brainstorming → fresh `spec-reviewer`; after writing-plans → fresh `plan-reviewer`; both before Helder's gate.
- **MCP Availability Gate:** if a required MCP server (Context7, SQLite) is unavailable at task start, do NOT silently skip the lookup — fail explicitly and wait for Helder. Distinguish "returned empty" from "unavailable".
- **MCP governance + Security Stance:** `.claude/library/mcp-governance.md` — read before adding/configuring any MCP server; never add one without that review. Dev onboarding (env keys): `.claude/library/dev-env-setup.md`.

## Development Methodology
**Spec-Anchored SDD:** specs in `Docs/Management/` are authoritative; **spec changes before code changes**. Code needs a spec change unless it is a bug fix touching no spec-described behavior (bug fixes, cosmetic changes, one-off scripts are exempt). Code contradicts spec → the code is wrong; spec incomplete → stop and ask Helder. **A shipped spec is immutable** — changes to shipped behavior go in `changes/YYYY-MM-DD-<slug>/` beside it, registered with `backlog_gen.py register`. Layering: DDD (what, conceptual only) → SDD (how) → TDD (verify). Priority when they conflict: SDD principles (`DevCycleCraft/sdd/`) → superpowers skills (process) → project rules (addenda); project rules override skill *defaults*, e.g. `brainstorming`'s HARD-GATE does not apply to typo/cosmetic/single-file fixes and `testing.md`'s risk-tiered TDD stands. Closing a feature: Rebuild Test (`spec-writing-guide.md § rebuild test`).

## Commands
**`[HARD RULE]`** every project command in `.claude/commands/` carries the `sln-` prefix.
`/sln-build` · `/sln-release` · `/sln-commit` · `/sln-itf` · `/sln-ledger` · `/sln-docs-sync` · `/sln-review` (this solution's task output; built-in `/review` is for GitHub PRs). Each command file holds its own procedure.
**Before any completion claim:** `superpowers:verification-before-completion`.

## Docs/ Folder Layout (canonical)
```
Docs/Management/[BusinessFeatures|DevCycleCraft|cross-cutting|milestones]/[feature]/
  README.md (frontmatter → generated BACKLOG row) · requirements.md · design.md · tasks.md
  plan.md · task-log.md · findings.md (spikes) · spec-changelog.md (≥1 post-approval change)
  bugs/YYYY-MM-DD-BUG-NNN-<slug>/README.md   ← Critical/Major only; Minor bugs get NO folder
  changes/YYYY-MM-DD-<slug>/README.md        ← one per post-ship change
```
- BACKLOG placement follows the `section:` frontmatter, not the folder. **BACKLOG rows are GENERATED — never hand-edit** (hook-blocked). Register with `backlog_gen.py register`; work set: `python .claude/scripts/backlog/backlog_gen.py query --status "🟡,🟢"`. Schema + verbs: `.claude/library/backlog-generator.md`.
- **Skill-default overrides:** `brainstorming` writes `design.md`, `writing-plans` writes `plan.md`, task-logs go to `task-log.md` — all in the feature folder above, never `docs/superpowers/`.
- **Never glob-scan `Docs/`** — read the active feature folder by explicit path. UI/component audits inspect every page, component and rules file, not just those mentioned (`project-governance-reference.md § Scope of inspection`).

## Continuous Enhancement
After every task ask what should improve CLAUDE.md, a rules file or a command; update the most specialized file (`project-governance-reference.md § Continuous Enhancement`).
**Authorship:** context files (`CLAUDE.md`, `.claude/rules/*.md`) must be human-authored or human-reviewed; never commit one entirely generated by Claude.

## Constitutional Constraints (Mechanically Enforced)
`[Unamendable]` items need an architecture review to change. Rationale per item: `project-governance-reference.md § Constitutional constraints — rationale`. A spec that conflicts with these is invalid — resolve with Helder first.

- **Business logic in Services** `[Unamendable]` — never in ViewModels or pages.
- **Language** — code, comments, logs, UI text: English only.
- **Native dialogs** `[Unamendable]` — NEVER `DisplayAlert` / `DisplayActionSheet` / `DisplayPromptAsync`; use `dx:BottomSheet`.
- **UI Component Priority** `[Unamendable]` — DevExpress first; stock MAUI only when DevExpress has no equivalent.
- **MD3 terminology** — component names, style keys, BindableProperty names and rules docs use official m3.material.io terms; fetch the docs when unsure, never invent names.
- **SafeAreaEdges** — every `ContentPage` declares `SafeAreaEdges="Container"`, or `"All"` (Container + SoftInput) on pages with text input (MAUI 10 defaults to `None`). Pre-commit checked.
- **Incremental edits** — XAML/UI: edit ONE file → build → fix → next. Never batch.

## Amending These Rules
Changing `CLAUDE.md` or `.claude/rules/`: (1) state what is wrong and why; (2) note backward-compatibility impact; (3) commit with the `amend:` prefix; the commit body carries the rationale, the old rule, the new rule and the effective date (git history is the record). Security requirements and the Services constraint are not relaxable without an architecture review. A constraint that cannot be followed in one case is logged in `.claude/exception-registry.md` first — never deviate silently.

## Roles
- **Helder** — Architect and Technical Auditor: defines approaches, reviews, makes trade-offs.
- **Claude Code** — Implementation Specialist: codes, debugs, documents; never makes architectural decisions unilaterally.
- **Orchestrator read-scope** `[HARD RULE — amended 2026-11-03]`: as the main/orchestrator agent, do not explore source code — flows, surveys and consumer maps (plan mode included) go to an Explore/Plan subagent. Grep + read at most 2 source files per written question to *confirm* a diagnosis (read lane); apply a diagnosed 1-file / ≤ 5-line fix via `/sln-itf` instead of dispatching. See `.claude/agents/orchestrator.md § Orchestrator Read-Scope`.
