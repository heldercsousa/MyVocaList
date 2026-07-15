# Testing — Reference — Quality-audit checklist + Builder-must-not-modify + anti-patterns

> Section file split from `testing-reference.md` on 2026-07-14 (token-scoped reads). Index + provenance: `testing-reference.md`. Never-miss rules: `.claude/rules/testing.md`.

## Test Quality Audit Checklist

Run this checklist during code review for any test file. A test that fails one or more items must be fixed before the feature is marked `To Review`.

### For each test method

- [ ] **Name follows convention** — `{Method}_{Context}_{Expected}` with all three parts present
- [ ] **Has a Red phase** — the test was seen failing before the implementation that makes it pass was written (or Tester/Builder split was used)
- [ ] **Single behavioral assertion** — the test asserts one outcome; related asserts for the same outcome are permitted, but unrelated behaviors must be in separate tests
- [ ] **AC tag present** — user-facing behavior tests carry an `// [AC] REQ-XXX-YY` comment
- [ ] **AC exists in spec** — the referenced AC ID is present in `requirements.md`
- [ ] **No `Thread.Sleep`** — async timing uses `await Task.Delay` or `TaskCompletionSource`
- [ ] **No private-state assertions** — only public interface is tested
- [ ] **Arrange/Act/Assert** structure is visible — blank lines separate the three phases

### For each test class

- [ ] **No shared mutable state** between tests — each `[Fact]` is independent
- [ ] **Repository tests use real SQLite** — no in-memory EF provider
- [ ] **Service tests use Moq** — no real repositories, no real DB
- [ ] **Traceability matrix exists** in task-log for user-facing feature tests

### Audit frequency

- Before setting a task to `To Review` in the task-log
- During `/sln-review` (run after every task)

---

## Builder Must Not Modify Tests

During the Green phase, the Builder's only permitted action is writing or modifying **production code** in `MyVocaList.Domain`, `MyVocaList.Services`, `MyVocaList.Infra`, or `MyVocaList` (MAUI).

**The Builder must never:**
- Edit a test file to make a test pass
- Comment out an assertion
- Change a test's setup to avoid triggering a failure
- Delete a test that cannot be made to pass

**If a test appears wrong:**
The Builder must stop, document the suspected spec gap in the task-log (`blocked: spec gap`), and wait for the architect (Helder) to resolve it. The Builder does not unilaterally decide a test is wrong.

**Rationale:** A test represents an encoded acceptance criterion. Changing the test without changing the spec is silent spec deletion — the behavior remains unverified but appears tested.

---

## Anti-Patterns — Never Do These

| Anti-pattern | Why |
|---|---|
| Mock the DbContext in repository tests | Defeats the purpose — EF query translation only runs against a real provider |
| Assert on private state (`_field`) | Test the public interface only |
| Test XAML binding correctness | That's the MAUI runtime's job |
| Call `Shell.Current` in ViewModel tests | `Shell.Current` is null in test context — wrap navigation behind a service interface |
| Write multiple `Assert.*` for unrelated behaviors | One test, one behavioral assertion (related asserts for a single behavior are fine) |
| Use `Thread.Sleep` for async timing | Use `await Task.Delay` or `TaskCompletionSource` |
| Skip writing the failing test first (Step 4+) | This is TDD — the failing test is not optional |
| Modify a test to make it pass during Green phase | Tests define the contract. Changing a test to pass is not Green — it is spec deletion. If a test is wrong, escalate to the architect; do not silently fix it. See "Builder Must Not Modify Tests" for full escalation protocol. |
| Delete a failing test instead of implementing the behavior | Same as above — spec deletion. Failing tests are blockers, not noise. |

---
