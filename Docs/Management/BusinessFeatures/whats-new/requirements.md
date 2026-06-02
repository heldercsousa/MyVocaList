# What's New / Release Notes — Requirements
**Date:** 2026-05-30  
**Status:** Draft — pending Helder review

**Dependency:** App Versioning Strategy must be implemented first (`Docs/Management/DevCycleCraft/app-versioning/`). This feature relies on `AppInfo.VersionString` being driven by MinVer git tags.

---

## Problem Statement

When a new app version is installed, users have no way to discover what changed. Release notes exist in the dev changelog but are never surfaced in the app. This reduces perceived quality and leaves users unaware of fixes and new capabilities.

---

## Domain Vocabulary

| Term | Definition |
|------|-----------|
| **Release entry** | A single version's worth of notes: highlights + fixes |
| **`releases.json`** | Bundled MauiAsset file containing all release entries |
| **What's New modal** | The `dx:BottomSheet` displayed once per version upgrade |
| **Last-seen version** | The app version stored in `Preferences` after the user dismisses the modal |
| **Fresh install** | First ever launch — `Preferences` has no last-seen version value |
| **Version upgrade** | App launch where `AppInfo.VersionString` differs from last-seen version |

---

## User Stories

### US-WN-01 — Discover what changed after an update
**As** a user who has just updated the app,  
**I want** to see a concise summary of what is new in this version,  
**so that** I know which features to try and which bugs have been fixed.

### US-WN-02 — Modal does not reappear
**As** a user,  
**I want** the What's New modal to appear only once per version,  
**so that** it does not interrupt my workflow on subsequent launches.

### US-WN-03 — No modal on fresh install
**As** a first-time user,  
**I want** to go directly to the app without seeing a changelog,  
**so that** I am not confused by release notes before I have used the app.  
*(Rationale: industry research and UX practice confirm that showing a changelog before any usage is disorienting. The user has no baseline to compare against.)*

### US-WN-04 — Works offline
**As** a user in an area with no connectivity,  
**I want** the What's New modal to appear even without internet access,  
**so that** the feature never blocks app startup.

---

## Acceptance Criteria

### AC-WN-01 — Modal shown once per upgrade
**Given** the app is launched after an update (current version ≠ last-seen version),  
**Then** the What's New modal is displayed on that launch only.  
**And** re-launching the app does not display the modal again for the same version.

### AC-WN-02 — Modal hidden on fresh install
**Given** the app is launched for the first time (no last-seen version in Preferences),  
**Then** no modal is displayed.  
**And** the current version is stored as last-seen so subsequent launches also skip the modal.

### AC-WN-03 — Modal hidden when no entry exists
**Given** `releases.json` does not contain an entry for the current version,  
**Then** no modal is displayed (version is still stored as last-seen).

### AC-WN-04 — Modal content is correct
**Given** the modal is shown,  
**Then** it displays the current version string, the release date, all highlights, and all fix descriptions from the matching `releases.json` entry.

### AC-WN-05 — Dismiss persists version
**Given** the modal is displayed,  
**When** the user taps "Got it" or dismisses via swipe,  
**Then** `Preferences.Set("last_seen_version", currentVersion)` is called and the modal does not reappear.

### AC-WN-06 — No network call
**Given** the app is offline,  
**Then** the What's New check and modal display function identically to the online case.

---

## Validation Rules

- `releases.json` must be valid JSON; if malformed, the modal is silently skipped (no crash).
- Version comparison is exact string match (`AppInfo.VersionString` vs stored value).
- If `releases.json` is missing entirely from the bundle, the modal is silently skipped.

---

## `releases.json` Schema

```json
[
  {
    "version": "0.2.0",
    "date": "2026-06-01",
    "highlights": [
      "Queue Management with round-based progression",
      "Singer registration and absence tracking"
    ],
    "fixes": [
      "Fixed crash on empty venue list"
    ]
  }
]
```

- `version`: exact string matching `AppInfo.VersionString` (e.g. `"0.2.0-alpha.3"`)
- `date`: ISO 8601 date string (display only)
- `highlights`: list of new feature one-liners (may be empty array)
- `fixes`: list of fix one-liners (may be empty array)

Content for each release is sourced from `Docs/Changelog/changelog.md` at release time and added to `releases.json` as part of the `/project:release` command workflow.

---

## Out of Scope

- Full in-app changelog history / "View all releases" screen (future)
- Fetching release notes from a remote URL
- Push notification announcing a new release
- Localization of release notes (English only at MVP)
- Rich text / images in release notes
