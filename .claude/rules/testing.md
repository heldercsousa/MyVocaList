# Testing Rules — Routing Table

> Full detail (structure, patterns, naming, Tester/Builder split, running tests, quality-audit, anti-patterns) lives in `.claude/library/testing-reference.md`; periodic techniques in `library/mutation-testing-stryker.md` and `library/property-based-testing-fscheck.md`. The project-core never-miss rules stay inline. TDD applies to all new/modified Services, ViewModels, Repositories. Generic xUnit/Moq/ViewModel setup: `maui-unit-testing` skill. (`superpowers:test-driven-development` is deliberately NOT enabled — see the authority note below.)

## TDD within SDD (never-miss)

Every acceptance criterion → at least one test. Do not write a test with no AC (add the AC to the spec first). Do not write code with no test. Red → run (confirm it fails for the right reason) → Green (minimal code) → Refactor; one test at a time in a single-agent session.

## Acceptance Criteria Traceability (never-miss — project SDD core)

Every test covering user-facing behavior traces to an AC in `requirements.md`.
- **Format:** first line of the test's doc/inline comment: `// [AC] REQ-VENUE-03: <criterion>`.
- **Rules:** every AC → ≥1 test; one test → one AC (split if more); AC IDs come from the spec (don't invent); infra tests (`TestDbContextFactory`, builders) are exempt.
- **Traceability matrix** (AC ID | Criterion | Implementation location | Test method) goes in the task-log at code review (template: `workflow.md` Rule 5). Missing rows = missing tests = incomplete feature.

## TDD Level Guidance by Risk (never-miss — project calibration)

| Level | Risk | Test requirement |
|-------|------|------------------|
| **A** | High — business logic, validation, state mutation, user-facing failure | Full TDD (Red→Green→Refactor); unit + property-based; all branches |
| **B** | Medium — query logic, mapping, pagination, EF config | Example tests for happy path + key edges; integration tests for queries |
| **C** | Low — pure plumbing, DI registration, DTO records, trivial getters | No mandatory test; document the no-test decision in the task-log if the task has ACs |

Escalation: a Level-C method that later causes a production bug is reclassified A/B with a regression test before fixing.

> **Authority note (project rule governs):** this risk-tiered model is intentional and OVERRIDES the absolutist "no production code without a failing test, no exceptions" stance of the generic `superpowers:test-driven-development` skill. Level C = no mandatory test is correct for this project.

### Regression tests

When fixing a bug, write the failing test FIRST, confirm it fails, then fix, then confirm it passes. (Severity → when mandatory: `bug-tracking.md`.)

## Builder Must Not Modify Tests (never-miss — HARD discipline)

During Green, the Builder edits **production code only**. Never edit a test to pass, comment out an assertion, weaken setup, or delete a test that won't pass. A test is an encoded AC — changing it without changing the spec is silent spec deletion. If a test appears wrong: stop, log `blocked: spec gap`, escalate to Helder. Do not self-adjudicate.

## Project anti-patterns (never-miss)

- **Repository tests use real SQLite** (temp file), never the in-memory EF provider — it doesn't replicate SQLite collation quirks (`EF.Functions.Collate`).
- **Never mock the DbContext** in repository tests — EF query translation only runs against a real provider.
- **Service tests use Moq** (no real DB); **never call `Shell.Current`** in ViewModel tests (null in test context — wrap navigation behind an interface).

## Routing

| Need | Source |
|------|--------|
| Structure, test-type patterns, naming, what-to-test, Tester/Builder split, running tests, quality-audit, full anti-pattern table | `.claude/library/testing-reference.md` (family index — read ONLY the section file needed) |
