---
id: BUG-077
title: Flyout About item does not navigate to the About page
status: 🟡 In Progress
severity: Major
target: 2026-08-20
section: BusinessFeatures
parent: about-page
goal: Tapping About in the flyout leaves the user on the current page; the About page has never been reachable in the shipped app.
gate: Flyout About item navigates to AboutPage; manual E2E recorded in the task-log.
kind: bug
---

# Flyout About item does not navigate to the About page

Tapping About in the flyout leaves the user on the current page; the About page has never been reachable in the shipped app.


## Report

Reported by Helder 2026-08-20: tapping the **About** entry in the hamburger/flyout does not open the
About page — the user stays on the current page. Per the report the About page has **never** been
reachable in the shipped app, so the About feature's manual E2E was presumably never exercised end to end.

## Severity rationale

**Major** — a shipped feature is entirely unusable with no workaround (there is no other entry point to
the About page). No data loss or crash, so not Critical.

## Scope

Diagnosis and fix live in `AppShell` / flyout navigation wiring. The page content itself is unaffected;
the concurrent change `../../changes/2026-08-20-about-page-license-text-mit/` updates its license text.

## Regression test

Major → mandatory where testable. If the root cause sits in testable non-UI code, a failing test comes
first. If it is pure XAML/Shell wiring with no testable seam, manual E2E is recorded here instead
(`bug-tracking.md`).

## Status

Fix in progress on branch `fix/about-page-license-and-nav`. Root cause, changed files and verification
evidence are appended to `../../task-log.md`.
