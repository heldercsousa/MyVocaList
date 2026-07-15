# Testing — Reference — Tester/Builder split + TDD workflow + running tests

> Section file split from `testing-reference.md` on 2026-07-14 (token-scoped reads). Index + provenance: `testing-reference.md`. Never-miss rules: `.claude/rules/testing.md`.

## Tester/Builder Role Separation

In a TDD cycle, the agent that writes tests (Tester) and the agent that writes implementation (Builder) must be kept conceptually separate. In practice with subagents, enforce this by dispatching test-writing and implementation-writing as distinct tasks.

### Why it matters
When a single agent writes both tests and implementation simultaneously, it naturally writes tests that match the implementation rather than tests that verify the spec. The result is tests that pass but prove nothing.

### Rules

1. **Tester writes tests first, then stops.** The Tester subagent writes all tests for a task, confirms they compile and fail (Red), commits, and exits. It does NOT write any implementation. (Note: in a single-agent session, apply one-at-a-time discipline per "One test at a time — Exception.")
2. **Builder receives failing tests, makes them pass.** The Builder subagent reads the committed failing tests, writes only enough implementation to make them pass (Green), and exits. It does NOT modify tests.
3. **Refactor is a third, optional pass.** After Green, a separate refactor pass may clean up implementation without changing test or behavior.
4. **In a single-agent session:** apply the same discipline mentally — write all tests, run them to confirm failure, then switch to implementation mode.

### Dispatch pattern (from workflow.md)

```
Wave A: Tester subagent
  Input: spec (requirements.md, design.md), task description
  Output: committed failing tests, task-log status = "Red — tests written"

Wave B: Builder subagent
  Input: failing tests from Wave A, spec files
  Output: committed passing implementation, task-log status = "To Review"
```

---

## TDD Workflow (Red → Green → Refactor)

Starting from AutocompleteField + Person CRUD (Step 4+):

1. **Write the test first** — it fails (Red).
2. **Run `dotnet test`** — confirm failure message matches expected behavior.
3. **Write only enough implementation to make it pass** (Green).
4. **Run `dotnet test`** — confirm all pass.
5. **Refactor if needed** — no new tests fail.

**Never write implementation before the test.** If you write implementation first, you are not doing TDD.

### Regression tests
When fixing a bug, write the failing test FIRST, confirm it fails, then fix, then confirm it passes. The regression test proves the bug existed and the fix works.

### One test at a time

Write and run **one test** before proceeding to the next. Do not write all tests for a service method in one batch, then run them together.

**Rationale:** Batching test writes delays the Red confirmation. A test that was never seen failing may have been written incorrectly (wrong assertion, wrong setup). Each test must be seen to fail before the implementation that makes it pass is written.

**Incremental TDD cycle per test:**
1. Write one test → run → confirm Red
2. Write minimal implementation → run → confirm Green
3. Write next test → run → confirm Red (existing tests still Green)
4. Extend implementation → run → confirm all Green
5. Repeat

**Exception:** When the Tester/Builder split is used (separate subagents), the Tester writes all tests for a task together and confirms all fail, because the Builder has not yet run. The one-at-a-time discipline applies within a single-agent session.

---

## Running Tests

Test project path: `MyVocaList.Tests/MyVocaList.Tests.csproj` — e.g. `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj`. Filter/verbosity/coverage command variants → **`maui-unit-testing` skill § Running Tests**.

---
