# MyVocaList

[![License: CC BY-NC-ND 4.0](https://img.shields.io/badge/License-CC%20BY--NC--ND%204.0-lightgrey.svg)](https://creativecommons.org/licenses/by-nc-nd/4.0/)

Karaoke (video based) and Bandoke (artist perform instrumental) queue management for live events — .NET MAUI 10 (Android & iOS & Windows)

**Status:** currently, development cycle is approaching with succes the launch of the MVP version, which targets in this early version Android OS devices only. Predicted stuning post-MVP functionalities, but only app has some engagement from a minimal community.

**License:** [CC BY-NC-ND 4.0](LICENSE) 

**Author:** Helder Sousa | heldercsousa@gmail.com

**Personal goal**: be an Helder's portfolio, a playground where a bunch of new skills set are exploited, incrementing the Helder's 26 years of experience (almost in .Net Web field). Beyond putting Helder's capacity for creating adaptable/extensible production ready softwares with great accurary since early version, it's bringing me crucial knowledge in:
1. Hybrid mobile/desktop app development on top of .NET MAUI 10.
2. .NET 10 updates
3. Material Design 3
5. UraniumUI (exploited but replaced by DeveExpress MAUI)
6. DevExpress MAUI (exploited deeply. Current version uses it)
7. MudBlazor (will replace DevExpress MAUI. Post-MVP phase. Is Android, iOS, Windows and Web compatibleP). The very same UI is ensured across the platforms by using a shared centralized UI libraly
8. Claude Code most recent tools, SDD as guideline for Helder's coding leadership/product owner and Claude Code senior development accuracy. 
9. Phyton coding
10. Custom LLM fundamental creation aiming to offer the users an straightforward experience and delivering some minimal services in a nut.
11. Software Architecture concepts and usage: MVVM - DDD - SOLID - Clean Code - TDD
12. System Design -fundamentals - MVP must prove a minimal engagement before exploiting in real labor more advanced approaches.
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
