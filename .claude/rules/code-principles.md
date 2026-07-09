# Code Principles — Routing Table

> Language rule: English only — see `CLAUDE.md § Constitutional Constraints`.
> **This file is a routing table.** Detailed patterns live in `.claude/library/code-style-reference.md`, loaded on demand via the `myvocalist-coding` skill. The section headings below are preserved because other files cross-reference them by `§` anchor — each points to the authoritative source.

| Topic | Authoritative source |
|-------|----------------------|
| XML documentation comments (`<inheritdoc />` rule) | `library/code-style-reference.md § XML Documentation Comments` |
| Nullable reference types (disabled/lenient) | `library/code-style-reference.md § Nullable Reference Types` |
| C# style — design principles, modern C# 13+, naming, async, ViewModel pattern | `library/code-style-reference.md § C# Style` |
| Service return tuple patterns | `library/code-style-reference.md § Service Return Patterns` |
| Exception handling — GlobalExceptionHandler + allowed try-catch | `library/code-style-reference.md § Exception Handling` |
| Global usings (per project + Directory.Build.props) | `library/code-style-reference.md § Global Usings` |
| Pagination single source of truth | `library/code-style-reference.md § Pagination` |
| DI registration lifetimes | `library/code-style-reference.md § DI Registration Conventions` |
| ObservableRangeCollection / DXCollectionView reset perf | `library/code-style-reference.md § UI Thread Performance` |
| Static analysis suppressions | `library/code-style-reference.md § Static Analysis Suppressions` |
| EF Core / SQLite constraints | `constraints-registry.md § EF Core / SQLite` |

Design skills for deeper C#/.NET guidance: `dotnet-skills:modern-csharp-coding-standards`, `dotnet-skills:dependency-injection-patterns`, `dotnet-skills:efcore-patterns`.

---

## Architecture Constraints
Architecture layer constraints are defined in `CLAUDE.md § Architecture` — they apply equally to code. The "business logic in Services" constraint is unamendable (`CLAUDE.md § Constitutional Constraints`).

## C# Style / Naming
See `library/code-style-reference.md § C# Style` (design principles, modern C# 13+, naming conventions, async, ViewModel pattern).

## Service Return Patterns
See `library/code-style-reference.md § Service Return Patterns` — tuple returns `(bool success, string message, T? entity)`; never throw for expected business failures.

## Exception Handling
See `library/code-style-reference.md § Exception Handling` — `GlobalExceptionHandler` categories + the four allowed try-catch patterns; no silent-swallow catches.

## DI Registration Conventions
See `library/code-style-reference.md § DI Registration Conventions` — Singleton (shared state) / Scoped (repos, services) / Transient (pages, ViewModels).

## UI Thread Performance — ObservableRangeCollection
See `library/code-style-reference.md § UI Thread Performance` — one `ReplaceRange` per `RunOnUiThread` block; clear selection after reload; no LINQ/service calls inside the UI-thread block.

---

> **Authorship note:** Human-reviewed and approved by Helder 2026-07-09 (CLAUDE.md § Continuous Enhancement — Authorship). Full content preserved in `library/code-style-reference.md`.
