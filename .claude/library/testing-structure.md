# Testing — Reference — Test project structure (csproj deltas, GlobalUsings)

> Section file split from `testing-reference.md` on 2026-07-14 (token-scoped reads). Index + provenance: `testing-reference.md`. Never-miss rules: `.claude/rules/testing.md`.

## Test Project Structure

### Project: `MyVocaList.Tests`

```
MyVocaList.Tests/
├── MyVocaList.Tests.csproj
├── GlobalUsings.cs
├── Unit/
│   ├── Services/              ← pure business logic, Moq dependencies
│   │   └── VenueServiceTests.cs
│   └── ViewModels/            ← ViewModel commands + state, Moq services
│       └── VenuesViewModelTests.cs
├── Integration/
│   └── Repositories/          ← real SQLite temp DB, no containers
│       └── VenueRepositoryTests.cs
└── Infrastructure/
    └── TestDbContextFactory.cs ← shared DB setup/teardown helper
```

### .csproj — project-specific deltas only

Generic test-project setup (csproj skeleton, xUnit/Moq/coverlet packages, conditional `OutputType` trick for referencing the app head) → **`maui-unit-testing` skill § Test Project Setup**. MyVocaList-specific facts:

- **TFM is `net10.0` only** (NOT `net10.0-android`) so tests run on the desktop host.
- Add `Microsoft.EntityFrameworkCore.Sqlite` `10.*` (repository integration tests use real SQLite).
- Reference the four non-MAUI projects: `MyVocaList.Domain`, `MyVocaList.Contracts`, `MyVocaList.Infra`, `MyVocaList.Services`.
- Do NOT reference `MyVocaList.csproj` (MAUI head) unless ViewModel tests are needed; if referenced, apply the skill's conditional `<OutputType>Library</OutputType>` on the `net10.0` TFM.

### GlobalUsings.cs

```csharp
global using Xunit;
global using Moq;
global using MyVocaList.Domain.Entities;
global using MyVocaList.Domain.Interfaces;
global using MyVocaList.Contracts.DTOs.List;
global using MyVocaList.Services;
```

---
