# S6.1 — Constitutional Constraints

**Status:** Researched
**Predecessor(s) ID:** S6

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-04-30 | Researched | Content written by research agent |

---

## Overview

Constitutional constraints are non-negotiable rules that agents cannot override — immutable principles enforced at the harness level rather than through prompt guidance alone. In traditional software governance, rules live in documentation, wiki pages, or architectural guidelines; agents read them, remember them during the current context window, and forget them when context fills. Constitutional constraints fix this by moving enforcement from the prompt layer into the system layer — the harness itself prevents violations before they occur.

The power of constitutional constraints is structural: a shell script that exits with code 2 blocks a tool call in a way no prompt instruction can. The model cannot argue with an exit code, cannot forget a pre-tool check, and cannot reason around a hard denial. This converts governance from a matter of prompt quality and context maintenance into a matter of machine-enforced, always-on structural design.

---

## Definition and Scope

A constitutional constraint is any rule that satisfies **both** conditions:

1. **Non-negotiable:** The rule applies regardless of context, time pressure, feature urgency, or deadline. There are no exceptions approved at runtime.
2. **Harness-enforced:** The rule is checked by the system (hooks, CI gates, linters) before the violation can manifest in the codebase.

Rules that are merely documented, even in `CLAUDE.md`, without mechanical enforcement are not constitutional constraints — they are guidelines. Guidelines fail when context fills or pressure mounts. Constitutional constraints persist.

### Scope

Constitutional constraints apply to:

- **All agents:** Every AI agent in the project reads and is bound by the same constitutional document.
- **All sessions:** Every session loads the constitution at startup before any other context.
- **All phases:** The constitution governs discovery, planning, implementation, review, and deployment equally.
- **Parallel execution:** When multiple agents work simultaneously, they cannot violate the constitution through coordination or aggregation of decisions.

The constitution does not govern preferences, style choices, or performance optimizations. It governs the boundaries that, if crossed, compromise the integrity of the codebase.

---

## Anatomy of a Constitutional Constraint

A well-formed constitutional constraint has four elements, as described in the Agent Factory research (Panaversity, 2026) and demonstrated in production SDD implementations:

### 1. Principle Statement

The rule expressed as a positive statement (what must be done) rather than a prohibition (what cannot be done). Positive statements are easier to enforce mechanically and harder to rationalize around.

**Example (good):**
> "All user input must be validated at the external boundary before being passed to internal services."

**Example (weak):**
> "Don't allow invalid input."

### 2. Enforcement Mechanism

The specific tool, hook, linter rule, or CI gate that makes the constraint mechanical. Without an enforcement mechanism, the constraint is a guideline.

- **Hook enforcement:** A `PreToolUse` hook that intercepts `Write` and `Edit` calls and validates against a schema.
- **Linter enforcement:** A static analysis rule that detects the violation (e.g., `gitleaks` for hardcoded secrets).
- **CI gate enforcement:** A pipeline check that blocks merge if the constraint is violated (e.g., "All commits to main must have a test file referenced").
- **Type system enforcement:** Language-level constraints (e.g., marking a function `sealed` in C# prevents override).

### 3. Rationale

Why the constraint exists. The rationale is not enforcement but it drives the design of the enforcement mechanism — a constraint built for the wrong reason is brittle.

**Example:**
> "User input validation must happen at the boundary because internal services assume their inputs are clean. Validating late (inside a service) means every service must replicate validation logic or risk memory corruption if a novel input path is discovered."

### 4. Amendment Scope

Which entity can modify the constraint and under what conditions. Constraints that never change become obstacles. Constraints that change without a process become undisciplined. The amendment scope defines the difference.

**Example:**
> "This constraint may be amended by the Technical Lead only. Amendment requires (1) documentation of the new risk that makes the old constraint wrong, (2) review by two Architects, and (3) a six-month pilot on new code before retroactive application."

---

## Five Content Domains

Constitutional constraints typically fall into five domains, each with different enforcement mechanisms:

### 1. Architecture Principles

How components must relate; which boundaries are inviolable.

**Examples:**
- "Services communicate only through interfaces defined in the Domain project; never by importing from Infra internals."
- "UI components depend on Services, never on Infra."
- "Repositories are always injected through interfaces; never instantiated directly."

**Enforcement:**
- Namespace visibility rules (internal classes in Infra cannot be imported by UI)
- Compilation errors (type system prevents violations)
- CI gates that detect cross-layer imports via static analysis

**In MyVocaList:**
From `code-principles.md`: "Business logic lives in Services only — never in ViewModels or pages. Repository interfaces in Domain — implementations in Infra. Only the MAUI project references Infra (for DI wiring, AppDbContext, migrations)."

### 2. Technology Constraints

Locked choices that agents must respect and cannot override.

**Examples:**
- "Target platform is .NET MAUI 10 for iOS and Android; no alternative platforms are permitted."
- "Database is SQLite; no migrations to PostgreSQL without architecture review."
- "Authentication uses the configured Identity Provider; do not add local password databases."
- "All database queries use Entity Framework Core; raw SQL is permitted only for stored procedures."

**Enforcement:**
- Project file constraints (`.csproj` targets limit platform choices)
- NuGet version pinning (lock file prevents unauthorized upgrades)
- Pre-commit hooks that reject migrations targeting the wrong database
- CI gates that detect direct `SqlCommand` usage outside approved patterns

**In MyVocaList:**
From `CLAUDE.md`: "Stack: .NET MAUI 10 · net10.0-android · net10.0-ios · C# 13 · CommunityToolkit.Mvvm · Serilog · EF Core 10 · SQLite."

### 3. Code Quality Standards

Measurable properties that every file must satisfy.

**Examples:**
- "Async methods always use `CancellationToken` on Service signatures; blocking with `.Result` or `.Wait()` is prohibited."
- "Function length shall not exceed 50 lines (excluding tests and auto-generated code)."
- "Every public method must have XML documentation comments on the interface."
- "Test classes follow the naming pattern `{Subject}Tests` with test methods named `{Method}_{Context}_{Expected}`."

**Enforcement:**
- Linters (StyleCop, Roslyn analyzers configured to fail the build)
- Pre-commit hooks that run static analysis
- Code review gates that measure and reject violations
- Compiler warnings configured as errors for specific rules

**In MyVocaList:**
From `code-principles.md`: "Use `ArgumentNullException.ThrowIfNull`, `ArgumentOutOfRangeException.ThrowIfNegativeOrZero`. Use collection expressions `[item1, item2]` over `new List<T> { ... }`. Services return tuples for operations that can fail."

### 4. Security Requirements

Non-negotiables that must apply even when a feature spec omits them.

**Examples:**
- "No secrets (API keys, passwords, connection strings) may be checked into version control; all secrets must be injected at runtime via environment variables or secure vaults."
- "All user input must be validated against a schema before being accepted; accept-anything-and-validate-later is prohibited."
- "SQL queries must use parameterized queries exclusively; string concatenation is prohibited."
- "All external API calls must validate the response status before using the response body."

**Enforcement:**
- Pre-commit hooks running `gitleaks` or `trufflehog` to detect secrets
- SAST tools that block merge if SQL injection patterns are detected
- Input validation linters that fail on unvalidated input
- Runtime checks that throw before executing unvalidated queries

**In MyVocaList:**
From `CLAUDE.md` (Non-Negotiables): "Never use `DisplayAlert`, `DisplayActionSheet`, `DisplayPromptAsync`" — security-related: these are uncontrolled dialogs that allow phishing and spoofing attacks.

### 5. Workflow Rules

Procedural behavior — how agents must act when facing ambiguity, how to sequence work, when to ask clarifying questions.

**Examples:**
- "All commits to main must reference an approved spec or issue; orphan commits are prohibited."
- "Before implementing a feature, the agent must read the spec; implementation without spec knowledge is prohibited."
- "Merge conflicts are resolved by the feature author, not the reviewer; automated resolution tools are prohibited."
- "When spec ambiguity is discovered, the agent must ask clarifying questions rather than assume intent."

**Enforcement:**
- Pre-commit hooks that validate commit message format and required trailers (e.g., `Refs: SPEC-001`)
- `Stop` hooks that block agent exit until spec is read and tasks.md is consulted
- CI gates that require commit message validation
- Linters that detect assume-y patterns (e.g., `TODO: "I think X means Y"` in a comment)

**In MyVocaList:**
From `workflow.md`: "Rule 1 — Spec-First: Before writing any implementation code for a feature, read `Docs/specs/[feature]/design.md`." Enforced by: Agent guidance + human review.

---

## Constitutional Hierarchy and Inheritance

In multi-layered projects with organizational and project constitutions, a hierarchy prevents conflicts:

| Layer | Location | Override rules | Scope |
|-------|----------|-----------------|-------|
| **Enterprise / Managed** | `/etc/claude-code/CLAUDE.md` (system-level) | Cannot be overridden by any lower layer | All projects, all agents |
| **Global User** | `~/.claude/CLAUDE.md` | Can be overridden by project layer only | All projects for this user |
| **Project** | `./CLAUDE.md` or `./.claude/CLAUDE.md` (committed) | Overrides global; cannot override enterprise | This project, all agents |
| **Project Modular Rules** | `./.claude/rules/*.md` | Extend project constitution; cannot weaken | This project, path-scoped |
| **Local Override** | `./CLAUDE.local.md` (gitignored) | Personal overrides for testing only; never deployed | This user's session only |

**Conflict resolution:** Lower layers can only strengthen or extend upper-layer rules, never weaken them. A project can require stricter test coverage than the organization baseline; it cannot reduce test coverage below the organization minimum.

---

## Constitutional Constraints in MyVocaList

The MyVocaList project implements constitutional constraints via two documents:

### `CLAUDE.md` (Project root)

Contains the core project constitution. Sections that are constitutional (enforced mechanically or through workflow gates):

- **Non-Negotiables:** "Never use `DisplayAlert`" — enforced by code review + linter warnings.
- **Architecture Constraints:** "Services depend only on Domain interfaces — never on Infra types directly." — enforced by namespace visibility.
- **DI Registration Conventions:** "Repositories are Scoped; Pages/ViewModels are Transient." — enforced by project setup + code review.
- **UI Thread Performance Rules:** "Never call `ReplaceRange` more than once per `RunOnUiThread` block." — enforced through monitoring + code review (no linter available yet).

### `.claude/rules/` (Modular, path-scoped)

Extracted domain-specific rules:

- `code-principles.md` — C# style, naming, async patterns. Applies project-wide.
- `testing.md` — Test structure, TDD workflow, naming conventions. Applies to test files.
- `mediatr-patterns.md` — Placeholder for when MediatR is introduced. Will constrain command/query naming and registration.

---

## Known Gaps and Limitations

### 1. Subjective Constraints Cannot Be Automated

Constraints like "Use clear, idiomatic C#" or "Keep components focused" are real but resist automation. They are enforced through code review, not hooks. Over-automating subjective constraints produces false positives (linter noise) and false negatives (subtle violations the linter misses). The correct balance is: automate binary, non-negotiable rules; use code review for judgment calls.

### 2. Intent Gaps in Enforcement

A constraint like "All endpoints validate input against schema" can be checked with a SAST tool (does the code call a validator?) but cannot verify that the validator is correct or complete. The tool detects the call; only a human can audit the logic. Enforcement mechanisms catch structural violations; they do not validate correctness.

### 3. Constitutional Drift Over Time

Constraints written at project inception may become wrong. A database platform constraint that locked in SQLite at the start may prove inadequate for a workload that emerges mid-project. Old constraints that are no longer correct but still enforced force agents to either violate them (build fails) or obey them (architecture stays wrong). This is solved through an amendment process (see S6.1.2 — Amendment Governance in the predecessor S6 document), but the amendment process itself must be constitutional.

### 4. Coverage Gaps in Hook Enforcement

Hook enforcement in Claude Code is strong but not complete. Known gaps (per AgentPatterns.ai, 2025):

- Exit code 2 has been documented to fail for `Write` and `Edit` in some harness versions while still working for `Bash`
- Silent hook failures occur if a hook has a missing dependency (hook error is logged, but enforcement does not happen)
- Exit code 2 has caused idle halts rather than feedback loops in some harness configurations

The operational implication: hooks are a best-effort enforcement layer. Pair hooks with CI gates for binary rules that must never be violated.

---

## Constitutional Constraints vs. Guidelines

The distinction is critical:

| Property | Constitutional Constraint | Guideline |
|----------|---------------------------|-----------|
| **Enforcement** | Mechanical (hook, CI gate, linter, type system) | Prompt / documentation |
| **Failure mode** | Violating the constraint prevents the tool use | Violating the guideline produces a build or review failure later |
| **Scope** | Applies across all contexts and sessions | Easily forgotten or deprioritized under time pressure |
| **Consistency** | Guaranteed consistent enforcement | Variable enforcement across agents and sessions |
| **Amendment** | Formal process (documented, reviewed, versioned) | Informal (wiki updates, comments) |

A rule that exists only in `CLAUDE.md` without mechanical enforcement is a guideline, no matter how many times it is stated. Converting a guideline to a constitutional constraint requires identifying an enforcement mechanism — or accepting that the rule cannot be made constitutional.

---

## Implementation Patterns

### Pattern 1: Type System Enforcement

Use the language's type system to make violations impossible:

```csharp
// Constitutional: Repositories are injected, never instantiated
public interface IVenueRepository { ... }

// Compile error: constructor injection enforces this
public VenueService(IVenueRepository repo) { ... }

// Violation attempt — compile error
var repo = new VenueRepository(_db);  // ← IVenueRepository is internal; this cannot compile
```

### Pattern 2: Pre-Commit Hook Enforcement

Block violations before they reach the repository:

```bash
# In .claude/hooks/pre-write.sh
if [[ "$FILE_PATH" =~ \.env$ ]]; then
    # Detect if secrets are being written
    if grep -E "(password|API_KEY|secret)" "$FILE_CONTENT"; then
        echo "Blocked: Secrets detected in .env file"
        exit 2  # Block the write
    fi
fi
```

### Pattern 3: CI Gate Enforcement

Run automated checks on every push:

```yaml
# In .github/workflows/constitution-check.yml
- name: Validate No Hardcoded Secrets
  run: gitleaks detect --verbose

- name: Validate Architecture Layers
  run: |
    # Fail if UI imports from Infra
    grep -r "using MyVocaList.Infra" src/MyVocaList/
    if [ $? -eq 0 ]; then exit 1; fi
```

### Pattern 4: Code Review Gate

Subjective constraints are enforced through explicit review:

```markdown
# Code Review Checklist (from CLAUDE.md)
- [ ] Async methods have CancellationToken parameter
- [ ] No hardcoded configuration (use dependency injection)
- [ ] Test name follows {Method}_{Context}_{Expected} pattern
```

---

## Amendment Governance (Overview)

Constitutional constraints require an amendment process as formal as the constraints themselves. This is covered in depth in S6.1.2 (not yet written) but the overview:

1. **Proposal:** The constraint is identified as wrong or obsolete. A proposal documents the rationale for change.
2. **Review:** The Technical Lead or Architect reviews the proposal against the project's architectural principles.
3. **Pilot:** On projects with existing code, the new constraint is applied to new code only (pilot period).
4. **Retroactive application:** Once proven in pilot, the constraint is applied to existing code incrementally.
5. **Documentation:** The amendment is documented in `CHANGELOG.md` with rationale and effective date.

Without this process, constraints either accumulate (stale rules never removed) or drift (rules changed informally without coordination).

---

## Sources

- [The Project Constitution — Agent Factory / Panaversity (2026)](https://agentfactory.panaversity.org/docs/General-Agents-Foundations/spec-driven-development/the-project-constitution)
- [Beyond Vibe Coding: Spec Kit and the Constitution for Consistent, GDS-Compliant AI Development — Mark Craddock (Medium, 2025)](https://medium.com/@mcraddock/beyond-vibe-coding-spec-kit-and-the-constitution-for-consistent-gds-compliant-ai-development-e4b2693a241f)
- [Constitutional Constraints for AI Code Generation — AgentPatterns.ai (2025)](http://agentpatterns.ai/security/security-constitution-ai-code-gen/)
- [AI Governance Framework for Humanitarian Use — AOS Foundation (2026)](https://aos-constitution.com/)
- [Policy as Prompt: Dynamic Runtime Guardrails for AI Agents — arXiv:2509.23994](https://arxiv.org/pdf/2509.23994)
- [Constitutional Self-Governance for Autonomous AI Agents — CTE Research (2026)](https://www.cteinvest.com/research/constitutional-self-governance.html)
- [Exploring Laws of Robotics: Constitutional AI and Constitutional Economics — Springer Nature (2025)](https://link.springer.com/article/10.1007/s44206-025-00204-8)
- [Claude Code Configuration Guide: Memory Hierarchy and Levels — GitHub](https://github.com/war851/AI-Governance-Architecture/blob/main/docs/claude-code-configuration-guide.md)
- [CLAUDE.md Design Principles: Build Your Project Constitution — ClaudeWorld (2026)](https://claude-world.com/articles/claude-md-design/)
- [cc-sdd: Spec-Driven Development for Multiple AI Agents — GitHub](https://github.com/gotalab/claude-code-spec/)
- [Guardrails for SDD Plugin — Mintlify](https://noelserdna-claude-plugin-sdd.mintlify.app/automation/guardrails)
- [Harness AI December 2025 Updates: AI Governance That Scales — Harness (2026)](https://www.harness.io/blog/harness-ai-december-2025-updates)
