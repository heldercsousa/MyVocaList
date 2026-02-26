# CLAUDE.md - MyVocaList

## App
Karaoke queue management for live events. Manages one active queue at a time with round-based progression. Admin registers singers, tracks participation/absence, reorders queue, estimates completion time. Two queue modes: Mechanical Karaoke and Bandokê (live instrumental). Future: singer self-registration, song catalog, lyrics via API, social features.

**Stack:** .NET MAUI 10 · net10.0-android · net10.0-ios · C# 13 · MediatR · FluentValidation · Serilog · EF Core 10 · SQLite · DevExpress MAUI v24.2+

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
- Debugging: follow `systematic-debugging` skill (obra/superpowers)
- Architecture patterns: follow `ddd-dotnet` skill (nesbo)
- .NET patterns: follow `dotnet-skills` (Aaronontheweb)
- MAUI patterns: follow installed maui-skills, always filtered by `maui-current-apis`

## Rules Files
- DevExpress patterns: `.claude/rules/devexpress-patterns.md` — check this FIRST before any UI work
- MediatR patterns: `.claude/rules/mediatr-patterns.md`
- Theme & locale: `.claude/rules/theme-locale.md`
- Code principles: `.claude/rules/code-principles.md`
- Dialogs & validation: `.claude/rules/dialogs-validation.md`

## Commands
- Build: `/project:build`
- Commit: `/project:commit`
- Changelog: `/project:changelog`
- Review: `/project:review` — run after EVERY completed task before committing

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

## Non-Negotiables
- **Language**: Code, comments, logs, UI text — English only. Translate any non-English text immediately.
- **Native dialogs**: NEVER use `DisplayAlert`, `DisplayActionSheet`, `DisplayPromptAsync`. See `.claude/rules/dialogs-validation.md`.
- **UI Component Priority**: Always check `.claude/rules/devexpress-patterns.md` first. Use stock MAUI only when DevExpress has no equivalent.
- **SafeAreaEdges**: .NET MAUI 10 breaking change — `ContentPage` defaults to `SafeAreaEdges="None"`. Add `SafeAreaEdges="Container"` to existing pages explicitly.
- **Incremental edits**: For XAML/UI work, edit ONE file → build → fix → then next file. Never batch UI edits.
- **Build on every change**: Run `dotnet build` after every code change. Fix all errors autonomously. Never present incomplete work.

## Roles
- **Helder**: Architect and Technical Auditor. Defines approaches, reviews code, makes trade-off decisions.
- **Claude Code**: Implementation Specialist. Codes, debugs, documents. Never makes architectural decisions unilaterally.