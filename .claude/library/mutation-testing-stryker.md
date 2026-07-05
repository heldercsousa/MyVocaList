# Mutation Testing with Stryker.NET — Reference

> Extracted from `.claude/rules/testing.md` (2026-07-05, rules-file-refactoring Task 09–10). Its own on-demand file because mutation testing is a periodic quality gate, not per-commit work — no reason to carry it in the unconditionally-loaded rule. Discovered via the `myvocalist-coding` skill map or the `testing.md` routing table.
> Content moved verbatim; corrupted code fences normalized only.

Use mutation testing to detect tests that pass even when the production code is subtly wrong. Stryker.NET introduces small code mutations and verifies that at least one test fails per mutation.

## When to run

- After completing a Level A feature (see `testing.md` TDD Level Guidance)
- When a bug is found in production in an area believed to be well-tested
- As part of a quality audit requested by Helder

> Do NOT run Stryker on every commit — it is slow (minutes to hours). Run it as a periodic quality gate, not a CI gate.

## Setup (one-time, global .NET tool)

```bash
dotnet tool install -g dotnet-stryker
```

## Running

```bash
# From solution root — targets Services project, reports to TestResults/
dotnet stryker --project MyVocaList.Services/MyVocaList.Services.csproj \
               --test-project MyVocaList.Tests/MyVocaList.Tests.csproj \
               --reporter html \
               --output TestResults/Stryker
```

Open `TestResults/Stryker/reports/mutation-report.html` to review surviving mutants.

## Interpreting results

| Outcome | Meaning | Action |
|---------|---------|--------|
| **Killed** | A test caught this mutation | Good |
| **Survived** | No test failed for this mutation | Test gap — write a test that kills it |
| **No coverage** | No test exercises this code at all | Test gap or Level C code — classify and decide |
| **Timeout** | Mutation caused an infinite loop | May indicate a logic bug in the code |

## Target mutation score

| Layer | Minimum score |
|-------|--------------|
| Services (Level A methods) | 80% |
| Repositories (Level B methods) | 60% |
| ViewModels (Level A state transitions) | 70% |

> These are minimums, not targets. Aim higher when the effort is justified by risk.

## Surviving mutant triage

For each surviving mutant, decide:
1. **Write a killing test** — the mutant exposes a real gap; write a test that fails for this mutation
2. **Exclude the mutant** — the mutation is semantically equivalent (e.g., `i++` vs `i += 1`); add to `.stryker-config.json` excludes with a comment explaining why
3. **Reclassify code as Level C** — if the surviving mutant is in trivial plumbing, record the decision in the task-log

## Configuration file

Create `.stryker-config.json` at solution root when exclusions are needed:

```json
{
  "stryker-config": {
    "mutate": [
      "MyVocaList.Services/**/*.cs",
      "!MyVocaList.Services/GlobalUsings.cs"
    ],
    "excluded-mutations": ["StringLiteral"]
  }
}
```

---

> **Authorship note:** This file must be human-reviewed before it is relied upon (CLAUDE.md § Continuous Enhancement — Authorship).
