# About Page License Text — MIT · Requirements

> **Change to shipped behavior.** Supersedes `../../requirements.md § AC-AB-05` only.
> The original spec remains immutable history; every other About-page AC is unchanged.

## Context

The repository was relicensed from CC BY-NC-ND 4.0 to the MIT License on 2026-08-20
(root `LICENSE` + `README.md`). The About page still renders the retired license name and
its non-commercial summary sentence, so the app now contradicts the repository it ships from.

## Acceptance criteria

### AC-AB-05a — License section (MIT) *(supersedes AC-AB-05)*
**Given** I open the About page,
**Then** the "License" section displays:
- License name: "MIT License"
- One-line summary: "Free to use, modify, and distribute."
- Copyright line: "© 2025 Helder Sousa" *(unchanged)*

### AC-AB-05b — No retired license text
**Given** I open the About page,
**Then** neither "CC BY-NC-ND 4.0" nor "Free for personal and non-commercial use. No derivatives."
appears anywhere on the page.

## Out of scope

- Layout, typography, section ordering, dividers — unchanged from the original design.
- The "What's New" section and `IWhatsNewService`.
- Making the license text data-driven (it stays a literal in XAML).
