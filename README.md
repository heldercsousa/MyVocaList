# MyVocaList

[![License: CC BY-NC-ND 4.0](https://img.shields.io/badge/License-CC%20BY--NC--ND%204.0-lightgrey.svg)](https://creativecommons.org/licenses/by-nc-nd/4.0/)

Karaoke queue management for live events — .NET MAUI 10 (Android & iOS)

**Status:** Active development — Clean Architecture implementation in progress

**License:** [CC BY-NC-ND 4.0](LICENSE) — Portfolio project with commercial intentions

**Author:** Helder Sousa | heldercsousa@gmail.com

---

## What it does

MyVocaList helps a karaoke event admin manage one active singer queue at a time with round-based progression. Key capabilities:

- Register singers and track participation or absence across rounds
- Reorder the queue manually at any time
- Estimate time-to-completion for the current queue
- Two queue modes: **Mechanical Karaoke** (pre-recorded tracks) and **Bandoke** (live instrumental band)

Planned features: singer self-registration, song catalog, lyrics via external API, social features.

## Tech stack

| Area | Technology |
|------|-----------|
| Framework | .NET MAUI 10 — Android & iOS |
| Language | C# 13 |
| UI components | DevExpress MAUI v25.2 |
| Architecture | Clean Architecture (Domain / Infra / Services / UI) |
| State management | CommunityToolkit.Mvvm |
| Persistence | EF Core 10 + SQLite |
| Logging | Serilog |
| Planned | MediatR, FluentValidation |

## Project structure

```
MyVocaList.Domain      — entities, repository interfaces, domain logic
MyVocaList.Contracts   — DTOs, pagination constants
MyVocaList.Infra       — EF Core DbContext, migrations, repository implementations
MyVocaList.Services    — business logic (all business rules live here)
MyVocaList             — MAUI head project: pages, ViewModels, DI registration
MyVocaList.Tests       — xUnit tests (unit + integration)
```

## Getting started

Requirements: .NET 10 SDK, Android SDK or iOS toolchain, DevExpress MAUI license.

```bash
git clone https://github.com/heldersousa/MyVocaList.git
cd MyVocaList
dotnet restore
dotnet build
```

Run on Android emulator:

```bash
dotnet build -t:Run -f net10.0-android
```

Run tests:

```bash
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj
```

## Claude Code setup (for AI-assisted development)

For AI-assisted development with Claude Code:

```bash
dotnet restore
dotnet build
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj
```

**Database & Testing Details:** See [`.claude/library/database-setup.md`](.claude/library/database-setup.md) for:
- How database migrations are applied
- How the SQLite MCP database is kept in sync (auto-sync via hooks)
- How tests guarantee the current schema
- Setup on fresh clones

> ⚠️ **Note:** Database setup instructions are maintained in `.claude/library/database-setup.md` to ensure they stay current if the DB solution changes. The README does not duplicate technical DB details to avoid becoming stale.
