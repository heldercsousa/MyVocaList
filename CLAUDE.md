# CLAUDE.md - MyVocaList

## App
Karaoke queue management with round-based progression. .NET MAUI 8.0 (net8.0-android).

## Language
Code, comments, logs: **English only**

## Translation
Translate any non-English text (comments, strings, logs) to English when encountered.

## Comments
- **Only**: classes, records, structs, methods (XML summary)
- **Never**: code inside method bodies

## Architecture
```
Domain → Contracts → Services → Infrastructure → View
(Entities)  (DTOs)    (Logic)    (EF+SQLite)    (MAUI)
```
- Business logic **only** in Services
- Interface + Implementation in **same folder**

## DDD Patterns
| Pattern | Implementation |
|---------|----------------|
| Aggregates/Entities | Base classes |
| Value Objects | Records |
| Domain Events | MediatR notifications |
| CQRS | Command/Query handlers |
| Repository | EF Core 9 + SQLite |

## TDD
- Test-first: Domain + Services
- Stack: xUnit, FluentAssertions, NSubstitute

## Error Handling
- **Avoid**: try-catch, `Debug.WriteLine`, `Console.WriteLine`
- **Use**: Serilog via `ILogger<T>`
- **Use**: Guard pattern for validation

```csharp
// ✅ Correct
Guard.AgainstNullOrWhiteSpace(name, nameof(name));

// ❌ Wrong
if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException();
```

## Git Commits
```
<type>: <summary>

- detail 1
- detail 2

Co-Authored-By: Claude <noreply@anthropic.com>
```
Types: `feat:`, `fix:`, `refactor:`, `docs:`, `perf:`, `test:`

## Changelog
- Location: `Docs/Changelog/changelog.md`
- Format: `- **MM/dd/yyyy** - Type - Description`
- Types: Enhancement | Fix
- **Update after every completed task**

## Stack
```
MediatR, FluentValidation, Serilog, EF Core 9, SQLite
UraniumUI (Material Design 3)
```