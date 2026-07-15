---
name: myvocalist-coding
description: Use when implementing any MyVocaList feature — CRUD pages, DevExpress UI, EF Core queries, MAUI navigation, forms, dialogs, themes, or database schema in the MyVocaList project.
---

# MyVocaList Coding Reference

## Overview
Project-specific coding rules for the MyVocaList .NET MAUI app. Read the relevant library files before implementing — they contain confirmed patterns and non-negotiable constraints.

## Rule Files — Read Before Coding

> **Token-scoped reads (2026-07-14):** the four large rule families (`devexpress-patterns`, `crud-pages`, `m3-components`, `workflow-reference`) are split into section files. Read ONLY the section file(s) your task needs — the family index file lists them. Never read a whole family.

| Task | File |
|------|------|
| C# style, naming, async, DI lifetimes, service return tuples, exception handling, global usings | `.claude/library/code-style-reference.md` |
| Any UI work — CRUD laws + index | `.claude/library/crud-pages.md` (index) → `crud-listview.md` · `crud-appbar-list-toolbar.md` · `crud-form-page.md` · `crud-checklists.md` · `crud-migration-specfirst.md` · `crud-supporting.md` |
| DevExpress components — **FIRST, always** read `dx-audit-namespaces.md`, then the component section | `.claude/library/devexpress-patterns.md` (index) → `dx-buttons.md` · `dx-collectionview.md` · `dx-visual-containers.md` · `dx-editors.md` · `dx-bottomsheet-theme.md` · `dx-form-page.md` · `dx-misc-patterns.md` · `dx-appbars.md` · `dx-styles-gotchas.md` · `dx-subcomponents-styles.md` |
| EF Core entity config, migrations, repository queries | `.claude/library/database-indexing.md` |
| Dialogs, confirmations, BottomSheet, validation | `.claude/library/dialogs-validation.md` |
| MD3 AppBar, Lists, FloatingToolbar, EmptyState anatomy | `.claude/library/m3-components.md` (index + terminology) → `m3-appbars.md` · `m3-lists.md` · `m3-floating-toolbar.md` · `m3-emptystate-chips.md` |
| Colors, typography, DevExpress theme setup | `.claude/library/theme-locale.md` |
| Touch targets, multi-select UX, empty state positioning | `.claude/library/ux-patterns.md` |
| Changing a shared custom component (4-gate governance, consumer map, no-bundling) | `.claude/library/component-safety-gate.md` |
| Tracking a bug (BUG-NNN scheme, severity, regression-test requirement, task-log rules) | `.claude/library/bug-tracking-reference.md` |
| Discovered constraints (DevExpress/UI, .NET MAUI, EF Core/SQLite, .sln registration) | `.claude/library/constraints-reference.md` |
| Writing tests — structure, Service/ViewModel/Repository patterns, naming, Tester/Builder, anti-patterns | `.claude/library/testing-reference.md` (index) → `testing-structure.md` · `testing-test-types.md` · `testing-conventions.md` · `testing-tdd-roles.md` · `testing-quality-audit.md` (rule: `.claude/rules/testing.md`) |
| Mutation testing (Stryker) / property-based testing (FsCheck) | `.claude/library/mutation-testing-stryker.md` · `.claude/library/property-based-testing-fscheck.md` |
| Activating/adding/budgeting MCP servers (Security Stance, context budget, Playwright, response discipline) | `.claude/library/mcp-governance.md` |
| Governance narrative (SDD rationale, Continuous Enhancement procedure, Constitutional Audit, Tool Selection) | `.claude/library/project-governance-reference.md` |

## Non-Negotiables (always enforce, no exceptions)

- **DevExpress first**: Read `devexpress-patterns.md` before reaching for stock MAUI or a custom component.
- **No native dialogs**: Never `DisplayAlert`, `DisplayActionSheet`, `DisplayPromptAsync`. Use `dx:BottomSheet`.
- **MD3 terminology**: Component names, style keys, and BindableProperty names must match official MD3 spec (m3.material.io). No invented names.
- **SafeAreaEdges**: Every `ContentPage` needs `SafeAreaEdges="Container"` — MAUI 10 breaking change default is `None`.
- **Incremental UI edits**: Edit ONE XAML file → build → fix errors → then next file. Never batch UI changes.
- **Language**: All code, comments, logs, and UI text in English only.
