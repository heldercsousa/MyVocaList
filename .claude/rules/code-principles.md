# Code Principles — Routing Table

> Detailed patterns live in `.claude/library/code-style-reference.md`, loaded on demand via the `myvocalist-coding` skill. The `##` headings below are cross-referenced by `§` anchor from other files — keep them.

## Architecture Constraints
Defined in `CLAUDE.md § Architecture` — they apply equally to code. "Business logic in Services" is unamendable.

## C# Style / Naming
`library/code-style-reference.md § C# Style` (design principles, modern C# 13+, naming, async, ViewModel pattern). Also: XML docs (`<inheritdoc />` rule), nullable reference types (disabled/lenient), global usings, pagination (single source of truth), static-analysis suppressions — each a `§` of the same file.

## Service Return Patterns
`library/code-style-reference.md § Service Return Patterns` — tuple returns `(bool success, string message, T? entity)`; never throw for expected business failures.

## Exception Handling
`library/code-style-reference.md § Exception Handling` — `GlobalExceptionHandler` categories + the four allowed try-catch patterns; no silent-swallow catches.

## DI Registration Conventions
`library/code-style-reference.md § DI Registration Conventions` — Singleton (shared state) / Scoped (repos, services) / Transient (pages, ViewModels).

## UI Thread Performance — ObservableRangeCollection
`library/code-style-reference.md § UI Thread Performance` — one `ReplaceRange` per `RunOnUiThread` block; clear selection after reload; no LINQ/service calls inside the UI-thread block.

EF Core / SQLite constraints: `constraints-registry.md § EF Core / SQLite`. Design skills: `modern-csharp-coding-standards`, `dependency-injection-patterns`, `efcore-patterns`.
