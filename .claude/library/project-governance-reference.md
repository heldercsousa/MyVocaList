# Project Governance — Reference

> Extracted verbatim from `CLAUDE.md` (2026-07-07, rules-file-refactoring Task 16 / audit R5). CLAUDE.md keeps the constitutional constraints, authority hierarchies, and routing inline; this file holds the rationale/procedure narrative. Consult it when questioning why SDD applies, running the Continuous Enhancement loop or a Constitutional Audit, or re-evaluating tooling.

---

## SDD Applicability for MyVocaList

MyVocaList is past the 10–20 interdependent file threshold where SDD becomes strictly beneficial:
- Multiple layers (Domain, Infra, Services, MAUI) interact on every feature
- Features span multiple sessions and require context persistence across resets
- Queue management logic has business rule complexity where hallucination cost is high

This means:
- Spec-first is not optional overhead — it is the mechanism that prevents compounding technical debt
- Vibe coding on new features increases total delivery time beyond 3 months due to rework
- The ROI on specs for MyVocaList is currently positive; skipping specs costs more than writing them

Exception: Bug fixes, cosmetic changes, and one-off scripts remain spec-exempt (see `workflow.md` bypass rule).

## Continuous Enhancement — full procedure

CLAUDE.md, rules, hooks and commands are a living system — not a fixed set.
After every task, always ask:
> "What was learned that should improve CLAUDE.md, any rules file, or any command file?"

- **Add** new files for any area not yet covered
- **Update** existing files with confirmed patterns
- **Replace** outdated patterns with working ones
- **Delete** guidelines that proved wrong or are superseded by skills
- **Update** CLAUDE.md when architecture, stack, or fundamental decisions change only when no specialized and dedicated file is in place in solution's .claude folder. Otherwise such specialized file must be the one to be updated.

Any area where Claude Code repeatedly makes mistakes or needs repeated guidance is a candidate for a new rule, command, or CLAUDE.md update.

> **Note:** Changes to CLAUDE.md or `.claude/rules/*.md` must follow the Amending These Rules process (`CLAUDE.md § Amending These Rules`) — including `amend:` commit prefix and changelog entry.

**Quarterly Constitutional Audit:** At significant project milestones (phase completion, feature launch), review `CLAUDE.md` and all `.claude/rules/` files for:
- Rules with no rationale — add rationale or remove the rule
- Redundant rules — remove if the type system or DI container now enforces them
- Contradictions — two rules that conflict in an edge case
- Exception accumulation — rules with 2+ `unless X` qualifiers (the rule may be wrong)
- Rules where violation rate is rising (a sign the rule is fighting reality)

**Context size governance:** CLAUDE.md must stay under 600 lines (target <200). When it approaches a limit:
- Move stable, detailed patterns to `.claude/library/` or `.claude/rules/` files
- Replace inline examples with "See `.claude/rules/X.md`" references
- Keep only routing tables, non-negotiables, and architectural constraints inline
Do not add rules that a linter or type-checker already enforces.

## Methodology Layering — rationale

**(1) DDD** defines what to build — bounded contexts, aggregate boundaries, ubiquitous language. Applied conceptually at spec time (the `ddd-dotnet` plugin is disabled — tactical DDD patterns like rich aggregates conflict with the unamendable "business logic in Services" constraint).
**(2) SDD** defines how it works — spec (`requirements.md` + `design.md` + `tasks.md`) within the DDD boundaries.
**(3) TDD** verifies it is correct — Red/Green/Refactor within each SDD task.
These layers are sequential, not interchangeable. Do not apply TDD before the SDD spec exists; do not write an SDD spec without first confirming DDD boundaries.

## Scope of inspection for complex tasks

Before proposing anything that touches UI, styles, or components, inspect ALL of: every page, every custom component, every relevant rules file, AND verify what the platform/libraries already provide. Never limit the audit to the files initially mentioned. Cross-file pattern counts (how many times the same inline style appears) must be established before proposing centralization.

## Docs/ layout — examples and detail

**Example resolutions:**
- New business feature `Queue Management` → `Docs/Management/BusinessFeatures/queue-management/`
- New dev-cycle activity → `Docs/Management/DevCycleCraft/[name]/`
- Sub-feature nested under Artists & Songs → `Docs/Management/BusinessFeatures/artists-songs/[sub-feature]/`

**Excluded from glob scans (access by explicit path only; `.claudeignore`-enforced):**
- `Docs/Management/DevCycleCraft/sdd/**` — SDD theory, 96 files (spec-, plan-, impl- prefixes), reference material only
- `Docs/Changelog/**` — historical changelog
- `Docs/Plans/**` — legacy plans folder

## Tool Selection

**Primary AI assistant:** Claude Code (Anthropic CLI)
**Decision rationale:** Spec-first discipline (CLAUDE.md + rules files), subagent delegation support, 1M-token context window, terminal-native workflow, MCP client built-in.
**Lock-in accepted:** Spec format and rules files are Claude Code-specific; migrating to Cursor or Copilot would require translating CLAUDE.md to `.cursorrules` or `copilot-instructions.md`.
**Re-evaluation trigger:** If Anthropic discontinues Claude Code, pricing exceeds $200/month, or a competing tool delivers >2x productivity improvement on SDD tasks.

**Tooling Evaluation & Migration:** for guidance on evaluating Tessl Registry, sdd-mcp, Spec Kit migration, or Cursor integration, read `Docs/Design/tooling-evaluations.md` by explicit path (the folder is glob-ignored).

---

> **Authorship note:** This file must be human-reviewed before it is relied upon (CLAUDE.md § Continuous Enhancement — Authorship).
