# Property-Based Testing with FsCheck — Reference

> Extracted from `.claude/rules/testing.md` (2026-07-05, rules-file-refactoring Task 09–10). Its own on-demand file because PBT applies to a minority of methods (invariants across an input space) — no reason to carry it in the unconditionally-loaded rule. Discovered via the `myvocalist-coding` skill map or the `testing.md` routing table.
> Content moved verbatim; corrupted code fences normalized only.

Use property-based testing (PBT) for service methods whose correctness must hold across a wide range of inputs — not just the specific examples in example-based tests.

## When to use PBT

| Use PBT | Use example-based tests |
|---------|------------------------|
| Validation rules (length, format, range) | Specific error messages |
| Round-trip invariants (create → read → same) | Exact entity mapping |
| Pagination arithmetic (skip/take consistency) | Integration flows |
| Commutative or associative operations | Error path specifics |

## Setup

Add to `MyVocaList.Tests.csproj`:

```xml
<PackageReference Include="FsCheck.Xunit" Version="2.*" />
```

Add to `GlobalUsings.cs`:

```csharp
global using FsCheck;
global using FsCheck.Xunit;
```

## Usage pattern

```csharp
[Property]
public Property CreateVenueAsync_NameWithinLimit_AlwaysSucceeds(string name)
{
    // Generate names that are 1–30 chars (within valid range)
    return Prop.ForAll(
        Arb.Default.NonEmptyString().Filter(s => s.Value.Length <= 30),
        async name =>
        {
            _repoMock.Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(false);
            var sut = CreateSut();
            var (success, _, _) = await sut.CreateVenueAsync(name.Value);
            return success;
        });
}

[Property]
public Property ValidateNameInput_NameExceedsLimit_AlwaysInvalid()
{
    return Prop.ForAll(
        Arb.Default.NonEmptyString().Filter(s => s.Value.Length > 30),
        name =>
        {
            var sut = CreateSut();
            var (isValid, _) = sut.ValidateNameInput(name.Value);
            return !isValid;
        });
}
```

## Rules

1. **PBT supplements, does not replace, example-based tests.** Keep the example tests — they document specific behaviors. PBT adds confidence across the input space.
2. **Always filter generated inputs to valid domains.** Unconstrained string generation produces null, empty, and control characters that test error-handling rather than the property.
3. **Properties must be deterministic.** A property that depends on external I/O (DB, clock) is not a property — it is a flaky test. Mock all dependencies.
4. **Label failures.** Use `|@` in FsCheck to attach labels when a property has multiple conditions, so shrunk counterexamples are readable.

---

> **Authorship note:** This file must be human-reviewed before it is relied upon (CLAUDE.md § Continuous Enhancement — Authorship).
