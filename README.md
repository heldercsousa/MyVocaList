# MyVocaList

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Karaoke queue management for live events. A .NET MAUI 10 app for Android and iOS.

**Status:** pre-MVP, in active development (app version `0.1.0`). Not yet published to any store; Android test builds are distributed by hand.

**License:** [MIT](LICENSE). Anyone may use, modify and distribute it.

**Author:** Helder Sousa | heldercsousa@gmail.com

---

## What it is for

The admin of a karaoke event runs **one active singer queue at a time**, with round-based progression. The admin registers singers, tracks who took part and who was absent in each round, reorders the queue by hand, and sees an estimate of when the queue will finish. There are two queue modes: **Mechanical Karaoke** (pre-recorded tracks) and **Bandokê** (a live instrumental band).

## What works today

The app's catalog and registration side is built. **The queue itself is not built yet** (see [Roadmap](#roadmap)).

| Area | What ships |
|------|-----------|
| **Venues** | Create, edit, delete and search. International address fields; pick the place on a map to fill in the name and address (OpenStreetMap Nominatim); venue picture. A venue is a duplicate only if both name and address match. |
| **Singers** | Create, edit, delete and search. Birthday as day + month dropdowns; the list shows birthday and email. |
| **Songs & artists** | A catalog where you type the title first, with one search across local songs and Deezer. A Deezer pick fills in the artist, artwork and (via LRCLIB) the lyrics. Fuzzy near-duplicate detection, YouTube karaoke links and a YouTube search, and pictures with automatic crop. **Artist catalogs** stay in sync with the songs automatically: a saved song (new or edited) appears in its artist's catalog, a changed artist moves it, and a deleted song leaves it. Adding cover songs to a catalog by hand is not built yet. |
| **Lists** | One shared CRUD list pattern across pages: off-UI-thread loading with a shimmer, persistent search bar, multi-select gestures, MD3 overflow menus. |
| **App infrastructure** | Settings, backup & restore (local bundle, Tiers 1 + 3), crash and error reporting (Sentry), What's New release notes, update check, in-app suggestions, About page. |

## Roadmap

The full list is generated from item frontmatter in [`Docs/Management/BACKLOG.md`](Docs/Management/BACKLOG.md) (MVP) and [`BACKLOG-POST-MVP.md`](Docs/Management/BACKLOG-POST-MVP.md). Never edit the rows there by hand.

**Before the MVP release**
- **Queue management**: the queue page becomes the list of events. Creating an event opens queue management (rounds, participation/absence, reordering, completion estimate).
- Catalog follow-ups: a picker for adding cover songs to an artist's catalog by hand; a grouped catalog grid filled in automatically from karaoke participations (built together with the queue pages); loading indicators for remote searches.
- **Licensing of the UI component library**: the DevExpress free offer ended, so DevExpress 25.2 is paid. Syncfusion Community is being evaluated. This blocks the MVP.
- Play Store / Samsung Galaxy Store pre-submission compliance, a full pre-release mobile test pass, and a large-volume data stress test.

**After the MVP:** singer self-registration and self check-in, social features, Wi-Fi backup mirror, in-app tutorial, a project website, and richer Deezer browsing.

## Tech stack

The single source of truth for versions is [`Directory.Packages.props`](Directory.Packages.props) (central package management). Only major versions are shown here.

| Area | Technology |
|------|-----------|
| Framework | .NET 10 · .NET MAUI 10 (`net10.0-android`, `net10.0-ios`) |
| Language | C# (SDK default for .NET 10); nullable reference types disabled on purpose |
| UI components | DevExpress .NET MAUI 25.2 (DevExpress first; stock MAUI only where DevExpress has no equivalent) · Material Design 3 |
| MVVM | CommunityToolkit.Mvvm 8 · CommunityToolkit.Maui 14 |
| Persistence | EF Core 10 + SQLite (DB-side collation for search and dedup; no normalized shadow columns) |
| Logging & crashes | Serilog 4 · Sentry 5 |
| Other | Scrutor (DI scanning) · FuzzySharp (similarity) · Plugin.LocalNotification · MinVer (versions from git tags) |
| Tests | xUnit 2 · Moq 4 · FsCheck 2 (property-based) · real-SQLite integration tests |

**External services:** Deezer API (song/artist search, artwork) · LRCLIB (lyrics) · YouTube Data API v3 (karaoke video search, using your own API key set in the app's Settings) · OpenStreetMap Nominatim (venue address lookup) · Sentry (crash reporting).

## Architecture

```
Domain/      MyVocaList.Domain      entities, domain rules, repository + unit-of-work interfaces
Contracts/   MyVocaList.Contracts   DTOs, pagination constants
Extensions/  MyVocaList.Extensions  shared, dependency-free helpers
Infra/       MyVocaList.Infra       EF Core DbContext, migrations, repositories
Services/    MyVocaList.Services    business logic (ALL business rules live here) + external API providers
MyVocaList/  MyVocaList             MAUI head: Shell, pages, ViewModels, components, DI registration
Tests/       MyVocaList.Tests       unit, integration (real SQLite) and architecture tests
```

These constraints are enforced mechanically by hooks and tests (full list in [`CLAUDE.md`](CLAUDE.md)):
- Business logic lives in Services, never in ViewModels or pages.
- No native dialogs; every dialog is a DevExpress `BottomSheet`.
- Every `ContentPage` declares `SafeAreaEdges`.
- Code, comments, logs and UI text are in English only.

## Getting started

**Setting up a machine from scratch** (toolchain, DevExpress feed and license, git hooks, Claude Code, MCP servers): follow [`Docs/DevEnv/devenv-setup.md`](Docs/DevEnv/devenv-setup.md). It is the only setup guide; the commands below are a quick reference.

**Requirements:** .NET 10 SDK or newer with the MAUI workload (Visual Studio 2026), the Android SDK (or the iOS toolchain on macOS), and access to the DevExpress NuGet feed with a valid DevExpress license.

```bash
git clone https://github.com/heldercsousa/MyVocaList.git
cd MyVocaList
git config core.hooksPath .claude/githooks   # required: enables the pre-commit and commit-msg hooks
dotnet restore
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
```

Run on an Android emulator or device:

```bash
dotnet build MyVocaList/MyVocaList.csproj -t:Run -f net10.0-android
```

Run the tests:

```bash
dotnet test Tests/MyVocaList.Tests.csproj
```

The app creates and migrates its SQLite database on the device at startup. Migrations, the schema-synced query database and test databases are covered in [`.claude/library/database-setup.md`](.claude/library/database-setup.md). That file is the only place these details are kept, so this README does not repeat them.

## How this app is built: Helder's role and the AI-assisted dev cycle

MyVocaList is a solo project. **Helder Sousa** owns the product and acts as **architect and technical auditor**. Most of the code is written by **Claude Code** (Anthropic) working as the implementation specialist. It runs inside a development process that Helder designed and keeps refining. Of roughly 2,640 commits since the first one on 2025-12-25, about 1,620 carry a Claude co-author trailer.

**What Helder does**
- **Product owner.** Defines the vision, the features and their priority (the backlog), and the acceptance criteria.
- **Architect.** Makes every architectural decision and trade-off. Examples: Clean Architecture layering, DevExpress-first UI, DB-side collation instead of normalized columns, and the UI-library licensing review. Claude Code never makes these decisions on its own.
- **Gatekeeper.** Approves every spec and plan before implementation starts. A fresh-context `spec-reviewer` and `plan-reviewer` check each one first. Changes to shared UI components need his recorded approval.
- **Auditor and tester.** Reviews delivered work, runs on-device and emulator verification gates, and decides when a fix is really fixed.
- **Designer of the process.** Writes and reviews the governance that steers the AI. Context files (`CLAUDE.md`, `.claude/rules/`) must be authored or reviewed by a human, and every rule change is an `amend:` commit with its rationale.
- **Release owner.** Decides when a build goes out. Deployments run only when he asks, after the screens are checked on an emulator.

**The process he set up**
- **Spec-anchored SDD (spec-driven development).** Every feature has `requirements.md`, `design.md`, `tasks.md` and `task-log.md` under [`Docs/Management/`](Docs/Management/), and the spec changes before the code does. A shipped spec is never edited; later changes go in dated `changes/` folders beside it.
- **Risk-tiered TDD.** Business logic gets full Red → Green → Refactor. Every test traces to an acceptance-criterion ID (`// [AC] REQ-…`). Repository tests run against real SQLite. The suite has about 1,780 test methods.
- **Orchestrator plus subagents.** A coordinating agent dispatches implementor subagents in waves of at most 4. Each works in its own git worktree on a task branch, and an independent `verifier` checks the result against the spec.
- **Tracked bugs.** Each bug gets a severity, a generated ID and, when Critical or Major, a mandatory regression test that must fail before the fix and pass after it.
- **Mechanical enforcement.** Claude Code hooks and git hooks block violations instead of relying on reminders. They cover constitutional constraints, worktree-only code edits, generated-backlog protection, `.sln` registration of docs and commit-message trailers.

Where it lives: [`CLAUDE.md`](CLAUDE.md) (always-loaded rules) · [`.claude/rules/`](.claude/rules/) · [`.claude/agents/`](.claude/agents/) (orchestrator, implementor, verifier, reviewers) · [`.claude/commands/`](.claude/commands/) (`/sln-*` commands) · [`.claude/library/`](.claude/library/) (on-demand reference) · [`Docs/Management/LEDGER.md`](Docs/Management/LEDGER.md) (session ledger).

### Setting up Claude Code for this repo

Covered in [`Docs/DevEnv/devenv-setup.md` § 5](Docs/DevEnv/devenv-setup.md#5-claude-code-for-this-repo): env keys, the MCP servers in [`.mcp.json`](.mcp.json), the `superpowers` plugin, the committed skills, and a verification checklist. Read [`.claude/library/mcp-governance.md`](.claude/library/mcp-governance.md) before adding an MCP server.
