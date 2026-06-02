# Architecture Tests — Value Assessment & Adoption Decision

**Date:** 2026-05-12  
**Type:** Research / Adoption Decision (not a feature spec)

---

## Context

MyVocaList enforces architectural constraints through documented rules (CLAUDE.md, code-principles.md) and agent-applied discipline. Five of the six constitutional constraints are **behavioral** — no type system or automated check prevents violations. The question is: would adopting an architecture test library (NetArchTest.Rules or ArchUnitNET) add enough value to justify the overhead, given the current project state?

---

## Current State Snapshot

| Dimension | Finding |
|-----------|---------|
| Project count | 6 (Domain, Contracts, Infra, Services, MAUI, Tests) |
| .cs file count | ~100 (Domain 13, Contracts 7, Infra 15, Services 8, MAUI 33, Tests 11) |
| Documented rules | 6 constitutional constraints, all unamendable |
| Type-enforceable rules | 5 of 6 (layer dependency graph, namespace discipline) |
| Existing violations | **Zero found** (grep scan confirmed) |
| Current enforcement | Manual code review + CLAUDE.md rules + agent skill gating |
| Test infra maturity | Mature (xunit, Moq, real SQLite, coverage) |

---

## Dependency Graph (Actual)

```
Contracts  (no deps)
  ↑
Domain     → Contracts
  ↑          ↑
Infra      → Domain + Contracts
Services   → Domain + Contracts
  ↑
MAUI       → Services + Domain + Infra
Tests      → all 5
```

No circular references. No illegal cross-layer references observed.

---

## Where Architecture Tests Would Add Value

### Risk 1 — Business logic leaking into ViewModels (HIGHEST RISK)
- **Rule:** Business logic in Services only (unamendable)
- **How it happens:** "Just sort/filter locally in the ViewModel to save a service call"
- **Current protection:** Rule in CLAUDE.md, agent skills, code review
- **Architecture test coverage:** Partial — can assert ViewModels don't call domain entities directly; cannot enforce "no if-statements in ViewModel" (that's a lint/review concern, not a type dependency)

### Risk 2 — Services depending on Infra directly (MEDIUM RISK)
- **Rule:** Services depend on Domain interfaces, not Infra implementations
- **How it happens:** `using MyVocaList.Infra.SomeRepository` instead of `IRepository`
- **Current protection:** csproj does NOT include Infra reference in Services — **this is already mechanically enforced by the project reference graph**
- **Architecture test coverage:** Redundant (csproj already enforces this)

### Risk 3 — MAUI consuming Service implementations directly (LOWER RISK)
- **Rule:** MAUI binds to service interfaces, not concrete classes
- **How it happens:** `new VenueService(...)` or direct class reference in UI
- **Current protection:** DI in MauiProgram.cs + naming discipline; interfaces are what's injected
- **Architecture test coverage:** Could assert MAUI pages/ViewModels only reference types from `IVenueService`, not `VenueService`

### Risk 4 — Domain referencing Infra or Services (LOW RISK)
- **Current protection:** csproj Domain project has NO reference to Infra or Services — **mechanically enforced**
- **Architecture test coverage:** Redundant

---

## Honest Assessment: What Would Architecture Tests Actually Catch?

| Rule | Already enforced by csproj? | Architecture test value |
|------|---------------------------|------------------------|
| Domain not referencing Infra | YES (no project reference) | Low — redundant |
| Services not referencing Infra | YES (no project reference) | Low — redundant |
| Contracts not referencing Domain | YES (no project reference) | Low — redundant |
| ViewModels not importing entities directly | No | Medium — meaningful |
| ViewModel logic stays out of domain behavior | No (behavioral, not type-level) | None — untestable at type level |
| English-only naming | No | None — requires lint, not arch test |
| No DisplayAlert in UI code | No | Medium — can grep/assert at test level |

**Key insight:** The three highest-risk, unamendable rules (business logic in Services, no native dialogs, DevExpress-first) are **behavioral rules** — they cannot be verified by a type-dependency architecture test library. What can be verified (cross-project references) is already enforced by the csproj reference graph.

---

## The Two Frameworks

### NetArchTest.Rules
- **Maturity:** High (stable, widely used in .NET DDD codebases)
- **API:** Fluent, readable (`Types().That().ResideInNamespace("x").Should().NotDependOn("y")`)
- **Target framework:** `net10.0` test project — compatible
- **Setup time:** ~2 hours (install + write 5-8 rules)
- **Limitation:** Operates on compiled assemblies — namespace/type-level only; cannot detect method-level behavior

### ArchUnitNET
- **Maturity:** High (more expressive than NetArchTest, supports slices/architecture styles)
- **API:** More verbose but richer predicate system
- **Target framework:** `net10.0` — compatible
- **Setup time:** ~3-4 hours
- **Advantage over NetArchTest:** Supports architecture slice definitions (hexagonal, onion, clean)

---

## Recommendation

**Verdict: Defer, with a low-effort hedge.**

### Why defer full adoption

1. **csproj already enforces the most critical cross-project boundaries** — the three unamendable dependency rules (Domain→Contracts only, Services→Domain only, Infra→Domain) are structurally enforced, not just documented.

2. **The remaining risks are behavioral** — "no business logic in ViewModel" cannot be caught by NetArchTest; it requires review or linting.

3. **Zero violations in current codebase** — adoption now is preventive infrastructure for a problem that doesn't exist yet.

4. **Single-developer project** — the ROI argument for architecture tests strengthens significantly with team growth.

### The low-effort hedge: 3 targeted rules worth doing now

If Helder wants to adopt architecture tests, the highest-ROI subset is:

```
MyVocaList.Tests\Architecture\
  └── LayerDependencyTests.cs     (~3 assertions, ~30 lines)
```

**Rule 1 — ViewModel should not directly instantiate entities**
- Detects: ViewModels creating domain entities inline instead of calling services
- Assertion: types in `MyVocaList.UI.ViewModels` must not construct types in `MyVocaList.Domain.Entities`

**Rule 2 — Domain must not reference Infra or Services (belt-and-suspenders)**
- Redundant with csproj but acts as a human-readable executable spec
- Documents intent in runnable form (not just in CLAUDE.md)

**Rule 3 — Services must not reference MAUI (belt-and-suspenders)**
- Prevents accidental MAUI → Services reverse dependency
- Documents business layer independence from the UI framework

**Estimated effort:** 1.5 hours (install NetArchTest + write 3 tests)  
**Ongoing cost:** 0 (tests run with existing `dotnet test`, no new CI step)

---

## Decision Framework

| Scenario | Recommendation |
|----------|---------------|
| Helder remains sole dev + codebase stays < 150 files | Defer — review discipline sufficient |
| Team grows to 2+ developers | Adopt — full layer rule set (8-10 tests) |
| A behavioral violation reaches production | Adopt immediately + add regression test for that violation |
| MAUI head grows beyond 150 .cs files | Adopt — manual review of layer boundaries becomes impractical |
| A new bounded context is added (e.g. Queue Management) | Adopt — multiple module boundaries justify formalization |

---

## If Adopting: Suggested Rule Set

```csharp
// File: MyVocaList.Tests/Architecture/LayerDependencyTests.cs
// Framework: NetArchTest.Rules

[Fact] Domain_ShouldNotReferenceInfra()
[Fact] Domain_ShouldNotReferenceServices()
[Fact] Domain_ShouldNotReferenceMaui()
[Fact] Services_ShouldNotReferenceMaui()
[Fact] Services_ShouldNotReferenceInfra()
[Fact] ViewModels_ShouldNotDirectlyConstructDomainEntities()
[Fact] AllTypes_ShouldResideInCorrectNamespace()  // no stray types
```

**Not recommended to encode as arch tests:**
- Business logic boundary (ViewModels vs Services) — behavioral, not type-level
- DevExpress-first rule — UI component choice, not a namespace constraint
- English-only naming — requires a naming analyzer, not an arch test
- SafeAreaEdges — XAML attribute, not a C# type dependency

---

## Files to Change (if adopting)

| File | Change |
|------|--------|
| `MyVocaList.Tests\MyVocaList.Tests.csproj` | Add `<PackageReference Include="NetArchTest.Rules" Version="1.3.*" />` |
| `MyVocaList.Tests\Architecture\LayerDependencyTests.cs` | New file — 3-7 arch test assertions |
| `.claude\rules\testing.md` | Add section: Architecture Tests — when to add, what to encode |
| `CLAUDE.md` | Update "Constitutional Constraints" to note which rules are test-covered vs review-only |

---

## Summary

Architecture tests for MyVocaList would be **low-effort to add** and **partially redundant** — the most critical boundaries are already enforced by the csproj reference graph. The real value is in **documenting intent as executable code** and catching the smaller class of violations (ViewModel-entity coupling) that csproj can't prevent.

**Recommended action:** Defer full adoption; optionally add 3 targeted tests as a hedge. Revisit when team grows or MAUI expands past 150 types.
