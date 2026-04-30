# CLAUDE.md - MyVocaList

## App
Karaoke queue management for live events. Manages one active queue at a time with round-based progression. Admin registers singers, tracks participation/absence, reorders queue, estimates completion time. Two queue modes: Mechanical Karaoke and Bandokê (live instrumental). Future: singer self-registration, song catalog, lyrics via API, social features.

**Stack:** .NET MAUI 10 · net10.0-android · net10.0-ios · C# 13 · CommunityToolkit.Mvvm · Serilog · EF Core 10 · SQLite · DevExpress MAUI v25.2.4
**Planned:** MediatR · FluentValidation (not yet registered in MauiProgram.cs)

## Architecture
```
MyVocaList.Domain        — Entities, repository interfaces, domain events. No dependencies.
MyVocaList.Contracts     — DTOs, Models. No dependencies.
MyVocaList.Infra         — EF Core + SQLite. Depends on Domain only. Replaceable.
MyVocaList.Services      — Business logic. Depends on Domain + Contracts.
MyVocaList (MAUI)        — UI + DI wiring + database bootstrap. Depends on Domain + Contracts + Services + Infra.
```
- Business logic **only** in Services
- Repository interfaces in **Domain** — implementations in **Infra**
- Only MAUI depends on Infra — for repository DI, AppDbContext, CollationInterceptor, migration execution
- Services depends only on Domain interfaces — never on Infra directly
- DTOs as records in Contracts
- Prefer composition over inheritance
- `Directory.Build.props` (solution root): usings shared across 2+ projects
- Each project's `GlobalUsings.cs`: usings shared across 2+ types within that project only

## MCP & Skills
- Context7: auto-triggered for all .NET MAUI, DevExpress, EF Core, MediatR documentation
- SQLite MCP (`sqlite`): db file at `.claude/MyVocaList.db` (pulled from emulator via `adb exec-out run-as com.myvocalist cat /data/data/com.myvocalist/files/MyVocaList.db`). Refresh before use if emulator has new data.
- Debugging: follow `systematic-debugging` skill (obra/superpowers)
- Architecture patterns: follow `ddd-dotnet` skill (nesbo)
- .NET patterns: follow `dotnet-skills` (Aaronontheweb)
- MAUI patterns: follow installed maui-skills, always filtered by `maui-current-apis`
- **MyVocaList coding rules** (UI, DevExpress, dialogs, EF Core, themes): invoke `myvocalist-coding` skill before any implementation task

## Rules Files
- MediatR patterns: `.claude/rules/mediatr-patterns.md`
- Code principles: `.claude/rules/code-principles.md`
- **Testing**: `.claude/rules/testing.md` — read before writing any test or setting up the test project. Covers test types, naming, TDD workflow, and prerequisites for Step 3.

## Commands
- Build: `/project:build`
- Commit: `/project:commit`
- Changelog: `/project:changelog`
- Review: `/project:review` — run after every completed task and after creating or updating any spec or plan file
- **Development workflow** (spec-first, subagent delegation, commit discipline): `.claude/rules/workflow.md`

## Coding Rules (on-demand via skill)
Invoke `myvocalist-coding` skill before any implementation task. It maps tasks to the relevant rule files in `.claude/library/`:
- UI / CRUD pages → `crud-pages.md`
- DevExpress components → `devexpress-patterns.md` (always first)
- Dialogs, BottomSheet, validation → `dialogs-validation.md`
- MD3 AppBars, Lists, FloatingToolbar → `m3-components.md`
- Colors, typography → `theme-locale.md`
- Touch targets, UX patterns → `ux-patterns.md`
- EF Core config, repository queries → `database-indexing.md`

## Skill & MCP Lookup (mandatory per task step)
Before starting each implementation task, scan available skills/MCPs for relevant guidance — this is not optional:
- Domain/Contracts/Infra: `dotnet-skills:efcore-patterns`, `dotnet-skills:modern-csharp-coding-standards`, `dotnet-skills:dotnet-project-structure`
- Tests: `superpowers:test-driven-development`, `dotnet-skills:testcontainers-integration-tests`
- Services with HTTP: `maui-rest-api`, context7 for library docs
- DI: `dotnet-skills:dependency-injection-patterns`
- MAUI UI: `maui-current-apis` (always), `maui-data-binding`, `maui-shell-navigation`, `maui-performance`
- DevExpress: `.claude/rules/devexpress-patterns.md` first (non-negotiable)

## Continuous Enhancement
CLAUDE.md, rules, and commands are a living system — not a fixed set.
After every task, always ask:
> "What was learned that should improve CLAUDE.md, any rules file, or any command file?"

- **Add** new files for any area not yet covered
- **Update** existing files with confirmed patterns
- **Replace** outdated patterns with working ones
- **Delete** rules that proved wrong or are superseded by skills
- **Update CLAUDE.md** when architecture, stack, or fundamental decisions change

Any area where Claude Code repeatedly makes mistakes or needs repeated guidance
is a candidate for a new rule, command, or CLAUDE.md update.

**Scope of inspection for complex tasks:** Before proposing anything that touches UI, styles, or components, inspect ALL of: every page, every custom component, every relevant rules file, AND verify what the platform/libraries already provide. Never limit the audit to the files initially mentioned. Cross-file pattern counts (how many times the same inline style appears) must be established before proposing centralization.

## Non-Negotiables
- **Language**: Code, comments, logs, UI text — English only. Translate any non-English text immediately.
- **Native dialogs**: NEVER use `DisplayAlert`, `DisplayActionSheet`, `DisplayPromptAsync`. Use `dx:BottomSheet` — see `myvocalist-coding` skill.
- **UI Component Priority**: DevExpress first, always. Use stock MAUI only when DevExpress has no equivalent — see `myvocalist-coding` skill.
- **MD3 terminology**: All component names, style keys, BindableProperty names, and rules file documentation must use official MD3 terminology (m3.material.io). Code must be directly cross-referenceable against MD3 docs without mental translation. When unsure, fetch the official docs — never invent names.
- **SafeAreaEdges**: .NET MAUI 10 breaking change — `ContentPage` defaults to `SafeAreaEdges="None"`. Add `SafeAreaEdges="Container"` to existing pages explicitly.
- **Incremental edits**: For XAML/UI work, edit ONE file → build → fix → then next file. Never batch UI edits.

## Roles
- **Helder**: Architect and Technical Auditor. Defines approaches, reviews code, makes trade-off decisions.
- **Claude Code**: Implementation Specialist. Codes, debugs, documents. Never makes architectural decisions unilaterally.