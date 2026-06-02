# Crash & Error Reporting — Requirements
**Date:** 2026-05-30  
**Status:** Draft — pending Helder review

---

## Problem Statement

Unhandled exceptions in production builds are currently silent: `GlobalExceptionHandler` exists but is never initialized, and `LoggingConfiguration` is orphaned (configured but not registered). When a user hits a crash, the dev team has no signal. Bugs may go unreported for weeks or never be reported at all.

---

## Domain Vocabulary

| Term | Definition |
|------|-----------|
| **Crash event** | An unhandled exception that terminates or destabilizes the app |
| **Error event** | A handled exception or `Error`/`Fatal` log entry that does not crash the app |
| **Sentry** | Third-party crash reporting SaaS; the chosen crash aggregation backend |
| **DSN** | Sentry Data Source Name — the endpoint URL that identifies the project |
| **Breadcrumb** | A structured log entry Sentry attaches to a crash event as contextual trail |
| **Session** | A single app launch from cold start to background/termination |
| **Anonymous session ID** | A GUID generated per install, not per user — no PII |

---

## User Stories

### US-CRASH-01 — Dev team notified on new crash
**As** the dev team,  
**I want** to receive an alert when a new crash type occurs in a production build,  
**so that** I can investigate and release a fix before more users are affected.

### US-CRASH-02 — Crash context is actionable
**As** the dev team,  
**I want** each crash report to include the stack trace, app version, OS version, device model, and a breadcrumb trail of recent log events,  
**so that** I can reproduce the issue without needing to contact the user.

### US-CRASH-03 — Debug builds are isolated
**As** the dev team,  
**I want** crash events from debug/development builds to not pollute the production Sentry project,  
**so that** alert noise is minimized and production metrics are accurate.

### US-CRASH-04 — Existing Serilog pipeline is unified
**As** the dev team,  
**I want** all existing `ILogger` calls at `Error` and `Fatal` level to automatically appear as Sentry events,  
**so that** I don't need to instrument the codebase separately for crash reporting.

### US-CRASH-05 — User is not exposed to crash detail
**As** a user,  
**I want** the app to handle crashes gracefully without displaying raw stack traces,  
**so that** I am not confused or alarmed by technical error output.

---

## Acceptance Criteria

### AC-CRASH-01 — Unhandled exception captured
**Given** a release build is running,  
**When** an unhandled exception occurs (any thread, async task, or Android platform thread),  
**Then** the exception is captured in Sentry within 60 seconds, with stack trace and context.

### AC-CRASH-02 — Required context present
**Given** a Sentry event is created,  
**Then** it includes: `app.version` (from `AppInfo.VersionString`), OS name + version, device model, and anonymous session GUID.  
**And** the event does NOT include any personally identifiable information.

### AC-CRASH-03 — Serilog forwarding
**Given** any `ILogger.LogError` or `ILogger.LogCritical` call executes in a release build,  
**Then** a corresponding Sentry event is created, with preceding `Information`-level log entries attached as breadcrumbs (max 50 breadcrumbs).

### AC-CRASH-04 — Debug isolation
**Given** the app is running in DEBUG configuration,  
**Then** no events are sent to Sentry; all log output goes to the local Debug sink and file sink only.

### AC-CRASH-05 — Alert on first occurrence
**Given** a new crash fingerprint (unique issue) occurs in Sentry,  
**Then** an alert email is sent to the configured address on first occurrence.  
*(Alert configuration is done in the Sentry dashboard, not in app code.)*

### AC-CRASH-06 — Serilog file sink active
**Given** the app starts in any configuration,  
**Then** Serilog writes to a rolling daily log file at `{AppDataDirectory}/logs/myvocalist-.log` with 7-day retention.  
*(This fixes the orphaned `LoggingConfiguration` that is currently never registered.)*

### AC-CRASH-07 — GlobalExceptionHandler initialized
**Given** the app starts,  
**Then** `GlobalExceptionHandler.Initialize()` is called before the first page is displayed.  
*(This fixes the current bug where unhandled exceptions are not intercepted.)*

---

## Validation Rules

- DSN must be a non-empty string; if absent or empty in release builds, app starts but logs a local warning and disables Sentry silently (no crash on missing DSN).
- Anonymous session GUID is generated once per install via `Preferences` and never reset.
- Breadcrumb level floor is `Information` — `Debug`/`Verbose` entries are not forwarded to Sentry.

---

## Out of Scope

- User-visible crash recovery UI (modal, restart prompt) — future feature
- Sentry performance tracing / distributed tracing
- Sentry replay / session replay
- Self-hosted Sentry deployment (documented as alternative in design.md; not implemented at MVP)
- Monitoring agent / scheduled Claude agent polling Sentry API (deferred post-MVP)
- Symbolication / dSYM upload automation (manual process for now)
