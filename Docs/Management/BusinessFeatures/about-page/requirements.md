# Requirements — About Page

## Overview

A dedicated Shell page accessible from the System section of the flyout menu. Provides permanent, on-demand access to app identity information, licensing terms, and the current release notes — complementing the once-per-upgrade What's New modal.

---

## Domain Vocabulary

| Term | Definition |
|---|---|
| About page | A read-only Shell page exposing app identity, licensing, and release notes |
| Founded year | The year the app was first released; hardcoded constant (`AppConstants.FoundedYear = 2025`) |
| Distribution model | The licensing terms under which the app is made available |
| Current release notes | The `ReleaseEntry` for the current app version, sourced from `releases.json` via `IWhatsNewService` |

---

## User Stories

| ID | Story |
|---|---|
| US-AB-01 | As a venue administrator, I want to see the app version at a glance so that I can report it when requesting support. |
| US-AB-02 | As a venue operator, I want to know the app's licensing terms so that I can confirm it is appropriate for my use case. |
| US-AB-03 | As a user who dismissed the What's New modal, I want to re-read the current release notes at any time so that I don't have to wait for the next update. |
| US-AB-04 | As a new administrator handed the device, I want to see a single sentence describing the app's purpose so that I immediately understand its intent. |

---

## Acceptance Criteria

### AC-AB-01 — Version in AppBar header
**Given** I open the About page,  
**Then** the AppBar title area displays the current version string (e.g., `v1.0.0`) sourced from `AppInfo.VersionString`.

### AC-AB-02 — App logo and title
**Given** I open the About page,  
**Then** the page body shows the app logo image and the app name "MyVocaList".

### AC-AB-03 — One-sentence app goal
**Given** I open the About page,  
**Then** a single subtitle-level sentence describes the app purpose (e.g., "Karaoke queue management for live events"), without a dedicated section heading.

### AC-AB-04 — Since year
**Given** I open the About page,  
**Then** the text "Since 2025" is displayed directly below the app name.

### AC-AB-05 — License section
**Given** I open the About page,  
**Then** a "License" section displays:
- License name: "CC BY-NC-ND 4.0"
- One-line summary: "Free for personal and non-commercial use. No derivatives."
- Copyright line: "© 2025 Helder Sousa"

### AC-AB-06 — Current release notes (happy path)
**Given** the current version has an entry in `releases.json`,  
**When** I open the About page,  
**Then** a "What's New" section shows the version string, release date, highlights list, and fixes list for the current version, sourced via `IWhatsNewService`.

### AC-AB-07 — Current release notes (no entry)
**Given** the current version has no matching entry in `releases.json`,  
**When** I open the About page,  
**Then** the "What's New" section is hidden entirely — no placeholder or error message is shown.

### AC-AB-08 — Navigation entry
**Given** I open the flyout menu,  
**Then** "About" appears as the last item in the System group, after "Backup & Restore" and before "Exit".

### AC-AB-09 — No network dependency
**Given** the device has no network connection,  
**When** I open the About page,  
**Then** all content loads successfully (no spinners, no error states — all data is local).

---

## Out of Scope

- Features list (redundant for active users)
- Third-party library attribution (future, if legally required)
- Privacy policy link (future)
- Support / contact link (future)
- Version history / full changelog list (covered by the future "View all releases" enhancement in the What's New spec)
- Localization of license text (English only per Constitutional Constraints)
