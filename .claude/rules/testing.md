# Testing Rules — Routing Table

> **This file is a routing table.** Full detail (project structure, test-type patterns with code, naming, what-to-test, Tester/Builder split, TDD workflow, running tests, quality-audit checklist, anti-patterns) lives in `.claude/library/testing-reference.md`. Two periodic-only techniques have their own on-demand files: `library/mutation-testing-stryker.md`, `library/property-based-testing-fscheck.md`. All three load on demand via the `myvocalist-coding` skill map. The project-core never-miss rules below stay inline. TDD applies to all new/modified Services, ViewModels, Repositories.
> Skill reference (forward — enabled in rules-file-refactoring Task 11, do not enable here): the project's own `maui-unit-testing` skill covers generic xUnit/Moq/ViewModel setup. Project rules in THIS file and `testing-reference.md` govern where they differ.

## TDD within SDD (never-miss)

SDD says *what* must be true (`requirements.md`); the failing test is the machine-checkable encoding of it; code is the minimum to pass.
- Every acceptance criterion → at least one test. Do not write a test with no AC (add the AC to the spec first). Do not write code with no test.

## Acceptance Criteria Traceability (never-miss — project SDD core)

Every test covering user-facing behavior must trace to an AC in `requirements.md`.
- **Format:** first line of the test's doc/inline comment: `// [AC] REQ-VENUE-03: <criterion>`.
- **Rules:** every AC → ≥1 test; one test → one AC (split if more); AC IDs come from the spec (don't invent); infra tests (`TestDbContextFactory`, builders) are exempt.
- **Traceability matrix** (AC ID | Criterion | Implementation location | Test method) goes in the task-log at code review. Missing rows = missing tests = incomplete feature. Full example: `library/testing-reference.md` is behavior; matrix template also in `workflow.md` Rule 5.

## TDD Level Guidance by Risk (never-miss — project calibration)

| Level | Risk | Test requirement |
|-------|------|------------------|
| **A** | High — business logic, validation, state mutation, user-facing failure | Full TDD (Red→Green→Refactor); unit + property-based; all branches |
| **B** | Medium — query logic, mapping, pagination, EF config | Example tests for happy path + key edges; integration tests for queries |
| **C** | Low — pure plumbing, DI registration, DTO records, trivial getters | No mandatory test; document the no-test decision in the task-log if the task has ACs |

Escalation: a Level-C method that later causes a production bug is reclassified A/B with a regression test before fixing.

> **Authority note (project rule governs):** this risk-tiered model is intentional and OVERRIDES the absolutist "no production code without a failing test, no exceptions" stance of the generic `superpowers:test-driven-development` skill — which is why that skill is deliberately NOT enabled (rules-file-refactoring Task 11). Level C = no mandatory test is correct for this project.

## TDD Workflow

Red (write failing test) → run `dotnet test` (confirm it fails for the right reason) → Green (minimal code) → run (confirm pass) → Refactor (stay green). Never write implementation before the test. One test at a time in a single-agent session; the Tester/Builder split writes all tests first (details: `library/testing-reference.md`).

### Regression tests

When fixing a bug, write the failing test FIRST, confirm it fails, then fix, then confirm it passes — the regression test proves the bug existed and the fix works. (Severity → when a regression test is mandatory: `bug-tracking.md`.)

## Builder Must Not Modify Tests (never-miss — HARD discipline)

During Green, the Builder edits **production code only**. Never edit a test to pass, comment out an assertion, weaken setup, or delete a test that won't pass. A test is an encoded AC — changing it without changing the spec is silent spec deletion. If a test appears wrong: stop, log `blocked: spec gap`, escalate to Helder. Do not self-adjudicate.

## Project anti-patterns (never-miss)

- **Repository tests use real SQLite** (temp file), never the in-memory EF provider — it doesn't replicate SQLite collation quirks (`EF.Functions.Collate`).
- **Never mock the DbContext** in repository tests — EF query translation only runs against a real provider.
- **Service tests use Moq** (no real DB); **never call `Shell.Current`** in ViewModel tests (null in test context — wrap navigation behind an interface).
- Full anti-pattern table + rationale: `library/testing-reference.md`.

## Routing

| Need | Source |
|------|--------|
| Test project structure (csproj, GlobalUsings, dir layout), full test-type code patterns (Service/ViewModel/Repository, `TestDbContextFactory`), naming, what-to-test, Tester/Builder split, full TDD workflow, running-tests commands, quality-audit checklist, anti-pattern table | `.claude/library/testing-reference.md` |
| Mutation testing (Stryker.NET) — periodic quality gate | `.claude/library/mutation-testing-stryker.md` |
| Property-based testing (FsCheck) — invariants across input space | `.claude/library/property-based-testing-fscheck.md` |
| Generic xUnit/Moq/ViewModel test setup | `maui-unit-testing` skill (project) |

> **Authorship note:** Human-reviewed and approved by Helder 2026-07-09 (CLAUDE.md § Continuous Enhancement — Authorship). Full content preserved in `library/testing-reference.md` (+ Stryker/FsCheck files).
