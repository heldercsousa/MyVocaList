# Plan: App Update Check Feature

## Context

The app has no mechanism to inform users that a newer version exists or to block critically outdated versions from running. Three existing specs (What's New, Crash Reporting, User Suggestions) all reference `AppInfo.VersionString` (powered by the completed App Versioning strategy), making them natural companions to a version-check feature. Distribution target: Play Store + App Store. Enforcement: two-tier (soft nudge for minor updates, hard block for critically old versions).

## Feature Scope

**Name:** App Update Check  
**Spec folder:** `Docs/Management/BusinessFeatures/app-update-check/`  
**BACKLOG target:** 2026-06, Business Features table

---

## Architecture

### 1. Version Manifest (hosted artifact)

A `version-manifest.json` file committed to the repo root and accessed via GitHub raw URL:

```
https://raw.githubusercontent.com/heldercsousa/MyVocaList/main/version-manifest.json
```

Schema:
```json
{
  "latestVersion": "0.3.0",
  "minRequiredVersion": "0.2.0",
  "storeUrls": {
    "android": "https://play.google.com/store/apps/details?id=com.myvocalist",
    "ios": "https://apps.apple.com/app/myvocalist/idXXXXXXX"
  },
  "updateMessage": "This version is no longer supported. Please update to continue."
}
```

Updated by the main agent as part of `/project:release`.

### 2. Domain / Contracts

- `VersionManifest` record (Contracts.DTOs)
- `UpdateCheckResult` record: `{ IsUpToDate, IsUpdateAvailable, IsUpdateRequired, StoreUrl, LatestVersion }`
- `IVersionCheckService` interface (Domain.Interfaces)

### 3. Service (Services project)

`VersionCheckService : IVersionCheckService`
- Fetches manifest via `HttpClient` with 5-second timeout
- Compares `AppInfo.VersionString` (SemVer) against `latestVersion` and `minRequiredVersion`
- Returns `UpdateCheckResult`
- **On network failure → returns `IsUpToDate = true`** (fail-open: never block at a live event due to network)
- Platform detection: `DeviceInfo.Platform` → selects `android` or `ios` store URL

### 4. UI (MAUI project)

**Trigger:** `AppShellViewModel.InitializeAsync()` calls `IVersionCheckService.CheckForUpdatesAsync()` and raises a `WeakReferenceMessenger` message (same pattern as What's New).

**Soft nudge (IsUpdateAvailable):** `UpdateAvailableBottomSheet` — dx:BottomSheet, dismissible, two buttons: "Update Now" (opens store URL) + "Later" (dismisses).

**Hard block (IsUpdateRequired):** `UpdateRequiredBottomSheet` — dx:BottomSheet, `IsCancelable="False"`, single button: "Update Now" (opens store URL). No dismiss path.

**Opening store URL:** `await Launcher.OpenAsync(storeUrl)` — MAUI built-in, no extra dependency.

### 5. Release workflow integration

`/project:release` command gains a prompt step: "Update version-manifest.json?" → auto-bumps `latestVersion` (always) and asks whether to raise `minRequiredVersion`.

---

## Files to Create

| File | Purpose |
|------|---------|
| `version-manifest.json` (repo root) | Hosted manifest |
| `Docs/Management/BusinessFeatures/app-update-check/requirements.md` | ACs, user stories |
| `Docs/Management/BusinessFeatures/app-update-check/design.md` | Architecture, interfaces, interaction flows |
| `MyVocaList.Contracts/DTOs/VersionManifest.cs` | Manifest DTO |
| `MyVocaList.Contracts/DTOs/UpdateCheckResult.cs` | Result DTO |
| `MyVocaList.Domain/Interfaces/IVersionCheckService.cs` | Interface |
| `MyVocaList.Services/VersionCheckService.cs` | Implementation |
| `MyVocaList/UI/Pages/UpdateAvailableBottomSheet.xaml[.cs]` | Soft nudge UI |
| `MyVocaList/UI/Pages/UpdateRequiredBottomSheet.xaml[.cs]` | Hard block UI |

---

## Spec Writing Action Plan (post-plan-mode)

1. Write `requirements.md` and `design.md` to `Docs/Management/BusinessFeatures/app-update-check/`
2. Add BACKLOG.md row (Business Features table, 2026-06, `📋 Spec`)
3. Register both spec files in `MyVocaList.sln` (constraints-registry rule)
4. Commit with `amend:` prefix? No — this is a new spec, use conventional commit `docs: add App Update Check spec`
