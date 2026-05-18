# MyVocaList — Product Backlog

> **SCRUM product backlog.** Items are priority-ordered — top = most important. Every feature begins here as a brief idea and is promoted through the lifecycle below before any code is written.
>
> **When to read:** At the start of any new feature cycle (workflow.md Rule 1 step 0) and when resuming a session with no active handoff file (Rule 7).
>
> **Who updates statuses:** The main agent updates this file at each workflow milestone. Subagents do not touch BACKLOG.md.
>
> **MVP scope:** All items above the `── MVP scope ends here ──` marker are in-scope for the MVP release. Items below are post-MVP.

---

## Status reference

| Status | Meaning |
|--------|---------|
| `💡 Idea` | Captured, not yet evaluated |
| `📋 Spec` | Approved — spec being written |
| `🗺️ Plan` | Spec approved — plan being written |
| `🟢 Ready` | Plan approved — dispatchable |
| `🟡 In Progress` | Implementation underway |
| `🔵 Deferred` | Paused — reason documented |
| `🔴 Blocked` | Cannot proceed — dependency named |
| `✅ Done` | Shipped — changelog updated |

---

## 🟡 In Progress

- **Artists & Songs Catalog** `🟡 In Progress` — artist and song management with search; feeds singer song selection
  → Spec: `Docs/specs/artists-songs/` · Plan: `Docs/superpowers/plans/2026-04-23-artists-songs-catalog.md`

---

## 🟢 Ready

*(none — Artists & Songs must complete first)*

---

## 📋 Spec

- **YouTube Karaoke Mode** `📋 Spec` — YouTube URL management per song, in-app search (optional API key), next-singer 2-stage alert (notifications + Android blinking overlay), play-count-based URL suggestion
  → Spec: `Docs/specs/youtube-karaoke/` · Brainstorm: 2026-05-17

---

## 💡 Backlog

- **Queue management MVP** `💡 Idea` — core product: active queue, round-based progression, singer registration, absence tracking, completion time estimate
- **Visual Theme Refresh** `💡 Idea` — app UI is too dark and monochromatic; spike to introduce richer accent colors, gradient surfaces, warmer tones, and more contrast between states

---
> ── MVP scope ends here ──
---

- **Singer self-registration** `💡 Idea` — singers register via public link
- **Song catalog** `💡 Idea` — song library with lyrics API integration
- **Social features** `💡 Idea` — post-event sharing, singer stats

---

## 🔵 Deferred — Infrastructure

### Day-to-day task management workflow review
The session-start protocol in `workflow.md` referenced an SDD-specific plan file (MASTER_PLAN.md, now complete). The guidance for how the project manages backlog + nested tasks (BACKLOG.md → plan → task-log → spec) is scattered and incomplete. Systematic review needed.
→ Scope: update workflow.md Rule 7 session start; clarify BACKLOG.md role; remove stale MASTER_PLAN reference
→ Process: spec → spec review → plan → plan review → Helder approval → apply

### CLAUDE.md Deep Restructure
CLAUDE.md has grown beyond its constitutional purpose. Candidate removals: evaluation notes (Tessl, sdd-mcp, Cursor, Playwright, GitHub MCP), full MCP governance rules, detailed rule amendment protocol, SDD applicability rationale.
→ Process: spec → spec review → plan → plan review → Helder approval → apply

### workflow.md Reduction
Agent role files (`implementor.md`, `verifier.md`) should absorb responsibilities currently scattered inline in `workflow.md`, reducing its line count and improving readability.
→ Reference: `memory/project_workflow_cleanup.md`
→ Process: spec → spec review → plan → plan review → Helder approval → apply

### App Versioning Strategy
Establish a formal, automated version scheme for the app build cycle. Currently hardcoded (`ApplicationDisplayVersion=1.0.0`, `ApplicationVersion=1`) with no tooling enforcing consistency.

**Target pattern:** `MAJOR.MINOR.BUILD` — e.g. `0.1.42`
- `MAJOR` (AA): release milestone — currently `0` (pre-release). Bumped manually on milestone ship.
- `MINOR` (BBB): stable feature count — bumped per merged feature (conventional commits + git tag trigger).
- `BUILD` (CCC): monotonically increasing integer — derived from commit height since last tag; maps to Android `versionCode` / iOS `CFBundleVersion`.

**MAUI property mapping (set in `.csproj`):**
```xml
<ApplicationDisplayVersion>0.MINOR.BUILD</ApplicationDisplayVersion>  <!-- → versionName / CFBundleShortVersionString -->
<ApplicationVersion>BUILD</ApplicationVersion>                         <!-- → versionCode / CFBundleVersion — must be integer -->
```

**Recommended tooling (research complete — ranked):**

1. **MinVer** *(recommended)* — NuGet package, zero config, git-tag-driven. Sets MSBuild properties `$(MinVerVersion)`, `$(MinVerMajor)`, `$(MinVerMinor)`, `$(MinVerPatch)`, `$(MinVerBuildMetadata)`. Bind directly to `ApplicationDisplayVersion` and `ApplicationVersion` in a `<PropertyGroup>` target. Compatible with the conventional-commits workflow already in use. Lightweight, no YAML config required. Tag `v0.1.0` → version `0.1.0`; each additional commit adds `.{height}` pre-release suffix automatically.

2. **GitVersion** *(alternative for complex branching)* — enterprise-grade, configurable via `.gitversion.yml`. Heavier, but supports release channels (alpha/beta/rc) mapped to git branches. Worth evaluating if the release process ever involves feature branches with independent versioning.

3. **Nerdbank.GitVersioning (nbgv)** *(skip for now)* — requires `version.json` per-project; adds MSBuild complexity without meaningful benefit over MinVer for a single-app repo.

**Skills/MCP that touch this domain:**
- `changelog` skill — generates `CHANGELOG.md` from conventional commits; pair with MinVer tags for release notes
- `commit` skill — enforces conventional commit format that feeds SemVer bump decisions
- No dedicated versioning MCP server in the current stack; MinVer + skills covers the workflow without one

**Open decisions before spec:**
- What triggers a `MINOR` bump — every feature tag, or only milestone-shipped features?
- CI/CD pipeline: is there one, and should it validate that `versionCode` never decreases between builds?
- Should pre-release builds carry a suffix (e.g. `0.2.0-alpha.3`)? Yes/No affects how MinVer floor is configured.

→ Process: Helder decides open questions above → spec → plan → apply

---

## ✅ Recently Done

| Feature | Completed | Plan |
|---------|-----------|------|
| SDD Master Plan (Phases 1–11, 162 steps) | 2026-05-07 | `Docs/DevEnv/SDD/plans/impl/MASTER_PLAN.md` |
| Hooks redesign | 2026-05-03 | *(infrastructure only)* |
| Artists & Songs — Domain + TDD RED | 2026-04 | `Docs/superpowers/plans/2026-04-23-artists-songs-catalog.md` |
| Autocomplete field | 2026-04 | `Docs/superpowers/plans/2026-04-06-autocomplete-field.md` |
| Person CRUD | 2026-04 | `Docs/superpowers/plans/2026-04-07-person-crud.md` |
| Toolbar/FAB vibrant | 2026-04 | `Docs/superpowers/plans/2026-04-02-toolbar-fab-vibrant.md` |
| Styles & Structure | 2026-03 | `Docs/superpowers/plans/2026-03-31-styles-structure.md` |
| Venues MD3 rebuild | 2026-03 | `Docs/superpowers/plans/2026-03-29-venues-md3-rebuild.md` |
| M3 Lists | 2026-03 | `Docs/superpowers/plans/2026-03-11-m3-lists.md` |
