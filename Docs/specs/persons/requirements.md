# Persons — Requirements

> **Status:** Spec approved — pending implementation
> **Last updated:** 2026-03-30

## Overview

Persons are the singers registered in the system. The admin registers them before or during a live karaoke event. A person has a full name (required) and optional birthday (DD/MM) and email for disambiguation. Participations and absences are tracked per person across events.

**UI language:** "Singer(s)" throughout all labels, titles, snackbars, and empty states.
**Code identifiers:** `Person`, `PersonService`, `IPersonRepository` — unchanged. `Person` is the domain entity; a future version may also represent admins or guests.

**Dedup strategy (v1):** Option A — non-blocking name suggestions. When the admin types a name, a live suggestion list shows existing persons whose name matches. The admin can select an existing person (goes to edit mode) or ignore suggestions and save a new person. No blocking.

**Upgrade path:** A future version will introduce singer self-registration via their own device (Option C). The data model is designed to accommodate this: `ExternalId` (GUID) supports device/account identity; `BirthdayDayMonth` + `Email` support the "either/or" disambiguation requirement; composite unique index on name+birthday is in place at DB level.

---

## User Stories

### US-1: Register a Singer

**As an** admin
**I want to** register a new singer with a name (and optionally birthday and email)
**So that** the singer can participate in karaoke events

#### Acceptance Criteria

- AC-1.1: When the user taps the FAB on the Persons list page, the app shall navigate to the New Singer form page.
- AC-1.2: The form shall show a `Full Name` field (required), a `Birthday` field (optional, DD/MM), an `Email` field (optional), a `Save` button, and a `Cancel` button.
- AC-1.3: When the user submits with an empty or whitespace-only name, the form shall show an inline error "Name is required" and shall not save.
- AC-1.4: When the user submits a name shorter than 2 characters, the form shall show "Name too short. Minimum 2 characters."
- AC-1.5: When the user submits a name with only one word (no last name), the form shall show "Enter first and last name."
- AC-1.6: When the user submits a last name shorter than 2 characters, the form shall show "Last name must have at least 2 characters."
- AC-1.7: When the user submits a name longer than 200 characters, the form shall show "Name too long. Maximum 200 characters."
- AC-1.8: When the birthday field is non-empty and does not match DD/MM format or has an invalid day/month value, the form shall show the appropriate validation error inline.
- AC-1.9: When the email field is non-empty and is not a valid email format, the form shall show "Invalid email" inline.
- AC-1.10: When the email field is non-empty and already belongs to another registered singer, the form shall show "Email already registered to another singer." inline.
- AC-1.11: When all fields are valid, the app shall save the singer, navigate back to the list, and show a success snackbar "{name} registered successfully!".
- AC-1.12: A character counter shall appear when the name length exceeds 180 characters, showing `current/200`. It turns warning color at 191+ and error color at 200.
- AC-1.13: Tapping `Cancel` shall navigate back without saving.

---

### US-2: Name Suggestions (Duplicate Detection)

**As an** admin
**I want to** see existing singers with similar names while typing
**So that** I can avoid accidentally registering a duplicate

#### Acceptance Criteria

- AC-2.1: While the user types in the Full Name field (≥ 2 characters), the form shall show a suggestion list of up to 5 existing singers whose normalized name matches the search term.
- AC-2.2: Each suggestion row shall show the singer's `FullName` as headline and their `GetDisplayIdentifier()` result as supporting text (email, birthday, or ID — in that priority).
- AC-2.3: When the user taps a suggestion, the app shall navigate to the Edit Singer form pre-populated with that singer's data.
- AC-2.4: When the suggestion list is visible and the field is cleared below 2 chars, the suggestion list shall be hidden.
- AC-2.5: The suggestion list shall not block save — the admin can ignore all suggestions and proceed.
- AC-2.6: When no suggestions match, the suggestion list shall be hidden.

---

### US-3: Browse and Search Singers

**As an** admin
**I want to** see all registered singers in a scrollable list and search by name or email
**So that** I can quickly find and manage a specific singer

#### Acceptance Criteria

- AC-3.1: When the page opens, the app shall load the first page of singers (20 items) sorted by `FullName` ascending.
- AC-3.2: The list shall show a shimmer skeleton while the first page is loading.
- AC-3.3: While the list is empty and no search is active, the app shall show a "No singer registered" empty state centered on screen.
- AC-3.4: When the user taps the search icon in the app bar, the `SmallAppBar` shall be replaced by the `SearchAppBar`.
- AC-3.5: While the `SearchAppBar` is active, the app shall debounce input by 400ms and reload the list on each change.
- AC-3.6: The search shall match against both `FullNameNormalized` and `Email` (case-insensitive).
- AC-3.7: When a search returns no results, the app shall show a "No singer found" empty state.
- AC-3.8: When the user taps the back arrow in the `SearchAppBar`, the search shall be cleared and the `SmallAppBar` shall be restored.
- AC-3.9: When the user scrolls down, the app bar shall show an elevated state (surface tint).
- AC-3.10: When the list reaches the last item, the app shall automatically load the next page (load-more with spinner).
- AC-3.11: The user shall be able to pull-to-refresh to reload from the first page.
- AC-3.12: Each list row shall show the singer's `FullName` as headline and their participation/absence counts as supporting text (e.g. "Participations: 5 / Absences: 1").
- AC-3.13: Each list row shall have a leading avatar icon and a trailing checkbox that reflects selection state.

---

### US-4: Edit a Singer

**As an** admin
**I want to** edit a singer's name, birthday, or email
**So that** their profile stays accurate over time

#### Acceptance Criteria

- AC-4.1: When exactly one singer is selected, the FloatingToolbar Edit button shall be active (highlighted).
- AC-4.2: When the user taps Edit with exactly one singer selected, the app shall clear the selection and navigate to the Edit Singer form pre-populated with the current name, birthday, and email.
- AC-4.3: The Edit Singer form shall apply the same name validation rules as AC-1.3–1.7.
- AC-4.4: Birthday and email fields shall apply the same validation as AC-1.8–1.10.
- AC-4.5: The email uniqueness check (AC-1.10) shall exclude the singer being edited.
- AC-4.6: On successful save, the app shall navigate back, reload the list, and show "{name} updated successfully!".
- AC-4.7: On failure, the form shall show the error inline — no navigation.
- AC-4.8: The suggestion list (US-2) shall also be active on the edit form.

---

### US-5: Delete Singers

**As an** admin
**I want to** delete one or more singers
**So that** incorrectly registered or duplicate persons are removed

#### Acceptance Criteria

- AC-5.1: When one or more singers are selected, the FloatingToolbar Delete button shall be active.
- AC-5.2: When the user taps Delete, the app shall show a confirmation BottomSheet with the message "Delete N singer(s)?" and a red "Delete" action button.
- AC-5.3: The hardware Back button shall dismiss the confirmation sheet.
- AC-5.4: When all selected singers are deleted successfully, the snackbar shall read "N singer(s) successfully removed!".
- AC-5.5: After deletion, the selection shall be cleared and the list shall reload.

---

### US-6: Select Singers (always-on selection)

**As an** admin
**I want to** select singers by tapping rows
**So that** I can batch-edit or batch-delete

#### Acceptance Criteria

- AC-6.1: Selection is always active (`SelectionMode.Multiple` hardcoded in XAML — no mode toggle).
- AC-6.2: Tapping a row shall toggle its selection state natively via `DXCollectionView`.
- AC-6.3: The app bar title shall show "Singers" when nothing is selected and "N selected" when N ≥ 1.
- AC-6.4: The FloatingToolbar `Select All` button shall select all loaded items when not all are selected, and deselect all when all are selected.
- AC-6.5: Selection state shall be preserved across list refreshes (restored by matching IDs).

---

## Data Model

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `Id` | `int` | PK, auto-increment | EF Core generated |
| `ExternalId` | `Guid?` | nullable, unique index | Reserved for future self-registration/device identity |
| `FullName` | `string` | NOT NULL, maxLen=250 (DB) / 200 (input) | Trimmed before save |
| `FullNameNormalized` | `string` | NOT NULL, maxLen=250 | Set by `ITextNormalizationService`; indexed |
| `BirthdayDayMonth` | `string` | nullable, format DD/MM | For disambiguation; especially for non-tech/elder users |
| `Email` | `string` | nullable, maxLen=100, unique index | For disambiguation and future marketing/auth |
| `Participations` | `int` | NOT NULL, default 0 | Managed by event/queue logic |
| `Absences` | `int` | NOT NULL, default 0 | Managed by event/queue logic |

**Database indexes:**

| Index | Fields | Type | Condition |
|-------|--------|------|-----------|
| `IX_Persons_FullNameNormalized` | `FullNameNormalized` | Standard | — |
| `IX_Persons_Email` | `Email` | Unique | Nullable (multiple NULLs allowed) |
| `IX_Persons_ExternalId` | `ExternalId` | Unique | Nullable |
| `IX_Persons_Name_Birthday` | `FullNameNormalized, BirthdayDayMonth` | Unique | WHERE BirthdayDayMonth IS NOT NULL |

**Note on composite index:** The `Name+Birthday` filtered unique index prevents two persons with the same name AND same birthday. Same name + no birthday creates a second record (admin manages). This is the DB-level foundation for future Option C without blocking v1 behavior.

**Note on birthday:** `BirthdayDayMonth` stores only DD/MM — intentional. Year is not captured; the field exists solely for disambiguation and serves non-tech/elder users who may not have an email. If age-based sorting is ever needed, a separate `BirthYear` field should be added.

**Future "either/or" consideration (Option C):** A future version may require that at least one of `Email` or `BirthdayDayMonth` is provided. This is a service-level validation change only — the data model already supports it.

---

## Validation Rules

| Field | Rule | Error message |
|-------|------|---------------|
| FullName | required | "Name is required" |
| FullName | minLen = 2 | "Name too short. Minimum 2 characters." |
| FullName | requires 2+ words | "Enter first and last name." |
| FullName | last name minLen = 2 | "Last name must have at least 2 characters." |
| FullName | maxLen = 200 (input) | "Name too long. Maximum 200 characters." |
| BirthdayDayMonth | optional; if set, DD/MM format | "Use DD/MM format (e.g.: 15/03)" |
| BirthdayDayMonth | valid day (1–31) | "Day must be between 1 and 31" |
| BirthdayDayMonth | valid month (1–12) | "Month must be between 1 and 12" |
| BirthdayDayMonth | valid day for month | "Invalid date for this month" |
| Email | optional; if set, valid format | "Invalid email" |
| Email | optional; if set, maxLen = 100 | "Email too long" |
| Email | optional; if set, unique across persons (excluding self on edit) | "Email already registered to another singer." |

**Character counter thresholds (name field):**
| Length | State |
|--------|-------|
| ≤ 180 | Hidden |
| 181–190 | Visible, neutral color |
| 191–199 | Visible, warning color |
| 200 | Visible, error color |

---

## Out of Scope

- Merging duplicate persons — deferred
- Singer self-registration via their own device — planned future version
- Composite key enforcement at service level (name + birthday) — deferred to Option C
- Photo / avatar upload
- Participation/absence counters modified from this page — managed by event/queue logic
- Soft delete / archive
- Multiple emails per person — single nullable unique Email covers v1 needs
- Year of birth / age sorting — `BirthdayDayMonth` is DD/MM only; add `BirthYear` separately if needed
