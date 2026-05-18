# Venues — Requirements

> **Status:** Implemented (reference spec — canonical pattern for all future CRUD features)
> **Last updated:** 2026-03-29

## Overview

Venues are physical locations where karaoke events take place. The admin registers venues once; they are then selectable when creating events. A venue can have zero or more events. This is the first fully MD3-compliant CRUD feature and serves as the reference implementation for all future list/form page pairs.

---

## User Stories

### US-1: Register a Venue

**As an** admin
**I want to** register a new venue with a name
**So that** it is available when scheduling karaoke events

#### Acceptance Criteria

- AC-1.1: When the user taps the FAB on the Venues list page, the app shall navigate to the New Venue form page.
- AC-1.2: The form shall show a single `Name` field, a `Save` button, and a `Cancel` button.
- AC-1.3: When the user submits with an empty or whitespace-only name, the form shall show an inline error "Venue name is required" and shall not save.
- AC-1.4: When the user submits a name shorter than 2 characters, the form shall show "Name is too short. Minimum is 2 characters."
- AC-1.5: When the user submits a name longer than 30 characters, the form shall show "Name is too long. Maximum 30 characters."
- AC-1.6: When the user submits a name that already exists (case-insensitive), the form shall show "There is another venue registered with this name."
- AC-1.7: When the name is valid and unique, the app shall save the venue, navigate back to the list, and show a success snackbar "Venue '{name}' successfully created!".
- AC-1.8: A character counter shall appear when the name length exceeds 25 characters, showing `current/30`. It turns warning color at 28+ and error color at 30.
- AC-1.9: Tapping `Cancel` shall navigate back without saving.

---

### US-2: Browse and Search Venues

**As an** admin
**I want to** see all registered venues in a scrollable list and search by name
**So that** I can quickly find and manage a specific venue

#### Acceptance Criteria

- AC-2.1: When the page opens, the app shall load the first page of venues (20 items) from the database.
- AC-2.2: The list shall show a shimmer skeleton while the first page is loading.
- AC-2.3: While the list is empty and no search is active, the app shall show an "No venue registered" empty state centered on screen.
- AC-2.4: When the user taps the search icon in the app bar, the `SmallAppBar` shall be replaced by the `SearchAppBar`.
- AC-2.5: While the `SearchAppBar` is active, the app shall debounce input by 400ms and reload the list on each change.
- AC-2.6: When a search returns no results, the app shall show a "No venue found" empty state.
- AC-2.7: When the user taps the back arrow in the `SearchAppBar`, the search shall be cleared and the `SmallAppBar` shall be restored.
- AC-2.8: When the user scrolls down, the app bar shall show an elevated state (surface tint).
- AC-2.9: When the list reaches the last item, the app shall automatically load the next page (load-more with spinner).
- AC-2.10: The user shall be able to pull-to-refresh to reload from the first page.
- AC-2.11: Each list row shall show the venue name and a leading icon.
- AC-2.12: Each list row shall have a trailing checkbox that reflects whether the row is selected.

---

### US-3: Edit a Venue

**As an** admin
**I want to** rename a venue
**So that** the name reflects a change at the physical location

#### Acceptance Criteria

- AC-3.1: When exactly one venue is selected, the FloatingToolbar Edit button shall be active (highlighted).
- AC-3.2: When the user taps the Edit button with exactly one venue selected, the app shall clear the selection and navigate to the Edit Venue form pre-populated with the current name.
- AC-3.3: The Edit Venue form shall apply the same validation rules as AC-1.3–1.6.
- AC-3.4: AC-1.6's uniqueness check shall exclude the venue being edited (a venue can be saved with its own current name).
- AC-3.5: On successful save, the app shall navigate back, reload the list, and show "Venue name successfully updated to '{name}'!".
- AC-3.6: On failure, the form shall show the error inline — no navigation.

---

### US-4: Delete Venues

**As an** admin
**I want to** delete one or more venues
**So that** unused or incorrect registrations are removed from the system

#### Acceptance Criteria

- AC-4.1: When one or more venues are selected, the FloatingToolbar Delete button shall be active.
- AC-4.2: When the user taps Delete, the app shall show a confirmation BottomSheet with the message "Delete N venue(s)?" and a red "Delete" action button.
- AC-4.3: The hardware Back button shall dismiss the confirmation sheet.
- AC-4.4: A venue that has registered events shall not be deleted. It shall be skipped with an explanation in the snackbar.
- AC-4.5: When all selected venues are deleted successfully, the snackbar shall read "N venue(s) successfully removed!".
- AC-4.6: When some venues are blocked (have events), the snackbar shall read "X of Y successfully removed. Z venue(s) couldn't be removed (have events)."
- AC-4.7: When all selected venues are blocked, the snackbar shall show an error "The venue(s) couldn't be removed (have events)."
- AC-4.8: After deletion, the selection shall be cleared and the list shall reload.

---

### US-5: Select Venues (always-on selection)

**As an** admin
**I want to** select venues by tapping rows
**So that** I can batch-edit or batch-delete

#### Acceptance Criteria

- AC-5.1: Selection is always active (`SelectionMode.Multiple` hardcoded in XAML — no mode toggle).
- AC-5.2: Tapping a row shall toggle its selection state natively via `DXCollectionView`.
- AC-5.3: The app bar title shall show "Venues" when nothing is selected and "N selected" when N ≥ 1.
- AC-5.4: The FloatingToolbar `Select All` button shall select all loaded items when not all are selected, and deselect all when all are selected.
- AC-5.5: Selection state shall be preserved across list refreshes (restored by matching IDs).

---

## Data Model

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `Id` | `int` | PK, auto-increment | EF Core generated |
| `Name` | `string` | required, minLen=2, maxLen=30, unique (case-insensitive) | Trimmed before save |

**Navigation properties:**
- `ICollection<Event> Events` — zero-or-more events. Venue cannot be deleted while it has events.

---

## Validation Rules

| Field | Rule | Error message |
|-------|------|---------------|
| Name | required (not null/whitespace) | "Venue name is required" |
| Name | minLen = 2 | "Name is too short. Minimum is 2 characters." |
| Name | maxLen = 30 | "Name is too long. Maximum 30 characters." |
| Name | unique (case-insensitive) | "There is another venue registered with this name" |
| Name | uniqueness on edit | Excludes the venue being edited |

**Character counter thresholds:**
| Length | State |
|--------|-------|
| ≤ 25 | Hidden |
| 26–27 | Visible, neutral color |
| 28–29 | Visible, warning color |
| 30 | Visible, error color |

---

## Out of Scope

- Soft delete / archive — venues are hard-deleted
- Venue address, geolocation, or contact info — deferred to a future version
- Singer self-registration of venues — admin-only
- Export or import of venue list
