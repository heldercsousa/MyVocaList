# Orchestration Plan — App Settings Page

## Context

The Settings page for MyVocaList was built as part of the YouTube Karaoke URLs feature but never went through the SDD spec/plan cycle. Three concrete issues were found during exploration:

1. **Navigation disconnect**: The flyout menu "Preferences" item points to `PreferencesPage`, which is a stub ("under construction"). `SettingsPage` — the real, functional page — is only reachable via the "Add a YouTube API key in Settings" nudge inside `SongFormPage`. There is no way to reach it from the main navigation.
2. **Stale `HasYouTubeApiKey` flag**: `SongFormViewModel.HasYouTubeApiKey` is read from secure storage only when a song loads (`LoadKaraokeUrlsAsync`). If the user saves a key in Settings then navigates back, the search strip stays hidden until they reload the song.
3. **No spec exists**: The entire settings surface is undocumented in `Docs/Management/`.

This plan orchestrates: retroactive spec → spec review → fix plan → plan review → implementation → code review.

---

## Relevant Files

| File | Role |
|------|------|
| `MyVocaList/UI/Pages/Settings/SettingsPage.xaml` | Functional settings UI (YouTube API key) |
| `MyVocaList/UI/Pages/Settings/SettingsPage.xaml.cs` | Code-behind — calls `InitializeAsync` on `OnAppearing` |
| `MyVocaList/UI/ViewModels/SettingsViewModel.cs` | Save / Test / Clear / Mask logic |
| `MyVocaList/UI/Pages/Preferences/PreferencesPage.xaml` | Stub — "under construction" — wired to flyout menu |
| `MyVocaList/Navigation/NavigationConfig.cs` | Flyout menu groups; "Preferences" item → `Routes.Preferences` |
| `MyVocaList/Navigation/Routes.cs` | `Routes.Settings = "settings"` and `Routes.Preferences` both exist |
| `MyVocaList/AppShell.xaml` | `FlyoutItem Route="settings"` with SettingsPage already defined |
| `MyVocaList/UI/ViewModels/SongFormViewModel.cs` | `HasYouTubeApiKey` + `GoToSettingsCommand`; staleness issue here |
| `MyVocaList/MauiProgram.cs` | Both `SettingsPage` and `SettingsViewModel` already registered (Transient) |
| `Domain/ServicesInterfaces/ISecureStorageWrapper.cs` | `GetAsync`, `SetAsync`, `Remove` |
| `Domain/ServicesInterfaces/IYouTubeSearchService.cs` | `SearchAsync`, `ValidateApiKeyAsync` |
| `Services/YouTubeSearchService.cs` | Full implementation; reads `"youtube_api_key"` from secure storage |

---

## Orchestration Steps

### Wave 1 — Spec (subagent: Spec Writer)

**Input:** This plan + all files listed above.

**Skills to load:**
- `myvocalist-coding` — DevExpress patterns, CRUD pages, dialogs, UX
- `maui-current-apis` — MAUI 10 correctness gate
- `ux:interaction-design` — MD3 settings page interaction pattern
- `ux:visual-design` — MD3 typography and surface hierarchy for settings

**Task:** Write a retroactive spec at `Docs/Management/BusinessFeatures/app-settings/` with three files:
- `requirements.md` — user stories + acceptance criteria covering: view/enter API key, mask/unmask, test, save, clear, navigation access from flyout, stale-key refresh after returning from Settings
- `design.md` — architecture (SettingsPage replaces PreferencesPage stub; `HasYouTubeApiKey` refresh mechanism via `OnAppearing` re-read), interface signatures, MD3 settings surface layout
- `tasks.md` — ordered implementation tasks for the two fixes (navigation consolidation + staleness fix)

Register all three files in `MyVocaList.sln`.

---

### Wave 2 — Spec Review (fresh subagent: Spec Reviewer)

**Input:** `Docs/Management/BusinessFeatures/app-settings/` spec files + `.claude/agents/spec-reviewer.md`.

**Task:** Review the spec against MD3 settings patterns, SDD quality gates (correctness, completeness, consistency, testability), and project conventions. Fix any issues inline. Report a summary of findings.

**After this wave:** Spec is considered Helder-approved.

---

### Wave 3 — Plan (fresh subagent: Plan Writer)

**Input:** Approved spec + `superpowers:writing-plans` skill + `myvocalist-coding` skill.

**Task:** Write `Docs/Management/BusinessFeatures/app-settings/plan.md` with implementation tasks using the DRY Onion ordering. Two implementation tasks:

1. **Navigation consolidation** — redirect the "Preferences" flyout entry to `SettingsPage`. Options: (a) update `NavigationConfig.cs` to use `Routes.Settings`; (b) delete `PreferencesPage` stub. Plan subagent picks the cleanest approach.
2. **Stale `HasYouTubeApiKey` fix** — in `SongFormViewModel`, add a `RefreshYouTubeKeyAsync()` method and call it from `OnAppearing` (or via `WeakReferenceMessenger` if `OnAppearing` isn't accessible from the ViewModel).

Register `plan.md` in `MyVocaList.sln`.

---

### Wave 4 — Plan Review (fresh subagent: Plan Reviewer)

**Input:** `Docs/Management/BusinessFeatures/app-settings/plan.md` + `.claude/agents/plan-reviewer.md`.

**Task:** Review for task atomicity, DRY Onion order, sizing limits, file ownership conflicts, and spec alignment. Fix issues inline.

**After this wave:** Plan is considered Helder-approved. Update BACKLOG.md: App Settings → `🟡 In Progress`.

---

### Wave 5 — Implementation (1–2 subagents based on task independence)

**Input:** Approved plan + spec + `myvocalist-coding` skill + `maui-current-apis` skill.

Both implementation tasks are independent (different files), so they can run in parallel as one wave with two subagents:

**Subagent A — Navigation consolidation:**
- Files owned: `NavigationConfig.cs`, `PreferencesPage.xaml`, `PreferencesPage.xaml.cs`, `AppShell.xaml`, `MyVocaList.sln`
- Redirect the flyout "Preferences" → `SettingsPage`; delete or stub-replace `PreferencesPage`

**Subagent B — Stale key fix:**
- Files owned: `SongFormViewModel.cs`
- Add `RefreshYouTubeKeyAsync` and call it on page-appear lifecycle

Each subagent: build → test → post-edit re-read → .sln check → task-log → commit → push.

---

### Wave 6 — Code Review (fresh subagent: Code Reviewer)

**Input:** All changed files from Wave 5.

**Task:** Run `/project:review` checklist: constitutional constraints, DevExpress-first, SafeAreaEdges, no DisplayAlert, English-only, test coverage for any new service logic.

**On completion:** Update BACKLOG.md: App Settings → `✅ Done`.

---

## Verification

- `dotnet build` — 0 errors
- `dotnet test` — 0 failures  
- Manual smoke: open flyout → "Preferences" → lands on Settings page (YouTube API key field visible)
- Manual smoke: paste key → Save → back-navigate to SongFormPage → YouTube search strip visible without reloading song
- Manual smoke: Test key → valid feedback shown; clear key → nudge reappears
