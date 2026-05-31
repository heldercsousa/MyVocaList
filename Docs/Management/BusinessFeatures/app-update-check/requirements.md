# App Update Check — Requirements
**Date:** 2026-05-31  
**Status:** Draft — pending Helder review

**Dependency:** App Versioning Strategy must be implemented (`Docs/Management/DevCycleCraft/app-versioning/`) ✅ Done. This feature relies on `AppInfo.VersionString` being driven by MinVer git tags.

---

## Problem Statement

The app has no mechanism to inform users that a newer version is available or to block critically outdated versions from running. Users may unknowingly operate on an old build, missing bug fixes or behaviorally incompatible versions at live events.

---

## Domain Vocabulary

| Term | Definition |
|------|-----------|
| **Version manifest** | A `version-manifest.json` file hosted on GitHub (raw URL) containing the latest and minimum required version |
| **Soft nudge** | A dismissible bottom sheet informing the user an update is available |
| **Hard block** | A non-dismissible bottom sheet requiring the user to update before continuing |
| **`latestVersion`** | The most recent published version string in the manifest |
| **`minRequiredVersion`** | The oldest version string still permitted to run the app |
| **Fail-open** | When the manifest cannot be fetched, the app proceeds normally (no block) |

---

## User Stories

### US-UC-01 — Discover that an update is available
**As** a user running an older version of the app,  
**I want** to see a notification that a newer version is available,  
**so that** I can choose to update at a convenient time.

### US-UC-02 — Forced update on critically outdated version
**As** an admin using a version no longer supported,  
**I want** to be told clearly that my version is incompatible and I must update,  
**so that** I understand why the app is blocked and know what to do.

### US-UC-03 — Go to the store directly from the prompt
**As** a user seeing an update prompt,  
**I want** a direct "Update Now" button that opens the app's store listing,  
**so that** I can update without hunting for the app manually.

### US-UC-04 — App continues normally when offline
**As** a user at a live event without reliable internet,  
**I want** the version check to fail silently if the manifest cannot be fetched,  
**so that** a connectivity issue never blocks the app at an event.

### US-UC-05 — Soft nudge is dismissible
**As** a user who knows about the update but wants to finish what they are doing,  
**I want** to dismiss the update prompt and continue using the app,  
**so that** the update is my choice, not forced on minor releases.

---

## Acceptance Criteria

### AC-UC-01 — Soft nudge shown when update available
**Given** the manifest fetch succeeds and `currentVersion < latestVersion` but `currentVersion >= minRequiredVersion`,  
**Then** the `UpdateAvailableBottomSheet` is shown (dismissible).  
**And** tapping "Later" closes the sheet and allows normal app use.  
**And** tapping "Update Now" opens the platform-appropriate store URL and closes the sheet.

### AC-UC-02 — Hard block shown when version below minimum
**Given** the manifest fetch succeeds and `currentVersion < minRequiredVersion`,  
**Then** the `UpdateRequiredBottomSheet` is shown (non-dismissible, `IsCancelable="False"`).  
**And** the only available action is "Update Now" which opens the store URL.  
**And** the user cannot navigate past the sheet or use any other app functionality.

### AC-UC-03 — App proceeds when up to date
**Given** the manifest fetch succeeds and `currentVersion >= latestVersion`,  
**Then** no sheet is shown and the app loads normally.

### AC-UC-04 — Fail-open on network error
**Given** the manifest URL is unreachable or the fetch times out (5-second timeout),  
**Then** no sheet is shown and the app loads normally.  
**And** the failure is logged at Warning level but never surfaced to the user.

### AC-UC-05 — Correct store URL opened per platform
**Given** the user taps "Update Now" on Android,  
**Then** the Play Store URL from the manifest is opened via `Launcher.OpenAsync`.  
**Given** the user taps "Update Now" on iOS,  
**Then** the App Store URL from the manifest is opened via `Launcher.OpenAsync`.

### AC-UC-06 — Version comparison is SemVer-aware
**Given** version strings follow SemVer (e.g. `"0.3.0-alpha.5"`),  
**Then** comparison uses `System.Version` or `NuGet.Versioning.NuGetVersion` semantics — not lexicographic string comparison.

---

## Validation Rules

- If `version-manifest.json` is malformed JSON, treat as fetch failure (fail-open).
- If `storeUrls` is missing the current platform's key, log a warning and skip opening the store (do not crash).
- Version comparison must handle pre-release labels (`-alpha.N`) correctly: `0.3.0-alpha.5 < 0.3.0`.

---

## Out of Scope

- In-app release notes within the update prompts (see What's New feature)
- Automatic silent background update download
- Push notification announcing a new version
- Version check on every screen (startup-only)
- Per-user or per-role minimum version rules
- Localization of prompt text (English only at MVP)
