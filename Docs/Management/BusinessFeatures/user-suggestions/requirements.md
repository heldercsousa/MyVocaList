# User Suggestions — Requirements
**Date:** 2026-05-30  
**Status:** Draft — pending Helder review

---

## Problem Statement

There is no channel for users to report bugs or suggest features from within the app. Feedback that would inform the product roadmap is lost because the friction of emailing or finding a GitHub repo is too high for most users.

---

## Domain Vocabulary

| Term | Definition |
|------|-----------|
| **Suggestion** | A user-submitted message: bug report, feature request, or general comment |
| **Category** | The type of suggestion: Bug Report, Feature Request, or Other |
| **GitHub Issue** | The artifact created in the MyVocaList repository when a suggestion is submitted |
| **PAT** | Personal Access Token — fine-grained GitHub token with issue-create scope only |
| **Auto-captured metadata** | Device and app context appended automatically to the issue body |

---

## User Stories

### US-FB-01 — Submit a suggestion from within the app
**As** a user,  
**I want** to send feedback or a feature request directly from the app,  
**so that** I can communicate with the dev team without leaving the app or finding an email address.

### US-FB-02 — Know my feedback was received
**As** a user,  
**I want** to receive a confirmation when my suggestion is successfully submitted,  
**so that** I know it reached the dev team.

### US-FB-03 — Graceful failure
**As** a user,  
**I want** to be informed if submission fails and keep my message,  
**so that** I can try again without retyping.

### US-FB-04 — Dev team receives actionable context
**As** the dev team,  
**I want** each submission to include app version, OS, device model, and timestamp automatically,  
**so that** I can reproduce issues without asking follow-up questions.

---

## Acceptance Criteria

### AC-FB-01 — Successful submission creates a GitHub Issue
**Given** the user fills in a non-empty message and taps "Send",  
**Then** a GitHub Issue is created in the MyVocaList repository within 10 seconds (on good connectivity).  
**And** the issue title is `[{Category}] {first 60 chars of message}`.  
**And** the issue is labeled with the mapped GitHub label (`bug`, `enhancement`, or `question`).

### AC-FB-02 — Auto-captured metadata present
**Given** a GitHub Issue is created,  
**Then** its body includes: App version, OS name + version, device model, and submission timestamp (UTC ISO 8601).  
**And** the user's message appears verbatim above the metadata block.

### AC-FB-03 — Optional email included when provided
**Given** the user fills in the optional email field,  
**Then** the email is appended to the metadata block in the GitHub Issue body.  
**And** the email is never validated or stored locally beyond the current form session.

### AC-FB-04 — Empty message prevents submission
**Given** the message field is empty or whitespace-only,  
**Then** the "Send" button is disabled.

### AC-FB-05 — Success feedback
**Given** submission succeeds,  
**Then** a snackbar "Feedback sent — thank you!" is shown.  
**And** the form fields are cleared.

### AC-FB-06 — Failure feedback with preservation
**Given** a network error or GitHub API error occurs during submission,  
**Then** a snackbar "Could not send — please try again" is shown.  
**And** the form fields retain their content.

### AC-FB-07 — Duplicate prevention (basic)
**Given** the user taps "Send",  
**Then** the button is disabled until the API call completes (success or failure),  
**so that** double-tapping does not create duplicate issues.

---

## Validation Rules

- Message: required; max 1000 characters; leading/trailing whitespace trimmed before submission.
- Category: required; must be one of Bug Report / Feature Request / Other; defaults to Bug Report.
- Email: optional; no format validation; max 254 characters; stored only in the current form session, never persisted.
- PAT: must be present in configuration; if absent, submission fails with AC-FB-06 behavior and a local log warning.

---

## Category → GitHub Label Mapping

| Category | GitHub label |
|----------|-------------|
| Bug Report | `bug` |
| Feature Request | `enhancement` |
| Other | `question` |

The label `user-feedback` is always applied in addition to the category label, enabling the dev team to filter all user-submitted issues.

---

## Out of Scope

- User accounts or authentication
- In-app tracking of previously submitted suggestions
- Response / reply mechanism from dev team to user
- Attachment / screenshot upload
- Offline queue / retry on reconnect (submission fails immediately if offline)
- Moderation or spam filtering
- Rate limiting (GitHub API free tier is sufficient; abuse is low-risk for an indie app)
